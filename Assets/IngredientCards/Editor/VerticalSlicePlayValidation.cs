using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
namespace CookingCards.Editor
{
    public static class VerticalSlicePlayValidation
    {
        public static async Task<string> Run()
        {
            Require(Application.isPlaying,"Enter Play mode first.");
            SceneManager.LoadScene("MainMenu");await Task.Delay(500);
            UnityEngine.Object.FindFirstObjectByType<Menu.MainMenuController>().ShowPanel(0);
            await WaitForHand();
            Require(ActiveLevel.Config.name=="Level01","Menu must start Level01.");
            var hand=UnityEngine.Object.FindFirstObjectByType<IngredientCardHand>();
            Require(hand.CardCount==5,"Opening hand must contain five cards.");
            hand.DrawCard();Require(hand.CardCount==6,"First manual draw failed.");
            hand.DrawCard();Require(hand.CardCount==6&&!hand.CanDraw,"Immediate draw bypassed cooldown.");
            await Task.Delay(3100);hand.DrawCard();Require(hand.CardCount==7,"Draw did not unlock.");
            foreach(var card in hand.GetComponentsInChildren<IngredientCard>())hand.ConsumeCard(card);
            for(int i=0;i<7;i++)Require(hand.TryReturnCard(IngredientKind.Shrimp),"Fixture card failed.");
            Require(!hand.TryExchangeSelectedCard(),"Exchange requires selection.");
            hand.GetComponentsInChildren<IngredientCard>().First(c=>c.gameObject.activeSelf).OnPointerClick(new PointerEventData(EventSystem.current));
            Require(hand.TryExchangeSelectedCard(),"Full-hand exchange failed.");
            Require(hand.CardCount==7&&hand.GetComponentsInChildren<IngredientCard>().Any(c=>c.gameObject.activeSelf&&c.Ingredient==IngredientKind.Rice),"Exchange must restore missing resource without increasing hand size.");
            Require(!hand.TryExchangeSelectedCard(),"Exchange bypassed cooldown.");
            for(int n=1;n<=3;n++)
            {
                Require(ActiveLevel.Config.name=="Level0"+n,"Next-level selection failed.");
                hand=UnityEngine.Object.FindFirstObjectByType<IngredientCardHand>();
                var stove=UnityEngine.Object.FindFirstObjectByType<IngredientStove>();
                var belt=UnityEngine.Object.FindFirstObjectByType<ConveyorController>();
                var journal=UnityEngine.Object.FindFirstObjectByType<RecipeJournal>();
                Require(ActiveLevel.Recipes.Count==n*2,"Runtime recipe count mismatch.");
                if(n==3)
                {
                    foreach(var card in hand.GetComponentsInChildren<IngredientCard>())hand.ConsumeCard(card);
                    Require(hand.TryReturnCard(IngredientKind.Shrimp)&&hand.TryReturnCard(IngredientKind.Shrimp),"Merge fixture failed.");
                    var pair=hand.GetComponentsInChildren<IngredientCard>().Where(c=>c.gameObject.activeSelf).ToArray();
                    Require(hand.TryCombine(pair[0],pair[1])&&hand.CardCount==1,"Level03 merge failed.");
                    var tuna=hand.GetComponentsInChildren<IngredientCard>().First(c=>c.gameObject.activeSelf);
                    Require(tuna.Ingredient==IngredientKind.Tuna&&hand.TrySplitCard(tuna)&&hand.CardCount==2,"Level03 knife split failed.");
                }
                foreach(var recipe in ActiveLevel.Recipes)
                {
                    stove.ResetForNewGame();foreach(var card in hand.GetComponentsInChildren<IngredientCard>())hand.ConsumeCard(card);
                    journal.ShowTab((int)recipe.Grade);
                    bool found=false;
                    for(int page=0;page<n*2;page++){if(journal.CurrentRecipe?.Id==recipe.Id)found=true;journal.Next();}
                    Require(found,"Journal missing "+recipe.Id);
                    belt.SpawnPlate(recipe.Grade,0);
                    foreach(var item in recipe.Ingredients)for(int c=0;c<item.Count;c++)
                    {
                        Require(hand.TryReturnCard(item.Kind),"Missing artwork/prefab for "+item.Kind);
                        var card=hand.GetComponentsInChildren<IngredientCard>().First(x=>x.gameObject.activeSelf);
                        Require(stove.Deposit(card),"Deposit rejected active ingredient.");
                    }
                    foreach(var item in recipe.Ingredients)Require(stove.StoredCount(item.Kind)==item.Count,"Deposited stock mismatch.");
                    var notes=(TMPro.TMP_Text)new UnityEditor.SerializedObject(stove).FindProperty("inventoryLabel").objectReferenceValue;
                    Require(notes!=null,"Ingredient notes reference missing.");
                    foreach(var item in recipe.Ingredients)Require(notes.text.Contains(IngredientRules.DisplayName(item.Kind)+" × "+item.Count),"Ingredient notes mismatch.");
                    Require(stove.SummonActiveRecipe()&&stove.CookedCount==1,"Summon failed for "+recipe.Id);
                    Require(notes.text=="BOX STOCK","Ingredient notes did not clear after summon.");
                    foreach(var item in recipe.Ingredients)Require(stove.StoredCount(item.Kind)==0,"Stock not consumed.");
                    Require(belt.BuyAvailable(1)==1,"Served food not purchasable.");
                }
                var day=DayShiftController.Active;
                while(!day.Won){var recipe=ActiveLevel.Recipes[0];Require(belt.SpawnPlate(recipe.Grade,0).TryServe(recipe),"Sale fixture failed.");belt.BuyAvailable(1);}
                day.Advance(day.Remaining+1);Require(day.HasEnded&&day.Won,"Successful results failed.");
                var nav=UnityEngine.Object.FindFirstObjectByType<Menu.ResultsNavigation>();
                var button=nav.transform.Find("ResultsNavigation/REPLAY").GetComponent<UnityEngine.UI.Button>();
                Require(button.GetComponentInChildren<TMPro.TMP_Text>().text==(n<3?"NEXT LEVEL":"REPLAY"),"Wrong results action.");
                if(n<3){button.onClick.Invoke();await WaitForHand();}
            }
            Debug.Log("Batch A play validation passed: menu entry; opening hand; 3s cooldown; controlled exchange; all 12 recipe summons, stock consumption and journal entries; purchases; results progression Level01 -> Level02 -> Level03.");
            return "PASS: menu, cooldown, exchange, 12 summons/journal/notes, purchases, next-level results";
        }
        private static async Task WaitForHand()
        {
            await Task.Delay(500);
            for(int i=0;i<80;i++){var hand=UnityEngine.Object.FindFirstObjectByType<IngredientCardHand>();if(hand!=null&&!hand.IsDealing)return;await Task.Delay(100);}
            throw new Exception("Opening deal timed out.");
        }
        private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    }
}
