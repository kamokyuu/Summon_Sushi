using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
namespace CookingCards.Menu.Editor
{
    public static class CampaignFlowSetup
    {
        [MenuItem("Tools/Cooking Cards/Set Up Final Campaign Flow")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Edit mode only.");
            string path="Assets/Resources/Levels/Day01.asset";
            if(AssetDatabase.LoadAssetAtPath<LevelConfig>(path)==null)
            {
                var config=Object.Instantiate(Resources.Load<LevelConfig>("Levels/Level03"));config.name="Day01";config.campaignShift=true;config.guidedSteps=new LevelGuideStep[0];config.nextLevel=null;config.durationSeconds=150;config.targetYen=1200;config.finishOnTarget=false;config.firstWaveSeconds=70;config.bossArrivalSeconds=90;config.minimumShiftSeconds=120;config.bossTimeLimit=60;config.bossTitle="Grand Opening — AYCE Guest";config.ayceOrders=new[]{new BossPlateOrder{grade=RecipeGrade.Common,quantity=2},new BossPlateOrder{grade=RecipeGrade.Uncommon,quantity=1}};config.InvalidateRecipes();AssetDatabase.CreateAsset(config,path);AssetDatabase.SaveAssets();
            }
            var previous=EditorSceneManager.GetActiveScene();var menu=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");bool opened=!menu.isLoaded;if(opened)menu=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity",OpenSceneMode.Additive);EditorSceneManager.SetActiveScene(menu);
            var controller=menu.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MainMenuController>(true)).First();var canvas=controller.transform;var navigation=canvas.Find("Navigation");
            string[] titles={"NEW GAME","CONTINUE","TUTORIAL","DIARY","SETTINGS"};UnityAction[] actions={controller.NewGame,controller.ContinueGame,controller.ReplayTutorial,controller.Diary,controller.Settings};
            var buttons=navigation.GetComponentsInChildren<UnityEngine.UI.Button>(true);for(int i=0;i<5;i++){buttons[i].name=titles[i];buttons[i].GetComponentInChildren<TMP_Text>(true).text=titles[i];Bind(buttons[i],actions[i]);}
            var hub=Panel(canvas,"RestaurantHub");var hubText=Text(hub.transform,"HubDetails","Restaurant Hub",new Vector2(0,60),new Vector2(900,430),22);hubText.enableAutoSizing=true;hubText.fontSizeMin=16;hubText.fontSizeMax=22;var start=Button(hub.transform,"StartShift","START DAY01 SHIFT",new Vector2(180,-235),controller.StartShift);Button(hub.transform,"Back","MAIN MENU",new Vector2(-180,-235),controller.Diary);
            var hubBack=hub.transform.Find("Back").GetComponent<UnityEngine.UI.Button>();Clear(hubBack);UnityEventTools.AddIntPersistentListener(hubBack.onClick,controller.ShowPanel,-1);
            var complete=Panel(canvas,"TutorialComplete");var completion=Text(complete.transform,"Introduction","Tutorial Complete",new Vector2(0,60),new Vector2(850,330),26);Button(complete.transform,"Continue","CONTINUE",new Vector2(0,-190),controller.LeaveTutorialComplete);
            var diary=canvas.GetComponentsInChildren<Transform>(true).First(t=>t.name=="DiaryPanel");foreach(var old in diary.GetComponentsInChildren<TMP_Text>(true))if(old.transform.parent==diary&&old.name!="Heading"&&old.name!="CampaignDiary")old.gameObject.SetActive(false);
            var diaryText=Text(diary,"CampaignDiary","Restaurant diary",Vector2.zero,new Vector2(550,330),23);
            controller.ConfigureCampaign(hub,complete,hubText,completion,diaryText,buttons[1],start);hub.SetActive(false);complete.SetActive(false);EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(menu);EditorSceneManager.SaveScene(menu);if(opened)EditorSceneManager.CloseScene(menu,true);if(previous.IsValid()&&previous.isLoaded)EditorSceneManager.SetActiveScene(previous);
        }
        private static void Clear(UnityEngine.UI.Button button){for(int i=button.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEventTools.RemovePersistentListener(button.onClick,i);}
        private static void Bind(UnityEngine.UI.Button button,UnityAction action){Clear(button);UnityEventTools.AddPersistentListener(button.onClick,action);}
        private static GameObject Panel(Transform parent,string name)
        {
            var old=parent.Find(name);if(old!=null)return old.gameObject;var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);((RectTransform)go.transform).sizeDelta=new Vector2(1000,620);var image=go.GetComponent<UnityEngine.UI.Image>();image.color=GameUiTheme.Paper;image.sprite=Resources.Load<GameUiTheme>("GameUiTheme")?.buttonSprite;image.type=UnityEngine.UI.Image.Type.Sliced;return go;
        }
        private static TMP_Text Text(Transform parent,string name,string value,Vector2 position,Vector2 size,int fontSize)
        {
            var existing=parent.Find(name);var go=existing!=null?existing.gameObject:new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.anchoredPosition=position;rect.sizeDelta=size;var text=go.GetComponent<TMP_Text>();text.font=Resources.Load<GameUiTheme>("GameUiTheme")?.font??TMP_Settings.defaultFontAsset;text.text=value;text.fontSize=fontSize;text.color=GameUiTheme.Ink;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
        }
        private static UnityEngine.UI.Button Button(Transform parent,string name,string title,Vector2 position,UnityAction action)
        {
            var existing=parent.Find(name);var go=existing!=null?existing.gameObject:new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(parent,false);((RectTransform)go.transform).anchoredPosition=position;((RectTransform)go.transform).sizeDelta=new Vector2(320,55);var image=go.GetComponent<UnityEngine.UI.Image>();image.color=Color.white;image.sprite=Resources.Load<GameUiTheme>("GameUiTheme")?.buttonSprite;image.type=UnityEngine.UI.Image.Type.Sliced;var button=go.GetComponent<UnityEngine.UI.Button>();Bind(button,action);Text(go.transform,"Label",title,Vector2.zero,new Vector2(305,50),22);return button;
        }
    }
}
