using UnityEngine;
namespace CookingCards
{
    public sealed class TutorialGuide : MonoBehaviour
    {
        public static int Step { get; private set; }
        public static int Draws { get; private set; }
        [SerializeField] private TMPro.TMP_Text label;
        [SerializeField] private GameObject recipeReference;
        private Menu.TutorialDialogueOverlay dialogue;
        private int shownStep=-1,shownDraws=-1;
        public static bool CanDeposit => !PreparationSession.Tutorial || PreparationSession.BossTutorial || Step>=2;
        public static bool CanDraw => !PreparationSession.Tutorial || PreparationSession.BossTutorial || Step>=1;
        public static void Merged() { if(PreparationSession.Tutorial && !PreparationSession.KnifeTutorial && Step==0) Step=1; }
        public static void Cut() { if(PreparationSession.Tutorial && PreparationSession.KnifeTutorial && Step==0) Step=2; }
        public static void Cooked() { if(PreparationSession.Tutorial && Step==2) Step=3; }
        public static void Drew() { if(PreparationSession.Tutorial && Step==1 && ++Draws>=3) Step=2; }
        private void Awake() { Step=0;Draws=0; }
        private void Start()
        {
            dialogue=GetComponentInParent<Canvas>().GetComponentInChildren<Menu.TutorialDialogueOverlay>(true);
            EnsureUi();
            if(label!=null)label.gameObject.SetActive(PreparationSession.Tutorial&&dialogue==null);
            if(recipeReference!=null)recipeReference.SetActive(PreparationSession.Tutorial);
            if(!PreparationSession.Tutorial){enabled=false;return;}
        }
        public void EnsureUi()
        {
            if(dialogue!=null)return;
            if(label!=null && recipeReference!=null)return;
            var go=new GameObject("TutorialInstruction",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));go.transform.SetParent(GetComponentInParent<Canvas>().transform,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-15);rect.sizeDelta=new Vector2(1000,90);
            label=go.GetComponent<TMPro.TextMeshProUGUI>();label.font=TMPro.TMP_Settings.defaultFontAsset;label.fontSize=24;label.color=Color.white;label.outlineWidth=.2f;label.outlineColor=Color.black;label.alignment=TMPro.TextAlignmentOptions.Center;label.raycastTarget=false;
            var recipes=new GameObject("SeafoodRecipeReference",typeof(RectTransform),typeof(UnityEngine.UI.Image));recipes.transform.SetParent(go.transform.parent,false);
            var rr=(RectTransform)recipes.transform;rr.anchorMin=rr.anchorMax=new Vector2(0,1);rr.pivot=new Vector2(0,1);rr.anchoredPosition=new Vector2(15,-105);rr.sizeDelta=new Vector2(330,115);
            var bg=recipes.GetComponent<UnityEngine.UI.Image>();bg.color=new Color(.08f,.08f,.12f,.9f);bg.raycastTarget=false;
            recipeReference=recipes;
            var textObject=new GameObject("Recipes",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));textObject.transform.SetParent(recipes.transform,false);((RectTransform)textObject.transform).sizeDelta=new Vector2(315,110);
            var rt=textObject.GetComponent<TMPro.TextMeshProUGUI>();rt.font=TMPro.TMP_Settings.defaultFontAsset;rt.fontSize=20;rt.alignment=TMPro.TextAlignmentOptions.Center;rt.raycastTarget=false;rt.text="SEAFOOD RECIPES\nEbi: 2 Rice + 1 Shrimp\nMaguro: 2 Rice + 2 Tuna\nSalmon: 2 Rice + 3 Salmon";
        }
        private void Update()
        {
            if(CustomerSystem.GameFinished) { if(label!=null)label.text="";dialogue?.HideDialogue();return; }
            if(PreparationSession.KnifeTutorial)
            {
                Publish(Step==0?"KNIFE TUTORIAL: Drag KNIFE onto Salmon.\n1 Salmon becomes 2 Tuna. Knife has 3 uses.":Step==2?"STEP 2: Put 2 Rice + 2 Tuna into the summon box.\nCustomer timers start after this first summon.":"Customers now have 20 seconds! Watch their patience bars.\nEbi = 2 Rice + 1 Shrimp / Maguro = 2 Rice + 2 Tuna");
                return;
            }
            Publish(Step==0?"MERGE TUTORIAL: Merge Shrimp + Shrimp into Tuna.":Step==1?"STEP 2: Click DRAW three times ("+Draws+"/3).":"STEP 3: Merge the new Shrimp pair, then summon with 2 Rice + 2 Tuna.\nEbi = 2 Rice + 1 Shrimp");
        }
        private void Publish(string text)
        {
            if(dialogue==null){if(label!=null)label.text=text;return;}
            if(shownStep==Step&&shownDraws==Draws)return;
            shownStep=Step;shownDraws=Draws;dialogue.ShowLegacyInstruction(Step,PreparationSession.KnifeTutorial,text);
        }
    }
}
