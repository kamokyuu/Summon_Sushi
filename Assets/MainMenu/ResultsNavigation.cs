using UnityEngine;
using UnityEngine.SceneManagement;
namespace CookingCards.Menu
{
    /// <summary>Result navigation belongs to UI; scoring and served counts remain in CustomerSystem.</summary>
    public sealed class ResultsNavigation : MonoBehaviour
    {
        public void Show()
        {
            if(transform.Find("ResultsNavigation")!=null){Bind();return;}
            var root=new GameObject("ResultsNavigation",typeof(RectTransform));root.transform.SetParent(transform,false);
            var r=(RectTransform)root.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(0,-255);r.sizeDelta=new Vector2(1000,60);
            Make(root.transform,"MAIN MENU",-180,"MainMenu");Make(root.transform,"REPLAY",180,"MainScene");
            Bind();
        }
        private void Awake(){Bind();}
        public void GoMainMenu(){Time.timeScale=1;SceneManager.LoadScene("MainMenu");}
        public void Replay(){PreparationSession.Replay();Time.timeScale=1;SceneManager.LoadScene("MainScene");}
        private void Bind()
        {
            var root=transform.Find("ResultsNavigation");if(root==null)return;
            var menu=root.Find("MAIN MENU").GetComponent<UnityEngine.UI.Button>();
            var replay=root.Find("REPLAY").GetComponent<UnityEngine.UI.Button>();
            menu.onClick.RemoveListener(GoMainMenu);menu.onClick.AddListener(GoMainMenu);
            replay.onClick.RemoveListener(Replay);replay.onClick.AddListener(Replay);
        }
        private static void Make(Transform parent,string title,float x,string scene)
        {
            var go=new GameObject(title,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchoredPosition=new Vector2(x,0);r.sizeDelta=new Vector2(300,60);
            var image=go.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.2f,.3f,.4f);
            var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var label=new GameObject("Label",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(go.transform,false);((RectTransform)label.transform).sizeDelta=r.sizeDelta;
            var text=label.GetComponent<TMPro.TextMeshProUGUI>();text.font=TMPro.TMP_Settings.defaultFontAsset;text.text=title;text.fontSize=24;text.color=Color.white;text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;
        }
    }
}
