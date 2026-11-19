using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
namespace CookingCards.Menu
{
    public sealed class LevelMapController:MonoBehaviour
    {
        [SerializeField] private RectTransform character,tutorialNode;
        [SerializeField] private UnityEngine.UI.Button enterButton;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private float speed=260,interactionRadius=110;
        [SerializeField] private string tutorialScene="MainScene";
        private bool loading;
        public void Configure(RectTransform player,RectTransform node,UnityEngine.UI.Button button,TMP_Text label){character=player;tutorialNode=node;enterButton=button;hint=label;}
        private void Start(){if(LevelMapReturnState.HasPosition)character.anchoredPosition=LevelMapReturnState.Position;PreparationSession.PrepareUnified();}
        private void Update()
        {
            if(loading)return;var k=Keyboard.current;Vector2 move=Vector2.zero;
            if(k!=null){move.x=(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);move.y=(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0);if(k.escapeKey.wasPressedThisFrame)Back();}
            var p=character.anchoredPosition+Vector2.ClampMagnitude(move,1)*speed*Time.unscaledDeltaTime;p.x=Mathf.Clamp(p.x,-530,530);p.y=Mathf.Clamp(p.y,-230,180);character.anchoredPosition=p;
            bool near=Vector2.Distance(p,tutorialNode.anchoredPosition)<=interactionRadius;enterButton.interactable=near;
            hint.text="TUTORIAL: Merge → Knife → Glutton\nSupplies provided by level. "+(near?"Press E to enter.":"Walk to the Tutorial node.");
            if(near && k!=null && k.eKey.wasPressedThisFrame)EnterTutorial();
        }
        public void EnterTutorial(){if(loading||Vector2.Distance(character.anchoredPosition,tutorialNode.anchoredPosition)>interactionRadius)return;PreparationSession.Begin();loading=true;LevelMapReturnState.Remember(character.anchoredPosition);Time.timeScale=1;SceneManager.LoadScene(tutorialScene);}
        public void Back(){if(!loading)SceneManager.LoadScene("MainMenu");}
    }
}
