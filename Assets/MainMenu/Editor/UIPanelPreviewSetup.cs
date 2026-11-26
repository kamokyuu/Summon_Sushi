using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CookingCards.Menu.Editor
{
    public static class UIPanelPreviewSetup
    {
        [MenuItem("Tools/Cooking Cards/Enable All UI Panel Previews")]
        public static void Install()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode only.");
            foreach(string path in new[]{"Assets/Scenes/MainScene.unity","Assets/Scenes/MainMenu.unity","Assets/Scenes/LevelMap.unity","Assets/Scenes/StudioSplash.unity"})
            {
                var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try{InstallScene(scene);UIPanelPreviewEditor.Apply();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
        }
        private static T[] All<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        private static void InstallScene(Scene scene)
        {
            UIPanelPreviewEditor.Restore(All<UIPanelPreview>(scene));
            var belt=All<ConveyorController>(scene).FirstOrDefault();
            if(belt!=null)
            {
                var controller=belt.GetComponent<ComboShopBoss>();bool temporary=controller==null;
                if(temporary)controller=belt.gameObject.AddComponent<ComboShopBoss>();
                try
                {
                    typeof(ComboShopBoss).GetMethod("EnsureUi",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
                    typeof(ComboShopBoss).GetMethod("EnsureBossUi",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
                    var canvas=belt.GetComponentInParent<Canvas>().transform;
                    foreach(string name in new[]{"PowerUpShop","AYCEAnnouncement"})foreach(var button in canvas.Find(name).GetComponentsInChildren<Button>(true))button.onClick.RemoveAllListeners();
                    Sample(canvas.Find("ComboBossStatus"),"COMBO 3x");Sample(canvas.Find("BossEffectsStatus"),"SHIELD 1   AYCE — Common × 2");
                    Sample(canvas.Find("PowerUpShop/ShopTitle"),"POWER-UP SHOP — 10 seconds");
                    Sample(canvas.Find("AYCEAnnouncement/Orders"),"AYCE CUSTOMER\n\nCommon × 2\nUncommon × 1\n\nAny matching recipe. 60 seconds after Ready.");
                    var resources=canvas.Find("PowerUpShop/ResourceChoices");
                    var make=typeof(ComboShopBoss).GetMethod("Button",BindingFlags.Static|BindingFlags.NonPublic);
                    int slot=resources.childCount;
                    for(int i=0;i<IngredientRules.Count;i++)
                    {
                        string name=((IngredientKind)i).ToString();if(resources.Find(name)!=null)continue;
                        var button=(Button)make.Invoke(null,new object[]{resources,name,new Vector2((slot%4-1.5f)*200,30-slot/4*75),new Vector2(190,65),new UnityAction(()=>{})});
                        button.GetComponentInChildren<TMP_Text>().text=IngredientRules.DisplayName((IngredientKind)i);slot++;
                    }
                    string[] samples={"Lucky Draw\n2 selected ingredients — ¥300","Knife Refill\nUp to 2 charges — ¥150","Extra Hand\n+2 slots — ¥250"};
                    for(int i=0;i<3;i++)Sample(canvas.Find("PowerUpShop/Choice"+i+"/Description"),samples[i]);
                    foreach(string name in new[]{"ComboBossStatus","BossEffectsStatus","PowerUpShop","AYCEAnnouncement"})canvas.Find(name).gameObject.SetActive(false);
                    resources.gameObject.SetActive(false);
                    var resourcePreview=Register(resources.gameObject);
                    resourcePreview.hideWhilePreviewing=Enumerable.Range(0,3).Select(i=>canvas.Find("PowerUpShop/Choice"+i).gameObject).ToArray();
                    Capture(resourcePreview,true);
                }
                finally{if(temporary)UnityEngine.Object.DestroyImmediate(controller);}
            }
            var result=All<RectTransform>(scene).FirstOrDefault(r=>r.name=="GameResults");
            if(result!=null)
            {
                var receipt=result.GetComponentInChildren<ShiftCompletionReceipt>(true);
                if(receipt==null)
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ShiftCompletionReceipt.prefab");
                    if(prefab!=null)receipt=((GameObject)PrefabUtility.InstantiatePrefab(prefab,result)).GetComponent<ShiftCompletionReceipt>();
                }
                if(receipt!=null)
                {
                    receipt.gameObject.SetActive(false);var preview=Register(receipt.gameObject);
                    var legacy=result.Find("ResultsNote");preview.hideWhilePreviewing=legacy!=null?new[]{legacy.gameObject}:Array.Empty<GameObject>();Capture(preview,true);
                    var data=new SerializedObject(receipt);Sample(data.FindProperty("title").objectReferenceValue as TMP_Text,"SHIFT COMPLETE");Sample(data.FindProperty("levelName").objectReferenceValue as TMP_Text,"DAY 01 — Grand Opening");Sample(data.FindProperty("summary").objectReferenceValue as TMP_Text,"Ebi Nigiri (3x)\nTamago Nigiri (2x)\n\nServed: 5   Broken plates: 0\nNet: ¥600");
                }
                if(result.Find("ResultsNavigation")==null)
                {
                    var root=new GameObject("ResultsNavigation",typeof(RectTransform));root.transform.SetParent(result,false);var rect=(RectTransform)root.transform;rect.anchoredPosition=new Vector2(0,-255);rect.sizeDelta=new Vector2(1000,60);
                    var make=typeof(ResultsNavigation).GetMethod("Make",BindingFlags.Static|BindingFlags.NonPublic);
                    make.Invoke(null,new object[]{root.transform,"MAIN MENU",-180f,"MainMenu"});make.Invoke(null,new object[]{root.transform,"REPLAY",180f,"MainScene"});
                }
            }
            foreach(var rect in All<RectTransform>(scene))
            {
                if(rect.GetComponentInParent<RecipeJournal>()!=null)continue;
                if(rect.GetComponentInParent<Canvas>()==null||rect.GetComponentInParent<RecipeJournal>()!=null)continue;
                bool panel=rect.name.EndsWith("Panel")||rect.name=="RestaurantHub"||rect.name=="TutorialComplete"||rect.name=="GameResults"||rect.name=="ComboBossStatus"||rect.name=="BossEffectsStatus"||rect.name=="PowerUpShop"||rect.name=="AYCEAnnouncement"||rect.GetComponent<TutorialDialogueOverlay>()!=null||rect.GetComponent<ShiftCompletionReceipt>()!=null||(!rect.gameObject.activeSelf&&rect.childCount>0&&rect.GetComponent<Button>()==null&&rect.GetComponent<TMP_Text>()==null);
                if(panel)Register(rect.gameObject);
            }
        }
        private static void Sample(Transform target,string text){if(target!=null)Sample(target.GetComponent<TMP_Text>(),text);}
        private static void Sample(TMP_Text target,string text){if(target!=null&&(string.IsNullOrWhiteSpace(target.text)||target.text=="New Text"||target.text=="New Text (TMP)")){target.text=text;EditorUtility.SetDirty(target);}}
        private static UIPanelPreview Register(GameObject target)
        {
            var preview=target.GetComponent<UIPanelPreview>()??target.AddComponent<UIPanelPreview>();
            if(preview.normalVisibility.Length==0)Capture(preview,false);return preview;
        }
        private static void Capture(UIPanelPreview preview,bool refresh)
        {
            var states=new List<UIPanelPreview.Visibility>();
            for(var node=preview.transform;node!=null;node=node.parent)Add(node.gameObject);
            foreach(var hidden in preview.hideWhilePreviewing)if(hidden!=null)Add(hidden);
            preview.normalVisibility=states.ToArray();EditorUtility.SetDirty(preview);
            void Add(GameObject target)
            {
                if(states.Any(s=>s.target==target))return;var group=target.GetComponent<CanvasGroup>();
                states.Add(new UIPanelPreview.Visibility{target=target,active=target.activeSelf,group=group,alpha=target.GetComponent<TutorialDialogueOverlay>()!=null?0:group!=null?group.alpha:1});
            }
        }
    }
}
