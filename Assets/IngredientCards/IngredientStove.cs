using UnityEngine;
using UnityEngine.EventSystems;

namespace CookingCards
{
    public sealed class IngredientStove : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private IngredientCardHand hand;
        [SerializeField] private RectTransform dishesLayer;
        [SerializeField] private Sprite tamagoNigiri;
        [SerializeField] private Sprite salmonMaki;
        [SerializeField] private Sprite salmonNigiri;
        [SerializeField] private TMPro.TMP_Text inventoryLabel;
        private readonly int[] ingredients = new int[IngredientRules.Count];
        private static readonly string[] DishNames = { "tamago_nigiri", "salmon_maki", "salmon_nigiri", "ebi_nigiri", "maguro_nigiri" };
        public int CookedCount { get; private set; }
        public int StoredCount(IngredientKind kind) => ingredients[(int)kind];

        public void SetInventoryLabel(TMPro.TMP_Text label) { inventoryLabel = label; RefreshInventory(); }
        private void Start()
        {
            ResetForNewGame();
            (GetComponent<SummonControls>()??gameObject.AddComponent<SummonControls>()).Configure(this);
        }
        public SushiRecipe ReadyRecipe
        {get{for(int grade=3;grade>=0;grade--)foreach(var r in ActiveLevel.Conveyor((RecipeGrade)grade))if(CanMake(r))return r;return null;}}
        private bool CanMake(SushiRecipe recipe)
        {foreach(var amount in recipe.Ingredients)if(ingredients[(int)amount.Kind]<amount.Count)return false;return true;}
        private void Update()
        {
            if(PreparationSession.Tutorial || Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished)return;
            var belt=Object.FindFirstObjectByType<ConveyorController>();
            ServeStock(belt);
        }
        private bool ServeStock(ConveyorController belt)
        {
            if(belt==null)return false;
            bool served=false;
            // Higher grades first when several dishes can use the same stock.
            for(int grade=3;grade>=0;grade--)
                foreach(var recipe in ActiveLevel.Conveyor((RecipeGrade)grade))
                    while(CanMake(recipe) && belt.TryServeLeftmost(recipe))
                    {
                        foreach(var item in recipe.Ingredients)ingredients[(int)item.Kind]-=item.Count;
                        CookedCount++;served=true;
                    }
            if(served)RefreshInventory();return served;
        }
        public bool SummonActiveRecipe()
        {
            if(PreparationSession.Tutorial || Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished || hand==null || hand.IsDealing)return false;
            return ServeStock(Object.FindFirstObjectByType<ConveyorController>());
        }
        public void ReturnIngredients()
        {
            if(PreparationSession.Tutorial || Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished || hand==null || hand.IsDealing)return;
            for(int i=0;i<ingredients.Length;i++)
                while(ingredients[i]>0 && hand.TryReturnCard((IngredientKind)i))ingredients[i]--;
            RefreshInventory();
        }

        public void ResetForNewGame()
        {
            System.Array.Clear(ingredients,0,ingredients.Length);
            CookedCount = 0;
            if (dishesLayer != null)
                foreach (Transform dish in dishesLayer)
                {
                    dish.gameObject.SetActive(false);
                    Destroy(dish.gameObject);
                }
            RefreshInventory();
        }

        private void RefreshInventory()
        {
            if (inventoryLabel != null)
            {
                var text = new System.Text.StringBuilder("BOX STOCK");
                for (int i=0;i<ingredients.Length;i++)
                    if (ingredients[i]>0) text.Append("\n").Append(IngredientRules.DisplayName((IngredientKind)i)).Append(" × ").Append(ingredients[i]);
                inventoryLabel.text = text.ToString();
            }
        }

        public bool TryDropOverlappingCard(IngredientCard card, Camera eventCamera)
        {
            if (!isActiveAndEnabled || card == null || !card.IsDragging) return false;
            var cardRect = (RectTransform)card.transform;
            var center = RectTransformUtility.WorldToScreenPoint(eventCamera, cardRect.TransformPoint(cardRect.rect.center));
            return RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, center, eventCamera) && Deposit(card);
        }

        public void Configure(IngredientCardHand owner, RectTransform output, Sprite tamago, Sprite maki, Sprite nigiri)
        {
            hand = owner; dishesLayer = output;
            tamagoNigiri = tamago; salmonMaki = maki; salmonNigiri = nigiri;
        }

        public void OnDrop(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || data.pointerDrag == null) return;
            var card = data.pointerDrag.GetComponent<IngredientCard>();
            if (card != null && card.IsDragging) Deposit(card);
            SetHighlight(false);
        }

        public bool Deposit(IngredientCard card)
        {
            if(!TutorialGuide.CanDeposit) return false;
            if (CustomerSystem.GameFinished) return false;
            if(Menu.GameplayPauseController.IsPaused)return false;
            // Never remove an ingredient if the stove output is not configured.
            if (hand == null || dishesLayer == null || (PreparationSession.Tutorial && (tamagoNigiri == null || salmonMaki == null || salmonNigiri == null)) ||
                card == null || !card.isActiveAndEnabled) return false;
            var kind = card.Ingredient;
            if(!PreparationSession.Tutorial && !ActiveLevel.Config.Allows(kind))return false;
            if (!hand.ConsumeCard(card)) return false;
            ingredients[(int)kind]++;
            Menu.UIAudioManager.PlayCardSummon();
            if(PreparationSession.Tutorial)CookAvailableRecipes();
            RefreshInventory();
            return true;
        }

        private void CookAvailableRecipes()
        {
            while (RecipeRules.TryCook(ingredients, out var dish))
                ShowDish((int)dish);
        }

        private void ShowDish(int recipe)
        {
            TutorialGuide.Cooked();
            dishesLayer.gameObject.SetActive(true);
            var dish = new GameObject(DishNames[recipe], typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CookedDish));
            dish.transform.SetParent(dishesLayer, false);
            var image = dish.GetComponent<UnityEngine.UI.Image>();
            image.sprite = recipe == 0 ? tamagoNigiri : recipe == 1 ? salmonMaki : salmonNigiri;
            if(recipe>=3)
            {
                image.sprite=null; image.color=new Color(1,.65f,.4f);
                var label=new GameObject("EbiLabel",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(dish.transform,false);
                ((RectTransform)label.transform).sizeDelta=new Vector2(145,110);
                var text=label.GetComponent<TMPro.TextMeshProUGUI>();text.text=recipe==3?"EBI\nNIGIRI":"MAGURO\nNIGIRI";text.font=TMPro.TMP_Settings.defaultFontAsset;text.fontSize=22;text.color=Color.black;text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;
            }
            image.preserveAspect = true;
            image.raycastTarget = true;
            dish.GetComponent<CookedDish>().Configure((DishKind)recipe, this);
            ((RectTransform)dish.transform).sizeDelta = new Vector2(150, 120);
            dish.transform.SetAsLastSibling();
            CookedCount++;
            ArrangeDishes();
            hand.SetCollapsed(true);
        }

        private void ArrangeDishes()
        {
            int count = dishesLayer.childCount;
            float step = count > 1 ? Mathf.Min(190, 900f / (count - 1)) : 0;
            for (int i = 0; i < count; i++)
                ((RectTransform)dishesLayer.GetChild(i)).anchoredPosition = new Vector2((i - (count - 1) * .5f) * step, 0);
        }

        public void RemoveServedDish(GameObject dish)
        {
            dish.SetActive(false);
            dish.transform.SetParent(null, false);
            Destroy(dish);
            ArrangeDishes();
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (data.pointerDrag != null && data.pointerDrag.GetComponent<IngredientCard>() != null) SetHighlight(true);
        }
        public void OnPointerExit(PointerEventData data) => SetHighlight(false);
        private void SetHighlight(bool active)
        {
            var image = GetComponent<UnityEngine.UI.Image>();
            if (image != null) image.color = active ? new Color(1, .87f, .6f) : Color.white;
        }
    }
}

