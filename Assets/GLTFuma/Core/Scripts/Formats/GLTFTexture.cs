using System;
using System.IO;
using UniJSON;
using UnityEngine;

namespace UMa.GLTF
{
    [Serializable]
    public class GLTFTextureSampler : JsonSerializableBase
    {
        [JsonSchema(EnumSerializationType = EnumSerializationType.AsInt,
            EnumExcludes = new object[] {
                GLTFFilter.NONE,
                GLTFFilter.NEAREST_MIPMAP_NEAREST,
                GLTFFilter.LINEAR_MIPMAP_NEAREST,
                GLTFFilter.NEAREST_MIPMAP_LINEAR,
                GLTFFilter.LINEAR_MIPMAP_LINEAR,
            })]
        public GLTFFilter magFilter = GLTFFilter.NEAREST;

        [JsonSchema(EnumSerializationType = EnumSerializationType.AsInt,
            EnumExcludes = new object[] { GLTFFilter.NONE })]
        public GLTFFilter minFilter = GLTFFilter.NEAREST;

        [JsonSchema(EnumSerializationType = EnumSerializationType.AsInt,
            EnumExcludes = new object[] { GLTFWrap.NONE })]
        public GLTFWrap wrapS = GLTFWrap.REPEAT;

        [JsonSchema(EnumSerializationType = EnumSerializationType.AsInt,
            EnumExcludes = new object[] { GLTFWrap.NONE })]
        public GLTFWrap wrapT = GLTFWrap.REPEAT;

        // empty schemas
        public object extensions;
        public object extras;
        public string name;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.Key("magFilter"); f.Value((int)magFilter);
            f.Key("minFilter"); f.Value((int)minFilter);
            f.Key("wrapS"); f.Value((int)wrapS);
            f.Key("wrapT"); f.Value((int)wrapT);
        }
    }

    [Serializable]
    public class GLTFImage : JsonSerializableBase
    {
        public string name;
        public string uri;

        [JsonSchema(Dependencies = new string[] { "mimeType" }, Minimum = 0)]
        public int bufferView;

        [JsonSchema(EnumValues = new object[] { "image/jpeg", "image/png" }, EnumSerializationType =EnumSerializationType.AsString)]
        public string mimeType;

        public string GetExt()
        {
            switch (mimeType)
            {
                case "image/png":
                    return ".png";

                case "image/jpeg":
                    return ".jpg";

                default:
                    if (uri.StartsWith("data:image/jpeg;"))
                    {
                        return ".jpg";
                    }
                    else if (uri.StartsWith("data:image/png;"))
                    {
                        return ".png";
                    }
                    else
                    {
                        return Path.GetExtension(uri).ToLower();
                    }
            }
        }

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            if (!string.IsNullOrEmpty(uri))
            {
                f.KeyValue(() => uri);
            }
            else
            {
                f.KeyValue(() => name);
                f.KeyValue(() => bufferView);
                f.KeyValue(() => mimeType);
            }
        }
    }

    [Serializable]
    public class GLTFTexture : JsonSerializableBase
    {
        [JsonSchema(Minimum = 0)]
        public int sampler;

        [JsonSchema(Minimum = 0)]
        public int source;

        // empty schemas
        public object extensions;
        public object extras;
        public string name;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => sampler);
            f.KeyValue(() => source);
        }
    }
}
