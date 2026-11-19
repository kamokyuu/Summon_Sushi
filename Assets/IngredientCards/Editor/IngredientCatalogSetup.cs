using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace CookingCards.Editor
{
    public static class IngredientCatalogSetup
    {
        [MenuItem("Tools/Cooking Cards/Create Ingredient Catalog")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            const string folder="Assets/IngredientCards/Prefabs";
            var prefabs=new IngredientCard[IngredientRules.Count];
            string[] existing={"rice_card","Nori_Card","Eggs_card","avocado_card","Salmon_card"};
            for(int i=0;i<prefabs.Length;i++)
            {
                var kind=(IngredientKind)i;
                string path=folder+"/"+(i<5?existing[i]:kind+"_card")+".prefab";
                var saved=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(saved!=null) { prefabs[i]=saved.GetComponent<IngredientCard>(); continue; }
                var go=new GameObject(kind+"_card",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(CanvasGroup),typeof(IngredientCard));
                ((RectTransform)go.transform).sizeDelta=new Vector2(190,254);
                go.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
                var art=new GameObject("Artwork",typeof(RectTransform),typeof(UnityEngine.UI.Image)); art.transform.SetParent(go.transform,false);
                var r=(RectTransform)art.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;
                art.GetComponent<UnityEngine.UI.Image>().color=Color.HSVToRGB((int)IngredientRules.Category(kind)/5f,.35f,.65f);
                art.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
                var label=new GameObject("IngredientLabel",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(art.transform,false);
                var lr=(RectTransform)label.transform;lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=new Vector2(12,12);lr.offsetMax=new Vector2(-12,-12);
                var t=label.GetComponent<TMPro.TextMeshProUGUI>();t.font=TMPro.TMP_Settings.defaultFontAsset;t.text=IngredientRules.Category(kind)+"\n\n"+IngredientRules.DisplayName(kind);t.fontSize=25;t.alignment=TMPro.TextAlignmentOptions.Center;t.raycastTarget=false;
                var card=go.GetComponent<IngredientCard>();card.SetIngredient(kind);card.Configure(r);
                prefabs[i]=PrefabUtility.SaveAsPrefabAsset(go,path).GetComponent<IngredientCard>();Object.DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            // Reload imported assets: freshly saved prefab components may not have persistent file IDs yet.
            for(int i=0;i<prefabs.Length;i++)
            {
                string path=folder+"/"+(i<5?existing[i]:((IngredientKind)i)+"_card")+".prefab";
                prefabs[i]=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<IngredientCard>();
                if(prefabs[i]==null) throw new System.InvalidOperationException("Missing card component: "+path);
            }
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            var hand=Object.FindFirstObjectByType<IngredientCardHand>();
            var so=new SerializedObject(hand);
            var pile=so.FindProperty("drawPile").objectReferenceValue as RectTransform;
            hand.Configure(prefabs,pile);
            EditorUtility.SetDirty(hand);
            PrefabUtility.RecordPrefabInstancePropertyModifications(hand);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
    }
}
