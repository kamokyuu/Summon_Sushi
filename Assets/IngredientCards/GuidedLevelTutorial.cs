using System.Linq;
using UnityEngine;
namespace CookingCards
{
    public sealed class GuidedLevelTutorial:MonoBehaviour
    {
        public static GuidedLevelTutorial Active {get;private set;}
        public static bool Protecting=>Active!=null&&Active.IsGuiding;
        public bool IsGuiding {get;private set;}
        public LevelGuideStep Current=>ActiveLevel.Config.guidedSteps[index];
        private int index;
        private bool prepared;
        private IngredientCardHand hand;
        private GameObject panel;
        [SerializeField] private TMPro.TMP_Text instruction;
        private SushiRecipe recipe;
        private IngredientKind mergeInput,mergeOutput;
        private Menu.TutorialDialogueOverlay dialogue;
        private string currentInstruction;
        private string InstructionText {set{currentInstruction=value;if(dialogue==null&&instruction!=null)instruction.text=value;}}
        private void Awake(){Active=this;IsGuiding=!ActiveLevel.Config.campaignShift&&!ActiveLevel.Legacy&&ActiveLevel.Config.guidedSteps!=null&&ActiveLevel.Config.guidedSteps.Length>0;hand=GetComponent<IngredientCardHand>();}
        private void OnDestroy(){if(Active==this)Active=null;if(panel!=null)Destroy(panel);}
        private void Update()
        {
            if(!IsGuiding||hand.IsDealing||Menu.GameplayPauseController.IsPaused||CustomerSystem.GameFinished)return;
            if(!prepared){EnsureUi();Prepare();prepared=true;}
            if(Current==LevelGuideStep.Summon||Current==LevelGuideStep.PlateMatch)
            {
                var belt=Object.FindFirstObjectByType<ConveyorController>();
                if(!belt.Plates.Any(p=>!p.IsBroken&&p.ServedRecipe==null&&p.Grade==recipe.Grade))belt.SpawnPlate(recipe.Grade,0);
            }
        }
        public void Skip(){IsGuiding=false;if(panel!=null)panel.SetActive(false);dialogue?.HideDialogue();}
        public static void Notify(LevelGuideStep action,SushiRecipe served=null)
        {
            var guide=Active;
            if(guide==null||!guide.IsGuiding||guide.hand.IsDealing||!guide.prepared||guide.Current!=action)return;
            if(served!=null&&guide.recipe!=null&&served.Id!=guide.recipe.Id)return;
            guide.index++;guide.prepared=false;
            if(guide.index>=ActiveLevel.Config.guidedSteps.Length){guide.Skip();return;}
            guide.Prepare();guide.prepared=true;
        }
        private void Prepare()
        {
            recipe=ActiveLevel.Recipes.FirstOrDefault(r=>r.Service==RecipeService.Conveyor&&(Current!=LevelGuideStep.PlateMatch||r.Grade!=RecipeGrade.Common))??ActiveLevel.Recipes[0];
            switch(Current)
            {
                case LevelGuideStep.Draw:InstructionText="DRAW: Click the draw pile to take a card.";break;
                case LevelGuideStep.Journal:InstructionText="RECIPE JOURNAL: Open the journal to see this level's recipes.";break;
                case LevelGuideStep.Summon:
                case LevelGuideStep.PlateMatch:
                    foreach(var item in recipe.Ingredients)EnsureCards(item.Kind,item.Count);
                    var belt=Object.FindFirstObjectByType<ConveyorController>();belt.SpawnPlate(recipe.Grade,0);
                    InstructionText=(Current==LevelGuideStep.Summon?"SUMMON: ":"PLATE MATCHING: ")+recipe.DisplayName+" — "+string.Join(" + ",recipe.Ingredients.Select(x=>x.Count+" "+IngredientRules.DisplayName(x.Kind)))+".\nDrop ingredients into the box for a "+recipe.Grade+" plate.";break;
                case LevelGuideStep.Serve:InstructionText="SERVE: A customer will buy your cooked dish from the conveyor.";break;
                case LevelGuideStep.Merge:
                    foreach(var kind in ActiveLevel.Config.activeIngredients)if(IngredientRules.TryMerge(kind,kind,out var result)&&ActiveLevel.Config.Allows(result)){mergeInput=kind;mergeOutput=result;break;}
                    EnsureCards(mergeInput,2);InstructionText="MERGE: Drag "+IngredientRules.DisplayName(mergeInput)+" onto another "+IngredientRules.DisplayName(mergeInput)+" to make "+IngredientRules.DisplayName(mergeOutput)+".";break;
                case LevelGuideStep.Knife:
                    EnsureCards(mergeOutput,1);hand.SetCollapsed(false);
                    if(hand.CardCount>=ActiveLevel.Config.maximumHandCapacity){var spare=hand.GetComponentsInChildren<IngredientCard>().FirstOrDefault(c=>c.Ingredient!=mergeOutput);if(spare!=null)hand.ConsumeCard(spare);}
                    InstructionText="KNIFE: Drag the knife onto "+IngredientRules.DisplayName(mergeOutput)+" to split it. Only successful cuts use a charge (2 maximum).";break;
            }
            dialogue?.ShowTutorialInstruction(Current,currentInstruction);
        }
        private void EnsureCards(IngredientKind kind,int needed)
        {
            int count=hand.GetComponentsInChildren<IngredientCard>().Count(c=>c.Ingredient==kind);
            while(count++<needed)
            {
                if(hand.CardCount>=ActiveLevel.Config.maximumHandCapacity)
                {
                    var all=hand.GetComponentsInChildren<IngredientCard>();
                    var spare=all.FirstOrDefault(c=>c.Ingredient!=kind&&(recipe==null||all.Count(x=>x.Ingredient==c.Ingredient)>recipe.Required(c.Ingredient)));
                    if(spare==null)spare=hand.GetComponentsInChildren<IngredientCard>().FirstOrDefault(c=>c.Ingredient!=kind);
                    if(spare==null)return;hand.ConsumeCard(spare);
                }
                hand.TryReturnCard(kind);
            }
            hand.SetCollapsed(false);
        }
        private void EnsureUi()
        {
            if(dialogue==null)dialogue=GetComponentInParent<Canvas>().GetComponentInChildren<Menu.TutorialDialogueOverlay>(true);
            if(dialogue!=null){if(instruction!=null&&instruction.GetComponentInParent<Menu.TutorialDialogueOverlay>()==null)instruction.gameObject.SetActive(false);return;}
            if(panel!=null)return;
            panel=new GameObject("LevelTutorial",typeof(RectTransform),typeof(UnityEngine.UI.Image));panel.transform.SetParent(GetComponentInParent<Canvas>().transform,false);
            var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-105);rect.sizeDelta=new Vector2(900,105);
            var image=panel.GetComponent<UnityEngine.UI.Image>();image.sprite=GameplayHudFeedback.Active?.panelSprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=GameUiTheme.Paper;image.raycastTarget=false;
            var label=new GameObject("Instruction",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(panel.transform,false);
            rect=(RectTransform)label.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(12,8);rect.offsetMax=new Vector2(-120,-8);
            instruction=label.GetComponent<TMPro.TMP_Text>();instruction.font=Resources.Load<GameUiTheme>("GameUiTheme")?.font??TMPro.TMP_Settings.defaultFontAsset;instruction.fontSize=22;instruction.color=GameplayHudFeedback.Ink;instruction.alignment=TMPro.TextAlignmentOptions.Center;instruction.raycastTarget=false;
            var skip=new GameObject("Skip",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));skip.transform.SetParent(panel.transform,false);rect=(RectTransform)skip.transform;rect.anchorMin=rect.anchorMax=new Vector2(1,.5f);rect.pivot=new Vector2(1,.5f);rect.anchoredPosition=new Vector2(-8,0);rect.sizeDelta=new Vector2(100,40);skip.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Skip);
            var text=new GameObject("Label",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));text.transform.SetParent(skip.transform,false);rect=(RectTransform)text.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
            var tmp=text.GetComponent<TMPro.TMP_Text>();tmp.font=TMPro.TMP_Settings.defaultFontAsset;tmp.text="SKIP";tmp.fontSize=20;tmp.color=Color.black;tmp.alignment=TMPro.TextAlignmentOptions.Center;tmp.raycastTarget=false;
        }
    }
}
