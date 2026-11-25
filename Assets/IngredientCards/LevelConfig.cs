using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookingCards
{
    public enum LevelGuideStep { Draw, Summon, Serve, Journal, PlateMatch, Merge, Knife }
    public enum PowerUpEffect { ExtraHand, KnifeRefill, ConveyorFreeze, PlateShield, LuckyDraw }
    [Serializable] public sealed class LevelPowerUp
    {
        public string title,description;
        public PowerUpEffect effect;
    }
    [Serializable] public sealed class RecipeFoodVisual {public string recipeId;public Sprite sprite;}
    public enum IngredientDrawMode { LegacyWeightedBag=0, UniformActiveIngredients=1, WeightedActiveIngredients=2, RecipeDemandShuffleBag=3 }
    [Serializable] public sealed class IngredientDrawWeight { public IngredientKind ingredient; [Min(0)] public float weight=1; }
    [Serializable] public sealed class LevelIngredientAmount { public IngredientKind ingredient; [Min(1)] public int count=1; }
    [Serializable] public sealed class LevelRecipe
    {
        public string id, displayName;
        public RecipeGrade grade;
        public RecipeFamily family;
        public DishKind tutorialDish;
        [Tooltip("Relative recipe demand used to allocate ingredient tokens in demand shuffle bags.")]
        [Min(.01f)] public float demandWeight=1;
        public LevelIngredientAmount[] ingredients;
        public SushiRecipe Build()
        {
            var amounts=new IngredientAmount[ingredients.Length];
            for(int i=0;i<amounts.Length;i++)amounts[i]=new IngredientAmount(ingredients[i].ingredient,ingredients[i].count);
            return new SushiRecipe(id,displayName,grade,family,amounts);
        }
        public static LevelRecipe From(SushiRecipe recipe)
        {
            var result=new LevelRecipe{id=recipe.Id,displayName=recipe.DisplayName,grade=recipe.Grade,family=recipe.Family,ingredients=new LevelIngredientAmount[recipe.Ingredients.Count]};
            for(int i=0;i<result.ingredients.Length;i++)result.ingredients[i]=new LevelIngredientAmount{ingredient=recipe.Ingredients[i].Kind,count=recipe.Ingredients[i].Count};
            return result;
        }
    }
    [Serializable] public sealed class PlateSpawnWeight { public RecipeGrade grade; [Min(0)] public float weight=1; }

    [CreateAssetMenu(menuName="Summon Sushi/Level Config")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Tooltip("Retain existing scene hand/deal tuning, distance-based conveyor scheduling and scripted tutorials.")]
        public bool legacyCompatibility;
        public LevelRecipe[] activeRecipes;
        [Tooltip("Legacy scripted tutorial recipes, kept separate from normal-game balancing.")]
        public LevelRecipe[] tutorialRecipes;
        public IngredientKind[] activeIngredients;
        [Tooltip("Used by WeightedActiveIngredients. Demand shuffle bags instead use recipe quantities multiplied by each recipe's demandWeight.")]
        public IngredientDrawWeight[] ingredientWeights;
        [HideInInspector] public int ingredientBalanceVersion;
        [HideInInspector] public int verticalSliceBatchVersion;
        [HideInInspector] public int batchBVersion;
        [HideInInspector] public int batchCVersion;
        [Min(.1f)] public float riceDemandMultiplier=1,noriDemandMultiplier=1,premiumDemandMultiplier=1;
        public LevelGuideStep[] guidedSteps;
        public RecipeFoodVisual[] foodVisuals;
        public bool finishOnTarget;
        public LevelPowerUp[] powerUps;
        public PlateSpawnWeight[] plateWeights;
        [Min(0)] public int initialHandSize=7;
        [Min(1)] public int maximumHandCapacity=7;
        [Tooltip("Delay between cards in the opening deal; manual draws use Manual Draw Cooldown Seconds.")]
        [Min(.01f)] public float drawInterval=.25f;
        public IngredientDrawMode drawMode;
        [Tooltip("Each bag contains every demanded ingredient at least once; larger bags improve weight precision.")]
        [Min(1)] public int shuffleBagSize=20;
        [Min(0)] public float manualDrawCooldownSeconds;
        public bool exchangeEnabled;
        [Min(.1f)] public float exchangeCooldownSeconds=3;
        public LevelConfig nextLevel;
        [Min(1)] public float durationSeconds=180;
        [Min(0)] public int targetYen=900;
        [Tooltip("Seconds between plates; legacy compatibility uses the existing distance scheduler instead.")]
        [Min(.01f)] public float plateSpawnInterval=5;
        [Min(0)] public int initialKnifeCharges=3;
        public bool mergeEnabled=true, knifeEnabled=true, comboEnabled, powerUpsEnabled, bossCustomersEnabled;
        [NonSerialized] private IReadOnlyList<SushiRecipe> cachedRecipes;
        public IReadOnlyList<SushiRecipe> Recipes
        {
            get
            {
                if(cachedRecipes==null){var list=new List<SushiRecipe>();foreach(var recipe in activeRecipes)list.Add(recipe.Build());cachedRecipes=list.AsReadOnly();}
                return cachedRecipes;
            }
        }
        public void InvalidateRecipes(){cachedRecipes=null;}
        private void OnValidate(){InvalidateRecipes();}
        public bool Allows(IngredientKind kind)=>Array.IndexOf(activeIngredients,kind)>=0;
        public IngredientKind SampleIngredient(float sample)
        {
            if(ingredientWeights==null)throw new InvalidOperationException("Missing ingredient weights.");
            float total=0;foreach(var entry in ingredientWeights)
            {
                if(entry==null||!Allows(entry.ingredient)||entry.weight<0||float.IsNaN(entry.weight)||float.IsInfinity(entry.weight))
                    throw new InvalidOperationException("Cannot draw from invalid or inactive ingredient weights.");
                total+=entry.weight;
            }
            if(total<=0||float.IsInfinity(total))throw new InvalidOperationException("No finite positive ingredient weight total.");
            float target=Mathf.Clamp(sample,0,.999999f)*total;
            foreach(var entry in ingredientWeights){target-=entry.weight;if(target<0)return entry.ingredient;}
            throw new InvalidOperationException("Invalid ingredient distribution.");
        }
        public RecipeGrade SamplePlate(float sample)
        {
            float total=0;foreach(var entry in plateWeights)total+=Mathf.Max(0,entry.weight);
            if(total<=0)throw new InvalidOperationException(name+": no positive plate weights.");
            float target=Mathf.Clamp(sample,0,.999999f)*total;
            foreach(var entry in plateWeights){target-=Mathf.Max(0,entry.weight);if(target<0)return entry.grade;}
            throw new InvalidOperationException("Invalid plate distribution.");
        }
        public void ValidateConfiguration()
        {
            if(activeRecipes==null||activeRecipes.Length==0||activeIngredients==null||activeIngredients.Length==0)throw new InvalidOperationException("Level needs recipes and ingredients.");
            if(maximumHandCapacity<1||initialHandSize<0||initialHandSize>maximumHandCapacity||drawInterval<=0||plateSpawnInterval<=0||initialKnifeCharges<0)throw new InvalidOperationException("Invalid level limits/timing.");
            if(plateWeights==null||plateWeights.Length==0)throw new InvalidOperationException("Level needs plate grades.");
            SamplePlate(0);
            if(drawMode==IngredientDrawMode.RecipeDemandShuffleBag)
            {
                if(manualDrawCooldownSeconds<0||exchangeCooldownSeconds<=0)throw new InvalidOperationException("Invalid draw/exchange cooldown.");
                foreach(var recipe in activeRecipes)if(recipe.demandWeight<=0||float.IsNaN(recipe.demandWeight)||float.IsInfinity(recipe.demandWeight))throw new InvalidOperationException("Invalid recipe demand.");
                var state=UnityEngine.Random.state;try{RecipeDemandBag.Build(this);}finally{UnityEngine.Random.state=state;}
            }
            var ids=new HashSet<string>();
            foreach(var recipe in Recipes)
            {
                if(string.IsNullOrEmpty(recipe.Id)||!ids.Add(recipe.Id))throw new InvalidOperationException("Invalid/duplicate recipe ID.");
                foreach(var item in recipe.Ingredients)if(!Allows(item.Kind))throw new InvalidOperationException("Inactive ingredient in "+recipe.Id);
                if(recipe.Service==RecipeService.Conveyor&&!Array.Exists(plateWeights,p=>p.grade==recipe.Grade&&p.weight>0))throw new InvalidOperationException("Recipe has no active plate grade: "+recipe.Id);
            }
            if(drawMode==IngredientDrawMode.LegacyWeightedBag)
                foreach(var kind in GameJamRules.Ingredients)if(!Allows(kind))throw new InvalidOperationException("Legacy bag requires all six jam ingredients.");
            if(drawMode==IngredientDrawMode.WeightedActiveIngredients)
            {
                if(ingredientWeights==null||ingredientWeights.Length==0)throw new InvalidOperationException("Weighted mode requires ingredient weights.");
                var kinds=new HashSet<IngredientKind>();float total=0;
                foreach(var entry in ingredientWeights)
                {
                    if(entry==null||!Allows(entry.ingredient)||!kinds.Add(entry.ingredient)||float.IsNaN(entry.weight)||float.IsInfinity(entry.weight)||entry.weight<0)
                        throw new InvalidOperationException("Ingredient weights must be finite, nonnegative, unique, and active.");
                    total+=entry.weight;
                }
                if(float.IsInfinity(total)||total<=0)throw new InvalidOperationException("Ingredient weight total must be finite and positive.");
            }
        }
    }
}
