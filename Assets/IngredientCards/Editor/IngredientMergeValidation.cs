using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CookingCards.Editor
{
    public static class IngredientMergeValidation
    {
        public static async System.Threading.Tasks.Task<string> Run()
        {
            if (!EditorApplication.isPlaying) throw new System.InvalidOperationException("Run validation in Play mode.");
            string[] names = { "rice_card", "Nori_Card", "Eggs_card", "avocado_card", "Salmon_card" };
            var prefabs = names.Select(name => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/IngredientCards/Prefabs/" + name + ".prefab").GetComponent<IngredientCard>()).ToArray();
            var canvas = GameObject.Find("IngredientCardCanvas").transform;
            int valid = 0, rejected = 0;
            for (int a = 0; a < 5; a++)
            for (int b = 0; b < 5; b++)
            {
                var root = new GameObject("MergeValidation", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster), typeof(IngredientCardHand));
                root.transform.SetParent(canvas, false);
                root.GetComponent<Canvas>().overrideSorting = true;
                root.GetComponent<Canvas>().sortingOrder = 100;
                try
                {
                    var source = Object.Instantiate(prefabs[a], root.transform, false);
                    var target = Object.Instantiate(prefabs[b], root.transform, false);
                    var hand = root.GetComponent<IngredientCardHand>();
                    hand.Configure(prefabs, null);
                    Canvas.ForceUpdateCanvases();
                    await System.Threading.Tasks.Task.Yield();
                    var data = new PointerEventData(EventSystem.current)
                    {
                        button = PointerEventData.InputButton.Left,
                        pointerDrag = source.gameObject,
                        position = RectTransformUtility.WorldToScreenPoint(null, source.transform.position)
                    };
                    ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.pointerDownHandler);
                    ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.beginDragHandler);
                    Require(source.IsDragging && !source.GetComponent<CanvasGroup>().blocksRaycasts, "Drag must release raycasts");
                    data.position = RectTransformUtility.WorldToScreenPoint(null, target.transform.position);
                    ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.dragHandler);
                    Require(Vector2.Distance(source.transform.position, target.transform.position) < .1f, "Card must follow pointer");
                    var draggedPosition = source.transform.position;
                    hand.SendMessage("LateUpdate");
                    Require(Vector2.Distance(source.transform.position, draggedPosition) < .1f, "Layout must not pull a dragged card back");
                    Canvas.ForceUpdateCanvases();
                    await System.Threading.Tasks.Task.Yield();
                    var hits = new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(data, hits);
                    Require(hits.Any(hit => hit.gameObject == target.gameObject), "Target must accept raycasts");
                    Require(!hits.Any(hit => hit.gameObject == source.gameObject), "Dragged card must not block raycasts");
                    ExecuteEvents.Execute(target.gameObject, data, ExecuteEvents.dropHandler);
                    ExecuteEvents.Execute(source.gameObject, data, ExecuteEvents.endDragHandler);
                    bool shouldMerge = a == b && a < 4;
                    if (shouldMerge)
                    {
                        var remaining = root.GetComponentsInChildren<IngredientCard>();
                        Require(hand.CardCount == 1 && remaining.Length == 1, "Merge must consume exactly two and create one");
                        Require((int)remaining[0].Ingredient == a + 1, "Wrong recipe result");
                        Require(!hand.TryCombine(source, target), "Consumed cards cannot merge twice");
                        valid++;
                    }
                    else
                    {
                        Require(hand.CardCount == 2 && source.gameObject.activeSelf && target.gameObject.activeSelf, "Invalid recipe must preserve both cards");
                        Require(!source.IsDragging && source.GetComponent<CanvasGroup>().blocksRaycasts, "Invalid drop must restore interaction");
                        Require(!hand.TryCombine(source, source), "Cannot merge a card with itself");
                        rejected++;
                    }
                    int before = hand.CardCount;
                    hand.DrawCard();
                    Require(hand.CardCount == before + 1, "Drawing must still work after a drop");
                }
                finally { root.SetActive(false); Object.Destroy(root); }
            }
            return valid + " valid recipes passed; " + rejected + " invalid pairs rejected; drag movement, raycast drop targets, consumption, and drawing verified.";
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new System.Exception(message);
        }
    }
}
