using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace UMa.GLTF
{
    public interface IStorage : IDisposable
    {
        ArraySegment<Byte> GetBinary(string url);
        string GetString(string url);
        Task<ArraySegment<byte>> LoadBinary(string url, Action<float> progress);
        Task<string> LoadString(string url, Action<float> progress);
        Task<Texture2D> LoadTexture(string url, Action<float> progress);
        /// <summary>
        /// Get original filepath if exists
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        string GetPath(string url);
    }

    public class SimpleStorage : IStorage
    {
        private ArraySegment<Byte> _bytes;

        public SimpleStorage() : this(new ArraySegment<byte>()) { }

        public SimpleStorage(ArraySegment<Byte> bytes)
        {
            _bytes = bytes;
        }

        public ArraySegment<byte> GetBinary(string url)
        {
            return _bytes;
        }
        public async Task<ArraySegment<byte>> LoadBinary(string url, Action<float> progress)
        {
            await Task.Yield();
            return _bytes;
        }
        public async Task<Texture2D> LoadTexture(string url, Action<float> progress)
        {
            await Task.Yield();
            return null;
        }

        public string GetPath(string url)
        {
            return null;
        }

        public void Dispose()
        {
        }

        public string GetString(string url)
        {
            return string.Empty;
        }

        public async Task<string> LoadString(string url, Action<float> progress)
        {
            await Task.Yield();
            return string.Empty;
        }
    }

    public class FileSystemStorage : IStorage
    {
        private string m_root;
        private Dictionary<string, ArraySegment<byte>> _data;

        public FileSystemStorage(string root)
        {
            m_root = Path.GetFullPath(root);
            _data = new Dictionary<string, ArraySegment<byte>>();
        }

        public ArraySegment<byte> GetBinary(string url)
        {
            if (!_data.ContainsKey(url))
            {
                var bytes = (url.StartsWith("data:"))
                ? url.ReadEmbeded()
                : File.ReadAllBytes(Path.Combine(m_root, url));
                _data.Add(url, new ArraySegment<byte>(bytes));
            }

            return _data[url];
        }
        public async Task<ArraySegment<byte>> LoadBinary(string url, Action<float> progress)
        {
            var data = GetBinary(url);
            await Task.Yield();
            return data;
        }
        public async Task<Texture2D> LoadTexture(string url, Action<float> progress)
        {
            await Task.Yield();
            return null;
        }

        public string GetPath(string url)
        {
            if (url.StartsWith("data:")) return null;
            return Path.Combine(m_root, url).Replace("\\", "/");
        }

        public void Dispose()
        {
            var keys = new string[_data.Count];
            _data.Keys.CopyTo(keys, 0);
            for (int i = 0; i < keys.Length; i++)
            {
                _data.Remove(keys[i]);
            }
            _data = null;
        }

        public string GetString(string url)
        {
            return string.Empty;
        }

        public async Task<string> LoadString(string url, Action<float> progress)
        {
            await Task.Yield();
            return string.Empty;
        }
    }
}