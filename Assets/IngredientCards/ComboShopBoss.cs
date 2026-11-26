using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CookingCards
{
    public sealed class ComboShopBoss:MonoBehaviour
    {
        public static ComboShopBoss Active {get;private set;}
        public static bool Selecting=>Active!=null&&Active.ShopOpen;
        public static bool EncounterPaused=>Selecting||Active!=null&&Active.BossAnnouncing;
        public static bool ConveyorStopped=>EncounterPaused||Active!=null&&Active.freezeRemaining>0;
        public bool ShopOpen {get;private set;}
        public int Consecutive {get;private set;}
        public int BestCombo {get;private set;}
        public int ShopSpent {get;private set;}
        public int ExtraHandSlots {get;private set;}
        public int ShieldCharges {get;private set;}
        public float ComboBonus=>ActiveLevel.Config.comboEnabled?Mathf.Min(2,Mathf.Max(0,Consecutive-1))*.1f:0;
        public float ComboRemaining {get;private set;}
        private bool streakShopOffered;
        private UnityEngine.UI.Image comboBar;
        private TMPro.TMP_Text effectsStatus;
        public bool BossStarted {get;private set;}
        public int BossSales {get;private set;}
        public bool BossAnnouncing {get;private set;}
        private int[] bossOrders;
        private GameObject bossAnnouncement;
        private bool Platter=>ActiveLevel.Config.bossKind==CampaignBossKind.SushiPlatter;
        private int OrderCount=>Platter?ActiveLevel.Config.platterOrders?.Length??0:ActiveLevel.Config.ayceOrders?.Length??0;
        private int Required(int i)=>Platter?ActiveLevel.Config.platterOrders[i].quantity:ActiveLevel.Config.ayceOrders[i].quantity;
        private SushiRecipe RequiredRecipe(int i)=>ActiveLevel.Recipes.First(r=>r.Id==ActiveLevel.Config.platterOrders[i].recipeId);
        private RecipeGrade RequiredGrade(int i)=>Platter?RequiredRecipe(i).Grade:ActiveLevel.Config.ayceOrders[i].grade;
        private bool MatchesOrder(int i,SushiRecipe recipe)=>Platter?recipe.Id==ActiveLevel.Config.platterOrders[i].recipeId:recipe.Grade==RequiredGrade(i);
        public bool BossComplete=>!ActiveLevel.Config.bossCustomersEnabled||BossStarted&&!BossAnnouncing&&(OrderCount>0?Enumerable.Range(0,OrderCount).All(i=>bossOrders!=null&&bossOrders[i]>=Required(i)):BossSales>=2);
        public bool BossFailed=>BossStarted&&bossRemaining<=0&&!BossComplete;
        public IReadOnlyList<LevelPowerUp> Offered=>offered;
        private float shopRemaining,bossRemaining,freezeRemaining;
        private bool choosingResource;
        private IngredientKind? firstLuckyIngredient;
        private ConveyorController belt;
        private TMPro.TMP_Text status,shopTitle;
        private GameObject shop,resources;
        private LevelPowerUp[] offered=new LevelPowerUp[0];
        private UnityEngine.UI.Button[] buttons;
        private readonly Dictionary<string,int> sales=new Dictionary<string,int>();
        private void Awake(){Active=this;belt=GetComponent<ConveyorController>();}
        private void OnDestroy(){if(!Application.isPlaying)return;if(Active==this)Active=null;if(status!=null)Destroy(status.gameObject);if(effectsStatus!=null)Destroy(effectsStatus.gameObject);if(shop!=null)Destroy(shop);if(bossAnnouncement!=null)Destroy(bossAnnouncement);}
        private Vector3 comboHomeScale=Vector3.one;
        private void Start(){EnsureUi();comboHomeScale=status.transform.localScale;status.text="";effectsStatus.text="";status.gameObject.SetActive(true);effectsStatus.gameObject.SetActive(true);shop.SetActive(false);resources.SetActive(false);}
        public float RecordFill()
        {
            Consecutive=Mathf.Min(3,Consecutive+1);ComboRemaining=10;BestCombo=Mathf.Max(BestCombo,Consecutive);float bonus=ComboBonus;
            if(ActiveLevel.Config.comboEnabled)Menu.UIAudioManager.PlayCombo(Consecutive);
            if(status!=null)status.transform.localScale=comboHomeScale*1.08f;
            if(ActiveLevel.Config.powerUpsEnabled&&Consecutive==3&&!streakShopOffered)
            {streakShopOffered=true;if(!GuidedLevelTutorial.Protecting)OpenShop();}
            return 1+bonus;
        }
        public void RecordPurchase(SushiRecipe recipe)
        {
            if(!sales.ContainsKey(recipe.Id))sales[recipe.Id]=0;sales[recipe.Id]++;
            if(BossStarted&&!BossAnnouncing&&!BossComplete)
            {
                if(OrderCount==0)BossSales++;
                else for(int i=0;i<OrderCount;i++)if(MatchesOrder(i,recipe)&&bossOrders[i]<Required(i)){bossOrders[i]++;BossSales++;break;}
            }
        }
        public void Missed(){Consecutive=0;ComboRemaining=0;streakShopOffered=false;}
        public bool TryShieldMiss(){if(ShieldCharges<=0)return false;ShieldCharges--;return true;}
        public void CheckBoss(bool targetReached)
        {
            if(!ActiveLevel.Config.bossCustomersEnabled||BossStarted||!targetReached)return;
            BossStarted=true;bossRemaining=ActiveLevel.Config.bossTimeLimit;bossOrders=new int[OrderCount];
            if(ActiveLevel.Config.campaignShift)
            {
                EnsureUi();BossAnnouncing=true;var canvas=GetComponentInParent<Canvas>().transform;
                EnsureBossUi();bossAnnouncement.SetActive(true);
                var announcementText=bossAnnouncement.transform.Find("Orders").GetComponent<TMPro.TMP_Text>();announcementText.text=ActiveLevel.Config.bossTitle+"\n\n"+(Platter?"SUSHI PLATTER":"AYCE ORDER")+"\n"+OrdersText()+"\n\n"+(Platter?"Serve the listed dishes.":"Any valid recipe matching each grade.")+"\nTimer: "+bossRemaining+" seconds after Ready.\nMerge, knife, exchange and power-ups remain available.";return;
            }
            SupplyBoss();
        }
        public void BeginBoss(){if(!BossAnnouncing)return;BossAnnouncing=false;if(bossAnnouncement!=null)bossAnnouncement.SetActive(false);SupplyBoss();}
        private void SupplyBoss()
        {
            if(OrderCount>0){for(int i=0;i<OrderCount;i++)if(bossOrders[i]<Required(i)&&!belt.Plates.Any(p=>!p.IsBroken&&p.ServedRecipe==null&&p.Grade==RequiredGrade(i)))belt.SpawnPlate(RequiredGrade(i),0);return;}
            var recipe=ActiveLevel.Recipes.First(r=>r.Grade==RecipeGrade.Common);
            belt.SpawnPlate(recipe.Grade,0);belt.SpawnPlate(recipe.Grade,155);
            var hand=Object.FindFirstObjectByType<IngredientCardHand>();
            foreach(var item in recipe.Ingredients)for(int i=0;i<item.Count;i++)hand.TryReturnCard(item.Kind);
        }
        private string OrdersText()
        {
            if(OrderCount==0)return BossSales+"/2 dishes";
            return string.Join("  |  ",Enumerable.Range(0,OrderCount).Select(i=>(Platter?RequiredRecipe(i).DisplayName:RequiredGrade(i).ToString())+" "+Mathf.Max(0,Required(i)-(bossOrders==null?0:bossOrders[i]))+" remaining"));
        }
        public RecipeGrade? AvailableBossGrade
        {
            get{if(!BossStarted||BossAnnouncing||BossComplete)return null;for(int i=0;i<OrderCount;i++)if(bossOrders[i]<Required(i)&&belt.Plates.Any(p=>p.ServedRecipe!=null&&!p.IsBroken&&MatchesOrder(i,p.ServedRecipe)&&Mathf.Abs(((RectTransform)p.transform).anchoredPosition.x)<=((RectTransform)belt.transform).rect.width*.5f))return RequiredGrade(i);return null;}
        }
        public string AvailableBossRecipe=>Platter&&AvailableBossGrade.HasValue?Enumerable.Range(0,OrderCount).Where(i=>bossOrders[i]<Required(i)&&belt.Plates.Any(p=>p.ServedRecipe!=null&&!p.IsBroken&&MatchesOrder(i,p.ServedRecipe)&&Mathf.Abs(((RectTransform)p.transform).anchoredPosition.x)<=((RectTransform)belt.transform).rect.width*.5f)).Select(i=>RequiredRecipe(i).Id).FirstOrDefault():null;
        public bool Applicable(LevelPowerUp option)
        {
            if(option==null)return false;
            var hand=Object.FindFirstObjectByType<IngredientCardHand>();
            switch(option.effect)
            {
                case PowerUpEffect.LuckyDraw:return hand!=null&&!hand.IsDealing&&hand.Capacity-hand.CardCount>=2;
                case PowerUpEffect.ExtraHand:return hand!=null&&hand.Capacity<11;
                case PowerUpEffect.KnifeRefill:var knife=Object.FindFirstObjectByType<KnifeDragHandle>();return ActiveLevel.KnifeEnabled&&knife!=null&&knife.Charges<knife.MaximumCharges;
                case PowerUpEffect.PlateShield:return ShieldCharges==0;
                case PowerUpEffect.ConveyorFreeze:return freezeRemaining<=0;
                default:return true;
            }
        }
        public bool OpenShop()
        {
            if(BossAnnouncing||ShopOpen||!ActiveLevel.Config.powerUpsEnabled||CustomerSystem.GameFinished)return false;
            var options=(ActiveLevel.Config.powerUps??new LevelPowerUp[0]).Where(Applicable).ToList();if(options.Count==0)return false;
            for(int i=options.Count-1;i>0;i--){int j=Random.Range(0,i+1);var value=options[i];options[i]=options[j];options[j]=value;}
            offered=options.Take(3).ToArray();EnsureUi();ShopOpen=true;choosingResource=false;shopRemaining=10;shop.SetActive(true);resources.SetActive(false);shop.transform.SetAsLastSibling();
            firstLuckyIngredient=null;for(int i=0;i<3;i++){buttons[i].gameObject.SetActive(i<offered.Length);if(i<offered.Length)buttons[i].GetComponentInChildren<TMPro.TMP_Text>().text=offered[i].title+"\n"+offered[i].description+"\n¥"+offered[i].Price;}
            RefreshShop();return true;
        }
        private bool CanPurchase(int cost)=>ShopOpen&&!Menu.GameplayPauseController.IsPaused&&!CustomerSystem.GameFinished&&shopRemaining>0&&PreparationSession.Yen>=cost;
        public bool Purchase(int index)
        {
            if(choosingResource||index<0||index>=offered.Length||!CanPurchase(offered[index].Price)||!Applicable(offered[index]))return false;
            switch(offered[index].effect)
            {
                case PowerUpEffect.ExtraHand:ExtraHandSlots+=Mathf.Min(2,11-Object.FindFirstObjectByType<IngredientCardHand>().Capacity);break;
                case PowerUpEffect.KnifeRefill:if(!Object.FindFirstObjectByType<KnifeDragHandle>().Refill(2))return false;break;
                case PowerUpEffect.ConveyorFreeze:freezeRemaining=10;break;
                case PowerUpEffect.PlateShield:ShieldCharges=1;break;
                case PowerUpEffect.LuckyDraw:choosingResource=true;foreach(var button in buttons)button.gameObject.SetActive(false);resources.SetActive(true);RefreshShop();return true;
            }
            PayAndClose(offered[index].Price);return true;
        }
        public bool PickResource(IngredientKind kind)
        {
            var hand=Object.FindFirstObjectByType<IngredientCardHand>();if(!CanPurchase(300)||!choosingResource||!ActiveLevel.Config.Allows(kind)||hand.Capacity-hand.CardCount<2)return false;
            if(!firstLuckyIngredient.HasValue){firstLuckyIngredient=kind;RefreshShop();return true;}
            if(!hand.TryGiveCards(firstLuckyIngredient.Value,kind))return false;PayAndClose(300);return true;
        }
        private void PayAndClose(int cost){PreparationSession.Reward(-cost);CustomerSystem.Active?.RecordConveyorPurchase(-cost);belt.RefreshWallet();ShopSpent+=cost;Menu.UIAudioManager.PlayPowerUp();CloseShop();}
        public void CloseShop(){ShopOpen=false;choosingResource=false;firstLuckyIngredient=null;if(shop!=null)shop.SetActive(false);}
        public void Advance(float seconds)
        {
            if(BossAnnouncing||Menu.GameplayPauseController.IsPaused||CustomerSystem.GameFinished||GuidedLevelTutorial.Protecting)return;
            seconds=Mathf.Max(0,seconds);
            if(ShopOpen){shopRemaining=Mathf.Max(0,shopRemaining-seconds);RefreshShop();if(shopRemaining<=0)CloseShop();return;}
            if(Consecutive>0){ComboRemaining=Mathf.Max(0,ComboRemaining-seconds);if(ComboRemaining<=0)Missed();}
            freezeRemaining=Mathf.Max(0,freezeRemaining-seconds);
            if(BossStarted&&!BossComplete)bossRemaining=Mathf.Max(0,bossRemaining-seconds);
        }
        private void Update()
        {
            Advance(Time.deltaTime);
            if(ActiveLevel.Config.campaignShift&&BossStarted&&!BossAnnouncing&&!BossComplete&&!BossFailed&&!EncounterPaused&&!Menu.GameplayPauseController.IsPaused)SupplyBoss();
            if(status!=null){status.text=Consecutive>0?"COMBO "+Consecutive+"x":"";status.transform.localScale=Vector3.Lerp(status.transform.localScale,comboHomeScale,Time.unscaledDeltaTime*12);}
            if(comboBar!=null){comboBar.gameObject.SetActive(Consecutive>0);comboBar.fillAmount=ComboRemaining/10;}
            if(effectsStatus!=null)effectsStatus.text=(freezeRemaining>0?"FREEZE "+Mathf.CeilToInt(freezeRemaining)+"s  ":"")+(ShieldCharges>0?"SHIELD  ":"")+(BossStarted?(Platter?"PLATTER: ":"AYCE: ")+OrdersText()+" — "+Mathf.CeilToInt(bossRemaining)+"s":ActiveLevel.Config.campaignShift?DayShiftController.Active?.WaveObjective:"");
        }
        private void RefreshShop(){if(shopTitle==null)return;shopTitle.text=(choosingResource?(firstLuckyIngredient.HasValue?"LUCKY DRAW ¥300 — choose second ingredient":"LUCKY DRAW ¥300 — choose first ingredient"):"POWER-UP SHOP — choose one")+" — "+Mathf.CeilToInt(shopRemaining)+"s";for(int i=0;i<offered.Length;i++)buttons[i].interactable=PreparationSession.Yen>=offered[i].Price&&Applicable(offered[i]);}
        public string Results()
        {
            var text=new System.Text.StringBuilder("\nBest combo: ").Append(BestCombo).Append(" | Shop: ¥").Append(ShopSpent);
            if(ActiveLevel.Config.bossCustomersEnabled)text.Append("\nBoss: ").Append(BossComplete?"DEFEATED":BossStarted?"FAILED":"NOT REACHED");
            if(ActiveLevel.Config.campaignShift)text.Append(Platter?"\nPlatter: ":"\nAYCE: ").Append(OrdersText());
            foreach(var recipe in ActiveLevel.Recipes)text.Append("\n").Append(recipe.DisplayName).Append(" × ").Append(sales.TryGetValue(recipe.Id,out int count)?count:0);
            return text.ToString();
        }
        private void EnsureUi()
        {
            if(status!=null)return;var canvas=GetComponentInParent<Canvas>().transform;
            if(canvas.Find("ComboBossStatus")!=null)
            {
                status=canvas.Find("ComboBossStatus").GetComponent<TMPro.TMP_Text>();comboBar=status.transform.Find("ComboCountdown").GetComponent<UnityEngine.UI.Image>();
                effectsStatus=canvas.Find("BossEffectsStatus").GetComponent<TMPro.TMP_Text>();shop=canvas.Find("PowerUpShop").gameObject;resources=shop.transform.Find("ResourceChoices").gameObject;
                shopTitle=shop.transform.Find("ShopTitle").GetComponent<TMPro.TMP_Text>();buttons=new UnityEngine.UI.Button[3];
                for(int i=0;i<3;i++){int choice=i;buttons[i]=shop.transform.Find("Choice"+i).GetComponent<UnityEngine.UI.Button>();buttons[i].onClick.AddListener(()=>Purchase(choice));}
                shop.transform.Find("SkipShop").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(CloseShop);
                foreach(var button in resources.GetComponentsInChildren<UnityEngine.UI.Button>(true))if(System.Enum.TryParse(button.name,out IngredientKind kind)){var ingredient=kind;button.gameObject.SetActive(ActiveLevel.Config.Allows(kind));button.onClick.AddListener(()=>PickResource(ingredient));}
                return;
            }
            status=Label(canvas,"ComboBossStatus",new Vector2(0,-35),new Vector2(240,36));var r=(RectTransform)status.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);status.fontSize=26;status.fontStyle=TMPro.FontStyles.Bold;
            var comboCanvas=status.gameObject.AddComponent<Canvas>();var parentCanvas=canvas.GetComponent<Canvas>();
            comboCanvas.overrideSorting=true;comboCanvas.sortingLayerID=parentCanvas.sortingLayerID;comboCanvas.sortingOrder=parentCanvas.sortingOrder+1;
            var bar=new GameObject("ComboCountdown",typeof(RectTransform),typeof(UnityEngine.UI.Image));bar.transform.SetParent(status.transform,false);r=(RectTransform)bar.transform;r.anchoredPosition=new Vector2(0,-25);r.sizeDelta=new Vector2(150,4);comboBar=bar.GetComponent<UnityEngine.UI.Image>();comboBar.color=GameplayHudFeedback.Ink;comboBar.raycastTarget=false;comboBar.type=UnityEngine.UI.Image.Type.Filled;comboBar.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
            effectsStatus=Label(canvas,"BossEffectsStatus",new Vector2(0,-80),new Vector2(600,28));r=(RectTransform)effectsStatus.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);effectsStatus.fontSize=18;effectsStatus.enableAutoSizing=true;effectsStatus.fontSizeMin=10;effectsStatus.fontSizeMax=18;
            shop=new GameObject("PowerUpShop",typeof(RectTransform),typeof(UnityEngine.UI.Image));shop.transform.SetParent(canvas,false);r=(RectTransform)shop.transform;r.sizeDelta=new Vector2(870,280);shop.GetComponent<UnityEngine.UI.Image>().color=GameUiTheme.Paper;shop.GetComponent<UnityEngine.UI.Image>().sprite=GameplayHudFeedback.Active?.panelSprite;shop.GetComponent<UnityEngine.UI.Image>().type=UnityEngine.UI.Image.Type.Sliced;
            var shopCanvas=shop.AddComponent<Canvas>();shopCanvas.overrideSorting=true;shopCanvas.sortingLayerID=parentCanvas.sortingLayerID;shopCanvas.sortingOrder=parentCanvas.sortingOrder+1;
            shop.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            shopTitle=Label(shop.transform,"ShopTitle",new Vector2(0,100),new Vector2(850,50));buttons=new UnityEngine.UI.Button[3];
            for(int i=0;i<3;i++){int choice=i;buttons[i]=Button(shop.transform,"Choice"+i,new Vector2((i-1)*280,-10),new Vector2(260,160),()=>Purchase(choice));}
            Button(shop.transform,"SkipShop",new Vector2(350,-115),new Vector2(120,35),CloseShop).GetComponentInChildren<TMPro.TMP_Text>().text="SKIP";
            resources=new GameObject("ResourceChoices",typeof(RectTransform));resources.transform.SetParent(shop.transform,false);
            int n=0;foreach(var ingredient in ActiveLevel.Config.activeIngredients)
            {
                var kind=ingredient;var button=Button(resources.transform,kind.ToString(),new Vector2((n%4-1.5f)*200,30-n/4*75),new Vector2(190,65),()=>PickResource(kind));button.GetComponentInChildren<TMPro.TMP_Text>().text=IngredientRules.DisplayName(kind);n++;
            }
            shop.SetActive(false);
        }
        private void EnsureBossUi()
        {
            if(bossAnnouncement!=null)return;
            var canvas=GetComponentInParent<Canvas>().transform;var existing=canvas.Find("AYCEAnnouncement");
            if(existing!=null){bossAnnouncement=existing.gameObject;existing.Find("Begin AYCE").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(BeginBoss);return;}
            bossAnnouncement=new GameObject("AYCEAnnouncement",typeof(RectTransform),typeof(UnityEngine.UI.Image));bossAnnouncement.transform.SetParent(canvas,false);((RectTransform)bossAnnouncement.transform).sizeDelta=new Vector2(760,350);bossAnnouncement.GetComponent<UnityEngine.UI.Image>().color=GameUiTheme.Paper;
            var label=Label(bossAnnouncement.transform,"Orders",new Vector2(0,35),new Vector2(710,250));label.enableAutoSizing=true;label.fontSizeMin=16;label.fontSizeMax=21;
            Button(bossAnnouncement.transform,"Begin AYCE",new Vector2(0,-125),new Vector2(280,55),BeginBoss).GetComponentInChildren<TMPro.TMP_Text>().text="READY — START BOSS";
        }
        private static UnityEngine.UI.Button Button(Transform parent,string name,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchoredPosition=position;r.sizeDelta=size;var image=go.GetComponent<UnityEngine.UI.Image>();image.color=new Color(1,.96f,.88f);image.sprite=Resources.Load<GameUiTheme>("GameUiTheme")?.buttonSprite;image.type=UnityEngine.UI.Image.Type.Sliced;
            var button=go.GetComponent<UnityEngine.UI.Button>();button.onClick.AddListener(action);Label(go.transform,"Description",Vector2.zero,size-new Vector2(12,12));return button;
        }
        private static TMPro.TMP_Text Label(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchoredPosition=position;r.sizeDelta=size;
            var text=go.GetComponent<TMPro.TMP_Text>();text.font=Resources.Load<GameUiTheme>("GameUiTheme")?.font??TMPro.TMP_Settings.defaultFontAsset;text.fontSize=21;text.color=GameplayHudFeedback.Ink;text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;return text;
        }
    }
}
