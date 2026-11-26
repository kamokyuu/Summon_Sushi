using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CookingCards.Editor
{
    [CustomEditor(typeof(RecipeCatalog))]
    public sealed class RecipeCatalogEditor : UnityEditor.Editor
    {
        private LevelConfig[] levels;
        private LevelConfig previewLevel;
        private Sprite[] plates=Array.Empty<Sprite>();
        private Vector2 plateSize=new Vector2(145,110);
        private float globalScale=.8f;

        private void OnEnable()
        {
            levels=AssetDatabase.FindAssets("t:LevelConfig").Select(g=>AssetDatabase.LoadAssetAtPath<LevelConfig>(AssetDatabase.GUIDToAssetPath(g))).Where(l=>l!=null).ToArray();
            previewLevel=Resources.Load<LevelConfig>("Levels/Level03");
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/MainScene.unity");
            bool preview=!scene.isLoaded;
            if(preview)scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/MainScene.unity");
            try
            {
                var belt=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ConveyorController>(true)).FirstOrDefault();
                if(belt==null)return;
                var data=new SerializedObject(belt);
                globalScale=Mathf.Clamp(data.FindProperty("foodScale").floatValue,.2f,.9f);
                plateSize=data.FindProperty("plateSize").vector2Value;
                var sprites=data.FindProperty("normalSprites");plates=new Sprite[sprites.arraySize];
                for(int i=0;i<plates.Length;i++)plates[i]=sprites.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
            }
            finally{if(preview)EditorSceneManager.ClosePreviewScene(scene);}
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using(new EditorGUI.DisabledScope(true))EditorGUILayout.ObjectField("Script",MonoScript.FromScriptableObject((RecipeCatalog)target),typeof(MonoScript),false);
            previewLevel=(LevelConfig)EditorGUILayout.ObjectField("Preview Level",previewLevel,typeof(LevelConfig),false);
            EditorGUILayout.LabelField("Preview only • Scale 0 uses global "+globalScale.ToString("0.##"),EditorStyles.miniLabel);
            var entries=serializedObject.FindProperty("foodVisualSettings");
            entries.isExpanded=EditorGUILayout.Foldout(entries.isExpanded,"Food Visual Settings",true);
            if(entries.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(entries.FindPropertyRelative("Array.size"));
                for(int i=0;i<entries.arraySize;i++)
                {
                    var entry=entries.GetArrayElementAtIndex(i);
                    string id=entry.FindPropertyRelative("recipeId").stringValue;
                    entry.isExpanded=EditorGUILayout.Foldout(entry.isExpanded,string.IsNullOrEmpty(id)?"Recipe "+i:id,true);
                    if(!entry.isExpanded)continue;
                    using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        foreach(string field in new[]{"recipeId","foodScale","offsetX","offsetY","rotationZ"})EditorGUILayout.PropertyField(entry.FindPropertyRelative(field));
                        DrawFood(entry);
                    }
                }
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFood(SerializedProperty entry)
        {
            string id=entry.FindPropertyRelative("recipeId").stringValue;
            var config=previewLevel;
            RecipeFoodVisual visual=config?.foodVisuals?.FirstOrDefault(v=>v!=null&&v.recipeId==id&&v.sprite!=null);
            if(visual==null)
                foreach(var level in levels)
                {
                    visual=level.foodVisuals?.FirstOrDefault(v=>v!=null&&v.recipeId==id&&v.sprite!=null);
                    if(visual!=null){config=level;break;}
                }
            var recipe=config?.activeRecipes?.FirstOrDefault(r=>r!=null&&r.id==id);
            var catalogRecipe=SushiRecipeCatalog.All.FirstOrDefault(r=>r.Id==id);
            int grade=(int)(recipe!=null?recipe.grade:catalogRecipe!=null?catalogRecipe.Grade:RecipeGrade.Common);
            Sprite plate=grade<plates.Length?plates[grade]:null;
            Sprite food=visual?.sprite;
            if(food!=null&&id!="salmon_nigiri"&&food.name.Replace(" ","").Replace("_","").ToLowerInvariant().Contains("salmonnigiri"))food=null;
            Rect view=GUILayoutUtility.GetRect(80,210,GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(view,new Color(.16f,.16f,.16f));
            if(Event.current.type==EventType.Repaint)
            {
                GUI.BeginClip(view);
                try
                {
                    float zoom=Mathf.Min((view.width-36)/Mathf.Max(1,plateSize.x),(view.height-36)/Mathf.Max(1,plateSize.y));
                    Vector2 center=view.size*.5f,size=plateSize*zoom;
                    Rect bounds=new Rect(center-size*.5f,size);
                    DrawSprite(bounds,plate);
                    GUI.BeginClip(bounds);
                    try
                    {
                        float scale=entry.FindPropertyRelative("foodScale").floatValue;if(scale<=0)scale=globalScale;
                        Vector2 offset=new Vector2(entry.FindPropertyRelative("offsetX").floatValue,-entry.FindPropertyRelative("offsetY").floatValue)*zoom;
                        Vector2 foodCenter=size*.5f+offset;
                        Matrix4x4 matrix=GUI.matrix;
                        try
                        {
                            GUIUtility.RotateAroundPivot(-entry.FindPropertyRelative("rotationZ").floatValue,foodCenter);
                            DrawSprite(new Rect(foodCenter-size*scale*.5f,size*scale),food);
                        }
                        finally{GUI.matrix=matrix;}
                    }
                    finally{GUI.EndClip();}
                }
                finally{GUI.EndClip();}
            }
            if(food==null)EditorGUILayout.HelpBox("No matching food sprite assigned for this recipe.",MessageType.Info);
            if(plate==null)EditorGUILayout.HelpBox("No plate sprite available for this grade.",MessageType.Info);
        }

        private static void DrawSprite(Rect bounds,Sprite sprite)
        {
            if(sprite==null||sprite.texture==null)return;
            float fit=Mathf.Min(bounds.width/sprite.rect.width,bounds.height/sprite.rect.height);
            Vector2 size=sprite.rect.size*fit;
            Rect rect=new Rect(bounds.center-size*.5f,size),source=sprite.textureRect;
            Rect uv=new Rect(source.x/sprite.texture.width,source.y/sprite.texture.height,source.width/sprite.texture.width,source.height/sprite.texture.height);
            GUI.DrawTextureWithTexCoords(rect,sprite.texture,uv,true);
        }
    }
}
