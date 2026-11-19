using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CookingCards.Editor
{
    public static class GameResultsSetup
    {
        [MenuItem("Tools/Cooking Cards/Set Up Ten Coin Goal")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            var canvas = GameObject.Find("IngredientCardCanvas");
            var system = canvas.GetComponentInChildren<CustomerSystem>();
            var overlay = canvas.transform.Find("GameResults");
            if (overlay == null)
            {
                var go = new GameObject("GameResults", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster), typeof(UnityEngine.UI.Image));
                Undo.RegisterCreatedObjectUndo(go, "Add end-game results");
                go.transform.SetParent(canvas.transform, false);
                overlay = go.transform;
                var dim = go.GetComponent<UnityEngine.UI.Image>();
                dim.color = new Color(.08f, .04f, .02f, .78f); dim.raycastTarget = true;
                var note = new GameObject("ResultsNote", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                note.transform.SetParent(overlay, false);
                ((RectTransform)note.transform).sizeDelta = new Vector2(520, 420);
                note.GetComponent<UnityEngine.UI.Image>().color = new Color(1, .95f, .82f);
                var text = new GameObject("ResultsText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                text.transform.SetParent(note.transform, false);
                var tr = (RectTransform)text.transform;
                tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
                tr.offsetMin = new Vector2(28, 24); tr.offsetMax = new Vector2(-28, -24);
                var label = text.GetComponent<TMPro.TextMeshProUGUI>();
                label.font = TMPro.TMP_Settings.defaultFontAsset; label.fontSize = 25;
                label.alignment = TMPro.TextAlignmentOptions.Center;
                label.color = new Color(.22f, .10f, .04f); label.raycastTarget = false;
                label.text = "<size=36><b>SHIFT COMPLETE</b></size>\n10 COINS\n\nSalmon Maki (0x)\nTamago Nigiri (0x)\nSalmon Nigiri (0x)\n\n10 served, 0 miss";
            }
            var rect = (RectTransform)overlay;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var top = overlay.GetComponent<Canvas>();
            top.overrideSorting = true; top.sortingOrder = 100;
            system.ConfigureResults(overlay.gameObject, overlay.GetComponentInChildren<TMPro.TMP_Text>(true));
            overlay.gameObject.SetActive(false);
            EditorUtility.SetDirty(system);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            EditorSceneManager.SaveScene(canvas.scene);
        }
    }
}
