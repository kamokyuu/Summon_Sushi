using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace CookingCards.Menu
{
    public sealed class ShiftCompletionReceipt : MonoBehaviour
    {
        [SerializeField] private TMP_Text title,levelName,summary;
        [SerializeField] private string heading="SHIFT COMPLETE";
        [SerializeField,Min(0)] private float upwardDistance=640;
        [SerializeField,Min(.1f)] private float scrollDuration=1.2f;
        private RectTransform rect;
        private Vector2 home;
        private float age;
        private bool presented;
        public bool AnimationFinished=>presented&&age>=scrollDuration;
        public void Present(ConveyorController belt,LevelConfig config)
        {
            if(presented)return;
            presented=true;rect=(RectTransform)transform;home=rect.anchoredPosition;age=0;
            if(title!=null)title.text=heading;
            if(levelName!=null)levelName.text=config.campaignShift?"DAY "+config.campaignDay.ToString("00")+" — "+config.dailyEventTitle:config.name;
            var text=new StringBuilder();
            foreach(var pair in belt.PurchasedRecipes.OrderBy(p=>p.Key))
            {
                var recipe=ActiveLevel.Recipes.FirstOrDefault(r=>r.Id==pair.Key);
                text.Append(recipe!=null?recipe.DisplayName:pair.Key.Replace('_',' ')).Append("  ( ").Append(pair.Value).AppendLine("x )");
            }
            text.AppendLine("────────────────────");
            text.Append("Served: ").Append(belt.PurchasedCount).Append("    Broken plates: ").Append(belt.MissedCount);
            text.Append("\nSales: ¥").Append(belt.Revenue).Append("\nPenalties: -¥").Append(belt.Penalties).Append("\nNet: ¥").Append(belt.Revenue-belt.Penalties);
            if(config.campaignShift&&config.campaignDay==7)text.Append("\n\nWEEK 01 COMPLETE");
            if(summary!=null)summary.text=text.ToString();
            rect.anchoredPosition=home-Vector2.up*upwardDistance;
        }
        private void Update()
        {
            if(!presented||rect==null)return;
            age+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(age/scrollDuration);
            rect.anchoredPosition=home-Vector2.up*(upwardDistance*(1-t*t*(3-2*t)));
        }
        private void OnDisable(){if(rect!=null&&presented)rect.anchoredPosition=home;}
    }
}
