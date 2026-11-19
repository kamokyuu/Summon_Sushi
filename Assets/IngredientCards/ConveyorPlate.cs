using UnityEngine;
using UnityEngine.EventSystems;
namespace CookingCards
{
    public sealed class ConveyorPlate : MonoBehaviour, IDropHandler
    {
        public RecipeGrade Grade { get; private set; }
        public bool IsBroken { get; private set; }
        public SushiRecipe ServedRecipe { get; private set; }
        private ConveyorController owner;
        private UnityEngine.UI.Image image;
        private Sprite brokenSprite;
        public void Configure(ConveyorController belt,RecipeGrade grade,Sprite normal,Sprite broken)
        {owner=belt;Grade=grade;brokenSprite=broken;image=GetComponent<UnityEngine.UI.Image>();image.sprite=normal;image.preserveAspect=true;}
        public bool TryServe(SushiRecipe recipe)
        {
            if(CustomerSystem.GameFinished || Menu.GameplayPauseController.IsPaused || IsBroken || ServedRecipe!=null || recipe==null || recipe.Service!=RecipeService.Conveyor || recipe.Grade!=Grade)return false;
            ServedRecipe=recipe;owner.RecordServed(Grade);
            var sprite=owner.FoodSprite(Grade);
            if(sprite!=null)
            {
                var food=new GameObject("ServedFood",typeof(RectTransform),typeof(UnityEngine.UI.Image));food.transform.SetParent(transform,false);
                var foodRect=(RectTransform)food.transform;float inset=(1-owner.FoodScale)*.5f;
                foodRect.anchorMin=new Vector2(inset,inset);foodRect.anchorMax=new Vector2(1-inset,1-inset);foodRect.sizeDelta=Vector2.zero;
                var foodImage=food.GetComponent<UnityEngine.UI.Image>();foodImage.sprite=sprite;foodImage.preserveAspect=true;foodImage.raycastTarget=false;
                return true;
            }
            var go=new GameObject("ServedRecipe",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));go.transform.SetParent(transform,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(12,20);rect.offsetMax=new Vector2(-12,-20);
            var text=go.GetComponent<TMPro.TextMeshProUGUI>();text.font=TMPro.TMP_Settings.defaultFontAsset;text.text=recipe.DisplayName;text.fontSize=17;text.color=Color.white;text.outlineWidth=.3f;text.outlineColor=Color.black;text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;
            return true;
        }
        public void BreakIfEmpty()
        {if(CustomerSystem.GameFinished || IsBroken || ServedRecipe!=null)return;IsBroken=true;image.sprite=brokenSprite;owner.RecordMissed(Grade);}
        public void OnDrop(PointerEventData data)
        {if(data.pointerDrag!=null)data.pointerDrag.GetComponent<CookedDish>()?.TryServeOn(this);}
    }
}
