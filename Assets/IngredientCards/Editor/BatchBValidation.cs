using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CookingCards.Editor
{
    public static class BatchBValidation
    {
        public static async Task<string> Run()
        {
            Require(Application.isPlaying,"Play mode required.");
            Require(ConveyorController.Price(RecipeGrade.Common)==120&&ConveyorController.Price(RecipeGrade.Uncommon)==180&&ConveyorController.Price(RecipeGrade.Rare)==260&&ConveyorController.Price(RecipeGrade.Special)==400,"Wrong prices.");
            foreach(var config in Resources.LoadAll<LevelConfig>("Levels"))Require(config.manualDrawCooldownSeconds==0,"Nonzero cooldown.");
            for(int level=1;level<=3;level++)
            {
                PreparationSession.BeginBeginnerLevel();var fixture=UnityEngine.Object.Instantiate(Resources.Load<LevelConfig>("Levels/Level0"+level));fixture.comboEnabled=fixture.powerUpsEnabled=fixture.bossCustomersEnabled=false;ActiveLevel.Select(fixture);SceneManager.LoadScene("MainScene");
                await WaitForDeal();
                var hand=UnityEngine.Object.FindFirstObjectByType<IngredientCardHand>();var guide=GuidedLevelTutorial.Active;
                Require(!hand.IsDealing&&guide.IsGuiding,"Guide did not start.");
                var belt=UnityEngine.Object.FindFirstObjectByType<ConveyorController>();var day=DayShiftController.Active;
                float remaining=day.Remaining;day.Advance(999);belt.RecordMissed(RecipeGrade.Common);
                Require(day.Remaining==remaining&&!day.HasEnded&&belt.Penalties==0,"Guided penalty/timer protection failed.");
                if(level==1)
                {
                    Require(guide.Current==LevelGuideStep.Draw,"Draw instruction missing.");hand.DrawCard();int count=hand.CardCount;hand.DrawCard();Require(hand.CardCount==count&&!hand.CanDraw,"Duplicate animation input.");
                    Require(guide.Current==LevelGuideStep.Summon,"Draw did not advance.");
                    await Task.Delay(450);Require(hand.CanDraw||hand.CardCount==ActiveLevel.Config.maximumHandCapacity,"Animation lock persists.");
                    Cook(hand,belt,ActiveLevel.Recipes[0]);Require(guide.Current==LevelGuideStep.Serve,"Summon did not advance.");
                    Require(belt.BuyAvailable(1)==1&&!guide.IsGuiding,"Serve did not advance.");
                }
                if(level==2)
                {
                    Require(guide.Current==LevelGuideStep.Journal,"Journal instruction missing.");
                    var journal=UnityEngine.Object.FindFirstObjectByType<RecipeJournal>();journal.Toggle();Require(guide.Current==LevelGuideStep.PlateMatch,"Journal did not advance.");journal.Toggle();
                    var recipe=ActiveLevel.Recipes.First(r=>r.Grade==RecipeGrade.Uncommon);
                    Require(!belt.SpawnPlate(RecipeGrade.Common,0).TryServe(recipe)&&guide.IsGuiding,"Wrong plate accepted.");
                    Cook(hand,belt,recipe);Require(guide.Current==LevelGuideStep.Merge,"Matching plate did not advance tutorial.");
                    var basics=hand.GetComponentsInChildren<IngredientCard>().Where(c=>c.Ingredient==IngredientKind.Rice).Take(2).ToArray();Require(hand.TryCombine(basics[0],basics[1])&&!guide.IsGuiding,"Level02 merge step failed.");
                    var placeholder=UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None).FirstOrDefault(t=>t.gameObject.name=="ServedRecipe");
                    // Avocado Maki uses its existing sprite; Maguro below must have an honest named fallback.
                    var fallback=ActiveLevel.Recipes.First(r=>r.Id=="maguro_nigiri");var plate=belt.SpawnPlate(fallback.Grade,0);Require(plate.TryServe(fallback),"Fallback serve failed.");
                    Require(plate.GetComponentInChildren<TMPro.TMP_Text>().text==fallback.DisplayName&&plate.transform.Find("RecipePlaceholder")!=null,"Wrong fallback visual.");
                }
                var knife=UnityEngine.Object.FindFirstObjectByType<CardKnifeTool>().EnsureTool();
                if(level<3)Require(!ActiveLevel.KnifeEnabled&&knife.Charges==0,"Knife enabled too early.");
                else
                {
                    Require(guide.Current==LevelGuideStep.Merge&&knife.Charges==2&&knife.MaximumCharges==2,"Level03 knife setup failed.");
                    var pair=hand.GetComponentsInChildren<IngredientCard>().Where(c=>c.Ingredient==IngredientKind.Rice).Take(2).ToArray();Require(pair.Length==2&&hand.TryCombine(pair[0],pair[1]),"Guided merge failed.");
                    Require(guide.Current==LevelGuideStep.Knife,"Merge did not advance.");
                    Require(!knife.TryUse(null)&&knife.Charges==2,"Invalid cut consumed charge.");
                    var tuna=hand.GetComponentsInChildren<IngredientCard>().First(c=>c.Ingredient==IngredientKind.Nori);Require(knife.TryUse(tuna)&&knife.Charges==1&&!guide.IsGuiding,"Guided cut failed.");
                    foreach(var card in hand.GetComponentsInChildren<IngredientCard>())hand.ConsumeCard(card);
                    hand.TryReturnCard(IngredientKind.Tuna);tuna=hand.GetComponentsInChildren<IngredientCard>().First();Require(knife.TryUse(tuna)&&knife.Charges==0,"Second cut failed.");
                    hand.TryReturnCard(IngredientKind.Tuna);Require(!knife.TryUse(hand.GetComponentsInChildren<IngredientCard>().First(c=>c.Ingredient==IngredientKind.Tuna))&&knife.Charges==0,"Knife exceeded two charges.");
                }
                foreach(var recipe in ActiveLevel.Recipes)
                {
                    var match=ActiveLevel.Config.foodVisuals.FirstOrDefault(v=>v.recipeId==recipe.Id);
                    if(recipe.Id=="ebi_nigiri"||recipe.Id=="tamago_nigiri"||recipe.Id=="avocado_maki"||recipe.Id=="salmon_nigiri")Require(match?.sprite!=null,"Existing artwork not mapped: "+recipe.Id);
                }
                while(!day.Won){var r=ActiveLevel.Recipes[0];Require(belt.SpawnPlate(r.Grade,0).TryServe(r),"Sale failed.");belt.BuyAvailable(1);}
                day.Advance(0);Require(day.HasEnded&&day.Remaining>0,"Revenue target did not complete immediately.");
            }
            PreparationSession.BeginBeginnerLevel();SceneManager.LoadScene("MainScene");await WaitForDeal();
            var skip=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b=>b.name=="Skip");skip.onClick.Invoke();Require(!GuidedLevelTutorial.Protecting,"Skip failed.");
            return "PASS: guided actions, skip, supply, plates, protection, prices, visuals, knife charges, animation lock, early completion";
        }
        private static void Cook(IngredientCardHand hand,ConveyorController belt,SushiRecipe recipe)
        {
            var stove=UnityEngine.Object.FindFirstObjectByType<IngredientStove>();
            foreach(var amount in recipe.Ingredients)for(int i=0;i<amount.Count;i++)Require(stove.Deposit(hand.GetComponentsInChildren<IngredientCard>().First(c=>c.Ingredient==amount.Kind)),"Missing guided ingredients.");
            Require(stove.SummonActiveRecipe(),"Guided summon failed.");
        }
        private static void Require(bool value,string message){if(!value)throw new Exception(message);}
        private static async Task WaitForDeal(){await Task.Delay(500);for(int i=0;i<60;i++){var h=UnityEngine.Object.FindFirstObjectByType<IngredientCardHand>();if(h!=null&&!h.IsDealing){await Task.Delay(200);return;}await Task.Delay(150);}throw new Exception("Opening deal timed out.");}
    }
}
