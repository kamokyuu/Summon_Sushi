using System.Linq;
using UnityEditor;
using UnityEngine;
namespace CookingCards.Menu.Editor
{
    public static class Week01Setup
    {
        [MenuItem("Tools/Cooking Cards/Set Up Week01 Campaign")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Edit mode only.");
            var first=Resources.Load<LevelConfig>("Levels/Day01");if(first==null)throw new System.InvalidOperationException("Create the existing Day01 campaign configuration first.");
            string[] titles={"Grand Opening","Seafood Festival","Lunch Rush","Tamago Special","Premium Night","VIP Reservation","Weekly Finale"};
            string[] descriptions={"Standard service and your first AYCE guest.","Seafood recipes and seafood-focused resources.","Fast service on Common and Uncommon plates.","Egg-focused dishes and ingredients.","Accessible premium dishes on Rare and Special plates.","Picky guests and a reserved Sushi Platter.","Combined service challenges; finish Week01."};
            for(int day=1;day<=7;day++)
            {
                string path="Assets/Resources/Levels/Day"+day.ToString("00")+".asset";var config=AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
                if(config==null){config=Object.Instantiate(first);config.name="Day"+day.ToString("00");config.weekCampaignVersion=0;AssetDatabase.CreateAsset(config,path);}
                if(config.weekCampaignVersion>0)continue;
                config.campaignDay=day;config.dailyEventTitle=titles[day-1];config.dailyEventDescription=descriptions[day-1];
                if(day>1)
                {
                    config.campaignShift=true;config.guidedSteps=new LevelGuideStep[0];config.nextLevel=null;config.finishOnTarget=false;
                    config.mergeEnabled=config.knifeEnabled=config.comboEnabled=config.powerUpsEnabled=config.bossCustomersEnabled=true;config.initialKnifeCharges=2;config.initialHandSize=5;config.maximumHandCapacity=7;config.manualDrawCooldownSeconds=0;
                    config.firstWaveSeconds=70;config.bossArrivalSeconds=90;config.minimumShiftSeconds=day<5?120:150;config.durationSeconds=day<5?150:180;config.bossTimeLimit=day<5?60:90;
                    config.targetYen=new[]{0,1200,1300,1400,1200,1500,1600,1800}[day];config.conveyorSpeedMultiplier=day==3?1.45f:day==7?1.2f:1;config.plateSpawnInterval=day==3?7.2f:day==7?8.8f:10;
                    config.riceDemandMultiplier=1.1f;config.noriDemandMultiplier=1;config.premiumDemandMultiplier=1.2f;
                    config.pickyCustomerChance=day==6?.6f:day==7?.35f:.2f;config.hungryCustomerChance=day==6?.25f:day==7?.4f:day==3?.4f:.25f;
                    config.bossKind=day==6?CampaignBossKind.SushiPlatter:CampaignBossKind.AYCE;config.platterOrders=new BossRecipeOrder[0];config.bossTitle=titles[day-1]+(day==6?" — VIP Platter":" — AYCE Guest");
                    switch(day)
                    {
                        case 2:config.activeRecipes=new[]{Ebi(),Maguro(RecipeGrade.Uncommon),Salmon(RecipeGrade.Rare),SalmonRoll()};config.plateWeights=Weights(4,3,3,0);config.ayceOrders=Orders(1,1,1,0);break;
                        case 3:config.activeRecipes=new[]{Ebi(),Tamago(),Avocado(),Maguro(RecipeGrade.Uncommon)};config.plateWeights=Weights(6,4,0,0);config.ayceOrders=Orders(3,1,0,0);break;
                        case 4:config.activeRecipes=new[]{Tamago(),Recipe("tamago_maki","Tamago Maki",RecipeGrade.Uncommon,IngredientKind.Rice,IngredientKind.Nori,IngredientKind.Eggs),TamagoRoll(RecipeGrade.Rare)};config.plateWeights=Weights(4,4,2,0);config.ayceOrders=Orders(2,1,0,0);break;
                        case 5:config.activeRecipes=new[]{Maguro(RecipeGrade.Rare),SalmonRoll(),Salmon(RecipeGrade.Special),TamagoRoll(RecipeGrade.Special)};config.plateWeights=Weights(0,0,6,4);config.ayceOrders=Orders(0,0,2,1);break;
                        case 6:config.activeRecipes=new[]{Avocado(),Maguro(RecipeGrade.Rare),SalmonRoll(),Salmon(RecipeGrade.Special),TamagoRoll(RecipeGrade.Special)};config.plateWeights=Weights(0,3,5,2);config.ayceOrders=new BossPlateOrder[0];config.platterOrders=new[]{Dish("maguro_nigiri"),Dish("salmon_avocado_roll"),Dish("avocado_maki")};break;
                        default:config.activeRecipes=new[]{Ebi(),Tamago(),Avocado(),SalmonRoll(),Maguro(RecipeGrade.Rare),TamagoRoll(RecipeGrade.Rare),Salmon(RecipeGrade.Special),Recipe("tamago_maki","Tamago Maki",RecipeGrade.Special,IngredientKind.Rice,IngredientKind.Nori,IngredientKind.Eggs)};config.plateWeights=Weights(3,3,3,1);config.ayceOrders=Orders(1,1,1,1);break;
                    }
                    config.activeIngredients=config.activeRecipes.SelectMany(r=>r.ingredients).Select(a=>a.ingredient).Distinct().ToArray();config.ingredientWeights=new IngredientDrawWeight[0];config.drawMode=IngredientDrawMode.RecipeDemandShuffleBag;config.shuffleBagSize=20;
                    config.foodVisuals=config.activeRecipes.Select(r=>new RecipeFoodVisual{recipeId=r.id,sprite=CookingCards.Editor.BatchDSetup.ExactArtwork(r.id)}).ToArray();config.InvalidateRecipes();
                }
                config.weekCampaignVersion=1;EditorUtility.SetDirty(config);
            }
            foreach(var config in Resources.LoadAll<LevelConfig>("Levels"))
            {
                if(config.powerUps==null)continue;
                foreach(var power in config.powerUps)
                {
                    switch(power.effect)
                    {
                        case PowerUpEffect.LuckyDraw:power.description="Choose 2 ingredients; needs 2 free slots";break;
                        case PowerUpEffect.KnifeRefill:power.description="Restore up to 2 knife charges";break;
                        case PowerUpEffect.ExtraHand:power.description="+2 hand slots this shift; maximum 11";break;
                        case PowerUpEffect.ConveyorFreeze:power.description="Freeze conveyor for 10 seconds";break;
                        case PowerUpEffect.PlateShield:power.description="Protect the next missed plate";break;
                    }
                }
                EditorUtility.SetDirty(config);
            }
            AssetDatabase.SaveAssets();
        }
        private static LevelRecipe Ebi()=>Recipe("ebi_nigiri","Ebi Nigiri",RecipeGrade.Common,IngredientKind.Rice,IngredientKind.Shrimp);
        private static LevelRecipe Tamago()=>Recipe("tamago_nigiri","Tamago Nigiri",RecipeGrade.Common,IngredientKind.Rice,IngredientKind.Eggs);
        private static LevelRecipe Avocado()=>Recipe("avocado_maki","Avocado Maki",RecipeGrade.Uncommon,IngredientKind.Rice,IngredientKind.Nori,IngredientKind.Avocado);
        private static LevelRecipe Maguro(RecipeGrade grade)=>Recipe("maguro_nigiri","Maguro Nigiri",grade,IngredientKind.Rice,IngredientKind.Tuna);
        private static LevelRecipe Salmon(RecipeGrade grade)=>Recipe("salmon_nigiri","Salmon Nigiri",grade,IngredientKind.Rice,IngredientKind.Salmon);
        private static LevelRecipe SalmonRoll()=>Recipe("salmon_avocado_roll","Salmon Avocado Roll",RecipeGrade.Rare,IngredientKind.Rice,IngredientKind.Nori,IngredientKind.Avocado,IngredientKind.Salmon);
        private static LevelRecipe TamagoRoll(RecipeGrade grade)=>Recipe("tamago_avocado_roll","Tamago Avocado Roll",grade,IngredientKind.Rice,IngredientKind.Nori,IngredientKind.Avocado,IngredientKind.Eggs);
        private static LevelRecipe Recipe(string id,string title,RecipeGrade grade,params IngredientKind[] kinds)=>LevelRecipe.From(new SushiRecipe(id,title,grade,id.Contains("nigiri")?RecipeFamily.Nigiri:RecipeFamily.Maki,kinds.Select(k=>new IngredientAmount(k,1)).ToArray()));
        private static PlateSpawnWeight[] Weights(params int[] weights)=>weights.Select((w,i)=>new PlateSpawnWeight{grade=(RecipeGrade)i,weight=w}).Where(w=>w.weight>0).ToArray();
        private static BossPlateOrder[] Orders(params int[] counts)=>counts.Select((q,i)=>new BossPlateOrder{grade=(RecipeGrade)i,quantity=q}).Where(o=>o.quantity>0).ToArray();
        private static BossRecipeOrder Dish(string id)=>new BossRecipeOrder{recipeId=id,quantity=1};
    }
}
