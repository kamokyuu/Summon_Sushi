using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CookingCards.Editor
{
    public static class FinalCardArtworkSetup
    {
        [MenuItem("Tools/Cooking Cards/Set Up Final Cards")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
            string[] prefabs={"Shrimp_card","Tuna_card","Salmon_card","rice_card","Nori_Card","avocado_card"};
            string[] files={"Card_Shrimp","Card_Tuna","Card_Salmon","Card_Rice","Card_Nori","Card_Avocado"};
            for(int i=0;i<prefabs.Length;i++)
            {
                var sprite=Import("Assets/Asset/new card/"+files[i]+".png");
                string path="Assets/IngredientCards/Prefabs/"+prefabs[i]+".prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var card=root.GetComponent<IngredientCard>();var so=new SerializedObject(card);
                    var art=so.FindProperty("artwork").objectReferenceValue as RectTransform;
                    if(art==null)throw new System.InvalidOperationException("No artwork in "+path);
                    foreach(var old in art.GetComponentsInChildren<UnityEngine.UI.Image>(true))if(old.transform!=art)old.enabled=false;
                    foreach(var label in art.GetComponentsInChildren<TMPro.TMP_Text>(true))label.enabled=false;
                    var image=art.GetComponent<UnityEngine.UI.Image>()??art.gameObject.AddComponent<UnityEngine.UI.Image>();
                    image.enabled=true;image.sprite=sprite;image.color=Color.white;image.type=UnityEngine.UI.Image.Type.Simple;image.preserveAspect=true;image.raycastTarget=false;
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            var hand=Object.FindFirstObjectByType<IngredientCardHand>();
            if(hand==null)throw new System.InvalidOperationException("Open gameplay scene first.");
            var state=new SerializedObject(hand);var pile=state.FindProperty("drawPile").objectReferenceValue as RectTransform;
            if(pile==null)throw new System.InvalidOperationException("No draw pile configured.");
            var back=pile.GetComponent<UnityEngine.UI.Image>();Undo.RecordObject(back,"Set final draw back artwork");
            back.sprite=Import("Assets/Asset/new card/Back_Card_Summon Sushi.png");back.preserveAspect=true;back.color=Color.white;
            PrefabUtility.RecordPrefabInstancePropertyModifications(back);
            EditorSceneManager.MarkSceneDirty(hand.gameObject.scene);EditorSceneManager.SaveScene(hand.gameObject.scene);AssetDatabase.SaveAssets();
        }
        private static Sprite Import(string path)
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new System.InvalidOperationException("Missing card artwork: "+path);
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            int limit=Mathf.Max(2048,Mathf.NextPowerOfTwo(Mathf.Max(width,height)));
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.maxTextureSize=limit;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.crunchedCompression=false;
            importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;
            importer.wrapMode=TextureWrapMode.Clamp;importer.alphaIsTransparency=true;importer.sRGBTexture=true;
            foreach(string platform in new[]{"Standalone","WebGL"})
            {
                var settings=importer.GetPlatformTextureSettings(platform);settings.overridden=true;settings.maxTextureSize=limit;
                settings.format=TextureImporterFormat.RGBA32;settings.textureCompression=TextureImporterCompression.Uncompressed;settings.crunchedCompression=false;
                importer.SetPlatformTextureSettings(settings);
            }
            importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(sprite==null)throw new System.InvalidOperationException("Cannot load card sprite: "+path);
            return sprite;
        }
    }
}
