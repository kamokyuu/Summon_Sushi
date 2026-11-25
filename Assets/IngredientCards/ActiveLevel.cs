using System.Collections.Generic;
using UnityEngine;

namespace CookingCards
{
    /// <summary>Single level selection boundary; scene reload/replay retain a selected level.</summary>
    public static class ActiveLevel
    {
        private static LevelConfig selected, legacy, beginner;
        private static string selectionSource;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset(){selected=null;legacy=null;beginner=null;selectionSource=null;UnityEngine.SceneManagement.SceneManager.sceneLoaded-=OnSceneLoaded;}
        public static LevelConfig Config
        {
            get
            {
                if(selected!=null)return selected;
                if(PreparationSession.Tutorial)
                {
                    if(legacy==null)legacy=Resources.Load<LevelConfig>("Levels/LegacyDefault");
                    if(legacy==null)throw new System.InvalidOperationException("Missing Resources/Levels/LegacyDefault asset.");
                    return legacy;
                }
                if(beginner==null)beginner=Resources.Load<LevelConfig>("Levels/Level01");
                if(beginner==null)throw new System.InvalidOperationException("Missing beginner Level01; refusing silent legacy fallback.");
                return beginner;
            }
        }
        public static void Select(LevelConfig config,string source="Explicit selection")
        {
            if(config!=null)
            {
                config.ValidateConfiguration();
                if(!config.legacyCompatibility && PreparationSession.Tutorial)throw new System.InvalidOperationException("Start a normal PreparationSession before selecting a configured level; scripted tutorials use LegacyDefault.");
            }
            selected=config;
            selectionSource=config!=null?source:null;
        }
        public static string SelectionSource=>selected!=null?selectionSource:PreparationSession.Tutorial?"Scripted tutorial legacy fallback":"Normal beginner default";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallDiagnostics()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded-=OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded+=OnSceneLoaded;
            OnSceneLoaded(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            foreach(var root in scene.GetRootGameObjects())
                if(root.GetComponentInChildren<IngredientCardHand>(true)!=null){LogDiagnostics(scene.name);break;}
        }
        public static string Diagnostics()
        {
            var config=Config;
            var text=new System.Text.StringBuilder("[Summon Sushi Level] ").Append(config.name).Append(" | source: ").Append(SelectionSource)
                .Append("\nTutorial: ").Append(ScriptedTutorial).Append(" | draw: ").Append(ScriptedTutorial?"scripted finite deck":config.drawMode.ToString())
                .Append(" | opening: ").Append(config.initialHandSize).Append(" | capacity: ").Append(config.maximumHandCapacity)
                .Append(" | merge: ").Append(config.mergeEnabled).Append(" | knife: ").Append(config.knifeEnabled).Append(" | charges: ").Append(config.initialKnifeCharges)
                .Append("\nActive ingredients: ").Append(string.Join(", ",config.activeIngredients)).Append("\nDraw weights (normal play): ");
            if(config.drawMode==IngredientDrawMode.RecipeDemandShuffleBag)
            {
                var state=Random.state;var bag=RecipeDemandBag.Build(config);Random.state=state;
                var counts=new Dictionary<IngredientKind,int>();foreach(var kind in bag){if(!counts.ContainsKey(kind))counts[kind]=0;counts[kind]++;}
                foreach(var pair in counts)text.Append(pair.Key).Append(' ').Append((100f*pair.Value/bag.Count).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)).Append("%; ");
                text.Append(" | bag size: ").Append(config.shuffleBagSize);
            }
            else
            {
                float total=0;if(config.ingredientWeights!=null)foreach(var entry in config.ingredientWeights)total+=entry.weight;
                if(config.ingredientWeights!=null)foreach(var entry in config.ingredientWeights)text.Append(entry.ingredient).Append(' ').Append((total>0?entry.weight/total*100:0).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)).Append("%; ");
            }
            text.Append(" | manual cooldown: ").Append(config.manualDrawCooldownSeconds).Append("s | exchange: ").Append(config.exchangeEnabled);
            text.Append("\nPlate weights: ");foreach(var entry in config.plateWeights)text.Append(entry.grade).Append('=').Append(entry.weight).Append("; ");
            text.Append("\nRecipes:");foreach(var recipe in Recipes)
            {text.Append("\n  ").Append(recipe.Id).Append(" [").Append(recipe.Grade).Append("]: ");foreach(var item in recipe.Ingredients)text.Append(item.Count).Append(' ').Append(item.Kind).Append("; ");}
            return text.ToString();
        }
        public static void LogDiagnostics(string context="Manual")=>Debug.Log(Diagnostics()+"\nContext: "+context,Config);
        public static bool Legacy=>Config.legacyCompatibility;
        // Scripted tutorial decks/recipes remain an explicit compatibility boundary.
        public static bool ScriptedTutorial=>Legacy&&PreparationSession.Tutorial;
        public static bool MergeEnabled=>Config.mergeEnabled;
        public static bool KnifeEnabled=>Config.knifeEnabled;
        public static IReadOnlyList<SushiRecipe> Recipes
        {
            get
            {
                if(!ScriptedTutorial)return Config.Recipes;
                var recipes=new List<SushiRecipe>();foreach(var entry in Config.tutorialRecipes)recipes.Add(entry.Build());return recipes;
            }
        }
        public static bool TryCookTutorial(int[] stock,out DishKind dish)
        {
            foreach(var entry in Config.tutorialRecipes)
            {
                bool ready=true;foreach(var item in entry.ingredients)if(stock[(int)item.ingredient]<item.count){ready=false;break;}
                if(!ready)continue;
                foreach(var item in entry.ingredients)stock[(int)item.ingredient]-=item.count;
                dish=entry.tutorialDish;return true;
            }
            dish=default;return false;
        }
        public static IReadOnlyList<SushiRecipe> Conveyor(RecipeGrade grade)
        {var result=new List<SushiRecipe>();foreach(var recipe in Recipes)if(recipe.Service==RecipeService.Conveyor&&recipe.Grade==grade)result.Add(recipe);return result;}
        public static SushiRecipe Match(IReadOnlyList<int> stock)
        {foreach(var recipe in Recipes)if(recipe.MatchesExactly(stock))return recipe;return null;}
        public static List<IngredientKind> DrawBatch()
        {
            if(Config.drawMode==IngredientDrawMode.RecipeDemandShuffleBag)return RecipeDemandBag.Build(Config);
            if(Config.drawMode==IngredientDrawMode.LegacyWeightedBag)return GameJamRules.CreateDrawBatch();
            if(Config.drawMode==IngredientDrawMode.WeightedActiveIngredients)return new List<IngredientKind>{Config.SampleIngredient(Random.value)};
            return new List<IngredientKind>{Config.activeIngredients[Random.Range(0,Config.activeIngredients.Length)]};
        }
    }
}
