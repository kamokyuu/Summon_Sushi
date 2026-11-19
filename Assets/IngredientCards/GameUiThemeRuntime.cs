using UnityEngine;
namespace CookingCards
{
    /// <summary>Also styles UI created by gameplay scripts after scene load.</summary>
    public sealed class GameUiThemeRuntime:MonoBehaviour
    {
        private GameUiTheme theme;
        private float nextApply;
        [SerializeField, Tooltip("Enable only to deliberately restyle UI at runtime. Leave off to preserve Inspector artwork and styling.")]
        private bool applyAutomatically;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Object.FindFirstObjectByType<GameUiThemeRuntime>()!=null)return;
            var go=new GameObject("GameUiTheme");Object.DontDestroyOnLoad(go);go.AddComponent<GameUiThemeRuntime>();
        }
        private void Awake(){theme=Resources.Load<GameUiTheme>("GameUiTheme");}
        private void LateUpdate()
        {
            if(!applyAutomatically || theme==null || Time.unscaledTime<nextApply)return;
            nextApply=Time.unscaledTime+.2f;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(canvas.isRootCanvas)theme.Apply(canvas.gameObject);
        }
    }
}
