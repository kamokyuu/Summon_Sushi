using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace CookingCards
{
    public sealed class KnifeDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private IngredientCardHand hand;
        [SerializeField] private TMPro.TMP_Text label;
        private int uses=3;
        public int Charges=>uses;
        public int MaximumCharges=>ActiveLevel.Legacy?ActiveLevel.Config.initialKnifeCharges:2;
        private void Awake(){uses=Mathf.Clamp(ActiveLevel.Config.initialKnifeCharges,0,MaximumCharges);}
        public bool TryUse(IngredientCard card)
        {
            if(!ActiveLevel.KnifeEnabled||uses<=0||hand==null||!hand.TrySplitCard(card))return false;
            uses--;Refresh();GuidedLevelTutorial.Notify(LevelGuideStep.Knife);return true;
        }
        private Vector2 home;
        private bool dragging;
        [SerializeField] private bool tintWhenEmpty;
        [SerializeField] private Color emptyTint=Color.gray;
        public void Configure(IngredientCardHand owner,TMPro.TMP_Text text) { hand=owner;label=text;Refresh(); }
        private void Refresh() { if(label!=null)label.text="KNIFE\n"+uses+" / "+MaximumCharges;if(tintWhenEmpty && uses<=0)GetComponent<UnityEngine.UI.Image>().color=emptyTint; }
        public void OnBeginDrag(PointerEventData e)
        {
            if(!ActiveLevel.KnifeEnabled || e.button!=PointerEventData.InputButton.Left || uses<=0 || Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished || hand.IsDealing)return;
            home=((RectTransform)transform).anchoredPosition;dragging=true;GetComponent<CanvasGroup>().blocksRaycasts=false;transform.SetAsLastSibling();
        }
        public void OnDrag(PointerEventData e)
        {
            if(!dragging)return;
            if(RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)transform.parent,e.position,e.pressEventCamera,out var world))transform.position=world;
        }
        public void OnEndDrag(PointerEventData e)
        {
            if(!dragging)return;
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
            foreach(var hit in hits)
            {
                var card=hit.gameObject.GetComponentInParent<IngredientCard>();
                if(card!=null) { TryUse(card);break; }
            }
            dragging=false;((RectTransform)transform).anchoredPosition=home;GetComponent<CanvasGroup>().blocksRaycasts=true;Refresh();
        }
        private void Update()
        {
            if(dragging && Menu.GameplayPauseController.IsPaused) { dragging=false;((RectTransform)transform).anchoredPosition=home;GetComponent<CanvasGroup>().blocksRaycasts=true; }
        }
    }
}
