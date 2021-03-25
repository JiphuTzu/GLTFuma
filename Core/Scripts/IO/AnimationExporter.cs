using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UMa.GLTF
{

    public static class AnimationExporter
    {
        public class InputOutputValues
        {
            public float[] input;
            public float[] output;
        }

        public class AnimationWithSampleCurves
        {
            public GLTFAnimation animation;
            public Dictionary<int, InputOutputValues> samplers = new Dictionary<int, InputOutputValues>();
        }

#if UNITY_EDITOR
        public static List<AnimationClip> GetAnimationClips(Animation animation)
        {
            var clips = new List<AnimationClip>();
            foreach (AnimationState state in animation)
            {
                clips.Add(state.clip);
            }
            return clips;
        }

        public static List<AnimationClip> GetAnimationClips(Animator animator)
        {
            var clips = new List<AnimationClip>();

            RuntimeAnimatorController runtimeAnimatorController = animator.runtimeAnimatorController;
            UnityEditor.Animations.AnimatorController animationController = runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

            if (animationController == null)
            {
                return clips;
            }

            foreach (var layer in animationController.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    clips.Add(state.state.motion as AnimationClip);
                }
            }
            return clips;
        }

        static int GetNodeIndex(Transform root, List<Transform> nodes, string path)
        {
            var descendant = root.GetFromPath(path);
            return nodes.IndexOf(descendant);
        }

        public static GLTFAnimationTarget.AnimationProperty PropertyToTarget(string property)
        {
            if (property.StartsWith("m_LocalPosition."))
            {
                return GLTFAnimationTarget.AnimationProperty.Translation;
            }
            else if (property.StartsWith("localEulerAnglesRaw."))
            {
                return GLTFAnimationTarget.AnimationProperty.EulerRotation;
            }
            else if (property.StartsWith("m_LocalRotation."))
            {
                return GLTFAnimationTarget.AnimationProperty.Rotation;
            }
            else if (property.StartsWith("m_LocalScale."))
            {
                return GLTFAnimationTarget.AnimationProperty.Scale;
            }
            else if (property.StartsWith("blendShape."))
            {
                return GLTFAnimationTarget.AnimationProperty.BlendShape;
            }
            else
            {
                return GLTFAnimationTarget.AnimationProperty.NotImplemented;
            }
        }

        public static int GetElementOffset(string property)
        {
            if (property.EndsWith(".x"))
            {
                return 0;
            }
            if (property.EndsWith(".y") || property.StartsWith("blendShape."))
            {
                return 1;
            }
            if (property.EndsWith(".z"))
            {
                return 2;
            }
            if (property.EndsWith(".w"))
            {
                return 3;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static AnimationWithSampleCurves Export(AnimationClip clip, Transform root, List<Transform> nodes)
        {
            var animation = new AnimationWithSampleCurves
            {
                animation = new GLTFAnimation(),
            };

            List<AnimationCurveData> curveDatas = new List<AnimationCurveData>();
            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);

                var property = AnimationExporter.PropertyToTarget(binding.propertyName);
                if (property == GLTFAnimationTarget.AnimationProperty.NotImplemented)
                {
                    Debug.LogWarning("Not Implemented keyframe property : " + binding.propertyName);
                    continue;
                }
                if (property == GLTFAnimationTarget.AnimationProperty.EulerRotation)
                {
                    Debug.LogWarning("Interpolation setting of AnimationClip should be Quaternion");
                    continue;
                }

                var nodeIndex = GetNodeIndex(root, nodes, binding.path);
                var samplerIndex = animation.animation.AddChannelAndGetSampler(nodeIndex, property);
                var elementCount = 0;
                if (property == GLTFAnimationTarget.AnimationProperty.BlendShape)
                {
                    var mesh = nodes[nodeIndex].GetComponent<SkinnedMeshRenderer>().sharedMesh;
                    elementCount = mesh.blendShapeCount;
                }
                else
                {
                    elementCount = GLTFAnimationTarget.GetElementCount(property);
                }

                // 同一のsamplerIndexが割り当てられているcurveDataがある場合はそれを使用し、無ければ作る
                    var curveData = curveDatas.FirstOrDefault(x => x.samplerIndex == samplerIndex);
                if (curveData == null)
                {
                    curveData = new AnimationCurveData(AnimationUtility.GetKeyRightTangentMode(curve, 0), property, samplerIndex, elementCount);
                    curveDatas.Add(curveData);
                }

                // 全てのキーフレームを回収
                int elementOffset = 0;
                float valueFactor = 1.0f;
                if (property == GLTFAnimationTarget.AnimationProperty.BlendShape)
                {
                    var mesh = nodes[nodeIndex].GetComponent<SkinnedMeshRenderer>().sharedMesh;
                    var blendShapeName = binding.propertyName.Replace("blendShape.", "");
                    elementOffset = mesh.GetBlendShapeIndex(blendShapeName);
                    valueFactor = 0.01f;
                }
                else
                {
                    elementOffset = AnimationExporter.GetElementOffset(binding.propertyName);
                }

                if (elementOffset >= 0 && elementOffset < elementCount)
                {
                    for (int i = 0; i < curve.keys.Length; i++)
                    {
                        curveData.SetKeyframeData(curve.keys[i].time, curve.keys[i].value * valueFactor, elementOffset);
                    }
                }
            }

            //キー挿入
            foreach (var curve in curveDatas)
            {
                if (curve.Keyframes.Count == 0)
                    continue;

                curve.RecountEmptyKeyframe();

                var elementNum = curve.Keyframes.First().Values.Length;
                var values = default(InputOutputValues);
                if (!animation.samplers.TryGetValue(curve.samplerIndex, out values))
                {
                    values = new InputOutputValues();
                    values.input = new float[curve.Keyframes.Count];
                    values.output = new float[curve.Keyframes.Count * elementNum];
                    animation.samplers[curve.samplerIndex] = values;
                    animation.animation.samplers[curve.samplerIndex].interpolation = curve.GetInterpolation();
                }

                int keyframeIndex = 0;
                foreach (var keyframe in curve.Keyframes)
                {
                    values.input[keyframeIndex] = keyframe.time;
                    Buffer.BlockCopy(keyframe.GetRightHandCoordinate(), 0, values.output, keyframeIndex * elementNum * sizeof(float), elementNum * sizeof(float));
                    keyframeIndex++;
                }
            }

            return animation;
        }
#endif
        }
    }