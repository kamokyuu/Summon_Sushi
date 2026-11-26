using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CookingCards.Menu.Editor
{
    public static class StudioSplashSetup
    {
        [MenuItem("Tools/Cooking Cards/Add Studio Splash")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Edit mode only.");
            const string path="Assets/Scenes/StudioSplash.unity";
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null)
            {
                var logo=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Asset/Corepanics.png");if(logo==null)throw new System.InvalidOperationException("Missing Assets/Asset/Corepanics.png.");
                var previous=EditorSceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);EditorSceneManager.SetActiveScene(scene);
                var camera=new GameObject("SplashCamera",typeof(Camera));camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;camera.GetComponent<Camera>().backgroundColor=new Color(.025f,.025f,.035f,1);camera.GetComponent<Camera>().cullingMask=0;
                var root=new GameObject("StudioSplashCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(StudioSplash));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
                var backdrop=new GameObject("DarkBackground",typeof(RectTransform),typeof(Image));backdrop.transform.SetParent(root.transform,false);var rect=(RectTransform)backdrop.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;backdrop.GetComponent<Image>().raycastTarget=false;
                var container=new GameObject("LogoBounds",typeof(RectTransform),typeof(CanvasGroup));container.transform.SetParent(root.transform,false);((RectTransform)container.transform).sizeDelta=new Vector2(800,420);var group=container.GetComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
                var visual=new GameObject("CorepanicsLogo",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));visual.transform.SetParent(container.transform,false);visual.GetComponent<RawImage>().raycastTarget=false;
                root.GetComponent<StudioSplash>().Configure(logo,visual.GetComponent<RawImage>(),visual.GetComponent<AspectRatioFitter>(),group,backdrop.GetComponent<Image>());
                EditorSceneManager.SaveScene(scene,path);EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)EditorSceneManager.SetActiveScene(previous);
            }
            var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=path).ToList();scenes.Insert(0,new EditorBuildSettingsScene(path,true));EditorBuildSettings.scenes=scenes.ToArray();
        }
    }
}
