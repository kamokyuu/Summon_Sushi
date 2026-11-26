using UnityEngine;
using UnityEngine.EventSystems;
namespace CookingCards
{
    public sealed class ConveyorPlate : MonoBehaviour, IDropHandler
    {
        public RecipeGrade Grade { get; private set; }
        public bool IsBroken { get; private set; }
        public SushiRecipe ServedRecipe { get; private set; }
        public float SaleMultiplier {get;private set;}=1;
        private bool shielded;
        private ConveyorController owner;
        private UnityEngine.UI.Image image;
        private Sprite brokenSprite;
        private RectTransform foodVisual;
        private bool foodAnimating;
        private bool purchased;
        public void Configure(ConveyorController belt,RecipeGrade grade,Sprite normal,Sprite broken)
        {owner=belt;Grade=grade;brokenSprite=broken;image=GetComponent<UnityEngine.UI.Image>();image.sprite=normal;image.preserveAspect=true;}
        public bool TryServe(SushiRecipe recipe)
        {
            if(ComboShopBoss.EncounterPaused||CustomerSystem.GameFinished || Menu.GameplayPauseController.IsPaused || IsBroken || ServedRecipe!=null || recipe==null || recipe.Service!=RecipeService.Conveyor || recipe.Grade!=Grade)return false;
            ServedRecipe=recipe;owner.RecordServed(Grade);
            SaleMultiplier=ComboShopBoss.Active!=null?ComboShopBoss.Active.RecordFill():1;
            GuidedLevelTutorial.Notify(LevelGuideStep.Summon,recipe);
            GuidedLevelTutorial.Notify(LevelGuideStep.PlateMatch,recipe);
            var sprite=owner.FoodSprite(recipe);
            if(sprite!=null)
            {
                var food=new GameObject("ServedFood",typeof(RectTransform),typeof(UnityEngine.UI.Image));food.transform.SetParent(transform,false);
                var foodRect=(RectTransform)food.transform;float inset=(1-owner.FoodScale)*.5f;
                foodRect.anchorMin=new Vector2(inset,inset);foodRect.anchorMax=new Vector2(1-inset,1-inset);foodRect.sizeDelta=Vector2.zero;
                ApplyRecipeVisual(foodRect,recipe,false);
                var foodImage=food.GetComponent<UnityEngine.UI.Image>();foodImage.sprite=sprite;foodImage.preserveAspect=true;foodImage.raycastTarget=false;
                foodVisual=foodRect;
                GameplayHudFeedback.Active?.AnimateServing(foodRect);
                return true;
            }
            var background=new GameObject("RecipePlaceholder",typeof(RectTransform),typeof(UnityEngine.UI.Image));background.transform.SetParent(transform,false);
            var br=(RectTransform)background.transform;br.anchorMin=new Vector2(.1f,.15f);br.anchorMax=new Vector2(.9f,.85f);br.sizeDelta=Vector2.zero;background.GetComponent<UnityEngine.UI.Image>().color=new Color(.18f,.23f,.25f);background.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var go=new GameObject("ServedRecipe",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));go.transform.SetParent(background.transform,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(6,6);rect.offsetMax=new Vector2(-6,-6);
            var text=go.GetComponent<TMPro.TextMeshProUGUI>();text.font=TMPro.TMP_Settings.defaultFontAsset;text.text=recipe.DisplayName;text.enableAutoSizing=true;text.fontSizeMin=10;text.fontSizeMax=17;text.color=Color.white;text.outlineWidth=.3f;text.outlineColor=Color.black;text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;
            foodVisual=br;
            ApplyRecipeVisual(br,recipe,true);
            GameplayHudFeedback.Active?.AnimateServing(br);
            return true;
        }
        private void ApplyRecipeVisual(RectTransform visual,SushiRecipe recipe,bool placeholder)
        {
            var settings=RecipeCatalog.FindVisual(recipe.Id);
            float size=settings!=null&&settings.foodScale>0?settings.foodScale:owner.FoodScale;
            var inset=new Vector2((1-size)*.5f,(1-size*(placeholder?.875f:1))*.5f);
            visual.anchorMin=inset;visual.anchorMax=Vector2.one-inset;visual.sizeDelta=Vector2.zero;
            visual.anchoredPosition=settings!=null?new Vector2(settings.offsetX,settings.offsetY):Vector2.zero;
            visual.localRotation=Quaternion.Euler(0,0,settings!=null?settings.rotationZ:0);
        }
        public void AnimateFood(RectTransform visual,Vector3 origin,RectTransform canvas,float arcHeight,float duration,float bounce)
        {
            if(foodAnimating||visual==null||visual!=foodVisual||!isActiveAndEnabled)return;
            foodAnimating=true;
            Menu.UIAudioManager.PlayServe();
            StartCoroutine(FlyFood(visual,origin,canvas,arcHeight,Mathf.Clamp(duration,.5f,.7f),bounce));
        }
        public void RemoveAfterPurchase()
        {
            if(purchased)return;
            purchased=true;
            if(!foodAnimating){gameObject.SetActive(false);Destroy(gameObject);return;}
            // Already removed from purchasable stock; keep both visuals until landing finishes.
            image.raycastTarget=false;
        }
        private System.Collections.IEnumerator FlyFood(RectTransform visual,Vector3 origin,RectTransform canvas,float arcHeight,float duration,float bounce)
        {
            var plate=(RectTransform)transform;
            var anchorMin=visual.anchorMin;var anchorMax=visual.anchorMax;var pivot=visual.pivot;
            var size=visual.sizeDelta;var position=visual.anchoredPosition3D;var scale=visual.localScale;var rotation=visual.localRotation;
            var destination=plate.InverseTransformPoint(visual.TransformPoint(visual.rect.center));
            float width=canvas.InverseTransformVector(visual.TransformVector(Vector3.right*visual.rect.width)).magnitude;
            float height=canvas.InverseTransformVector(visual.TransformVector(Vector3.up*visual.rect.height)).magnitude;
            Vector3 start=canvas.InverseTransformPoint(origin);
            Quaternion flightRotation=Quaternion.Inverse(canvas.rotation)*visual.rotation;
            visual.SetParent(canvas,false);visual.anchorMin=visual.anchorMax=new Vector2(.5f,.5f);visual.pivot=new Vector2(.5f,.5f);
            visual.sizeDelta=new Vector2(width,height);visual.localScale=Vector3.one;visual.localRotation=flightRotation;
            visual.localPosition=start;
            float pixels=Mathf.Max(.01f,canvas.GetComponent<Canvas>().scaleFactor),age=0;
            try
            {
                while(visual!=null&&isActiveAndEnabled&&age<duration)
                {
                    if(Menu.GameplayPauseController.IsPaused){yield return null;continue;}
                    if(purchased&&owner!=null&&!GuidedLevelTutorial.Protecting&&!ComboShopBoss.ConveyorStopped&&!CustomerSystem.GameFinished)
                        plate.anchoredPosition+=Vector2.left*(owner.EffectiveSpeed*Time.deltaTime);
                    float t=Mathf.Clamp01(age/duration),travel=Mathf.Clamp01(t/.8f);
                    float eased=travel*travel*(3-2*travel);
                    Vector3 end=canvas.InverseTransformPoint(plate.TransformPoint(destination));
                    visual.localPosition=Vector3.Lerp(start,end,eased)+Vector3.up*(Mathf.Sin(travel*Mathf.PI)*arcHeight/pixels);
                    float land=Mathf.Clamp01((t-.8f)/.2f),squash=Mathf.Sin(land*Mathf.PI*2)*(1-land)*bounce;
                    visual.localScale=new Vector3(1+squash,1-squash,1);
                    age+=Time.unscaledDeltaTime;yield return null;
                }
            }
            finally
            {
                foodAnimating=false;
                if(visual!=null&&isActiveAndEnabled)
                {
                    visual.SetParent(transform,false);visual.anchorMin=anchorMin;visual.anchorMax=anchorMax;visual.pivot=pivot;
                    visual.sizeDelta=size;visual.anchoredPosition3D=position;visual.localScale=scale;visual.localRotation=rotation;
                }
                else if(visual!=null){visual.gameObject.SetActive(false);Destroy(visual.gameObject);}
                if(purchased&&this!=null){gameObject.SetActive(false);Destroy(gameObject);}
            }
        }
        private void OnDisable()
        {
            if(foodVisual!=null){foodVisual.gameObject.SetActive(false);Destroy(foodVisual.gameObject);foodVisual=null;}
            foodAnimating=false;
        }
        public void BreakIfEmpty()
        {
            if(GuidedLevelTutorial.Protecting||ComboShopBoss.ConveyorStopped||shielded||CustomerSystem.GameFinished||IsBroken||ServedRecipe!=null)return;
            if(ComboShopBoss.Active!=null&&ComboShopBoss.Active.TryShieldMiss()){shielded=true;return;}
            IsBroken=true;image.sprite=brokenSprite;owner.RecordMissed(Grade);
            Menu.UIAudioManager.PlayPlateBreak();
        }
        public void OnDrop(PointerEventData data)
        {if(data.pointerDrag!=null)data.pointerDrag.GetComponent<CookedDish>()?.TryServeOn(this);}
    }
}
