using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CookingCards.Menu.Editor
{
    [CustomEditor(typeof(UIPanelPreview))]
    public sealed class UIPanelPreviewEditor : UnityEditor.Editor
    {
        [InitializeOnLoadMethod] private static void Register()
        {
            UIPanelPreview.Changed-=Apply;UIPanelPreview.Changed+=Apply;
            EditorApplication.playModeStateChanged-=Mode;EditorApplication.playModeStateChanged+=Mode;
            Undo.undoRedoPerformed-=Apply;Undo.undoRedoPerformed+=Apply;
            EditorSceneManager.sceneOpened-=Opened;EditorSceneManager.sceneOpened+=Opened;
            EditorApplication.delayCall+=Apply;
        }
        private static UIPanelPreview[] Panels()=>Object.FindObjectsByType<UIPanelPreview>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.gameObject.scene.IsValid()).ToArray();
        private static void Opened(Scene scene,OpenSceneMode mode){EditorApplication.delayCall+=Apply;}
        private static void Mode(PlayModeStateChange mode)
        {
            if(mode==PlayModeStateChange.ExitingEditMode)Restore(Panels());
            if(mode==PlayModeStateChange.EnteredEditMode)Apply();
        }
        public static void Restore(IEnumerable<UIPanelPreview> panels)
        {
            foreach(var panel in panels)foreach(var state in panel.normalVisibility)
            {if(state.target!=null)state.target.SetActive(state.active);if(state.group!=null)state.group.alpha=state.alpha;}
        }
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||BuildPipeline.isBuildingPlayer)return;
            var panels=Panels();Restore(panels);
            foreach(var panel in panels.Where(p=>p.previewInEditor))
            {
                foreach(var state in panel.normalVisibility){if(state.target!=null)state.target.SetActive(true);if(state.group!=null)state.group.alpha=1;}
                foreach(var hidden in panel.hideWhilePreviewing)if(hidden!=null&&!panels.Any(p=>p.gameObject==hidden&&p.previewInEditor))hidden.SetActive(false);
            }
            SceneView.RepaintAll();
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("previewInEditor"),new GUIContent("Preview in Editor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hideWhilePreviewing"));
            EditorGUILayout.HelpBox("Edit the real child objects in Scene view. Save the scene. Preview visibility is restored before Play and stripped from builds; no gameplay actions are called.",MessageType.Info);
            bool changed=EditorGUI.EndChangeCheck();serializedObject.ApplyModifiedProperties();if(changed)Apply();
        }
    }
    public sealed class UIPanelPreviewBuildCleanup : IProcessSceneWithReport
    {
        public int callbackOrder=>10000;
        public void OnProcessScene(Scene scene,BuildReport report)
        {
            if(report==null)return;
            var panels=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UIPanelPreview>(true)).ToArray();
            UIPanelPreviewEditor.Restore(panels);foreach(var panel in panels)Object.DestroyImmediate(panel);
        }
    }
}
