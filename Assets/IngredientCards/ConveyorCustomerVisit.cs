using UnityEngine;
namespace CookingCards
{
    public sealed class ConveyorCustomerVisit:MonoBehaviour
    {
        public enum VisitState { Entering, Ordering, Leaving }
        public VisitState State {get;private set;}
        private ConveyorCustomers owner;
        private ConveyorController belt;
        private TMPro.TMP_Text label;
        private RectTransform rect;
        private float target,outside,age;
        private int quantity;
        public void Configure(ConveyorCustomers system,ConveyorController conveyor,TMPro.TMP_Text text,float start,float stop,int count,float groundY=80)
        {owner=system;belt=conveyor;label=text;outside=start;target=stop;quantity=count;rect=(RectTransform)transform;rect.anchoredPosition=new Vector2(start,groundY);State=VisitState.Entering;}
        private void Update()
        {
            if(Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished || rect==null)return;
            if(State==VisitState.Entering)
            {
                var p=rect.anchoredPosition;p.x=Mathf.MoveTowards(p.x,target,300*Time.deltaTime);rect.anchoredPosition=p;
                if(Mathf.Abs(p.x-target)<.1f)State=VisitState.Ordering;
            }
            if(State==VisitState.Ordering)
            {
                int bought=belt.BuyAvailable(quantity);
                if(bought==0)return; // Wait for stock without a patience timer.
                owner.RecordPurchase(bought);label.text="Bought "+bought+" sushi";State=VisitState.Leaving;age=0;
            }
            else if(State==VisitState.Leaving)
            {
                age+=Time.deltaTime;if(age<1)return;
                var p=rect.anchoredPosition;p.x=Mathf.MoveTowards(p.x,outside,300*Time.deltaTime);rect.anchoredPosition=p;
                if(Mathf.Abs(p.x-outside)<.1f){owner.Departed();Destroy(gameObject);}
            }
        }
    }
}
