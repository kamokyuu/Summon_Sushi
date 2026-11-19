using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CookingCards.Editor
{
    public static class IngredientMergeSetup
    {
        public static IngredientKind KindForName(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "rice_card": return IngredientKind.Rice;
                case "nori_card": return IngredientKind.Nori;
                case "eggs_card": return IngredientKind.Eggs;
                case "avocado_card": return IngredientKind.Avocado;
                case "salmon_card": return IngredientKind.Salmon;
                default: throw new System.ArgumentException("Unknown card: " + name);
            }
        }

        [MenuItem("Tools/Cooking Cards/Configure Merge Recipes")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            string[] names = { "rice_card", "Nori_Card", "Eggs_card", "avocado_card", "Salmon_card" };
            foreach (var name in names)
            {
                string path = "Assets/IngredientCards/Prefabs/" + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    root.GetComponent<IngredientCard>().SetIngredient(KindForName(name));
                    if (root.GetComponent<CanvasGroup>() == null) root.AddComponent<CanvasGroup>();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (var card in Object.FindObjectsByType<IngredientCard>(FindObjectsSortMode.None))
            {
                Undo.RecordObject(card, "Set ingredient merge identity");
                card.SetIngredient(KindForName(card.name));
                EditorUtility.SetDirty(card);
                PrefabUtility.RecordPrefabInstancePropertyModifications(card);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("Merge recipes configured: Rice > Nori > Eggs > Avocado > Salmon. Drag matching pairs together.");
        }
    }
}
