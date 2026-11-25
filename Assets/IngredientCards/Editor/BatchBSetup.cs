using System.Linq;
using UnityEditor;
using UnityEngine;
namespace CookingCards.Editor
{
    public static class BatchBSetup
    {
        [InitializeOnLoadMethod] private static void Schedule()=>EditorApplication.delayCall+=Apply;
        [MenuItem("Tools/Cooking Cards/Apply Batch B")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)return;
            VerticalSliceBatchSetup.CreateAssets();
            foreach(var config in Resources.LoadAll<LevelConfig>("Levels"))
            {
                if(config.batchBVersion!=0)continue;
                config.manualDrawCooldownSeconds=0;
                config.foodVisuals=new[]{Visual("ebi_nigiri","ebi nigiri final"),Visual("tamago_nigiri","tamago_nigiri"),Visual("avocado_maki","Avocado Maki final"),Visual("salmon_nigiri","salmon nigiri final"),Visual("salmon_sashimi","salmon_sashimi"),Visual("salmon_maki","salmon_maki")};
                if(!config.legacyCompatibility)
                {
                    config.finishOnTarget=true;
                    if(config.name=="Level01"){config.durationSeconds=45;config.targetYen=600;config.guidedSteps=new[]{LevelGuideStep.Draw,LevelGuideStep.Summon,LevelGuideStep.Serve};}
                    if(config.name=="Level02"){config.durationSeconds=90;config.targetYen=1260;config.guidedSteps=new[]{LevelGuideStep.Journal,LevelGuideStep.PlateMatch};}
                    if(config.name=="Level03"){config.durationSeconds=120;config.targetYen=1800;config.guidedSteps=new[]{LevelGuideStep.Merge,LevelGuideStep.Knife};}
                    config.knifeEnabled=config.name=="Level03";config.initialKnifeCharges=config.knifeEnabled?2:0;
                }
                config.batchBVersion=1;EditorUtility.SetDirty(config);
            }
            AssetDatabase.SaveAssets();
        }
        private static RecipeFoodVisual Visual(string id,string filename)
        {
            string path=AssetDatabase.FindAssets("t:Sprite",new[]{"Assets/Asset"}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>System.IO.Path.GetFileNameWithoutExtension(p)==filename);
            return new RecipeFoodVisual{recipeId=id,sprite=path==null?null:AssetDatabase.LoadAssetAtPath<Sprite>(path)};
        }
    }
}
