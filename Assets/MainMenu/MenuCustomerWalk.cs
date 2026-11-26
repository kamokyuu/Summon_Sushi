using UnityEngine;
namespace CookingCards.Menu
{
    [RequireComponent(typeof(RectTransform),typeof(CanvasGroup))]
    public sealed class MenuCustomerWalk:MonoBehaviour
    {
        [Tooltip("Existing endpoint in UI coordinates.")]
        [SerializeField]private Vector2 endPosition=new Vector2(-350,71);
        [SerializeField,Min(.1f)]private float entranceSeconds=2.5f;
        [SerializeField,Min(0)]private float startGapPixels=4;
        [SerializeField]private float startYOffset;
        [SerializeField,Min(.1f)]private float stepSeconds=.8f;
        [SerializeField,Min(0)]private float bobPixels=3,swayDegrees=1.2f;
        [SerializeField,Min(.1f)]private float idlePeriodSeconds=3,settleSeconds=.5f;
        [SerializeField,Min(0)]private float idleBobPixels=6,idleSwayDegrees=2;
        [SerializeField,Min(1)]private float idleBreathingScale=1.025f;
        private RectTransform rect,parent;
        private Canvas canvas;
        private CanvasGroup group;
        private Vector2 home;
        private Vector3 scale;
        private Quaternion rotation;
        private float walkAge,idleAge,homeAlpha;
        private bool initialized,arrived;
        public void ConfigureRoute(Vector2 start,Vector2 end){endPosition=end;startYOffset=start.y-end.y;}
        private void OnEnable()
        {
            if(initialized){if(arrived)rect.anchoredPosition=endPosition;return;}
            rect=(RectTransform)transform;parent=rect.parent as RectTransform;canvas=GetComponentInParent<Canvas>();group=GetComponent<CanvasGroup>();
            home=rect.anchoredPosition;scale=rect.localScale;rotation=rect.localRotation;homeAlpha=group.alpha;walkAge=idleAge=0;initialized=true;
        }
        private float PixelScale=>Mathf.Max(.01f,canvas!=null?canvas.scaleFactor:1);
        private Vector2 EntranceStart()
        {
            float swayPadding=Mathf.Abs(rect.rect.height*scale.y)*Mathf.Abs(Mathf.Sin(swayDegrees*Mathf.Deg2Rad));
            float rightExtent=Mathf.Abs(rect.rect.xMax*scale.x)+swayPadding;
            return new Vector2(Mathf.Min(parent.rect.xMin-rightExtent-startGapPixels/PixelScale,endPosition.x-1),endPosition.y+startYOffset);
        }
        private void LateUpdate()
        {
            if(parent==null)return;
            float dt=Time.unscaledDeltaTime;
            if(!arrived)
            {
                walkAge+=dt;float progress=Mathf.Clamp01(walkAge/Mathf.Max(.1f,entranceSeconds));
                float envelope=1-Mathf.SmoothStep(0,1,progress);float step=walkAge*Mathf.PI*2/Mathf.Max(.1f,stepSeconds);
                rect.anchoredPosition=Vector2.Lerp(EntranceStart(),endPosition,Mathf.SmoothStep(0,1,progress))+Vector2.up*(Mathf.Sin(step)*bobPixels*envelope/PixelScale);
                rect.localRotation=rotation*Quaternion.Euler(0,0,Mathf.Sin(step)*swayDegrees*envelope);
                if(progress>=1){arrived=true;idleAge=0;rect.anchoredPosition=endPosition;rect.localRotation=rotation;}return;
            }
            idleAge+=dt;float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(idleAge/Mathf.Max(.1f,settleSeconds)));float breath=Mathf.Sin(idleAge*Mathf.PI*2/Mathf.Max(.1f,idlePeriodSeconds))*blend;
            rect.anchoredPosition=endPosition+Vector2.up*(breath*idleBobPixels/PixelScale);rect.localRotation=rotation*Quaternion.Euler(0,0,breath*idleSwayDegrees);
            float phase=idleAge*Mathf.PI*2/Mathf.Max(.1f,idlePeriodSeconds);
            rect.localScale=scale*(1+(Mathf.Max(1,idleBreathingScale)-1)*(1-Mathf.Cos(phase))*.5f*blend);
        }
        private void OnDisable(){if(!initialized||rect==null)return;rect.anchoredPosition=home;rect.localRotation=rotation;rect.localScale=scale;if(group!=null)group.alpha=homeAlpha;}
    }
}
