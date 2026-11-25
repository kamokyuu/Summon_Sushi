using System;
using UnityEditor;
using UnityEngine;

namespace CookingCards.Editor
{
    public static class GameJamRulesValidation
    {
        [MenuItem("Tools/Cooking Cards/Validate Jam Recipes And Draws")]
        public static void Run()
        {
            var state=UnityEngine.Random.state;
            try
            {
                Require(VerticalSliceRecipes.All.Count==5,"Expected 4 conveyor recipes and Salmon Sashimi.");
                int conveyor=0,direct=0;
                foreach(var recipe in VerticalSliceRecipes.All)
                {
                    if(recipe.Service==RecipeService.Conveyor)conveyor++;else{direct++;Require(recipe.Id=="salmon_sashimi","Only Salmon Sashimi is a direct order.");}
                    var inventory=new int[IngredientRules.Count];
                    foreach(var item in recipe.Ingredients)
                    {
                        Require(GameJamRules.Allows(item.Kind),"Inactive ingredient in "+recipe.Id);
                        for(int i=0;i<item.Count;i++)
                        {Require(ActiveRecipeRules.CanAdd(inventory,item.Kind),"Valid deposit rejected: "+recipe.Id);inventory[(int)item.Kind]++;}
                    }
                    Require(ActiveRecipeRules.TryCook(inventory,out var cooked) && cooked.Id==recipe.Id,"Wrong result: "+recipe.Id);
                    foreach(int remaining in inventory)Require(remaining==0,"Leftover ingredients: "+recipe.Id);
                    Require(!ActiveRecipeRules.TryCook(inventory,out _),"Empty box cooked food.");
                }
                Require(conveyor==4 && direct==1,"Wrong service counts.");
                Require(!VerticalSliceRecipes.Contains("maguro_sashimi") && !VerticalSliceRecipes.Contains("tamago_nigiri"),"Inactive recipe available.");
                Require(!ActiveRecipeRules.CanAdd(new int[IngredientRules.Count],IngredientKind.Eggs),"Roe accepted.");
                var partial=new int[IngredientRules.Count];partial[(int)IngredientKind.Rice]=1;partial[(int)IngredientKind.Shrimp]=1;
                Require(!ActiveRecipeRules.TryCook(partial,out _) && partial[(int)IngredientKind.Shrimp]==1,"Legacy Ebi recipe consumed ingredients.");
                UnityEngine.Random.InitState(17011);
                int first=0,second=0,third=0;
                for(int batch=0;batch<10000;batch++)
                {
                    var cards=GameJamRules.CreateDrawBatch();int midori=0,umi=0;
                    Require(cards.Count==5,"Bag must have five cards.");
                    foreach(var kind in cards)
                    {
                        Require(GameJamRules.Allows(kind),"Inactive draw.");
                        if(kind==IngredientKind.Rice || kind==IngredientKind.Nori || kind==IngredientKind.Avocado)midori++;else umi++;
                        if(kind==IngredientKind.Rice || kind==IngredientKind.Shrimp)first++;
                        else if(kind==IngredientKind.Nori || kind==IngredientKind.Tuna)second++;else third++;
                    }
                    Require(midori==3 && umi==2,"Pack quota broken.");
                }
                Require(Math.Abs(first/50000f-.9f)<.01f && Math.Abs(second/50000f-.08f)<.01f && Math.Abs(third/50000f-.02f)<.01f,"Tier odds outside tolerance.");
                Debug.Log("JAM VALIDATION PASSED: 5 recipes, exact consumption, 10000 five-card bags, 3 Midori / 2 Umi; tiers="+first+"/"+second+"/"+third);
            }
            finally{UnityEngine.Random.state=state;}
        }
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        public static string RunPlayMode()
        {
            Require(EditorApplication.isPlaying,"Enter Play mode first.");
            var hand=UnityEngine.Object.FindFirstObjectByType<IngredientCardHand>();
            var box=UnityEngine.Object.FindFirstObjectByType<IngredientStove>();
            Require(hand!=null && box!=null && !hand.IsDealing,"Wait for the opening deal.");
            Require(!PreparationSession.Tutorial,"Legacy tutorial still active.");
            Require(hand.CardCount==7,"Opening hand must contain seven cards.");
            for(int batch=0;batch<50;batch++)
            {
                int rice=0;
                foreach(var card in hand.GetComponentsInChildren<IngredientCard>())
                {
                    Require(GameJamRules.Allows(card.Ingredient),"Inactive live card.");
                    if(card.Ingredient==IngredientKind.Rice)rice++;
                }
                foreach(var card in hand.GetComponentsInChildren<IngredientCard>())hand.ConsumeCard(card);
                for(int i=0;i<7;i++)hand.DrawCard();
                Require(hand.CardCount==7,"Live draw failed.");
            }
            foreach(var card in hand.GetComponentsInChildren<IngredientCard>())hand.ConsumeCard(card);
            Require(hand.TryReturnCard(IngredientKind.Nori) && hand.TryReturnCard(IngredientKind.Nori),"Missing Garden prefabs.");
            var pair=hand.GetComponentsInChildren<IngredientCard>();
            Require(!hand.TryCombine(pair[0],pair[1]),"Basic Nori must not merge.");
            Require(!hand.TrySplitCard(pair[0]),"Basic Nori must not split.");
            foreach(var card in hand.GetComponentsInChildren<IngredientCard>())hand.ConsumeCard(card);
            foreach(var recipe in VerticalSliceRecipes.All)
            {
                box.ResetForNewGame();
                if(recipe.Service==RecipeService.DirectOrder)continue;

                foreach(var item in recipe.Ingredients)
                    for(int i=0;i<item.Count;i++)
                    {
                        Require(hand.TryReturnCard(item.Kind),"Missing prefab: "+item.Kind);
                        Require(box.Deposit(hand.GetComponentInChildren<IngredientCard>()),"Live deposit failed: "+recipe.Id);
                    }
                Require(box.ReadyRecipe!=null && box.ReadyRecipe.Id==recipe.Id,"Live recipe mismatch.");
                if(recipe.Service==RecipeService.DirectOrder){box.ResetForNewGame();continue;}
                Require(box.ReadyRecipe!=null,"Recipe not ready: "+recipe.Id); box.ResetForNewGame();
                for(int i=0;i<IngredientRules.Count;i++)Require(box.StoredCount((IngredientKind)i)==0,"Ingredients left after summon.");
            }
            return "PASS: 50 live hand refills; basic Nori merge/split rejected; legacy recipe deposits validated. Reload gameplay after this destructive playtest.";
        }
    }
}




