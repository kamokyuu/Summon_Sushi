using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CookingCards
{
    public sealed class ComboShopBoss:MonoBehaviour
    {
        public static ComboShopBoss Active {get;private set;}
        public static bool Selecting=>Active!=null&&Active.ShopOpen;
        public static bool ConveyorStopped=>Selecting||Active!=null&&Active.freezeRemaining>0;
        public bool ShopOpen {get;private set;}
        public int Consecutive {get;private set;}
        public int BestCombo {get;private set;}
        public int ShopSpent {get;private set;}
        public int ExtraHandSlots {get;private set;}
        public int ShieldCharges {get;private set;}
        public float ComboBonus=>ActiveLevel.Config.comboEnabled?Mathf.Min(4,Mathf.Max(0,Consecutive-1))*.1f:0;
        public bool BossStarted {get;private set;}
        public int BossSales {get;private set;}
        public bool BossComplete=>!ActiveLevel.Config.bossCustomersEnabled||BossStarted&&BossSales>=2;
        public bool BossFailed=>BossStarted&&bossRemaining<=0&&!BossComplete;
        public IReadOnlyList<LevelPowerUp> Offered=>offered;
        private float shopRemaining,bossRemaining,freezeRemaining;
        private bool choosingResource;
        private ConveyorController belt;
        private TMPro.TMP_Text status,shopTitle;
        private GameObject shop,resources;
        private LevelPowerUp[] offered=new LevelPowerUp[0];
        private UnityEngine.UI.Button[] buttons;
        private readonly Dictionary<string,int> sales=new Dictionary<string,int>();
        private void Awake(){Active=this;belt=GetComponent<ConveyorController>();}
        private void OnDestroy(){if(Active==this)Active=null;if(status!=null)Destroy(status.gameObject);if(shop!=null)Destroy(shop);}
        private void Start(){EnsureUi();}
        public float RecordFill()
        {
            Consecutive++;BestCombo=Mathf.Max(BestCombo,Consecutive);float bonus=ComboBonus;
            if(ActiveLevel.Config.powerUpsEnabled&&Consecutive%3==0&&!GuidedLevelTutorial.Protecting)OpenShop();
            return 1+bonus;
        }
        public void RecordPurchase(SushiRecipe recipe)
        {
            if(!sales.ContainsKey(recipe.Id))sales[recipe.Id]=0;sales[recipe.Id]++;
            if(BossStarted&&!BossComplete)BossSales++;
        }
        public void Missed(){Consecutive=0;}
        public bool TryShieldMiss(){if(ShieldCharges<=0)return false;ShieldCharges--;return true;}
        public void CheckBoss(bool targetReached)
        {
            if(!ActiveLevel.Config.bossCustomersEnabled||BossStarted||!targetReached)return;
            BossStarted=true;bossRemaining=30;
            var recipe=ActiveLevel.Recipes.First(r=>r.Grade==RecipeGrade.Common);
            belt.SpawnPlate(recipe.Grade,0);belt.SpawnPlate(recipe.Grade,155);
            var hand=Object.FindFirstObjectByType<IngredientCardHand>();
            foreach(var item in recipe.Ingredients)for(int i=0;i<item.Count;i++)hand.TryReturnCard(item.Kind);
        }
        public bool Applicable(LevelPowerUp option)
        {
            if(option==null)return false;
            var hand=Object.FindFirstObjectByType<IngredientCardHand>();
            switch(option.effect)
            {
                case PowerUpEffect.LuckyDraw:return hand!=null&&!hand.IsDealing&&hand.Capacity-hand.CardCount>=3;
                case PowerUpEffect.KnifeRefill:var knife=Object.FindFirstObjectByType<KnifeDragHandle>();return ActiveLevel.KnifeEnabled&&knife!=null&&knife.Charges<knife.MaximumCharges;
                case PowerUpEffect.PlateShield:return ShieldCharges==0;
                case PowerUpEffect.ConveyorFreeze:return freezeRemaining<=0;
                default:return true;
            }
        }
        public bool OpenShop()
        {
            if(ShopOpen||!ActiveLevel.Config.powerUpsEnabled||CustomerSystem.GameFinished)return false;
            var options=(ActiveLevel.Config.powerUps??new LevelPowerUp[0]).Where(Applicable).ToList();if(options.Count==0)return false;
            for(int i=options.Count-1;i>0;i--){int j=Random.Range(0,i+1);var value=options[i];options[i]=options[j];options[j]=value;}
            offered=options.Take(3).ToArray();EnsureUi();ShopOpen=true;choosingResource=false;shopRemaining=10;shop.SetActive(true);resources.SetActive(false);shop.transform.SetAsLastSibling();
            for(int i=0;i<3;i++){buttons[i].gameObject.SetActive(i<offered.Length);if(i<offered.Length)buttons[i].GetComponentInChildren<TMPro.TMP_Text>().text=offered[i].title+"\n"+offered[i].description+"\n¥250";}
            RefreshShop();return true;
        }
        private bool CanPurchase=>ShopOpen&&!Menu.GameplayPauseController.IsPaused&&!CustomerSystem.GameFinished&&shopRemaining>0&&PreparationSession.Yen>=250;
        public bool Purchase(int index)
        {
            if(!CanPurchase||choosingResource||index<0||index>=offered.Length||!Applicable(offered[index]))return false;
            switch(offered[index].effect)
            {
                case PowerUpEffect.ExtraHand:ExtraHandSlots++;break;
                case PowerUpEffect.KnifeRefill:if(!Object.FindFirstObjectByType<KnifeDragHandle>().Refill(1))return false;break;
                case PowerUpEffect.ConveyorFreeze:freezeRemaining=30;break;
                case PowerUpEffect.PlateShield:ShieldCharges=1;break;
                case PowerUpEffect.LuckyDraw:choosingResource=true;foreach(var button in buttons)button.gameObject.SetActive(false);resources.SetActive(true);RefreshShop();return true;
            }
            PayAndClose();return true;
        }
        public bool PickResource(IngredientKind kind)
        {
            if(!CanPurchase||!choosingResource||!ActiveLevel.Config.Allows(kind)||!Object.FindFirstObjectByType<IngredientCardHand>().TryGiveCards(kind,3))return false;
            PayAndClose();return true;
        }
        private void PayAndClose(){PreparationSession.Reward(-250);CustomerSystem.Active?.RecordConveyorPurchase(-250);belt.RefreshWallet();ShopSpent+=250;CloseShop();}
        public void CloseShop(){ShopOpen=false;choosingResource=false;if(shop!=null)shop.SetActive(false);}
        public void Advance(float seconds)
        {
            if(Menu.GameplayPauseController.IsPaused||CustomerSystem.GameFinished||GuidedLevelTutorial.Protecting)return;
            seconds=Mathf.Max(0,seconds);
            if(ShopOpen){shopRemaining=Mathf.Max(0,shopRemaining-seconds);RefreshShop();if(shopRemaining<=0)CloseShop();return;}
            freezeRemaining=Mathf.Max(0,freezeRemaining-seconds);
            if(BossStarted&&!BossComplete)bossRemaining=Mathf.Max(0,bossRemaining-seconds);
        }
        private void Update()
        {
            Advance(Time.deltaTime);
            if(status!=null)status.text="COMBO "+Consecutive+"  +"+Mathf.RoundToInt(ComboBonus*100)+"%"+(freezeRemaining>0?"  FREEZE "+Mathf.CeilToInt(freezeRemaining)+"s":"")+(ShieldCharges>0?"  SHIELD":"")+(BossStarted?"\nBOSS: sell any 2 dishes  "+BossSales+"/2  "+Mathf.CeilToInt(bossRemaining)+"s":"");
        }
        private void RefreshShop(){if(shopTitle==null)return;shopTitle.text=(choosingResource?"LUCKY DRAW — choose 3 cards of one resource":"POWER-UP SHOP — choose one / ¥250")+" — "+Mathf.CeilToInt(shopRemaining)+"s";for(int i=0;i<offered.Length;i++)buttons[i].interactable=PreparationSession.Yen>=250&&Applicable(offered[i]);}
        public string Results()
        {
            var text=new System.Text.StringBuilder("\nBest combo: ").Append(BestCombo).Append(" | Shop: ¥").Append(ShopSpent);
            if(ActiveLevel.Config.bossCustomersEnabled)text.Append("\nBoss: ").Append(BossComplete?"DEFEATED":BossFailed?"FAILED":"NOT REACHED");
            foreach(var recipe in ActiveLevel.Recipes)text.Append("\n").Append(recipe.DisplayName).Append(" × ").Append(sales.TryGetValue(recipe.Id,out int count)?count:0);
            return text.ToString();
        }
        private void EnsureUi()
        {
            if(status!=null)return;var canvas=GetComponentInParent<Canvas>().transform;
            status=Label(canvas,"ComboBossStatus",new Vector2(-205,-55),new Vector2(400,110));var r=(RectTransform)status.transform;r.anchorMin=r.anchorMax=new Vector2(1,1);
            shop=new GameObject("PowerUpShop",typeof(RectTransform),typeof(UnityEngine.UI.Image));shop.transform.SetParent(canvas,false);r=(RectTransform)shop.transform;r.sizeDelta=new Vector2(870,280);shop.GetComponent<UnityEngine.UI.Image>().color=new Color(.07f,.1f,.15f,.98f);
            shopTitle=Label(shop.transform,"ShopTitle",new Vector2(0,100),new Vector2(850,50));buttons=new UnityEngine.UI.Button[3];
            for(int i=0;i<3;i++){int choice=i;buttons[i]=Button(shop.transform,"Choice"+i,new Vector2((i-1)*280,-10),new Vector2(260,160),()=>Purchase(choice));}
            Button(shop.transform,"SkipShop",new Vector2(350,-115),new Vector2(120,35),CloseShop).GetComponentInChildren<TMPro.TMP_Text>().text="SKIP";
            resources=new GameObject("ResourceChoices",typeof(RectTransform));resources.transform.SetParent(shop.transform,false);
            int n=0;foreach(var ingredient in ActiveLevel.Config.activeIngredients)
            {
                var kind=ingredient;var button=Button(resources.transform,kind.ToString(),new Vector2((n%4-1.5f)*200,30-n/4*75),new Vector2(190,65),()=>PickResource(kind));button.GetComponentInChildren<TMPro.TMP_Text>().text=IngredientRules.DisplayName(kind)+" ×3";n++;
            }
            shop.SetActive(false);
        }
        private static UnityEngine.UI.Button Button(Transform parent,string name,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchoredPosition=position;r.sizeDelta=size;go.GetComponent<UnityEngine.UI.Image>().color=new Color(.2f,.3f,.38f);
            var button=go.GetComponent<UnityEngine.UI.Button>();button.onClick.AddListener(action);Label(go.transform,"Description",Vector2.zero,size-new Vector2(12,12));return button;
        }
        private static TMPro.TMP_Text Label(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchoredPosition=position;r.sizeDelta=size;
            var text=go.GetComponent<TMPro.TMP_Text>();text.font=TMPro.TMP_Settings.defaultFontAsset;text.fontSize=21;text.color=Color.white;text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;return text;
        }
    }
}
