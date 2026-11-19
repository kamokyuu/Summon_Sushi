using UnityEngine;
using UnityEngine.EventSystems;

namespace CookingCards
{
    public enum DishKind { TamagoNigiri, SalmonMaki, SalmonNigiri, EbiNigiri, MaguroNigiri }

    public sealed class CookedDish : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public DishKind Kind { get; private set; }
        public SushiRecipe Recipe { get; private set; }
        public void ConfigureRecipe(SushiRecipe recipe,IngredientStove owner){Recipe=recipe;stove=owner;}
        public int QualityStars { get; private set; } = 1;
        private IngredientStove stove;
        private bool served;
        private Transform home;
        private Vector3 homePosition;
        private CanvasGroup dragGroup;
        public bool TryServeOn(ConveyorPlate plate)
        {
            if(served || Recipe==null || !plate.TryServe(Recipe))return false;
            served=true;
            if(stove!=null)stove.RemoveServedDish(gameObject);else Destroy(gameObject);
            return true;
        }
        public void OnBeginDrag(PointerEventData data)
        {
            if(Recipe==null || served || data.button!=PointerEventData.InputButton.Left || Menu.GameplayPauseController.IsPaused)return;
            home=transform.parent;homePosition=transform.position;
            dragGroup=GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();dragGroup.blocksRaycasts=false;
            transform.SetParent(GetComponentInParent<Canvas>().transform,true);transform.SetAsLastSibling();
        }
        public void OnDrag(PointerEventData data)
        {
            if(home==null || served || Menu.GameplayPauseController.IsPaused)return;
            var parent=(RectTransform)transform.parent;
            if(RectTransformUtility.ScreenPointToWorldPointInRectangle(parent,data.position,data.pressEventCamera,out var world))transform.position=world;
        }
        public void OnEndDrag(PointerEventData data)
        {
            if(home==null || served)return;
            transform.SetParent(home,true);transform.position=homePosition;home=null;
            if(dragGroup!=null)dragGroup.blocksRaycasts=true;
        }
        public void Configure(DishKind kind, IngredientStove owner)
        {
            Kind = kind; stove = owner;
            QualityStars=SushiQualityRules.FromSample(Random.value);
            var go=new GameObject("QualityBadge",typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(transform,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,0);rect.anchoredPosition=new Vector2(0,6);rect.sizeDelta=new Vector2(185,58);
            var bg=go.GetComponent<UnityEngine.UI.Image>();bg.color=new Color(.12f,.09f,.18f,.95f);bg.raycastTarget=false;
            var label=new GameObject("Label",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(go.transform,false);
            var lr=(RectTransform)label.transform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.sizeDelta=Vector2.zero;
            var text=label.GetComponent<TMPro.TextMeshProUGUI>();text.font=TMPro.TMP_Settings.defaultFontAsset;text.text=new string('*',QualityStars)+"  "+QualityStars+"/3\n"+SushiQualityRules.Description(QualityStars);text.fontSize=18;text.color=new Color(1,.84f,.3f);text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;
        }
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || served) return;
            if(Recipe!=null)return; // Conveyor serving is implemented in the next milestone.
            var customers = Object.FindFirstObjectByType<CustomerSystem>();
            if (customers == null || !customers.TryServe(Kind, QualityStars)) return;
            served = true;
            if (stove != null) stove.RemoveServedDish(gameObject);
            else { gameObject.SetActive(false); Destroy(gameObject); }
        }
    }
}
