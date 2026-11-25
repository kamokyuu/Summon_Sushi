using UnityEditor;
using UnityEngine;
namespace CookingCards.Editor
{
    public static class BatchCSetup
    {
        [InitializeOnLoadMethod]private static void Schedule()=>EditorApplication.delayCall+=Apply;
        [MenuItem("Tools/Cooking Cards/Apply Batch C")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)return;BatchBSetup.Apply();
            for(int n=2;n<=3;n++)
            {
                var config=Resources.Load<LevelConfig>("Levels/Level0"+n);if(config.batchCVersion>0)continue;
                config.mergeEnabled=true;config.knifeEnabled=n==3;
                config.riceDemandMultiplier=1.35f;config.noriDemandMultiplier=2;config.premiumDemandMultiplier=.65f;
                config.comboEnabled=config.powerUpsEnabled=true;config.bossCustomersEnabled=n==3;
                config.powerUps=new[]{
                    Power("Extra Hand","+1 hand slot for this shift",PowerUpEffect.ExtraHand),
                    Power("Knife Refill","Restore 1 spent knife charge",PowerUpEffect.KnifeRefill),
                    Power("Conveyor Freeze","Freeze conveyor for 30 seconds",PowerUpEffect.ConveyorFreeze),
                    Power("Plate Shield","Protect the next missed plate",PowerUpEffect.PlateShield),
                    Power("Lucky Draw","Choose 3 identical allowed cards; needs 3 free slots",PowerUpEffect.LuckyDraw)};
                if(n==2)config.guidedSteps=new[]{LevelGuideStep.Journal,LevelGuideStep.PlateMatch,LevelGuideStep.Merge};
                config.batchCVersion=1;config.ValidateConfiguration();EditorUtility.SetDirty(config);
            }
            AssetDatabase.SaveAssets();
        }
        private static LevelPowerUp Power(string title,string description,PowerUpEffect effect)=>new LevelPowerUp{title=title,description=description,effect=effect};
    }
}
