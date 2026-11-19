using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CookingCards.Editor
{
    public static class IngredientDrawSetup
    {
        [MenuItem("Tools/Cooking Cards/Set Up Draw Pile")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode first.");
            var canvas = GameObject.Find("IngredientCardCanvas");
            var hand = canvas != null ? canvas.transform.Find("IngredientHand") : null;
            if (hand == null) throw new System.InvalidOperationException("IngredientHand must exist first.");

            var back = AssetDatabase.LoadAllAssetsAtPath("Assets/Asset/back_card.png").OfType<Sprite>().FirstOrDefault();
            if (back == null)
            {
                var importer = AssetImporter.GetAtPath("Assets/Asset/back_card.png") as TextureImporter;
                if (importer == null) throw new System.InvalidOperationException("back_card.png not found.");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                back = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Asset/back_card.png");
            }
            Undo.RecordObject(hand, "Position hand above draw pile");
            var handRect = (RectTransform)hand;
            handRect.anchorMin = handRect.anchorMax = new Vector2(.5f, .64f);
            handRect.pivot = new Vector2(.5f, .5f);
            handRect.sizeDelta = new Vector2(1060, 300);
            handRect.anchoredPosition = Vector2.zero;
            foreach (Transform child in hand)
            {
                if (child.GetComponent<IngredientCard>() == null) continue;
                Undo.RecordObject(child, "Tighten card spacing");
                var rect = (RectTransform)child;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            }

            var pile = canvas.transform.Find("DrawCardButton");
            if (pile == null)
            {
                var go = new GameObject("DrawCardButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                Undo.RegisterCreatedObjectUndo(go, "Add draw pile");
                go.transform.SetParent(canvas.transform, false);
                pile = go.transform;
            }
            var pileRect = (RectTransform)pile;
            pileRect.anchorMin = pileRect.anchorMax = new Vector2(.5f, .17f);
            pileRect.pivot = new Vector2(.5f, .5f);
            pileRect.sizeDelta = new Vector2(110, 147);
            pileRect.anchoredPosition = Vector2.zero;
            var image = pile.GetComponent<UnityEngine.UI.Image>();
            image.sprite = back;
            image.preserveAspect = true;
            image.raycastTarget = true;
            var button = pile.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, .94f, .78f);
            colors.pressedColor = new Color(.78f, .68f, .49f);
            button.colors = colors;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };

            string[] names = { "avocado_card", "Eggs_card", "rice_card", "Salmon_card", "Nori_Card" };
            var prefabs = names.Select(name => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/IngredientCards/Prefabs/" + name + ".prefab").GetComponent<IngredientCard>()).ToArray();
            var controller = hand.GetComponent<IngredientCardHand>();
            if (controller == null) controller = Undo.AddComponent<IngredientCardHand>(hand.gameObject);
            Undo.RecordObject(controller, "Configure unlimited draws");
            controller.Configure(prefabs, pileRect);
            // Re-running setup must not add duplicate draw listeners.
            Undo.RecordObject(button, "Wire draw button");
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                if (button.onClick.GetPersistentTarget(i) == controller)
                    UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, controller.DrawCard);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(button);
            if (PrefabUtility.IsPartOfPrefabInstance(hand.gameObject))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(handRect);
                foreach (Transform child in hand) PrefabUtility.RecordPrefabInstancePropertyModifications(child);
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            Debug.Log("Draw pile ready: click back_card to draw unlimited random ingredients above it.");
        }
    }
}
