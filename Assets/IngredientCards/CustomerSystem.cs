using System.Collections.Generic;
using UnityEngine;

namespace CookingCards
{
    public sealed class CustomerSystem : MonoBehaviour
    {
        [SerializeField] private Sprite[] characterSprites;
        [SerializeField] private Sprite[] dishSprites;
        [SerializeField] private TMPro.TMP_Text coinLabel;
        [SerializeField, Range(1, 3)] private int maximumCustomers = 3;
        [SerializeField, Min(1)] private float spawnInterval = 5;
        [SerializeField, Min(30)] private float walkingSpeed = 330;
        [SerializeField, Min(.1f)] private float patienceSeconds = 5;
        [SerializeField, Min(1)] private float tutorialBossPatienceSeconds = 65;
        [SerializeField, Min(1)] private float tutorialCustomerPatienceSeconds = 20;
        [SerializeField, Min(0)] private float tutorialBossBreatherSeconds = 3;
        private int bossTimeouts;
        private float nextBossCourse;
        public bool HasPatienceTimer => !PreparationSession.Tutorial || PreparationSession.KnifeTutorial || PreparationSession.BossTutorial;
        public float PatienceSeconds => PreparationSession.Tutorial && PreparationSession.KnifeTutorial ? 20f : patienceSeconds;
        public float PatienceFor(bool boss) => PreparationSession.BossTutorial ? (boss?tutorialBossPatienceSeconds:tutorialCustomerPatienceSeconds) : PatienceSeconds;
        public float BossBreather => PreparationSession.BossTutorial?tutorialBossBreatherSeconds:0;
        private int bossServed, regularServed, timedOut;
        private bool failed;
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TMPro.TMP_Text resultsText;
        private readonly List<NpcCustomer> customers = new List<NpcCustomer>();
        private readonly int[] servedDishes = new int[5];
        private readonly int[] servedQuality = new int[3];
        private float nextSpawn;
        private int spawned;
        private int tunaOrders;
        private int served;
        public const int CoinGoal = 10;
        public static CustomerSystem Active { get; private set; }
        public static bool GameFinished => (Active != null && Active.HasEnded) || (DayShiftController.Active!=null && DayShiftController.Active.HasEnded);
        public bool HasEnded { get; private set; }
        public int Misses { get; private set; }
        public int ServedCount(DishKind kind) => servedDishes[(int)kind];
        public int Coins { get; private set; }
        public int ActiveCount => customers.Count;
        public IReadOnlyList<NpcCustomer> Customers => customers;
        public float OutsideX => ((RectTransform)transform.parent).rect.width * .5f + 240;

        public void Configure(Sprite[] portraits, Sprite[] dishes, TMPro.TMP_Text label)
        { characterSprites = portraits; dishSprites = dishes; coinLabel = label; RefreshCoins(); }
        private void Awake() => Active = this;
        private void OnDestroy() { if (Active == this) Active = null; }
        public void ConfigureResults(GameObject panel, TMPro.TMP_Text label) { resultsPanel = panel; resultsText = label; }
        public void RecordConveyorPurchase(int yen){Coins+=yen;RefreshCoins();}
        private void Start()
        {
            Coins = 0; Misses = 0; HasEnded = false;
            System.Array.Clear(servedDishes, 0, servedDishes.Length);
            if (resultsPanel != null) resultsPanel.SetActive(false);
            RefreshCoins(); nextSpawn = Time.time + 2.5f;
            if(!PreparationSession.Tutorial)
                {(GetComponent<ConveyorCustomers>()??gameObject.AddComponent<ConveyorCustomers>()).Configure(characterSprites);(GetComponent<DayShiftController>()??gameObject.AddComponent<DayShiftController>()).Configure(resultsPanel,resultsText);}
        }
        public Sprite DishSprite(DishKind kind) => (int)kind<dishSprites.Length?dishSprites[(int)kind]:null;
        private void Update()
        {
            // The old timed customer loop belongs to the retired tutorial.
            // Jam conveyor failure and direct-order scheduling will be introduced separately.
            if(!PreparationSession.Tutorial)return;
            if (Menu.GameplayPauseController.IsPaused || HasEnded || Time.time < nextSpawn || customers.Count >= Mathf.Clamp(maximumCustomers, 1, 3)) return;
            SpawnCustomer();
            nextSpawn = Time.time + spawnInterval;
        }
        public NpcCustomer SpawnCustomer()
        {
            if(PreparationSession.BossTutorial && spawned>=1+GluttonRules.NormalCustomers)return null;
            if(PreparationSession.Tutorial && !PreparationSession.BossTutorial && spawned>=3) return null;
            if (HasEnded || customers.Count >= Mathf.Clamp(maximumCustomers, 1, 3) || characterSprites == null || characterSprites.Length == 0) return null;
            int slot = 0;
            while (customers.Exists(customer => customer.Slot == slot)) slot++;
            var requested = new DishKind[Random.Range(1, 4)];
            for (int i = 0; i < requested.Length; i++) requested[i] = (DishKind)Random.Range(0, 3);
            if(PreparationSession.Tutorial) requested=new[]{TutorialOrders.At(spawned)};
            if(PreparationSession.Tutorial) { if(tunaOrders>=2)requested[0]=DishKind.EbiNigiri;if(requested[0]==DishKind.MaguroNigiri)tunaOrders++; }
            spawned++;
            bool boss=PreparationSession.BossTutorial && spawned==1;
            if(PreparationSession.BossTutorial)requested=boss?new[]{DishKind.SalmonNigiri,DishKind.SalmonNigiri,DishKind.SalmonNigiri,DishKind.SalmonNigiri,DishKind.SalmonNigiri}:new[]{PreparationSession.NormalBossOrder(spawned-2)};
            var go = new GameObject(boss?"TheGlutton":"Customer_" + (slot + 1), typeof(RectTransform), typeof(NpcCustomer));
            go.transform.SetParent(transform, false);
            var customer = go.GetComponent<NpcCustomer>();
            customer.Configure(this, slot, OutsideX * (Random.value < .5f ? -1 : 1), (slot - 1) * 310, walkingSpeed,
                characterSprites[Random.Range(0, characterSprites.Length)], requested);
            customers.Add(customer);
            if(boss)SpawnCustomer();
            return customer;
        }
        public bool TryServe(DishKind dish, int qualityStars = 1)
        {
            if (HasEnded || (int)dish < 0 || (int)dish >= servedDishes.Length) return false;
            var serviceOrder=new List<NpcCustomer>(customers);
            serviceOrder.Sort((a,b)=>a.PatienceRemaining.CompareTo(b.PatienceRemaining));
            foreach (var customer in serviceOrder)
            {
                if (!customer.Accept(dish)) continue;
                served++;
                if(customer.IsGlutton)bossServed++;else regularServed++;
                if(customer.IsGlutton)nextBossCourse=Time.time+4;
                int revenue=SushiQualityRules.SalePrice(dish,qualityStars);
                Coins+=revenue;
                PreparationSession.Reward(revenue);
                if(PreparationSession.Tutorial)TutorialTotals.Record(dish,qualityStars,revenue);
                servedDishes[(int)dish]++;
                servedQuality[Mathf.Clamp(qualityStars,1,3)-1]++;
                RefreshCoins();
                if (PreparationSession.Tutorial && !PreparationSession.BossTutorial && served>=3)
                {
                    PreparationSession.CarryRemaining(Object.FindFirstObjectByType<IngredientCardHand>(),Object.FindFirstObjectByType<IngredientStove>());
                    PreparationSession.AdvancePhase();
                    HasEnded=true;
                    UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
                }
                else if (PreparationSession.BossTutorial?bossServed>=GluttonRules.Target && regularServed>=GluttonRules.NormalCustomers:served >= CoinGoal) EndGame();
                return true;
            }
            Misses++;
            return false;
        }
        public string ResultsSummary() =>
            "<size=36><b>"+(failed?"SHIFT FAILED":"SHIFT COMPLETE")+"</b></size>\n¥" + Coins + "\n\n" +
            "Ebi Nigiri (" + ServedCount(DishKind.EbiNigiri) + "x)\n" +
            "Maguro Nigiri (" + ServedCount(DishKind.MaguroNigiri) + "x)\n" +
            "Salmon Maki (" + ServedCount(DishKind.SalmonMaki) + "x)\n" +
            "Tamago Nigiri (" + ServedCount(DishKind.TamagoNigiri) + "x)\n" +
            "Salmon Nigiri (" + ServedCount(DishKind.SalmonNigiri) + "x)\n\n" +
            "Quality: 1-star " + servedQuality[0] + " / 2-star " + servedQuality[1] + " / 3-star " + servedQuality[2] + "\n" +
            served + " served, " + Misses + " miss / " + timedOut + " left";

        public void CustomerTimedOut(NpcCustomer customer)
        {
            if(HasEnded)return;
            timedOut++;Misses++;
            if(customer.IsGlutton || timedOut>=3) { failed=true;EndGame(); }
            else if(PreparationSession.BossTutorial || PreparationSession.Tutorial) spawned--;
        }

        private void EndGame()
        {
            HasEnded = true;
            PreparationSession.CarryRemaining(Object.FindFirstObjectByType<IngredientCardHand>(),Object.FindFirstObjectByType<IngredientStove>());
            if (resultsText != null) resultsText.text = PreparationSession.Tutorial?(failed?"TUTORIAL FAILED":"TUTORIAL COMPLETE")+TutorialTotals.Summary():ResultsSummary();
            if (resultsPanel != null) { resultsPanel.SetActive(true); resultsPanel.transform.SetAsLastSibling(); }
            if(resultsPanel!=null)
            {
                var navigation=resultsPanel.GetComponent<Menu.ResultsNavigation>();
                if(navigation==null)navigation=resultsPanel.AddComponent<Menu.ResultsNavigation>();
                navigation.Show();
            }
            var hand = Object.FindFirstObjectByType<IngredientCardHand>();
            if (hand != null)
            {
                foreach (var card in hand.GetComponentsInChildren<IngredientCard>()) card.ResetInteraction();
                var group = hand.GetComponent<CanvasGroup>();
                if (group != null) { group.blocksRaycasts = false; group.interactable = false; }
            }
            var draw = GameObject.Find("DrawCardButton");
            if (draw != null) draw.GetComponent<UnityEngine.UI.Button>().interactable = false;
        }
        public void CustomerLeft(NpcCustomer customer) => customers.Remove(customer);
        private void RefreshCoins() { if (coinLabel != null) coinLabel.text = "¥ " + PreparationSession.Yen + (PreparationSession.BossTutorial?" / GLUTTON SALMON "+bossServed+"/5 / Customers "+regularServed+"/"+GluttonRules.NormalCustomers+"\nBoss: 65s TOTAL / Customer: 20s":PreparationSession.Tutorial?" / "+(PreparationSession.KnifeTutorial?"KNIFE":"MERGE")+" "+served+"/3":""); }
    }
}

