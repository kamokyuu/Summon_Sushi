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
            UpgradeBinder(view);
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
        public static void UpgradeBinder(RecipeJournal journal)
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Edit mode only.");
            var data=new SerializedObject(journal);
            var panel=((GameObject)data.FindProperty("binderPanel").objectReferenceValue).GetComponent<RectTransform>();
            panel.sizeDelta=new Vector2(490,390);
            var panelImage=panel.GetComponent<UnityEngine.UI.Image>();if(panelImage!=null){panelImage.color=Color.clear;panelImage.raycastTarget=false;}
            foreach(string old in new[]{"BackPage1","BackPage2"}){var layer=panel.Find(old);if(layer!=null)layer.gameObject.SetActive(false);}
            var baseImage=Artwork("Binder Base",panel,"Base_Note_Recipe.png");
            Centered(baseImage.rectTransform,new Vector2(-10,0),new Vector2(370,440));baseImage.rectTransform.localRotation=Quaternion.Euler(0,0,-90);baseImage.transform.SetAsFirstSibling();
            var page=(RectTransform)panel.Find("Page");Position(page,new Vector2(18,-35),new Vector2(420,330));page.localRotation=Quaternion.identity;page.SetSiblingIndex(1);
            var oldPaper=page.GetComponent<UnityEngine.UI.Image>();if(oldPaper!=null){oldPaper.color=Color.clear;oldPaper.raycastTarget=false;}
            var paper=Artwork("Paper Artwork",page,"kertas recipe.png");Centered(paper.rectTransform,Vector2.zero,new Vector2(330,420));paper.rectTransform.localRotation=Quaternion.Euler(0,0,-90);paper.transform.SetAsFirstSibling();
            var rings=Artwork("Binder Rings",page,"ring notes.png");Centered(rings.rectTransform,new Vector2(0,139),new Vector2(34,238));rings.rectTransform.localRotation=Quaternion.Euler(0,0,-90);
            Position((RectTransform)page.Find("Header"),new Vector2(24,-47),new Vector2(372,28));
            Position((RectTransform)page.Find("RecipeContent"),new Vector2(24,-84),new Vector2(372,186));
            Position((RectTransform)page.Find("PageCounter"),new Vector2(165,-275),new Vector2(90,22));
            Position((RectTransform)page.Find("PreviousRecipeButton"),new Vector2(24,-302),new Vector2(72,26));
            Position((RectTransform)page.Find("NextRecipeButton"),new Vector2(324,-302),new Vector2(72,26));
            var menu=page.Find("MENUTab")??panel.Find("MENUTab");if(menu!=null){menu.SetParent(page,false);Position((RectTransform)menu,new Vector2(165,-302),new Vector2(90,26));}
            foreach(string control in new[]{"PreviousRecipeButton","NextRecipeButton","MENUTab"})
            {
                var button=page.Find(control);if(button==null)continue;
                foreach(var label in button.GetComponentsInChildren<TMP_Text>(true)){label.fontSize=14;label.alignment=TextAlignmentOptions.Center;var rect=label.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(3,0);rect.offsetMax=new Vector2(-3,0);}
            }
            foreach(var text in page.GetComponentsInChildren<TMP_Text>(true)){text.color=GameUiTheme.Ink;text.outlineWidth=0;}
            var content=(TMP_Text)data.FindProperty("recipeText").objectReferenceValue;content.enableAutoSizing=true;content.fontSizeMin=15;content.fontSizeMax=21;content.alignment=TextAlignmentOptions.TopLeft;content.margin=Vector4.zero;content.lineSpacing=5;content.paragraphSpacing=6;
            var header=page.Find("Header").GetComponent<TMP_Text>();header.fontSize=20;header.alignment=TextAlignmentOptions.Left;
            var counter=(TMP_Text)data.FindProperty("pageLabel").objectReferenceValue;counter.fontSize=16;counter.alignment=TextAlignmentOptions.Center;
            string[] files={"sticky_notes_kuning.png","sticky_notes_merah.png","sticky_notes_biru.png","sticky_notes_hitam.png"};
            var tabs=data.FindProperty("tabButtons");var sticky=data.FindProperty("stickyTabs");sticky.arraySize=4;
            for(int i=0;i<4;i++)
            {
                var button=(UnityEngine.UI.Button)tabs.GetArrayElementAtIndex(i).objectReferenceValue;button.transform.SetParent(panel,false);
                Position((RectTransform)button.transform,new Vector2(418,-80-i*66),new Vector2(72,60));
                var image=button.GetComponent<UnityEngine.UI.Image>();image.sprite=Sprite(files[i]);image.color=Color.white;image.type=UnityEngine.UI.Image.Type.Simple;image.preserveAspect=true;
                button.targetGraphic=image;
                foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))label.enabled=false;
                sticky.GetArrayElementAtIndex(i).objectReferenceValue=button;
            }
            for(int i=3;i>=0;i--)((UnityEngine.UI.Button)tabs.GetArrayElementAtIndex(i).objectReferenceValue).transform.SetAsLastSibling();
            rings.transform.SetAsLastSibling();
            data.FindProperty("binderBase").objectReferenceValue=baseImage;data.FindProperty("recipePaper").objectReferenceValue=paper;data.FindProperty("binderRings").objectReferenceValue=rings;
            data.ApplyModifiedPropertiesWithoutUndo();
            foreach(var component in journal.GetComponentsInChildren<Component>(true))EditorUtility.SetDirty(component);
        }
        private static Sprite Sprite(string file)
        {
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Asset/UI/"+file))if(asset is Sprite sprite)return sprite;
            throw new System.InvalidOperationException("Missing notebook sprite: "+file);
        }
        private static UnityEngine.UI.Image Artwork(string name,Transform parent,string file)
        {
            var child=parent.Find(name);if(child==null){var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);child=go.transform;}
            var image=child.GetComponent<UnityEngine.UI.Image>();image.sprite=Sprite(file);image.color=Color.white;image.preserveAspect=true;image.raycastTarget=false;return image;
        }
        private static void Position(RectTransform rect,Vector2 position,Vector2 size){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;rect.localScale=Vector3.one;}
        private static void Centered(RectTransform rect,Vector2 position,Vector2 size){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;rect.localScale=Vector3.one;}
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
