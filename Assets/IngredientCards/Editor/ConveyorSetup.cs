using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace CookingCards.Editor
{
    public static class ConveyorSetup
    {
        [MenuItem("Tools/Cooking Cards/Set Up Conveyor")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode.");
            if(Object.FindFirstObjectByType<ConveyorController>()!=null)return;
            var box=Object.FindFirstObjectByType<IngredientStove>();var so=new SerializedObject(box);
            var output=(RectTransform)so.FindProperty("dishesLayer").objectReferenceValue;
            var canvas=box.GetComponentInParent<Canvas>();
            var go=new GameObject("Conveyor",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.RectMask2D),typeof(ConveyorController));
            go.transform.SetParent(output.parent,false);go.transform.SetSiblingIndex(output.GetSiblingIndex());
            var r=(RectTransform)go.transform;r.anchorMin=new Vector2(0,.5f);r.anchorMax=new Vector2(1,.5f);r.sizeDelta=new Vector2(0,150);r.anchoredPosition=new Vector2(0,output.anchoredPosition.y);
            var image=go.GetComponent<UnityEngine.UI.Image>();image.sprite=SpriteAt("Assets/Asset/Konveyor.png");image.raycastTarget=false;
            string[] colors={"Kuning","Merah","Biru","Hitam"};var normal=new Sprite[4];var broken=new Sprite[4];
            for(int i=0;i<4;i++){normal[i]=SpriteAt("Assets/Asset/Piring Konveyor/Piring "+colors[i]+".png");broken[i]=SpriteAt("Assets/Asset/Piring Konveyor/Piring Pecah "+colors[i]+".png");}
            var wallet=canvas.transform.Find("CoinCounter")?.GetComponent<TMPro.TMP_Text>();
            go.GetComponent<ConveyorController>().Configure(normal,broken,wallet);
            // Keep cooked food just below the belt so it can be dragged onto moving plates.
            Undo.RecordObject(output,"Move prepared food below conveyor");output.anchoredPosition+=new Vector2(0,-145);
            EditorSceneManager.MarkSceneDirty(box.gameObject.scene);EditorSceneManager.SaveScene(box.gameObject.scene);AssetDatabase.SaveAssets();
        }
        private static Sprite SpriteAt(string path)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer.textureType!=TextureImporterType.Sprite || importer.spriteImportMode!=SpriteImportMode.Single)
            {importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.SaveAndReimport();}
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite==null)throw new System.InvalidOperationException("Missing sprite: "+path);return sprite;
        }
    }
}
