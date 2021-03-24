using System;
using System.IO;
using System.Runtime.InteropServices;


namespace UMa.GLTF
{
    public interface IBytesBuffer
    {
        string uri { get; }
        ArraySegment<Byte> GetBytes();
        GLTFBufferView Extend<T>(ArraySegment<T> array, GLTFBufferTarget target) where T : struct;
    }

    public static class IBytesBufferExtensions
    {
        public static GLTFBufferView Extend<T>(this IBytesBuffer buffer, T[] array, GLTFBufferTarget target) where T : struct
        {
            return buffer.Extend(new ArraySegment<T>(array), target);
        }
        public static byte[] ReadEmbeded(this string uri)
        {
            var pos = uri.IndexOf(";base64,");
            if (pos < 0)
            {
                throw new NotImplementedException();
            }
            else
            {
                return Convert.FromBase64String(uri.Substring(pos + 8));
            }
        }
    }

    /// <summary>
    /// for buffer with uri read
    /// </summary>
    public class UriByteBuffer : IBytesBuffer
    {
        public string uri
        {
            get;
            private set;
        }

        private byte[] _bytes;
        public ArraySegment<byte> GetBytes()
        {
            return new ArraySegment<byte>(_bytes);
        }

        public UriByteBuffer(string baseDir, string uri)
        {
            this.uri = uri;
            _bytes = ReadFromUri(baseDir, uri);
        }

        const string PREFIX_STREAM = "data:application/octet-stream;base64,";

        const string PREFIX_BUFFER = "data:application/gltf-buffer;base64,";

        const string PREFIX_IMAGE = "data:image/jpeg;base64,";

        

        private byte[] ReadFromUri(string baseDir, string uri)
        {
            var bytes = uri.ReadEmbeded();
            if (bytes != null)
            {
                return bytes;
            }
            else
            {
                // as local file path
                return File.ReadAllBytes(Path.Combine(baseDir, uri));
            }
        }

        public GLTFBufferView Extend<T>(ArraySegment<T> array, GLTFBufferTarget target) where T : struct
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// for glb chunk buffer read
    /// </summary>
    public class ArraySegmentByteBuffer : IBytesBuffer
    {
        ArraySegment<Byte> m_bytes;

        public ArraySegmentByteBuffer(ArraySegment<Byte> bytes)
        {
            m_bytes = bytes;
        }

        public string uri
        {
            get;
            private set;
        }

        public GLTFBufferView Extend<T>(ArraySegment<T> array, GLTFBufferTarget target) where T : struct
        {
            throw new NotImplementedException();
        }

        public ArraySegment<byte> GetBytes()
        {
            return m_bytes;
        }
    }

    /// <summary>
    /// for exporter
    /// </summary>
    public class ArrayByteBuffer : IBytesBuffer
    {
        public string uri
        {
            get;
            private set;
        }

        Byte[] m_bytes;
        int m_used;

        public ArrayByteBuffer(Byte[] bytes = null)
        {
            uri = "";
            m_bytes = bytes;
        }

        public GLTFBufferView Extend<T>(ArraySegment<T> array, GLTFBufferTarget target) where T : struct
        {
            using (var pin = array.ToPin())
            {
                var elementSize = Marshal.SizeOf(typeof(T));
                var view = Extend(pin.Ptr, array.Count * elementSize, elementSize, target);
                return view;
            }
        }

        public GLTFBufferView Extend(IntPtr p, int bytesLength, int stride, GLTFBufferTarget target)
        {
            var tmp = m_bytes;
            // alignment
            var padding = m_used % stride == 0 ? 0 : stride - m_used % stride;

            if (m_bytes == null || m_used + padding + bytesLength > m_bytes.Length)
            {
                // recreate buffer
                m_bytes = new Byte[m_used + padding + bytesLength];
                if (m_used > 0)
                {
                    Buffer.BlockCopy(tmp, 0, m_bytes, 0, m_used);
                }
            }
            if (m_used + padding + bytesLength > m_bytes.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            Marshal.Copy(p, m_bytes, m_used + padding, bytesLength);
            var result=new GLTFBufferView
            {
                buffer = 0,
                byteLength = bytesLength,
                byteOffset = m_used + padding,
                byteStride = stride,
                target = target,
            };
            m_used = m_used + padding + bytesLength;
            return result;
        }

        public ArraySegment<byte> GetBytes()
        {
            if (m_bytes == null)
            {
                return new ArraySegment<byte>();
            }

            return new ArraySegment<byte>(m_bytes, 0, m_used);
        }
    }
}
