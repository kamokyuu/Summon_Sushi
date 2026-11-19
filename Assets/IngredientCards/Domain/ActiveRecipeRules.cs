namespace CookingCards
{
    public static class ActiveRecipeRules
    {
        public static bool CanAdd(int[] inventory, IngredientKind kind)
        {
            if(inventory==null || inventory.Length!=IngredientRules.Count || !GameJamRules.Allows(kind))return false;
            foreach(var recipe in VerticalSliceRecipes.All)
            {
                bool possible=true;
                for(int i=0;i<inventory.Length;i++)
                    if(inventory[i]+(i==(int)kind?1:0)>recipe.Required((IngredientKind)i)){possible=false;break;}
                if(possible)return true;
            }
            return false;
        }
        public static bool TryCook(int[] inventory,out SushiRecipe recipe)
        {
            recipe=VerticalSliceRecipes.Match(inventory);
            if(recipe==null)return false;
            foreach(var item in recipe.Ingredients)inventory[(int)item.Kind]-=item.Count;
            return true;
        }
    }
}
