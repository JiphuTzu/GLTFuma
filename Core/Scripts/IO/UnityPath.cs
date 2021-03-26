using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UMa.GLTF
{
    /// <summary>
    /// relative path from Unity project root.
    /// For AssetDatabase.
    /// </summary>
    public struct UnityPath
    {
        #region UnityPath
        public string value{get;private set;}
        public string fileName
        {
            get { return Path.GetFileName(value); }
        }

        public override string ToString()
        {
            return string.Format("unity://{0}", value);
        }

        public bool isNull
        {
            get { return value == null; }
        }

        public bool isUnderAssetsFolder
        {
            get
            {
                if (isNull) return false;
                return value == "Assets" || value.StartsWith("Assets/");
            }
        }

        public string fileNameWithoutExtension
        {
            get { return Path.GetFileNameWithoutExtension(value); }
        }

        public string extension
        {
            get { return Path.GetExtension(value); }
        }

        public UnityPath parent
        {
            get
            {
                if (isNull) return default(UnityPath);

                return new UnityPath(Path.GetDirectoryName(value));
            }
        }

        public bool hasParent
        {
            get
            {
                return !string.IsNullOrEmpty(value);
            }
        }

        static readonly char[] EscapeChars = new char[]{ '\\', '/', ':', '*', '?', '"', '<', '>', '|', };

        private string EscapeFilePath(string path)
        {
            foreach (var x in EscapeChars)
            {
                path = path.Replace(x, '+');
            }
            return path;
        }

        public UnityPath Child(string name)
        {
            if (isNull)
                throw new NotImplementedException();
            if (string.IsNullOrEmpty(value))
                return new UnityPath(name);
            return new UnityPath(value + "/" + name);
        }

        public override int GetHashCode()
        {
            if (isNull) return 0;

            return value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((obj is UnityPath)) return false;
            var rhs = (UnityPath)obj;
            if (value == null && rhs.value == null) return true;
            if (value == null) return false;
            if (rhs.value == null) return false;
            return value == rhs.value;
        }

        /// <summary>
        /// Remove extension and add suffix
        /// </summary>
        /// <param name="prefabPath"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public UnityPath GetAssetFolder(string suffix)
        {
            if (!isUnderAssetsFolder)
                throw new NotImplementedException();

            return new UnityPath($"{parent.value}/{fileNameWithoutExtension}{suffix}");
        }

        public UnityPath(string value)
        {
            this.value = value.Replace("\\", "/");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="unityPath">Relative from unity current path. GetParent(Application.dataPath)</param>
        /// <returns></returns>
        public static UnityPath FromUnityPath(string unityPath)
        {
            if (string.IsNullOrEmpty(unityPath))
            {
                return new UnityPath("");
            }
            return FromFullpath(Path.GetFullPath(unityPath));
        }
        #endregion

        #region FullPath
        static string _basePath;
        static string baseFullPath
        {
            get
            {
                if (string.IsNullOrEmpty(_basePath))
                {
                    _basePath = Path.GetFullPath(Application.dataPath + "/..").Replace("\\", "/");
                }
                return _basePath;
            }
        }

        static string assetFullPath
        {
            get
            {
                return baseFullPath + "/Assets";
            }
        }

        public string fullPath
        {
            get
            {
                if (isNull)
                {
                    throw new NotImplementedException();
                }
                return Path.Combine(baseFullPath, value).Replace("\\", "/");
            }
        }

        public bool isFileExists
        {
            get { return File.Exists(fullPath); }
        }

        public bool isDirectoryExists
        {
            get { return Directory.Exists(fullPath); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullPath">C:/path/to/file</param>
        /// <returns></returns>
        public static UnityPath FromFullpath(string fullPath)
        {
            if (fullPath == null) fullPath = "";

            fullPath = fullPath.Replace("\\", "/");

            if (fullPath == baseFullPath)
                return new UnityPath("");
            if (fullPath.StartsWith(baseFullPath + "/"))
                return new UnityPath(fullPath.Substring(baseFullPath.Length + 1));
            return default(UnityPath);
        }

        public static bool IsUnderAssetFolder(string fullPath)
        {
            return fullPath.Replace("\\", "/").StartsWith(assetFullPath);
        }
        #endregion

        public IEnumerable<UnityPath> travserseDir()
        {
            if (isDirectoryExists)
            {
                yield return this;

                foreach (var child in childDirs)
                {
                    foreach (var x in child.travserseDir())
                    {
                        yield return x;
                    }
                }
            }
        }

        public IEnumerable<UnityPath> childDirs
        {
            get
            {
                foreach (var x in Directory.GetDirectories(fullPath))
                {
                    yield return UnityPath.FromFullpath(x);
                }
            }
        }

        public IEnumerable<UnityPath> childFiles
        {
            get
            {
                foreach (var x in Directory.GetFiles(fullPath))
                {
                    yield return UnityPath.FromFullpath(x);
                }
            }
        }

#if UNITY_EDITOR
        public T GetImporter<T>() where T : AssetImporter
        {
            return AssetImporter.GetAtPath(value) as T;
        }

        public static UnityPath FromAsset(UnityEngine.Object asset)
        {
            return new UnityPath(AssetDatabase.GetAssetPath(asset));
        }

        public void ImportAsset()
        {
            if (!isUnderAssetsFolder)
            {
                throw new NotImplementedException();
            }
            AssetDatabase.ImportAsset(value);
        }

        public void EnsureFolder()
        {
            if (isNull)
            {
                throw new NotImplementedException();
            }

            if (hasParent)
            {
                parent.EnsureFolder();
            }

            if (!isDirectoryExists)
            {
                var parent = this.parent;
                // ensure parent
                parent.ImportAsset();
                // create
                AssetDatabase.CreateFolder(parent.value, Path.GetFileName(value));
                ImportAsset();
            }
        }

        public UnityEngine.Object[] GetSubAssets()
        {
            if (!isUnderAssetsFolder)
            {
                throw new NotImplementedException();
            }

            return AssetDatabase.LoadAllAssetsAtPath(value);
        }

        public void CreateAsset(UnityEngine.Object o)
        {
            if (!isUnderAssetsFolder)
            {
                throw new NotImplementedException();
            }

            AssetDatabase.CreateAsset(o, value);
        }

        public void AddObjectToAsset(UnityEngine.Object o)
        {
            if (!isUnderAssetsFolder)
            {
                throw new NotImplementedException();
            }

            AssetDatabase.AddObjectToAsset(o, value);
        }

        public T LoadAsset<T>() where T : UnityEngine.Object
        {
            if (!isUnderAssetsFolder)
            {
                throw new NotImplementedException();
            }

            return AssetDatabase.LoadAssetAtPath<T>(value);
        }

        public UnityPath GenerateUniqueAssetPath()
        {
            if (!isUnderAssetsFolder)
            {
                throw new NotImplementedException();
            }

            return new UnityPath(AssetDatabase.GenerateUniqueAssetPath(value));
        }
#endif
    }
}
