using System;
using System.Collections.Generic;
namespace CookingCards
{
    public static class VerticalSliceRecipes
    {
        public static readonly IReadOnlyList<SushiRecipe> All=Build();
        private static IReadOnlyList<SushiRecipe> Build()
        {
            // Jam compositions are independent of the full-game catalog.
            var result=new List<SushiRecipe>
            {
                new SushiRecipe("ebi_nigiri","Ebi Nigiri",RecipeGrade.Common,RecipeFamily.Nigiri,
                    new IngredientAmount(IngredientKind.Shrimp,1),new IngredientAmount(IngredientKind.Rice,2)),
                new SushiRecipe("avocado_maki","Avocado Maki",RecipeGrade.Uncommon,RecipeFamily.Maki,
                    new IngredientAmount(IngredientKind.Avocado,1),new IngredientAmount(IngredientKind.Nori,1),new IngredientAmount(IngredientKind.Rice,1)),
                new SushiRecipe("tuna_avocado_maki","Tuna Avocado Maki",RecipeGrade.Rare,RecipeFamily.Maki,
                    new IngredientAmount(IngredientKind.Tuna,1),new IngredientAmount(IngredientKind.Avocado,1),new IngredientAmount(IngredientKind.Nori,1),new IngredientAmount(IngredientKind.Rice,1)),
                new SushiRecipe("salmon_nigiri","Salmon Nigiri",RecipeGrade.Special,RecipeFamily.Nigiri,
                    new IngredientAmount(IngredientKind.Salmon,3),new IngredientAmount(IngredientKind.Rice,1)),
                new SushiRecipe("salmon_sashimi","Salmon Sashimi",RecipeGrade.Special,RecipeFamily.SideDish,
                    new IngredientAmount(IngredientKind.Salmon,2))
            };
            return result.AsReadOnly();
        }
        public static IReadOnlyList<SushiRecipe> Conveyor(RecipeGrade grade)
        {var result=new List<SushiRecipe>();foreach(var r in All)if(r.Service==RecipeService.Conveyor && r.Grade==grade)result.Add(r);return result.AsReadOnly();}
        public static bool Contains(string id)
        { foreach(var recipe in All)if(recipe.Id==id)return true;return false; }
        public static SushiRecipe Match(IReadOnlyList<int> inventory)
        { foreach(var recipe in All)if(recipe.MatchesExactly(inventory))return recipe;return null; }
    }
}
