using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CookingCards.Editor
{
    public static class WebGameBuild
    {
        [MenuItem("Tools/Cooking Cards/Set Up Final Background And Web Build")]
        public static void Configure()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
            var hand=UnityEngine.Object.FindFirstObjectByType<IngredientCardHand>();
            if(hand==null)throw new InvalidOperationException("Open gameplay scene first.");
            var canvas=hand.GetComponentInParent<Canvas>();var canvasRect=(RectTransform)canvas.transform;
            var belt=UnityEngine.Object.FindFirstObjectByType<ConveyorController>();
            var background=canvas.transform.Find("GameplayBackground");
            if(background==null){var go=new GameObject("GameplayBackground",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(GameplayBackgroundLayout));go.transform.SetParent(canvas.transform,false);background=go.transform;}
            var rect=(RectTransform)background;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;background.SetAsFirstSibling();
            var image=background.GetComponent<UnityEngine.UI.Image>();image.sprite=SpriteAt("Assets/Asset/background game/background paling bawah.png");image.color=Color.white;image.raycastTarget=false;
            var table=background.Find("SushiTable");
            if(table==null){var go=new GameObject("SushiTable",typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(background,false);table=go.transform;}
            var tr=(RectTransform)table;tr.anchorMin=Vector2.zero;tr.anchorMax=new Vector2(1,0);tr.pivot=new Vector2(.5f,0);tr.anchoredPosition=Vector2.zero;
            var tableImage=table.GetComponent<UnityEngine.UI.Image>();tableImage.sprite=SpriteAt("Assets/Asset/background game/meja salmon.png");tableImage.color=Color.white;tableImage.raycastTarget=false;
            background.GetComponent<GameplayBackgroundLayout>().Configure(tr,(RectTransform)belt.transform,canvasRect);
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(renderer.name.StartsWith("Sushi_Background") || renderer.name.StartsWith("Serene Japanese Restaurant")){Undo.RecordObject(renderer,"Replace legacy background");renderer.enabled=false;}
            var hr=(RectTransform)hand.transform;Undo.RecordObject(hr,"Lower card hand");hr.anchorMin=hr.anchorMax=new Vector2(.42f,.16f);hr.anchoredPosition=Vector2.zero;
            var state=new SerializedObject(hand);var pile=state.FindProperty("drawPile").objectReferenceValue as RectTransform;
            if(pile!=null){Undo.RecordObject(pile,"Separate draw pile from hand");pile.anchorMin=pile.anchorMax=new Vector2(.08f,.14f);pile.anchoredPosition=Vector2.zero;}
            var paw=canvas.transform.Find("PawHandToggle");if(paw!=null){Undo.RecordObject(paw.gameObject,"Hide paw control");paw.gameObject.SetActive(false);}
            hand.SetCollapsed(false);
            EditorSceneManager.MarkSceneDirty(hand.gameObject.scene);EditorSceneManager.SaveScene(hand.gameObject.scene);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",true),new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity",true)};
            PlayerSettings.productName="Summon Sushi";PlayerSettings.companyName="Corepanics";
            PlayerSettings.defaultWebScreenWidth=1280;PlayerSettings.defaultWebScreenHeight=720;
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.WebGL.initialMemorySize=256;
            AssetDatabase.SaveAssets();
        }
        private static Sprite SpriteAt(string path)
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new InvalidOperationException("Missing background: "+path);
            importer.GetSourceTextureWidthAndHeight(out int w,out int h);importer.maxTextureSize=Mathf.Max(2048,Mathf.NextPowerOfTwo(Mathf.Max(w,h)));
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        public static void MakeHtmlResponsive(string output)
        {
            string path=Path.Combine(output,"index.html");
            string html=File.ReadAllText(path);
            if(html.Contains("summon-responsive-layout"))return;
            const string style="<style id=\"summon-responsive-layout\">html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#FBF2E2}#unity-container.unity-desktop{width:min(100vw,calc((100vh - 40px)*16/9));height:auto;left:50%;top:50%;transform:translate(-50%,-50%)}#unity-canvas{display:block;width:100%!important;height:auto!important;aspect-ratio:16/9}#unity-footer{width:100%}@media(max-width:600px){#unity-footer{display:none}#unity-container.unity-desktop{width:min(100vw,calc(100vh*16/9))}}</style>";
            html=html.Replace("</head>",style+"\n</head>");File.WriteAllText(path,html);
        }
    }
    public sealed class ResponsiveWebPostprocess:UnityEditor.Build.IPostprocessBuildWithReport
    {
        public int callbackOrder=>10000;
        public void OnPostprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {if(report.summary.platform==BuildTarget.WebGL)WebGameBuild.MakeHtmlResponsive(report.summary.outputPath);}
    }
}
