using System;
using System.Collections.Generic;
using UnityEngine;
namespace CookingCards
{
    public static class RecipeDemandBag
    {
        public static List<IngredientKind> Build(LevelConfig level)
        {
            var weights=new Dictionary<IngredientKind,float>();
            foreach(var entry in level.activeRecipes)foreach(var amount in entry.ingredients)
            {
                if(!level.Allows(amount.ingredient))throw new InvalidOperationException("Inactive recipe ingredient.");
                if(!weights.ContainsKey(amount.ingredient))weights[amount.ingredient]=0;
                weights[amount.ingredient]+=amount.count*entry.demandWeight;
            }
            if(weights.Count==0||level.shuffleBagSize<weights.Count)throw new InvalidOperationException("Bag must fit every demanded ingredient.");
            foreach(var kind in new List<IngredientKind>(weights.Keys))
            {
                float bias=kind==IngredientKind.Rice?level.riceDemandMultiplier:kind==IngredientKind.Nori?level.noriDemandMultiplier:kind==IngredientKind.Avocado||kind==IngredientKind.Tuna||kind==IngredientKind.Salmon?level.premiumDemandMultiplier:1;
                if(bias<=0||float.IsNaN(bias)||float.IsInfinity(bias))throw new InvalidOperationException("Invalid demand multiplier.");
                weights[kind]*=bias;
            }
            float total=0;foreach(var w in weights.Values)total+=w;
            if(total<=0)throw new InvalidOperationException("Recipes need positive demand weights.");
            var counts=new Dictionary<IngredientKind,int>();foreach(var kind in weights.Keys)counts[kind]=1;
            for(int filled=counts.Count;filled<level.shuffleBagSize;filled++)
            {
                IngredientKind best=default;float deficit=float.NegativeInfinity;
                foreach(var pair in weights){float d=pair.Value/total*level.shuffleBagSize-counts[pair.Key];if(d>deficit){deficit=d;best=pair.Key;}}
                counts[best]++;
            }
            var result=new List<IngredientKind>();foreach(var pair in counts)for(int i=0;i<pair.Value;i++)result.Add(pair.Key);
            for(int i=result.Count-1;i>0;i--){int j=UnityEngine.Random.Range(0,i+1);var value=result[i];result[i]=result[j];result[j]=value;}
            return result;
        }
    }
}
