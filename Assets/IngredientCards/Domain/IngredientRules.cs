namespace CookingCards
{
    public enum IngredientCategory { Staple, Garden, Roe, Seafood, Fungi, Grill }
    public static class IngredientRules
    {
        public const int Count = 16;
        public static IngredientCategory Category(IngredientKind kind)
        {
            switch(kind)
            {
                case IngredientKind.Rice: return IngredientCategory.Staple;
                case IngredientKind.Nori: case IngredientKind.Cucumber: case IngredientKind.Avocado: return IngredientCategory.Garden;
                case IngredientKind.Eggs: case IngredientKind.Tobiko: case IngredientKind.Ikura: return IngredientCategory.Roe;
                case IngredientKind.Shrimp: case IngredientKind.Tuna: case IngredientKind.Salmon: return IngredientCategory.Seafood;
                case IngredientKind.Chicken: case IngredientKind.Unagi: case IngredientKind.Beef: return IngredientCategory.Grill;
                default: return IngredientCategory.Fungi;
            }
        }
        public static string DisplayName(IngredientKind kind) => kind == IngredientKind.Eggs ? "Chicken Egg" : kind.ToString();
        public static bool TrySplit(IngredientKind kind,out IngredientKind result)
        {
            for(int i=0;i<Count;i++)
                if(TryMerge((IngredientKind)i,(IngredientKind)i,out var next) && next==kind) { result=(IngredientKind)i;return true; }
            result=kind;return false;
        }
        public static bool TryMerge(IngredientKind first, IngredientKind second, out IngredientKind result)
        {
            result=first;
            if(first!=second) return false;
            switch(first)
            {
                case IngredientKind.Eggs: result=IngredientKind.Tobiko; return true;
                case IngredientKind.Tobiko: result=IngredientKind.Ikura; return true;
                case IngredientKind.Shrimp: result=IngredientKind.Tuna; return true;
                case IngredientKind.Tuna: result=IngredientKind.Salmon; return true;
                case IngredientKind.Mushroom: result=IngredientKind.Shiitake; return true;
                case IngredientKind.Shiitake: result=IngredientKind.Matsutake; return true;
                default: return false;
            }
        }
    }
}
