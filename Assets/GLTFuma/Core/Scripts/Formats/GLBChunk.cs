using System;
using System.IO;
using System.Text;

namespace UMa.GLTF
{
    public enum GLBChunkType : UInt32
    {
        JSON = 0x4E4F534A,
        BIN = 0x004E4942,
    }

    public struct GLBHeader
    {
        public static void WriteTo(Stream s)
        {
            s.WriteByte((byte)'g');
            s.WriteByte((byte)'l');
            s.WriteByte((byte)'T');
            s.WriteByte((byte)'F');
            var bytes = BitConverter.GetBytes((UInt32)2);
            s.Write(bytes, 0, bytes.Length);
        }
    }

    public struct GLBChunk
    {
        public GLBChunkType type;
        public ArraySegment<Byte> bytes;

        public GLBChunk(string json) : this(GLBChunkType.JSON, new ArraySegment<byte>(Encoding.UTF8.GetBytes(json))) { }

        public GLBChunk(ArraySegment<Byte> bytes) : this(GLBChunkType.BIN, bytes) { }

        public GLBChunk(GLBChunkType type, ArraySegment<Byte> bytes)
        {
            this.type = type;
            this.bytes = bytes;
        }

        private byte GetPaddingByte()
        {
            // chunk type
            switch (type)
            {
                case GLBChunkType.JSON:
                    return 0x20;

                case GLBChunkType.BIN:
                    return 0x00;

                default:
                    throw new Exception("unknown chunk type: " + type);
            }
        }

        public int WriteTo(Stream s)
        {
            // padding
            var paddingValue = this.bytes.Count % 4;
            var padding = (paddingValue > 0) ? 4 - paddingValue : 0;

            // size
            var bytes = BitConverter.GetBytes((int)(this.bytes.Count + padding));
            s.Write(bytes, 0, bytes.Length);

            // chunk type
            switch (type)
            {
                case GLBChunkType.JSON:
                    s.WriteByte((byte)'J');
                    s.WriteByte((byte)'S');
                    s.WriteByte((byte)'O');
                    s.WriteByte((byte)'N');
                    break;

                case GLBChunkType.BIN:
                    s.WriteByte((byte)'B');
                    s.WriteByte((byte)'I');
                    s.WriteByte((byte)'N');
                    s.WriteByte((byte)0);
                    break;

                default:
                    throw new Exception("unknown chunk type: " + type);
            }

            // body
            s.Write(this.bytes.Array, this.bytes.Offset, this.bytes.Count);

            // 4byte align
            var pad = GetPaddingByte();
            for (int i = 0; i < padding; ++i)
            {
                s.WriteByte(pad);
            }

            return 4 + 4 + this.bytes.Count + padding;
        }
    }
}