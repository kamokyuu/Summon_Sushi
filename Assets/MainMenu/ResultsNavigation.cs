using UnityEngine;
using UnityEngine.SceneManagement;
namespace CookingCards.Menu
{
    /// <summary>Result navigation belongs to UI; scoring and served counts remain in CustomerSystem.</summary>
    public sealed class ResultsNavigation : MonoBehaviour
    {
        [SerializeField] private ShiftCompletionReceipt completionReceiptPrefab;
        [SerializeField] private GameObject legacyCompletionPanel;
        private ShiftCompletionReceipt receipt;
        [SerializeField,Min(.05f)] private float buttonAnimationDuration=.25f;
        [SerializeField,Range(.5f,1)] private float buttonStartScale=.92f;
        [SerializeField,Range(1,1.2f)] private float buttonPopScale=1.04f;
        private CanvasGroup navigationGroup;
        private float navigationAlpha=1;
        private bool navigationInteractable=true,navigationRaycasts=true,revealStarted;
        private Transform[] animatedButtons;
        private Vector3[] buttonScales;
        private Coroutine revealRoutine;
        public void Show()
        {
            ShowSuccessReceipt();
            if(transform.Find("ResultsNavigation")!=null){Bind();RefreshNextLabel();RevealWhenReady();return;}
            var root=new GameObject("ResultsNavigation",typeof(RectTransform));root.transform.SetParent(transform,false);
            var r=(RectTransform)root.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(0,-255);r.sizeDelta=new Vector2(1000,60);
            Make(root.transform,"MAIN MENU",-180,"MainMenu");Make(root.transform,"REPLAY",180,"MainScene");
            Bind();
            RefreshNextLabel();
            RevealWhenReady();
        }
        private void Awake(){Bind();HoldNavigation();}
        private void HoldNavigation()
        {
            var root=transform.Find("ResultsNavigation");if(root==null)return;
            if(navigationGroup==null)
            {
                navigationGroup=root.GetComponent<CanvasGroup>();
                if(navigationGroup==null)navigationGroup=root.gameObject.AddComponent<CanvasGroup>();
                navigationAlpha=navigationGroup.alpha;navigationInteractable=navigationGroup.interactable;navigationRaycasts=navigationGroup.blocksRaycasts;
            }
            navigationGroup.alpha=0;navigationGroup.interactable=false;navigationGroup.blocksRaycasts=false;
        }
        private void RevealWhenReady()
        {
            if(revealStarted)return;
            HoldNavigation();if(navigationGroup==null)return;
            if(receipt==null){RestoreNavigation();return;}
            revealStarted=true;revealRoutine=StartCoroutine(RevealButtons());
        }
        private System.Collections.IEnumerator RevealButtons()
        {
            while(receipt!=null&&!receipt.AnimationFinished)yield return null;
            var buttons=navigationGroup.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            animatedButtons=new Transform[buttons.Length];buttonScales=new Vector3[buttons.Length];
            for(int i=0;i<buttons.Length;i++){animatedButtons[i]=buttons[i].transform;buttonScales[i]=animatedButtons[i].localScale;}
            float age=0;
            while(age<buttonAnimationDuration)
            {
                float t=Mathf.Clamp01(age/Mathf.Max(.05f,buttonAnimationDuration));
                float scale=t<.6f?Mathf.Lerp(buttonStartScale,buttonPopScale,Mathf.SmoothStep(0,1,t/.6f)):Mathf.Lerp(buttonPopScale,1,Mathf.SmoothStep(0,1,(t-.6f)/.4f));
                navigationGroup.alpha=navigationAlpha*Mathf.SmoothStep(0,1,Mathf.Clamp01(t*2));
                for(int i=0;i<animatedButtons.Length;i++)if(animatedButtons[i]!=null)animatedButtons[i].localScale=buttonScales[i]*scale;
                age+=Time.unscaledDeltaTime;yield return null;
            }
            RestoreNavigation();revealRoutine=null;
        }
        private void RestoreNavigation()
        {
            if(animatedButtons!=null)for(int i=0;i<animatedButtons.Length;i++)if(animatedButtons[i]!=null)animatedButtons[i].localScale=buttonScales[i];
            if(navigationGroup!=null){navigationGroup.alpha=navigationAlpha;navigationGroup.interactable=navigationInteractable;navigationGroup.blocksRaycasts=navigationRaycasts;}
        }
        private void OnDisable(){if(revealRoutine!=null)StopCoroutine(revealRoutine);revealRoutine=null;RestoreNavigation();revealStarted=false;}
        private void ShowSuccessReceipt()
        {
            var shift=DayShiftController.Active;
            if(shift==null||!shift.HasEnded||!shift.Won)return;
            if(receipt==null)
            {
                receipt=GetComponentInChildren<ShiftCompletionReceipt>(true);
            }
            if(receipt==null)
            {
                var prefab=completionReceiptPrefab!=null?completionReceiptPrefab:Resources.Load<ShiftCompletionReceipt>("ShiftCompletionReceipt");
                var belt=Object.FindFirstObjectByType<ConveyorController>();
                if(prefab==null||belt==null)return;
                receipt=Instantiate(prefab,transform,false);receipt.transform.SetAsFirstSibling();
            }
            receipt.gameObject.SetActive(true);receipt.Present(Object.FindFirstObjectByType<ConveyorController>(),ActiveLevel.Config);
            var legacy=legacyCompletionPanel!=null?legacyCompletionPanel:transform.Find("ResultsNote")?.gameObject;
            if(legacy!=null)legacy.SetActive(false);
        }
        public void GoMainMenu(){CampaignProgress.SaveShift();CampaignProgress.ToMenu(null);}
        private bool CanAdvance=>DayShiftController.Active!=null&&DayShiftController.Active.HasEnded&&DayShiftController.Active.Won&&ActiveLevel.Config.nextLevel!=null;
        private void RefreshNextLabel(){var label=transform.Find("ResultsNavigation/REPLAY").GetComponentInChildren<TMPro.TMP_Text>();if(label!=null)label.text=CampaignProgress.Mode==CampaignProgress.RunMode.Shift?(ActiveLevel.Config.campaignDay==7&&DayShiftController.Active.Won?"WEEK COMPLETE":"SAVE & HUB"):!CanAdvance&&DayShiftController.Active!=null&&DayShiftController.Active.Won&&(CampaignProgress.Mode==CampaignProgress.RunMode.Tutorial||CampaignProgress.Mode==CampaignProgress.RunMode.Practice)?"TUTORIAL COMPLETE":CanAdvance?"NEXT LEVEL":"REPLAY";}
        public void Replay(){if(CampaignProgress.HandleResults())return;if(CanAdvance){var next=ActiveLevel.Config.nextLevel;PreparationSession.BeginBeginnerLevel();ActiveLevel.Select(next,"Next level results");}else PreparationSession.Replay();Time.timeScale=1;SceneManager.LoadScene("MainScene");}
        private void Bind()
        {
            var root=transform.Find("ResultsNavigation");if(root==null)return;
            var menu=root.Find("MAIN MENU").GetComponent<UnityEngine.UI.Button>();
            var replay=root.Find("REPLAY").GetComponent<UnityEngine.UI.Button>();
            menu.onClick.RemoveListener(GoMainMenu);menu.onClick.AddListener(GoMainMenu);
            replay.onClick.RemoveListener(Replay);replay.onClick.AddListener(Replay);
        }
        private static void Make(Transform parent,string title,float x,string scene)
        {
            var go=new GameObject(title,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchoredPosition=new Vector2(x,0);r.sizeDelta=new Vector2(300,60);
            var image=go.GetComponent<UnityEngine.UI.Image>();image.color=GameUiTheme.Paper;image.sprite=Resources.Load<GameUiTheme>("GameUiTheme")?.buttonSprite;image.type=UnityEngine.UI.Image.Type.Sliced;
            var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var label=new GameObject("Label",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(go.transform,false);((RectTransform)label.transform).sizeDelta=r.sizeDelta;
            var text=label.GetComponent<TMPro.TextMeshProUGUI>();text.font=TMPro.TMP_Settings.defaultFontAsset;text.text=title;text.fontSize=24;text.color=GameplayHudFeedback.Ink;text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;
        }
    }
}
