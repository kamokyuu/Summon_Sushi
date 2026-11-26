using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace CookingCards.Editor
{
    public sealed class FoodArtworkWindow:EditorWindow
    {
        [SerializeField]private Sprite foodSprite;
        private string[] recipeIds=Array.Empty<string>();
        private int selected;
        private string result;
        [MenuItem("Tools/Cooking Cards/Food Artwork (All Levels)")]
        private static void Open(){GetWindow<FoodArtworkWindow>("Food Artwork").Show();}
        private static LevelConfig[] Configs()=>AssetDatabase.FindAssets("t:LevelConfig").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<LevelConfig>).Where(c=>c!=null).ToArray();
        private void OnEnable()
        {
            recipeIds=Configs().Where(c=>c.activeRecipes!=null).SelectMany(c=>c.activeRecipes).Where(r=>r!=null&&!string.IsNullOrEmpty(r.id)).Select(r=>r.id).Distinct().OrderBy(id=>id).ToArray();
            selected=Mathf.Max(0,Array.IndexOf(recipeIds,"maguro_nigiri"));
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Ganti gambar makanan sekaligus",EditorStyles.boldLabel);
            if(recipeIds.Length==0){EditorGUILayout.HelpBox("Tidak ada resep dalam LevelConfig.",MessageType.Info);return;}
            selected=EditorGUILayout.Popup("Recipe ID",Mathf.Clamp(selected,0,recipeIds.Length-1),recipeIds);
            foodSprite=(Sprite)EditorGUILayout.ObjectField("Sprite baru",foodSprite,typeof(Sprite),false);
            EditorGUILayout.HelpBox("Menerapkan gambar ke semua level/hari yang memakai Recipe ID ini. Resep dan gameplay tidak berubah.",MessageType.Info);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlaying||foodSprite==null))
                if(GUILayout.Button("Apply to All Levels"))ApplyArtwork();
            if(!string.IsNullOrEmpty(result))EditorGUILayout.HelpBox(result,MessageType.Info);
        }
        private void ApplyArtwork()
        {
            if(EditorApplication.isPlaying||foodSprite==null)return;
            string id=recipeIds[selected];int changed=0;Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Replace food artwork across levels");
            foreach(var config in Configs())
            {
                bool used=config.activeRecipes!=null&&config.activeRecipes.Any(r=>r!=null&&r.id==id);
                var entries=(config.foodVisuals??Array.Empty<RecipeFoodVisual>()).Where(v=>v!=null).ToList();
                var matching=entries.Where(v=>v.recipeId==id).ToArray();if(!used&&matching.Length==0)continue;
                if(matching.Length>0&&matching.All(v=>v.sprite==foodSprite))continue;
                Undo.RecordObject(config,"Replace food artwork");
                if(matching.Length==0)entries.Add(new RecipeFoodVisual{recipeId=id,sprite=foodSprite});else foreach(var visual in matching)visual.sprite=foodSprite;
                config.foodVisuals=entries.ToArray();EditorUtility.SetDirty(config);changed++;
            }
            AssetDatabase.SaveAssets();Undo.CollapseUndoOperations(group);result="Selesai: "+changed+" konfigurasi diperbarui untuk "+id+".";
        }
    }
}
