namespace CookingCards
{
    /// <summary>Cooking rules independent of scene objects and UI. Recipe order is priority order.</summary>
    public static class RecipeRules
    {
        private static readonly int[][] Requirements =
        {
            new[] { 2, 2, 2, 0, 0 },
            new[] { 2, 2, 0, 2, 2 },
            new[] { 2, 0, 0, 0, 3 }
        };

        public static bool TryCook(int[] inventory, out DishKind dish)
        {
            if(inventory==null || inventory.Length<IngredientRules.Count)
                throw new System.ArgumentException("Inventory must include all ingredient types.",nameof(inventory));
            if(ActiveLevel.ScriptedTutorial)return ActiveLevel.TryCookTutorial(inventory,out dish);
            if(inventory[(int)IngredientKind.Rice]>=2)
            {
                if(inventory[(int)IngredientKind.Salmon]>=3) { inventory[0]-=2;inventory[(int)IngredientKind.Salmon]-=3;dish=DishKind.SalmonNigiri;return true; }
                if(inventory[(int)IngredientKind.Tuna]>=2) { inventory[0]-=2;inventory[(int)IngredientKind.Tuna]-=2;dish=DishKind.MaguroNigiri;return true; }
                if(inventory[(int)IngredientKind.Shrimp]>=1) { inventory[0]-=2;inventory[(int)IngredientKind.Shrimp]--;dish=DishKind.EbiNigiri;return true; }
            }
            if(PreparationSession.Tutorial) { dish=default;return false; }
            if (inventory == null || inventory.Length < 5)
                throw new System.ArgumentException("Inventory must include existing recipe ingredients.", nameof(inventory));

            for (int recipe = 0; recipe < Requirements.Length; recipe++)
            {
                bool ready = true;
                for (int kind = 0; kind < Requirements[recipe].Length; kind++)
                    if (inventory[kind] < Requirements[recipe][kind]) { ready = false; break; }
                if (!ready) continue;
                for (int kind = 0; kind < Requirements[recipe].Length; kind++)
                    inventory[kind] -= Requirements[recipe][kind];
                dish = (DishKind)recipe;
                return true;
            }
            dish = default;
            return false;
        }
    }
}
