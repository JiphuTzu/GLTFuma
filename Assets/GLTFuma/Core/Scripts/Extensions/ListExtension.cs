using System;
using System.Collections.Generic;
using System.Linq;
//============================================================
//支持中文，文件使用UTF-8编码
//@author	JiphuTzu
//@create	20210323
//@company	UMa
//
//@description:
//============================================================
namespace UMa.GLTF
{
    public static class ListExtensions
    {
        public static void Assign<T>(this List<T> dst, T[] src, Func<T, T> pred)
        {
            dst.Capacity = src.Length;
            dst.AddRange(src.Select(pred));
        }
    }
}
