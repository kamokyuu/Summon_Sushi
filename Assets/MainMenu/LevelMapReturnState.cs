using UnityEngine;
namespace CookingCards.Menu
{
    /// <summary>Map position for this play session, independent of purchases and tutorial resets.</summary>
    public static class LevelMapReturnState
    {
        public static bool HasPosition { get; private set; }
        public static Vector2 Position { get; private set; }
        public static void Remember(Vector2 position) { Position=position;HasPosition=true; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { HasPosition=false;Position=Vector2.zero; }
    }
}
