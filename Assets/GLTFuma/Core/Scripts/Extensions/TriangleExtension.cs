using System;
using System.Linq;
using System.Collections.Generic;

namespace UMa.GLTF
{
    public static class TriangleExtension
    {
        public static IEnumerable<int> FlipTriangle(this IEnumerable<Byte> src)
        {
            return src.Select(x => (Int32)x).FlipTriangle();
        }

        public static IEnumerable<int> FlipTriangle(this IEnumerable<UInt16> src)
        {
            return src.Select(x => (Int32)x).FlipTriangle();
        }

        public static IEnumerable<int> FlipTriangle(this IEnumerable<UInt32> src)
        {
            return src.Select(x => (Int32)x).FlipTriangle();
        }

        public static IEnumerable<int> FlipTriangle(this IEnumerable<Int32> src)
        {
            var it = src.GetEnumerator();
            while (true)
            {
                if (!it.MoveNext())
                    yield break;
                var i0 = it.Current;
                if (!it.MoveNext())
                    yield break;

                var i1 = it.Current;
                if (!it.MoveNext())
                    yield break;

                var i2 = it.Current;

                yield return i2;
                yield return i1;
                yield return i0;
            }
        }
    }
}