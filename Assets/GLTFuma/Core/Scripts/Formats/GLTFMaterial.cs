using System;
using UniJSON;

namespace UMa.GLTF
{
    public enum GLTFTextureType
    {
        BaseColor,
        Metallic,
        Normal,
        Occlusion,
        Emissive,
        Unknown
    }

    public interface IGLTFTextureinfo
    {
        GLTFTextureType TextreType { get; }
    }

    [Serializable]
    public abstract class GLTFTextureInfo : JsonSerializableBase, IGLTFTextureinfo
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int index = -1;

        [JsonSchema(Minimum = 0)]
        public int texCoord;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => index);
            f.KeyValue(() => texCoord);
        }

        public abstract GLTFTextureType TextreType { get; }
    }


    [Serializable]
    public class GLTFMaterialBaseColorTextureInfo : GLTFTextureInfo
    {
        public override GLTFTextureType TextreType
        {
            get { return GLTFTextureType.BaseColor; }
        }
    }

    [Serializable]
    public class GLTFMaterialMetallicRoughnessTextureInfo : GLTFTextureInfo
    {
        public override GLTFTextureType TextreType
        {
            get { return GLTFTextureType.Metallic; }
        }
    }

    [Serializable]
    public class GLTFMaterialNormalTextureInfo : GLTFTextureInfo
    {
        public float scale = 1.0f;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => scale);
            base.SerializeMembers(f);
        }

        public override GLTFTextureType TextreType
        {
            get { return GLTFTextureType.Normal; }
        }
    }

    [Serializable]
    public class GLTFMaterialOcclusionTextureInfo : GLTFTextureInfo
    {
        [JsonSchema(Minimum = 0.0, Maximum = 1.0)]
        public float strength = 1.0f;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => strength);
            base.SerializeMembers(f);
        }

        public override GLTFTextureType TextreType
        {
            get { return GLTFTextureType.Occlusion; }
        }
    }

    [Serializable]
    public class GLTFMaterialEmissiveTextureInfo : GLTFTextureInfo
    {
        public override GLTFTextureType TextreType
        {
            get { return GLTFTextureType.Emissive; }
        }
    }

    [Serializable]
    public class GLTFPbrMetallicRoughness : JsonSerializableBase
    {
        public GLTFMaterialBaseColorTextureInfo baseColorTexture = null;

        [JsonSchema(MinItems = 4, MaxItems = 4)]
        [ItemJsonSchema(Minimum = 0.0, Maximum = 1.0)]
        public float[] baseColorFactor;

        public GLTFMaterialMetallicRoughnessTextureInfo metallicRoughnessTexture = null;

        [JsonSchema(Minimum = 0.0, Maximum = 1.0)]
        public float metallicFactor = 1.0f;

        [JsonSchema(Minimum = 0.0, Maximum = 1.0)]
        public float roughnessFactor = 1.0f;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            if (baseColorTexture != null)
            {
                f.KeyValue(() => baseColorTexture);
            }
            if (baseColorFactor != null)
            {
                f.KeyValue(() => baseColorFactor);
            }
            if (metallicRoughnessTexture != null)
            {
                f.KeyValue(() => metallicRoughnessTexture);
            }
            f.KeyValue(() => metallicFactor);
            f.KeyValue(() => roughnessFactor);
        }
    }

    [Serializable]
    public class GLTFMaterial : JsonSerializableBase
    {
        public string name;
        public GLTFPbrMetallicRoughness pbrMetallicRoughness;
        public GLTFMaterialNormalTextureInfo normalTexture = null;

        public GLTFMaterialOcclusionTextureInfo occlusionTexture = null;

        public GLTFMaterialEmissiveTextureInfo emissiveTexture = null;

        [JsonSchema(MinItems = 3, MaxItems = 3)]
        [ItemJsonSchema(Minimum = 0.0, Maximum = 1.0)]
        public float[] emissiveFactor;

        [JsonSchema(EnumValues = new object[] { "OPAQUE", "MASK", "BLEND" }, EnumSerializationType = EnumSerializationType.AsUpperString)]
        public string alphaMode;

        [JsonSchema(Dependencies = new string[] { "alphaMode" }, Minimum = 0.0)]
        public float alphaCutoff = 0.5f;

        public bool doubleSided;

        [JsonSchema(SkipSchemaComparison = true)]
        public GLTFMaterialExtensions extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            if (!String.IsNullOrEmpty(name))
            {
                f.Key("name"); f.Value(name);
            }
            if (pbrMetallicRoughness != null)
            {
                f.Key("pbrMetallicRoughness"); f.GLTFValue(pbrMetallicRoughness);
            }
            if (normalTexture != null)
            {
                f.Key("normalTexture"); f.GLTFValue(normalTexture);
            }
            if (occlusionTexture != null)
            {
                f.Key("occlusionTexture"); f.GLTFValue(occlusionTexture);
            }
            if (emissiveTexture != null)
            {
                f.Key("emissiveTexture"); f.GLTFValue(emissiveTexture);
            }
            if (emissiveFactor != null)
            {
                f.Key("emissiveFactor"); f.Serialize(emissiveFactor);
            }

            f.KeyValue(() => doubleSided);

            if (!string.IsNullOrEmpty(alphaMode))
            {
                f.KeyValue(() => alphaMode);
            }

            if (extensions != null)
            {
                f.KeyValue(() => extensions);
            }
        }

        public GLTFTextureInfo[] GetTextures()
        {
            return new GLTFTextureInfo[]
            {
                pbrMetallicRoughness.baseColorTexture,
                pbrMetallicRoughness.metallicRoughnessTexture,
                normalTexture,
                occlusionTexture,
                emissiveTexture
            };
        }
    }
    [Serializable]
    public class KHRMaterialUnlit : JsonSerializableBase
    {
        public static string ExtensionName
        {
            get
            {
                return "KHR_materials_unlit";
            }
        }

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            //throw new System.NotImplementedException();
        }

        public static GLTFMaterial CreateDefault()
        {
            return new GLTFMaterial
            {
                pbrMetallicRoughness = new GLTFPbrMetallicRoughness
                {
                    baseColorFactor = new float[] { 1.0f, 1.0f, 1.0f, 1.0f },
                    roughnessFactor = 0.9f,
                    metallicFactor = 0.0f,
                },
                extensions = new GLTFMaterialExtensions
                {
                    KHR_materials_unlit = new KHRMaterialUnlit(),
                },
            };
        }
    }

    [Serializable]
    public partial class GLTFMaterialExtensions : ExtensionsBase<GLTFMaterialExtensions>
    {
        [JsonSchema(Required = true)]
        public KHRMaterialUnlit KHR_materials_unlit;

        [JsonSerializeMembers]
        void SerializeMembers_unlit(GLTFJsonFormatter f)
        {
            if (KHR_materials_unlit != null)
            {
                f.KeyValue(() => KHR_materials_unlit);
            }
        }
    }
}
