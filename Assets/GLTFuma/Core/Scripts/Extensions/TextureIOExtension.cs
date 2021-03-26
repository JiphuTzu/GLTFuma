using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UMa.GLTF
{
    public struct TextureExportItem
    {
        public Texture Texture;
        public GLTFTextureType TextureType;

        public TextureExportItem(Texture texture, GLTFTextureType textureType)
        {
            Texture = texture;
            TextureType = textureType;
        }
    }
    public static class TextureIOExtension
    {
        public static RenderTextureReadWrite GetColorSpace(this GLTFTextureType textureType)
        {
            switch (textureType)
            {
                case GLTFTextureType.Metallic:
                case GLTFTextureType.Normal:
                case GLTFTextureType.Occlusion:
                    return RenderTextureReadWrite.Linear;
                case GLTFTextureType.BaseColor:
                case GLTFTextureType.Emissive:
                    return RenderTextureReadWrite.sRGB;
                default:
                    return RenderTextureReadWrite.sRGB;
            }
        }

        public static GLTFTextureType GetglTFTextureType(string shaderName, string propName)
        {
            switch (propName)
            {
                case "_Color":
                    return GLTFTextureType.BaseColor;
                case "_MetallicGlossMap":
                    return GLTFTextureType.Metallic;
                case "_BumpMap":
                    return GLTFTextureType.Normal;
                case "_OcclusionMap":
                    return GLTFTextureType.Occlusion;
                case "_EmissionMap":
                    return GLTFTextureType.Emissive;
                default:
                    return GLTFTextureType.Unknown;
            }
        }

        public static GLTFTextureType GetTextureType(this GLTFRoot gltf, int textureIndex)
        {
            foreach (var material in gltf.materials)
            {
                var textureInfo = material.GetTextures().FirstOrDefault(x => (x != null) && x.index == textureIndex);
                if (textureInfo != null)
                {
                    return textureInfo.TextreType;
                }
            }
            return GLTFTextureType.Unknown;
        }

#if UNITY_EDITOR
        public static void MarkTextureAssetAsNormalMap(this string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (null == textureImporter)
            {
                return;
            }

            //Debug.LogFormat("[MarkTextureAssetAsNormalMap] {0}", assetPath);
            textureImporter.textureType = TextureImporterType.NormalMap;
            textureImporter.SaveAndReimport();
        }
#endif



        public static IEnumerable<TextureExportItem> GetTextures(this Material material)
        {
            var props = ShaderPropExporter.PreShaderPropExporter.GetPropsForSupportedShader(material.shader.name);
            if (props == null)
            {
                yield return new TextureExportItem(material.mainTexture, GLTFTextureType.BaseColor);
            }

            foreach (var prop in props.Properties)
            {

                if (prop.ShaderPropertyType == ShaderPropExporter.ShaderPropertyType.TexEnv)
                {
                    yield return new TextureExportItem(material.GetTexture(prop.Key), GetglTFTextureType(material.shader.name, prop.Key));
                }
            }
        }


        public struct BytesWithMime
        {
            public byte[] bytes;
            public string mime;
        }

        public static BytesWithMime GetBytesWithMime(this Texture texture, GLTFTextureType textureType)
        {
#if UNITY_EDITOR
            var path = UnityPath.FromAsset(texture);
            if (path.isUnderAssetsFolder)
            {
                if (path.extension == ".png")
                {
                    return new BytesWithMime
                    {
                        bytes = System.IO.File.ReadAllBytes(path.fullPath),
                        mime = "image/png"
                    };
                }
            }
#endif

            return new BytesWithMime
            {
                bytes = texture.CopyTexture(textureType.GetColorSpace(), null).EncodeToPNG(),
                mime = "image/png"
            };
        }

        public static int ExportTexture(this GLTFRoot gltf, int bufferIndex, Texture texture, GLTFTextureType textureType)
        {
            //var bytesWithMime = GetBytesWithMime(texture, textureType); ;

            // add view
            //var view = gltf.buffers[bufferIndex].Append(bytesWithMime.bytes, GLTFBufferTarget.NONE);
            //var viewIndex = gltf.AddBufferView(view);

            // add image
            var imageIndex = gltf.images.Count;
            var image = new GLTFImage
            {
                name = texture.name,
                mimeType = "image/png"//bytesWithMime.mime,
            };
            //bufferView = viewIndex,
            image.uri = texture.name.ToLower() + image.GetExt().ToLower();
            gltf.images.Add(image);

            // add sampler
            var samplerIndex = gltf.samplers.Count;
            var sampler = texture.ToTextureSampler();
            gltf.samplers.Add(sampler);

            // add texture
            gltf.textures.Add(new GLTFTexture
            {
                sampler = samplerIndex,
                source = imageIndex,
            });

            return imageIndex;
        }
        public static Texture2D CopyTexture(this Texture src, RenderTextureReadWrite colorSpace, Material material)
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
    }
}
