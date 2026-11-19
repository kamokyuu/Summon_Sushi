using UnityEngine;
namespace CookingCards
{
    public sealed class DayShiftController:MonoBehaviour
    {
        public static DayShiftController Active {get;private set;}
        [SerializeField,Min(1)] private float durationSeconds=180;
        [SerializeField,Min(0)] private int targetYen=3000;
        private ConveyorController belt;
        private IngredientCardHand hand;
        private GameObject resultPanel;
        private TMPro.TMP_Text resultText;
        [SerializeField] private TMPro.TMP_Text hud;
        public void ConfigureDisplay(TMPro.TMP_Text display){hud=display;}
        public float Remaining {get;private set;}
        public bool HasEnded {get;private set;}
        public bool Won=>belt!=null && belt.Revenue-belt.Penalties>=targetYen;
        public int TargetYen=>targetYen;
        private void Awake(){Active=this;Remaining=durationSeconds;}
        private void OnDestroy(){if(Active==this)Active=null;}
        public void Configure(GameObject panel,TMPro.TMP_Text text)
        {
            resultPanel=panel;resultText=text;belt=Object.FindFirstObjectByType<ConveyorController>();hand=Object.FindFirstObjectByType<IngredientCardHand>();
            if(hud!=null){hud.transform.parent.gameObject.SetActive(true);Refresh();return;}
            var canvas=GetComponentInParent<Canvas>();
            var go=new GameObject("DayShiftHUD",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));go.transform.SetParent(canvas.transform,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-15);r.sizeDelta=new Vector2(600,85);
            hud=go.GetComponent<TMPro.TextMeshProUGUI>();hud.font=TMPro.TMP_Settings.defaultFontAsset;hud.fontSize=24;hud.color=Color.white;hud.outlineWidth=.2f;hud.outlineColor=Color.black;hud.alignment=TMPro.TextAlignmentOptions.Center;hud.raycastTarget=false;
            var theme=Resources.Load<GameUiTheme>("GameUiTheme");
            if(theme!=null)
            {
                var hudPanel=new GameObject("DayShiftPanel",typeof(RectTransform),typeof(UnityEngine.UI.Image));hudPanel.transform.SetParent(canvas.transform,false);
                var pr=(RectTransform)hudPanel.transform;pr.anchorMin=pr.anchorMax=pr.pivot=new Vector2(.5f,1);pr.anchoredPosition=new Vector2(0,-15);pr.sizeDelta=new Vector2(420,80);
                var image=hudPanel.GetComponent<UnityEngine.UI.Image>();image.sprite=theme.buttonSprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.raycastTarget=false;
                go.transform.SetParent(hudPanel.transform,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(8,4);r.offsetMax=new Vector2(-8,-4);
                hud.color=GameUiTheme.Ink;hud.outlineWidth=0;
            }
            Refresh();
        }
        private void Update(){Advance(Time.deltaTime);}
        public void Advance(float seconds)
        {
            if(HasEnded || Menu.GameplayPauseController.IsPaused || hand==null || hand.IsDealing || belt==null)return;
            Remaining=Mathf.Max(0,Remaining-Mathf.Max(0,seconds));Refresh();if(Remaining<=0)Finish();
        }
        private void Refresh()
        {
            if(hud!=null && belt!=null)hud.text="DAY 1   "+(Mathf.CeilToInt(Remaining)/60).ToString("00")+":"+(Mathf.CeilToInt(Remaining)%60).ToString("00")+"\nNET ¥"+(belt.Revenue-belt.Penalties)+" / ¥"+targetYen;
        }
        private void Finish()
        {
            HasEnded=true;
            if(resultText!=null)resultText.text="<size=36><b>DAY 1 "+(Won?"COMPLETE":"FAILED")+"</b></size>\n\nSales: ¥"+belt.Revenue+"\nBroken plate penalties: -¥"+belt.Penalties+"\nNet: ¥"+(belt.Revenue-belt.Penalties)+" / ¥"+targetYen+"\n\nSushi sold: "+belt.PurchasedCount+"\nBroken plates: "+belt.MissedCount;
            if(resultPanel!=null){resultPanel.SetActive(true);resultPanel.transform.SetAsLastSibling();var nav=resultPanel.GetComponent<Menu.ResultsNavigation>()??resultPanel.AddComponent<Menu.ResultsNavigation>();nav.Show();}
        }
    }
}

