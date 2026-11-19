using TMPro;
using UnityEngine;
namespace CookingCards
{
    [CreateAssetMenu(menuName="Summon Sushi/UI Theme")]
    public sealed class GameUiTheme:ScriptableObject
    {
        public TMP_FontAsset font;
        public Sprite buttonSprite;
        public static readonly Color Ink=new Color32(0x6A,0x41,0x41,255);
        public static readonly Color Paper=new Color32(0xFB,0xF2,0xE2,255);
        public void Apply(GameObject root)
        {
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
                if(font!=null && (text.font!=font || text.fontSharedMaterial==null || text.fontSharedMaterial.GetTexture(ShaderUtilities.ID_MainTex)!=font.atlasTexture))
                {text.font=font;text.fontSharedMaterial=font.material;text.ForceMeshUpdate(true,true);}
            foreach(var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                bool tab=button.GetComponentInParent<RecipeJournal>()!=null && button.name.EndsWith("Tab") && button.name!="MENUTab";
                bool artwork=tab || button.name=="DrawCardButton" || button.GetComponent<IngredientCard>()!=null;
                var image=button.targetGraphic as UnityEngine.UI.Image;
                if(!artwork && image!=null && image.sprite!=buttonSprite)
                {
                    if(button.name=="PawHandToggle" && image.sprite!=null && button.transform.Find("ButtonIcon")==null)
                    {
                        var icon=new GameObject("ButtonIcon",typeof(RectTransform),typeof(UnityEngine.UI.Image));icon.transform.SetParent(button.transform,false);
                        var r=(RectTransform)icon.transform;r.anchorMin=new Vector2(.12f,.12f);r.anchorMax=new Vector2(.88f,.88f);r.sizeDelta=Vector2.zero;
                        var visual=icon.GetComponent<UnityEngine.UI.Image>();visual.sprite=image.sprite;visual.preserveAspect=true;visual.raycastTarget=false;
                    }
                    image.sprite=buttonSprite;image.color=Color.white;image.type=UnityEngine.UI.Image.Type.Sliced;image.preserveAspect=false;
                    var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.97f,.92f);colors.pressedColor=new Color(.88f,.81f,.73f);colors.selectedColor=Color.white;colors.disabledColor=new Color(1,1,1,.55f);button.colors=colors;
                }
                if(!artwork)foreach(var text in button.GetComponentsInChildren<TMP_Text>(true))
                {if(text.color!=Ink)text.color=Ink;if(text.outlineWidth!=0)text.outlineWidth=0;}
            }
            foreach(var toggle in root.GetComponentsInChildren<UnityEngine.UI.Toggle>(true))
            {
                Round(toggle.targetGraphic as UnityEngine.UI.Image);
                if(toggle.graphic!=null)toggle.graphic.color=Ink;
                foreach(var text in toggle.GetComponentsInChildren<TMP_Text>(true))if(text.color!=Ink)text.color=Ink;
            }
            foreach(var slider in root.GetComponentsInChildren<UnityEngine.UI.Slider>(true))
            {Round(slider.GetComponent<UnityEngine.UI.Image>());Round(slider.targetGraphic as UnityEngine.UI.Image);}
        }
        private void Round(UnityEngine.UI.Image image)
        {if(image==null || image.sprite==buttonSprite)return;image.sprite=buttonSprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;image.preserveAspect=false;}
    }
}
