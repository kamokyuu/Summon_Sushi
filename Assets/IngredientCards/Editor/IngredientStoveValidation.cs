using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CookingCards.Editor
{
    public static class IngredientStoveValidation
    {
        public static async System.Threading.Tasks.Task<string> Run()
        {
            if (!EditorApplication.isPlaying) throw new System.InvalidOperationException("Run in Play mode.");
            var stove = Object.FindFirstObjectByType<IngredientStove>();
            stove.ResetForNewGame();
            await System.Threading.Tasks.Task.Yield();
            var stoveSprite = stove.GetComponent<UnityEngine.UI.Image>().sprite;
            Require(stoveSprite != null && stoveSprite.rect.width > 100 && stoveSprite.rect.height > 100, "Stove must use the main box artwork, not a tiny auto-sliced fragment");
            var hand = Object.FindFirstObjectByType<IngredientCardHand>();
            var draw = (RectTransform)GameObject.Find("DrawCardButton").transform;
            string[] names = { "rice_card", "Nori_Card", "Eggs_card", "avocado_card", "Salmon_card" };
            var prefabs = names.Select(name => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/IngredientCards/Prefabs/" + name + ".prefab").GetComponent<IngredientCard>()).ToArray();
            int[][] sequences = {
                new[] { 0, 0, 1, 1, 2, 2 },
                new[] { 0, 0, 1, 1, 3, 3, 4, 4 },
                new[] { 0, 0, 4, 4, 4, 4 },
                new[] { 0, 0, 4, 4, 4, 4 }
            };
            string[] outputs = { "tamago_nigiri", "salmon_maki", "salmon_nigiri", "salmon_nigiri" };
            int initialHand = hand.CardCount;
            var outputLayer = GameObject.Find("CookedDishes").transform;
            for (int recipe = 0; recipe < sequences.Length; recipe++)
            {
                hand.SetCollapsed(false);
                for (int index = 0; index < sequences[recipe].Length; index++)
                {
                    var card = Object.Instantiate(prefabs[sequences[recipe][index]], hand.transform, false);
                    card.name = prefabs[sequences[recipe][index]].name;
                    hand.Configure(prefabs, draw);
                    await System.Threading.Tasks.Task.Yield();
                    var data = new PointerEventData(EventSystem.current) {
                        pointerDrag = card.gameObject,
                        button = PointerEventData.InputButton.Left,
                        position = RectTransformUtility.WorldToScreenPoint(null, card.transform.position)
                    };
                    ExecuteEvents.Execute(card.gameObject, data, ExecuteEvents.beginDragHandler);
                    data.position = RectTransformUtility.WorldToScreenPoint(null, stove.transform.position);
                    ExecuteEvents.Execute(card.gameObject, data, ExecuteEvents.dragHandler);
                    await System.Threading.Tasks.Task.Yield();
                    var hits = new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(data, hits);
                    Require(hits.Count > 0 && hits[0].gameObject == stove.gameObject, "Stove must be the top drop target");
                    ExecuteEvents.Execute(hits[0].gameObject, data, ExecuteEvents.dropHandler);
                    ExecuteEvents.Execute(card.gameObject, data, ExecuteEvents.endDragHandler);
                    Require(!card.gameObject.activeSelf && hand.CardCount == initialHand, "Dropped card must leave hand immediately");
                    Require(!stove.Deposit(card), "Cannot count the same card twice");
                    int expected = index == sequences[recipe].Length - 1 ? recipe + 1 : recipe;
                    Require(stove.CookedCount == expected, "Recipe " + recipe + ", ingredient " + index + ": expected " + expected + " dishes, got " + stove.CookedCount);
                }
                Require(outputLayer.childCount == recipe + 1 && outputLayer.GetChild(recipe).name == outputs[recipe], "Wrong dish artwork or output count");
                Require(hand.IsCollapsed, "Successful cooking must automatically collapse the hand");
                float settleUntil = Time.unscaledTime + .65f;
                while (Time.unscaledTime < settleUntil) await System.Threading.Tasks.Task.Yield();
                var stack = GameObject.Find("CollapsedHandAnchor").transform;
                var firstCard = (RectTransform)hand.transform.GetChild(0);
                Require(Vector2.Distance(firstCard.position, stack.position) < 3, "Cards must visibly animate down to the stack");
                Require(Mathf.Abs(firstCard.localScale.x - .45f) < .02f, "Collapsed cards must animate to stack size");
                var image = outputLayer.GetChild(recipe).GetComponent<UnityEngine.UI.Image>();
                Require(image.sprite != null && image.raycastTarget && image.GetComponent<CookedDish>() != null, "Dish must display artwork and accept serving clicks");
                foreach (IngredientKind kind in System.Enum.GetValues(typeof(IngredientKind)))
                    Require(stove.StoredCount(kind) == 0, "Completed recipe must consume exact ingredient counts");
            }
            var extra = Object.Instantiate(prefabs[3], hand.transform, false);
            hand.Configure(prefabs, draw);
            Require(stove.Deposit(extra) && stove.StoredCount(IngredientKind.Avocado) == 1 && stove.CookedCount == 4, "Incomplete ingredients must wait for a recipe");
            int before = hand.CardCount;
            GameObject.Find("DrawCardButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Require(hand.CardCount == before + 1, "Drawing must still work after cooking");
            Require(!outputLayer.GetComponent<Canvas>().overrideSorting && outputLayer.GetSiblingIndex() < hand.transform.GetSiblingIndex(), "Dishes must render behind the reopened card hand");
            return "All three cooking recipes passed; repeat cooking, incomplete inventory, exact consumption, drag/drop raycasts, dishes behind cards, and drawing verified.";
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new System.Exception(message);
        }
    }
}
