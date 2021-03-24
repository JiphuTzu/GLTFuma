using System.IO;
using UnityEngine;

namespace UMa.GLTF
{
    public static class StringExtension
    {
        public static string ToLowerCamelCase(this string lower)
        {
            return lower.Substring(0, 1).ToLower() + lower.Substring(1);
        }
        public static string ToUpperCamelCase(this string lower)
        {
            return lower.Substring(0, 1).ToUpper() + lower.Substring(1);
        }

        private static string _unityBasePath;
        public static string unityBasePath
        {
            get
            {
                if (_unityBasePath == null)
                {
                    _unityBasePath = Path.GetFullPath(Application.dataPath + "/..").Replace("\\", "/");
                }
                return _unityBasePath;
            }
        }

        public static string AssetPathToFullPath(this string path)
        {
            return unityBasePath + "/" + path;
        }

        public static bool StartsWithUnityAssetPath(this string path)
        {
            return path.Replace("\\", "/").StartsWith(unityBasePath + "/Assets");
        }

        public static string ToUnityRelativePath(this string path)
        {
            path = path.Replace("\\", "/");
            if (path.StartsWith(unityBasePath))
            {
                return path.Substring(unityBasePath.Length + 1);
            }

            //Debug.LogWarningFormat("{0} is starts with {1}", path, basePath);
            return path;
        }

        static readonly char[] escapeChars = new char[]
        {
            '\\',
            '/',
            ':',
            '*',
            '?',
            '"',
            '<',
            '>',
            '|',
        };
        public static string EscapeFilePath(this string path)
        {
            foreach(var x in escapeChars)
            {
                path = path.Replace(x, '+');
            }
            return path;
        }
    }
}
