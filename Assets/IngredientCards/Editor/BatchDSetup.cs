using System.Linq;
using UnityEditor;
using UnityEngine;
namespace CookingCards.Editor
{
    public static class BatchDSetup
    {
        [InitializeOnLoadMethod]private static void Schedule()=>EditorApplication.delayCall+=Apply;
        [MenuItem("Tools/Cooking Cards/Apply Batch D")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)return;BatchCSetup.Apply();
            foreach(var config in Resources.LoadAll<LevelConfig>("Levels"))
            {
                if(config.batchDVersion>0)continue;
                if(config.name=="Level01"){config.conveyorSpeedMultiplier=1.5f;config.plateSpawnInterval*=.75f;}
                if(config.name=="Level02"){config.conveyorSpeedMultiplier=1.2f;config.plateSpawnInterval*=.9f;}
                if(config.name=="Level02"||config.name=="Level03")
                    foreach(var recipe in config.activeRecipes){if(recipe.id=="ebi_nigiri")recipe.demandWeight=1.5f;if(recipe.id=="tamago_nigiri")recipe.demandWeight=.5f;}
                config.foodVisuals=config.activeRecipes.Select(r=>new RecipeFoodVisual{recipeId=r.id,sprite=ExactArtwork(r.id)}).ToArray();
                config.batchDVersion=1;config.InvalidateRecipes();config.ValidateConfiguration();EditorUtility.SetDirty(config);
            }
            AssetDatabase.SaveAssets();
        }
        public static Sprite ExactArtwork(string id)
        {
            string filename;
            switch(id)
            {
                case "ebi_nigiri":filename="ebi nigiri final";break;
                case "avocado_maki":filename="Avocado Maki final";break;
                case "tuna_avocado_maki":filename="Tuna Avocado Final";break;
                case "salmon_nigiri":filename="salmon nigiri final";break;
                default:filename=id;break;
            }
            return AssetDatabase.LoadAllAssetsAtPath("Assets/Asset/"+filename+".png").OfType<Sprite>().OrderByDescending(s=>s.rect.width*s.rect.height).FirstOrDefault();
        }
    }
}
