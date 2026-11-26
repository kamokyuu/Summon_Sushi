using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace CookingCards.Menu
{
    public sealed class StudioSplash:MonoBehaviour
    {
        [SerializeField]private Texture2D studioLogo;
        [SerializeField,Min(0)]private float fadeInSeconds=.5f,holdSeconds=1.5f,fadeOutSeconds=.5f;
        [SerializeField]private string mainMenuScene="MainMenu";
        [SerializeField]private RawImage logoView;
        [SerializeField]private AspectRatioFitter aspect;
        [SerializeField]private CanvasGroup logoGroup;
        [SerializeField]private Image background;
        [SerializeField]private Color backgroundColor=new Color(.025f,.025f,.035f,1);
        private static bool shown;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void ResetLaunch()=>shown=false;
        public void Configure(Texture2D logo,RawImage image,AspectRatioFitter fitter,CanvasGroup group,Image backdrop){studioLogo=logo;logoView=image;aspect=fitter;logoGroup=group;background=backdrop;ApplyLogo();}
        private void OnValidate()=>ApplyLogo();
        private void ApplyLogo(){if(logoView!=null)logoView.texture=studioLogo;if(aspect!=null&&studioLogo!=null){aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=(float)studioLogo.width/Mathf.Max(1,studioLogo.height);}if(background!=null)background.color=backgroundColor;}
        private IEnumerator Start()
        {
            ApplyLogo();logoGroup.alpha=0;bool play=!shown;shown=true;
            var loading=SceneManager.LoadSceneAsync(mainMenuScene,LoadSceneMode.Single);
            if(loading==null){Debug.LogError("Studio splash could not load "+mainMenuScene,this);yield break;}
            loading.allowSceneActivation=false;
            if(play){yield return Fade(0,1,fadeInSeconds);yield return new WaitForSecondsRealtime(Mathf.Max(0,holdSeconds));yield return Fade(1,0,fadeOutSeconds);}
            loading.allowSceneActivation=true;
        }
        private IEnumerator Fade(float from,float to,float seconds)
        {
            logoGroup.alpha=from;if(seconds<=0){logoGroup.alpha=to;yield break;}
            float elapsed=0;while(elapsed<seconds){elapsed+=Time.unscaledDeltaTime;logoGroup.alpha=Mathf.Lerp(from,to,Mathf.Clamp01(elapsed/seconds));yield return null;}logoGroup.alpha=to;
        }
    }
}
