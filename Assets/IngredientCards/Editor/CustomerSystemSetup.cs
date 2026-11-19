using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CookingCards.Editor
{
    public static class CustomerSystemSetup
    {
        [MenuItem("Tools/Cooking Cards/Set Up Customers")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            var canvas = GameObject.Find("IngredientCardCanvas");
            var layer = canvas.transform.Find("Customers");
            if (layer == null)
            {
                var go = new GameObject("Customers", typeof(RectTransform), typeof(CustomerSystem));
                Undo.RegisterCreatedObjectUndo(go, "Add customer system");
                go.transform.SetParent(canvas.transform, false);
                layer = go.transform;
            }
            var rect = (RectTransform)layer;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .72f);
            rect.sizeDelta = new Vector2(1200, 250); rect.anchoredPosition = Vector2.zero;
            layer.SetAsFirstSibling();
            var hud = canvas.transform.Find("CoinCounter");
            if (hud == null)
            {
                var go = new GameObject("CoinCounter", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                Undo.RegisterCreatedObjectUndo(go, "Add coin counter");
                go.transform.SetParent(canvas.transform, false);
                hud = go.transform;
                go.GetComponent<UnityEngine.UI.Image>().color = new Color(.14f, .07f, .035f, .92f);
                go.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                var text = new GameObject("Coins", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                text.transform.SetParent(hud, false);
                var textRect = (RectTransform)text.transform;
                textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(8, 3); textRect.offsetMax = new Vector2(-8, -3);
                var label = text.GetComponent<TMPro.TextMeshProUGUI>();
                label.font = TMPro.TMP_Settings.defaultFontAsset;
                label.fontSize = 26; label.color = new Color(1, .84f, .38f);
                label.alignment = TMPro.TextAlignmentOptions.Center; label.raycastTarget = false;
            }
            var hr = (RectTransform)hud;
            hr.anchorMin = hr.anchorMax = hr.pivot = Vector2.one;
            hr.anchoredPosition = new Vector2(-20, -20); hr.sizeDelta = new Vector2(180, 48);
            var portraits = new[] { "NPC/Ganyu", "NPC/Paimon 5", "NPC/Yae Miko 1" }.Select(LoadSprite).ToArray();
            var dishes = new[] { "tamago_nigiri", "salmon_maki", "salmon_nigiri" }.Select(LoadSprite).ToArray();
            var system = layer.GetComponent<CustomerSystem>();
            system.Configure(portraits, dishes, hud.GetComponentInChildren<TMPro.TMP_Text>());
            var output = canvas.transform.Find("CookedDishes");
            if (output.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                Undo.AddComponent<UnityEngine.UI.GraphicRaycaster>(output.gameObject);
            EditorUtility.SetDirty(system);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            EditorSceneManager.SaveScene(canvas.scene);
        }

        private static Sprite LoadSprite(string name)
        {
            string path = "Assets/Asset/" + name + ".png";
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderByDescending(item => item.rect.width * item.rect.height).FirstOrDefault();
            if (sprite != null) return sprite;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new System.InvalidOperationException("Missing artwork: " + path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
