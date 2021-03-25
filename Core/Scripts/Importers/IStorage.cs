﻿using System;
using System.IO;
using UnityEngine;

namespace UMa.GLTF
{
    public interface IStorage
    {
        ArraySegment<Byte> Get(string url);
        void Load(string url,Action<string> complete);
        /// <summary>
        /// Get original filepath if exists
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        string GetPath(string url);
    }

    public class SimpleStorage : IStorage
    {
        ArraySegment<Byte> m_bytes;

        public SimpleStorage():this(new ArraySegment<byte>())
        {
        }

        public SimpleStorage(ArraySegment<Byte> bytes)
        {
            m_bytes = bytes;
        }

        public ArraySegment<byte> Get(string url)
        {
            return m_bytes;
        }
        public void Load(string url,Action<string> complete){
            complete.Invoke(url);
        }

        public string GetPath(string url)
        {
            return null;
        }
    }

    public class FileSystemStorage : IStorage
    {
        string m_root;

        public FileSystemStorage(string root)
        {
            m_root = Path.GetFullPath(root);
        }

        public ArraySegment<byte> Get(string url)
        {
            Debug.Log(m_root+"---> "+url);
            var bytes =
                (url.StartsWith("data:"))
                ? url.ReadEmbeded()
                : File.ReadAllBytes(Path.Combine(m_root, url))
                ;
            return new ArraySegment<byte>(bytes);
        }
        public void Load(string url,Action<string> complete){
            complete.Invoke(url);
        }

        public string GetPath(string url)
        {
            if (url.StartsWith("data:"))
            {
                return null;
            }
            else
            {
                return Path.Combine(m_root, url).Replace("\\", "/");
            }
        }
    }
}