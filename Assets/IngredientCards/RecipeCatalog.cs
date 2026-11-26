using System;
using UnityEngine;

namespace CookingCards
{
    [Serializable]
    public sealed class RecipeFoodSettings
    {
        public string recipeId;
        [Tooltip("0 uses the Conveyor Controller's global Food Scale.")]
        [Min(0)] public float foodScale;
        public float offsetX, offsetY, rotationZ;
    }

    [CreateAssetMenu(menuName="Summon Sushi/Recipe Visual Catalog")]
    public sealed class RecipeCatalog : ScriptableObject
    {
        public RecipeFoodSettings[] foodVisualSettings=Array.Empty<RecipeFoodSettings>();
        private static RecipeCatalog active;
        public static RecipeFoodSettings FindVisual(string recipeId)
        {
            if(active==null)active=Resources.Load<RecipeCatalog>("RecipeCatalog");
            return active!=null&&active.foodVisualSettings!=null
                ? Array.Find(active.foodVisualSettings,v=>v!=null&&v.recipeId==recipeId):null;
        }
    }
}
