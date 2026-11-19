using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CookingCards.Editor
{
    public static class RecipeJournalSetup
    {
        [MenuItem("Tools/Cooking Cards/Set Up Recipe Journal")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
            var hand=Object.FindFirstObjectByType<IngredientCardHand>();
            if(hand==null)throw new System.InvalidOperationException("Open MainScene first.");
            if(Object.FindFirstObjectByType<RecipeJournal>(FindObjectsInactive.Include)!=null)return;
            var canvas=hand.GetComponentInParent<Canvas>();
            var parent=canvas.transform.Find("OverlayLayer") ?? canvas.transform;
            var root=Rect("RecipeJournal",parent,Vector2.zero,Vector2.zero);
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            var view=root.gameObject.AddComponent<RecipeJournal>();
            var open=Button("JournalButton",root,"RECIPES [E]",new Vector2(20,-20),new Vector2(205,52),new Color(.16f,.22f,.3f),Color.white);
            var panel=Rect("RecipeJournalPanel",root,new Vector2(20,-85),new Vector2(440,355));
            panel.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.94f,.87f,.73f);
            var page=Rect("Page",panel,new Vector2(18,-15),new Vector2(300,325));
            page.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(1,.98f,.9f);
            var header=Label("Header",page,"RECIPE BINDER",new Vector2(18,-12),new Vector2(260,30),19,Color.gray);
            var content=Label("RecipeContent",page,"",new Vector2(18,-57),new Vector2(263,188),22,new Color(.15f,.12f,.1f));
            content.textWrappingMode=TextWrappingModes.Normal;
            var counter=Label("PageCounter",page,"",new Vector2(105,-255),new Vector2(90,38),20,Color.black);
            counter.alignment=TextAlignmentOptions.Center;
            var previous=Button("PreviousRecipeButton",page,"<",new Vector2(18,-255),new Vector2(65,43),new Color(.72f,.66f,.54f),Color.black);
            var next=Button("NextRecipeButton",page,"NEXT >",new Vector2(205,-255),new Vector2(80,43),new Color(.72f,.66f,.54f),Color.black);
            string[] names={"COMMON","UNCOMMON","RARE","SPECIAL","MENU"};
            Color[] colors={new Color(1,.82f,.2f),new Color(.86f,.27f,.26f),new Color(.24f,.54f,.88f),new Color(.16f,.16f,.2f),new Color(.42f,.65f,.4f)};
            var tabs=new UnityEngine.UI.Button[5];
            for(int i=0;i<5;i++)tabs[i]=Button(names[i]+"Tab",panel,names[i],new Vector2(318,-18-i*60),new Vector2(112,52),colors[i],i==0?Color.black:Color.white);
            view.Configure(panel.gameObject,open,previous,next,tabs,content,counter);
            panel.gameObject.SetActive(false);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/IngredientCards/Prefabs/RecipeJournal.prefab");
            Object.DestroyImmediate(root.gameObject);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
            var pause=parent.Find("PauseOverlay");
            if(pause!=null)instance.transform.SetSiblingIndex(pause.GetSiblingIndex());
            EditorSceneManager.MarkSceneDirty(hand.gameObject.scene);
            EditorSceneManager.SaveScene(hand.gameObject.scene);
            AssetDatabase.SaveAssets();
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        private static TextMeshProUGUI Label(string name,Transform parent,string text,Vector2 position,Vector2 size,float fontSize,Color color)
        {
            var rect=Rect(name,parent,position,size);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font=TMP_Settings.defaultFontAsset;label.text=text;label.fontSize=fontSize;label.color=color;label.alignment=TextAlignmentOptions.TopLeft;label.raycastTarget=false;return label;
        }
        private static UnityEngine.UI.Button Button(string name,Transform parent,string title,Vector2 position,Vector2 size,Color background,Color foreground)
        {
            var rect=Rect(name,parent,position,size);var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=background;
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var label=Label("Label",rect,title,Vector2.zero,size,17,foreground);label.alignment=TextAlignmentOptions.Center;
            return button;
        }
    }
}
