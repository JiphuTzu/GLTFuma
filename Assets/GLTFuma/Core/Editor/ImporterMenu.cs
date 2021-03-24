using System.IO;
using UnityEditor;
using UnityEngine;


namespace UMa.GLTF
{
    public static class ImporterMenu
    {
        [MenuItem("GLTFuma/Import", priority = 1)]
        public static void ImportMenu()
        {
            var path = EditorUtility.OpenFilePanel("open gltf", "", "gltf,glb,zip");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (Application.isPlaying)
            {
                //
                // load into scene
                //
                var context = new GLTFImporter();
                //context.Load(path);
                context.ShowMeshes();
                Selection.activeGameObject = context.root;
            }
            else
            {
                //
                // save as asset
                //
                if (path.StartsWithUnityAssetPath())
                {
                    Debug.LogWarningFormat("disallow import from folder under the Assets");
                    return;
                }

                var assetPath = EditorUtility.SaveFilePanel("save prefab", "Assets", Path.GetFileNameWithoutExtension(path), "prefab");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                // import as asset
                GLTFAssetPostprocessor.ImportAsset(path, Path.GetExtension(path).ToLower(), UnityPath.FromFullpath(assetPath));
            }
        }
    }
}
