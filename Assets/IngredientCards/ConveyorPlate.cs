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
            GuidedLevelTutorial.Notify(LevelGuideStep.Summon,recipe);
            GuidedLevelTutorial.Notify(LevelGuideStep.PlateMatch,recipe);
            var visual=System.Array.Find(ActiveLevel.Config.foodVisuals??System.Array.Empty<RecipeFoodVisual>(),v=>v.recipeId==recipe.Id);
            var sprite=visual?.sprite;
            if(sprite!=null)
            {
                var food=new GameObject("ServedFood",typeof(RectTransform),typeof(UnityEngine.UI.Image));food.transform.SetParent(transform,false);
                var foodRect=(RectTransform)food.transform;float inset=(1-owner.FoodScale)*.5f;
                foodRect.anchorMin=new Vector2(inset,inset);foodRect.anchorMax=new Vector2(1-inset,1-inset);foodRect.sizeDelta=Vector2.zero;
                var foodImage=food.GetComponent<UnityEngine.UI.Image>();foodImage.sprite=sprite;foodImage.preserveAspect=true;foodImage.raycastTarget=false;
                return true;
            }
            var background=new GameObject("RecipePlaceholder",typeof(RectTransform),typeof(UnityEngine.UI.Image));background.transform.SetParent(transform,false);
            var br=(RectTransform)background.transform;br.sizeDelta=new Vector2(125,65);background.GetComponent<UnityEngine.UI.Image>().color=new Color(.18f,.23f,.25f);background.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var go=new GameObject("ServedRecipe",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));go.transform.SetParent(background.transform,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(6,6);rect.offsetMax=new Vector2(-6,-6);
            var text=go.GetComponent<TMPro.TextMeshProUGUI>();text.font=TMPro.TMP_Settings.defaultFontAsset;text.text=recipe.DisplayName;text.enableAutoSizing=true;text.fontSizeMin=10;text.fontSizeMax=17;text.color=Color.white;text.outlineWidth=.3f;text.outlineColor=Color.black;text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;
            return true;
        }
        public void BreakIfEmpty()
        {if(GuidedLevelTutorial.Protecting || CustomerSystem.GameFinished || IsBroken || ServedRecipe!=null)return;IsBroken=true;image.sprite=brokenSprite;owner.RecordMissed(Grade);}
        public void OnDrop(PointerEventData data)
        {if(data.pointerDrag!=null)data.pointerDrag.GetComponent<CookedDish>()?.TryServeOn(this);}
    }
}
