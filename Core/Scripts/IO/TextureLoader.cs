using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UMa.GLTF
{
    public interface ITextureLoader : IDisposable
    {
        void Load(Action complete);
        Texture2D texture { get; }

        /// <summary>
        /// Call from any thread
        /// </summary>
        /// <param name="gltf"></param>
        /// <param name="storage"></param>
        void ProcessOnAnyThread(GLTFRoot gltf, IStorage storage);

        /// <summary>
        /// Call from unity main thread
        /// </summary>
        /// <param name="isLinear"></param>
        /// <returns></returns>
        IEnumerator ProcessOnMainThread(bool isLinear);
    }

#if UNITY_EDITOR
    public class AssetTextureLoader : ITextureLoader
    {
        public Texture2D texture { get; private set; }

        private UnityPath _assetPath;

        public AssetTextureLoader(UnityPath assetPath, string _)
        {
            _assetPath = assetPath;
        }

        public void Dispose()
        {
        }
        public void Load(Action complete)
        {
            complete.Invoke();
        }

        public void ProcessOnAnyThread(GLTFRoot gltf, IStorage storage)
        {
        }

        public IEnumerator ProcessOnMainThread(bool isLinear)
        {
            //
            // texture from assets
            //
            _assetPath.ImportAsset();
            var importer = _assetPath.GetImporter<TextureImporter>();
            if (importer == null)
            {
                Debug.LogWarningFormat("fail to get TextureImporter: {0}", _assetPath);
            }
            importer.sRGBTexture = !isLinear;
            importer.SaveAndReimport();

            texture = _assetPath.LoadAsset<Texture2D>();
            //Texture.name = m_textureName;
            if (texture == null)
            {
                Debug.LogWarningFormat("fail to Load Texture2D: {0}", _assetPath);
            }

            yield break;
        }
    }
#endif

    public class TextureLoader : ITextureLoader
    {
        int m_textureIndex;
        public TextureLoader(int textureIndex)
        {
            m_textureIndex = textureIndex;
        }

        public Texture2D texture
        {
            private set;
            get;
        }

        public void Dispose()
        {
        }
        public void Load(Action complete)
        {
            complete.Invoke();
        }

        static Byte[] ToArray(ArraySegment<byte> bytes)
        {
            if (bytes.Array == null)
            {
                return new byte[] { };
            }
            else if (bytes.Offset == 0 && bytes.Count == bytes.Array.Length)
            {
                return bytes.Array;
            }
            else
            {
                Byte[] result = new byte[bytes.Count];
                Buffer.BlockCopy(bytes.Array, bytes.Offset, result, 0, result.Length);
                return result;
            }
        }

        Byte[] m_imageBytes;
        string m_textureName;
        public void ProcessOnAnyThread(GLTFRoot gltf, IStorage storage)
        {
            var imageIndex = gltf.GetImageIndexFromTextureIndex(m_textureIndex);
            var segments = gltf.GetImageBytes(storage, imageIndex, out m_textureName, out var url);
            m_imageBytes = ToArray(segments);
        }

        public IEnumerator ProcessOnMainThread(bool isLinear)
        {
            //
            // texture from image(png etc) bytes
            //
            texture = new Texture2D(2, 2, TextureFormat.ARGB32, false, isLinear);
            texture.name = m_textureName;
            if (m_imageBytes != null)
            {
                texture.LoadImage(m_imageBytes);
            }
            yield break;
        }
    }

    public class UnityWebRequestTextureLoader : ITextureLoader
    {
        public Texture2D texture
        {
            private set;
            get;
        }

        int m_textureIndex;

        public UnityWebRequestTextureLoader(int textureIndex)
        {
            m_textureIndex = textureIndex;
        }

        UnityWebRequest m_uwr;
        public void Dispose()
        {
            if (m_uwr != null)
            {
                m_uwr.Dispose();
                m_uwr = null;
            }
        }

        ArraySegment<Byte> m_segments;
        string m_textureName;
        private string url;
        public void ProcessOnAnyThread(GLTFRoot gltf, IStorage storage)
        {
            var imageIndex = gltf.GetImageIndexFromTextureIndex(m_textureIndex);
            m_segments = gltf.GetImageBytes(storage, imageIndex, out m_textureName, out url);
        }

#if false
        HttpHost m_http;
        class HttpHost : IDisposable
        {
            TcpListener m_listener;
            Socket m_connection;

            public HttpHost(int port)
            {
                m_listener = new TcpListener(IPAddress.Loopback, port);
                m_listener.Start();
                m_listener.BeginAcceptSocket(OnAccepted, m_listener);
            }

            void OnAccepted(IAsyncResult ar)
            {
                var l = ar.AsyncState as TcpListener;
                if (l == null) return;
                m_connection = l.EndAcceptSocket(ar);
                // 次の接続受付はしない

                BeginRead(m_connection, new byte[8192]);
            }

            void BeginRead(Socket c, byte[] buffer)
            {
                AsyncCallback callback = ar =>
                {
                    var s = ar.AsyncState as Socket;
                    if (s == null) return;
                    var size = s.EndReceive(ar);
                    if (size > 0)
                    {
                        OnRead(buffer, size);
                    }
                    BeginRead(s, buffer);
                };
                m_connection.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, callback, m_connection);
            }

            List<Byte> m_buffer = new List<byte>();
            void OnRead(byte[] buffer, int len)
            {
                m_buffer.AddRange(buffer.Take(len));
            }

            public string Url
            {
                get
                {

                }
            }

            public void Dispose()
            {
                if (m_connection != null)
                {
                    m_connection.Dispose();
                    m_connection = null;
                }
                if(m_listener != null)
                {
                    m_listener.Stop();
                    m_listener = null;
                }
            }
        }
#endif

        class Deleter : IDisposable
        {
            string m_path;
            public Deleter(string path)
            {
                m_path = path;
            }
            public void Dispose()
            {
                if (File.Exists(m_path))
                {
                    File.Delete(m_path);
                }
            }
        }

        public IEnumerator ProcessOnMainThread(bool isLinear)
        {
            var tex = new Texture2D(2, 2);
            if (string.IsNullOrEmpty(url))
            {
                tex.LoadImage(m_segments.ToArray());
                tex.Apply();
                texture = tex;
                texture.name = m_textureName;
            }
            yield return null;
        }
        public void Load(Action complete)
        {
            if (string.IsNullOrEmpty(url)) return;
            Debug.LogFormat("UnityWebRequest: {0}", url);
            var res = StartLoad(complete);
        }
        private async Task<bool> StartLoad(Action complete)
        {
            var www = UnityWebRequestTexture.GetTexture(url);
            Debug.Log("load texture ... " + url);
            var ao = www.SendWebRequest();
            // wait for request
            while (!www.isDone)
            {
                await Task.Yield();
            }

            if (string.IsNullOrEmpty(www.error))
            {
                // Get downloaded asset bundle
                texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                texture.name = m_textureName;
                complete.Invoke();
                return true;
            }
            else
            {
                Debug.Log(www.error);
                return false;
            }
        }
    }
}