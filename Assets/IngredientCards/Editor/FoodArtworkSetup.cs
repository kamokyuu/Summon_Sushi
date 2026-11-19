using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CookingCards.Editor
{
    public static class FoodArtworkSetup
    {
        [MenuItem("Tools/Cooking Cards/Set Up Final Food Artwork")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
            var belt=Object.FindFirstObjectByType<ConveyorController>();
            if(belt==null)throw new System.InvalidOperationException("Open gameplay scene first.");
            string[] files={"ebi nigiri final","Avocado Maki final","Tuna Avocado Final","salmon nigiri final"};
            var so=new SerializedObject(belt);var sprites=so.FindProperty("foodSprites");sprites.arraySize=4;
            for(int i=0;i<4;i++)
            {
                string path="Assets/Asset/"+files[i]+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(importer==null)throw new System.InvalidOperationException("Missing artwork: "+path);
                if(importer.textureType!=TextureImporterType.Sprite || importer.spriteImportMode!=SpriteImportMode.Single)
                {importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.SaveAndReimport();}
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(sprite==null)throw new System.InvalidOperationException("Cannot load sprite: "+path);
                sprites.GetArrayElementAtIndex(i).objectReferenceValue=sprite;
            }
            so.ApplyModifiedProperties();EditorSceneManager.MarkSceneDirty(belt.gameObject.scene);EditorSceneManager.SaveScene(belt.gameObject.scene);AssetDatabase.SaveAssets();
        }
    }
}
