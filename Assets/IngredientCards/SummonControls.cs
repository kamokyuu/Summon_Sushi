using UnityEngine;
namespace CookingCards
{
    /// <summary>Shared stock controls. Recipes are automatic; the journal is a reference only.</summary>
    public sealed class SummonControls:MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button returnButton;
        public void Configure(IngredientStove box)
        {
            var button=EnsureButton();
            button.gameObject.SetActive(!PreparationSession.Tutorial);
            button.onClick.RemoveListener(box.ReturnIngredients);
            button.onClick.AddListener(box.ReturnIngredients);
        }
        public UnityEngine.UI.Button EnsureButton()
        {
            var canvas=GetComponentInParent<Canvas>();
            if(returnButton==null)
                foreach(var existing in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                    if(existing.name=="ReturnIngredientsButton"){returnButton=existing;break;}
            if(returnButton!=null)return returnButton;
            var go=new GameObject("ReturnIngredientsButton",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));
            go.transform.SetParent(canvas.transform,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(1,.5f);rect.anchoredPosition=new Vector2(-120,-280);rect.sizeDelta=new Vector2(220,55);
            var image=go.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.16f,.22f,.3f);
            var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var label=new GameObject("Label",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(go.transform,false);
            var lr=(RectTransform)label.transform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.sizeDelta=Vector2.zero;
            var text=label.GetComponent<TMPro.TextMeshProUGUI>();text.font=TMPro.TMP_Settings.defaultFontAsset;text.text="RETURN TO HAND";text.fontSize=18;text.color=Color.white;text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;
            returnButton=button;
            return button;
        }
    }
}
