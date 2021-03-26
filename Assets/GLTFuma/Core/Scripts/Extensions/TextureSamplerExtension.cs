using System;
using System.Collections.Generic;
using UnityEngine;


namespace UMa.GLTF
{
    public static class TextureSamplerExtension
    {
        #region WrapMode
        public enum TextureWrapType
        {
            All,
            U,
            V,
            W
        }

        private static KeyValuePair<TextureWrapType, TextureWrapMode> TypeWithMode(TextureWrapType type, TextureWrapMode mode)
        {
            return new KeyValuePair<TextureWrapType, TextureWrapMode>(type, mode);
        }

        private static IEnumerable<KeyValuePair<TextureWrapType, TextureWrapMode>> GetUnityWrapModes(GLTFTextureSampler sampler)
        {
            if (sampler.wrapS == sampler.wrapT)
            {
                switch (sampler.wrapS)
                {
                    case GLTFWrap.NONE: // default
                        yield return TypeWithMode(TextureWrapType.All, TextureWrapMode.Repeat);
                        break;

                    case GLTFWrap.CLAMP_TO_EDGE:
                        yield return TypeWithMode(TextureWrapType.All, TextureWrapMode.Clamp);
                        break;

                    case GLTFWrap.REPEAT:
                        yield return TypeWithMode(TextureWrapType.All, TextureWrapMode.Repeat);
                        break;

                    case GLTFWrap.MIRRORED_REPEAT:
                        yield return TypeWithMode(TextureWrapType.All, TextureWrapMode.Mirror);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (sampler.wrapS)
                {
                    case GLTFWrap.NONE: // default
                        yield return TypeWithMode(TextureWrapType.U, TextureWrapMode.Repeat);
                        break;

                    case GLTFWrap.CLAMP_TO_EDGE:
                        yield return TypeWithMode(TextureWrapType.U, TextureWrapMode.Clamp);
                        break;

                    case GLTFWrap.REPEAT:
                        yield return TypeWithMode(TextureWrapType.U, TextureWrapMode.Repeat);
                        break;

                    case GLTFWrap.MIRRORED_REPEAT:
                        yield return TypeWithMode(TextureWrapType.U, TextureWrapMode.Mirror);
                        break;

                    default:
                        throw new NotImplementedException();
                }
                switch (sampler.wrapT)
                {
                    case GLTFWrap.NONE: // default
                        yield return TypeWithMode(TextureWrapType.V, TextureWrapMode.Repeat);
                        break;

                    case GLTFWrap.CLAMP_TO_EDGE:
                        yield return TypeWithMode(TextureWrapType.V, TextureWrapMode.Clamp);
                        break;

                    case GLTFWrap.REPEAT:
                        yield return TypeWithMode(TextureWrapType.V, TextureWrapMode.Repeat);
                        break;

                    case GLTFWrap.MIRRORED_REPEAT:
                        yield return TypeWithMode(TextureWrapType.V, TextureWrapMode.Mirror);
                        break;

                    default:
                        throw new NotImplementedException();
                }

            }
        }
        #endregion

        private static FilterMode ToFilterMode(this GLTFFilter filterMode)
        {
            switch (filterMode)
            {
                case GLTFFilter.NEAREST:
                case GLTFFilter.NEAREST_MIPMAP_LINEAR:
                case GLTFFilter.NEAREST_MIPMAP_NEAREST:
                    return FilterMode.Point;

                case GLTFFilter.NONE:
                case GLTFFilter.LINEAR:
                case GLTFFilter.LINEAR_MIPMAP_NEAREST:
                    return FilterMode.Bilinear;

                case GLTFFilter.LINEAR_MIPMAP_LINEAR:
                    return FilterMode.Trilinear;

                default:
                    throw new NotImplementedException();

            }
        }

        public static void SetSampler(this Texture2D texture, GLTFTextureSampler sampler)
        {
            if (texture == null) return;
            var wrapModes= GetUnityWrapModes(sampler);
            foreach (var kv in wrapModes)
            {
                switch (kv.Key)
                {
                    case TextureWrapType.All:
                        texture.wrapMode = kv.Value;
                        break;
                    case TextureWrapType.U:
                        texture.wrapModeU = kv.Value;
                        break;
                    case TextureWrapType.V:
                        texture.wrapModeV = kv.Value;
                        break;
                    case TextureWrapType.W:
                        texture.wrapModeW = kv.Value;
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            texture.filterMode = sampler.minFilter.ToFilterMode();
        }

        #region Export
        private static GLTFFilter GetFilter(Texture texture)
        {
            switch (texture.filterMode)
            {
                case FilterMode.Point:
                    return GLTFFilter.NEAREST;

                case FilterMode.Bilinear:
                    return GLTFFilter.LINEAR;

                case FilterMode.Trilinear:
                    return GLTFFilter.LINEAR_MIPMAP_LINEAR;

                default:
                    throw new NotImplementedException();
            }
        }

        private static TextureWrapMode GetWrapS(this Texture texture)
        {
            return texture.wrapModeU;
        }

        private static TextureWrapMode GetWrapT(this Texture texture)
        {
            return texture.wrapModeV;
        }

        private static GLTFWrap GetWrapMode(this TextureWrapMode wrapMode)
        {
            switch (wrapMode)
            {
                case TextureWrapMode.Clamp:
                    return GLTFWrap.CLAMP_TO_EDGE;

                case TextureWrapMode.Repeat:
                    return GLTFWrap.REPEAT;

                case TextureWrapMode.Mirror:
                case TextureWrapMode.MirrorOnce:
                    return GLTFWrap.MIRRORED_REPEAT;

                default:
                    throw new NotImplementedException(wrapMode.ToString());
            }
        }

        public static GLTFTextureSampler ToTextureSampler(this Texture texture)
        {
            var filter = GetFilter(texture);
            var wrapS = texture.GetWrapS().GetWrapMode();
            var wrapT = texture.GetWrapT().GetWrapMode();
            return new GLTFTextureSampler
            {
                magFilter = filter,
                minFilter = filter,
                wrapS = wrapS,
                wrapT = wrapT,
            };
        }
        #endregion
    }
}
