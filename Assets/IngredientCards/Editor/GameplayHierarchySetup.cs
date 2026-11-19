using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CookingCards.Editor
{
    public static class GameplayHierarchySetup
    {
        [MenuItem("Tools/Cooking Cards/Organize Gameplay Canvas")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            var hand = Object.FindFirstObjectByType<IngredientCardHand>();
            if (hand == null) throw new System.InvalidOperationException("Open the gameplay scene first.");
            var canvas = hand.GetComponentInParent<Canvas>();
            Undo.RecordObject(canvas.gameObject, "Organize gameplay");
            canvas.name = "GameplayCanvas";
            var root = canvas.transform;
            var hud = Layer(root, "StaticHUD");
            var customers = Layer(root, "CustomerLayer");
            var box = Layer(root, "SummoningBoxLayer");
            var handLayer = Layer(root, "HandLayer");
            Layer(root, "EquipmentLayer");
            Layer(root, "TooltipLayer");
            var overlay = Layer(root, "OverlayLayer");
            Move(root, "Customers", customers);
            Move(root, "IngredientStove", box);
            Move(root, "CookedDishes", box);
            Move(root, "GameResults", overlay, "ResultPanel");
            Move(root, "CoinCounter", hud, "CoinLabel");
            foreach (var name in new[] { "DayLabel", "CoinLabel", "ReputationLabel", "DeckCounter", "DailyGoal" })
                if (hud.Find(name) == null) Layer(hud, name).gameObject.SetActive(false);
            foreach (var name in new[] { "PausePanel", "RecipeJournalPanel", "ResultPanel", "ConfirmationPanel" })
                if (overlay.Find(name) == null) Layer(overlay, name).gameObject.SetActive(false);
            // Move the existing hand as one unit: card coordinates and serialized anchors stay intact.
            if (hand.transform != root) Undo.SetTransformParent(hand.transform, handLayer, "Organize hand");
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveScene(canvas.gameObject.scene);
        }

        private static Transform Layer(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing;
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create gameplay layer");
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void Move(Transform root, string name, Transform target, string rename = null)
        {
            var item = root.Find(name);
            if (item == null || item == target) return;
            Undo.SetTransformParent(item, target, "Organize gameplay object");
            if (rename != null) { Undo.RecordObject(item.gameObject, "Rename gameplay object"); item.name = rename; }
        }
    }
}
