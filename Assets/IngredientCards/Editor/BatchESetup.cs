using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CookingCards.Editor
{
    public static class BatchESetup
    {
        [InitializeOnLoadMethod]private static void Schedule()=>EditorApplication.delayCall+=Apply;
        [MenuItem("Tools/Cooking Cards/Apply Batch E")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)return;BatchDSetup.Apply();
            for(int n=1;n<=3;n++)
            {
                var config=Resources.Load<LevelConfig>("Levels/Level0"+n);if(config.batchEVersion>0)continue;
                config.pickyCustomerChance=n==1?0:n==2?.2f:.25f;config.hungryCustomerChance=n==1?0:n==2?.2f:.35f;
                if(n==3)
                {
                    var maguro=LevelRecipe.From(config.Recipes.First(r=>r.Id=="maguro_nigiri"));maguro.grade=RecipeGrade.Rare;
                    config.activeRecipes=config.activeRecipes.Where(r=>r.grade==RecipeGrade.Common||r.id=="avocado_maki").Concat(new[]{
                        Recipe("salmon_avocado_roll","Salmon Avocado Roll",IngredientKind.Salmon),maguro,Recipe("tamago_avocado_roll","Tamago Avocado Roll",IngredientKind.Eggs)}).ToArray();
                    config.activeIngredients=config.activeRecipes.SelectMany(r=>r.ingredients).Select(a=>a.ingredient).Distinct().ToArray();
                    config.riceDemandMultiplier=config.noriDemandMultiplier=1;config.premiumDemandMultiplier=1.25f;
                    config.foodVisuals=config.activeRecipes.Select(r=>new RecipeFoodVisual{recipeId=r.id,sprite=BatchDSetup.ExactArtwork(r.id)}).ToArray();
                }
                config.batchEVersion=1;config.InvalidateRecipes();config.ValidateConfiguration();EditorUtility.SetDirty(config);
            }
            AssetDatabase.SaveAssets();
        }
        private static LevelRecipe Recipe(string id,string title,IngredientKind topping)=>LevelRecipe.From(new SushiRecipe(id,title,RecipeGrade.Rare,RecipeFamily.Maki,new IngredientAmount(IngredientKind.Rice,1),new IngredientAmount(IngredientKind.Nori,1),new IngredientAmount(IngredientKind.Avocado,1),new IngredientAmount(topping,1)));
        [MenuItem("Tools/Cooking Cards/Polish Gameplay HUD")]
        public static void PolishScene()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode.");
            var hand=Object.FindFirstObjectByType<IngredientCardHand>();var canvas=hand.GetComponentInParent<Canvas>();
            var feedback=canvas.GetComponent<GameplayHudFeedback>()??canvas.gameObject.AddComponent<GameplayHudFeedback>();var theme=Resources.Load<GameUiTheme>("GameUiTheme");
            feedback.panelSprite=theme.buttonSprite;feedback.preferenceBubbleSprite=theme.buttonSprite;
            foreach(var root in canvas.GetComponentsInChildren<Transform>(true))
            {
                if(root.name.EndsWith("Panel"))
                {
                    var image=root.GetComponent<UnityEngine.UI.Image>();if(image!=null){image.sprite=feedback.panelSprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=GameUiTheme.Paper;image.raycastTarget=false;}
                }
            }
            foreach(var text in canvas.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if(text.GetComponentInParent<IngredientCard>()!=null)continue;
                text.color=GameplayHudFeedback.Ink;text.outlineWidth=0;text.raycastTarget=false;if(theme.font!=null)text.font=theme.font;
            }
            var coins=Find(canvas.transform,"CoinCounter");if(coins!=null)coins.gameObject.SetActive(false);
            var panel=(RectTransform)Find(canvas.transform,"DayShiftPanel");panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(1,1);panel.anchoredPosition=new Vector2(-20,-20);panel.sizeDelta=new Vector2(300,76);
            var revenue=panel.GetComponentInChildren<TMPro.TMP_Text>();var rect=(RectTransform)revenue.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(48,8);rect.offsetMax=new Vector2(-12,-8);revenue.fontSize=20;revenue.alignment=TMPro.TextAlignmentOptions.MidlineLeft;
            var coin=Find(panel,"RevenueCoin") as RectTransform;if(coin==null){var go=new GameObject("RevenueCoin",typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(panel,false);coin=(RectTransform)go.transform;}
            coin.anchorMin=coin.anchorMax=new Vector2(0,.5f);coin.anchoredPosition=new Vector2(26,0);coin.sizeDelta=new Vector2(30,30);coin.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;coin.GetComponent<UnityEngine.UI.Image>().enabled=feedback.coinSprite!=null;
            if(coin.Find("CoinSymbol")==null){var symbol=new GameObject("CoinSymbol",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));symbol.transform.SetParent(coin,false);var tmp=symbol.GetComponent<TMPro.TMP_Text>();tmp.font=theme.font??TMPro.TMP_Settings.defaultFontAsset;tmp.text="¥";tmp.fontSize=28;tmp.color=GameplayHudFeedback.Ink;tmp.alignment=TMPro.TextAlignmentOptions.Center;tmp.raycastTarget=false;((RectTransform)symbol.transform).sizeDelta=new Vector2(30,30);}
            feedback.Configure(revenue,panel,coin);
            var book=(RectTransform)Find(canvas.transform,"JournalButton");book.sizeDelta=new Vector2(52,52);
            foreach(var label in book.GetComponentsInChildren<TMPro.TMP_Text>(true))label.gameObject.SetActive(false);
            var existing=book.Find("ButtonIcon");if(existing==null){var go=new GameObject("ButtonIcon",typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(book,false);existing=go.transform;}
            var icon=existing.GetComponent<UnityEngine.UI.Image>();icon.raycastTarget=false;icon.enabled=feedback.recipeBookSprite!=null;((RectTransform)existing).sizeDelta=new Vector2(34,30);feedback.ConfigureBook(icon);
            var glyph=book.Find("BookGlyph");if(glyph==null)
            {
                var go=new GameObject("BookGlyph",typeof(RectTransform));go.transform.SetParent(book,false);glyph=go.transform;
                for(int side=-1;side<=1;side+=2){var page=new GameObject("Page",typeof(RectTransform),typeof(UnityEngine.UI.Image));page.transform.SetParent(glyph,false);var pr=(RectTransform)page.transform;pr.anchoredPosition=new Vector2(side*9,0);pr.sizeDelta=new Vector2(16,26);page.GetComponent<UnityEngine.UI.Image>().color=GameplayHudFeedback.Ink;page.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;var inner=new GameObject("Paper",typeof(RectTransform),typeof(UnityEngine.UI.Image));inner.transform.SetParent(page.transform,false);((RectTransform)inner.transform).sizeDelta=new Vector2(12,21);inner.GetComponent<UnityEngine.UI.Image>().color=GameUiTheme.Paper;inner.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;}
            }
            var customer=Object.FindFirstObjectByType<CustomerSystem>();if(customer.GetComponent<ConveyorCustomers>()==null)customer.gameObject.AddComponent<ConveyorCustomers>();
            var exchange=(RectTransform)Find(canvas.transform,"ExchangeCardButton");exchange.anchorMin=exchange.anchorMax=exchange.pivot=new Vector2(0,1);exchange.anchoredPosition=new Vector2(84,-20);exchange.sizeDelta=new Vector2(136,52);
            Position((RectTransform)Find(canvas.transform,"DrawCardButton"),new Vector2(-550,-225),new Vector2(79,105));
            Position((RectTransform)Find(canvas.transform,"PawHandToggle"),new Vector2(-608,-326),new Vector2(40,44));
            Position((RectTransform)Find(canvas.transform,"ArrangeCardsButton"),new Vector2(-530,-326),new Vector2(100,44));
            Position((RectTransform)Find(canvas.transform,"ReturnIngredientsButton"),new Vector2(550,-326),new Vector2(136,44));
            foreach(var name in new[]{"ExchangeCardButton","ArrangeCardsButton","ReturnIngredientsButton"})
            {
                var button=Find(canvas.transform,name);var image=button.GetComponent<UnityEngine.UI.Image>();if(image!=null){image.sprite=feedback.panelSprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=GameUiTheme.Paper;}
                foreach(var text in button.GetComponentsInChildren<TMPro.TMP_Text>(true)){text.enableAutoSizing=true;text.fontSizeMin=12;text.fontSizeMax=17;}
            }
            var cooldown=Find(canvas.transform,"DrawCooldown");if(cooldown!=null){((RectTransform)cooldown).sizeDelta=new Vector2(0,18);cooldown.GetComponent<TMPro.TMP_Text>().fontSize=14;}
            var stove=Object.FindFirstObjectByType<IngredientStove>();var notes=(TMPro.TMP_Text)new SerializedObject(stove).FindProperty("inventoryLabel").objectReferenceValue;
            var inventory=(RectTransform)notes.transform.parent;inventory.SetParent(Find(canvas.transform,"UI_HUD"),false);inventory.anchorMin=inventory.anchorMax=inventory.pivot=new Vector2(1,1);inventory.anchoredPosition=new Vector2(-20,-120);inventory.sizeDelta=new Vector2(200,190);
            var background=inventory.GetComponent<UnityEngine.UI.Image>();if(background!=null){background.sprite=feedback.panelSprite;background.type=UnityEngine.UI.Image.Type.Sliced;background.color=GameUiTheme.Paper;background.raycastTarget=false;}
            var nr=(RectTransform)notes.transform;nr.anchorMin=Vector2.zero;nr.anchorMax=Vector2.one;nr.offsetMin=new Vector2(12,12);nr.offsetMax=new Vector2(-12,-12);notes.fontSize=16;notes.alignment=TMPro.TextAlignmentOptions.TopLeft;
            feedback.ConfigureNotes(notes,inventory,(RectTransform)Find(canvas.transform,"ReturnIngredientsButton"));
            EditorUtility.SetDirty(feedback);EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);EditorSceneManager.SaveScene(canvas.gameObject.scene);
        }
        private static void Position(RectTransform rect,Vector2 center,Vector2 size){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=center;rect.sizeDelta=size;}
        private static Transform Find(Transform parent,string name)=>parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
    }
}
