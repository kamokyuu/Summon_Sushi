namespace CookingCards
{
    public static class TutorialTotals
    {
        private static readonly int[] dishes=new int[5];
        private static readonly int[] quality=new int[3];
        public static int Revenue { get; private set; }
        public static int Served { get; private set; }
        public static void Reset(){System.Array.Clear(dishes,0,dishes.Length);System.Array.Clear(quality,0,quality.Length);Revenue=0;Served=0;}
        public static void Record(DishKind dish,int stars,int yen){dishes[(int)dish]++;quality[System.Math.Max(0,System.Math.Min(2,stars-1))]++;Revenue+=yen;Served++;}
        public static string Summary()
        {
            var text=new System.Text.StringBuilder("\nFULL TUTORIAL RECAP / ¥"+Revenue+"\n");
            for(int i=0;i<dishes.Length;i++)if(dishes[i]>0)text.Append((DishKind)i).Append(" × ").Append(dishes[i]).AppendLine();
            text.Append("Stars 1/2/3: ").Append(quality[0]).Append(" / ").Append(quality[1]).Append(" / ").Append(quality[2]);return text.ToString();
        }
    }
}
