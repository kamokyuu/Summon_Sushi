using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CookingCards.Menu.Editor
{
    public static class MenuAnimationSetup
    {
        [MenuItem("Tools/Cooking Cards/Add Cozy Menu Animation")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Edit mode only.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/MainMenu.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity",OpenSceneMode.Additive);
            var canvas=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).First(c=>c.isRootCanvas);
            RectTransform Find(string name)=>canvas.GetComponentsInChildren<RectTransform>(true).First(r=>r.name==name);
            var customer=Find("Meja (1)");if(customer.GetComponent<MenuCustomerWalk>()==null)customer.gameObject.AddComponent<MenuCustomerWalk>();
            var logo=Find("Logo");if(logo.GetComponent<MenuHoverMotion>()==null){var motion=logo.gameObject.AddComponent<MenuHoverMotion>();motion.Configure(true);EditorUtility.SetDirty(motion);}logo.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            foreach(var button in canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true))if(button.GetComponent<MenuHoverMotion>()==null){var hover=button.gameObject.AddComponent<MenuHoverMotion>();hover.Configure(false);EditorUtility.SetDirty(hover);}
            if(canvas.GetComponent<MenuEnvironmentDrift>()==null)
            {
                var drift=canvas.gameObject.AddComponent<MenuEnvironmentDrift>();drift.Configure(new[]{
                    new MenuEnvironmentDrift.Layer{target=Find("Background"),amplitudePixels=new Vector2(4,4),protectFullBackground=true},
                    new MenuEnvironmentDrift.Layer{target=Find("Meja"),amplitudePixels=new Vector2(5,2),phase=.3f},
                    new MenuEnvironmentDrift.Layer{target=Find("Conveyor"),amplitudePixels=new Vector2(6,2),phase=.6f},
                    new MenuEnvironmentDrift.Layer{target=Find("Piring"),amplitudePixels=new Vector2(6,2),phase=.6f}});EditorUtility.SetDirty(drift);
            }
            if(!scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).Any(c=>c.enabled&&c.gameObject.activeInHierarchy))
            {
                var camera=canvas.GetComponent<Camera>()??canvas.gameObject.AddComponent<Camera>();camera.enabled=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=0;camera.depth=-100;camera.targetDisplay=canvas.targetDisplay;EditorUtility.SetDirty(camera);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
        }
    }
}
