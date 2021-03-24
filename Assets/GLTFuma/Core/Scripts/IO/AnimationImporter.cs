using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UMa.GLTF
{
    public static class AnimationImporter
    {
        private enum TangentMode
        {
            Linear,
            Constant,
            Cubicspline
        }

        private static TangentMode GetTangentMode(string interpolation)
        {
            if (interpolation == GLTFAnimationTarget.Interpolation.LINEAR.ToString())
            {
                return TangentMode.Linear;
            }
            else if (interpolation == GLTFAnimationTarget.Interpolation.STEP.ToString())
            {
                return TangentMode.Constant;
            }
            else if (interpolation == GLTFAnimationTarget.Interpolation.CUBICSPLINE.ToString())
            {
                return TangentMode.Cubicspline;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private static void CalculateTanget(List<Keyframe> keyframes, int current)
        {
            int back = current - 1;
            if (back < 0)
            {
                return;
            }
            if (current < keyframes.Count)
            {
                var rightTangent = (keyframes[current].value - keyframes[back].value) / (keyframes[current].time - keyframes[back].time);
                keyframes[back] = new Keyframe(keyframes[back].time, keyframes[back].value, keyframes[back].inTangent, rightTangent);

                var leftTangent = (keyframes[back].value - keyframes[current].value) / (keyframes[back].time - keyframes[current].time);
                keyframes[current] = new Keyframe(keyframes[current].time, keyframes[current].value, leftTangent, 0);
            }
        }

        public static Quaternion GetShortest(Quaternion last, Quaternion rot)
        {
            if (Quaternion.Dot(last, rot) > 0.0)
            {
                return rot;
            }
            else
            {
                return new Quaternion(-rot.x, -rot.y, -rot.z, -rot.w);
            }

        }

        //public delegate float[] ReverseZ(float[] current, float[] last);
        public static void SetAnimationCurve(
            AnimationClip targetClip,
            string relativePath,
            string[] propertyNames,
            float[] input,
            float[] output,
            string interpolation,
            Type curveType,
            Func<float[],float[],float[]> reverse)
        {
            var tangentMode = GetTangentMode(interpolation);
            // Debug.Log(relativePath+" == "+interpolation);
            // Debug.Log("input = "+string.Join(",",input));
            // Debug.Log("output = "+string.Join(",",output));
            var curveCount = propertyNames.Length;
            AnimationCurve[] curves = new AnimationCurve[curveCount];
            List<Keyframe>[] keyframes = new List<Keyframe>[curveCount];

            int elementNum = curveCount;
            int inputIndex = 0;
            //Quaternion用
            float[] last = new float[curveCount];
            if (last.Length == 4)
            {
                last[3] = 1.0f;
            }
            for (inputIndex = 0; inputIndex < input.Length; ++inputIndex)
            {
                var time = input[inputIndex];
                var outputIndex = 0;
                if (tangentMode == TangentMode.Cubicspline)
                {
                    outputIndex = inputIndex * elementNum * 3;
                    var value = new float[curveCount];
                    for (int i = 0; i < value.Length; i++)
                    {
                        value[i] = output[outputIndex + elementNum + i];
                    }
                    var reversed = reverse(value, last);
                    last = reversed;
                    for (int i = 0; i < keyframes.Length; i++)
                    {
                        if (keyframes[i] == null)
                            keyframes[i] = new List<Keyframe>();
                        keyframes[i].Add(new Keyframe(
                            time,
                            reversed[i],
                            output[outputIndex + i],
                            output[outputIndex + i + elementNum * 2]));
                    }
                }
                else
                {
                    outputIndex = inputIndex * elementNum;
                    var value = new float[curveCount];
                    for (int i = 0; i < value.Length; i++)
                    {
                        value[i] = output[outputIndex + i];
                    }
                    var reversed = reverse(value, last);
                    last = reversed;

                    for (int i = 0; i < keyframes.Length; i++)
                    {
                        if (keyframes[i] == null)
                            keyframes[i] = new List<Keyframe>();
                        if (tangentMode == TangentMode.Linear)
                        {
                            keyframes[i].Add(new Keyframe(time, reversed[i], 0, 0));
                            if (keyframes[i].Count > 0)
                            {
                                CalculateTanget(keyframes[i], keyframes[i].Count - 1);
                            }
                        }
                        else if (tangentMode == TangentMode.Constant)
                            keyframes[i].Add(new Keyframe(time, reversed[i], 0, float.PositiveInfinity));
                    }
                }
            }

            for (int i = 0; i < curves.Length; i++)
            {
                curves[i] = new AnimationCurve();
                for (int j = 0; j < keyframes[i].Count; j++)
                {
                    curves[i].AddKey(keyframes[i][j]);
                }

                targetClip.SetCurve(relativePath, curveType, propertyNames[i], curves[i]);
            }
        }

        public static List<AnimationClip> ImportAnimationClip(GLTFImporter importer)
        {
            List<AnimationClip> animasionClips = new List<AnimationClip>();
            for (int i = 0; i < importer.gltf.animations.Count; ++i)
            {
                var clip = new AnimationClip();
                clip.ClearCurves();
                clip.legacy = true;
                clip.name = importer.gltf.animations[i].name;
                if (string.IsNullOrEmpty(clip.name))
                {
                    clip.name = "legacy_" + i;
                }
                clip.wrapMode = WrapMode.Loop;

                var animation = importer.gltf.animations[i];
                if (string.IsNullOrEmpty(animation.name))
                {
                    animation.name = string.Format("animation:{0}", i);
                }

                foreach (var channel in animation.channels)
                {
                    var targetTransform = importer.nodes[channel.target.node];
                    var relativePath = targetTransform.RelativePathFrom(importer.root.transform);
                    switch (channel.target.path)
                    {
                        case GLTFAnimationTarget.PATH_TRANSLATION:
                            {
                                var sampler = animation.samplers[channel.sampler];
                                var input = importer.gltf.GetArrayFromAccessor<float>(sampler.input);
                                var output = importer.gltf.GetArrayFromAccessorAsFloat(sampler.output);

                                AnimationImporter.SetAnimationCurve(
                                    clip,
                                    relativePath,
                                    new string[] { "localPosition.x", "localPosition.y", "localPosition.z" },
                                    input,
                                    output,
                                    sampler.interpolation,
                                    typeof(Transform),
                                    (values, last) =>
                                    {
                                        Vector3 temp = new Vector3(values[0], values[1], values[2]);
                                        return temp.ReverseZ().ToArray();
                                    }
                                    );
                            }
                            break;

                        case GLTFAnimationTarget.PATH_ROTATION:
                            {
                                var sampler = animation.samplers[channel.sampler];
                                var input = importer.gltf.GetArrayFromAccessor<float>(sampler.input);
                                var output = importer.gltf.GetArrayFromAccessorAsFloat(sampler.output);

                                AnimationImporter.SetAnimationCurve(
                                    clip,
                                    relativePath,
                                    new string[] { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" },
                                    input,
                                    output,
                                    sampler.interpolation,
                                    typeof(Transform),
                                    (values, last) =>
                                    {
                                        Quaternion currentQuaternion = new Quaternion(values[0], values[1], values[2], values[3]);
                                        Quaternion lastQuaternion = new Quaternion(last[0], last[1], last[2], last[3]);
                                        return AnimationImporter.GetShortest(lastQuaternion, currentQuaternion.ReverseZ()).ToArray();
                                    }
                                );

                                clip.EnsureQuaternionContinuity();
                            }
                            break;

                        case GLTFAnimationTarget.PATH_SCALE:
                            {
                                var sampler = animation.samplers[channel.sampler];
                                var input = importer.gltf.GetArrayFromAccessor<float>(sampler.input);
                                var output = importer.gltf.GetArrayFromAccessorAsFloat(sampler.output);

                                AnimationImporter.SetAnimationCurve(
                                    clip,
                                    relativePath,
                                    new string[] { "localScale.x", "localScale.y", "localScale.z" },
                                    input,
                                    output,
                                    sampler.interpolation,
                                    typeof(Transform),
                                    (values, last) => values);
                            }
                            break;

                        case GLTFAnimationTarget.PATH_WEIGHT:
                            {
                                var node = importer.gltf.nodes[channel.target.node];
                                var mesh = importer.gltf.meshes[node.mesh];
                                //var primitive = mesh.primitives.FirstOrDefault();
                                //var targets = primitive.targets;

                                List<string> blendShapeNames = new List<string>();
                                var transform = importer.nodes[channel.target.node];
                                var skinnedMeshRenderer = transform.GetComponent<SkinnedMeshRenderer>();
                                if (skinnedMeshRenderer == null)
                                {
                                    continue;
                                }

                                for (int j = 0; j < skinnedMeshRenderer.sharedMesh.blendShapeCount; j++)
                                {
                                    blendShapeNames.Add(skinnedMeshRenderer.sharedMesh.GetBlendShapeName(j));
                                }

                                var keyNames = blendShapeNames
                                    .Where(x => !string.IsNullOrEmpty(x))
                                    .Select(x => "blendShape." + x)
                                    .ToArray();

                                var sampler = animation.samplers[channel.sampler];
                                var input = importer.gltf.GetArrayFromAccessor<float>(sampler.input);
                                var output = importer.gltf.GetArrayFromAccessor<float>(sampler.output);
                                AnimationImporter.SetAnimationCurve(
                                    clip,
                                    relativePath,
                                    keyNames,
                                    input,
                                    output,
                                    sampler.interpolation,
                                    typeof(SkinnedMeshRenderer),
                                    (values, last) =>
                                    {
                                        for (int j = 0; j < values.Length; j++)
                                        {
                                            values[j] *= 100.0f;
                                        }
                                        return values;
                                    });
                                
                            }
                            break;

                        default:
                            Debug.LogWarningFormat("unknown path: {0}", channel.target.path);
                            break;
                    }
                }
                animasionClips.Add(clip);
            }

            return animasionClips;
        }

        public static void ImportAnimation(GLTFImporter importer)
        {
            Debug.Log("import animations "+importer.gltf.animations?.Count);
            // animation
            if (importer.gltf.animations != null && importer.gltf.animations.Any())
            {
                var animation = importer.root.AddComponent<Animation>();
                importer.AnimationClips = ImportAnimationClip(importer);
                foreach (var clip in importer.AnimationClips)
                {
                    animation.AddClip(clip, clip.name);
                }
                if (importer.AnimationClips.Count > 0)
                {
                    animation.clip = importer.AnimationClips.First();
                }
            }
        }
    }
}
