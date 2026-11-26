using UnityEditor;
using UnityEngine;

namespace CookingCards.Editor
{
    [CustomEditor(typeof(RecipeJournal))]
    public sealed class RecipeJournalEditor : UnityEditor.Editor
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.playModeStateChanged-=PlayModeChanged;
            EditorApplication.playModeStateChanged+=PlayModeChanged;
        }
        private static void PlayModeChanged(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.ExitingEditMode&&state!=PlayModeStateChange.EnteredEditMode)return;
            foreach(var journal in Object.FindObjectsByType<RecipeJournal>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(state==PlayModeStateChange.ExitingEditMode)journal.EditorSetPreviewVisibility(false);
                else journal.EditorApplyPreview(false);
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Scene Preview",EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("previewInEditor"),new GUIContent("Preview in Editor"));
            using(new EditorGUI.DisabledScope(!serializedObject.FindProperty("previewInEditor").boolValue||EditorApplication.isPlaying))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("previewLevel"),new GUIContent("Preview Level (optional)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("previewCategory"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("previewRecipeIndex"));
            }
            bool previewChanged=EditorGUI.EndChangeCheck();
            EditorGUILayout.HelpBox("Uses the real Scene UI. Edit its child objects directly. Category changes update only recipe text; layout and artwork remain yours.",MessageType.Info);
            DrawPropertiesExcluding(serializedObject,"previewInEditor","previewLevel","previewCategory","previewRecipeIndex");
            serializedObject.ApplyModifiedProperties();
            if(previewChanged&&!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var journal=(RecipeJournal)target;
                var panel=serializedObject.FindProperty("binderPanel").objectReferenceValue;
                if(panel!=null)Undo.RecordObject(panel,"Toggle recipe preview");
                journal.EditorApplyPreview(true);
                if(panel!=null){PrefabUtility.RecordPrefabInstancePropertyModifications(panel);EditorUtility.SetDirty(panel);}
                if(journal.gameObject.scene.IsValid())UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(journal.gameObject.scene);
            }
        }
    }
}
