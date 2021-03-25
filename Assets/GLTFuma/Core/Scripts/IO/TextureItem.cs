using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Threading.Tasks;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UMa.GLTF
{
    public class TextureItem
    {
        private int m_textureIndex;
        public Texture2D texture
        {
            get
            {
                return m_textureLoader.Texture;
            }
        }
        public void Load(GLTFRoot gltf,IStorage storage, Action<Texture2D> complete){
            Debug.Log("TextureItem Load ");
            //m_textureLoader.Load(complete);
           
            var load = StartLoad(gltf,storage,complete);
        }
        private async Task<bool> StartLoad(GLTFRoot gltf,IStorage storage,Action<Texture2D> complete)
        {
            Debug.Log("Start Load "+m_textureIndex);
             var imageIndex = gltf.GetImageIndexFromTextureIndex(m_textureIndex);
             Debug.Log("image index "+imageIndex);
            gltf.GetImageBytes(storage, imageIndex, out var name, out var url);
            Debug.Log("image url ... "+url);
            if (string.IsNullOrEmpty(url)) return false;
            await Task.Yield();
            Debug.LogFormat("UnityWebRequest: {0}", url);
            var m_uwr = UnityWebRequestTexture.GetTexture(url);
            Debug.Log("load texture ... "+url);
            var ao = m_uwr.SendWebRequest();
            // wait for request
            while (!m_uwr.isDone)
            {
                await Task.Yield();
            }

            if (string.IsNullOrEmpty(m_uwr.error))
            {
                // Get downloaded asset bundle
                var texture = ((DownloadHandlerTexture)m_uwr.downloadHandler).texture;
                texture.name = name;
                complete.Invoke(texture);
                return true;
            }
            else
            {
                Debug.Log(m_uwr.error);
                return false;
            }
        }

        #region Texture converter
        private Dictionary<string, Texture2D> _converts = new Dictionary<string, Texture2D>();

        public Texture2D ConvertTexture(string prop)
        {
            var convertedTexture = _converts.FirstOrDefault(x => x.Key == prop);
            if (convertedTexture.Value != null)
                return convertedTexture.Value;

            if (prop == "_BumpMap")
            {
                if (Application.isPlaying)
                {
                    var converted = new NormalConverter().GetImportTexture(texture);
                    _converts.Add(prop, converted);
                    return converted;
                }
                else
                {
#if UNITY_EDITOR
                    var textureAssetPath = AssetDatabase.GetAssetPath(texture);
                    if (!string.IsNullOrEmpty(textureAssetPath))
                    {
                        TextureIO.MarkTextureAssetAsNormalMap(textureAssetPath);
                    }
                    else
                    {
                        Debug.LogWarningFormat("no asset for {0}", texture);
                    }
#endif
                    return texture;
                }
            }

            if (prop == "_MetallicGlossMap")
            {
                var converted = new MetallicRoughnessConverter().GetImportTexture(texture);
                _converts.Add(prop, converted);
                return converted;
            }

            if (prop == "_OcclusionMap")
            {
                var converted = new OcclusionConverter().GetImportTexture(texture);
                _converts.Add(prop, converted);
                return converted;
            }

            return null;
        }
        #endregion

        public bool isAsset{get;private set;}

        public IEnumerable<Texture2D> GetTexturesForSaveAssets()
        {
            if (!isAsset)
            {
                yield return texture;
            }
            if (_converts.Any())
            {
                foreach (var texture in _converts)
                {
                    yield return texture.Value;
                }
            }
        }

        /// <summary>
        /// Texture from buffer
        /// </summary>
        /// <param name="index"></param>
        public TextureItem(int index)
        {
            m_textureIndex = index;
// #if UNIGLTF_USE_WEBREQUEST_TEXTURELOADER
            m_textureLoader = new UnityWebRequestTextureLoader(m_textureIndex);
// #else
//             m_textureLoader = new TextureLoader(m_textureIndex);
// #endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Texture from asset
        /// </summary>
        /// <param name="index"></param>
        /// <param name="assetPath"></param>
        /// <param name="textureName"></param>
        public TextureItem(int index, UnityPath assetPath, string textureName)
        {
            m_textureIndex = index;
            isAsset = true;
            m_textureLoader = new AssetTextureLoader(assetPath, textureName);
        }
#endif

        #region Process
        private ITextureLoader m_textureLoader;

        // public void Process(glTF gltf, IStorage storage)
        // {
        //     ProcessOnAnyThread(gltf, storage);
        //     ProcessOnMainThreadCoroutine(gltf).CoroutinetoEnd();
        // }

        public IEnumerator ProcessCoroutine(GLTFRoot gltf, IStorage storage)
        {
            ProcessOnAnyThread(gltf, storage);
            yield return ProcessOnMainThreadCoroutine(gltf);
        }

        public void ProcessOnAnyThread(GLTFRoot gltf, IStorage storage)
        {
            m_textureLoader.ProcessOnAnyThread(gltf, storage);
        }

        public IEnumerator ProcessOnMainThreadCoroutine(GLTFRoot gltf)
        {
            using (m_textureLoader)
            {
                var textureType = TextureIO.GetglTFTextureType(gltf, m_textureIndex);
                var colorSpace = TextureIO.GetColorSpace(textureType);
                var isLinear = colorSpace == RenderTextureReadWrite.Linear;
                yield return m_textureLoader.ProcessOnMainThread(isLinear);
                TextureSamplerUtil.SetSampler(texture, gltf.GetSamplerFromTextureIndex(m_textureIndex));
            }
        }
        #endregion

        struct ColorSpaceScope : IDisposable
        {
            bool m_sRGBWrite;

            public ColorSpaceScope(RenderTextureReadWrite colorSpace)
            {
                m_sRGBWrite = GL.sRGBWrite;
                switch (colorSpace)
                {
                    case RenderTextureReadWrite.Linear:
                        GL.sRGBWrite = false;
                        break;

                    case RenderTextureReadWrite.sRGB:
                    default:
                        GL.sRGBWrite = true;
                        break;
                }
            }
            public ColorSpaceScope(bool sRGBWrite)
            {
                m_sRGBWrite = GL.sRGBWrite;
                GL.sRGBWrite = sRGBWrite;
            }

            public void Dispose()
            {
                GL.sRGBWrite = m_sRGBWrite;
            }
        }

#if UNITY_EDITOR && VRM_DEVELOP
        [MenuItem("Assets/CopySRGBWrite", true)]
        static bool CopySRGBWriteIsEnable()
        {
            return Selection.activeObject is Texture;
        }

        [MenuItem("Assets/CopySRGBWrite")]
        static void CopySRGBWrite()
        {
            CopySRGBWrite(true);
        }

        [MenuItem("Assets/CopyNotSRGBWrite", true)]
        static bool CopyNotSRGBWriteIsEnable()
        {
            return Selection.activeObject is Texture;
        }

        [MenuItem("Assets/CopyNotSRGBWrite")]
        static void CopyNotSRGBWrite()
        {
            CopySRGBWrite(false);
        }

        static string AddPath(string path, string add)
        {
            return string.Format("{0}/{1}{2}{3}",
            Path.GetDirectoryName(path),
            Path.GetFileNameWithoutExtension(path),
            add,
            Path.GetExtension(path));
        }

        static void CopySRGBWrite(bool isSRGB)
        {
            var src = Selection.activeObject as Texture;
            var texturePath = UnityPath.FromAsset(src);

            var path = EditorUtility.SaveFilePanel("save prefab", "Assets",
            Path.GetFileNameWithoutExtension(AddPath(texturePath.FullPath, ".sRGB")), "prefab");
            var assetPath = UnityPath.FromFullpath(path);
            if (!assetPath.IsUnderAssetsFolder)
            {
                return;
            }
            Debug.LogFormat("[CopySRGBWrite] {0} => {1}", texturePath, assetPath);

            var renderTexture = new RenderTexture(src.width, src.height, 0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            using (var scope = new ColorSpaceScope(isSRGB))
            {
                Graphics.Blit(src, renderTexture);
            }

            var dst = new Texture2D(src.width, src.height, TextureFormat.ARGB32, false,
                RenderTextureReadWrite.sRGB == RenderTextureReadWrite.Linear);
            dst.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            dst.Apply();

            RenderTexture.active = null;

            assetPath.CreateAsset(dst);

            GameObject.DestroyImmediate(renderTexture);
        }
#endif

        public static Texture2D CopyTexture(Texture src, RenderTextureReadWrite colorSpace, Material material)
        {
            Texture2D dst = null;

            var renderTexture = new RenderTexture(src.width, src.height, 0, RenderTextureFormat.ARGB32, colorSpace);

            using (var scope = new ColorSpaceScope(colorSpace))
            {
                if (material != null)
                {
                    Graphics.Blit(src, renderTexture, material);
                }
                else
                {
                    Graphics.Blit(src, renderTexture);
                }
            }

            dst = new Texture2D(src.width, src.height, TextureFormat.ARGB32, false, colorSpace == RenderTextureReadWrite.Linear);
            dst.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            dst.name = src.name;
            dst.Apply();

            RenderTexture.active = null;
            if (Application.isEditor)
            {
                GameObject.DestroyImmediate(renderTexture);
            }
            else
            {
                GameObject.Destroy(renderTexture);
            }
            return dst;
        }
    }
}
