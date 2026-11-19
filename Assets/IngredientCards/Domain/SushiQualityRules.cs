namespace CookingCards
{
    public static class SushiQualityRules
    {
        /// <summary>Accepts a normalized random sample; equal odds for the prototype.</summary>
        public static int FromSample(float sample) => sample < 1f/3f ? 1 : sample < 2f/3f ? 2 : 3;
        public static string Description(int stars) => stars==1?"Biasa":stars==2?"Enak":"Enak banget!";
        public static int SalePrice(DishKind dish,int stars)
        {
            if(dish==DishKind.SalmonNigiri)return 1200+300*(System.Math.Max(1,System.Math.Min(3,stars))-1);
            int price=dish==DishKind.SalmonNigiri?1200:dish==DishKind.MaguroNigiri?400:dish==DishKind.SalmonMaki?500:200;
            int clamped=System.Math.Max(1,System.Math.Min(3,stars));
            return price*(clamped+1)/2;
        }
    }
}
