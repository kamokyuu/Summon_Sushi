using UnityEngine;
using UnityEngine.SceneManagement;

namespace CookingCards.Menu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject[] panels;
        [SerializeField] private string gameplayScene = "MainScene";
        [SerializeField]private GameObject hubPanel,tutorialCompletePanel;
        [SerializeField]private TMPro.TMP_Text hubText,completionText,diaryText;
        [SerializeField]private UnityEngine.UI.Button continueButton,startShiftButton;
        private bool weekScreen;
        public void ConfigureCampaign(GameObject hub,GameObject complete,TMPro.TMP_Text hubLabel,TMPro.TMP_Text completeLabel,TMPro.TMP_Text diary,UnityEngine.UI.Button resume,UnityEngine.UI.Button start){hubPanel=hub;tutorialCompletePanel=complete;hubText=hubLabel;completionText=completeLabel;diaryText=diary;continueButton=resume;startShiftButton=start;}
        public void Configure(GameObject[] views) => panels = views;
        private void Start() { GameSettings.Apply(); ShowPanel(-1);var view=CampaignProgress.PendingView;CampaignProgress.PendingView=null;if(view=="Hub")ShowHub();else if(view=="TutorialComplete")ShowTutorialComplete();else if(view=="WeekComplete")ShowWeekComplete(); }
        public void ShowPanel(int index)
        {
            weekScreen=false;
            if(hubPanel!=null)hubPanel.SetActive(false);if(tutorialCompletePanel!=null)tutorialCompletePanel.SetActive(false);SetNavigation(true);
            if(continueButton!=null)continueButton.interactable=CampaignProgress.HasSave;
            if(index==1&&diaryText!=null){var data=CampaignProgress.Read();diaryText.text=data==null?"Start a New Game to begin your restaurant diary.":data.lastShift+"\n\nTutorial: "+(data.tutorialComplete?"Complete":"Lesson "+data.tutorialLevel)+"\nCurrent day: "+data.currentDay;}
            if (index == 0) { PlayLevelOne(); return; }
            for (int i = 0; i < panels.Length; i++) panels[i].SetActive(i == index);
        }
        public void PlayLevelOne()=>CampaignProgress.NewGame();
        public void NewGame()=>CampaignProgress.NewGame();
        public void ContinueGame()=>CampaignProgress.Continue();
        public void ReplayTutorial()=>CampaignProgress.BeginTutorial(true);
        public void Diary()=>ShowPanel(1);
        public void Settings()=>ShowPanel(2);
        public void StartShift()=>CampaignProgress.StartShift();
        private void SetNavigation(bool active){foreach(var name in new[]{"Navigation","Title","Subtitle"}){var t=transform.Find(name);if(t!=null)t.gameObject.SetActive(active);}}
        public void ShowHub()
        {
            ShowPanel(-1);SetNavigation(false);hubPanel.SetActive(true);var data=CampaignProgress.Read()??new CampaignProgress.SaveData();
            var calendar=new System.Text.StringBuilder();for(int day=1;day<=7;day++){var entry=Resources.Load<LevelConfig>("Levels/Day"+day.ToString("00"));calendar.Append("Day").Append(day.ToString("00")).Append(": ").Append(entry!=null?entry.dailyEventTitle:"Unavailable").Append(day<data.currentDay?" [COMPLETE]":day==data.currentDay?" [TODAY]":" [LOCKED]").Append('\n');}
            hubText.text="RESTAURANT HUB — WEEK01\nSavings: ¥"+data.savings+"\n\n"+calendar+"\n"+(data.weekComplete||data.currentDay>7?"WEEK COMPLETE — CHAPTER COMPLETE\nAll seven daily events completed.":DayOneObjectives());
            startShiftButton.interactable=data.tutorialComplete&&!data.weekComplete&&data.currentDay<=7&&Resources.Load<LevelConfig>("Levels/Day"+data.currentDay.ToString("00"))!=null;startShiftButton.GetComponentInChildren<TMPro.TMP_Text>().text=data.currentDay>7?"WEEK COMPLETE":"START DAY"+data.currentDay.ToString("00")+" SHIFT";
        }
        public void ShowTutorialComplete(){ShowPanel(-1);SetNavigation(false);tutorialCompletePanel.SetActive(true);completionText.text=CampaignProgress.PracticeCompletion?"TUTORIAL REPLAY COMPLETE\n\nYour campaign progress has not changed.":"TUTORIAL COMPLETE\n\nYou are ready to manage your restaurant!\nCheck the calendar, savings and daily objectives.\nStart Day01: Grand Opening from your restaurant hub.";}
        public void ShowWeekComplete(){ShowPanel(-1);SetNavigation(false);weekScreen=true;tutorialCompletePanel.SetActive(true);var data=CampaignProgress.Read();completionText.text="WEEK01 COMPLETE\nCHAPTER COMPLETE\n\nSeven daily events completed!\nRestaurant savings: ¥"+(data?.savings??0)+"\n\nProgress saved. Return to your restaurant calendar.";}
        public void LeaveTutorialComplete(){if(weekScreen)ShowHub();else if(CampaignProgress.PracticeCompletion)ShowPanel(-1);else ShowHub();}
        private string DayOneObjectives()
        {
            var data=CampaignProgress.Read();var level=Resources.Load<LevelConfig>("Levels/Day"+(data?.currentDay??1).ToString("00"));if(level==null)return "Daily configuration is missing.";
            var orders=new System.Collections.Generic.List<string>();if(level.bossKind==CampaignBossKind.SushiPlatter){foreach(var order in level.platterOrders){string title=order.recipeId;foreach(var r in level.Recipes)if(r.Id==order.recipeId)title=r.DisplayName;orders.Add(order.quantity+" "+title);}}else foreach(var order in level.ayceOrders)orders.Add(order.quantity+" "+order.grade);
            return level.dailyEventTitle+" — "+level.dailyEventDescription+"\nWave 1: service ("+level.firstWaveSeconds+"s); Wave 2: ¥"+level.targetYen+" + boss.\n"+level.bossKind+": "+string.Join(" + ",orders)+"\nBoss timer: "+level.bossTimeLimit+"s; minimum shift: "+level.minimumShiftSeconds+"s.";
        }
        public void SetVolume(float value) => GameSettings.Volume = value;
        public void SetFullscreen(bool value) => GameSettings.Fullscreen = value;
        private void OnDisable() => GameSettings.Save();
        public void Exit()
        {
            GameSettings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
