using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CookingCards.Menu
{
    public static class CampaignProgress
    {
        [Serializable]public sealed class SaveData {public int version=1,currentDay=1,tutorialLevel=1,savings;public bool tutorialComplete,weekComplete;public string lastShift="No shifts completed yet.";}
        public enum RunMode { None,Tutorial,Practice,Shift }
        public static RunMode Mode {get;private set;}
        public static string PendingView {get;set;}
        public static bool PracticeCompletion {get;private set;}
        private static bool shiftSaved;
        private const string Key="SummonSushi.Campaign.v1";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void Reset(){Mode=RunMode.None;PendingView=null;PracticeCompletion=false;shiftSaved=false;}
        public static bool HasSave=>Read()!=null;
        public static SaveData Read(){try{if(!PlayerPrefs.HasKey(Key))return null;var data=JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(Key));if(data==null||data.version!=1)return null;data.currentDay=Mathf.Max(1,data.currentDay);data.tutorialLevel=Mathf.Clamp(data.tutorialLevel,1,3);data.savings=Mathf.Max(0,data.savings);return data;}catch(Exception){return null;}}
        private static void Write(SaveData data){PlayerPrefs.SetString(Key,JsonUtility.ToJson(data));PlayerPrefs.Save();}
        public static void NewGame(){Write(new SaveData());BeginTutorial(false,1);}
        public static void Continue(){var data=Read();if(data==null)return;if(!data.tutorialComplete)BeginTutorial(false,data.tutorialLevel);else ToMenu("Hub");}
        public static void BeginTutorial(bool practice,int level=1)
        {
            Mode=practice?RunMode.Practice:RunMode.Tutorial;PreparationSession.BeginBeginnerLevel();ActiveLevel.Select(Resources.Load<LevelConfig>("Levels/Level0"+level),practice?"Tutorial replay":"Campaign tutorial");Time.timeScale=1;SceneManager.LoadScene("MainScene");
        }
        public static bool HandleResults()
        {
            var day=DayShiftController.Active;if(day==null||!day.HasEnded)return false;
            if(Mode==RunMode.Shift){bool finale=day.Won&&ActiveLevel.Config.campaignDay==7;SaveShift();ToMenu(finale?"WeekComplete":"Hub");return true;}
            if(Mode!=RunMode.Tutorial&&Mode!=RunMode.Practice||!day.Won)return false;
            bool practice=Mode==RunMode.Practice;var next=ActiveLevel.Config.nextLevel;
            if(next!=null)
            {
                if(!practice){var data=Read()??new SaveData();data.tutorialLevel=Mathf.Clamp(data.tutorialLevel+1,1,3);Write(data);}
                PreparationSession.BeginBeginnerLevel();ActiveLevel.Select(next,"Next tutorial");Time.timeScale=1;SceneManager.LoadScene("MainScene");return true;
            }
            PracticeCompletion=practice;if(!practice){var data=Read()??new SaveData();data.tutorialComplete=true;data.tutorialLevel=3;Write(data);}ToMenu("TutorialComplete");return true;
        }
        public static void StartShift()
        {
            var data=Read();if(data==null||!data.tutorialComplete||data.weekComplete||data.currentDay>7)return;
            var config=Resources.Load<LevelConfig>("Levels/Day"+data.currentDay.ToString("00"));if(config==null)return;
            PreparationSession.BeginBeginnerLevel();ActiveLevel.Select(config,"Restaurant Day"+data.currentDay.ToString("00"));Mode=RunMode.Shift;shiftSaved=false;Time.timeScale=1;SceneManager.LoadScene("MainScene");
        }
        public static void SaveShift()
        {
            if(Mode!=RunMode.Shift||shiftSaved||DayShiftController.Active==null||!DayShiftController.Active.HasEnded)return;
            var data=Read()??new SaveData();var day=DayShiftController.Active;var config=ActiveLevel.Config;data.savings=Mathf.Max(0,data.savings+PreparationSession.Yen);data.lastShift="Day"+config.campaignDay.ToString("00")+" — "+config.dailyEventTitle+"\n"+(day.Won?"SUCCESS":"FAILED — retry from the hub")+"\nRestaurant balance change: ¥"+PreparationSession.Yen+"\nSavings: ¥"+data.savings;
            if(day.Won){data.currentDay=Mathf.Max(data.currentDay,config.campaignDay+1);if(config.campaignDay==7){data.weekComplete=true;data.lastShift+="\nWEEK01 COMPLETE";}}Write(data);shiftSaved=true;
        }
        public static void ToMenu(string view){Mode=RunMode.None;PendingView=view;Time.timeScale=1;SceneManager.LoadScene("MainMenu");}
    }
}
