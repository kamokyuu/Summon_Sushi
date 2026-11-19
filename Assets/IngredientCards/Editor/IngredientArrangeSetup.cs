using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CookingCards.Editor
{
    public static class IngredientArrangeSetup
    {
        [MenuItem("Tools/Cooking Cards/Set Up Arrange Button")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            var canvas = GameObject.Find("IngredientCardCanvas");
            var hand = canvas.GetComponentInChildren<IngredientCardHand>();
            var draw = (RectTransform)canvas.transform.Find("DrawCardButton");
            var paw = (RectTransform)canvas.transform.Find("PawHandToggle");
            var target = canvas.transform.Find("ArrangeCardsButton");
            if (target == null)
            {
                var go = new GameObject("ArrangeCardsButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                Undo.RegisterCreatedObjectUndo(go, "Add Arrange button");
                go.transform.SetParent(canvas.transform, false);
                target = go.transform;
                var labelObject = new GameObject("ModeLabel", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                labelObject.transform.SetParent(target, false);
                var labelRect = (RectTransform)labelObject.transform;
                labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(4, 2); labelRect.offsetMax = new Vector2(-4, -2);
                var text = labelObject.GetComponent<TMPro.TextMeshProUGUI>();
                text.font = TMPro.TMP_Settings.defaultFontAsset;
                text.fontSize = 17; text.alignment = TMPro.TextAlignmentOptions.Center;
                text.color = new Color(1, .9f, .65f); text.raycastTarget = false;
            }
            var rect = (RectTransform)target;
            rect.anchorMin = draw.anchorMin; rect.anchorMax = draw.anchorMax;
            rect.anchoredPosition = (draw.anchoredPosition + paw.anchoredPosition) * .5f;
            rect.sizeDelta = new Vector2(108, 58);
            var image = target.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(.22f, .105f, .045f, .97f); image.raycastTarget = true;
            var button = target.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                if (button.onClick.GetPersistentTarget(i) == hand) UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, hand.ToggleArrange);
            hand.ConfigureArrangeLabel(target.GetComponentInChildren<TMPro.TMP_Text>());
            EditorUtility.SetDirty(hand); EditorUtility.SetDirty(button);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            EditorSceneManager.SaveScene(canvas.scene);
        }
    }
}
