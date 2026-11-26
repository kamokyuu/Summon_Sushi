using UnityEngine;
namespace CookingCards
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class GameMusic:MonoBehaviour
    {
        [SerializeField] private AudioClip gameplayClip;
        private AudioClip defaultClip;
        private bool defaultLoop;
        private static GameMusic instance;
        private bool stoppedForCompletion;
        public static void StopForCompletion()
        {
            if(instance==null)return;
            instance.GetComponent<AudioSource>().Stop();instance.stoppedForCompletion=true;
        }
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
            var source=GetComponent<AudioSource>();defaultClip=source.clip;defaultLoop=source.loop;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded+=SceneLoaded;
            ApplySceneMusic(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),true);
        }
        private void SceneLoaded(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if(mode==UnityEngine.SceneManagement.LoadSceneMode.Single)ApplySceneMusic(scene,false);
        }
        private void ApplySceneMusic(UnityEngine.SceneManagement.Scene scene,bool initial)
        {
            var source=GetComponent<AudioSource>();
            bool gameplay=scene.name=="MainScene"&&gameplayClip!=null;
            var desired=gameplay?gameplayClip:defaultClip;
            bool play=source.isPlaying||stoppedForCompletion||(initial&&source.playOnAwake);
            if(source.clip!=desired){source.Stop();source.clip=desired;}
            source.loop=gameplay||defaultLoop;stoppedForCompletion=false;
            if(play&&source.clip!=null&&!source.isPlaying)source.Play();
        }
        private void OnDestroy(){UnityEngine.SceneManagement.SceneManager.sceneLoaded-=SceneLoaded;if(instance==this)instance=null;}
    }
}
