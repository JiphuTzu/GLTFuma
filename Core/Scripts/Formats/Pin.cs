using System;
using System.Runtime.InteropServices;
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
    public class Pin<T> : IDisposable
        where T : struct
    {
        private GCHandle _pinnedArray;

        private ArraySegment<T> _source;

        public int length
        {
            get
            {
                return _source.Count * Marshal.SizeOf(typeof(T));
            }
        }

        public Pin(ArraySegment<T> src)
        {
            _source = src;
            _pinnedArray = GCHandle.Alloc(src.Array, GCHandleType.Pinned);
        }

        public IntPtr Ptr
        {
            get
            {
                var ptr = _pinnedArray.AddrOfPinnedObject();
                return new IntPtr(ptr.ToInt64() + _source.Offset);
            }
        }

        #region IDisposable Support
        private bool _disposed = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。
                if (_pinnedArray.IsAllocated)
                {
                    _pinnedArray.Free();
                }

                _disposed = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~Pin() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
