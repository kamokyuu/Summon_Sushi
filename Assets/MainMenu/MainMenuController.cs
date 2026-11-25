using UnityEngine;
using UnityEngine.SceneManagement;

namespace CookingCards.Menu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject[] panels;
        [SerializeField] private string gameplayScene = "MainScene";
        public void Configure(GameObject[] views) => panels = views;
        private void Start() { GameSettings.Apply(); ShowPanel(-1); }
        public void ShowPanel(int index)
        {
            if (index == 0) { PlayLevelOne(); return; }
            for (int i = 0; i < panels.Length; i++) panels[i].SetActive(i == index);
        }
        public void PlayLevelOne() { PreparationSession.BeginBeginnerLevel(); Time.timeScale = 1; SceneManager.LoadScene(gameplayScene); }
        public void SetVolume(float value) => GameSettings.Volume = value;
        public void SetFullscreen(bool value) => GameSettings.Fullscreen = value;
        private void OnDisable() => GameSettings.Save();
        public void Exit()
        {
            GameSettings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
