using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CookingCards.Editor
{
    public static class IngredientStoveSetup
    {
        [MenuItem("Tools/Cooking Cards/Set Up Stove")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            var canvas = GameObject.Find("IngredientCardCanvas");
            var hand = canvas.GetComponentInChildren<IngredientCardHand>();
            var draw = (RectTransform)canvas.transform.Find("DrawCardButton");
            var box = LoadSprite("box_card");
            var tamago = LoadSprite("tamago_nigiri");
            var maki = LoadSprite("salmon_maki");
            var nigiri = LoadSprite("salmon_nigiri");

            var stove = canvas.transform.Find("IngredientStove");
            if (stove == null)
            {
                var go = new GameObject("IngredientStove", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(IngredientStove));
                Undo.RegisterCreatedObjectUndo(go, "Add ingredient stove");
                go.transform.SetParent(canvas.transform, false);
                stove = go.transform;
            }
            var rect = (RectTransform)stove;
            Undo.RecordObject(rect, "Position stove under rightmost card");
            rect.anchorMin = draw.anchorMin; rect.anchorMax = draw.anchorMax;
            rect.sizeDelta = new Vector2(180, 180);
            // The initial compact five-card hand ends at x = 2 * 135.
            rect.anchoredPosition = draw.anchoredPosition + new Vector2(270, 0);
            var image = stove.GetComponent<UnityEngine.UI.Image>();
            image.sprite = box; image.preserveAspect = true; image.raycastTarget = true;

            var output = canvas.transform.Find("CookedDishes");
            if (output == null)
            {
                var go = new GameObject("CookedDishes", typeof(RectTransform), typeof(Canvas));
                Undo.RegisterCreatedObjectUndo(go, "Add finished dish layer");
                go.transform.SetParent(canvas.transform, false);
                output = go.transform;
                var newOutput = (RectTransform)output;
                newOutput.anchorMin = newOutput.anchorMax = new Vector2(.5f, .5f);
                newOutput.sizeDelta = new Vector2(1080, 120);
                newOutput.anchoredPosition = new Vector2(0, 60);
            }
            var outputRect = (RectTransform)output;
            output.SetSiblingIndex(hand.transform.GetSiblingIndex());
            var topCanvas = output.GetComponent<Canvas>();
            topCanvas.overrideSorting = false; topCanvas.sortingOrder = 0;
            var controller = stove.GetComponent<IngredientStove>();
            controller.Configure(hand, outputRect, tamago, maki, nigiri);
            var inventory = stove.Find("IngredientCounts");
            if (inventory == null)
            {
                var panel = new GameObject("IngredientCounts", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                panel.transform.SetParent(stove, false);
                inventory = panel.transform;
                panel.GetComponent<UnityEngine.UI.Image>().color = new Color(.14f, .07f, .035f, .9f);
                panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                var labelObject = new GameObject("Counts", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                labelObject.transform.SetParent(inventory, false);
                var labelRect = (RectTransform)labelObject.transform;
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(6, 3); labelRect.offsetMax = new Vector2(-6, -3);
                var label = labelObject.GetComponent<TMPro.TextMeshProUGUI>();
                label.font = TMPro.TMP_Settings.defaultFontAsset;
                label.fontSize = 16; label.alignment = TMPro.TextAlignmentOptions.Center;
                label.color = new Color(1, .95f, .84f); label.raycastTarget = false;
            }
            var inventoryRect = (RectTransform)inventory;
            inventoryRect.anchorMin = inventoryRect.anchorMax = new Vector2(.5f, 1);
            inventoryRect.anchoredPosition = new Vector2(0, 50);
            inventoryRect.sizeDelta = new Vector2(280, 65);
            controller.SetInventoryLabel(inventory.GetComponentInChildren<TMPro.TMP_Text>());
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(image);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log("Stove ready: drop ingredients into box_card to cook Tamago Nigiri, Salmon Maki, and Salmon Nigiri.");
        }

        private static Sprite LoadSprite(string name)
        {
            string path = "Assets/Asset/" + name + ".png";
            // Auto-sliced artwork can include tiny stray pixels before the main image.
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderByDescending(item => item.rect.width * item.rect.height).FirstOrDefault();
            if (sprite != null) return sprite;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new System.InvalidOperationException("Missing artwork: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
