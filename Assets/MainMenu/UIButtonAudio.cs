using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CookingCards.Menu
{
    [DisallowMultipleComponent, RequireComponent(typeof(Button))]
    public sealed class UIButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Button button;
        private bool inside, dedicatedDraw;
        private readonly ClickSound clickSound = new ClickSound();
        // A plain C# listener survives destruction of the button during a scene-changing action.
        private sealed class ClickSound
        {
            public bool eligible;
            private int lastClickFrame = -1;
            public void Invoke()
            {
                if (!eligible || lastClickFrame == Time.frameCount) return;
                lastClickFrame = Time.frameCount;
                UIAudioManager.Play(true);
            }
        }
        private void OnEnable() { Prepare(); }
        internal void Prepare()
        {
            if (button == null) button = GetComponent<Button>();
            dedicatedDraw = button.name == "DrawCardButton";
            for (int i=0;i<button.onClick.GetPersistentEventCount();i++)
                if(button.onClick.GetPersistentTarget(i) is IngredientCardHand && button.onClick.GetPersistentMethodName(i)=="DrawCard")dedicatedDraw=true;
            clickSound.eligible = !dedicatedDraw && isActiveAndEnabled && button.IsActive() && button.IsInteractable();
            // Rebind once: dynamic UI can replace or clear its onClick event.
            button.onClick.RemoveListener(clickSound.Invoke);
            if(!dedicatedDraw)button.onClick.AddListener(clickSound.Invoke);
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (inside) return;
            inside = true;
            if (!dedicatedDraw && isActiveAndEnabled && button.IsActive() && button.IsInteractable()) UIAudioManager.Play(false);
        }
        public void OnPointerExit(PointerEventData eventData) { inside = false; }
        private void OnDisable() { inside = false; }
    }
}
