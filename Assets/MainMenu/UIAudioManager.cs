using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

namespace CookingCards.Menu
{
    [DefaultExecutionOrder(-10000)]
    [RequireComponent(typeof(AudioSource))]
    public sealed class UIAudioManager : MonoBehaviour
    {
        [SerializeField] private AudioClip clickClip, hoverClip;
        [SerializeField, Range(0, 1)] private float volume = 1;
        [SerializeField] private AudioClip comboClip;
        [SerializeField] private AudioClip yenClip, plateBreakClip;
        [SerializeField] private AudioClip knifeSlashClip, cardMergeClip;
        [SerializeField] private AudioClip cardDrawClip, powerUpClip;
        [SerializeField] private AudioClip cardSummonClip;
        [SerializeField] private AudioClip serveClip;
        [SerializeField] private AudioClip shiftDoneClip;
        [SerializeField, Range(0, 1)] private float shiftDoneVolume=.7f;
        private bool shiftDonePlayed;
        [SerializeField, Range(0, 1)] private float serveVolume=.3f;
        [SerializeField, Range(0, 1)] private float summonVolume=.3f;
        [Header("SFX multipliers (master volume applies afterward)")]
        [SerializeField, Range(0, 1)] private float hoverVolume=.15f, clickVolume=.35f, drawVolume=.35f;
        [SerializeField, Range(0, 1)] private float mergeVolume=.55f, knifeVolume=.45f, yenVolume=.25f, breakVolume=.4f;
        [SerializeField, Range(0, 1)] private float comboVolumeX1=.05f, comboVolumeX2=.35f, comboVolumeX3=.55f, powerUpVolume=.7f;
        [SerializeField, Range(1, 8)] private int maximumEffectVoices=4;
        [SerializeField, Min(0)] private float sameClipMinimumInterval=.06f;
        private readonly Dictionary<AudioClip,float> lastPlayed=new Dictionary<AudioClip,float>();
        private AudioSource[] effectVoices;
        private int nextVoice;
        [SerializeField, Range(.1f, 3)] private float comboPitchX1 = 1, comboPitchX2 = 1.15f, comboPitchX3 = 1.3f;
        private AudioSource comboSource;
        private static UIAudioManager active;
        private AudioSource source;
        private AudioListener fallbackListener;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() { active = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (active != null) return;
            var prefab = Resources.Load<UIAudioManager>("UIAudioManager");
            if (prefab != null) Instantiate(prefab);
        }

        private void Awake()
        {
            if (active != null && active != this) { Destroy(gameObject); return; }
            active = this;
            DontDestroyOnLoad(gameObject);
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.ignoreListenerPause = true;
            source.ignoreListenerVolume = false;
            effectVoices=new AudioSource[Mathf.Clamp(maximumEffectVoices,1,8)];
            for(int i=0;i<effectVoices.Length;i++)
            {
                var voice=i==0?source:gameObject.AddComponent<AudioSource>();
                voice.playOnAwake=false;voice.spatialBlend=0;voice.ignoreListenerPause=true;voice.ignoreListenerVolume=false;
                effectVoices[i]=voice;
            }
            comboSource = gameObject.AddComponent<AudioSource>();
            comboSource.playOnAwake = false;
            comboSource.spatialBlend = 0;
            comboSource.ignoreListenerPause = true;
            comboSource.ignoreListenerVolume = false;
            AudioListener.volume = GameSettings.Volume;
            fallbackListener = gameObject.AddComponent<AudioListener>();
            SceneManager.sceneLoaded += SceneLoaded;
            RefreshListener();
        }
        private void SceneLoaded(Scene scene, LoadSceneMode mode) { shiftDonePlayed=false;RefreshListener(); AttachButtons(); }
        private void RefreshListener()
        {
            bool external = false;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (listener != fallbackListener && listener.isActiveAndEnabled) { external = true; break; }
            fallbackListener.enabled = !external;
        }

        private void Update() { AttachButtons(); }
        private void AttachButtons()
        {
            foreach (var selectable in Selectable.allSelectablesArray)
            {
                var button = selectable as Button;
                if (button == null) continue;
                var feedback = button.GetComponent<UIButtonAudio>();
                if (feedback == null) feedback = button.gameObject.AddComponent<UIButtonAudio>();
                feedback.Prepare();
            }
        }

        internal static void Play(bool click)
        {
            if (active == null || active.source == null) return;
            var clip = click ? active.clickClip : active.hoverClip;
            PlayEffect(clip,click?active.clickVolume:active.hoverVolume);
        }

        public static void PlayYen() { if(active!=null)PlayEffect(active.yenClip,active.yenVolume); }
        public static void PlayPlateBreak() { if(active!=null)PlayEffect(active.plateBreakClip,active.breakVolume); }
        public static void PlayKnifeSlash() { if(active!=null)PlayEffect(active.knifeSlashClip,active.knifeVolume); }
        public static void PlayCardMerge() { if(active!=null)PlayEffect(active.cardMergeClip,active.mergeVolume); }
        public static void PlayCardDraw(AudioClip clip=null) { if(active!=null)PlayEffect(clip!=null?clip:active.cardDrawClip,active.drawVolume); }
        public static void PlayPowerUp() { if(active!=null)PlayEffect(active.powerUpClip,active.powerUpVolume); }
        public static void PlayCardSummon() { if(active!=null)PlayEffect(active.cardSummonClip,active.summonVolume); }
        public static void PlayServe() { if(active!=null)PlayEffect(active.serveClip,active.serveVolume); }
        public static void PlayShiftDone()
        {
            if(active==null||active.shiftDonePlayed)return;
            active.shiftDonePlayed=true;PlayEffect(active.shiftDoneClip,active.shiftDoneVolume);
        }
        private static void PlayEffect(AudioClip clip,float gain)
        {
            if(active==null||clip==null||active.effectVoices==null)return;
            float now=Time.unscaledTime;
            if(active.lastPlayed.TryGetValue(clip,out float last)&&now-last<active.sameClipMinimumInterval)return;
            active.lastPlayed[clip]=now;
            var voice=active.effectVoices[active.nextVoice];
            foreach(var candidate in active.effectVoices)if(!candidate.isPlaying){voice=candidate;break;}
            active.nextVoice=(active.nextVoice+1)%active.effectVoices.Length;
            voice.Stop();voice.pitch=1;voice.volume=active.volume*gain;voice.clip=clip;voice.Play();
        }

        public static void PlayCombo(int multiplier)
        {
            if (active == null || active.comboSource == null || active.comboClip == null) return;
            float now=Time.unscaledTime;
            if(active.lastPlayed.TryGetValue(active.comboClip,out float last)&&now-last<active.sameClipMinimumInterval)return;
            active.lastPlayed[active.comboClip]=now;
            active.comboSource.Stop();
            active.comboSource.pitch = multiplier <= 1 ? active.comboPitchX1 : multiplier == 2 ? active.comboPitchX2 : active.comboPitchX3;
            active.comboSource.volume=active.volume*(multiplier<=1?active.comboVolumeX1:multiplier==2?active.comboVolumeX2:active.comboVolumeX3);
            active.comboSource.clip=active.comboClip;active.comboSource.Play();
        }

        private void OnDestroy()
        {
            if (active != this) return;
            SceneManager.sceneLoaded -= SceneLoaded;
            active = null;
        }
    }
}
