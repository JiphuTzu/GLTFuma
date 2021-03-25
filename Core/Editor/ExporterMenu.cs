using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
//============================================================
//支持中文，文件使用UTF-8编码
//@author	JiphuTzu
//@create	20210324
//@company	UMa
//
//@description:
//============================================================
namespace UMa.GLTF
{
    public static class ExporterMenu
    {

        [MenuItem("GLTFuma/ExportGLTF", true, 1)]
        private static bool ExportGLTFValidate()
        {
            return Selection.activeObject != null && Selection.activeObject is GameObject;
        }
        [MenuItem("GLTFuma/ExportGLTF", false, 1)]
        private static void ExportGLTFFromMenu()
        {
            var go = Selection.activeObject as GameObject;
            var path = EditorUtility.SaveFolderPanel("Save GLTF", "", go.name);
            if (string.IsNullOrEmpty(path)) return;

            Debug.Log("export ... " + path);
            if (!path.EndsWith(go.name))
                path += Path.AltDirectorySeparatorChar + go.name;
            Debug.Log(path + "exist = " + Directory.Exists(path));
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var gltf = new GLTFRoot();
            var exporter = new GLTFExporter(gltf);
            exporter.Prepare(go);
            exporter.Export();
            EditorApplication.delayCall += () =>
            {
                var res = SaveFiles(path, go.name, exporter);
            };
            // return;
            // var bytes = gltf.ToGlbBytes();
            // File.WriteAllBytes(path, bytes);

            // if (path.StartsWithUnityAssetPath())
            // {
            //     AssetDatabase.ImportAsset(path.ToUnityRelativePath());
            //     AssetDatabase.Refresh();
            // }
        }
        private static async Task SaveFiles(string path, string name, GLTFExporter exporter)
        {
            var buffers = exporter.gltf.buffers;
            if (buffers.Any())
            {
                for (int i = 0; i < buffers.Count; i++)
                {
                    buffers[i].uri = i == 0 ? $"{name}.bin" : $"{name}_{i}bin";
                }
            }
            File.WriteAllText($"{path}{Path.AltDirectorySeparatorChar}{name}.gltf", exporter.gltf.ToJson());
            await Task.Yield();
            File.WriteAllBytes($"{path}{Path.AltDirectorySeparatorChar}{name}.bin", exporter.gltf.ToBinary());
            await Task.Yield();
            Debug.Log("===>" + exporter.textureManager.unityTextures.Count);
            for (int i = 0; i < exporter.textureManager.unityTextures.Count; i++)
            {
                var ut = exporter.textureManager.unityTextures[i];
                var tex = exporter.textureManager.GetExportTexture(i);
                var bwm = TextureIO.GetBytesWithMime(tex, ut.TextureType);
                var p = $"{path}{Path.AltDirectorySeparatorChar}{tex.name.ToLower()}.png";
                Debug.Log("save png " + i + " -- " + p);
                File.WriteAllBytes(p, bwm.bytes);
                await Task.Yield();
            }
            exporter.Dispose();
        }
        [MenuItem("GLTFuma/ExportGLB", true, 1)]
        private static bool ExportValidate()
        {
            return Selection.activeObject != null && Selection.activeObject is GameObject;
        }

        [MenuItem("GLTFuma/ExportGLB", false, 1)]
        private static void ExportFromMenu()
        {
            var go = Selection.activeObject as GameObject;
            var path = EditorUtility.SaveFilePanel("Save glb", "", go.name + ".glb", "glb");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var gltf = new GLTFRoot();
            using (var exporter = new GLTFExporter(gltf))
            {
                exporter.Prepare(go);
                exporter.Export();
            }
            var bytes = gltf.ToGlbBytes();
            File.WriteAllBytes(path, bytes);

            if (path.StartsWithUnityAssetPath())
            {
                AssetDatabase.ImportAsset(path.ToUnityRelativePath());
                AssetDatabase.Refresh();
            }
        }
    }
}