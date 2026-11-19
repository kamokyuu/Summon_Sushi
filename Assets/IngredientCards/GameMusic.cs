using UnityEngine;
namespace CookingCards
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameMusic:MonoBehaviour
    {
        private static GameMusic instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance(){instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Object.FindFirstObjectByType<GameMusic>()!=null)return;
            var prefab=Resources.Load<GameObject>("GameMusic");
            if(prefab!=null)Object.Instantiate(prefab);
        }
        private void Awake()
        {
            if(instance!=null && instance!=this){Destroy(gameObject);return;}
            instance=this;DontDestroyOnLoad(gameObject);
        }
        private void OnDestroy(){if(instance==this)instance=null;}
    }
}
