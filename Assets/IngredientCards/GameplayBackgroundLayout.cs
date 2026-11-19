using UnityEngine;
namespace CookingCards
{
    [ExecuteAlways]
    public sealed class GameplayBackgroundLayout:MonoBehaviour
    {
        [SerializeField] private RectTransform tabletop,conveyor,canvasRect;
        public void Configure(RectTransform table,RectTransform belt,RectTransform canvas)
        {tabletop=table;conveyor=belt;canvasRect=canvas;Refresh();}
        private void LateUpdate(){Refresh();}
        private void Refresh()
        {
            if(tabletop==null || conveyor==null || canvasRect==null)return;
            float beltBottom=canvasRect.InverseTransformPoint(conveyor.TransformPoint(new Vector3(0,conveyor.rect.yMin,0))).y;
            var size=new Vector2(0,Mathf.Max(1,beltBottom-canvasRect.rect.yMin+1));
            if(tabletop.sizeDelta!=size)tabletop.sizeDelta=size;
        }
    }
}
