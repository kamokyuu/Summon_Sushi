using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CookingCards.Editor
{
    public static class BatchCValidation
    {
        private static IngredientCardHand hand;
        private static ConveyorController belt;
        private static ComboShopBoss feature;
        private static LevelConfig runtime;
        private static SushiRecipe common;
        public static void Assets()
        {
            BatchCSetup.Apply();VerticalSliceBatchSetup.Validate();IngredientRebalanceValidation.Run();
            for(int n=1;n<=3;n++)
            {
                var config=Resources.Load<LevelConfig>("Levels/Level0"+n);
                for(int bag=0;bag<100;bag++)
                {
                    var cards=RecipeDemandBag.Build(config);Require(cards.All(config.Allows),"Inactive resource.");
                    if(n==1)Require(cards.Count(k=>k==IngredientKind.Rice)==10,"Level01 changed.");
                    else Require(cards.Count(k=>k==IngredientKind.Rice)>=(n==3?7:9)&&cards.Count(k=>k==IngredientKind.Nori)>=3&&cards.Count(k=>k==IngredientKind.Avocado||k==IngredientKind.Salmon||k==IngredientKind.Tuna)<=6,"Basic resource balance failed.");
                }
                Require(config.manualDrawCooldownSeconds==0&&config.mergeEnabled==(n>1)&&config.knifeEnabled==(n==3),"Wrong gates.");
            }
        }
        public static async Task<string> Run()
        {
            Require(Application.isPlaying,"Play mode required.");
            SceneManager.LoadScene("MainMenu");await Task.Delay(500);UnityEngine.Object.FindFirstObjectByType<Menu.MainMenuController>().ShowPanel(0);await Ready();
            Require(ActiveLevel.Config.name=="Level01"&&feature==null&&hand.CardCount==5&&hand.Capacity==7,"Normal beginner entry changed.");
            hand.DrawCard();Cook(common);Require(belt.BuyAvailable(1)==1&&!GuidedLevelTutorial.Protecting,"Beginner guided actions failed.");
            while(!DayShiftController.Active.Won)Sale();DayShiftController.Active.Advance(0);Next();await Ready();
            Require(ActiveLevel.Config.name=="Level02"&&ActiveLevel.MergeEnabled&&!ActiveLevel.KnifeEnabled,"Level02 progression/gates.");
            var journal=UnityEngine.Object.FindFirstObjectByType<RecipeJournal>();journal.Toggle();journal.Toggle();
            var guide=GuidedLevelTutorial.Active;Require(guide.Current==LevelGuideStep.PlateMatch,"Journal guide failed.");
            Cook(ActiveLevel.Recipes.First(r=>r.Grade==RecipeGrade.Uncommon));Require(guide.Current==LevelGuideStep.Merge,"Level02 merge guide missing.");
            var rice=hand.GetComponentsInChildren<IngredientCard>().Where(c=>c.Ingredient==IngredientKind.Rice).Take(2).ToArray();Require(rice.Length==2&&hand.TryCombine(rice[0],rice[1])&&!guide.IsGuiding,"Contextual Rice merge failed.");belt.BuyAvailable(3);
            ClearHand();hand.TryReturnCard(IngredientKind.Nori);hand.TryReturnCard(IngredientKind.Nori);var nori=hand.GetComponentsInChildren<IngredientCard>();Require(hand.TryCombine(nori[0],nori[1]),"Nori merge failed.");Require(!hand.TrySplitCard(hand.GetComponentsInChildren<IngredientCard>().First()),"Level02 knife enabled.");
            feature.Missed();int before=belt.Revenue;float[] multipliers={1,1.1f,1.2f,1.2f,1.2f};
            for(int i=0;i<5;i++)
            {
                var plate=belt.SpawnPlate(common.Grade,0);Require(plate.TryServe(common),"Combo fill failed.");Require(Mathf.Abs(plate.SaleMultiplier-multipliers[i])<.001f,"Wrong combo snapshot.");
                if(i==2){Require(feature.ShopOpen&&feature.Offered.Count==3,"Third fill shop failed.");feature.CloseShop();}
            }
            feature.Missed();while(belt.HasAvailableFood)belt.BuyAvailable(3);Require(belt.Revenue-before==684,"Snapshots not paid exactly once.");Require(belt.BuyAvailable(3)==0,"Double credit.");
            runtime=UnityEngine.Object.Instantiate(ActiveLevel.Config);ActiveLevel.Select(runtime);PreparationSession.Reward(5000);
            Power(PowerUpEffect.ExtraHand);Require(hand.Capacity==8,"Extra hand failed.");
            ClearHand();runtime.powerUps=new[]{Option(PowerUpEffect.LuckyDraw)};Require(feature.OpenShop()&&feature.Purchase(0),"Lucky Draw selection failed.");int funds=PreparationSession.Yen;Require(!feature.PickResource(IngredientKind.Ikura)&&PreparationSession.Yen==funds,"Invalid Lucky Draw charged.");Require(feature.PickResource(IngredientKind.Nori)&&hand.CardCount==3&&PreparationSession.Yen==funds-250,"Lucky Draw delivery failed.");Require(!feature.PickResource(IngredientKind.Nori),"Duplicate Lucky Draw.");
            while(hand.CardCount<hand.Capacity)hand.TryReturnCard(IngredientKind.Rice);Require(!feature.OpenShop(),"Full-hand Lucky Draw offered.");
            Power(PowerUpEffect.ConveyorFreeze);var frozenPlate=belt.SpawnPlate(common.Grade,0);float x=((RectTransform)frozenPlate.transform).anchoredPosition.x;await Task.Delay(150);Require(((RectTransform)frozenPlate.transform).anchoredPosition.x==x,"Freeze did not stop conveyor.");
            runtime.powerUps=new[]{Option(PowerUpEffect.ExtraHand)};Require(feature.OpenShop(),"Shop during freeze failed.");feature.Advance(10.1f);Require(!feature.ShopOpen&&ComboShopBoss.ConveyorStopped,"Shop timeout removed independent freeze.");feature.Advance(31);Require(!ComboShopBoss.ConveyorStopped,"Freeze failed to expire.");
            Power(PowerUpEffect.PlateShield);feature.RecordFill();int streak=feature.Consecutive,missed=belt.MissedCount;var shielded=belt.SpawnPlate(common.Grade,0);shielded.BreakIfEmpty();shielded.BreakIfEmpty();Require(!shielded.IsBroken&&feature.ShieldCharges==0&&feature.Consecutive==streak&&belt.MissedCount==missed,"Shield failed.");belt.SpawnPlate(common.Grade,0).BreakIfEmpty();Require(feature.Consecutive==0&&belt.MissedCount==missed+1,"Unprotected miss did not reset.");
            runtime.powerUps=new[]{Option(PowerUpEffect.ExtraHand)};Require(feature.OpenShop(),"Funds fixture.");PreparationSession.Reward(-PreparationSession.Yen);Require(!feature.Purchase(0)&&feature.ShopOpen,"Insufficient funds accepted.");feature.Advance(10.1f);PreparationSession.Reward(5000);
            while(!DayShiftController.Active.Won)Sale();DayShiftController.Active.Advance(0);Next();UnityEngine.Object.Destroy(runtime);runtime=null;await Ready();
            Require(ActiveLevel.Config.name=="Level03"&&feature.ExtraHandSlots==0&&!ComboShopBoss.ConveyorStopped,"Temporary effects leaked.");
            guide=GuidedLevelTutorial.Active;Require(guide.Current==LevelGuideStep.Merge,"Level03 merge guide.");rice=hand.GetComponentsInChildren<IngredientCard>().Where(c=>c.Ingredient==IngredientKind.Rice).Take(2).ToArray();Require(hand.TryCombine(rice[0],rice[1]),"Level03 Rice merge.");var knife=UnityEngine.Object.FindFirstObjectByType<CardKnifeTool>().EnsureTool();var card=hand.GetComponentsInChildren<IngredientCard>().First(c=>c.Ingredient==IngredientKind.Nori);Require(knife.TryUse(card)&&knife.Charges==1&&!guide.IsGuiding,"Level03 reverse chain.");
            runtime=UnityEngine.Object.Instantiate(ActiveLevel.Config);ActiveLevel.Select(runtime);PreparationSession.Reward(5000);Power(PowerUpEffect.KnifeRefill);Require(knife.Charges==2&&!knife.Refill(1),"Knife refill cap.");
            int net()=>belt.Revenue-belt.Penalties;
            while(net()<DayShiftController.Active.TargetYen)Sale();DayShiftController.Active.Advance(0);Require(feature.BossStarted&&!DayShiftController.Active.HasEnded,"Boss not required for success.");
            Sale();Sale();DayShiftController.Active.Advance(0);Require(feature.BossComplete&&DayShiftController.Active.HasEnded&&DayShiftController.Active.Won&&feature.Results().Contains("DEFEATED"),"Boss/final results failed.");
            var nav=UnityEngine.Object.FindFirstObjectByType<Menu.ResultsNavigation>();Require(nav.transform.Find("ResultsNavigation/REPLAY").GetComponentInChildren<TMPro.TMP_Text>().text=="REPLAY","Final replay missing.");
            ActiveLevel.Select(Resources.Load<LevelConfig>("Levels/Level03"));nav.Replay();UnityEngine.Object.Destroy(runtime);runtime=null;await Ready();GuidedLevelTutorial.Active.Skip();feature.CheckBoss(true);feature.Advance(31);DayShiftController.Active.Advance(0);Require(feature.BossFailed&&DayShiftController.Active.HasEnded&&!DayShiftController.Active.Won,"Boss timeout softlock.");
            return "PASS: normal entry/progression, restored chains/gates, basic bags, tutorials, combo snapshot/reset, shop trigger/timeout, all five power-ups, boss success/failure/results";
        }
        private static LevelPowerUp Option(PowerUpEffect effect)=>new LevelPowerUp{title=effect.ToString(),description="Test",effect=effect};
        private static void Power(PowerUpEffect effect)
        {
            runtime.powerUps=new[]{Option(effect)};int funds=PreparationSession.Yen;Require(feature.OpenShop()&&feature.Purchase(0),"Power failed: "+effect);Require(PreparationSession.Yen==funds-250&&!feature.ShopOpen&&!feature.Purchase(0),"Power charged twice.");
        }
        private static void Sale(){var plate=belt.SpawnPlate(common.Grade,0);Require(plate.TryServe(common),"Sale fill failed.");feature?.CloseShop();Require(belt.BuyAvailable(1)==1,"Sale purchase failed.");}
        private static void Cook(SushiRecipe recipe)
        {
            var stove=UnityEngine.Object.FindFirstObjectByType<IngredientStove>();foreach(var item in recipe.Ingredients)for(int i=0;i<item.Count;i++)Require(stove.Deposit(hand.GetComponentsInChildren<IngredientCard>().First(c=>c.Ingredient==item.Kind)),"Guided ingredient missing.");Require(stove.SummonActiveRecipe(),"Guided summon failed.");
        }
        private static void ClearHand(){foreach(var card in hand.GetComponentsInChildren<IngredientCard>())hand.ConsumeCard(card);}
        private static void Next(){UnityEngine.Object.FindFirstObjectByType<Menu.ResultsNavigation>().transform.Find("ResultsNavigation/REPLAY").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();}
        private static async Task Ready()
        {
            await Task.Delay(500);for(int i=0;i<100;i++){hand=UnityEngine.Object.FindFirstObjectByType<IngredientCardHand>();if(hand!=null&&!hand.IsDealing){await Task.Delay(200);belt=UnityEngine.Object.FindFirstObjectByType<ConveyorController>();feature=ComboShopBoss.Active;common=ActiveLevel.Recipes.First(r=>r.Grade==RecipeGrade.Common);return;}await Task.Delay(100);}throw new Exception("Deal timeout.");
        }
        private static void Require(bool value,string message){if(!value)throw new Exception(message);}
    }
}
