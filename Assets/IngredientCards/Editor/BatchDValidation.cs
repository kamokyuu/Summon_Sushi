using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CookingCards.Editor
{
    public static class BatchDValidation
    {
        public static void Assets()
        {
            BatchDSetup.Apply();BatchCValidation.Assets();
            float[] intervals={6,8.1f,10},speeds={1.5f,1.2f,1};
            for(int n=1;n<=3;n++)
            {
                var config=Resources.Load<LevelConfig>("Levels/Level0"+n);
                Require(Mathf.Abs(config.plateSpawnInterval-intervals[n-1])<.001f&&config.conveyorSpeedMultiplier==speeds[n-1],"Wrong pacing.");
                if(n>1){var bag=RecipeDemandBag.Build(config);Require(bag.Count(k=>IngredientRules.Category(k)==IngredientCategory.Seafood)>bag.Count(k=>IngredientRules.Category(k)==IngredientCategory.Roe),"Eggs dominate seafood.");}
                foreach(var recipe in config.activeRecipes)Require(config.foodVisuals.Single(v=>v.recipeId==recipe.id).sprite==BatchDSetup.ExactArtwork(recipe.id),"Incorrect recipe artwork.");
            }
            var copy=ScriptableObject.CreateInstance<LevelConfig>();
            try
            {
                copy.activeIngredients=new[]{IngredientKind.Rice,IngredientKind.Eggs,IngredientKind.Shrimp,IngredientKind.Tobiko,IngredientKind.Ikura};
                copy.activeRecipes=new[]{Recipe("egg_one",IngredientKind.Eggs,RecipeGrade.Common),Recipe("egg_two",IngredientKind.Eggs,RecipeGrade.Common),Recipe("egg_three",IngredientKind.Eggs,RecipeGrade.Common),Recipe("shrimp",IngredientKind.Shrimp,RecipeGrade.Rare)};
                copy.plateWeights=new[]{new PlateSpawnWeight{grade=RecipeGrade.Common,weight=1},new PlateSpawnWeight{grade=RecipeGrade.Rare,weight=1}};
                var bag=RecipeDemandBag.Build(copy);Require(bag.Count(k=>k==IngredientKind.Eggs)==5&&bag.Count(k=>k==IngredientKind.Shrimp)==5,"Recipe count biases grade demand.");
                copy.plateWeights[1].weight=9;bag=RecipeDemandBag.Build(copy);
                Require(bag.Count(k=>k==IngredientKind.Eggs)==1&&bag.Count(k=>k==IngredientKind.Shrimp)==9&&!bag.Contains(IngredientKind.Ikura)&&!bag.Contains(IngredientKind.Tobiko),"Plate frequency or unused ingredients ignored.");
            }
            finally{UnityEngine.Object.DestroyImmediate(copy);}
        }
        private static LevelRecipe Recipe(string id,IngredientKind kind,RecipeGrade grade)=>LevelRecipe.From(new SushiRecipe(id,id,grade,RecipeFamily.Nigiri,new IngredientAmount(IngredientKind.Rice,1),new IngredientAmount(kind,1)));
        public static async Task<string> Run()
        {
            Require(Application.isPlaying,"Enter Play mode.");
            for(int n=1;n<=3;n++)
            {
                PreparationSession.BeginBeginnerLevel();var config=UnityEngine.Object.Instantiate(Resources.Load<LevelConfig>("Levels/Level0"+n));config.comboEnabled=config.powerUpsEnabled=config.bossCustomersEnabled=false;
                ActiveLevel.Select(config,"Batch D pacing fixture");SceneManager.LoadScene("MainScene");await Task.Delay(500);
                for(int i=0;i<100;i++){var hand=UnityEngine.Object.FindFirstObjectByType<IngredientCardHand>();if(hand!=null&&!hand.IsDealing)break;await Task.Delay(100);}
                GuidedLevelTutorial.Active.Skip();await Task.Delay(150);
                var belt=UnityEngine.Object.FindFirstObjectByType<ConveyorController>();
                Require(Mathf.Abs(belt.EffectiveSpeed-28*config.conveyorSpeedMultiplier/belt.GetComponentInParent<Canvas>().scaleFactor)<.001f,"Speed multiplier ignored.");
                var moving=belt.SpawnPlate(RecipeGrade.Common,0);float x=((RectTransform)moving.transform).anchoredPosition.x,time=Time.time;
                await Task.Delay(250);float elapsed=Time.time-time,travel=x-((RectTransform)moving.transform).anchoredPosition.x;
                Require(Mathf.Abs(travel/elapsed-belt.EffectiveSpeed)<belt.EffectiveSpeed*.15f,"Movement rate wrong.");
                int count=belt.Plates.Count;await Task.Delay((int)(config.plateSpawnInterval*1000+250));
                Require(belt.Plates.Count>count,"Normal spawn interval obstructed by spacing.");
                for(int i=0;i<8;i++)belt.SpawnPlate(RecipeGrade.Common,0);
                for(int i=0;i<belt.Plates.Count;i++)for(int j=i+1;j<belt.Plates.Count;j++)Require(Mathf.Abs(((RectTransform)belt.Plates[i].transform).anchoredPosition.x-((RectTransform)belt.Plates[j].transform).anchoredPosition.x)>=belt.MinimumPlateSpacing-.02f,"Plate overlap.");
                var data=new UnityEditor.SerializedObject(belt);data.FindProperty("foodScale").floatValue=1.5f;data.ApplyModifiedPropertiesWithoutUndo();Require(belt.FoodScale<=.9f,"Oversized food allowed.");
                var recipe=config.Recipes[0];var plated=belt.SpawnPlate(recipe.Grade,0);Require(plated.TryServe(recipe),"Recipe visual failed.");Canvas.ForceUpdateCanvases();
                var food=plated.transform.Find("ServedFood").GetComponent<UnityEngine.UI.Image>();Require(food.sprite==BatchDSetup.ExactArtwork(recipe.Id)&&plated.GetComponent<UnityEngine.UI.RectMask2D>()!=null,"Wrong or unbounded sprite.");
                var corners=new Vector3[4];((RectTransform)food.transform).GetWorldCorners(corners);foreach(var corner in corners)Require(((RectTransform)plated.transform).rect.Contains(plated.transform.InverseTransformPoint(corner)),"Food exceeds plate bounds.");
                var salmon=BatchDSetup.ExactArtwork("salmon_nigiri");config.foodVisuals=new[]{new RecipeFoodVisual{recipeId="salmon_avocado_roll",sprite=salmon}};
                var wrong=new SushiRecipe("salmon_avocado_roll","Salmon Avocado Roll",RecipeGrade.Common,RecipeFamily.Maki,new IngredientAmount(IngredientKind.Rice,1));var fallback=belt.SpawnPlate(RecipeGrade.Common,0);
                Require(belt.FoodSprite(wrong)==null&&fallback.TryServe(wrong)&&fallback.transform.Find("RecipePlaceholder/ServedRecipe").GetComponent<TMPro.TMP_Text>().text==wrong.DisplayName,"Salmon Avocado displays Nigiri.");
            }
            return "PASS: plate-frequency demand, unused ingredients, seafood balance, exact pacing and timed spawns, nonoverlapping slots, bounded sprites, Salmon Avocado placeholder";
        }
        private static void Require(bool value,string message){if(!value)throw new Exception(message);}
    }
}
