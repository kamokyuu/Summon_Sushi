using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CookingCards.Editor
{
    public static class BatchEValidation
    {
        private static ConveyorController belt;
        private static IngredientCardHand hand;
        private static ComboShopBoss combo;
        public static void Assets()
        {
            BatchESetup.Apply();BatchDValidation.Assets();var config=Resources.Load<LevelConfig>("Levels/Level03");
            var ids=config.Recipes.Where(r=>r.Grade==RecipeGrade.Rare).Select(r=>r.Id).OrderBy(x=>x).ToArray();
            Require(string.Join(",",ids)=="maguro_nigiri,salmon_avocado_roll,tamago_avocado_roll","Rare menu mismatch.");
            var bag=RecipeDemandBag.Build(config);Require(bag.Count(k=>k==IngredientKind.Rice)==7&&bag.Count(k=>k==IngredientKind.Nori)==4&&bag.Count(k=>k==IngredientKind.Avocado)==4,"Topping balance mismatch.");
            foreach(var kind in config.activeIngredients)Require(config.Recipes.Any(r=>r.Required(kind)>0),"Unused active ingredient.");
        }
        public static async Task<string> Run()
        {
            await Load(2);combo.RecordFill();combo.Advance(4);Require(combo.ComboRemaining==10,"Tutorial did not pause combo.");GuidedLevelTutorial.Active.Skip();combo.Missed();
            var recipe=ActiveLevel.Recipes[0];float[] bonus={1,1.1f,1.2f};
            for(int i=0;i<3;i++){var p=Fill(recipe);Require(Mathf.Abs(p.SaleMultiplier-bonus[i])<.001f,"Combo bonus mismatch.");}
            Require(combo.Consecutive==3&&combo.ShopOpen,"3x shop trigger failed.");combo.Advance(5);Require(combo.ComboRemaining==10,"Shop did not pause combo.");combo.CloseShop();Fill(recipe);Fill(recipe);Require(combo.Consecutive==3&&!combo.ShopOpen&&combo.ComboBonus<=.2001f,"Combo/shop cap failed.");
            var pause=UnityEngine.Object.FindFirstObjectByType<Menu.GameplayPauseController>();pause.Pause();combo.Advance(5);Require(combo.ComboRemaining==10,"Game pause did not pause combo.");pause.Continue();combo.Advance(9);Require(combo.Consecutive==3,"Combo expired too early.");Fill(recipe);Require(combo.ComboRemaining==10,"Serve did not refresh timer.");combo.Advance(10.1f);Require(combo.Consecutive==0,"Combo did not expire.");
            Fill(recipe);Fill(recipe);Fill(recipe);Require(combo.ShopOpen,"New streak did not earn shop.");combo.CloseShop();combo.Missed();Require(combo.Consecutive==0,"Miss did not reset combo.");
            var status=GameObject.Find("ComboBossStatus");Require(status.GetComponent<UnityEngine.UI.Image>()==null&&!status.GetComponent<TMPro.TMP_Text>().raycastTarget,"Combo background or blocked input.");
            await Load(1);GuidedLevelTutorial.Active.Skip();var hud=GameplayHudFeedback.Active;Require(hud!=null&&((RectTransform)GameObject.Find("JournalButton").transform).rect.width<=56,"HUD/icon setup missing.");
            Fill(ActiveLevel.Recipes[0]);Fill(ActiveLevel.Recipes[0]);int before=belt.Revenue,funds=PreparationSession.Yen;float display=hud.DisplayedRevenue,startTime=Time.unscaledTime;
            Require(belt.BuyAvailable(2,null,hud.transform.TransformPoint(new Vector3(-200,60,0)))==2&&belt.Revenue==before+240&&PreparationSession.Yen==funds+240&&hud.InFlight==1,"Accounting delayed or duplicated.");
            var money=GameObject.Find("FloatingMoney");Require(money.GetComponent<TMPro.TMP_Text>().text=="+¥240"&&!money.GetComponent<CanvasGroup>().blocksRaycasts,"Money overlay/input mismatch.");await Task.Delay(200);Require(hud.InFlight==1&&money.transform.localScale.x<1&&hud.DisplayedRevenue==display,"Money did not fly/shrink or displayed too early.");
            for(int i=0;i<60&&hud.InFlight>0;i++)await Task.Delay(20);Require(hud.InFlight==0&&Time.unscaledTime-startTime>=.55f,"Money flight duration incorrect.");await Task.Delay(550);Require(Mathf.Abs(hud.DisplayedRevenue-(before+240))<1&&belt.Revenue==before+240&&PreparationSession.Yen==funds+240,"Arrival duplicated money or display failed.");
            var regular=Visit(ConveyorCustomerBehavior.Regular,1,null);Fill(ActiveLevel.Recipes[0]);regular.SendMessage("Update");Require(regular.Remaining==0&&regular.State==ConveyorCustomerVisit.VisitState.Leaving,"Regular behavior failed.");
            await Load(2);GuidedLevelTutorial.Active.Skip();ActiveLevel.Config.powerUpsEnabled=false;
            var picky=Visit(ConveyorCustomerBehavior.Picky,1,RecipeGrade.Uncommon);Fill(ActiveLevel.Recipes[0]);picky.SendMessage("Update");Require(picky.Remaining==1,"Picky bought wrong grade.");Fill(ActiveLevel.Recipes.First(r=>r.Grade==RecipeGrade.Uncommon));picky.SendMessage("Update");Require(picky.Remaining==0,"Picky did not buy preferred grade.");picky.enabled=false;belt.BuyAvailable(3);
            var hungry=Visit(ConveyorCustomerBehavior.Hungry,3,null);Fill(ActiveLevel.Recipes[0]);hungry.SendMessage("Update");Require(hungry.Remaining==2&&hungry.State==ConveyorCustomerVisit.VisitState.Ordering,"Hungry did not wait for multiple dishes.");Fill(ActiveLevel.Recipes[0]);Fill(ActiveLevel.Recipes[0]);hungry.SendMessage("Update");Require(hungry.Remaining==0,"Hungry order did not complete.");
            await Load(3);GuidedLevelTutorial.Active.Skip();ActiveLevel.Config.powerUpsEnabled=false;var stove=UnityEngine.Object.FindFirstObjectByType<IngredientStove>();var journal=UnityEngine.Object.FindFirstObjectByType<RecipeJournal>();journal.ShowTab((int)RecipeGrade.Rare);var seen=new System.Collections.Generic.HashSet<string>();for(int i=0;i<3;i++){seen.Add(journal.CurrentRecipe.Id);journal.Next();}Require(seen.Count==3,"Rare journal mismatch.");
            foreach(var rare in ActiveLevel.Recipes.Where(r=>r.Grade==RecipeGrade.Rare))
            {
                foreach(var card in hand.GetComponentsInChildren<IngredientCard>())hand.ConsumeCard(card);foreach(var item in rare.Ingredients)for(int i=0;i<item.Count;i++)Require(hand.TryReturnCard(item.Kind),"Missing topping prefab.");belt.SpawnPlate(RecipeGrade.Rare,0);
                foreach(var item in rare.Ingredients)for(int i=0;i<item.Count;i++)Require(stove.Deposit(hand.GetComponentsInChildren<IngredientCard>().First(c=>c.Ingredient==item.Kind)),"Deposit failed.");Require(stove.SummonActiveRecipe()&&belt.BuyAvailable(1)==1,"Rare recipe summon failed.");
                foreach(var item in rare.Ingredients)Require(stove.StoredCount(item.Kind)==0,"Ingredient notes stock mismatch.");
            }
            return "PASS: capped/timed combo and pause rules, one shop per streak, immediate accounting and 0.6s money flight, icon/nonblocking HUD, all customer behaviors, three Rare recipes/journal/stock";
        }
        private static ConveyorPlate Fill(SushiRecipe recipe){var p=belt.SpawnPlate(recipe.Grade,0);Require(p.TryServe(recipe),"Fill failed.");return p;}
        private static ConveyorCustomerVisit Visit(ConveyorCustomerBehavior behavior,int quantity,RecipeGrade? grade)
        {
            var owner=UnityEngine.Object.FindFirstObjectByType<ConveyorCustomers>();var go=new GameObject("VisitFixture",typeof(RectTransform),typeof(ConveyorCustomerVisit));go.transform.SetParent(owner.transform,false);var text=new GameObject("Bubble",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));text.transform.SetParent(go.transform,false);text.GetComponent<TMPro.TMP_Text>().raycastTarget=false;var visit=go.GetComponent<ConveyorCustomerVisit>();visit.Configure(owner,belt,text.GetComponent<TMPro.TMP_Text>(),0,0,quantity,80,behavior,grade);return visit;
        }
        private static async Task Load(int level)
        {
            PreparationSession.BeginBeginnerLevel();var copy=UnityEngine.Object.Instantiate(Resources.Load<LevelConfig>("Levels/Level0"+level));copy.name="Level0"+level;ActiveLevel.Select(copy);SceneManager.LoadScene("MainScene");await Task.Delay(500);
            for(int i=0;i<100;i++){hand=UnityEngine.Object.FindFirstObjectByType<IngredientCardHand>();if(hand!=null&&!hand.IsDealing){await Task.Delay(100);belt=UnityEngine.Object.FindFirstObjectByType<ConveyorController>();combo=ComboShopBoss.Active;UnityEngine.Object.FindFirstObjectByType<ConveyorCustomers>().enabled=false;return;}await Task.Delay(100);}throw new Exception("Opening timeout.");
        }
        private static void Require(bool value,string message){if(!value)throw new Exception(message);}
    }
}
