using System.IO;
using UnityEditor;
using UnityEngine;

namespace CookingCards.Editor
{
    public static class LevelConfigSetup
    {
        [InitializeOnLoadMethod] private static void Schedule()=>EditorApplication.delayCall+=CreateAssets;
        [MenuItem("Tools/Cooking Cards/Create Level Configurations")]
        public static void CreateAssets()
        {
            const string folder="Assets/Resources/Levels";
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            if(AssetDatabase.LoadAssetAtPath<LevelConfig>(folder+"/LegacyDefault.asset")==null)
            {
                var config=ScriptableObject.CreateInstance<LevelConfig>();
                config.legacyCompatibility=true;config.bossCustomersEnabled=true;
                config.activeIngredients=new IngredientKind[GameJamRules.Ingredients.Count];
                for(int i=0;i<config.activeIngredients.Length;i++)config.activeIngredients[i]=GameJamRules.Ingredients[i];
                config.activeRecipes=new LevelRecipe[VerticalSliceRecipes.All.Count];
                for(int i=0;i<config.activeRecipes.Length;i++)config.activeRecipes[i]=LevelRecipe.From(VerticalSliceRecipes.All[i]);
                config.plateWeights=new[]{Weight(RecipeGrade.Common,.4f),Weight(RecipeGrade.Uncommon,.3f),Weight(RecipeGrade.Special,.2f),Weight(RecipeGrade.Rare,.1f)};
                config.ValidateConfiguration();AssetDatabase.CreateAsset(config,folder+"/LegacyDefault.asset");
            }
            if(AssetDatabase.LoadAssetAtPath<LevelConfig>(folder+"/Level01.asset")==null)
            {
                var config=ScriptableObject.CreateInstance<LevelConfig>();
                config.drawMode=IngredientDrawMode.UniformActiveIngredients;
                config.initialHandSize=5;config.maximumHandCapacity=7;config.initialKnifeCharges=0;config.knifeEnabled=false;
                config.activeIngredients=new[]{IngredientKind.Rice,IngredientKind.Shrimp,IngredientKind.Eggs,IngredientKind.Nori};
                SushiRecipe tamago=null;foreach(var recipe in SushiRecipeCatalog.All)if(recipe.Id=="tamago_nigiri")tamago=recipe;
                config.activeRecipes=new[]{LevelRecipe.From(VerticalSliceRecipes.All[0]),LevelRecipe.From(tamago)};
                config.plateWeights=new[]{Weight(RecipeGrade.Common,1)};
                config.ValidateConfiguration();AssetDatabase.CreateAsset(config,folder+"/Level01.asset");
            }
            var legacy=AssetDatabase.LoadAssetAtPath<LevelConfig>(folder+"/LegacyDefault.asset");
            if(legacy.tutorialRecipes==null||legacy.tutorialRecipes.Length==0)
            {
                legacy.tutorialRecipes=new[]{TutorialRecipe("salmon_nigiri","Salmon Nigiri",RecipeGrade.Special,IngredientKind.Salmon,3,DishKind.SalmonNigiri),TutorialRecipe("maguro_nigiri","Maguro Nigiri",RecipeGrade.Rare,IngredientKind.Tuna,2,DishKind.MaguroNigiri),TutorialRecipe("ebi_nigiri","Ebi Nigiri",RecipeGrade.Common,IngredientKind.Shrimp,1,DishKind.EbiNigiri)};
                EditorUtility.SetDirty(legacy);
            }
            MigrateIngredientBalance(legacy,false);
            MigrateIngredientBalance(AssetDatabase.LoadAssetAtPath<LevelConfig>(folder+"/Level01.asset"),true);
            AssetDatabase.SaveAssets();
        }
        private static void MigrateIngredientBalance(LevelConfig config,bool beginner)
        {
            if(config.ingredientBalanceVersion>=2)return;
            if(beginner)
            {
                config.activeIngredients=new[]{IngredientKind.Rice,IngredientKind.Shrimp,IngredientKind.Eggs};
                config.activeRecipes=new[]{LevelRecipe.From(new SushiRecipe("ebi_nigiri","Ebi Nigiri",RecipeGrade.Common,RecipeFamily.Nigiri,new IngredientAmount(IngredientKind.Rice,1),new IngredientAmount(IngredientKind.Shrimp,1))),LevelRecipe.From(new SushiRecipe("tamago_nigiri","Tamago Nigiri",RecipeGrade.Common,RecipeFamily.Nigiri,new IngredientAmount(IngredientKind.Rice,1),new IngredientAmount(IngredientKind.Eggs,1)))};
                config.ingredientWeights=new[]{DrawWeight(IngredientKind.Rice,45),DrawWeight(IngredientKind.Shrimp,30),DrawWeight(IngredientKind.Eggs,25)};
                config.plateWeights=new[]{Weight(RecipeGrade.Common,1)};
                config.mergeEnabled=false;config.knifeEnabled=false;config.initialKnifeCharges=0;
            }
            else
            {
                config.ingredientWeights=new IngredientDrawWeight[config.activeIngredients.Length];
                for(int i=0;i<config.activeIngredients.Length;i++)
                {
                    var kind=config.activeIngredients[i];float demand=0;
                    foreach(var recipe in config.activeRecipes)foreach(var item in recipe.ingredients)if(item.ingredient==kind)demand+=item.count;
                    config.ingredientWeights[i]=DrawWeight(kind,Mathf.Max(1,demand));
                }
            }
            config.drawMode=IngredientDrawMode.WeightedActiveIngredients;
            config.ingredientBalanceVersion=2;
            // Serialized recipe authoring data changed; invalidate its immutable runtime cache.
            config.InvalidateRecipes();
            config.ValidateConfiguration();EditorUtility.SetDirty(config);
        }
        private static IngredientDrawWeight DrawWeight(IngredientKind kind,float weight)=>new IngredientDrawWeight{ingredient=kind,weight=weight};
        private static LevelRecipe TutorialRecipe(string id,string name,RecipeGrade grade,IngredientKind kind,int count,DishKind dish)
        {var entry=LevelRecipe.From(new SushiRecipe(id,name,grade,RecipeFamily.Nigiri,new IngredientAmount(IngredientKind.Rice,2),new IngredientAmount(kind,count)));entry.tutorialDish=dish;return entry;}
        private static PlateSpawnWeight Weight(RecipeGrade grade,float weight)=>new PlateSpawnWeight{grade=grade,weight=weight};
        [MenuItem("Tools/Cooking Cards/Validate Level Configurations")]
        public static void Validate()
        {
            CreateAssets();
            var legacy=Resources.Load<LevelConfig>("Levels/LegacyDefault");var beginner=Resources.Load<LevelConfig>("Levels/Level01");
            var previous=ActiveLevel.Config;var random=Random.state;
            try
            {
                legacy.ValidateConfiguration();beginner.ValidateConfiguration();
                ActiveLevel.Select(legacy);GameJamRulesValidation.Run();
                Require(legacy.Recipes.Count==5&&legacy.maximumHandCapacity==7&&legacy.initialKnifeCharges==3,"Legacy defaults changed.");
                ActiveLevel.Select(beginner);
                Require(beginner.Recipes.Count==2,"Level01 must have two recipes.");
                for(int i=0;i<1000;i++)
                {
                    Require(ConveyorController.GradeFromSample(i/1000f)==RecipeGrade.Common,"Non-Common beginner plate.");
                    foreach(var kind in ActiveLevel.DrawBatch())Require(beginner.Allows(kind),"Inactive draw.");
                }
                int journalCount=0;for(int i=0;i<5;i++)journalCount+=RecipeJournal.RecipesForTab((RecipeJournalTab)i).Count;
                Require(journalCount==2,"Journal has inactive recipes.");
                foreach(var recipe in beginner.Recipes)
                {
                    var stock=new int[IngredientRules.Count];
                    foreach(var amount in recipe.Ingredients)for(int i=0;i<amount.Count;i++){Require(ActiveRecipeRules.CanAdd(stock,amount.Kind),"Valid deposit rejected.");stock[(int)amount.Kind]++;}
                    Require(ActiveRecipeRules.TryCook(stock,out var cooked)&&object.ReferenceEquals(cooked,recipe),"Journal and box recipe differ.");
                    foreach(int count in stock)Require(count==0,"Ingredients left after cooking.");
                }
                Require(!ActiveRecipeRules.CanAdd(new int[IngredientRules.Count],IngredientKind.Salmon),"Inactive ingredient accepted.");
                Require(!beginner.knifeEnabled&&!beginner.bossCustomersEnabled&&!beginner.comboEnabled&&!beginner.powerUpsEnabled,"Beginner expansion features enabled.");
                ActiveLevel.Select(legacy);
                foreach(var entry in legacy.tutorialRecipes)
                {
                    var stock=new int[IngredientRules.Count];foreach(var item in entry.ingredients)stock[(int)item.ingredient]=item.count;
                    Require(ActiveLevel.TryCookTutorial(stock,out var dish)&&dish==entry.tutorialDish,"Legacy tutorial recipe changed.");
                    foreach(var count in stock)Require(count==0,"Tutorial leftover ingredients.");
                }
                Debug.Log("Level config validation passed: legacy regression, Common-only Level01, active draws, recipe/journal parity, deposits and cooking.");
            }
            finally{ActiveLevel.Select(previous);Random.state=random;}
        }
        private static void Require(bool condition,string message){if(!condition)throw new System.Exception(message);}
        [MenuItem("Tools/Cooking Cards/Log Active Level Configuration")]
        private static void LogActive()=>ActiveLevel.LogDiagnostics("Editor diagnostics command");
    }
}
