namespace CookingCards
{
    public static class GluttonRules
    {
        public const int Target=5;
        public const int NormalCustomers=6;
        public const int RiceRequired=2*(Target+NormalCustomers);
        public static DishKind Order(float sample) => DishKind.SalmonNigiri;
    }
}
