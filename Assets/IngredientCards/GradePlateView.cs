using UnityEngine;
namespace CookingCards
{
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public sealed class GradePlateView:MonoBehaviour
    {
        [SerializeField] private PlateDefinition definition;
        [SerializeField] private TMPro.TMP_Text label;
        public PlateDefinition Definition=>definition;
        public string RecipeId { get; private set; }
        public void Configure(PlateDefinition data,TMPro.TMP_Text text){definition=data;label=text;Refresh();}
        private void Awake()=>Refresh();
        private void OnValidate()=>Refresh();
        private void Refresh(){if(definition==null)return;GetComponent<UnityEngine.UI.Image>().color=definition.Color;if(label!=null){label.text=definition.Grade+"\nEMPTY";label.color=definition.Grade==RecipeGrade.Common?Color.black:Color.white;}}
        public bool TryServe(SushiRecipe recipe)
        {
            if(!string.IsNullOrEmpty(RecipeId) || definition==null || !definition.Accepts(recipe))return false;
            RecipeId=recipe.Id;if(label!=null)label.text=definition.Grade+"\n"+recipe.DisplayName;return true;
        }
        public void Clear(){RecipeId=null;Refresh();}
    }
}
