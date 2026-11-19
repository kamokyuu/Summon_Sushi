using System.Collections.Generic;
using UnityEngine;
namespace CookingCards
{
    /// <summary>Owns motion, plate lifecycle and base-price payout; recipe data stays in the catalog.</summary>
    public sealed class ConveyorController : MonoBehaviour
    {
        [SerializeField] private Sprite[] normalSprites, brokenSprites;
        [SerializeField] private Sprite[] foodSprites = new Sprite[4];
        [SerializeField,Range(.2f,1.5f)] private float foodScale=.8f;
        public Sprite FoodSprite(RecipeGrade grade)=>foodSprites!=null && (int)grade<foodSprites.Length?foodSprites[(int)grade]:null;
        public float FoodScale=>foodScale;
        [SerializeField] private Vector2 plateSize=new Vector2(145,110);
        [SerializeField, Tooltip("Vertical position of moving plates relative to the conveyor center. Positive moves up; negative moves down.")]
        private float plateVerticalOffset;
        [SerializeField,Min(0)] private float gapPixels=5;
        [SerializeField] private Transform breakMarker;
        public bool TryServeLeftmost(SushiRecipe recipe)
        {
            if(recipe==null || CustomerSystem.GameFinished || Menu.GameplayPauseController.IsPaused)return false;
            ConveyorPlate target=null;
            foreach(var p in plates)
                if(!p.IsBroken && p.ServedRecipe==null && p.Grade==recipe.Grade && ((RectTransform)p.transform).anchoredPosition.x>=-rect.rect.width*.5f && ((RectTransform)p.transform).anchoredPosition.x<=rect.rect.width*.5f &&
                    (target==null || p.transform.position.x<target.transform.position.x))target=p;
            return target!=null && target.TryServe(recipe);
        }
        [SerializeField,Min(1)] private float speedPixelsPerSecond=28;
        [SerializeField,Range(0,1)] private float breakDistanceInPlateWidths=.65f;
        [SerializeField] private TMPro.TMP_Text walletLabel;
        private readonly List<ConveyorPlate> plates=new List<ConveyorPlate>();
        public IReadOnlyList<ConveyorPlate> Plates=>plates;
        public int ServedCount { get; private set; }
        public int MissedCount { get; private set; }
        public int Revenue { get; private set; }
        public int Penalties {get;private set;}
        public int PurchasedCount {get;private set;}
        public static int BreakPenalty(RecipeGrade grade)=>50+15*(int)grade;
        private RectTransform rect;
        private Canvas canvas;
        private float nextSpawnX;
        private bool spawnInitialized;
        private float Gap=>gapPixels/Mathf.Max(.01f,canvas.scaleFactor);
        public void Configure(Sprite[] normal,Sprite[] broken,TMPro.TMP_Text wallet)
        {normalSprites=normal;brokenSprites=broken;walletLabel=wallet;}
        private void Awake(){rect=(RectTransform)transform;canvas=GetComponentInParent<Canvas>();}
        public static int Price(RecipeGrade grade)=>grade==RecipeGrade.Common?150:grade==RecipeGrade.Uncommon?200:grade==RecipeGrade.Rare?300:350;
        public static RecipeGrade GradeFromSample(float sample)=>sample<.4f?RecipeGrade.Common:sample<.7f?RecipeGrade.Uncommon:sample<.9f?RecipeGrade.Special:RecipeGrade.Rare;
        private void Update()
        {
            if(Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished)return;
            float width=rect.rect.width;
            float step=speedPixelsPerSecond/Mathf.Max(.01f,canvas.scaleFactor)*Time.deltaTime;
            float size=Mathf.Max(10,plateSize.x);
            for(int i=plates.Count-1;i>=0;i--)
            {
                var plate=plates[i];var p=(RectTransform)plate.transform;
                p.sizeDelta=new Vector2(size,Mathf.Max(10,plateSize.y));
                p.anchoredPosition+=Vector2.left*step;
                p.anchoredPosition=new Vector2(p.anchoredPosition.x,plateVerticalOffset);
                float breakX=breakMarker!=null?rect.InverseTransformPoint(breakMarker.position).x:-width*.5f+size*breakDistanceInPlateWidths;
                if(p.anchoredPosition.x<breakX)plate.BreakIfEmpty();
                if(p.anchoredPosition.x<-width*.5f-size*.5f){plates.RemoveAt(i);Destroy(plate.gameObject);}
            }
            // Spawn slots move independently of plate removal: a purchase leaves a real gap.
            if(!spawnInitialized){nextSpawnX=width*.5f-size*.5f;spawnInitialized=true;}
            nextSpawnX-=step;
            if(nextSpawnX<=width*.5f+size*.5f)
            {SpawnPlate(GradeFromSample(Random.value),nextSpawnX);nextSpawnX+=size+Gap;}
        }
        public ConveyorPlate SpawnPlate(RecipeGrade grade,float x)
        {
            var go=new GameObject(grade+"Plate",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(ConveyorPlate));go.transform.SetParent(transform,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=plateSize;r.anchoredPosition=new Vector2(x,plateVerticalOffset);
            var plate=go.GetComponent<ConveyorPlate>();plate.Configure(this,grade,normalSprites[(int)grade],brokenSprites[(int)grade]);plates.Add(plate);return plate;
        }
        public void RecordServed(RecipeGrade grade){ServedCount++;}
        public int BuyAvailable(int limit)
        {
            if(Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished)return 0;
            var stock=new List<ConveyorPlate>();
            foreach(var p in plates)if(p.ServedRecipe!=null && !p.IsBroken && Mathf.Abs(((RectTransform)p.transform).anchoredPosition.x)<=rect.rect.width*.5f)stock.Add(p);
            stock.Sort((a,b)=>a.transform.position.x.CompareTo(b.transform.position.x));
            int bought=0;
            foreach(var p in stock)
            {
                if(bought>=Mathf.Clamp(limit,1,3))break;
                int price=Price(p.Grade);Revenue+=price;PurchasedCount++;PreparationSession.Reward(price);
                CustomerSystem.Active?.RecordConveyorPurchase(price);
                if(walletLabel!=null)walletLabel.text="¥ "+PreparationSession.Yen;
                plates.Remove(p);p.gameObject.SetActive(false);Destroy(p.gameObject);bought++;
            }
            return bought;
        }
        public bool HasAvailableFood
        {get{foreach(var p in plates)if(p.ServedRecipe!=null && !p.IsBroken && Mathf.Abs(((RectTransform)p.transform).anchoredPosition.x)<=rect.rect.width*.5f)return true;return false;}}
        public void RecordMissed(RecipeGrade grade)
        {
            if(CustomerSystem.GameFinished)return;
            MissedCount++;int penalty=BreakPenalty(grade);Penalties+=penalty;PreparationSession.Reward(-penalty);
            CustomerSystem.Active?.RecordConveyorPurchase(-penalty);
            if(walletLabel!=null)walletLabel.text="¥ "+PreparationSession.Yen;
        }
    }
}
