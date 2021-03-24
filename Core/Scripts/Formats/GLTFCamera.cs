using System;
using UniJSON;

namespace UMa.GLTF
{
    public enum ProjectionType
    {
        Perspective,
        Orthographic
    }

    [Serializable]
    public class GLTFOrthographic
    {
        [JsonSchema(Required = true)]
        public float xmag;
        [JsonSchema(Required = true)]
        public float ymag;
        [JsonSchema(Required = true, Minimum = 0.0f, ExclusiveMinimum = true)]
        public float zfar;
        [JsonSchema(Required = true, Minimum = 0.0f)]
        public float znear;

        [JsonSchema(MinProperties = 1)]
        public GLTFOrthographicExtensions extensions;
        [JsonSchema(MinProperties = 1)]
        public GLTFOrthographicExtras extras;
    }

    [Serializable]
    public class GLTFPerspective
    {
        [JsonSchema(Minimum = 0.0f, ExclusiveMinimum = true)]
        public float aspectRatio;
        [JsonSchema(Required = true, Minimum = 0.0f, ExclusiveMinimum = true)]
        public float yfov;
        [JsonSchema(Minimum = 0.0f, ExclusiveMinimum = true)]
        public float zfar;
        [JsonSchema(Required = true, Minimum = 0.0f, ExclusiveMinimum = true)]
        public float znear;

        public GLTFPerspectiveExtensions extensions;
        public GLTFPerspectiveExtras extras;
    }

    [Serializable]
    public class GLTFCamera
    {
        public GLTFOrthographic orthographic;
        public GLTFPerspective perspective;

        [JsonSchema(Required = true, EnumSerializationType = EnumSerializationType.AsLowerString)]
        public ProjectionType type;

        public string name;

        public GLTFCameraExtensions extensions;
        public GLTFCameraExtras extras;
    }
    [Serializable]
    [ItemJsonSchema(ValueType = ValueNodeType.Object)]
    public partial class GLTFOrthographicExtensions : ExtensionsBase<GLTFOrthographicExtensions> { }

    [Serializable]
    public partial class GLTFOrthographicExtras : ExtraBase<GLTFOrthographicExtras> { }

    [Serializable]
    [ItemJsonSchema(ValueType = ValueNodeType.Object)]
    public partial class GLTFPerspectiveExtensions : ExtensionsBase<GLTFPerspectiveExtensions> { }

    [Serializable]
    public partial class GLTFPerspectiveExtras : ExtraBase<GLTFPerspectiveExtras> { }

    [Serializable]
    [ItemJsonSchema(ValueType = ValueNodeType.Object)]
    public partial class GLTFCameraExtensions : ExtensionsBase<GLTFCameraExtensions> { }

    [Serializable]
    public partial class GLTFCameraExtras : ExtraBase<GLTFCameraExtras> { }
}
