using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CookingCards.Editor
{
    public static class IngredientCardSetup
    {
        private const string Folder = "Assets/IngredientCards";
        [MenuItem("Tools/Cooking Cards/Set Up Ingredient Cards")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play mode before setup.");
            System.IO.Directory.CreateDirectory(Folder + "/Prefabs");
            AssetDatabase.Refresh();
            string[] names = { "avocado_card", "Eggs_card", "rice_card", "Salmon_card", "Nori_Card" };
            var hand = new GameObject("IngredientHand", typeof(RectTransform));
            var handRect = (RectTransform)hand.transform;
            handRect.anchorMin = handRect.anchorMax = new Vector2(.5f, 0);
            handRect.pivot = new Vector2(.5f, 0);
            handRect.sizeDelta = new Vector2(1120, 370);
            handRect.anchoredPosition = new Vector2(0, 35);
            for (int i = 0; i < names.Length; i++)
            {
                string path = "Assets/Asset/Card/" + names[i] + ".png";
                var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
                if (sprite == null) throw new System.InvalidOperationException("No sprite imported for " + path);
                var card = new GameObject(names[i], typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(IngredientCard));
                var rect = (RectTransform)card.transform;
                rect.sizeDelta = new Vector2(190, 254);
                var hit = card.GetComponent<UnityEngine.UI.Image>();
                hit.color = Color.clear;
                hit.raycastTarget = true;
                var visual = new GameObject("Artwork", typeof(RectTransform));
                visual.transform.SetParent(card.transform, false);
                var visualRect = (RectTransform)visual.transform;
                visualRect.anchorMin = Vector2.zero;
                visualRect.anchorMax = Vector2.one;
                visualRect.sizeDelta = Vector2.zero;
                var shadowObject = new GameObject("Shadow", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                shadowObject.transform.SetParent(visual.transform, false);
                var sr = (RectTransform)shadowObject.transform;
                sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one;
                sr.sizeDelta = Vector2.zero; sr.anchoredPosition = new Vector2(5, -8);
                var shadow = shadowObject.GetComponent<UnityEngine.UI.Image>();
                shadow.sprite = sprite; shadow.preserveAspect = true;
                shadow.color = new Color(0, 0, 0, .35f); shadow.raycastTarget = false;
                var faceObject = new GameObject("Face", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                faceObject.transform.SetParent(visual.transform, false);
                var fr = (RectTransform)faceObject.transform;
                fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.sizeDelta = Vector2.zero;
                var face = faceObject.GetComponent<UnityEngine.UI.Image>();
                face.sprite = sprite; face.preserveAspect = true; face.raycastTarget = false;
                card.GetComponent<IngredientCard>().Configure(visualRect);
                card.GetComponent<IngredientCard>().SetIngredient(IngredientMergeSetup.KindForName(names[i]));
                var prefab = PrefabUtility.SaveAsPrefabAsset(card, Folder + "/Prefabs/" + names[i] + ".prefab");
                Object.DestroyImmediate(card);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(hand.transform, false);
                var ir = (RectTransform)instance.transform;
                ir.anchoredPosition = new Vector2((i - 2) * 211, 165 - Mathf.Abs(i - 2) * 7);
                ir.localRotation = Quaternion.Euler(0, 0, (2 - i) * 3);
            }
            var handPrefab = PrefabUtility.SaveAsPrefabAsset(hand, Folder + "/Prefabs/IngredientHand.prefab");
            Object.DestroyImmediate(hand);
            var existing = GameObject.Find("IngredientCardCanvas");
            if (existing == null)
            {
                var canvasObject = new GameObject("IngredientCardCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasObject, "Set up ingredient cards");
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
                var handInstance = (GameObject)PrefabUtility.InstantiatePrefab(handPrefab);
                handInstance.transform.SetParent(canvasObject.transform, false);
            }
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("CardEventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(events, "Set up card events");
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Prefabs/IngredientHand.prefab");
            Debug.Log("Ingredient cards ready: 5 prefabs and IngredientHand. Hover to tilt; click to select.");
        }
    }
}
