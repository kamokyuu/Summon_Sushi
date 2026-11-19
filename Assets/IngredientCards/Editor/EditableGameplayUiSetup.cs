using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CookingCards.Editor
{
    public static class EditableGameplayUiSetup
    {
        [MenuItem("Tools/Cooking Cards/Organize Editable Gameplay UI")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
            var hand=Object.FindFirstObjectByType<IngredientCardHand>();
            var canvas=hand.GetComponentInParent<Canvas>();
            var root=canvas.transform;
            var knifeOwner=hand.GetComponent<CardKnifeTool>()??Undo.AddComponent<CardKnifeTool>(hand.gameObject);
            var knife=knifeOwner.EnsureTool();
            var stove=Object.FindFirstObjectByType<IngredientStove>();
            var controls=stove.GetComponent<SummonControls>()??Undo.AddComponent<SummonControls>(stove.gameObject);
            controls.EnsureButton();
            EditorUtility.SetDirty(controls);
            var tutorial=hand.GetComponent<TutorialGuide>()??Undo.AddComponent<TutorialGuide>(hand.gameObject);
            tutorial.EnsureUi();EditorUtility.SetDirty(tutorial);
            var customer=Object.FindFirstObjectByType<CustomerSystem>();
            var shift=customer.GetComponent<DayShiftController>()??Undo.AddComponent<DayShiftController>(customer.gameObject);
            var panel=Find(root,"DayShiftPanel");
            if(panel==null)
            {
                var go=new GameObject("DayShiftPanel",typeof(RectTransform),typeof(UnityEngine.UI.Image));
                go.transform.SetParent(root,false);panel=go.transform;
                var r=(RectTransform)panel;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,1);
                r.anchoredPosition=new Vector2(0,-15);r.sizeDelta=new Vector2(420,80);
                var theme=Resources.Load<GameUiTheme>("GameUiTheme");
                var image=go.GetComponent<UnityEngine.UI.Image>();image.sprite=theme!=null?theme.buttonSprite:null;image.type=UnityEngine.UI.Image.Type.Sliced;image.raycastTarget=false;
                var label=new GameObject("DayShiftHUD",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(panel,false);
                var tr=(RectTransform)label.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(8,4);tr.offsetMax=new Vector2(-8,-4);
                var text=label.GetComponent<TMPro.TextMeshProUGUI>();text.font=TMPro.TMP_Settings.defaultFontAsset;text.fontSize=24;
                text.color=GameUiTheme.Ink;text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;text.text="DAY 1   03:00\nNET ¥0 / ¥3000";
            }
            shift.ConfigureDisplay(panel.GetComponentInChildren<TMPro.TMP_Text>(true));EditorUtility.SetDirty(shift);
            panel.gameObject.SetActive(false);
            Find(root,"TutorialInstruction").gameObject.SetActive(false);
            Find(root,"SeafoodRecipeReference").gameObject.SetActive(false);
            var results=Find(root,"GameResults");
            if(results!=null)
            {
                var nav=results.GetComponent<Menu.ResultsNavigation>()??Undo.AddComponent<Menu.ResultsNavigation>(results.gameObject);
                nav.Show();
            }
            var layers=new[]{"Background","Customers","Conveyor","Food","Hand","Buttons","HUD","Equipment","Panels"};
            foreach(var name in layers)Layer(root,"UI_"+name);
            Move(root,"GameplayBackground","UI_Background");
            Move(root,"Customers","UI_Customers");Move(root,"Conveyor","UI_Conveyor");
            Move(root,"CookedDishes","UI_Food");
            Move(root,"IngredientHand","UI_Hand");Move(root,"CollapsedHandAnchor","UI_Hand");
            foreach(var name in new[]{"DrawCardButton","ArrangeCardsButton","PawHandToggle","ReturnIngredientsButton","IngredientStove"})Move(root,name,"UI_Buttons");
            foreach(var name in new[]{"CoinCounter","DayShiftPanel","TutorialInstruction","SeafoodRecipeReference"})Move(root,name,"UI_HUD");
            Move(root,"KnifeTool","UI_Equipment");
            foreach(var name in new[]{"RecipeJournal","PauseOverlay","GameResults"})Move(root,name,"UI_Panels");
            if(canvas.GetComponent<GameUiThemeRuntime>()==null)Undo.AddComponent<GameUiThemeRuntime>(canvas.gameObject);
            EditorUtility.SetDirty(knifeOwner);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveScene(canvas.gameObject.scene);
            Selection.activeGameObject=knife.gameObject;
        }
        private static Transform Find(Transform root,string name)
        {foreach(var item in root.GetComponentsInChildren<Transform>(true))if(item.name==name)return item;return null;}
        private static Transform Layer(Transform root,string name)
        {
            var found=root.Find(name);if(found!=null)return found;
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(root,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
        }
        private static void Move(Transform root,string name,string layer)
        {var item=Find(root,name);var parent=root.Find(layer);if(item!=null&&item.parent!=parent)Undo.SetTransformParent(item,parent,"Organize editable UI");}
    }
}
