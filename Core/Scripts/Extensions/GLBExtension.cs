using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;


namespace UMa.GLTF
{
    public static class GLBExtension
    {
        public const string GLB_MAGIC = "glTF";
        public const float GLB_VERSION = 2.0f;

        public static GLBChunkType ToChunkType(this string src)
        {
            switch(src)
            {
                case "BIN":
                    return GLBChunkType.BIN;

                case "JSON":
                    return GLBChunkType.JSON;

                default:
                    throw new FormatException("unknown chunk type: " + src);
            }
        }

        public static List<GLBChunk> ToGLBChunks(this byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                throw new Exception("empty bytes");
            }

            int pos = 0;
            if (Encoding.ASCII.GetString(bytes, 0, 4) != GLB_MAGIC)
            {
                throw new Exception("invalid magic");
            }
            pos += 4;

            var version = BitConverter.ToUInt32(bytes, pos);
            if (version != GLB_VERSION)
            {
                Debug.LogWarningFormat("unknown version: {0}", version);
                return null;
            }
            pos += 4;

            //var totalLength = BitConverter.ToUInt32(bytes, pos);
            pos += 4;

            var chunks = new List<GLBChunk>();
            while (pos < bytes.Length)
            {
                var chunkDataSize = BitConverter.ToInt32(bytes, pos);
                pos += 4;

                //var type = (GlbChunkType)BitConverter.ToUInt32(bytes, pos);
                var chunkTypeBytes = bytes.Skip(pos).Take(4).Where(x => x != 0).ToArray();
                var chunkTypeStr = Encoding.ASCII.GetString(chunkTypeBytes);
                var type = chunkTypeStr.ToChunkType();
                pos += 4;

                chunks.Add(new GLBChunk(type,new ArraySegment<byte>(bytes, (int)pos, (int)chunkDataSize)));

                pos += chunkDataSize;
            }

            return chunks;
        }
        public static byte[] Join(this string json, ArraySegment<byte> body)
        {
            using (var s = new MemoryStream())
            {
                GLBHeader.WriteTo(s);

                var pos = s.Position;
                s.Position += 4; // skip total size

                int size = 12;

                {
                    var chunk = new GLBChunk(json);
                    size += chunk.WriteTo(s);
                }
                {
                    var chunk = new GLBChunk(body);
                    size += chunk.WriteTo(s);
                }

                s.Position = pos;
                var bytes = BitConverter.GetBytes(size);
                s.Write(bytes, 0, bytes.Length);

                return s.ToArray();
            }
        }
    }
}
