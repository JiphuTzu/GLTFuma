using System;
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
    public static class Pin
    {
        public static Pin<T> ToPin<T>(this ArraySegment<T> src) where T : struct
        {
            return new Pin<T>(src);
        }
        public static Pin<T> ToPin<T>(this T[] src) where T : struct
        {
            return new ArraySegment<T>(src).ToPin();
        }
    }
}
