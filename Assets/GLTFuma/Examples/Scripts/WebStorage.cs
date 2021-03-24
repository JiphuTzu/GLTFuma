using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UMa.GLTF;
using UnityEngine;
using UnityEngine.Networking;
//============================================================
//支持中文，文件使用UTF-8编码
//@author	JiphuTzu
//@create	#CREATEDATE#
//@company	#COMPANY#
//
//@description:
//============================================================
namespace UMa
{
    public class WebStorage : IStorage
    {
		private string _root;
		private Dictionary<string,byte[]> _data;
		public WebStorage(string root){
			_root = root;
			Debug.Log("web storage root = "+_root);
			_data = new Dictionary<string, byte[]>();
		}
		public IEnumerator Load(string url,Action<float> progress){
			var path = GetPath(url);
			Debug.Log("load path = "+path);
			var www = UnityWebRequest.Get(path);
            var ao = www.SendWebRequest();
            while (!www.isDone)
			{
				progress.Invoke(ao.progress);
				yield return null;
			}
			_data.Add(url,www.downloadHandler.data);
			// var s = "storage add \n";
			// foreach (var item in _data)
			// {
			// 	s+= item.Key+" = "+item.Value.Length+"\n";
			// }
			// Debug.Log(s);

		}
        public ArraySegment<byte> Get(string url)
        {
            var bytes =
                (url.StartsWith("data:"))
                ? url.ReadEmbeded()
                : _data[url]
                ;
			Debug.Log("get storage ... "+url + " ======= "+bytes.Length);
            return new ArraySegment<byte>(bytes);
        }

        public string GetPath(string url)
        {
            if (url.StartsWith("data:"))
            {
                return null;
            }
            else
            {
                return Path.Combine(_root, url).Replace("\\", "/");
            }
        }
    }
}
