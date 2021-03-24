using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UMa.GLTF
{
    public static class TextureIO
    {
        public static RenderTextureReadWrite GetColorSpace(GLTFTextureType textureType)
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

        public static GLTFTextureType GetglTFTextureType(GLTFRoot glTf, int textureIndex)
        {
            foreach (var material in glTf.materials)
            {
                var textureInfo = material.GetTextures().FirstOrDefault(x => (x!=null) && x.index == textureIndex);
                if (textureInfo != null)
                {
                    return textureInfo.TextreType;
                }
            }
            return GLTFTextureType.Unknown;
        }

#if UNITY_EDITOR
        public static void MarkTextureAssetAsNormalMap(string assetPath)
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

        public static IEnumerable<TextureExportItem> GetTextures(Material m)
        {
            var props = ShaderPropExporter.PreShaderPropExporter.GetPropsForSupportedShader(m.shader.name);
            if (props == null)
            {
                yield return new TextureExportItem(m.mainTexture, GLTFTextureType.BaseColor);
            }

            foreach (var prop in props.Properties)
            {

                if (prop.ShaderPropertyType == ShaderPropExporter.ShaderPropertyType.TexEnv)
                {
                    yield return new TextureExportItem(m.GetTexture(prop.Key), GetglTFTextureType(m.shader.name, prop.Key));
                }
            }
        }


        struct BytesWithMime
        {
            public Byte[] Bytes;
            public string Mime;
        }

        static BytesWithMime GetBytesWithMime(Texture texture, GLTFTextureType textureType)
        {
#if UNITY_EDITOR
            var path = UnityPath.FromAsset(texture);
            if (path.IsUnderAssetsFolder)
            {
                if (path.Extension == ".png")
                {
                    return new BytesWithMime
                    {
                        Bytes = System.IO.File.ReadAllBytes(path.FullPath),
                        Mime = "image/png",
                    };
                }                    
            }
#endif

            return new BytesWithMime
            {
                Bytes = TextureItem.CopyTexture(texture, TextureIO.GetColorSpace(textureType), null).EncodeToPNG(),
                Mime = "image/png",
            };
        }

        public static int ExportTexture(GLTFRoot gltf, int bufferIndex, Texture texture, GLTFTextureType textureType)
        {
            var bytesWithMime = GetBytesWithMime(texture, textureType); ;

            // add view
            var view = gltf.buffers[bufferIndex].Append(bytesWithMime.Bytes, GLTFBufferTarget.NONE);
            var viewIndex = gltf.AddBufferView(view);

            // add image
            var imageIndex = gltf.images.Count;
            gltf.images.Add(new GLTFImage
            {
                name = texture.name,
                bufferView = viewIndex,
                mimeType = bytesWithMime.Mime,
            });

            // add sampler
            var samplerIndex = gltf.samplers.Count;
            var sampler = TextureSamplerUtil.Export(texture);
            gltf.samplers.Add(sampler);

            // add texture
            gltf.textures.Add(new GLTFTexture
            {
                sampler = samplerIndex,
                source = imageIndex,
            });

            return imageIndex;
        }
    }
}
