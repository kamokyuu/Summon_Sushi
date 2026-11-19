using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace CookingCards.Menu.Editor
{
    public static class MainMenuSetup
    {
        const string Path = "Assets/Scenes/MainMenu.unity";
        [MenuItem("Tools/Cooking Cards/Create Main Menu")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            if (System.IO.File.Exists(Path)) throw new System.InvalidOperationException("MainMenu already exists; preserve existing scene.");
            var previous = EditorSceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(scene);
            var canvas = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            var controller = canvas.AddComponent<MainMenuController>();
            var bg = Box(canvas.transform, "Background", new Color(.10f, .12f, .17f), Vector2.zero, new Vector2(1280,720));
            bg.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            Label(canvas.transform, "Title", "SUMMON SUSHI", new Vector2(-380,255), new Vector2(420,70), 38);
            Label(canvas.transform, "Subtitle", "COREPANICS / PROTOTYPE", new Vector2(-380,200), new Vector2(420,35), 18);
            var nav = Box(canvas.transform, "Navigation", new Color(0,0,0,0), new Vector2(-400,-15), new Vector2(320,370));
            var layout = nav.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 14; layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var panels = new GameObject[5];
            string[] names = { "LevelSelectPanel", "DiaryPanel", "SettingsPanel", "CreditsPanel", "ExitPanel" };
            for (int i=0;i<panels.Length;i++) panels[i] = Box(canvas.transform,names[i],new Color(.18f,.21f,.29f),new Vector2(200,0),new Vector2(650,550));
            controller.Configure(panels);
            string[] buttons = { "PLAY", "DIARY", "SETTINGS", "CREDITS", "EXIT" };
            for(int i=0;i<buttons.Length;i++)
            {
                var b = Button(nav.transform, buttons[i], Vector2.zero, new Vector2(320,58));
                var le=b.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); le.preferredHeight=58;
                UnityEventTools.AddIntPersistentListener(b.onClick,controller.ShowPanel,i);
            }
            Label(panels[0].transform,"Heading","SELECT LEVEL",new Vector2(0,210),new Vector2(550,50),30);
            Wire(Button(panels[0].transform,"LEVEL 1",Vector2.zero,new Vector2(350,80)),controller.PlayLevelOne);
            Label(panels[1].transform,"Heading","DIARY",new Vector2(0,210),new Vector2(550,50),30);
            Label(panels[1].transform,"Placeholder","Recipe collection and daily recaps\nComing soon",Vector2.zero,new Vector2(550,180),24);
            Label(panels[2].transform,"Heading","SETTINGS",new Vector2(0,210),new Vector2(550,50),30);
            Label(panels[2].transform,"VolumeLabel","MASTER VOLUME",new Vector2(0,130),new Vector2(500,40),22);
            var track=Box(panels[2].transform,"MasterVolume",new Color(.3f,.34f,.42f),new Vector2(0,75),new Vector2(430,28));
            var slider=track.AddComponent<UnityEngine.UI.Slider>();
            var handle=Box(track.transform,"Handle",Color.white,Vector2.zero,new Vector2(24,40));
            slider.handleRect=(RectTransform)handle.transform; slider.targetGraphic=handle.GetComponent<UnityEngine.UI.Image>();
            slider.minValue=0; slider.maxValue=1; slider.value=GameSettings.Volume;
            UnityEventTools.AddPersistentListener(slider.onValueChanged,controller.SetVolume);
            var toggleObject=Box(panels[2].transform,"Fullscreen",new Color(.3f,.34f,.42f),new Vector2(0,-20),new Vector2(430,55));
            var toggle=toggleObject.AddComponent<UnityEngine.UI.Toggle>(); toggle.targetGraphic=toggleObject.GetComponent<UnityEngine.UI.Image>();
            var check=Box(toggleObject.transform,"Check",new Color(.55f,.85f,.65f),new Vector2(-180,0),new Vector2(25,25)); toggle.graphic=check.GetComponent<UnityEngine.UI.Image>();
            Label(toggleObject.transform,"Label","FULLSCREEN",new Vector2(20,0),new Vector2(320,50),22);
            toggle.isOn=GameSettings.Fullscreen; UnityEventTools.AddPersistentListener(toggle.onValueChanged,controller.SetFullscreen);
            Label(panels[3].transform,"Heading","CREDITS",new Vector2(0,210),new Vector2(550,50),30);
            Label(panels[3].transform,"Credits","artist : nindiy\n\ngame design : nindiy dan jarvis\n\nprogrammer : codex",Vector2.zero,new Vector2(580,300),24);
            Label(panels[4].transform,"Heading","EXIT GAME?",new Vector2(0,100),new Vector2(550,60),30);
            Wire(Button(panels[4].transform,"EXIT",Vector2.zero,new Vector2(320,60)),controller.Exit);
            for(int i=0;i<panels.Length;i++)
            {
                var back=Button(panels[i].transform,"BACK",new Vector2(0,-220),new Vector2(250,50));
                UnityEventTools.AddIntPersistentListener(back.onClick,controller.ShowPanel,-1);
                panels[i].SetActive(false);
            }
            new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            EditorSceneManager.SaveScene(scene,Path);
            var list=new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(Path,true),new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity",true) };
            foreach(var item in EditorBuildSettings.scenes) if(item.path!=Path && item.path!="Assets/Scenes/MainScene.unity") list.Add(item);
            EditorBuildSettings.scenes=list.ToArray();
            EditorSceneManager.CloseScene(scene,true);
            EditorSceneManager.SetActiveScene(previous);
            Debug.Log("Main Menu created and added first in Build Settings.");
        }
        static GameObject Box(Transform parent,string name,Color color,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)); go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform; r.anchorMin=r.anchorMax=new Vector2(.5f,.5f); r.sizeDelta=size; r.anchoredPosition=position;
            go.GetComponent<UnityEngine.UI.Image>().color=color; return go;
        }
        static void Label(Transform parent,string name,string text,Vector2 position,Vector2 size,int font)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform; r.sizeDelta=size; r.anchoredPosition=position;
            var t=go.GetComponent<TextMeshProUGUI>(); t.text=text; t.font=TMP_Settings.defaultFontAsset; t.fontSize=font; t.alignment=TextAlignmentOptions.Center; t.color=Color.white; t.raycastTarget=false;
        }
        static UnityEngine.UI.Button Button(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var go=Box(parent,name,new Color(.30f,.36f,.48f),position,size);
            var b=go.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic=go.GetComponent<UnityEngine.UI.Image>();
            Label(go.transform,"Label",name,Vector2.zero,size,23); return b;
        }
        static void Wire(UnityEngine.UI.Button button,UnityAction action) => UnityEventTools.AddPersistentListener(button.onClick,action);
    }
}
