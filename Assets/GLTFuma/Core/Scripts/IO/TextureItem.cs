using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UMa.GLTF
{
    public class TextureItem
    {
        private int _textureIndex;
        public Texture2D texture
        {
            get
            {
                return _textureLoader.texture;
            }
        }
        public void Load(GLTFRoot gltf, IStorage storage, Action<Texture2D> complete)
        {
            //Debug.Log("TextureItem Load ");
            //m_textureLoader.Load(complete);

            var load = StartLoad(gltf, storage, complete);
        }
        private async Task<bool> StartLoad(GLTFRoot gltf, IStorage storage, Action<Texture2D> complete)
        {
            //Debug.Log("Start Load "+m_textureIndex);
            var imageIndex = gltf.GetImageIndexFromTextureIndex(_textureIndex);
            //Debug.Log("image index "+imageIndex);
            gltf.GetImageBytes(storage, imageIndex, out var name, out var url);
            //Debug.Log("image url ... "+url);
            if (string.IsNullOrEmpty(url)) return false;
            var texture = await storage.LoadTexture(url, p => {});
            texture.name = name;
            complete.Invoke(texture);
            return true;
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
                        textureAssetPath.MarkTextureAssetAsNormalMap();
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

        public bool isAsset { get; private set; }

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
            _textureIndex = index;
            // #if UNIGLTF_USE_WEBREQUEST_TEXTURELOADER
            _textureLoader = new UnityWebRequestTextureLoader(_textureIndex);
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
            _textureIndex = index;
            isAsset = true;
            _textureLoader = new AssetTextureLoader(assetPath, textureName);
        }
#endif

        #region Process
        private ITextureLoader _textureLoader;

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
            _textureLoader.ProcessOnAnyThread(gltf, storage);
        }

        public IEnumerator ProcessOnMainThreadCoroutine(GLTFRoot gltf)
        {
            using (_textureLoader)
            {
                var textureType = gltf.GetTextureType(_textureIndex);
                var colorSpace = textureType.GetColorSpace();
                var isLinear = colorSpace == RenderTextureReadWrite.Linear;
                yield return _textureLoader.ProcessOnMainThread(isLinear);
                texture.SetSampler(gltf.GetSamplerFromTextureIndex(_textureIndex));
            }
        }
        #endregion


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
    }
}
