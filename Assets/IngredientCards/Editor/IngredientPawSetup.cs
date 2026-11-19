using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CookingCards.Editor
{
    public static class IngredientPawSetup
    {
        [MenuItem("Tools/Cooking Cards/Set Up Paw Toggle")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            var canvas = GameObject.Find("IngredientCardCanvas");
            var hand = canvas.GetComponentInChildren<IngredientCardHand>();
            var draw = (RectTransform)canvas.transform.Find("DrawCardButton");
            var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Asset/paw.png").OfType<Sprite>();
            var sprite = sprites.OrderByDescending(s => s.rect.width * s.rect.height).FirstOrDefault();
            if (sprite == null)
            {
                var importer = AssetImporter.GetAtPath("Assets/Asset/paw.png") as TextureImporter;
                if (importer == null) throw new System.InvalidOperationException("paw.png not found.");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Asset/paw.png");
            }
            var paw = canvas.transform.Find("PawHandToggle");
            if (paw == null)
            {
                var go = new GameObject("PawHandToggle", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                Undo.RegisterCreatedObjectUndo(go, "Add paw hand toggle");
                go.transform.SetParent(canvas.transform, false);
                paw = go.transform;
            }
            var rect = (RectTransform)paw;
            rect.anchorMin = draw.anchorMin; rect.anchorMax = draw.anchorMax;
            rect.anchoredPosition = draw.anchoredPosition + new Vector2(-270, 0);
            rect.sizeDelta = new Vector2(110, 110);
            var image = paw.GetComponent<UnityEngine.UI.Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = true;
            var button = paw.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            var colors = button.colors;
            colors.highlightedColor = new Color(1, .9f, .72f);
            colors.pressedColor = new Color(.8f, .65f, .5f);
            button.colors = colors;
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                if (button.onClick.GetPersistentTarget(i) == hand) UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, hand.ToggleHand);
            var anchor = canvas.transform.Find("CollapsedHandAnchor");
            if (anchor == null)
            {
                var go = new GameObject("CollapsedHandAnchor", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Add card stack anchor");
                go.transform.SetParent(canvas.transform, false);
                anchor = go.transform;
            }
            var ar = (RectTransform)anchor;
            ar.anchorMin = ar.anchorMax = new Vector2(.5f, .35f);
            ar.anchoredPosition = new Vector2(-270, 0);
            ar.sizeDelta = Vector2.zero;
            Undo.RecordObject(hand, "Configure card stack animation");
            hand.ConfigureStack(ar);
            if (hand.GetComponent<CanvasGroup>() == null) Undo.AddComponent<CanvasGroup>(hand.gameObject);
            EditorUtility.SetDirty(hand); EditorUtility.SetDirty(button); EditorUtility.SetDirty(image);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(canvas.scene);
        }
    }
}
