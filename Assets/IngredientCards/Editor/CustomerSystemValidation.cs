using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CookingCards.Editor
{
    public static class CustomerSystemValidation
    {
        public static async System.Threading.Tasks.Task<string> Run()
        {
            var system = Object.FindFirstObjectByType<CustomerSystem>();
            var hand = Object.FindFirstObjectByType<IngredientCardHand>();
            while (hand.IsDealing) await System.Threading.Tasks.Task.Yield();
            system.enabled = false;
            try
            {
                while (system.ActiveCount < 3) Require(system.SpawnCustomer() != null, "Customer failed to spawn");
                Require(system.SpawnCustomer() == null && system.ActiveCount == 3, "Three customer cap failed");
                float deadline = Time.realtimeSinceStartup + 15;
                while (system.Customers.Any(customer => customer.State != NpcCustomer.VisitState.Ordering))
                {
                    Require(Time.realtimeSinceStartup < deadline, "Customers did not walk to their stops");
                    await System.Threading.Tasks.Task.Yield();
                }
                int requested = system.Customers.Sum(customer => customer.RemainingCount);
                foreach (var customer in system.Customers)
                {
                    Require(customer.RemainingCount >= 1 && customer.RemainingCount <= 3, "Orders must contain 1-3 dishes");
                    Require(Mathf.Abs(((RectTransform)customer.transform).anchoredPosition.x - (customer.Slot - 1) * 310) < 1, "Wrong stop position");
                    Require(customer.transform.Find("OrderBubble").gameObject.activeSelf, "Order must show after arrival");
                }
                int startingCoins = system.Coins;
                Require(!system.TryServe((DishKind)999) && system.Coins == startingCoins, "Unmatched food must not pay coins");
                var stove = Object.FindFirstObjectByType<IngredientStove>();
                var output = GameObject.Find("CookedDishes").transform;
                var wrong = new GameObject("UnmatchedDishTest", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CookedDish));
                wrong.transform.SetParent(output, false);
                wrong.GetComponent<CookedDish>().Configure((DishKind)999, stove);
                var click = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
                wrong.GetComponent<CookedDish>().OnPointerClick(click);
                Require(wrong.activeSelf && system.Coins == startingCoins, "Unmatched dish must remain on counter");
                wrong.SetActive(false); wrong.transform.SetParent(null); Object.Destroy(wrong);
                int served = 0;
                while (system.Customers.Any(customer => customer.State == NpcCustomer.VisitState.Ordering))
                {
                    var customer = system.Customers.First(item => item.State == NpcCustomer.VisitState.Ordering);
                    var kind = customer.Order[0];
                    typeof(IngredientStove).GetMethod("ShowDish", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .Invoke(stove, new object[] { (int)kind });
                    var dish = output.GetChild(output.childCount - 1).GetComponent<CookedDish>();
                    // Allow both the new graphic and the automatic hand collapse to settle.
                    float settle = Time.unscaledTime + .55f;
                    while (Time.unscaledTime < settle) await System.Threading.Tasks.Task.Yield();
                    var hits = new System.Collections.Generic.List<RaycastResult>();
                    deadline = Time.realtimeSinceStartup + 5;
                    do
                    {
                        await System.Threading.Tasks.Task.Yield();
                        click.position = RectTransformUtility.WorldToScreenPoint(null, dish.transform.position);
                        hits.Clear();
                        EventSystem.current.RaycastAll(click, hits);
                        Require(Time.realtimeSinceStartup < deadline, "Food remained obscured after hand collapse");
                    } while (hits.Count == 0 || hits[0].gameObject != dish.gameObject);
                    Require(hits.Count > 0 && hits[0].gameObject == dish.gameObject, "Cooked dish must accept actual serving clicks");
                    ExecuteEvents.Execute(dish.gameObject, click, ExecuteEvents.pointerClickHandler);
                    Require(!dish.gameObject.activeSelf, "Served dish must disappear immediately");
                    dish.OnPointerClick(click);
                    served++;
                    Require(system.Coins == startingCoins + served, "Each dish must pay exactly one coin, including repeated clicks");
                }
                Require(served == requested, "All requested food must be served before customers leave");
                deadline = Time.realtimeSinceStartup + 15;
                while (system.ActiveCount > 0)
                {
                    Require(Time.realtimeSinceStartup < deadline, "Completed customers did not walk off screen");
                    await System.Threading.Tasks.Task.Yield();
                }
                Require(system.SpawnCustomer() != null, "A new customer must be able to use a vacated slot");
                return "Three-customer cap, walk-in stops, 1-3 item orders, wrong-dish rejection, clickable food, exact coins, duplicate-click protection, walk-out, and replacement arrivals passed. Served " + served + " dishes.";
            }
            finally { system.enabled = true; }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new System.Exception(message);
        }
    }
}
