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
        public ConveyorCustomerBehavior Behavior {get;private set;}
        public RecipeGrade? PreferredGrade {get;private set;}
        public int Remaining=>quantity;
        public void Configure(ConveyorCustomers system,ConveyorController conveyor,TMPro.TMP_Text text,float start,float stop,int count,float groundY=80,ConveyorCustomerBehavior behavior=ConveyorCustomerBehavior.Regular,RecipeGrade? preferred=null)
        {owner=system;belt=conveyor;label=text;outside=start;target=stop;quantity=count;Behavior=behavior;PreferredGrade=preferred;rect=(RectTransform)transform;rect.anchoredPosition=new Vector2(start,groundY);State=VisitState.Entering;RefreshBubble();}
        private void RefreshBubble(){if(label==null)return;label.text=Behavior==ConveyorCustomerBehavior.Picky?PreferredGrade?.ToString()??"Any":Behavior==ConveyorCustomerBehavior.Hungry?"×"+quantity:"";}
        private void Update()
        {
            if(ComboShopBoss.EncounterPaused||Menu.GameplayPauseController.IsPaused || CustomerSystem.GameFinished || rect==null)return;
            if(label!=null&&label.transform.parent.name=="PreferenceBubble")label.transform.parent.gameObject.SetActive(!GuidedLevelTutorial.Protecting&&State!=VisitState.Leaving);
            if(State==VisitState.Entering)
            {
                var p=rect.anchoredPosition;p.x=Mathf.MoveTowards(p.x,target,300*Time.deltaTime);rect.anchoredPosition=p;
                if(Mathf.Abs(p.x-target)<.1f)State=VisitState.Ordering;
            }
            if(State==VisitState.Ordering)
            {
                var boss=ComboShopBoss.Active;bool ayce=ActiveLevel.Config.campaignShift&&boss!=null&&boss.BossStarted&&!boss.BossComplete;
                var grade=ayce?boss.AvailableBossGrade:PreferredGrade;
                int bought=belt.BuyAvailable(quantity,grade,rect.TransformPoint(new Vector3(0,100,0)),ayce?boss.AvailableBossRecipe:null);
                if(bought==0)return; // Wait for stock without a patience timer.
                owner.RecordPurchase(bought);quantity-=bought;RefreshBubble();if(quantity>0)return;
                if(label!=null)label.text="";State=VisitState.Leaving;age=0;owner.Departed();
            }
            else if(State==VisitState.Leaving)
            {
                age+=Time.deltaTime;if(age<1)return;
                var p=rect.anchoredPosition;p.x=Mathf.MoveTowards(p.x,outside,300*Time.deltaTime);rect.anchoredPosition=p;
                if(Mathf.Abs(p.x-outside)<.1f)Destroy(gameObject);
            }
        }
    }
}
