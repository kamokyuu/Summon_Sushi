using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

namespace CookingCards.Menu.Editor
{
    public static class LevelMapSetup
    {
        [MenuItem("Tools/Cooking Cards/Create Level Map")]
        public static void Build()
        {
            const string path = "Assets/Scenes/LevelMap.unity";
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            if (System.IO.File.Exists(path)) throw new System.InvalidOperationException("LevelMap already exists.");
            var previous = EditorSceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(scene);
            var canvas = new GameObject("LevelMapCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
            var controller = canvas.AddComponent<LevelMapController>();
            Box(canvas.transform,"MapGround",Vector2.zero,new Vector2(1280,720),new Color(.13f,.22f,.22f));
            Box(canvas.transform,"WalkableArea",new Vector2(0,-25),new Vector2(1130,470),new Color(.23f,.34f,.29f));
            Box(canvas.transform,"Path",new Vector2(-180,-70),new Vector2(500,80),new Color(.55f,.49f,.35f));
            var node=Box(canvas.transform,"TutorialNode",new Vector2(50,-70),new Vector2(100,100),new Color(.95f,.74f,.35f));
            Text(node,"NodeLabel","1",Vector2.zero,new Vector2(90,90),35);
            Text(canvas.transform,"TutorialLabel","TUTORIAL",new Vector2(50,15),new Vector2(240,45),24);
            var player=Box(canvas.transform,"Player",new Vector2(-430,-70),new Vector2(45,55),new Color(.62f,.73f,1));
            Text(player,"Face",":)",Vector2.zero,new Vector2(45,55),24);
            Text(canvas.transform,"Title","SUMMON SUSHI / LEVEL MAP",new Vector2(0,290),new Vector2(1000,65),32);
            var hint=Text(canvas.transform,"InteractionHint","",new Vector2(0,-285),new Vector2(700,80),22);
            var enter=Button(canvas.transform,"ENTER TUTORIAL",new Vector2(410,-285),new Vector2(290,55));
            UnityEventTools.AddPersistentListener(enter.onClick,controller.EnterTutorial);
            enter.interactable=false;
            var back=Button(canvas.transform,"BACK",new Vector2(-480,290),new Vector2(150,50));
            UnityEventTools.AddPersistentListener(back.onClick,controller.Back);
            controller.Configure(player,node,enter,hint);
            new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            EditorSceneManager.SaveScene(scene,path);
            var scenes=new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s=>s.path==path); scenes.Insert(Mathf.Min(1,scenes.Count),new EditorBuildSettingsScene(path,true));
            EditorBuildSettings.scenes=scenes.ToArray(); AssetDatabase.SaveAssets();
            EditorSceneManager.CloseScene(scene,true); EditorSceneManager.SetActiveScene(previous);
        }
        static RectTransform Box(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)); go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform; r.sizeDelta=size; r.anchoredPosition=pos;
            var image=go.GetComponent<UnityEngine.UI.Image>(); image.color=color; image.raycastTarget=false; return r;
        }
        static TMP_Text Text(Transform parent,string name,string content,Vector2 pos,Vector2 size,int font)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform; r.sizeDelta=size; r.anchoredPosition=pos;
            var t=go.GetComponent<TextMeshProUGUI>(); t.text=content; t.font=TMP_Settings.defaultFontAsset; t.fontSize=font; t.color=Color.white; t.alignment=TextAlignmentOptions.Center; t.raycastTarget=false; return t;
        }
        static UnityEngine.UI.Button Button(Transform parent,string name,Vector2 pos,Vector2 size)
        {
            var r=Box(parent,name,pos,size,new Color(.25f,.31f,.42f)); var image=r.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget=true;
            var b=r.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic=image;
            Text(r,"Label",name,Vector2.zero,size,20); return b;
        }
    }
}
