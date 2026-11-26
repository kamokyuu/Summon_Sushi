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
        public float Elapsed=>Mathf.Max(0,durationSeconds-Remaining);
        public int CurrentWave=>Elapsed<ActiveLevel.Config.firstWaveSeconds?1:2;
        public string WaveObjective=>CurrentWave==1?"WAVE 1 — welcome guests and build income":"WAVE 2 — reach the income target and complete AYCE";
        public bool HasEnded {get;private set;}
        public bool Won=>belt!=null && belt.Revenue-belt.Penalties>=targetYen&&(!ActiveLevel.Config.bossCustomersEnabled||ComboShopBoss.Active!=null&&ComboShopBoss.Active.BossComplete);
        public int TargetYen=>targetYen;
        private void Awake(){Active=this;Remaining=durationSeconds;}
        private void OnDestroy(){if(Active==this)Active=null;}
        public void Configure(GameObject panel,TMPro.TMP_Text text)
        {
            resultPanel=panel;resultText=text;belt=Object.FindFirstObjectByType<ConveyorController>();hand=Object.FindFirstObjectByType<IngredientCardHand>();
            if(!ActiveLevel.Legacy){durationSeconds=ActiveLevel.Config.durationSeconds;targetYen=ActiveLevel.Config.targetYen;Remaining=durationSeconds;}
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
        private void Update(){Advance(Time.deltaTime);Refresh();}
        public void Advance(float seconds)
        {
            if(HasEnded || GuidedLevelTutorial.Protecting || ComboShopBoss.EncounterPaused || Menu.GameplayPauseController.IsPaused || hand==null || hand.IsDealing || belt==null)return;
            var feature=ComboShopBoss.Active;
            feature?.CheckBoss(ActiveLevel.Config.campaignShift?Elapsed>=ActiveLevel.Config.bossArrivalSeconds:belt.Revenue-belt.Penalties>=targetYen);
            if(feature!=null&&feature.BossAnnouncing)return;
            if(feature!=null&&feature.BossFailed){Finish();return;}
            if((ActiveLevel.Config.finishOnTarget||ActiveLevel.Config.campaignShift&&Elapsed>=ActiveLevel.Config.minimumShiftSeconds)&&Won){Finish();return;}
            if(!ActiveLevel.Config.campaignShift&&feature!=null&&feature.BossStarted&&!feature.BossComplete)return;
            Remaining=Mathf.Max(0,Remaining-Mathf.Max(0,seconds));Refresh();if(Remaining<=0)Finish();
        }
        private void Refresh()
        {
            if(hud!=null && belt!=null)hud.text=LevelTitle+"   "+(Mathf.CeilToInt(Remaining)/60).ToString("00")+":"+(Mathf.CeilToInt(Remaining)%60).ToString("00")+"\n¥"+Mathf.RoundToInt(GameplayHudFeedback.Active!=null?GameplayHudFeedback.Active.DisplayedRevenue:belt.Revenue-belt.Penalties)+" / ¥"+targetYen;
        }
        private string LevelTitle=>ActiveLevel.Config.campaignShift?"DAY"+ActiveLevel.Config.campaignDay.ToString("00")+" W"+CurrentWave:ActiveLevel.Legacy?"DAY 1":ActiveLevel.Config.name.ToUpperInvariant();
        private void Finish()
        {
            HasEnded=true;
            GameMusic.StopForCompletion();
            if(Won)Menu.UIAudioManager.PlayShiftDone();
            if(Won)Menu.CampaignProgress.SaveShift();
            if(resultText!=null)resultText.text="<size=36><b>"+LevelTitle+" "+(Won?"COMPLETE":"FAILED")+"</b></size>\n\nSales: ¥"+belt.Revenue+"\nBroken plate penalties: -¥"+belt.Penalties+"\nNet: ¥"+(belt.Revenue-belt.Penalties)+" / ¥"+targetYen+"\n\nSushi sold: "+belt.PurchasedCount+"\nBroken plates: "+belt.MissedCount;
            if(resultText!=null&&ComboShopBoss.Active!=null){resultText.text+=ComboShopBoss.Active.Results();resultText.enableAutoSizing=true;resultText.fontSizeMin=12;resultText.fontSizeMax=25;}
            if(resultText!=null&&Won&&ActiveLevel.Config.campaignShift&&ActiveLevel.Config.campaignDay==7)resultText.text="<b>WEEK01 COMPLETE — CHAPTER COMPLETE</b>\n"+resultText.text;
            if(resultPanel!=null){resultPanel.SetActive(true);resultPanel.transform.SetAsLastSibling();var nav=resultPanel.GetComponent<Menu.ResultsNavigation>()??resultPanel.AddComponent<Menu.ResultsNavigation>();nav.Show();}
        }
    }
}

