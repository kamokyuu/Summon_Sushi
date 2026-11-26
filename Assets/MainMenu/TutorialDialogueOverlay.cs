using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingCards.Menu
{
    public sealed class TutorialDialogueOverlay : MonoBehaviour, IPointerClickHandler
    {
        public enum Trigger { Manual, GuidedTutorial, LegacyMergeTutorial, LegacyKnifeTutorial }
        [Serializable] public sealed class Page
        {
            public Trigger trigger;
            public LevelGuideStep tutorialStep;
            public int legacyStep;
            public Sprite character;
            public string speakerName;
            [TextArea(3,8)] public string text;
            public UnityEvent onNext = new UnityEvent();
        }
        [SerializeField] private bool showOnStart=true;
        [Tooltip("Empty: any level. Useful when multiple levels share this scene.")]
        [SerializeField] private LevelConfig[] showForLevels=Array.Empty<LevelConfig>();
        [SerializeField] private Sprite defaultCharacter;
        [SerializeField] private string defaultSpeaker="NPC1";
        [SerializeField] private Page[] pages=Array.Empty<Page>();
        [SerializeField] private int sortingOrder=100;
        [Header("Editable scene UI references")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text speakerLabel,dialogueLabel;
        [SerializeField] private TMP_FontAsset dialogueFont;
        [SerializeField] private Image namePanel;
        [Tooltip("Edit this object's RectTransform directly for position and size.")]
        [SerializeField] private RectTransform dialoguePanel;
        public bool IsOpen {get;private set;}
        private int index,lastAdvanceFrame=-1;
        private bool instructionPage,pending;
        private Trigger pendingTrigger;
        private int pendingStep;
        private string instructionText,pendingText;

        private void Start()
        {
            ConfigureSorting();
            ApplyFont();
            bool allowed=showForLevels==null||showForLevels.Length==0||Array.IndexOf(showForLevels,ActiveLevel.Config)>=0;
            if(showOnStart&&allowed)ShowDialogue();else HideDialogue();
        }
        private void ConfigureSorting(){var canvas=GetComponent<Canvas>();if(canvas!=null){canvas.overrideSorting=true;canvas.sortingOrder=sortingOrder;}}
        private void OnValidate(){ConfigureSorting();ApplyFont();}
        private void ApplyFont(){if(dialogueFont==null)return;if(speakerLabel!=null)speakerLabel.font=dialogueFont;if(dialogueLabel!=null)dialogueLabel.font=dialogueFont;}
        public void Bind(CanvasGroup canvasGroup,Image character,TMP_Text speaker,TMP_Text dialogue)
        {group=canvasGroup;portrait=character;speakerLabel=speaker;dialogueLabel=dialogue;}
        public void ShowDialogue()
        {
            if(pages==null||pages.Length==0){HideDialogue();return;}
            index=Array.FindIndex(pages,p=>p!=null&&p.trigger==Trigger.Manual);
            if(index<0){HideDialogue();return;}
            instructionPage=false;OpenPage();
        }
        private void OpenPage()
        {
            IsOpen=true;
            if(group!=null){group.alpha=1;group.blocksRaycasts=true;group.interactable=true;}
            ShowPage();
        }
        private void ShowPage()
        {
            var page=pages[index];
            if(portrait!=null){portrait.sprite=page?.character!=null?page.character:defaultCharacter;portrait.preserveAspect=true;portrait.enabled=portrait.sprite!=null;}
            if(speakerLabel!=null)speakerLabel.text=string.IsNullOrEmpty(page?.speakerName)?defaultSpeaker:page.speakerName;
            if(dialogueLabel!=null)dialogueLabel.text=(page?.text??"").Replace("{instruction}",instructionText??"");
        }
        public void ShowTutorialInstruction(LevelGuideStep step,string text){ShowTriggered(Trigger.GuidedTutorial,(int)step,text);}
        public void ShowLegacyInstruction(int step,bool knife,string text){ShowTriggered(knife?Trigger.LegacyKnifeTutorial:Trigger.LegacyMergeTutorial,step,text);}
        private void ShowTriggered(Trigger trigger,int step,string text)
        {
            if(IsOpen&&!instructionPage){pending=true;pendingTrigger=trigger;pendingStep=step;pendingText=text;return;}
            int match=Array.FindIndex(pages,p=>p!=null&&p.trigger==trigger&&(trigger==Trigger.GuidedTutorial?(int)p.tutorialStep:p.legacyStep)==step);
            if(match<0)return;
            index=match;instructionText=text;instructionPage=true;pending=false;OpenPage();
        }
        public void AdvanceDialogue()
        {
            if(!IsOpen||lastAdvanceFrame==Time.frameCount)return;
            lastAdvanceFrame=Time.frameCount;
            var action=pages[index]?.onNext;
            int next=instructionPage?-1:Array.FindIndex(pages,index+1,p=>p!=null&&p.trigger==Trigger.Manual);
            if(next<0){HideDialogue();if(pending)ShowTriggered(pendingTrigger,pendingStep,pendingText);}
            else{index=next;ShowPage();}
            action?.Invoke();
        }
        public void HideDialogue()
        {
            IsOpen=false;
            if(group!=null){group.alpha=0;group.blocksRaycasts=false;group.interactable=false;}
        }
        public void SkipTutorial(){pending=false;GuidedLevelTutorial.Active?.Skip();HideDialogue();}
        public void OnPointerClick(PointerEventData data){if(data.button==PointerEventData.InputButton.Left)AdvanceDialogue();}
        private void OnDisable(){HideDialogue();}
    }
}
