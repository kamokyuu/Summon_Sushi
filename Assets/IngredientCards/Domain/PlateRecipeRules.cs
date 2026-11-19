namespace CookingCards
{
    public static class PlateRecipeRules
    {
        public static bool TryConsume(PlateDefinition plate,SushiRecipe recipe,int[] inventory)
        {
            if(plate==null || !plate.Accepts(recipe) || !recipe.MatchesExactly(inventory))return false;
            foreach(var item in recipe.Ingredients)inventory[(int)item.Kind]-=item.Count;
            return true;
        }
    }
}
