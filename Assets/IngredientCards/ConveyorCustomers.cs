using UnityEngine;
namespace CookingCards
{
    public enum ConveyorCustomerBehavior { Regular,Picky,Hungry }
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
            if(ComboShopBoss.EncounterPaused)return;
            if(Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished || visitorActive || belt==null || Time.time<nextArrival || !belt.HasAvailableFood)return;
            CustomerCount++;visitorActive=true;
            var go=new GameObject("ConveyorCustomer_"+CustomerCount,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(ConveyorCustomerVisit));go.transform.SetParent(transform,false);
            var r=(RectTransform)go.transform;r.sizeDelta=new Vector2(160,240);r.pivot=new Vector2(.5f,0);
            var image=go.GetComponent<UnityEngine.UI.Image>();image.raycastTarget=false;image.preserveAspect=true;
            if(portraits!=null && portraits.Length>0)image.sprite=portraits[Random.Range(0,portraits.Length)];
            var label=new GameObject("PurchaseLabel",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(go.transform,false);
            var lr=(RectTransform)label.transform;lr.anchorMin=lr.anchorMax=new Vector2(1,.7f);lr.pivot=new Vector2(0,.5f);lr.sizeDelta=new Vector2(110,36);lr.anchoredPosition=new Vector2(8,0);
            var t=label.GetComponent<TMPro.TextMeshProUGUI>();t.font=TMPro.TMP_Settings.defaultFontAsset;t.text="";t.fontSize=17;t.color=GameplayHudFeedback.Ink;t.alignment=TMPro.TextAlignmentOptions.Center;t.raycastTarget=false;
            float sample=Random.value;var config=ActiveLevel.Config;
            var behavior=sample<config.pickyCustomerChance?ConveyorCustomerBehavior.Picky:sample<config.pickyCustomerChance+config.hungryCustomerChance?ConveyorCustomerBehavior.Hungry:ConveyorCustomerBehavior.Regular;
            RecipeGrade? preference=null;
            if(behavior==ConveyorCustomerBehavior.Picky)
                foreach(var plate in belt.Plates)if(plate.ServedRecipe!=null&&!plate.IsBroken&&Mathf.Abs(((RectTransform)plate.transform).anchoredPosition.x)<=((RectTransform)belt.transform).rect.width*.5f){preference=plate.Grade;break;}
            int quantity=behavior==ConveyorCustomerBehavior.Hungry?Random.Range(2,4):1;
            if(behavior!=ConveyorCustomerBehavior.Regular)
            {
                var bubble=new GameObject("PreferenceBubble",typeof(RectTransform),typeof(UnityEngine.UI.Image));bubble.transform.SetParent(go.transform,false);var br=(RectTransform)bubble.transform;br.anchorMin=br.anchorMax=lr.anchorMin;br.pivot=lr.pivot;br.sizeDelta=lr.sizeDelta;br.anchoredPosition=lr.anchoredPosition;
                var bg=bubble.GetComponent<UnityEngine.UI.Image>();bg.sprite=GameplayHudFeedback.Active?.preferenceBubbleSprite;bg.type=UnityEngine.UI.Image.Type.Sliced;bg.color=GameUiTheme.Paper;bg.raycastTarget=false;label.transform.SetParent(bubble.transform,false);lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=new Vector2(4,3);lr.offsetMax=new Vector2(-4,-3);
                var icon=GameplayHudFeedback.Active!=null?(behavior==ConveyorCustomerBehavior.Picky?GameplayHudFeedback.Active.pickyIcon:GameplayHudFeedback.Active.hungryIcon):null;
                if(icon!=null){var badge=new GameObject("BehaviorIcon",typeof(RectTransform),typeof(UnityEngine.UI.Image));badge.transform.SetParent(bubble.transform,false);((RectTransform)badge.transform).anchoredPosition=new Vector2(-40,0);((RectTransform)badge.transform).sizeDelta=new Vector2(20,20);badge.GetComponent<UnityEngine.UI.Image>().sprite=icon;badge.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;lr.offsetMin=new Vector2(24,3);}
            }
            float outside=((RectTransform)GetComponentInParent<Canvas>().transform).rect.width*.5f+180;
            var beltRect=(RectTransform)belt.transform;
            float groundY=transform.InverseTransformPoint(beltRect.TransformPoint(new Vector3(0,beltRect.rect.yMax-12,0))).y;
            go.GetComponent<ConveyorCustomerVisit>().Configure(this,belt,t,Random.value<.5f?-outside:outside,Random.Range(-220,221),quantity,groundY,behavior,preference);
        }
        public void RecordPurchase(int count){PurchasedCount+=count;}
        public void Departed(){visitorActive=false;nextArrival=Time.time+arrivalInterval;}
    }
}
