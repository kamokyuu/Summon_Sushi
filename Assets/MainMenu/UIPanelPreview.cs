using UnityEngine;

namespace CookingCards.Menu
{
    public sealed class UIPanelPreview : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] public bool previewInEditor;
        [SerializeField] public GameObject[] hideWhilePreviewing=System.Array.Empty<GameObject>();
        [System.Serializable] public sealed class Visibility
        {
            public GameObject target;
            public bool active;
            public CanvasGroup group;
            public float alpha;
        }
        [SerializeField,HideInInspector] public Visibility[] normalVisibility=System.Array.Empty<Visibility>();
        public static System.Action Changed;
        private void OnValidate(){UnityEditor.EditorApplication.delayCall-=Notify;UnityEditor.EditorApplication.delayCall+=Notify;}
        private void Notify(){if(this!=null&&!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)Changed?.Invoke();}
#endif
    }
}
