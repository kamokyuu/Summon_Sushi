using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CookingCards.Editor
{
    public static class NpcAndMusicSetup
    {
        [MenuItem("Tools/Cooking Cards/Set Up Final NPCs And Music")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
            var customers=UnityEngine.Object.FindFirstObjectByType<CustomerSystem>();
            if(customers==null)throw new InvalidOperationException("Open MainScene first.");
            string[] names={"npc ungu","npc pink","npc ijo"};
            var state=new SerializedObject(customers);var portraits=state.FindProperty("characterSprites");portraits.arraySize=names.Length;
            for(int i=0;i<names.Length;i++)
            {
                string path="Assets/Asset/NPC/"+names[i]+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)throw new InvalidOperationException("NPC missing: "+path);
                importer.GetSourceTextureWidthAndHeight(out int w,out int h);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.maxTextureSize=Mathf.Max(2048,Mathf.NextPowerOfTwo(Mathf.Max(w,h)));importer.npotScale=TextureImporterNPOTScale.None;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.crunchedCompression=false;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.wrapMode=TextureWrapMode.Clamp;
                importer.SaveAndReimport();
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(sprite==null)throw new InvalidOperationException("NPC sprite import failed");
                portraits.GetArrayElementAtIndex(i).objectReferenceValue=sprite;
            }
            state.ApplyModifiedProperties();
            const string audioPath="Assets/Asset/sfx/andriih-cooking-cooking-music-630228.mp3";
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);if(clip==null)throw new InvalidOperationException("Cooking BGM missing");
            var go=new GameObject("GameMusic",typeof(AudioSource),typeof(GameMusic));
            var source=go.GetComponent<AudioSource>();source.clip=clip;source.loop=true;source.playOnAwake=true;source.spatialBlend=0;source.volume=.35f;source.priority=64;source.dopplerLevel=0;
            try{PrefabUtility.SaveAsPrefabAsset(go,"Assets/Resources/GameMusic.prefab");}finally{UnityEngine.Object.DestroyImmediate(go);}
            EditorSceneManager.MarkSceneDirty(customers.gameObject.scene);EditorSceneManager.SaveScene(customers.gameObject.scene);AssetDatabase.SaveAssets();
        }
    }
}
