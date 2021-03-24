﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;


namespace UMa.GLTF
{
    public class GLTFAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                var ext = Path.GetExtension(path).ToLower();
                switch (ext)
                {
                    case ".gltf":
                    case ".glb":
                        {
                            var gltfPath = UnityPath.FromUnityPath(path);
                            var prefabPath = gltfPath.Parent.Child(gltfPath.FileNameWithoutExtension + ".prefab");
                            ImportAsset(UnityPath.FromUnityPath(path).FullPath, ext, prefabPath);
                            break;
                        }
                }
            }
        }

        public static void ImportAsset(string src, string ext, UnityPath prefabPath)
        {
            if (!prefabPath.IsUnderAssetsFolder)
            {
                Debug.LogWarningFormat("out of asset path: {0}", prefabPath);
                return;
            }
            var context = new GLTFImporter();
            context.Parse(src);

            // Extract textures to assets folder
            context.ExtranctImages(prefabPath);

            ImportDelayed(src, prefabPath, context);
        }

        static void ImportDelayed(string src, UnityPath prefabPath, GLTFImporter context)
        {
            EditorApplication.delayCall += () =>
                {
                    //
                    // After textures imported(To ensure TextureImporter be accessible).
                    //
                    // try
                    // {

                            var n = context.Load(context.storage,p=>Debug.Log(p));
                            
                            //context.SaveAsAsset(prefabPath);
                        //context.EditorDestroyRoot();
                    // }
                    // catch (UniGLTFNotSupportedException ex)
                    // {
                    //     Debug.LogWarningFormat($"{src}: {ex.Message}");
                    //     context.EditorDestroyRootAndAssets();
                    // }
                    // catch (Exception ex)
                    // {
                    //     Debug.LogErrorFormat("import error: {0}", src);
                    //     Debug.LogErrorFormat("{0}", ex);
                    //     context.EditorDestroyRootAndAssets();
                    // }
                };
        }
    }
}