using UnityEngine;

namespace CookingCards.Menu
{
    public static class GameSettings
    {
        public static float Volume { get => PlayerPrefs.GetFloat("Settings.Volume", 1); set { PlayerPrefs.SetFloat("Settings.Volume", value); AudioListener.volume = value; } }
        public static bool Fullscreen { get => PlayerPrefs.GetInt("Settings.Fullscreen", 1) == 1; set { PlayerPrefs.SetInt("Settings.Fullscreen", value ? 1 : 0); Screen.fullScreen = value; } }
        public static void Apply() { AudioListener.volume = Volume; Screen.fullScreen = Fullscreen; }
        public static void Save() => PlayerPrefs.Save();
    }
}
