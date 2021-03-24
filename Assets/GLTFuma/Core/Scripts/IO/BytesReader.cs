using System;
using System.Runtime.InteropServices;
using System.Text;


namespace UMa.GLTF
{
    public class BytesReader
    {
        private byte[] _bytes;
        private int _pos;

        public BytesReader(byte[] bytes, int pos=0)
        {
            _bytes = bytes;
            _pos = pos;
        }

        public string ReadString(int count, Encoding encoding)
        {
            var s = encoding.GetString(_bytes, _pos, count);
            _pos += count;
            return s;
        }

        public float ReadSingle()
        {
            var n = BitConverter.ToSingle(_bytes, _pos);
            _pos += 4;
            return n;
        }

        public byte ReadUInt8()
        {
            return _bytes[_pos++];
        }

        public UInt16 ReadUInt16()
        {
            var n = BitConverter.ToUInt16(_bytes, _pos);
            _pos += 2;
            return n;
        }

        public sbyte ReadInt8()
        {
            return (sbyte)_bytes[_pos++];
        }

        public Int16 ReadInt16()
        {
            var n = BitConverter.ToInt16(_bytes, _pos);
            _pos += 2;
            return n;
        }

        public int ReadInt32()
        {
            var n = BitConverter.ToInt32(_bytes, _pos);
            _pos += 4;
            return n;
        }

        public void ReadToArray<T>(T[] dst) where T : struct
        {
            var size = new ArraySegment<Byte>(_bytes, _pos, _bytes.Length - _pos).MarshalCoyTo(dst);
            _pos += size;
        }

        public T ReadStruct<T>() where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            using (var pin = new ArraySegment<Byte>(_bytes, _pos, _bytes.Length - _pos).ToPin())
            {
                var s = (T)Marshal.PtrToStructure(pin.Ptr, typeof(T));
                _pos += size;
                return s;
            }
        }
    }
}
