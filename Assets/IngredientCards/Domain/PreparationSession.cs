using System.Collections.Generic;
using UnityEngine;
namespace CookingCards
{
    public static class PreparationSession
    {
        public static int Yen { get; private set; }=0;
        private static readonly List<IngredientKind> supplies=new List<IngredientKind>();
        private static readonly List<IngredientKind> deck=new List<IngredientKind>();
        private static readonly List<IngredientKind> startingDeck=new List<IngredientKind>();
        private static int startingYen;
        public static bool Tutorial { get; private set; }
        public static bool KnifeTutorial { get; private set; }
        public static bool BossTutorial { get; private set; }
        public static int TutorialPhase=>BossTutorial?2:KnifeTutorial?1:0;
        public static int Remaining=>deck.Count;
        private static DishKind[] normalBossOrders;
        public static DishKind NormalBossOrder(int index)=>normalBossOrders[Mathf.Clamp(index,0,GluttonRules.NormalCustomers-1)];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession(){Yen=0;Tutorial=false;KnifeTutorial=false;BossTutorial=false;supplies.Clear();deck.Clear();startingDeck.Clear();normalBossOrders=null;TutorialTotals.Reset();}
        public static void SelectTutorial(bool knife){KnifeTutorial=knife;BossTutorial=false;Tutorial=false;}
        public static void SelectBoss(){SelectTutorial(false);BossTutorial=true;EnsureOrders();}
        private static void EnsureOrders()
        {
            if(normalBossOrders!=null)return;
            normalBossOrders=new DishKind[GluttonRules.NormalCustomers];
            for(int i=0;i<normalBossOrders.Length;i++)normalBossOrders[i]=Random.value<.5f?DishKind.EbiNigiri:DishKind.MaguroNigiri;
        }
        public static void PrepareUnified(){EnsureOrders();SelectTutorial(false);}
        public static void BeginVerticalSlice()
        {
            ResetSession();
            var legacy=Resources.Load<LevelConfig>("Levels/LegacyDefault");
            if(legacy==null)throw new System.InvalidOperationException("Legacy regression config missing.");
            ActiveLevel.Select(legacy,"Explicit legacy regression entry");
        }
        public static void BeginBeginnerLevel()
        {
            ResetSession();
            var level=Resources.Load<LevelConfig>("Levels/Level01");
            if(level==null)throw new System.InvalidOperationException("Level01 missing; beginner entry cannot use legacy balance.");
            ActiveLevel.Select(level,"Normal beginner entry");
        }
        public static void Begin()
        {
            ActiveLevel.Select(null); // Tutorial entry explicitly selects legacy scripted compatibility.
            if(TutorialPhase==0)
            {
                TutorialTotals.Reset();
                // One level-provided supply, sufficient for all three current tutorial phases.
                supplies.Clear();Add(IngredientKind.Rice,40);Add(IngredientKind.Shrimp,16);Add(IngredientKind.Tuna,16);Add(IngredientKind.Salmon,22);
            }
            Tutorial=true;EnsureOrders();deck.Clear();deck.AddRange(supplies);supplies.Clear();
            var prefix=BossTutorial?new IngredientKind[0]:KnifeTutorial?new[]{IngredientKind.Salmon,IngredientKind.Salmon,IngredientKind.Rice,IngredientKind.Rice}:new[]{IngredientKind.Shrimp,IngredientKind.Shrimp,IngredientKind.Shrimp,IngredientKind.Shrimp,IngredientKind.Rice,IngredientKind.Rice};
            var arranged=RiceDrawRules.Arrange(deck,prefix);deck.Clear();deck.AddRange(arranged);
            if(TutorialPhase==0){startingDeck.Clear();startingDeck.AddRange(deck);startingYen=Yen;}
        }
        private static void Add(IngredientKind kind,int count){for(int i=0;i<count;i++)supplies.Add(kind);}
        public static void AdvancePhase(){if(TutorialPhase==0)SelectTutorial(true);else SelectBoss();Begin();}
        public static void Replay(){if(!Tutorial){var level=ActiveLevel.Config;ResetSession();ActiveLevel.Select(level,"Replay");return;}KnifeTutorial=false;BossTutorial=false;TutorialTotals.Reset();deck.Clear();deck.AddRange(startingDeck);supplies.Clear();Yen=startingYen;}
        public static bool TryDraw(out IngredientKind kind){kind=default;if(deck.Count==0)return false;int index=deck.Count-1;kind=deck[index];deck.RemoveAt(index);return true;}
        public static void Reward(int amount){Yen+=amount;}
        public static void CarryRemaining(IngredientCardHand hand,IngredientStove box)
        {
            supplies.AddRange(deck);deck.Clear();
            if(hand!=null)foreach(var card in hand.GetComponentsInChildren<IngredientCard>())if(card.isActiveAndEnabled)supplies.Add(card.Ingredient);
            if(box!=null)for(int i=0;i<IngredientRules.Count;i++)Add((IngredientKind)i,box.StoredCount((IngredientKind)i));
        }
    }
}

