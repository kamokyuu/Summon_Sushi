using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CookingCards.Editor
{
    public static class VerticalSliceBatchSetup
    {
        [InitializeOnLoadMethod] private static void Schedule()=>EditorApplication.delayCall+=CreateAssets;
        [MenuItem("Tools/Cooking Cards/Set Up Vertical Slice Batch A")]
        public static void CreateAssets()
        {
            LevelConfigSetup.CreateAssets();
            var one=Resources.Load<LevelConfig>("Levels/Level01");
            var two=LoadOrCreate("Level02");var three=LoadOrCreate("Level03");
            if(two.verticalSliceBatchVersion==0)
            {
                two.activeRecipes=new[]{one.activeRecipes[0],one.activeRecipes[1],Recipe("avocado_maki","Avocado Maki",RecipeGrade.Uncommon,RecipeFamily.Maki,IngredientKind.Rice,IngredientKind.Nori,IngredientKind.Avocado),Recipe("maguro_nigiri","Maguro Nigiri",RecipeGrade.Uncommon,RecipeFamily.Nigiri,IngredientKind.Rice,IngredientKind.Tuna)};
                Setup(two,210,1200,false);two.nextLevel=three;EditorUtility.SetDirty(two);
            }
            if(three.verticalSliceBatchVersion==0)
            {
                var list=new List<LevelRecipe>(two.activeRecipes);
                list.Add(Recipe("salmon_nigiri","Salmon Nigiri",RecipeGrade.Rare,RecipeFamily.Nigiri,IngredientKind.Rice,IngredientKind.Salmon,IngredientKind.Salmon));
                list.Add(Recipe("tuna_avocado_maki","Tuna Avocado Maki",RecipeGrade.Rare,RecipeFamily.Maki,IngredientKind.Rice,IngredientKind.Tuna,IngredientKind.Nori,IngredientKind.Avocado));
                three.activeRecipes=list.ToArray();Setup(three,240,1600,true);EditorUtility.SetDirty(three);
            }
            if(one.verticalSliceBatchVersion==0){Setup(one,180,900,false);one.nextLevel=two;EditorUtility.SetDirty(one);}
            one.ValidateConfiguration();two.ValidateConfiguration();three.ValidateConfiguration();AssetDatabase.SaveAssets();
        }
        private static LevelConfig LoadOrCreate(string name)
        {string path="Assets/Resources/Levels/"+name+".asset";var config=AssetDatabase.LoadAssetAtPath<LevelConfig>(path);if(config==null){config=ScriptableObject.CreateInstance<LevelConfig>();AssetDatabase.CreateAsset(config,path);}return config;}
        private static LevelRecipe Recipe(string id,string title,RecipeGrade grade,RecipeFamily family,params IngredientKind[] kinds)
        {
            var counts=new Dictionary<IngredientKind,int>();foreach(var kind in kinds){if(!counts.ContainsKey(kind))counts[kind]=0;counts[kind]++;}
            var amounts=new List<IngredientAmount>();foreach(var pair in counts)amounts.Add(new IngredientAmount(pair.Key,pair.Value));
            return LevelRecipe.From(new SushiRecipe(id,title,grade,family,amounts.ToArray()));
        }
        private static void Setup(LevelConfig config,float duration,int target,bool advanced)
        {
            var ingredients=new List<IngredientKind>();var grades=new Dictionary<RecipeGrade,float>();
            foreach(var recipe in config.activeRecipes)
            {
                if(!grades.ContainsKey(recipe.grade))grades[recipe.grade]=0;grades[recipe.grade]+=recipe.demandWeight;
                foreach(var amount in recipe.ingredients)if(!ingredients.Contains(amount.ingredient))ingredients.Add(amount.ingredient);
            }
            config.activeIngredients=ingredients.ToArray();var weights=new List<PlateSpawnWeight>();foreach(var pair in grades)weights.Add(new PlateSpawnWeight{grade=pair.Key,weight=pair.Value});config.plateWeights=weights.ToArray();
            config.drawMode=IngredientDrawMode.RecipeDemandShuffleBag;config.shuffleBagSize=20;config.manualDrawCooldownSeconds=3;config.exchangeEnabled=true;config.exchangeCooldownSeconds=3;
            config.initialHandSize=5;config.maximumHandCapacity=7;config.drawInterval=.25f;config.plateSpawnInterval=advanced?10:config.activeRecipes.Length==4?9:8;
            config.durationSeconds=duration;config.targetYen=target;config.mergeEnabled=advanced;config.knifeEnabled=advanced;config.initialKnifeCharges=advanced?3:0;
            config.comboEnabled=config.powerUpsEnabled=config.bossCustomersEnabled=false;config.ingredientBalanceVersion=2;config.verticalSliceBatchVersion=1;config.InvalidateRecipes();
        }
        [MenuItem("Tools/Cooking Cards/Add Card Exchange UI")]
        public static void AddExchangeUi()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play mode first.");
            var hand=Object.FindFirstObjectByType<IngredientCardHand>();var canvas=hand.GetComponentInParent<Canvas>();var draw=GameObject.Find("DrawCardButton");
            Transform existing=null;foreach(var child in canvas.GetComponentsInChildren<Transform>(true))if(child.name=="ExchangeCardButton")existing=child;
            if(existing==null)
            {
                var go=new GameObject("ExchangeCardButton",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(draw.transform.parent,false);existing=go.transform;
                var r=(RectTransform)existing;var dr=(RectTransform)draw.transform;r.anchorMin=dr.anchorMin;r.anchorMax=dr.anchorMax;r.anchoredPosition=dr.anchoredPosition+new Vector2(0,125);r.sizeDelta=new Vector2(155,55);
                var theme=Resources.Load<GameUiTheme>("GameUiTheme");var image=go.GetComponent<UnityEngine.UI.Image>();image.sprite=theme.buttonSprite;image.type=UnityEngine.UI.Image.Type.Sliced;
                var text=MakeLabel(existing,"ExchangeLabel");text.text="EXCHANGE\nSelect a card";
            }
            var button=existing.GetComponent<UnityEngine.UI.Button>();
            for(int i=button.onClick.GetPersistentEventCount()-1;i>=0;i--)if(button.onClick.GetPersistentTarget(i)==hand)UnityEventTools.RemovePersistentListener(button.onClick,i);
            UnityEventTools.AddPersistentListener(button.onClick,hand.ExchangeSelectedCard);
            var cooldown=draw.transform.Find("DrawCooldown");var label=cooldown!=null?cooldown.GetComponent<TMPro.TMP_Text>():MakeLabel(draw.transform,"DrawCooldown");
            var rect=(RectTransform)label.transform;rect.anchorMin=new Vector2(0,0);rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(0,28);rect.anchoredPosition=Vector2.zero;
            hand.ConfigureExchange(button,label);EditorUtility.SetDirty(hand);EditorUtility.SetDirty(button);EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);EditorSceneManager.SaveScene(canvas.gameObject.scene);
        }
        private static TMPro.TMP_Text MakeLabel(Transform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            var text=go.GetComponent<TMPro.TextMeshProUGUI>();text.font=TMPro.TMP_Settings.defaultFontAsset;text.fontSize=17;text.color=GameUiTheme.Ink;text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;return text;
        }
        [MenuItem("Tools/Cooking Cards/Validate Vertical Slice Batch A")]
        public static void Validate()
        {
            CreateAssets();
            for(int n=1;n<=3;n++)
            {
                var config=Resources.Load<LevelConfig>("Levels/Level0"+n);config.ValidateConfiguration();if(config.Recipes.Count!=n*2)throw new System.Exception("Wrong recipe count.");
                for(int bag=0;bag<100;bag++){var cards=RecipeDemandBag.Build(config);if(cards.Count!=20)throw new System.Exception("Bag size mismatch.");foreach(var recipe in config.Recipes)foreach(var amount in recipe.Ingredients)if(!cards.Contains(amount.Kind))throw new System.Exception("Missing resource in bag.");foreach(var kind in cards)if(!config.Allows(kind))throw new System.Exception("Inactive draw.");}
                if(config.manualDrawCooldownSeconds!=3||config.plateWeights.Length!=n||config.mergeEnabled!=(n==3)||config.knifeEnabled!=(n==3))throw new System.Exception("Wrong level features.");
                foreach(var recipe in config.Recipes){var stock=new int[IngredientRules.Count];foreach(var item in recipe.Ingredients)stock[(int)item.Kind]=item.Count;if(!recipe.MatchesExactly(stock))throw new System.Exception("Recipe mismatch.");}
            }
            if(Resources.Load<LevelConfig>("Levels/Level01").nextLevel.name!="Level02"||Resources.Load<LevelConfig>("Levels/Level02").nextLevel.name!="Level03")throw new System.Exception("Broken progression.");
            Debug.Log("Batch A validation passed: 2/4/6 recipes, 300 demand bags, active ingredients, plate grades, cooldowns, feature gates, progression.");
        }
    }
}
