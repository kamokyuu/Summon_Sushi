using System.Collections.Generic;
using UnityEngine;

namespace CookingCards
{
    /// <summary>Seven-card hand. Jam draws use weighted batches; legacy tutorial stock is kept separate.</summary>
    public sealed class IngredientCardHand : MonoBehaviour
    {
        [SerializeField] private IngredientCard[] cardPrefabs;
        [SerializeField] private RectTransform drawPile;
        [SerializeField] private float spacing = 135f;
        [SerializeField] private float maximumWidth = 850f;
        [SerializeField,Range(.2f,1.5f)] private float expandedCardScale = .65f;
        [SerializeField] private RectTransform stackAnchor;
        [SerializeField] private float stackScale = .45f;
        [SerializeField, Min(.1f)] private float openingDealInterval = .25f;
        [SerializeField, Min(0)] private float openingDealDelay = .4f;
        [SerializeField, Min(0)] private int startingCardCount = 5;
        [SerializeField] private TMPro.TMP_Text arrangeModeLabel;
        private CanvasGroup handGroup;
        private readonly List<RectTransform> cards = new List<RectTransform>();
        private readonly List<RectTransform> drawOrder = new List<RectTransform>();
        private readonly List<IngredientKind> freeDrawBag = new List<IngredientKind>();
        public const int HandLimit = 7;
        private int CurrentHandLimit => HandLimit;
        public int CardCount => cards.Count;
        public bool CanDraw => TutorialGuide.CanDraw && !Menu.GameplayPauseController.IsPaused && !CustomerSystem.GameFinished && !IsDealing && CardCount < CurrentHandLimit && (!PreparationSession.Tutorial || PreparationSession.Remaining>0);
        public bool IsCollapsed { get; private set; }
        public bool IsDealing { get; private set; }
        public bool IsAutoArrange { get; private set; }

        private void RefreshDrawButton()
        {
            if (drawPile == null) return;
            var button = drawPile.GetComponent<UnityEngine.UI.Button>();
            if (button != null) button.interactable = CanDraw;
        }

        public void ConfigureArrangeLabel(TMPro.TMP_Text label)
        {
            arrangeModeLabel = label;
            RefreshArrangeLabel();
        }

        public void ToggleArrange()
        {
            IsAutoArrange = !IsAutoArrange;
            if (IsAutoArrange) SortCards();
            else RestoreDrawOrder();
            RefreshArrangeLabel();
            PlayPaperFlip();
        }

        private void PlayPaperFlip()
        {
            if (drawPile == null) return;
            var source = drawPile.GetComponent<AudioSource>();
            if (source != null && source.clip != null) source.PlayOneShot(source.clip);
        }

        private void RestoreDrawOrder()
        {
            drawOrder.RemoveAll(card => card == null || !card.gameObject.activeSelf);
            cards.Clear();
            cards.AddRange(drawOrder);
            for (int i = 0; i < cards.Count; i++) cards[i].SetSiblingIndex(i);
        }

        private void RefreshArrangeLabel()
        {
            if (arrangeModeLabel != null) arrangeModeLabel.text = "ARRANGE\n" + (IsAutoArrange ? "ON" : "OFF");
        }

        private void SortCards()
        {
            cards.RemoveAll(card => card == null);
            // Stable insertion sort keeps existing cards of the same type in order.
            for (int i = 1; i < cards.Count; i++)
            {
                var card = cards[i];
                var kind = card.GetComponent<IngredientCard>().Ingredient;
                int j = i - 1;
                while (j >= 0 && cards[j].GetComponent<IngredientCard>().Ingredient > kind)
                {
                    cards[j + 1] = cards[j];
                    j--;
                }
                cards[j + 1] = card;
            }
            for (int i = 0; i < cards.Count; i++) cards[i].SetSiblingIndex(i);
        }

        public void ConfigureStack(RectTransform anchor) => stackAnchor = anchor;

        public void ToggleHand() => SetCollapsed(!IsCollapsed);

        public void SetCollapsed(bool collapsed)
        {
            IsCollapsed = collapsed;
            if (handGroup == null) handGroup = GetComponent<CanvasGroup>();
            if (handGroup == null) handGroup = gameObject.AddComponent<CanvasGroup>();
            handGroup.blocksRaycasts = !IsCollapsed && !IsDealing;
            handGroup.interactable = !IsCollapsed && !IsDealing;
            foreach (var rect in cards)
                if (rect != null) rect.GetComponent<IngredientCard>().ResetInteraction();
        }

        public void Configure(IngredientCard[] prefabs, RectTransform pile)
        {
            cardPrefabs = prefabs;
            drawPile = pile;
            CollectCards();
            if (IsAutoArrange) SortCards();
            Arrange(true);
            RefreshDrawButton();
        }

        private System.Collections.IEnumerator Start()
        {
            if (!IsDealing) yield break;
            Canvas.ForceUpdateCanvases();
            // No cards exist until their individual deal starts.
            yield return new WaitForSecondsRealtime(openingDealDelay);
            for (int i = 0; i < (PreparationSession.Tutorial ? Mathf.Min(!PreparationSession.BossTutorial?2:startingCardCount, HandLimit) : HandLimit); i++)
            {
                int before = cards.Count;
                SpawnRandomCard();
                if (cards.Count > before) PlayPaperFlip();
                yield return new WaitForSecondsRealtime(openingDealInterval);
            }
            yield return new WaitForSecondsRealtime(.65f);
            IsDealing = false;
            handGroup.blocksRaycasts = !IsCollapsed;
            handGroup.interactable = !IsCollapsed;
            RefreshDrawButton();
        }

        private void Awake()
        {
            if((!PreparationSession.Tutorial || PreparationSession.KnifeTutorial || PreparationSession.BossTutorial) && GetComponent<CardKnifeTool>()==null) gameObject.AddComponent<CardKnifeTool>();
            if(PreparationSession.Tutorial && !PreparationSession.BossTutorial && GetComponent<TutorialGuide>()==null) gameObject.AddComponent<TutorialGuide>();
            handGroup = GetComponent<CanvasGroup>();
            if (handGroup == null) handGroup = gameObject.AddComponent<CanvasGroup>();
            CollectCards();
            RefreshArrangeLabel();
            if (drawPile != null && cardPrefabs != null && cardPrefabs.Length > 0 && startingCardCount > 0)
            {
                foreach (var rect in cards)
                {
                    rect.gameObject.SetActive(false);
                    Destroy(rect.gameObject);
                }
                cards.Clear();
                drawOrder.Clear();
                IsDealing = true;
                handGroup.blocksRaycasts = false;
                handGroup.interactable = false;
            }
            else Arrange(true);
            RefreshDrawButton();
        }

        private void CollectCards()
        {
            cards.Clear();
            foreach (Transform child in transform)
                if (child.gameObject.activeSelf && child.GetComponent<IngredientCard>() != null)
                    cards.Add((RectTransform)child);
            drawOrder.RemoveAll(card => card == null || !cards.Contains(card));
            foreach (var card in cards)
                if (!drawOrder.Contains(card)) drawOrder.Add(card);
        }

        public void DrawCard()
        {
            if (!CanDraw) return;
            SpawnRandomCard();
            TutorialGuide.Drew();
        }

        private void SpawnRandomCard()
        {
            if (CustomerSystem.GameFinished) return;
            if (cards.Count >= CurrentHandLimit || cardPrefabs == null || cardPrefabs.Length == 0) return;
            // Sampling with replacement means drawing never empties the pile.
            IngredientCard prefab;
            if(PreparationSession.Tutorial)
            {
                if(!PreparationSession.TryDraw(out var kind)) return;
                prefab=System.Array.Find(cardPrefabs,p=>p!=null && p.Ingredient==kind);
            }
            else
            {
                foreach(var active in GameJamRules.Ingredients)
                    if(System.Array.Find(cardPrefabs,p=>p!=null && p.Ingredient==active)==null)
                    { Debug.LogError("Missing active ingredient prefab: "+active,this); return; }
                if(freeDrawBag.Count==0)
                {
                    freeDrawBag.AddRange(GameJamRules.CreateDrawBatch());
                }
                int index=freeDrawBag.Count-1;var kind=freeDrawBag[index];
                prefab=System.Array.Find(cardPrefabs,p=>p!=null && p.Ingredient==kind);
                if(prefab==null)return;
                freeDrawBag.RemoveAt(index);
            }
            if (prefab == null) return;
            var card = Instantiate(prefab, transform, false);
            card.name = prefab.name;
            var rect = (RectTransform)card.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.localScale = Vector3.one * .58f;
            if (drawPile != null) rect.position = drawPile.position;
            cards.Add(rect);
            drawOrder.Add(rect);
            // The newest card is rendered on top of overlapping cards.
            rect.SetAsLastSibling();
            if (IsAutoArrange) SortCards();
            RefreshDrawButton();
        }

        public static bool TryGetCombination(IngredientKind first, IngredientKind second, out IngredientKind result)
        {
            return IngredientRules.TryMerge(first, second, out result);
        }
        public bool TryReturnCard(IngredientKind kind)
        {
            if(cards.Count>=CurrentHandLimit || IsDealing)return false;
            var prefab=System.Array.Find(cardPrefabs,p=>p!=null && p.Ingredient==kind);
            if(prefab==null)return false;
            var card=Instantiate(prefab,transform,false);
            var rect=(RectTransform)card.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.localScale=Vector3.one*.58f;
            cards.Add(rect);drawOrder.Add(rect);if(IsAutoArrange)SortCards();SetCollapsed(false);RefreshDrawButton();return true;
        }

        public bool ConsumeCard(IngredientCard card)
        {
            if (card == null || !card.isActiveAndEnabled || !cards.Remove((RectTransform)card.transform)) return false;
            drawOrder.Remove((RectTransform)card.transform);
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
            RefreshDrawButton();
            return true;
        }

        public bool TrySplitCard(IngredientCard card)
        {
            if(Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished || IsDealing || IsCollapsed || cards.Count>=CurrentHandLimit || card==null || !cards.Contains((RectTransform)card.transform) || !IngredientRules.TrySplit(card.Ingredient,out var result)) return false;
            var prefab=System.Array.Find(cardPrefabs,p=>p!=null && p.Ingredient==result);
            if(prefab==null)return false;
            var position=card.transform.position;
            if(!ConsumeCard(card))return false;
            for(int i=0;i<2;i++)
            {
                var output=Instantiate(prefab,transform,false);output.name=prefab.name;output.transform.position=position;
                var rect=(RectTransform)output.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
                cards.Add(rect);drawOrder.Add(rect);
            }
            if(IsAutoArrange)SortCards();
            TutorialGuide.Cut();RefreshDrawButton();return true;
        }

        public bool TryCombine(IngredientCard source, IngredientCard target)
        {
            if(Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished || IsDealing)return false;
            if (source == null || target == null || source == target || !source.isActiveAndEnabled || !target.isActiveAndEnabled)
                return false;
            var sourceRect = (RectTransform)source.transform;
            var targetRect = (RectTransform)target.transform;
            if (!cards.Contains(sourceRect) || !cards.Contains(targetRect) ||
                !TryGetCombination(source.Ingredient, target.Ingredient, out var result)) return false;
            IngredientCard resultPrefab = null;
            if (cardPrefabs != null)
                foreach (var prefab in cardPrefabs)
                    if (prefab != null && prefab.Ingredient == result) { resultPrefab = prefab; break; }
            // Validate the recipe output before consuming either input.
            if (resultPrefab == null) return false;
            if(PreparationSession.Tutorial && !PreparationSession.BossTutorial && TutorialGuide.Step==1) return false;
            TutorialGuide.Merged();
            var output = Instantiate(resultPrefab, transform, false);
            output.name = resultPrefab.name;
            var outputRect = (RectTransform)output.transform;
            outputRect.anchorMin = outputRect.anchorMax = new Vector2(.5f, .5f);
            outputRect.position = targetRect.position;
            outputRect.localRotation = targetRect.localRotation;
            int index = cards.IndexOf(targetRect);
            if (cards.IndexOf(sourceRect) < index) index--;
            cards.Remove(sourceRect);
            cards.Remove(targetRect);
            cards.Insert(index, outputRect);
            // A merged card replaces its target in the original, unsorted hand.
            int originalIndex = drawOrder.IndexOf(targetRect);
            if (drawOrder.IndexOf(sourceRect) < originalIndex) originalIndex--;
            drawOrder.Remove(sourceRect);
            drawOrder.Remove(targetRect);
            drawOrder.Insert(Mathf.Clamp(originalIndex, 0, drawOrder.Count), outputRect);
            // Disable immediately: Unity defers Destroy until the end of the frame.
            source.gameObject.SetActive(false);
            target.gameObject.SetActive(false);
            Destroy(source.gameObject);
            Destroy(target.gameObject);
            for (int i = 0; i < cards.Count; i++) cards[i].SetSiblingIndex(i);
            if (IsAutoArrange) SortCards();
            RefreshDrawButton();
            return true;
        }

        private void LateUpdate() { if (!CustomerSystem.GameFinished) Arrange(false); }

        private void Arrange(bool instant)
        {
            cards.RemoveAll(card => card == null);
            float step = cards.Count > 1 ? Mathf.Min(spacing, maximumWidth / (cards.Count - 1)) : 0;
            float middle = (cards.Count - 1) * .5f;
            float blend = instant ? 1 : 1 - Mathf.Exp(-14 * Time.unscaledDeltaTime);
            Vector2 stackPosition = stackAnchor != null ? (Vector2)transform.InverseTransformPoint(stackAnchor.position) : new Vector2(-270, -210);
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].GetComponent<IngredientCard>().IsDragging) continue;
                float offset = i - middle;
                float normalized = middle > 0 ? offset / middle : 0;
                var position = new Vector2(offset * step, -Mathf.Abs(normalized) * 12);
                float angle = -normalized * 6;
                if (IsCollapsed)
                {
                    // Keep the stack compact even after unlimited draws.
                    float depth = cards.Count > 1 ? i / (float)(cards.Count - 1) : 0;
                    position = stackPosition + new Vector2(depth * 16, depth * 12);
                    angle = Mathf.Lerp(-4, 2, depth);
                }
                cards[i].anchoredPosition = Vector2.Lerp(cards[i].anchoredPosition, position, blend);
                cards[i].localRotation = Quaternion.Slerp(cards[i].localRotation, Quaternion.Euler(0, 0, angle), blend);
                cards[i].localScale = Vector3.Lerp(cards[i].localScale, Vector3.one * (IsCollapsed ? stackScale : expandedCardScale), blend);
            }
        }
    }
}
