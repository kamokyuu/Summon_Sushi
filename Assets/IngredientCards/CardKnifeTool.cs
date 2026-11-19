using UnityEngine;
using UnityEngine.EventSystems;
namespace CookingCards
{
    /// <summary>Owns tool charges; the hand owns card replacement and ingredient rules own split mapping.</summary>
    public sealed class CardKnifeTool : MonoBehaviour
    {
        [SerializeField] private KnifeDragHandle knife;
        private void Start()
        {
            var tool=EnsureTool();
            tool.gameObject.SetActive(!PreparationSession.Tutorial || PreparationSession.KnifeTutorial || PreparationSession.BossTutorial);
        }

        public KnifeDragHandle EnsureTool()
        {
            var canvas=GetComponentInParent<Canvas>();
            if(knife==null)knife=canvas.GetComponentInChildren<KnifeDragHandle>(true);
            if(knife!=null)
            {
                knife.Configure(GetComponent<IngredientCardHand>(),knife.GetComponentInChildren<TMPro.TMP_Text>(true));
                return knife;
            }
            var go=new GameObject("KnifeTool",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(CanvasGroup),typeof(KnifeDragHandle));
            go.transform.SetParent(canvas.transform,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(1,.5f);r.pivot=new Vector2(1,.5f);r.anchoredPosition=new Vector2(-25,-70);r.sizeDelta=new Vector2(140,85);
            go.GetComponent<UnityEngine.UI.Image>().color=new Color(.85f,.55f,.25f);
            var label=new GameObject("Label",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(go.transform,false);
            ((RectTransform)label.transform).sizeDelta=new Vector2(130,80);
            var text=label.GetComponent<TMPro.TextMeshProUGUI>();text.font=TMPro.TMP_Settings.defaultFontAsset;text.fontSize=22;text.color=Color.black;text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;
            knife=go.GetComponent<KnifeDragHandle>();
            knife.Configure(GetComponent<IngredientCardHand>(),text);
            return knife;
        }
    }
}
