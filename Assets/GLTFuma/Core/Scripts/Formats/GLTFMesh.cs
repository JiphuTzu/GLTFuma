﻿using System;
using System.Collections.Generic;
using UniJSON;

namespace UMa.GLTF
{
    [Serializable]
    public class GLTFMesh : JsonSerializableBase
    {
        public string name;

        [JsonSchema(Required = true, MinItems = 1)]
        public List<GLTFPrimitives> primitives;

        [JsonSchema(MinItems = 1)]
        public float[] weights;

        // empty schemas
        public object extensions;
        public object extras;

        public GLTFMesh(string _name)
        {
            name = _name;
            primitives = new List<GLTFPrimitives>();
        }

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => name);
            f.Key("primitives"); f.GLTFValue(primitives);
            if (weights != null && weights.Length > 0)
            {
                f.KeyValue(() => weights);
            }
        }
    }
    /// <summary>
    /// https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/mesh.primitive.schema.json
    /// </summary>
    [Serializable]
    public class GLTFPrimitives : JsonSerializableBase
    {
        [JsonSchema(EnumValues = new object[] { 0, 1, 2, 3, 4, 5, 6 })]
        public int mode;

        [JsonSchema(Minimum = 0)]
        public int indices = -1;

        [JsonSchema(Required = true, SkipSchemaComparison = true)]
        public GLTFAttributes attributes;

        public bool hasVertexColor { get { return attributes.COLOR_0 != -1; } }

        [JsonSchema(Minimum = 0)]
        public int material;

        [JsonSchema(MinItems = 1)]
        [ItemJsonSchema(SkipSchemaComparison = true)]
        public List<GLTFMorphTarget> targets = new List<GLTFMorphTarget>();

        public GLTFPrimitivesExtras extras = new GLTFPrimitivesExtras();

        [JsonSchema(SkipSchemaComparison = true)]
        public GLTFPrimitivesExtensions extensions = new GLTFPrimitivesExtensions();

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => mode);
            f.KeyValue(() => indices);
            f.Key("attributes"); f.GLTFValue(attributes);
            f.KeyValue(() => material);
            if (targets != null && targets.Count > 0)
            {
                f.Key("targets"); f.GLTFValue(targets);
            }
            if (extensions.KHR_draco_mesh_compression != null)
            {
                f.KeyValue(() => extensions);
            }
            if (extras.targetNames.Count > 0)
            {
                f.KeyValue(() => extras);
            }
        }
    }
    [Serializable]
    public class GLTFAttributes : JsonSerializableBase
    {
        [JsonSchema(Minimum = 0)]
        public int POSITION = -1;

        [JsonSchema(Minimum = 0)]
        public int NORMAL = -1;

        [JsonSchema(Minimum = 0)]
        public int TANGENT = -1;

        [JsonSchema(Minimum = 0)]
        public int TEXCOORD_0 = -1;

        [JsonSchema(Minimum = 0)]
        public int COLOR_0 = -1;

        [JsonSchema(Minimum = 0)]
        public int JOINTS_0 = -1;

        [JsonSchema(Minimum = 0)]
        public int WEIGHTS_0 = -1;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var rhs = obj as GLTFAttributes;
            if (rhs == null)
            {
                return base.Equals(obj);
            }

            return POSITION == rhs.POSITION
                && NORMAL == rhs.NORMAL
                && TANGENT == rhs.TANGENT
                && TEXCOORD_0 == rhs.TEXCOORD_0
                && COLOR_0 == rhs.COLOR_0
                && JOINTS_0 == rhs.JOINTS_0
                && WEIGHTS_0 == rhs.WEIGHTS_0
                ;
        }

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => POSITION);
            if (NORMAL != -1) f.KeyValue(() => NORMAL);
            if (TANGENT != -1) f.KeyValue(() => TANGENT);
            if (TEXCOORD_0 != -1) f.KeyValue(() => TEXCOORD_0);
            if (COLOR_0 != -1) f.KeyValue(() => COLOR_0);
            if (JOINTS_0 != -1) f.KeyValue(() => JOINTS_0);
            if (WEIGHTS_0 != -1) f.KeyValue(() => WEIGHTS_0);
        }
    }

    [Serializable]
    public class GLTFMorphTarget : JsonSerializableBase
    {
        public int POSITION = -1;
        public int NORMAL = -1;
        public int TANGENT = -1;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => POSITION);
            if (NORMAL >= 0) f.KeyValue(() => NORMAL);
            if (TANGENT >= 0) f.KeyValue(() => TANGENT);
        }
    }
    /// <summary>
    /// https://github.com/KhronosGroup/glTF/issues/1036
    /// </summary>
    [Serializable]
    public partial class GLTFPrimitivesExtras : ExtraBase<GLTFPrimitivesExtras>
    {
        [JsonSchema(Required = true, MinItems = 1)]
        public List<string> targetNames = new List<string>();

        [JsonSerializeMembers]
        void PrimitiveMembers(GLTFJsonFormatter f)
        {
            if (targetNames.Count > 0)
            {
                f.Key("targetNames");
                f.BeginList();
                foreach (var x in targetNames)
                {
                    f.Value(x);
                }
                f.EndList();
            }
        }
    }
    [Serializable]
    public class KHRDracoMeshCompression : JsonSerializableBase
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int bufferView = -1;
        public GLTFAttributes attributes;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            //throw new NotImplementedException();
        }
    }

    [Serializable]
    public partial class GLTFPrimitivesExtensions : ExtensionsBase<GLTFPrimitivesExtensions>
    {
        [JsonSchema(Required = true)]
        public KHRDracoMeshCompression KHR_draco_mesh_compression;

        [JsonSerializeMembers]
        void SerializeMembers_draco(GLTFJsonFormatter f)
        {
            //throw new NotImplementedException();
        }
    }
}