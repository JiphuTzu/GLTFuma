using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UMa.GLTF;
using UnityEngine;
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
        //public string url = "http://72studio.jcsureyes.com/presenting/presenting.gltf";
        public string url = "http://47.92.208.125:8080/files/BrainStem/BrainStem.gltf";
        public bool loadOnStart = false;
        private GLTFImporter _loader;
        private void Start()
        {
            if (loadOnStart && !string.IsNullOrEmpty(url))
            {
                var storage = new WebStorage(url.Substring(0, url.LastIndexOf("/")));
                Load(url,storage, p => text.text = $"{p * 100:f2}%", null);
            }
        }
        public void Load(string url,WebStorage storage, Action<float> progress, Action<GameObject> complete)
        {
            this.url = url;
            var res = Load(storage,progress, complete);
        }
        public async Task Load(WebStorage storage,Action<float> progress, Action<GameObject> complete)
        {
            var name = url.Substring(url.LastIndexOf("/")+1);
            //加载.gltf文件
            await storage.Load(name,p=>progress?.Invoke(p*0.1f));

            _loader = new GLTFImporter();
            //Debug.Log(www.downloadHandler.text);
            //用JsonUtility解析到gltf数据
            _loader.ParseJson(Encoding.UTF8.GetString(storage.Get(name).ToArray()));
            //加载buffers里面的.bin数据
            int total = _loader.gltf.buffers.Count;
            int current = 0;
            foreach (var buffer in _loader.gltf.buffers)
            {
                Debug.Log(buffer.uri);
                await storage.Load(buffer.uri, p => progress?.Invoke(0.1f + 0.8f * (current + p) / total));
                //Debug.Log(buffer.uri + " loaded");
                buffer.OpenStorage(storage);
                current++;
            }
            //跳过图片的加载
            //total = _loader.GLTF.images.Count;
            // foreach (var image in _loader.GLTF.images)
            // {
            //     yield return storage.Load(image.uri, p => progress?.Invoke(0.5f + 0.4f * (current + p) / total));
            //     current++;
            // }
            //解析mesh、material、animation等数据
            await _loader.Load(storage, p => progress?.Invoke(0.9f + p * 0.1f));
            //loader.Parse(url,www.downloadHandler.data);
            _loader.ShowMeshes();
            _loader.root.SetActive(false);
            _loader.root.transform.SetParent(transform);
            _loader.root.SetActive(true);
            complete?.Invoke(_loader.root);
        }
        public void Unload()
        {
            StopAllCoroutines();
            if (_loader == null) return;
            _loader.Dispose();
            _loader = null;
        }
    }
}