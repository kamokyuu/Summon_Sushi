using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CookingCards.Menu.Editor
{
    public static class GameplayPauseSetup
    {
        [MenuItem("Tools/Cooking Cards/Create Gameplay Pause")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            if(Object.FindFirstObjectByType<GameplayPauseController>()!=null) return;
            var canvas=Object.FindFirstObjectByType<IngredientCardHand>().GetComponentInParent<Canvas>();
            var root=new GameObject("PauseOverlay",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.GraphicRaycaster));
            root.transform.SetParent(canvas.transform,false);
            var rect=(RectTransform)root.transform; rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.sizeDelta=Vector2.zero;
            root.GetComponent<Canvas>().overrideSorting=true; root.GetComponent<Canvas>().sortingOrder=200;
            var controller=root.AddComponent<GameplayPauseController>();
            var pause=Panel(root.transform,"PausePanel"); var settings=Panel(root.transform,"SettingsPanel");
            controller.Configure(pause,settings);
            Button(pause.transform,"CONTINUE",120,controller.Continue);
            Button(pause.transform,"SETTINGS",40,controller.Settings);
            Button(pause.transform,"BACK TO MAIN MENU",-40,controller.MainMenu);
            Button(pause.transform,"EXIT",-120,controller.Exit);
            var main=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenes/MainMenu.unity");
            // Copy the existing settings controls so both menus share identical preferences.
            var menuScene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity",OpenSceneMode.Additive);
            GameObject source=null;
            foreach(var go in menuScene.GetRootGameObjects()) foreach(var t in go.GetComponentsInChildren<Transform>(true)) if(t.name=="SettingsPanel") source=t.gameObject;
            if(source!=null)
            {
                var content=Object.Instantiate(source,settings.transform,false); content.name="SettingsContent";
                ((RectTransform)content.transform).anchoredPosition=Vector2.zero; content.SetActive(true);
                var slider=content.GetComponentInChildren<UnityEngine.UI.Slider>(true);
                slider.onValueChanged=new UnityEngine.UI.Slider.SliderEvent(); UnityEventTools.AddPersistentListener(slider.onValueChanged,controller.SetVolume);
                slider.value=GameSettings.Volume;
                var toggle=content.GetComponentInChildren<UnityEngine.UI.Toggle>(true);
                toggle.onValueChanged=new UnityEngine.UI.Toggle.ToggleEvent(); UnityEventTools.AddPersistentListener(toggle.onValueChanged,controller.SetFullscreen); toggle.isOn=GameSettings.Fullscreen;
                foreach(var b in content.GetComponentsInChildren<UnityEngine.UI.Button>(true)) { b.onClick=new UnityEngine.UI.Button.ButtonClickedEvent(); UnityEventTools.AddPersistentListener(b.onClick,controller.ShowPause); }
            }
            EditorSceneManager.CloseScene(menuScene,true);
            pause.SetActive(false); settings.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        static GameObject Panel(Transform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)); go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform; r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;
            go.GetComponent<UnityEngine.UI.Image>().color=new Color(.05f,.07f,.12f,.94f); return go;
        }
        static void Button(Transform parent,string title,float y,UnityEngine.Events.UnityAction action)
        {
            var go=new GameObject(title,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.sizeDelta=new Vector2(360,60);r.anchoredPosition=new Vector2(0,y);
            go.GetComponent<UnityEngine.UI.Image>().color=new Color(.25f,.32f,.45f);
            var b=go.GetComponent<UnityEngine.UI.Button>();b.targetGraphic=go.GetComponent<UnityEngine.UI.Image>();UnityEventTools.AddPersistentListener(b.onClick,action);
            var label=new GameObject("Label",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(go.transform,false);
            var tr=(RectTransform)label.transform;tr.sizeDelta=r.sizeDelta;
            var t=label.GetComponent<TMPro.TextMeshProUGUI>();t.text=title;t.font=TMPro.TMP_Settings.defaultFontAsset;t.fontSize=24;t.alignment=TMPro.TextAlignmentOptions.Center;t.raycastTarget=false;
        }
    }
}
