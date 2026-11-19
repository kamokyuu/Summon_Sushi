using UnityEngine;
namespace CookingCards
{
    /// <summary>Customers walk in, stop and buy available food, then leave without patience timers.</summary>
    public sealed class ConveyorCustomers : MonoBehaviour
    {
        [SerializeField,Min(.2f)] private float arrivalInterval=4;
        private Sprite[] portraits;
        private float nextArrival;
        private ConveyorController belt;
        private bool visitorActive;
        public int CustomerCount {get;private set;}
        public int PurchasedCount {get;private set;}
        public void Configure(Sprite[] sprites){portraits=sprites;belt=Object.FindFirstObjectByType<ConveyorController>();}
        private void Update()
        {
            if(Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished || visitorActive || belt==null || Time.time<nextArrival || !belt.HasAvailableFood)return;
            CustomerCount++;visitorActive=true;
            var go=new GameObject("ConveyorCustomer_"+CustomerCount,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(ConveyorCustomerVisit));go.transform.SetParent(transform,false);
            var r=(RectTransform)go.transform;r.sizeDelta=new Vector2(160,240);r.pivot=new Vector2(.5f,0);
            var image=go.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=false;image.preserveAspect=true;
            if(portraits!=null && portraits.Length>0)image.sprite=portraits[Random.Range(0,portraits.Length)];
            var label=new GameObject("PurchaseLabel",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(go.transform,false);
            var lr=(RectTransform)label.transform;lr.sizeDelta=new Vector2(240,45);lr.anchoredPosition=new Vector2(150,50);
            var t=label.GetComponent<TMPro.TextMeshProUGUI>();t.font=TMPro.TMP_Settings.defaultFontAsset;t.text="";t.fontSize=22;t.color=Color.white;t.outlineWidth=.2f;t.outlineColor=Color.black;t.alignment=TMPro.TextAlignmentOptions.Center;t.raycastTarget=false;
            float outside=((RectTransform)GetComponentInParent<Canvas>().transform).rect.width*.5f+180;
            var beltRect=(RectTransform)belt.transform;
            float groundY=transform.InverseTransformPoint(beltRect.TransformPoint(new Vector3(0,beltRect.rect.yMax-12,0))).y;
            go.GetComponent<ConveyorCustomerVisit>().Configure(this,belt,t,Random.value<.5f?-outside:outside,Random.Range(-280,281),Random.Range(1,4),groundY);
        }
        public void RecordPurchase(int count){PurchasedCount+=count;}
        public void Departed(){visitorActive=false;nextArrival=Time.time+arrivalInterval;}
    }
}
