using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace CookingCards.Menu
{
    public sealed class GameplayPauseController : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel, settingsPanel;
        private float previousTimeScale = 1;
        public static bool IsPaused { get; private set; }
        public void Configure(GameObject pause, GameObject settings) { pausePanel = pause; settingsPanel = settings; }
        private void Awake() { IsPaused = false; pausePanel.SetActive(false); settingsPanel.SetActive(false); GameSettings.Apply(); }
        private void Update()
        {
            if(CustomerSystem.GameFinished) return;
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (settingsPanel.activeSelf) ShowPause(); else if (IsPaused) Continue(); else Pause();
        }
        public void Pause() { if (IsPaused) return; previousTimeScale = Time.timeScale; IsPaused = true; Time.timeScale = 0; ShowPause(); }
        public void ShowPause() { settingsPanel.SetActive(false); pausePanel.SetActive(true); }
        public void Settings() { pausePanel.SetActive(false); settingsPanel.SetActive(true); }
        public void Continue() { pausePanel.SetActive(false); settingsPanel.SetActive(false); IsPaused = false; Time.timeScale = previousTimeScale; GameSettings.Save(); }
        public void MainMenu() { Continue(); Time.timeScale = 1; SceneManager.LoadScene("MainMenu"); }
        public void SetVolume(float value) => GameSettings.Volume = value;
        public void SetFullscreen(bool value) => GameSettings.Fullscreen = value;
        public void Exit()
        {
            Continue();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void OnDestroy() { if (IsPaused) { IsPaused = false; Time.timeScale = previousTimeScale; } GameSettings.Save(); }
    }
}
