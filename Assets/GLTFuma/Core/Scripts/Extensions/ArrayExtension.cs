using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace UMa.GLTF
{
    public static class ArrayExtension
    {
        public static int MarshalCoyTo<T>(this ArraySegment<byte> src, T[] dst) where T : struct
        {
            var size = dst.Length * Marshal.SizeOf(typeof(T));
            using (var pin = dst.ToPin())
            {
                Marshal.Copy(src.Array, src.Offset, pin.Ptr, size);
            }
            return size;
        }

        public static byte[] ToArray(this ArraySegment<byte> src)
        {
            var dst = new byte[src.Count];
            Array.Copy(src.Array, src.Offset, dst, 0, src.Count);
            return dst;
        }

        public static T[] SelectInplace<T>(this T[] src, Func<T, T> pred)
        {
            for (int i = 0; i < src.Length; ++i)
            {
                src[i] = pred(src[i]);
            }
            return src;
        }
    }
}
