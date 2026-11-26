using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CookingCards.Menu.Editor
{
    public static class TutorialDialogueSetup
    {
        [MenuItem("GameObject/UI/Summon Sushi/Tutorial Dialogue Overlay",false,2100)]
        public static void Create()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Edit mode only.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var canvas=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<Canvas>():null;
            if(canvas!=null&&canvas.gameObject.scene!=scene)canvas=null;
            if(canvas==null)canvas=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).FirstOrDefault(c=>c.isRootCanvas);
            if(canvas==null){var root=new GameObject("Dialogue Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;Undo.RegisterCreatedObjectUndo(root,"Create Dialogue Canvas");}
            var go=new GameObject("Tutorial Dialogue Overlay",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster),typeof(CanvasGroup),typeof(TutorialDialogueOverlay));
            Undo.RegisterCreatedObjectUndo(go,"Create Tutorial Dialogue");go.transform.SetParent(canvas.transform,false);
            Stretch((RectTransform)go.transform,Vector2.zero,Vector2.one);
            var overlay=go.GetComponent<Canvas>();overlay.overrideSorting=true;overlay.sortingOrder=100;
            Image dim=Image(go.transform,"Background Dim",new Color(0,0,0,.72f),Vector2.zero,Vector2.one);
            var highlight=Image(go.transform,"Character Highlight",new Color(1,.92f,.76f,.16f),new Vector2(.04f,.32f),new Vector2(.34f,.92f));highlight.raycastTarget=false;
            var portrait=Image(go.transform,"Character NPC1",Color.white,new Vector2(.055f,.33f),new Vector2(.325f,.91f));portrait.preserveAspect=true;
            var npc=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Asset/NPC/NPC 1.png");portrait.sprite=npc;
            var panel=Image(go.transform,"Dialogue Panel",new Color(.98f,.94f,.85f),new Vector2(.05f,.04f),new Vector2(.95f,.31f));
            var speaker=Text(panel.transform,"Speaker",new Vector2(.035f,.74f),new Vector2(.96f,.96f),26);
            var dialogue=Text(panel.transform,"Dialogue",new Vector2(.035f,.2f),new Vector2(.96f,.72f),24);
            var hint=Text(panel.transform,"Advance Hint",new Vector2(.035f,.02f),new Vector2(.96f,.17f),16);hint.text="Click / tap to continue";hint.alignment=TextAlignmentOptions.BottomRight;
            speaker.text="NPC1";dialogue.text="Welcome to Summon Sushi!";
            var controller=go.GetComponent<TutorialDialogueOverlay>();controller.Bind(go.GetComponent<CanvasGroup>(),portrait,speaker,dialogue);
            var data=new SerializedObject(controller);data.FindProperty("defaultCharacter").objectReferenceValue=npc;
            var pages=data.FindProperty("pages");pages.arraySize=2;
            pages.GetArrayElementAtIndex(0).FindPropertyRelative("text").stringValue="Welcome to Summon Sushi!";
            pages.GetArrayElementAtIndex(1).FindPropertyRelative("text").stringValue="Let's learn to draw ingredients, summon sushi, and serve customers.";
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(scene);Selection.activeGameObject=go;
        }
        private static void Stretch(RectTransform rect,Vector2 min,Vector2 max){rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        private static Image Image(Transform parent,string name,Color color,Vector2 min,Vector2 max)
        {var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);Stretch((RectTransform)go.transform,min,max);var image=go.GetComponent<Image>();image.color=color;return image;}
        private static TMP_Text Text(Transform parent,string name,Vector2 min,Vector2 max,float size)
        {var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);Stretch((RectTransform)go.transform,min,max);var text=go.GetComponent<TMP_Text>();text.font=Resources.Load<GameUiTheme>("GameUiTheme")?.font??TMP_Settings.defaultFontAsset;text.fontSize=size;text.color=new Color(.29f,.18f,.13f);text.raycastTarget=false;text.alignment=TextAlignmentOptions.TopLeft;return text;}
    }
}
