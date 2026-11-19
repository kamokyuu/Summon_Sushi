namespace CookingCards
{
    public static class TutorialOrders
    {
        public static DishKind At(int index)
        {
            switch(index)
            {
                case 0: return DishKind.MaguroNigiri;
                case 2: return DishKind.SalmonNigiri;
                default: return DishKind.EbiNigiri;
            }
        }
    }
}
