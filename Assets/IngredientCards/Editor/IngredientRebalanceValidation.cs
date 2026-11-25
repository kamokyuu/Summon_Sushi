using System;
using UnityEditor;
using UnityEngine;

namespace CookingCards.Editor
{
    public static class IngredientRebalanceValidation
    {
        [MenuItem("Tools/Cooking Cards/Validate P0.2 Ingredient Balance")]
        public static void Run()
        {
            LevelConfigSetup.Validate();
            var previous=ActiveLevel.Config;
            var random=UnityEngine.Random.state;
            LevelConfig copy=null;
            try
            {
                foreach(var basic in new[]{IngredientKind.Cucumber})
                {
                    Require(!IngredientRules.TrySplit(basic,out _),basic+" must not split.");
                    for(int i=0;i<IngredientRules.Count;i++)Require(!IngredientRules.TryMerge(basic,(IngredientKind)i,out _),basic+" must not merge.");
                }
                foreach(var pair in new[]{new[]{IngredientKind.Rice,IngredientKind.Nori},new[]{IngredientKind.Nori,IngredientKind.Avocado},new[]{IngredientKind.Shrimp,IngredientKind.Tuna},new[]{IngredientKind.Tuna,IngredientKind.Salmon},new[]{IngredientKind.Eggs,IngredientKind.Tobiko},new[]{IngredientKind.Tobiko,IngredientKind.Ikura}})
                {
                    Require(IngredientRules.TryMerge(pair[0],pair[0],out var higher)&&higher==pair[1],"Merge chain broken.");
                    Require(IngredientRules.TrySplit(pair[1],out var lower)&&lower==pair[0],"Knife chain broken.");
                }
                Require((int)IngredientKind.Rice==0&&(int)IngredientKind.Nori==1&&(int)IngredientKind.Eggs==2&&(int)IngredientKind.Avocado==3&&(int)IngredientKind.Salmon==4,"Serialized IDs shifted.");
                var level=Resources.Load<LevelConfig>("Levels/Level01");ActiveLevel.Select(level);
                Require(!level.mergeEnabled&&!level.knifeEnabled&&level.activeIngredients.Length==3,"Wrong Level01 features/ingredients.");
                var counts=new int[IngredientRules.Count];
                for(int i=0;i<10000;i++)counts[(int)level.SampleIngredient((i+.5f)/10000f)]++;
                Require(counts[(int)IngredientKind.Rice]==4500&&counts[(int)IngredientKind.Shrimp]==3000&&counts[(int)IngredientKind.Eggs]==2500,"Expected 45/30/25 distribution.");
                foreach(var recipe in level.Recipes)
                {
                    Require(recipe.Grade==RecipeGrade.Common&&recipe.TotalCards==2&&recipe.Required(IngredientKind.Rice)==1,"Beginner recipe not rebalanced.");
                    var stock=new int[IngredientRules.Count];foreach(var item in recipe.Ingredients)stock[(int)item.Kind]=item.Count;
                    Require(ActiveRecipeRules.TryCook(stock,out var cooked)&&cooked.Id==recipe.Id,"Beginner summon failed.");
                }
                copy=UnityEngine.Object.Instantiate(level);
                copy.drawMode=IngredientDrawMode.WeightedActiveIngredients;
                foreach(var invalid in new[]{-1f,float.NaN,float.PositiveInfinity})
                {
                    copy.ingredientWeights=new[]{new IngredientDrawWeight{ingredient=IngredientKind.Rice,weight=invalid}};
                    ExpectInvalid(copy);
                }
                copy.ingredientWeights=new[]{new IngredientDrawWeight{ingredient=IngredientKind.Nori,weight=1}};ExpectInvalid(copy);
                copy.ingredientWeights=new[]{new IngredientDrawWeight{ingredient=IngredientKind.Rice,weight=0}};ExpectInvalid(copy);
                copy.activeIngredients=new[]{IngredientKind.Cucumber,IngredientKind.Avocado};
                copy.activeRecipes=new[]{LevelRecipe.From(new SushiRecipe("cucumber_test","Cucumber",RecipeGrade.Common,RecipeFamily.Nigiri,new IngredientAmount(IngredientKind.Cucumber,1))),LevelRecipe.From(new SushiRecipe("avocado_test","Avocado",RecipeGrade.Common,RecipeFamily.Nigiri,new IngredientAmount(IngredientKind.Avocado,1)))};
                copy.ingredientWeights=new[]{new IngredientDrawWeight{ingredient=IngredientKind.Cucumber,weight=1},new IngredientDrawWeight{ingredient=IngredientKind.Avocado,weight=1}};
                copy.InvalidateRecipes();copy.ValidateConfiguration();
                Require(copy.SampleIngredient(.1f)==IngredientKind.Cucumber&&copy.SampleIngredient(.9f)==IngredientKind.Avocado,"Garden basics not directly drawable.");
                Debug.Log("P0.2 passed: unchanged IDs; basic merge/split exclusions; seafood/egg chains; exact 45/30/25 weights; Common beginner summons; invalid weight rejection; direct Cucumber/Avocado draws; legacy regression.");
            }
            finally{ActiveLevel.Select(previous);UnityEngine.Random.state=random;if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);}
        }
        private static void ExpectInvalid(LevelConfig config)
        {bool invalid=false;try{config.ValidateConfiguration();}catch(InvalidOperationException){invalid=true;}Require(invalid,"Invalid weights accepted.");}
        private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    }
}
