using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace CookingCards
{
    public sealed class GameplayHudFeedback:MonoBehaviour
    {
        public static GameplayHudFeedback Active {get;private set;}
        public static readonly Color Ink=new Color32(0x4B,0x2F,0x22,255);
        [SerializeField] private TMP_Text revenueText;
        [SerializeField] private RectTransform revenuePanel;
        [SerializeField] private RectTransform coinTarget;
        [SerializeField] private UnityEngine.UI.Image bookImage;
        [SerializeField] private TMP_Text ingredientNotes;
        [SerializeField] private RectTransform inventoryPanel,returnButton;
        [SerializeField] public Sprite panelSprite,coinSprite,recipeBookSprite,preferenceBubbleSprite,pickyIcon,hungryIcon;
        [SerializeField] private AudioClip coinSound;
        [Header("Sushi serving feedback")]
        [SerializeField, Min(0)] private float servingArcHeight=100;
        [SerializeField, Range(.5f,.7f)] private float servingDuration=.6f;
        [SerializeField, Range(0,.25f)] private float landingBounce=.08f;
        public float DisplayedRevenue {get;private set;}
        public int InFlight=>flights.Count;
        private sealed class Flight {public RectTransform rect;public Vector2 start;public float age;public int amount;}
        private readonly List<Flight> flights=new List<Flight>();
        private ConveyorController belt;
        private RectTransform canvas;
        private int pending;
        private float bounce=1;
        private AudioSource sound;
        private Vector3 homeScale=Vector3.one;
        private void Awake(){Active=this;canvas=(RectTransform)GetComponent<Canvas>().transform;belt=Object.FindFirstObjectByType<ConveyorController>();if(revenuePanel!=null)homeScale=revenuePanel.localScale;if(coinSound!=null){sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;}}
        private void OnDestroy(){if(Active==this)Active=null;}
        private int Actual=>belt!=null?belt.Revenue-belt.Penalties:0;
        public void Configure(TMP_Text text,RectTransform panel,RectTransform coin){revenueText=text;revenuePanel=panel;coinTarget=coin;}
        public void ConfigureBook(UnityEngine.UI.Image image){bookImage=image;}
        public void ConfigureNotes(TMP_Text text,RectTransform panel,RectTransform button){ingredientNotes=text;inventoryPanel=panel;returnButton=button;FitNotes();}
        private void FitNotes()
        {
            if(ingredientNotes==null||inventoryPanel==null)return;
            int rows=0;foreach(char c in ingredientNotes.text)if(c=='\n')rows++;
            float height=Mathf.Clamp(52+rows*20,52,192);inventoryPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
            if(returnButton!=null)returnButton.anchoredPosition=new Vector2(returnButton.anchoredPosition.x,((RectTransform)transform).rect.yMax-120-height-34);
        }
        private void Start()
        {
            DisplayedRevenue=Actual;
            if(panelSprite!=null)foreach(var image in GetComponentsInChildren<UnityEngine.UI.Image>(true))if(image.name=="DayShiftPanel"||image.name=="IngredientCounts"){image.sprite=panelSprite;image.type=UnityEngine.UI.Image.Type.Sliced;}
            if(ActiveLevel.ScriptedTutorial){foreach(var t in GetComponentsInChildren<Transform>(true))if(t.name=="CoinCounter")t.gameObject.SetActive(true);if(revenuePanel!=null)revenuePanel.gameObject.SetActive(false);}
            if(coinTarget!=null){var image=coinTarget.GetComponent<UnityEngine.UI.Image>();if(image!=null){image.sprite=coinSprite;image.enabled=coinSprite!=null;}var fallback=coinTarget.Find("CoinSymbol");if(fallback!=null)fallback.gameObject.SetActive(coinSprite==null);}
            if(bookImage!=null){bookImage.sprite=recipeBookSprite;bookImage.enabled=recipeBookSprite!=null;var glyph=bookImage.transform.parent.Find("BookGlyph");if(glyph!=null)glyph.gameObject.SetActive(recipeBookSprite==null);}
        }
        public void MoneyEarned(int amount,Vector3 worldSource)
        {
            if(amount<=0||canvas==null)return;
            pending+=amount;
            var go=new GameObject("FloatingMoney",typeof(RectTransform),typeof(TextMeshProUGUI),typeof(CanvasGroup));go.transform.SetParent(canvas,false);
            var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(150,42);var start=(Vector2)canvas.InverseTransformPoint(worldSource);
            start.x=Mathf.Clamp(start.x,canvas.rect.xMin+80,canvas.rect.xMax-80);start.y=Mathf.Clamp(start.y,canvas.rect.yMin+30,canvas.rect.yMax-30);rect.anchoredPosition=start;
            var text=go.GetComponent<TMP_Text>();text.font=Resources.Load<GameUiTheme>("GameUiTheme")?.font??TMP_Settings.defaultFontAsset;text.text="+¥"+amount;text.fontSize=28;text.fontStyle=FontStyles.Bold;text.color=Ink;text.outlineColor=GameUiTheme.Paper;text.outlineWidth=.15f;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
            var group=go.GetComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            flights.Add(new Flight{rect=rect,start=start,amount=amount});
        }
        public void AnimateServing(RectTransform target)
        {
            var box=Object.FindFirstObjectByType<IngredientStove>();
            var plate=target!=null?target.GetComponentInParent<ConveyorPlate>():null;
            if(plate==null||box==null||canvas==null||!isActiveAndEnabled)return;
            var boxRect=(RectTransform)box.transform;
            plate.AnimateFood(target,boxRect.TransformPoint(boxRect.rect.center),canvas,servingArcHeight,servingDuration,landingBounce);
        }
        private void Update()
        {
            if(Menu.GameplayPauseController.IsPaused)return;
            FitNotes();
            if(belt==null)belt=Object.FindFirstObjectByType<ConveyorController>();
            float dt=Time.unscaledDeltaTime;
            for(int i=flights.Count-1;i>=0;i--)
            {
                var flight=flights[i];flight.age+=dt;float t=Mathf.Clamp01(flight.age/.6f);float smooth=t*t*(3-2*t);
                var target=coinTarget!=null?coinTarget:revenuePanel;
                Vector2 end=target!=null?(Vector2)canvas.InverseTransformPoint(target.TransformPoint(target.rect.center)):new Vector2(canvas.rect.xMax-130,canvas.rect.yMax-60);
                flight.rect.anchoredPosition=Vector2.Lerp(flight.start,end,smooth)+Vector2.up*Mathf.Sin(t*Mathf.PI)*24;
                flight.rect.localScale=Vector3.one*Mathf.Lerp(1,.45f,t);
                if(t>=1){pending-=flight.amount;flights.RemoveAt(i);Destroy(flight.rect.gameObject);bounce=0;if(sound!=null&&coinSound!=null)sound.PlayOneShot(coinSound,.3f);}
            }
            DisplayedRevenue=Mathf.Lerp(DisplayedRevenue,Actual-pending,1-Mathf.Exp(-14*dt));if(Mathf.Abs(DisplayedRevenue-(Actual-pending))<.1f)DisplayedRevenue=Actual-pending;
            bounce=Mathf.Min(1,bounce+dt/.22f);if(revenuePanel!=null)revenuePanel.localScale=homeScale*(1+Mathf.Sin(bounce*Mathf.PI)*.045f);
        }
    }
}
