using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                            var prefabPath = gltfPath.parent.Child(gltfPath.fileNameWithoutExtension + ".prefab");
                            ImportAsset(UnityPath.FromUnityPath(path).fullPath, ext, prefabPath);
                            break;
                        }
                }
            }
        }

        public static void ImportAsset(string src, string ext, UnityPath prefabPath)
        {
            if (!prefabPath.isUnderAssetsFolder)
            {
                Debug.LogWarningFormat("out of asset path: {0}", prefabPath);
                return;
            }
            var context = new GLTFImporter();
            context.Parse(src);

            // Extract textures to assets folder
            ExtranctImages(context, prefabPath);

            ImportDelayed(context, prefabPath);
        }

        static void ImportDelayed(GLTFImporter context, UnityPath prefabPath)
        {
            EditorApplication.delayCall += () =>
                {
                    //
                    // After textures imported(To ensure TextureImporter be accessible).
                    //
                    // try
                    // {

                    var t = Save(context, prefabPath);
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
        private static async Task Save(GLTFImporter context, UnityPath path)
        {
            await context.Load(context.storage, p => Debug.Log(p));
            context.ShowMeshes();
            await SaveAsAsset(context, path);
            context.EditorDestroyRoot();
        }
        public static bool MeshAsSubAsset = false;

        protected static UnityPath GetAssetPath(UnityPath prefabPath, UnityEngine.Object o)
        {
            if (o is Material)
            {
                var materialDir = prefabPath.GetAssetFolder(".Materials");
                var materialPath = materialDir.Child(o.name.EscapeFilePath() + ".asset");
                return materialPath;
            }
            else if (o is Texture2D)
            {
                var textureDir = prefabPath.GetAssetFolder(".Textures");
                var texturePath = textureDir.Child(o.name.EscapeFilePath() + ".asset");
                return texturePath;
            }
            else if (o is Mesh && !MeshAsSubAsset)
            {
                var meshDir = prefabPath.GetAssetFolder(".Meshes");
                var meshPath = meshDir.Child(o.name.EscapeFilePath() + ".asset");
                return meshPath;
            }
            else
            {
                return default(UnityPath);
            }
        }

        public static bool IsOverwrite(UnityEngine.Object o)
        {
            return !(o is Material);
        }

        public static async Task SaveAsAsset(GLTFImporter context, UnityPath prefabPath)
        {


            //var prefabPath = PrefabPath;
            if (prefabPath.isFileExists)
            {
                // clear SubAssets
                foreach (var x in prefabPath.GetSubAssets().Where(x => !(x is GameObject) && !(x is Component)))
                {
                    GameObject.DestroyImmediate(x, true);
                }
            }

            //
            // save sub assets
            //
            var paths = new List<UnityPath>(){
                prefabPath
            };
            foreach (var o in context.ObjectsForSubAsset())
            {
                if (o == null) continue;

                var assetPath = GetAssetPath(prefabPath, o);
                if (!assetPath.isNull)
                {
                    if (assetPath.isFileExists)
                    {
                        if (!IsOverwrite(o))
                        {
                            // 上書きしない
                            Debug.LogWarningFormat("already exists. skip {0}", assetPath);
                            continue;
                        }
                    }
                    assetPath.parent.EnsureFolder();
                    assetPath.CreateAsset(o);
                    paths.Add(assetPath);
                }
                else
                {
                    // save as subasset
                    prefabPath.AddObjectToAsset(o);
                }
            }
            await Task.Yield();
            // Create or upate Main Asset
            // if (prefabPath.isFileExists)
            // {
            //     Debug.LogFormat("replace prefab: {0}", prefabPath);
            //     //var prefab = prefabPath.LoadAsset<GameObject>();
            // }
            // else
            // {
            //     Debug.LogFormat("create prefab: {0}", prefabPath);
            //     PrefabUtility.SaveAsPrefabAsset(context.root, prefabPath.value);//.CreatePrefab(prefabPath.Value, context.root);
            // }
                PrefabUtility.SaveAsPrefabAssetAndConnect(context.root, prefabPath.value, InteractionMode.AutomatedAction);//.ReplacePrefab(context.root, prefab, ReplacePrefabOptions.ReplaceNameBased);
            await Task.Yield();
            AssetDatabase.Refresh();
            // foreach (var x in paths)
            // {
            //     x.ImportAsset();
            // }
            await Task.Yield();
        }

        /// <summary>
        /// Extract images from glb or gltf out of Assets folder.
        /// </summary>
        /// <param name="prefabPath"></param>
        public static void ExtranctImages(GLTFImporter context, UnityPath prefabPath)
        {
            var prefabParentDir = prefabPath.parent;

            // glb buffer
            var folder = prefabPath.GetAssetFolder(".Textures");

            //
            // https://answers.unity.com/questions/647615/how-to-update-import-settings-for-newly-created-as.html
            //
            int created = 0;
            //for (int i = 0; i < GLTF.textures.Count; ++i)
            for (int i = 0; i < context.gltf.images.Count; ++i)
            {
                folder.EnsureFolder();

                //var x = GLTF.textures[i];
                var image = context.gltf.images[i];
                var src = context.storage.GetPath(image.uri);
                if (UnityPath.FromFullpath(src).isUnderAssetsFolder)
                {
                    // asset is exists.
                }
                else
                {
                    string textureName;
                    var byteSegment = context.gltf.GetImageBytes(context.storage, i, out textureName, out var url);

                    // path
                    var dst = folder.Child(textureName + image.GetExt());
                    File.WriteAllBytes(dst.fullPath, byteSegment.ToArray());
                    dst.ImportAsset();

                    // make relative path from PrefabParentDir
                    image.uri = dst.value.Substring(prefabParentDir.value.Length + 1);
                    ++created;
                }
            }

            if (created > 0)
            {
                AssetDatabase.Refresh();
            }

            context.CreateTextureItems(prefabParentDir);
        }
    }
}
