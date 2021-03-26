using System;
using System.Linq;
using System.Collections.Generic;
using UniJSON;
using UnityEngine;

namespace UMa.GLTF
{
    [Serializable]
    public class GLTFAnimationTarget : JsonSerializableBase
    {
        [JsonSchema(Minimum = 0)]
        public int node;

        [JsonSchema(Required = true, EnumValues = new object[] { "translation", "rotation", "scale", "weights","active" }, EnumSerializationType = EnumSerializationType.AsString)]
        public string path;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => node);
            if (!string.IsNullOrEmpty(path))
            {
                f.KeyValue(() => path);
            }
        }

        public enum Interpolation
        {
            LINEAR,
            STEP,
            CUBICSPLINE
        }

        public const string PATH_TRANSLATION = "translation";
        public const string PATH_EULER_ROTATION = "rotation";
        public const string PATH_ROTATION = "rotation";
        public const string PATH_SCALE = "scale";
        public const string PATH_WEIGHT = "weights";
        public const string PATH_ACTIVE = "active";
        public const string NOT_IMPLEMENTED = "NotImplemented";

        public enum AnimationProperty
        {
            Translation,
            EulerRotation,
            Rotation,
            Scale,
            Weight,
            BlendShape,
            Active,

            NotImplemented
        }

        public static string GetPathName(AnimationProperty property)
        {
            switch (property)
            {
                case AnimationProperty.Translation:
                    return PATH_TRANSLATION;
                case AnimationProperty.EulerRotation: 
                case AnimationProperty.Rotation:
                    return PATH_ROTATION;
                case AnimationProperty.Scale:
                    return PATH_SCALE;
                case AnimationProperty.BlendShape:
                    return PATH_WEIGHT;
                case AnimationProperty.Active:
                    return PATH_ACTIVE;
                default: throw new NotImplementedException();
            }
        }

        public static AnimationProperty GetAnimationProperty(string path)
        {
            switch (path)
            {
                case PATH_TRANSLATION:
                    return AnimationProperty.Translation;
                case PATH_ROTATION:
                    return AnimationProperty.Rotation;
                case PATH_SCALE:
                    return AnimationProperty.Scale;
                case PATH_WEIGHT:
                    return AnimationProperty.BlendShape;
                case PATH_ACTIVE:
                    return AnimationProperty.Active;
                default: throw new NotImplementedException(path);
            }
        }

        public static int GetElementCount(AnimationProperty property)
        {
            switch (property)
            {
                case AnimationProperty.Translation: return 3;
                case AnimationProperty.EulerRotation: return 3;
                case AnimationProperty.Rotation: return 4;
                case AnimationProperty.Scale: return 3;
                case AnimationProperty.BlendShape: return 1;
                case AnimationProperty.Active: return 1;
                default: throw new NotImplementedException();
            }
        }

        public static int GetElementCount(string path)
        {
            return GetElementCount(GetAnimationProperty(path));
        }
    }

    [Serializable]
    public class GLTFAnimationChannel : JsonSerializableBase
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int sampler = -1;

        [JsonSchema(Required = true)]
        public GLTFAnimationTarget target;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => sampler);
            f.KeyValue(() => target);
        }
    }

    [Serializable]
    public class GLTFAnimationSampler : JsonSerializableBase
    {
        [JsonSchema(Required = true, Minimum = 0)]
        public int input = -1;

        [JsonSchema(EnumValues = new object[] { "LINEAR", "STEP", "CUBICSPLINE" }, EnumSerializationType = EnumSerializationType.AsString)]
        public string interpolation;

        [JsonSchema(Required = true, Minimum = 0)]
        public int output = -1;

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => input);
            if (!string.IsNullOrEmpty(interpolation))
            {
                f.KeyValue(() => interpolation);
            }
            f.KeyValue(() => output);
        }
    }

    [Serializable]
    public class GLTFAnimation : JsonSerializableBase
    {
        public string name = "";

        [JsonSchema(Required = true, MinItems = 1)]
        public List<GLTFAnimationChannel> channels = new List<GLTFAnimationChannel>();

        [JsonSchema(Required = true, MinItems = 1)]
        public List<GLTFAnimationSampler> samplers = new List<GLTFAnimationSampler>();

        // empty schemas
        public object extensions;
        public object extras;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            if (!string.IsNullOrEmpty(name))
            {
                f.KeyValue(() => name);
            }
            Debug.Log(channels.Count+" -- "+samplers.Count);
            f.KeyValue(() => channels);
            f.KeyValue(() => samplers);
        }

        public int AddChannelAndGetSampler(int nodeIndex, GLTFAnimationTarget.AnimationProperty property)
        {
            // find channel
            var channel = channels.FirstOrDefault(x => x.target.node == nodeIndex && x.target.path == GLTFAnimationTarget.GetPathName(property));
            if (channel != null)
            {
                return channel.sampler;
            }

            // not found. create new
            var samplerIndex = samplers.Count;
            var sampler = new GLTFAnimationSampler();
            samplers.Add(sampler);

            channel = new GLTFAnimationChannel
            {
                sampler = samplerIndex,
                target = new GLTFAnimationTarget
                {
                    node = nodeIndex,
                    path = GLTFAnimationTarget.GetPathName(property),
                },
            };
            channels.Add(channel);

            return samplerIndex;
        }
    }
}
