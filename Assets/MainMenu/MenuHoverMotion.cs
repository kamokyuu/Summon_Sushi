using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
namespace CookingCards.Menu
{
    public sealed class MenuHoverMotion:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        [SerializeField]private bool rocking;
        [SerializeField,Min(.1f)]private float rockPeriod=6;
        [SerializeField,Range(0,5)]private float rockDegrees=3;
        [SerializeField,Min(0)]private float verticalPixels=2;
        [SerializeField]private Vector2 chopstickPivot=new Vector2(.55f,.12f);
        [SerializeField,Range(1,1.15f)]private float hoverScale=1.03f;
        [SerializeField,Min(1)]private float hoverSmoothing=10;
        [Header("Main Menu logo rotation")]
        [SerializeField]private bool floatingLogo;
        [SerializeField,Min(0)]private float logoRotationDegrees=3;
        [Tooltip("Full rotation oscillations per second. 0.3333333 gives a 3-second loop.")]
        [SerializeField,Min(.01f)]private float logoRotationSpeed=1f/3f;
        [Header("Main Menu vertical Dock")]
        [SerializeField,Range(1,1.5f)]private float dockHoverScale=1.2f;
        [SerializeField,Range(1,1.2f)]private float adjacentScale=1.05f;
        [SerializeField,Min(0)]private float minimumGap=2;
        [SerializeField]private Vector2 dockPivot=new Vector2(1,.5f);
        [SerializeField,Min(0)]private float textLeftPadding=24;
        private static readonly List<MenuHoverMotion> docks=new List<MenuHoverMotion>();
        private bool dock;
        private RectTransform rect;
        private Canvas canvas;
        private Image image;
        private Selectable selectable;
        private Vector2 home;
        private Vector3 scale;
        private Quaternion rotation;
        private float age,hover=1;
        private bool over,ready;
        public void Configure(bool logo){rocking=logo;hoverScale=logo?1.05f:1.03f;}
        public void ConfigureFloatingLogo(){floatingLogo=true;rocking=false;hoverScale=1.08f;}
        private void OnEnable(){rect=(RectTransform)transform;canvas=GetComponentInParent<Canvas>();image=GetComponent<Image>();selectable=GetComponent<Selectable>();home=rect.anchoredPosition;scale=rect.localScale;rotation=rect.localRotation;age=0;hover=1;over=false;ready=true;dock=!rocking&&selectable is Button&&gameObject.scene.name=="MainMenu";if(dock&&!docks.Contains(this))docks.Add(this);}
        private void Start()
        {
            if(!dock)return;
            Canvas.ForceUpdateCanvases();
            var delta=dockPivot-rect.pivot;
            rect.anchoredPosition+=Vector2.Scale(Vector2.Scale(rect.rect.size,delta),new Vector2(scale.x,scale.y));rect.pivot=dockPivot;home=rect.anchoredPosition;
            ApplyTextLayout();
        }
        public void ApplyTextLayout()
        {
            foreach(var text in GetComponentsInChildren<Text>(true))
            {
                FitLabel(text.rectTransform);text.alignment=TextAnchor.MiddleLeft;
                text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            }
            foreach(var text in GetComponentsInChildren<TMP_Text>(true))
            {
                FitLabel(text.rectTransform);text.alignment=TextAlignmentOptions.MidlineLeft;
                text.margin=Vector4.zero;text.overflowMode=TextOverflowModes.Truncate;
            }
        }
        private void FitLabel(RectTransform label)
        {
            if(label.parent!=transform)return;
            label.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;label.pivot=new Vector2(.5f,.5f);
            label.localScale=Vector3.one;label.localRotation=Quaternion.identity;
            label.offsetMin=new Vector2(textLeftPadding,0);label.offsetMax=new Vector2(-8,0);
        }
        private void UpdateDock()
        {
            var column=docks.FindAll(x=>x!=null&&x.isActiveAndEnabled&&x.rect.parent==rect.parent);
            column.Sort((a,b)=>b.home.y.CompareTo(a.home.y));
            if(column.Count==0||column[0]!=this)return;
            int active=column.FindIndex(x=>x.over&&x.selectable.IsInteractable());
            float easing=1-Mathf.Exp(-hoverSmoothing*Time.unscaledDeltaTime);
            foreach(var item in column)
            {
                int distance=active<0?int.MaxValue:Mathf.Abs(column.IndexOf(item)-active);
                float target=distance==0?item.dockHoverScale:distance==1?item.adjacentScale:1;
                item.hover=Mathf.Lerp(item.hover,target,easing);item.rect.localScale=item.scale*item.hover;item.rect.anchoredPosition=item.home;
            }
            int center=active<0?column.Count/2:active;
            for(int i=center-1;i>=0;i--)Separate(column[i],column[i+1],true);
            for(int i=center+1;i<column.Count;i++)Separate(column[i],column[i-1],false);
        }
        private static void Separate(MenuHoverMotion item,MenuHoverMotion neighbor,bool above)
        {
            float itemHalf=item.rect.rect.height*Mathf.Abs(item.scale.y)*item.hover*.5f;
            float neighborHalf=neighbor.rect.rect.height*Mathf.Abs(neighbor.scale.y)*neighbor.hover*.5f;
            float separation=itemHalf+neighborHalf+Mathf.Max(item.minimumGap,neighbor.minimumGap);
            var position=item.rect.anchoredPosition;
            position.y=above?Mathf.Max(position.y,neighbor.rect.anchoredPosition.y+separation):Mathf.Min(position.y,neighbor.rect.anchoredPosition.y-separation);
            item.rect.anchoredPosition=position;
        }
        public void OnPointerEnter(PointerEventData data){over=selectable==null||selectable.IsInteractable();}
        public void OnPointerExit(PointerEventData data){over=false;}
        private Vector2 PivotPoint()
        {
            Rect area=rect.rect;
            if(image!=null&&image.preserveAspect&&image.sprite!=null)
            {
                float ratio=image.sprite.rect.width/Mathf.Max(1,image.sprite.rect.height);var fit=area.size;
                if(fit.x/Mathf.Max(1,fit.y)>ratio)fit.x=fit.y*ratio;else fit.y=fit.x/ratio;
                area=new Rect(area.min+Vector2.Scale(area.size-fit,rect.pivot),fit);
            }
            return area.min+Vector2.Scale(area.size,new Vector2(Mathf.Clamp01(chopstickPivot.x),Mathf.Clamp01(chopstickPivot.y)));
        }
        private void LateUpdate()
        {
            if(dock){UpdateDock();return;}
            age+=Time.unscaledDeltaTime;if(selectable!=null&&!selectable.IsInteractable())over=false;
            hover=Mathf.Lerp(hover,over?hoverScale:1,1-Mathf.Exp(-hoverSmoothing*Time.unscaledDeltaTime));rect.localScale=scale*hover;
            if(floatingLogo)
            {
                float floatPhase=age*Mathf.PI*2*logoRotationSpeed;
                rect.localRotation=rotation*Quaternion.Euler(0,0,Mathf.Sin(floatPhase)*logoRotationDegrees);
                return;
            }
            if(!rocking)return;
            float phase=age*Mathf.PI*2/Mathf.Max(.1f,rockPeriod);var turn=rotation*Quaternion.Euler(0,0,Mathf.Sin(phase)*rockDegrees);
            Vector3 pivot=PivotPoint();var offset=rotation*Vector3.Scale(scale,pivot)-turn*Vector3.Scale(scale*hover,pivot);
            rect.localRotation=turn;rect.anchoredPosition=home+(Vector2)offset+Vector2.up*(Mathf.Sin(phase)*verticalPixels/Mathf.Max(.01f,canvas!=null?canvas.scaleFactor:1));
        }
        private void OnDisable(){docks.Remove(this);if(!ready||rect==null)return;rect.localScale=scale;if(rocking||dock||floatingLogo){rect.anchoredPosition=home;rect.localRotation=rotation;}ready=false;}
    }
}
