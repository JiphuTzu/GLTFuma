using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
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
    public class WebLoader : MonoBehaviour
    {
        public Text text;
        public string url = "http://47.92.208.125:8080/files/BrainStem/BrainStem.gltf";
        public bool loadOnStart = false;
		private ImporterContext _loader;
        private void Start()
        {
            if (loadOnStart && !string.IsNullOrEmpty(url))
            {
                Load(url, p => text.text = $"{p * 100:f2}%", null);
            }
        }
		public void Load(string url, Action<float> progress, Action<GameObject> complete){
			this.url = url;
			StartCoroutine(Load(progress,complete));
		}
        public IEnumerator Load(Action<float> progress, Action<GameObject> complete)
        {
            var loader = new ImporterContext();
            var www = UnityWebRequest.Get(url);
            var ao = www.SendWebRequest();
            while (!www.isDone)
            {
                progress?.Invoke(ao.progress * 0.1f);
            }
            Debug.Log(www.downloadHandler.text);
            loader.ParseJson(www.downloadHandler.text);
            var storage = new WebStorage(url.Substring(0, url.LastIndexOf("/")));
            int total = loader.GLTF.buffers.Count + loader.GLTF.images.Count;
            int current = 0;
            foreach (var buffer in loader.GLTF.buffers)
            {
                Debug.Log(buffer.uri);
                yield return storage.Load(buffer.uri, p => progress?.Invoke(0.1f + 0.8f * current / total + p / total));
                Debug.Log(buffer.uri + " loaded");
                buffer.OpenStorage(storage);
                current++;
            }
            foreach (var image in loader.GLTF.images)
            {
                yield return storage.Load(image.uri, p => progress?.Invoke(0.1f + 0.8f * current / total + p / total));
                current++;
            }
            yield return loader.Load(storage, p => progress?.Invoke(0.9f + p * 0.1f));
            //loader.Parse(url,www.downloadHandler.data);
            loader.ShowMeshes();
            loader.Root.SetActive(false);
			loader.Root.transform.SetParent(transform);
            loader.Root.SetActive(true);
            complete?.Invoke(loader.Root);
        }
		public void Unload(){
			if(_loader==null) return;
			_loader.Dispose();
			_loader = null;
		}
    }
}