using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CookingCards
{
    public enum RecipeJournalTab { Common, Uncommon, Rare, Special, Menu }

    /// <summary>Non-modal recipe reference. Reads the same active catalog as the Summon Box.</summary>
    public sealed class RecipeJournal : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private bool previewInEditor;
        [SerializeField] private RecipeJournalTab previewCategory;
        [SerializeField] private LevelConfig previewLevel;
        [SerializeField,Min(0)] private int previewRecipeIndex;

        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall-=RefreshEditorVisibility;
            UnityEditor.EditorApplication.delayCall+=RefreshEditorVisibility;
        }
        private void RefreshEditorVisibility(){if(this!=null)EditorApplyPreview(false);}
        public void EditorSetPreviewVisibility(bool visible)
        {
            if(binderPanel!=null&&gameObject.scene.IsValid())binderPanel.SetActive(visible);
        }
        public void EditorApplyPreview(bool refreshContent)
        {
            if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||!gameObject.scene.IsValid())return;
            EditorSetPreviewVisibility(previewInEditor);
            if(!previewInEditor||!refreshContent||binderPanel==null)return;
            var source=new List<SushiRecipe>();
            if(previewLevel!=null)
            {foreach(var recipe in previewLevel.activeRecipes??System.Array.Empty<LevelRecipe>())if(recipe!=null)source.Add(recipe.Build());}
            else source.AddRange(SushiRecipeCatalog.All);
            source.RemoveAll(r=>previewCategory==RecipeJournalTab.Menu?r.Service!=RecipeService.DirectOrder:r.Service!=RecipeService.Conveyor||(int)r.Grade!=(int)previewCategory);
            int page=Mathf.Clamp(previewRecipeIndex,0,Mathf.Max(0,source.Count-1));
            EditorText(recipeText,source.Count>0?FormatRecipe(source[page]):"No recipes");
            EditorText(pageLabel,source.Count>0?(page+1)+" / "+source.Count:"0 / 0");
            EditorText(binderPanel.transform.Find("Page/Header")?.GetComponent<TMP_Text>(),previewCategory==RecipeJournalTab.Menu?"SIDE MENU":previewCategory.ToString().ToUpper()+" RECIPES");
            UnityEditor.SceneView.RepaintAll();
        }
        private static void EditorText(TMP_Text label,string value)
        {
            if(label==null)return;
            UnityEditor.Undo.RecordObject(label,"Preview recipe category");label.text=value;
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(label);UnityEditor.EditorUtility.SetDirty(label);
        }
#endif
        [SerializeField] private GameObject binderPanel;
        [SerializeField] private UnityEngine.UI.Button openButton, previousButton, nextButton;
        [SerializeField] private UnityEngine.UI.Button[] tabButtons;
        [SerializeField] private TMP_Text recipeText, pageLabel;
        [Header("Notebook artwork — edit each RectTransform in the Scene")]
        [SerializeField] private UnityEngine.UI.Image binderBase,recipePaper,binderRings;
        [SerializeField] private UnityEngine.UI.Button[] stickyTabs;
        private readonly int[] pageByTab = new int[5];
        private List<SushiRecipe> recipes;
        public RecipeJournalTab CurrentTab { get; private set; }
        public bool IsOpen => binderPanel != null && binderPanel.activeSelf;
        public SushiRecipe CurrentRecipe => recipes != null && recipes.Count > 0 ? recipes[pageByTab[(int)CurrentTab]] : null;

        public void Configure(GameObject panel, UnityEngine.UI.Button opener, UnityEngine.UI.Button previous,
            UnityEngine.UI.Button next, UnityEngine.UI.Button[] tabs, TMP_Text content, TMP_Text counter)
        { binderPanel=panel;openButton=opener;previousButton=previous;nextButton=next;tabButtons=tabs;recipeText=content;pageLabel=counter; }

        private void Awake()
        {
            openButton.onClick.AddListener(Toggle);
            previousButton.onClick.AddListener(Previous);
            nextButton.onClick.AddListener(Next);
            for(int i=0;i<tabButtons.Length;i++)
            { int index=i;tabButtons[i].onClick.AddListener(()=>ShowTab(index)); }
            ShowTab(0);
            binderPanel.SetActive(false);
        }

        private void Update()
        {
            if(IsOpen && CurrentRecipe!=null)
            {
                var box=Object.FindFirstObjectByType<IngredientStove>();
                if(box!=null)
                {
                    var text=new StringBuilder("<b>").Append(CurrentRecipe.DisplayName).Append("</b>\n\n");
                    foreach(var item in CurrentRecipe.Ingredients)
                    {
                        int missing=Mathf.Max(0,item.Count-box.StoredCount(item.Kind));
                        text.Append(item.Count).Append(' ').Append(IngredientRules.DisplayName(item.Kind));
                        text.Append(missing>0?" (need "+missing+")":" (ready)").Append('\n');
                    }
                    recipeText.text=text.ToString().TrimEnd();
                }
            }
            if(Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished)return;
            if(UnityEngine.EventSystems.EventSystem.current != null)
            {
                var selected=UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
                if(selected!=null && selected.GetComponent<TMP_InputField>()!=null)return;
            }
            if(Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame)Toggle();
        }

        public void Toggle()
        {
            if(Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished)return;
            binderPanel.SetActive(!binderPanel.activeSelf);
            if(binderPanel.activeSelf)GuidedLevelTutorial.Notify(LevelGuideStep.Journal);
        }
        public void ShowTab(int index)
        {
            if(index<0 || index>=5)return;
            CurrentTab=(RecipeJournalTab)index;
            recipes=RecipesForTab(CurrentTab);
            RefreshPage();
        }
        public void Next() => ChangePage(1);
        public void Previous() => ChangePage(-1);
        private void ChangePage(int direction)
        {
            if(recipes.Count==0)return;
            int tab=(int)CurrentTab;
            pageByTab[tab]=(pageByTab[tab]+direction+recipes.Count)%recipes.Count;
            RefreshPage();
        }
        private void RefreshPage()
        {
            var recipe=CurrentRecipe;
            recipeText.text=recipe==null?"No recipes":FormatRecipe(recipe);
            pageLabel.text=recipe==null?"0 / 0":(pageByTab[(int)CurrentTab]+1)+" / "+recipes.Count;
            previousButton.interactable=nextButton.interactable=recipes.Count>1;
            var page=binderPanel.transform.Find("Page");
            if(page!=null)
            {
                var header=page.Find("Header")?.GetComponent<TMP_Text>();
                if(header!=null)header.text=CurrentTab==RecipeJournalTab.Menu?"SIDE MENU":CurrentTab.ToString().ToUpper()+" RECIPES";
            }
        }
        public static List<SushiRecipe> RecipesForTab(RecipeJournalTab tab)
        {
            var result=new List<SushiRecipe>();
            foreach(var recipe in ActiveLevel.Recipes)
                if(tab==RecipeJournalTab.Menu ? recipe.Service==RecipeService.DirectOrder :
                    recipe.Service==RecipeService.Conveyor && (int)recipe.Grade==(int)tab)result.Add(recipe);
            return result;
        }
        public static string FormatRecipe(SushiRecipe recipe)
        {
            var text=new StringBuilder("<b>").Append(recipe.DisplayName).Append("</b>\n\n");
            foreach(var amount in recipe.Ingredients)
                if(amount.Kind!=IngredientKind.Rice)text.Append(amount.Count).Append(' ').Append(IngredientRules.DisplayName(amount.Kind)).Append('\n');
            int rice=recipe.Required(IngredientKind.Rice);
            if(rice>0)text.Append(rice).Append(" Rice");
            return text.ToString().TrimEnd();
        }
    }
}

