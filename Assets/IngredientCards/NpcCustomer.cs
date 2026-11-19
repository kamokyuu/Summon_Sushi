using System.Collections.Generic;
using UnityEngine;

namespace CookingCards
{
    public sealed class NpcCustomer : MonoBehaviour
    {
        public enum VisitState { Entering, Ordering, Leaving }
        public VisitState State { get; private set; }
        public int RemainingCount => order.Count;
        public IReadOnlyList<DishKind> Order => order;
        private readonly List<DishKind> order = new List<DishKind>();
        private CustomerSystem owner;
        private RectTransform rect;
        private RectTransform portrait;
        private RectTransform bubble;
        private float destination;
        private float speed;
        private float patience;
        private UnityEngine.UI.Image patienceBar;
        public bool IsGlutton { get; private set; }
        public float PatienceRemaining => patience;
        private int gluttonServed;
        private float courseReadyAt;
        public int Slot { get; private set; }

        public void Configure(CustomerSystem system, int slot, float startX, float stopX, float walkSpeed, Sprite character, DishKind[] requested)
        {
            owner = system; Slot = slot; speed = walkSpeed; destination = stopX;
            IsGlutton=gameObject.name=="TheGlutton";
            patience=owner.PatienceFor(IsGlutton);
            rect = (RectTransform)transform;
            rect.anchoredPosition = new Vector2(startX, 0);
            State = VisitState.Entering;
            order.AddRange(requested);
            var face = new GameObject("CustomerPortrait", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            face.transform.SetParent(transform, false);
            portrait = (RectTransform)face.transform;
            portrait.sizeDelta = new Vector2(190, 210);
            var image = face.GetComponent<UnityEngine.UI.Image>();
            image.sprite = character; image.preserveAspect = true; image.raycastTarget = false;
            var panel = new GameObject("OrderBubble", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(transform, false);
            bubble = (RectTransform)panel.transform;
            bubble.anchoredPosition = new Vector2(0, 135);
            bubble.sizeDelta = new Vector2(IsGlutton?350:205, 82);
            var background = panel.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(1, .96f, .85f, .96f); background.raycastTarget = false;
            RebuildOrder();
            var track=new GameObject("PatienceBar",typeof(RectTransform),typeof(UnityEngine.UI.Image));track.transform.SetParent(transform,false);
            var tr=(RectTransform)track.transform;tr.sizeDelta=new Vector2(190,14);tr.anchoredPosition=new Vector2(0,195);track.GetComponent<UnityEngine.UI.Image>().color=Color.black;track.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var fill=new GameObject("Fill",typeof(RectTransform),typeof(UnityEngine.UI.Image));fill.transform.SetParent(track.transform,false);
            var fr=(RectTransform)fill.transform;fr.anchorMin=Vector2.zero;fr.anchorMax=Vector2.one;fr.sizeDelta=Vector2.zero;fr.pivot=new Vector2(0,.5f);
            patienceBar=fill.GetComponent<UnityEngine.UI.Image>();patienceBar.color=IsGlutton?new Color(1,.4f,.15f):Color.green;patienceBar.raycastTarget=false;
            track.SetActive(owner.HasPatienceTimer);
            bubble.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Menu.GameplayPauseController.IsPaused) return;
            if (owner == null || owner.HasEnded) return;
            if (State == VisitState.Ordering)
            {
                if(!owner.HasPatienceTimer)return;
                // Introduce the timer only after the guided cut-and-summon step is complete.
                if(PreparationSession.KnifeTutorial && TutorialGuide.Step<3)return;
                if(Time.time<courseReadyAt)return;
                patience-=Time.deltaTime;patienceBar.rectTransform.localScale=new Vector3(Mathf.Clamp01(patience/owner.PatienceFor(IsGlutton)),1,1);
                if(patience<=0) { State=VisitState.Leaving;bubble.gameObject.SetActive(false);destination=owner.OutsideX;owner.CustomerTimedOut(this); }
                return;
            }
            var pos = rect.anchoredPosition;
            pos.x = Mathf.MoveTowards(pos.x, destination, speed * Time.unscaledDeltaTime);
            rect.anchoredPosition = pos;
            portrait.anchoredPosition = new Vector2(0, Mathf.Sin(Time.unscaledTime * 13) * 4);
            if (Mathf.Abs(pos.x - destination) > .1f) return;
            portrait.anchoredPosition = Vector2.zero;
            if (State == VisitState.Entering)
            {
                State = VisitState.Ordering;
                bubble.gameObject.SetActive(true);
            }
            else
            {
                owner.CustomerLeft(this);
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
        }

        public bool Accept(DishKind dish)
        {
            if (State != VisitState.Ordering) return false;
            int index = order.IndexOf(dish);
            if(IsGlutton && index!=0)return false;
            if (index < 0) return false;
            order.RemoveAt(index);
            RebuildOrder();
            if (order.Count == 0)
            {
                State = VisitState.Leaving;
                bubble.gameObject.SetActive(false);
                destination = owner.OutsideX * (Random.value < .5f ? -1 : 1);
            }
            return true;
        }

        private void RebuildOrder()
        {
            foreach (Transform child in bubble) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            for (int i = 0; i < order.Count; i++)
            {
                var icon = new GameObject(order[i].ToString(), typeof(RectTransform), typeof(UnityEngine.UI.Image));
                icon.transform.SetParent(bubble, false);
                var ir = (RectTransform)icon.transform;
                ir.sizeDelta = new Vector2(60, 58);
                ir.anchoredPosition = new Vector2((i - (order.Count - 1) * .5f) * 65, 0);
                var image = icon.GetComponent<UnityEngine.UI.Image>();
                image.sprite = owner.DishSprite(order[i]); image.preserveAspect = true; image.raycastTarget = false;
                if(order[i]==DishKind.EbiNigiri || order[i]==DishKind.MaguroNigiri)
                {
                    image.color=new Color(1,.65f,.4f);
                    var label=new GameObject("OrderLabel",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));label.transform.SetParent(icon.transform,false);
                    ((RectTransform)label.transform).sizeDelta=new Vector2(60,58);
                    var text=label.GetComponent<TMPro.TextMeshProUGUI>();text.font=TMPro.TMP_Settings.defaultFontAsset;text.text=order[i]==DishKind.EbiNigiri?"EBI":"TUNA";text.fontSize=18;text.color=Color.black;text.alignment=TMPro.TextAlignmentOptions.Center;text.raycastTarget=false;
                }
            }
        }
        public void RetryTimedOrder()
        {
            State=VisitState.Ordering;patience=owner.PatienceFor(IsGlutton);courseReadyAt=Time.time+owner.BossBreather;bubble.gameObject.SetActive(true);
        }
    }
}
