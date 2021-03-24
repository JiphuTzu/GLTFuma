using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UMa.GLTF
{
    class AnimationCurveData
    {
#if UNITY_EDITOR
        public AnimationUtility.TangentMode tangentMode { get; private set; }
        public GLTFAnimationTarget.AnimationProperty animationProperty { get; private set; }
        public int samplerIndex { get; private set; }
        public int elementCount { get; private set; }
        public readonly List<AnimationKeyframeData> Keyframes = new List<AnimationKeyframeData>();

        public AnimationCurveData(AnimationUtility.TangentMode tangentMode, GLTFAnimationTarget.AnimationProperty property, int samplerIndex, int elementCount)
        {
            this.tangentMode = tangentMode;
            animationProperty = property;
            this.samplerIndex = samplerIndex;
            this.elementCount = elementCount;
        }

        public string GetInterpolation()
        {
            switch (tangentMode)
            {
                case AnimationUtility.TangentMode.Linear:
                    return GLTFAnimationTarget.Interpolation.LINEAR.ToString();
                case AnimationUtility.TangentMode.Constant:
                    return GLTFAnimationTarget.Interpolation.STEP.ToString();
                default:
                    return GLTFAnimationTarget.Interpolation.LINEAR.ToString();
            }
        }

        /// <summary>
        /// キーフレームのデータを入力する
        /// </summary>
        /// <param name="time"></param>
        /// <param name="value"></param>
        /// <param name="valueOffset"></param>
        public void SetKeyframeData(float time, float value, int valueOffset)
        {
            var existKeyframe = Keyframes.FirstOrDefault(x => x.time == time);
            if (existKeyframe != null)
            {
                existKeyframe.SetValue(value, valueOffset);
            }
            else
            {
                var newKeyframe = GetKeyframeData(animationProperty, elementCount);
                newKeyframe.time = time;
                newKeyframe.SetValue(value, valueOffset);
                Keyframes.Add(newKeyframe);
            }
        }

        /// <summary>
        /// キー情報がなかった要素に対して直前のキーの値を入力する
        /// </summary>
        public void RecountEmptyKeyframe()
        {
            if (Keyframes.Count == 0)
            {
                return;
            }

            Keyframes.Sort((x, y) => (x.time < y.time) ? -1 : 1);

            for (int i = 1; i < Keyframes.Count; i++)
            {
                var current = Keyframes[i];
                var last = Keyframes[i - 1];
                for (int j = 0; j < current.EnterValues.Length; j++)
                {
                    if (!current.EnterValues[j])
                    {
                        Keyframes[i].SetValue(last.Values[j], j);
                    }
                }

            }
        }

        /// <summary>
        /// アニメーションプロパティに対応したキーフレームを挿入する
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static AnimationKeyframeData GetKeyframeData(GLTFAnimationTarget.AnimationProperty property, int elementCount)
        {
            switch (property)
            {
                case GLTFAnimationTarget.AnimationProperty.Translation:
                    return new AnimationKeyframeData(elementCount, (values) =>
                    {
                        var temp = new Vector3(values[0], values[1], values[2]);
                        return temp.ReverseZ().ToArray();
                    });
                case GLTFAnimationTarget.AnimationProperty.Rotation:
                    return new AnimationKeyframeData(elementCount, (values) =>
                    {
                        var temp = new Quaternion(values[0], values[1], values[2], values[3]);
                        return temp.ReverseZ().ToArray();
                    });
                case GLTFAnimationTarget.AnimationProperty.Scale:
                    return new AnimationKeyframeData(elementCount, null);
                case GLTFAnimationTarget.AnimationProperty.BlendShape:
                    return new AnimationKeyframeData(elementCount, null);
                default:
                    return null;
            }
        }
#endif
    }
}