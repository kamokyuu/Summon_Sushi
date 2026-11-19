using UnityEngine;
using UnityEngine.EventSystems;

namespace CookingCards
{
    // Explicit IDs preserve existing prefab and scene identities.
    public enum IngredientKind { Rice = 0, Nori = 1, Eggs = 2, Avocado = 3, Salmon = 4, Cucumber = 5, Tobiko = 6, Ikura = 7, Shrimp = 8, Tuna = 9, Mushroom = 10, Shiitake = 11, Matsutake = 12, Chicken = 13, Unagi = 14, Beef = 15 }

    /// <summary>Reusable card presentation. Keep the hit area still while the artwork moves.</summary>
    public sealed class IngredientCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private IngredientKind ingredient;
        [SerializeField] private RectTransform artwork;
        [SerializeField] private float hoverScale = 1.08f;
        [SerializeField] private float selectionLift = 35f;
        private bool hovered;
        private bool selected;
        private Vector2 pointer;
        private CanvasGroup dragGroup;
        private Vector2 dragOffset;
        private int previousSibling;
        private bool suppressClick;
        public bool IsSelected => selected;
        public bool IsDragging { get; private set; }
        public IngredientKind Ingredient => ingredient;
        public event System.Action<IngredientCard, bool> SelectionChanged;

        public void Configure(RectTransform visual) => artwork = visual;
        public void SetIngredient(IngredientKind kind) => ingredient = kind;
        public void ResetInteraction()
        {
            hovered = false; selected = false; pointer = Vector2.zero; IsDragging = false;
            if (dragGroup != null) dragGroup.blocksRaycasts = true;
        }
        private void Awake()
        {
            dragGroup = GetComponent<CanvasGroup>();
            if (dragGroup == null) dragGroup = gameObject.AddComponent<CanvasGroup>();
        }
        public void OnPointerDown(PointerEventData e) => suppressClick = false;
        public void OnBeginDrag(PointerEventData e)
        {
            if(Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished)return;
            if (e.button != PointerEventData.InputButton.Left) return;
            var hand = GetComponentInParent<IngredientCardHand>();
            if (hand == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform.parent, e.position, e.pressEventCamera, out var local)) return;
            IsDragging = true;
            suppressClick = true;
            hovered = false;
            pointer = Vector2.zero;
            dragOffset = (Vector2)transform.localPosition - local;
            previousSibling = transform.GetSiblingIndex();
            transform.SetAsLastSibling();
            // Let the raycaster find the card underneath the dragged artwork.
            dragGroup.blocksRaycasts = false;
        }
        public void OnDrag(PointerEventData e)
        {
            if (!IsDragging) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent,
                e.position, e.pressEventCamera, out var local))
                transform.localPosition = new Vector3(local.x + dragOffset.x, local.y + dragOffset.y, 0);
        }
        public void OnEndDrag(PointerEventData e)
        {
            if (!IsDragging) return;
            // A card grabbed near its edge can cover the box while the cursor
            // is outside it. Accept its visible center as a drop as well.
            foreach (var stove in Object.FindObjectsByType<IngredientStove>(FindObjectsSortMode.None))
                if (stove.TryDropOverlappingCard(this, e.pressEventCamera)) return;
            IsDragging = false;
            dragGroup.blocksRaycasts = true;
            transform.SetSiblingIndex(previousSibling);
        }
        public void OnDrop(PointerEventData e)
        {
            if (e.pointerDrag == null || e.button != PointerEventData.InputButton.Left) return;
            var source = e.pointerDrag.GetComponent<IngredientCard>();
            var hand = GetComponentInParent<IngredientCardHand>();
            if (source != null && source.IsDragging && hand != null) hand.TryCombine(source, this);
        }
        public void OnPointerEnter(PointerEventData e) { hovered = true; OnPointerMove(e); }
        public void OnPointerExit(PointerEventData e) { hovered = false; pointer = Vector2.zero; }
        public void OnPointerMove(PointerEventData e)
        {
            if (artwork == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var local);
            var size = ((RectTransform)transform).rect.size;
            pointer = new Vector2(Mathf.Clamp(local.x / Mathf.Max(1, size.x), -.5f, .5f), Mathf.Clamp(local.y / Mathf.Max(1, size.y), -.5f, .5f));
        }
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || suppressClick || IsDragging) return;
            selected = !selected;
            SelectionChanged?.Invoke(this, selected);
        }
        private void OnDisable()
        {
            ResetInteraction();
        }
        private void Update()
        {
            if (artwork == null) return;
            float blend = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
            float lift = (selected ? selectionLift : 0) + (hovered ? 14 : 0);
            artwork.anchoredPosition = Vector2.Lerp(artwork.anchoredPosition, new Vector2(0, lift), blend);
            artwork.localScale = Vector3.Lerp(artwork.localScale, Vector3.one * (hovered ? hoverScale : 1), blend);
            var rotation = Quaternion.Euler(hovered ? -pointer.y * 16 : 0, hovered ? pointer.x * 16 : 0, hovered ? -pointer.x * 5 : 0);
            artwork.localRotation = Quaternion.Slerp(artwork.localRotation, rotation, blend);
        }
    }
}
