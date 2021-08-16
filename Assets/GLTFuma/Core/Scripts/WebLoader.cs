using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
//============================================================
//支持中文，文件使用UTF-8编码
//@author	JiphuTzu
//@create	20210323
//@company	UMa
//
//@description:
//============================================================
namespace UMa.GLTF
{
    public class WebLoader : MonoBehaviour,IShaderStore
    {
        [Serializable]
        public class ProgressEvent : UnityEvent<float> { }
        [Serializable]
        public class CompleteEvent : UnityEvent<GameObject> { }
        //http://72studio.jcsureyes.com/202103241648111/Baseball.gltf
        //public string url = "http://72studio.jcsureyes.com/presenting/presenting.gltf";
        public string url;
        public bool loadOnStart = false;
        public bool showWithTexture = false;
        public string defaultShaderName = "Standard";
        public ProgressEvent onProgress = new ProgressEvent();
        public CompleteEvent onComplete = new CompleteEvent();
        private GLTFImporter _loader;
        private IStorage _storage;
        private bool _unloaded = false;
        private void Start()
        {
            if (loadOnStart && !string.IsNullOrEmpty(url))
            {
                var storage = new WebStorage(url.Substring(0, url.LastIndexOf("/")));
                Load(url, storage);
            }
        }
        public void Load(string url, IStorage storage)
        {
            this.url = url;
            _storage = storage;
            var task = Load(onProgress.Invoke);
            task.GetAwaiter().OnCompleted(() =>
            {
                if (task.Result != null)
                    onComplete.Invoke(task.Result);
            });
        }
        [Obsolete("Will remove at v0.4.0")]
        public void Load(string url, IStorage storage, Action<float> progress, Action<GameObject> complete)
        {
            this.url = url;
            _storage = storage;
            var task = Load(progress);
            task.GetAwaiter().OnCompleted(() =>
            {
                if (task.Result != null && complete != null)
                    complete.Invoke(task.Result);
            });
        }
        private async Task<GameObject> Load(Action<float> progress)
        {
            await Task.Yield();
            _unloaded = false;
            var name = url.Substring(url.LastIndexOf("/") + 1);
            //加载.gltf文件
            await _storage.LoadString(name, p => progress?.Invoke(p * 0.1f));
            if (_unloaded) return null;
            _loader = new GLTFImporter(this,null,null,null,null);
            //用JsonUtility解析到gltf数据
            _loader.ParseJson(_storage.GetString(name));
            //加载buffers里面的.bin数据
            int total = _loader.gltf.buffers.Count;
            int current = 0;
            var stepPrecent = showWithTexture ? 0.4f : 0.8f;
            foreach (var buffer in _loader.gltf.buffers)
            {
                Debug.Log(buffer.uri);
                await _storage.LoadBinary(buffer.uri, p => progress?.Invoke(0.1f + stepPrecent * (current + p) / total));
                if (_unloaded) return null;
                //Debug.Log(buffer.uri + " loaded");
                buffer.OpenStorage(_storage);
                current++;
            }
            //跳过图片的加载
            if (showWithTexture)
            {
                current = 0;
                total = _loader.gltf.images.Count;
                foreach (var image in _loader.gltf.images)
                {
                    await _storage.LoadTexture(image.uri, p => progress?.Invoke(0.5f + 0.4f * (current + p) / total));
                    current++;
                    if (_unloaded) return null;
                }
            }
            //解析mesh、material、animation等数据
            await _loader.Load(_storage, p => progress?.Invoke(0.9f + p * 0.1f));
            if (_unloaded) return null;
            //loader.Parse(url,www.downloadHandler.data);
            _loader.ShowMeshes();
            _loader.root.SetActive(false);
            _loader.root.transform.SetParent(transform);
            _loader.root.SetActive(true);
            return _loader.root;
        }
        public void Unload()
        {
            _unloaded = true;
            if (_loader != null)
            {
                _loader.Dispose();
                _loader = null;
            }
            if (_storage != null)
            {
                _storage.Dispose();
                _storage = null;
            }
        }
        private Shader _defaultShader;
        private Shader defaultShader
        {
            get
            {
                if (_defaultShader == null) _defaultShader = Shader.Find(defaultShaderName);
                return _defaultShader;
            }
        }
        private Shader _uniUnlitShader;
        private Shader uniUnlitShader
        {
            get
            {
                if (_uniUnlitShader == null) _uniUnlitShader = Shader.Find("UniGLTF/UniUnlit");
                return _uniUnlitShader;
            }
        }
        Shader IShaderStore.GetShader(GLTFMaterial material)
        {
            if (material == null)
            {
                return defaultShader;
            }

            if (material.extensions != null && material.extensions.KHR_materials_unlit != null)
            {
                return uniUnlitShader;
            }

            // standard
            return defaultShader;
        }
    }
}