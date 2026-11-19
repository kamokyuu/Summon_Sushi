using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
namespace CookingCards.Editor
{
    public static class GameUiArtworkSetup
    {
        private const string Folder="Assets/IngredientCards/UI";
        [MenuItem("Tools/Cooking Cards/Apply Final UI Artwork")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
            var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene before applying the theme.");
            Directory.CreateDirectory(Folder);Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.Refresh();
            var font=FontAsset();
            var tmpSettings=new SerializedObject(TMP_Settings.instance);
            tmpSettings.FindProperty("m_defaultFontAsset").objectReferenceValue=font;tmpSettings.ApplyModifiedProperties();EditorUtility.SetDirty(TMP_Settings.instance);
            var theme=AssetDatabase.LoadAssetAtPath<GameUiTheme>("Assets/Resources/GameUiTheme.asset");
            if(theme==null){theme=ScriptableObject.CreateInstance<GameUiTheme>();AssetDatabase.CreateAsset(theme,"Assets/Resources/GameUiTheme.asset");}
            theme.font=font;theme.buttonSprite=RoundedButton();EditorUtility.SetDirty(theme);
            var note=SpriteAt("Assets/Asset/UI/base note.png");
            var tabs=new Sprite[4];for(int i=0;i<4;i++)tabs[i]=SpriteAt("Assets/Asset/UI/Rectangle "+(80+i)+".png");
            string path="Assets/IngredientCards/Prefabs/RecipeJournal.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try{StyleJournal(prefab,note,tabs,theme);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
            var boxArt=SpriteAt("Assets/Asset/UI/summon box.png");
            foreach(string scenePath in new[]{"Assets/Scenes/MainScene.unity","Assets/Scenes/MainMenu.unity","Assets/Scenes/LevelMap.unity"})
            {
                if(!File.Exists(scenePath))continue;
                var scene=EditorSceneManager.OpenScene(scenePath);
                foreach(var root in scene.GetRootGameObjects())theme.Apply(root);
                var box=UnityEngine.Object.FindFirstObjectByType<IngredientStove>();
                if(box!=null){var image=box.GetComponent<UnityEngine.UI.Image>();image.sprite=boxArt;image.color=Color.white;image.preserveAspect=true;EditorUtility.SetDirty(image);PrefabUtility.RecordPrefabInstancePropertyModifications(image);}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);
        }
        private static TMP_FontAsset FontAsset()
        {
            string path=Folder+"/Bestime SDF.asset";var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(asset!=null)return asset;
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/Asset/UI/Bestime.ttf");if(source==null)throw new InvalidOperationException("Bestime.ttf missing");
            asset=TMP_FontAsset.CreateFontAsset(source,90,9,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);asset.name="Bestime SDF";
            string glyphs="";for(int i=32;i<127;i++)glyphs+=(char)i;glyphs+="¥×–";
            asset.TryAddCharacters(glyphs,out string missing);asset.atlasPopulationMode=AtlasPopulationMode.Static;
            if(TMP_Settings.defaultFontAsset!=null)asset.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{TMP_Settings.defaultFontAsset};
            AssetDatabase.CreateAsset(asset,path);AssetDatabase.AddObjectToAsset(asset.material,asset);
            foreach(var atlas in asset.atlasTextures)if(atlas!=null)AssetDatabase.AddObjectToAsset(atlas,asset);
            EditorUtility.SetDirty(asset);return asset;
        }
        private static Sprite RoundedButton()
        {
            const string path=Folder+"/RoundedButton.png";var texture=new Texture2D(128,64,TextureFormat.RGBA32,false);
            for(int y=0;y<64;y++)for(int x=0;x<128;x++)
            {
                var q=new Vector2(Mathf.Abs(x+.5f-64)-46,Mathf.Abs(y+.5f-32)-14);
                float d=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-18;
                var color=d>-2?GameUiTheme.Ink:GameUiTheme.Paper;color.a=Mathf.Clamp01(.5f-d);texture.SetPixel(x,y,color);
            }
            texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var sprite=SpriteAt(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteBorder=new Vector4(20,20,20,20);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static Sprite SpriteAt(string path)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)throw new InvalidOperationException("Missing UI asset "+path);
            importer.GetSourceTextureWidthAndHeight(out int w,out int h);importer.maxTextureSize=Mathf.Max(2048,Mathf.NextPowerOfTwo(Mathf.Max(w,h)));
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.crunchedCompression=false;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static void Position(Transform target,float x,float y,float w,float h)
        {var r=(RectTransform)target;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
        private static void StyleJournal(GameObject root,Sprite note,Sprite[] tabs,GameUiTheme theme)
        {
            var panel=root.transform.Find("RecipeJournalPanel");Position(panel,20,-85,320,365);
            var background=panel.GetComponent<UnityEngine.UI.Image>();background.color=Color.clear;background.raycastTarget=false;
            for(int i=2;i>=1;i--)
            {
                var name="BackPage"+i;var back=panel.Find(name);
                if(back==null){var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(panel,false);back=go.transform;}
                Position(back,i*5,0,260,350);var image=back.GetComponent<UnityEngine.UI.Image>();image.sprite=note;image.color=Color.white;image.raycastTarget=false;back.SetAsFirstSibling();
            }
            string[] names={"COMMONTab","UNCOMMONTab","RARETab","SPECIALTab"};
            for(int i=0;i<4;i++)
            {
                var tab=panel.Find(names[i]);Position(tab,240,-25-i*72,64,60);
                var image=tab.GetComponent<UnityEngine.UI.Image>();image.sprite=tabs[i];image.color=Color.white;image.type=UnityEngine.UI.Image.Type.Simple;
                foreach(var text in tab.GetComponentsInChildren<TMP_Text>(true))text.enabled=false;
            }
            var page=panel.Find("Page");Position(page,0,0,260,350);var paper=page.GetComponent<UnityEngine.UI.Image>();paper.sprite=note;paper.color=Color.white;paper.raycastTarget=false;page.SetAsLastSibling();
            Position(page.Find("Header"),22,-18,215,28);Position(page.Find("RecipeContent"),22,-60,215,195);
            Position(page.Find("PageCounter"),85,-265,90,25);
            Position(page.Find("PreviousRecipeButton"),20,-305,48,28);Position(page.Find("NextRecipeButton"),195,-305,48,28);
            var menu=panel.Find("MENUTab")??page.Find("MENUTab");menu.SetParent(page,false);Position(menu,85,-305,95,28);
            foreach(var text in page.GetComponentsInChildren<TMP_Text>(true)){text.color=GameUiTheme.Ink;text.font=theme.font;text.outlineWidth=0;}
            page.Find("RecipeContent").GetComponent<TMP_Text>().fontSize=20;page.Find("Header").GetComponent<TMP_Text>().fontSize=20;
            page.Find("Header").GetComponent<TMP_Text>().text="RECIPE NOTES";page.Find("PageCounter").GetComponent<TMP_Text>().fontSize=16;
            foreach(var b in page.GetComponentsInChildren<UnityEngine.UI.Button>(true))foreach(var text in b.GetComponentsInChildren<TMP_Text>(true))
            {
                text.fontSize=13;text.fontStyle=FontStyles.Normal;text.alignment=TextAlignmentOptions.Center;
                var tr=text.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(3,0);tr.offsetMax=new Vector2(-3,0);
            }
            page.Find("PreviousRecipeButton").GetComponentInChildren<TMP_Text>().text="BACK";
            page.Find("NextRecipeButton").GetComponentInChildren<TMP_Text>().text="NEXT";
            theme.Apply(root);
        }
    }
}
