using UnityEngine;
namespace CookingCards.Menu
{
    public sealed class MenuEnvironmentDrift:MonoBehaviour
    {
        [System.Serializable]public sealed class Layer
        {
            public RectTransform target;
            public Vector2 amplitudePixels=new Vector2(6,2);
            public float phase;
            public bool protectFullBackground;
            [System.NonSerialized]public Vector3 home;
        }
        [SerializeField,Min(1)]private float loopSeconds=20;
        [SerializeField]private Layer[] layers;
        private Canvas canvas;
        private float age;
        public void Configure(Layer[] targets){layers=targets;}
        private void OnEnable(){canvas=GetComponentInParent<Canvas>();age=0;if(layers!=null)foreach(var layer in layers)if(layer.target!=null)layer.home=layer.target.localPosition;}
        private void LateUpdate()
        {
            if(layers==null)return;age+=Time.unscaledDeltaTime;
            foreach(var layer in layers)
            {
                if(layer.target==null)continue;float t=age*Mathf.PI*2/Mathf.Max(1,loopSeconds);
                var pixels=new Vector2((Mathf.Sin(t+layer.phase)-Mathf.Sin(layer.phase))*.5f*layer.amplitudePixels.x,Mathf.Sin(t*.5f)*layer.amplitudePixels.y);
                var delta=canvas!=null?layer.target.parent.InverseTransformVector(canvas.transform.TransformVector(pixels/Mathf.Max(.01f,canvas.scaleFactor))):(Vector3)pixels;
                if(layer.protectFullBackground&&layer.target.parent is RectTransform parent)
                {
                    var box=layer.target.rect;var size=layer.target.localScale;
                    float minX=parent.rect.xMax-(layer.home.x+box.xMax*size.x),maxX=parent.rect.xMin-(layer.home.x+box.xMin*size.x);
                    float minY=parent.rect.yMax-(layer.home.y+box.yMax*size.y),maxY=parent.rect.yMin-(layer.home.y+box.yMin*size.y);
                    delta.x=minX<=maxX?Mathf.Clamp(delta.x,minX,maxX):0;delta.y=minY<=maxY?Mathf.Clamp(delta.y,minY,maxY):0;
                }
                layer.target.localPosition=layer.home+delta;
            }
        }
        private void OnDisable(){if(layers!=null)foreach(var layer in layers)if(layer.target!=null)layer.target.localPosition=layer.home;}
    }
}
