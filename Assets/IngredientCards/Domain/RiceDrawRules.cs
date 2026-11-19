using System.Collections.Generic;
using UnityEngine;
namespace CookingCards
{
    public static class RiceDrawRules
    {
        /// <summary>Reorders existing stock; never creates cards. Each full bag has 2 Rice and 5 other cards while both stocks permit.</summary>
        public static List<IngredientKind> Arrange(List<IngredientKind> stock,IngredientKind[] prefix)
        {
            var rice=stock.FindAll(k=>k==IngredientKind.Rice);
            var other=stock.FindAll(k=>k!=IngredientKind.Rice);
            Shuffle(other);
            var sequence=new List<IngredientKind>();
            foreach(var kind in prefix)
            {
                var pool=kind==IngredientKind.Rice?rice:other;
                if(pool.Remove(kind))sequence.Add(kind);
            }
            while(rice.Count+other.Count>0)
            {
                int offset=sequence.Count%7;
                int usedRice=0;
                for(int i=sequence.Count-offset;i<sequence.Count;i++)if(sequence[i]==IngredientKind.Rice)usedRice++;
                int slots=7-offset;
                var bag=new List<IngredientKind>();
                int wantedRice=Mathf.Min(2-usedRice,rice.Count);
                int wantedOther=Mathf.Min(slots-wantedRice,other.Count);
                for(int i=0;i<wantedRice;i++){bag.Add(IngredientKind.Rice);rice.RemoveAt(rice.Count-1);}
                for(int i=0;i<wantedOther;i++){bag.Add(other[other.Count-1]);other.RemoveAt(other.Count-1);}
                // If one stock is exhausted, finish with remaining real cards rather than inventing resources.
                if(bag.Count==0){bag.AddRange(rice);bag.AddRange(other);rice.Clear();other.Clear();}
                Shuffle(bag);sequence.AddRange(bag);
            }
            sequence.Reverse();return sequence;
        }
        private static void Shuffle(List<IngredientKind> cards)
        {for(int i=cards.Count-1;i>0;i--){int j=Random.Range(0,i+1);var k=cards[i];cards[i]=cards[j];cards[j]=k;}}
    }
}
