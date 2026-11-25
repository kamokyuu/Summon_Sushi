using UnityEngine;
namespace CookingCards
{
    /// <summary>Optional explicit scene override; an empty hook must not erase menu selection.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class LevelSelection : MonoBehaviour
    {
        [SerializeField] private LevelConfig level;
        private void Awake(){if(level!=null)ActiveLevel.Select(level,"Scene LevelSelection");}
    }
}
