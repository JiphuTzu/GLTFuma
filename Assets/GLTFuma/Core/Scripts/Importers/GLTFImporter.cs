using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UMa.GLTF
{
    /// <summary>
    /// GLTF importer
    /// </summary>
    public class GLTFImporter : IDisposable
    {
        #region MeasureTime
        public bool showSpeedLog = false;

        public struct KeyElapsed
        {
            public string key;
            public TimeSpan elapsed;
            public KeyElapsed(string key, TimeSpan elapsed)
            {
                this.key = key;
                this.elapsed = elapsed;
            }
        }

        public struct MeasureScope : IDisposable
        {
            private Action _onDispose;
            public MeasureScope(Action onDispose)
            {
                _onDispose = onDispose;
            }
            public void Dispose()
            {
                _onDispose();
            }
        }

        public List<KeyElapsed> _speedReports = new List<KeyElapsed>();

        public IDisposable MeasureTime(string key)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            return new MeasureScope(() =>
            {
                _speedReports.Add(new KeyElapsed(key, sw.Elapsed));
            });
        }

        public string GetSpeedLog()
        {
            var total = TimeSpan.Zero;

            var sb = new StringBuilder();
            sb.AppendLine("[SpeedLog]");
            foreach (var kv in _speedReports)
            {
                sb.AppendLine($"{kv.key}: {kv.elapsed.TotalMilliseconds:D0}ms");
                total += kv.elapsed;
            }
            sb.AppendLine($"total: { total.TotalMilliseconds:D0}ms");

            return sb.ToString();
        }
        #endregion

        private IShaderStore _shaderStore;
        public IShaderStore shaderStore
        {
            get
            {
                if (_shaderStore == null)
                    _shaderStore = new ShaderStore(this);
                return _shaderStore;
            }
        }

        private IMaterialImporter _materialImporter;
        public IMaterialImporter materialImporter
        {
            get
            {
                if (_materialImporter == null)
                {
                    _materialImporter = new MaterialImporter(shaderStore, this);
                }
                return _materialImporter;
            }
            set
            {
                _materialImporter = value;
            }
        }
        private IMeshImporter _meshImporter;
        private INodeImporter _nodeImporter;
        private IAnimationImporter _animationImporter;

        public GLTFImporter(IShaderStore shaderStore)
        {
            _shaderStore = shaderStore;
        }

        public GLTFImporter(IMaterialImporter materialImporter)
        {
            _materialImporter = materialImporter;
        }
        public GLTFImporter(IShaderStore shaderStore, IMeshImporter meshImporter, IMaterialImporter materialImporter, INodeImporter nodeImporter, IAnimationImporter animationImporter)
        {
            _shaderStore = shaderStore ?? new ShaderStore(this);
            _meshImporter = meshImporter ?? new MeshImporter();
            _materialImporter = materialImporter ?? new MaterialImporter(_shaderStore, this);
            _nodeImporter = nodeImporter ?? new NodeImporter();
            _animationImporter = animationImporter ?? new AnimationImporter();
        }

        public GLTFImporter() : this(null, null, null, null, null) { }

        #region Source

        /// <summary>
        /// JSON source
        /// </summary>
        public string json;

        /// <summary>
        /// GLTF parsed from JSON
        /// </summary>
        public GLTFRoot gltf; // parsed

        public static bool IsGeneratedGLTFumaAndOlderThan(string generatorVersion, int major, int minor)
        {
            if (string.IsNullOrEmpty(generatorVersion)) return false;
            if (generatorVersion == "GLTFuma") return true;
            if (!generatorVersion.StartsWith("GLTFuma-")) return false;

            try
            {
                var index = generatorVersion.IndexOf('.');
                var generatorMajor = int.Parse(generatorVersion.Substring(8, index - 8));
                var generatorMinor = int.Parse(generatorVersion.Substring(index + 1));

                if (generatorMajor < major) return true;
                return generatorMinor < minor;
            }
            catch (Exception ex)
            {
                Debug.LogWarningFormat("{0}: {1}", generatorVersion, ex);
                return false;
            }
        }

        public bool IsGeneratedGLTFumaAndOlder(int major, int minor)
        {
            if (gltf == null || gltf.asset == null) return false;
            return IsGeneratedGLTFumaAndOlderThan(gltf.asset.generator, major, minor);
        }

        /// <summary>
        /// URI access
        /// </summary>
        public IStorage storage;
        #endregion

        #region Parse
        public void Parse(string path)
        {
            Parse(path, File.ReadAllBytes(path));
        }

        /// <summary>
        /// Parse gltf json or Parse json chunk of glb
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bytes"></param>
        public virtual void Parse(string path, byte[] bytes)
        {
            var ext = Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".gltf":
                    ParseJson(Encoding.UTF8.GetString(bytes), new FileSystemStorage(Path.GetDirectoryName(path)));
                    break;

                case ".zip":
                    {
                        var zipArchive = Zip.ZipArchiveStorage.Parse(bytes);
                        var gltf = zipArchive.Entries.FirstOrDefault(x => x.FileName.ToLower().EndsWith(".gltf"));
                        if (gltf == null)
                        {
                            throw new Exception("no gltf in archive");
                        }
                        var jsonBytes = zipArchive.Extract(gltf);
                        var json = Encoding.UTF8.GetString(jsonBytes);
                        ParseJson(json, zipArchive);
                    }
                    break;

                case ".glb":
                    ParseGlb(bytes);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        public void ParseGlb(byte[] bytes)
        {
            var chunks = bytes.ToGLBChunks();

            if (chunks.Count != 2)
            {
                throw new Exception("unknown chunk count: " + chunks.Count);
            }

            if (chunks[0].type != GLBChunkType.JSON)
            {
                throw new Exception("chunk 0 is not JSON");
            }

            if (chunks[1].type != GLBChunkType.BIN)
            {
                throw new Exception("chunk 1 is not BIN");
            }

            var jsonBytes = chunks[0].bytes;
            ParseJson(Encoding.UTF8.GetString(jsonBytes.Array, jsonBytes.Offset, jsonBytes.Count), new SimpleStorage(chunks[1].bytes));
        }

        public virtual void ParseJson(string json, IStorage storage)
        {
            this.json = json;
            this.storage = storage;

            gltf = JsonUtility.FromJson<GLTFRoot>(this.json);
            if (gltf.asset.version != "2.0")
            {
                throw new GLTFumaException("unknown gltf version {0}", gltf.asset.version);
            }

            // Version Compatibility
            RestoreOlderVersionValues();

            // parepare byte buffer
            //GLTF.baseDir = System.IO.Path.GetDirectoryName(Path);
            foreach (var buffer in gltf.buffers)
            {
                buffer.OpenStorage(storage);
            }
        }
        public virtual void ParseJson(string json)
        {
            this.json = json;

            gltf = JsonUtility.FromJson<GLTFRoot>(this.json);
            if (gltf.asset.version != "2.0")
            {
                throw new GLTFumaException("unknown gltf version {0}", gltf.asset.version);
            }

            // Version Compatibility
            RestoreOlderVersionValues();

            // parepare byte buffer
            //GLTF.baseDir = System.IO.Path.GetDirectoryName(Path);
            // foreach (var buffer in GLTF.buffers)
            // {
            //     buffer.OpenStorage(storage);
            // }
        }

        private void RestoreOlderVersionValues()
        {
            var parsed = UniJSON.JsonParser.Parse(json);
            for (int i = 0; i < gltf.images.Count; ++i)
            {
                if (string.IsNullOrEmpty(gltf.images[i].name))
                {
                    try
                    {
                        var extraName = parsed["images"][i]["extra"]["name"].Value.GetString();
                        if (!string.IsNullOrEmpty(extraName))
                        {
                            //Debug.LogFormat("restore texturename: {0}", extraName);
                            gltf.images[i].name = extraName;
                        }
                    }
                    catch (Exception)
                    {
                        // do nothing
                    }
                }
            }
            for (int i = 0; i < gltf.meshes.Count; ++i)
            {
                var mesh = gltf.meshes[i];
                try
                {
                    for (int j = 0; j < mesh.primitives.Count; ++j)
                    {
                        var primitive = mesh.primitives[j];
                        for (int k = 0; k < primitive.targets.Count; ++k)
                        {
                            var extraName = parsed["meshes"][i]["primitives"][j]["targets"][k]["extra"]["name"].Value.GetString();
                            //Debug.LogFormat("restore morphName: {0}", extraName);
                            primitive.extras.targetNames.Add(extraName);
                        }
                    }
                }
                catch (Exception)
                {
                    // do nothing
                }
            }
#if false
            for (int i = 0; i < GLTF.nodes.Count; ++i)
            {
                var node = GLTF.nodes[i];
                try
                {
                    var extra = parsed["nodes"][i]["extra"]["skinRootBone"].AsInt;
                    //Debug.LogFormat("restore extra: {0}", extra);
                    //node.extras.skinRootBone = extra;
                }
                catch (Exception)
                {
                    // do nothing
                }
            }
#endif
        }
        #endregion

        public void CreateTextureItems(UnityPath imageBaseDir = default(UnityPath))
        {
//            Debug.Log("create texture items" + _textures.Count);
            if (_textures.Any()) return;

            for (int i = 0; i < gltf.textures.Count; ++i)
            {
                var image = gltf.GetImageFromTextureIndex(i);

                TextureItem item = null;
#if UNITY_EDITOR
                if (imageBaseDir.isUnderAssetsFolder
                    && !string.IsNullOrEmpty(image.uri)
                    && !image.uri.StartsWith("data:")
                    )
                {
                    ///
                    /// required SaveTexturesAsPng or SetTextureBaseDir
                    ///
                    var assetPath = imageBaseDir.Child(image.uri);
                    var textureName = !string.IsNullOrEmpty(image.name) ? image.name : Path.GetFileNameWithoutExtension(image.uri);
                    item = new TextureItem(i, assetPath, textureName);
                }
                else
#endif
                {
                    item = new TextureItem(i);
                }

                AddTexture(item);
            }
        }

        public async Task Load(IStorage storage, Action<float> progress)
        {
            this.storage = storage;
            if (_textures.Count == 0)
            {
                //
                // runtime
                //
                CreateTextureItems();
                await Task.Yield();
                progress.Invoke(0.1f);
            }
            else
            {
                //
                // already CreateTextures(by assetPostProcessor or editor menu)
                //
            }
            // yield return TexturesProcessOnAnyThread();
            // progress.Invoke(0.15f);
            // yield return TexturesProcessOnMainThread();
            // progress.Invoke(0.2f);
            //Debug.Log("start load material");
            await LoadMaterials();
            progress.Invoke(0.3f);
            // if (gltf.meshes.SelectMany(x => x.primitives)
            //     .Any(x => x.extensions.KHR_draco_mesh_compression != null))
            // {
            //throw new UniGLTFNotSupportedException("draco is not supported");
            // }

            // meshes
            //var meshImporter = new MeshImporter();
            for (int i = 0; i < gltf.meshes.Count; ++i)
            {
                var index = i;
                using (MeasureTime("ReadMesh"))
                {
                    //Debug.Log("read mesh ... " + index);
                    // var x = meshImporter.ReadMesh(this, index);
                    // var meshWithMaterials = meshImporter.BuildMesh(this, x);
                    var meshWithMaterials = _meshImporter.BuildMesh(gltf, index);
                    await Task.Yield();
                    meshWithMaterials.materials = meshWithMaterials.materialIndices.Select(x => GetMaterial(x)).ToArray();

                    var mesh = meshWithMaterials.mesh;

                    // mesh name
                    if (string.IsNullOrEmpty(mesh.name))
                    {
                        mesh.name = $"GLTFuma import#{i}";
                    }
                    var originalName = mesh.name;
                    for (int j = 1; meshes.Any(y => y.mesh.name == mesh.name); ++j)
                    {
                        mesh.name = string.Format("{0}({1})", originalName, j);
                    }
                    await Task.Yield();
                    meshes.Add(meshWithMaterials);
                    await Task.Yield();
                    progress.Invoke(0.3f + 0.5f * (i + 1) / gltf.meshes.Count);
                }
            }

            await LoadNodes();
            progress.Invoke(0.9f);
            //Debug.Log("start BuildHierarchy ");
            await BuildHierarchy();

            using (MeasureTime("AnimationImporter"))
            {
                //Debug.Log("animators....");
                _animationImporter.ImportAnimation(this);
            }

            OnLoadModel();
            progress.Invoke(1);
            await Task.Yield();
            if (showSpeedLog)
            {
                Debug.Log(GetSpeedLog());
            }
        }

        protected virtual void OnLoadModel()
        {
            root.name = "GLTF";
        }

        private IEnumerator TexturesProcessOnAnyThread()
        {
            using (MeasureTime("TexturesProcessOnAnyThread"))
            {
                foreach (var x in GetTextures())
                {
                    x.ProcessOnAnyThread(gltf, storage);
                    yield return null;
                }
            }
        }

        private IEnumerator TexturesProcessOnMainThread()
        {
            using (MeasureTime("TexturesProcessOnMainThread"))
            {
                foreach (var x in GetTextures())
                {
                    yield return x.ProcessOnMainThreadCoroutine(gltf);
                }
            }
        }

        private async Task LoadMaterials()
        {
            using (MeasureTime("LoadMaterials"))
            {
                if (gltf.materials == null || !gltf.materials.Any())
                {
                    AddMaterial(materialImporter.CreateMaterial(0, null));
                }
                else
                {
                    for (int i = 0; i < gltf.materials.Count; ++i)
                    {
                        AddMaterial(materialImporter.CreateMaterial(i, gltf.materials[i]));
                    }
                }
            }
            await Task.Yield();
        }

        private IEnumerator LoadMeshes()
        {
            //var meshImporter = new MeshImporter();
            for (int i = 0; i < gltf.meshes.Count; ++i)
            {
                // var meshContext = meshImporter.ReadMesh(this, i);
                // var meshWithMaterials = meshImporter.BuildMesh(this, meshContext);
                var meshWithMaterials = _meshImporter.BuildMesh(gltf, i);
                meshWithMaterials.materials = meshWithMaterials.materialIndices.Select(x => GetMaterial(x)).ToArray();
                var mesh = meshWithMaterials.mesh;
                if (string.IsNullOrEmpty(mesh.name))
                {
                    mesh.name = string.Format("GLTFuma import#{0}", i);
                }
                meshes.Add(meshWithMaterials);

                yield return null;
            }
        }

        private async Task LoadNodes()
        {
            using (MeasureTime("LoadNodes"))
            {
                foreach (var x in gltf.nodes)
                {
                    nodes.Add(_nodeImporter.ImportNode(x).transform);
                }
            }

            await Task.Yield();
        }

        private async Task BuildHierarchy()
        {
            //Debug.Log("BuildHierarchy");
            using (MeasureTime("BuildHierarchy"))
            {
                var nodes = new List<TransformWithSkin>();
                for (int i = 0; i < this.nodes.Count; ++i)
                {
                    nodes.Add(_nodeImporter.BuildHierarchy(this, i));
                }

                _nodeImporter.FixCoordinate(gltf, nodes);

                // skinning
                for (int i = 0; i < nodes.Count; ++i)
                {
                    _nodeImporter.SetupSkinning(gltf, nodes, i);
                }

                // connect root
                root = new GameObject("_root_");
                foreach (var x in gltf.rootnodes)
                {
                    var t = nodes[x].transform;
                    t.SetParent(root.transform, false);
                }
            }
            //Debug.Log("root .... "+root);
            await Task.Yield();
        }

        #region Imported
        public GameObject root;
        public List<Transform> nodes = new List<Transform>();

        List<TextureItem> _textures = new List<TextureItem>();
        public IList<TextureItem> GetTextures()
        {
            return _textures;
        }
        // public TextureItem GetTexture(int i)
        // {
        //     if (i < 0 || i >= m_textures.Count)
        //     {
        //         return null;
        //     }
        //     return m_textures[i];
        // }
        public bool SetMaterialTexture(Material material, int index, string prop)
        {
            if (index < 0 || index >= _textures.Count) return false;
            //Debug.Log("set material texture " + index);
            if (string.IsNullOrEmpty(prop))
                _textures[index].Load(gltf, storage, t => material.mainTexture = t);
            else
                _textures[index].Load(gltf, storage, t => material.SetTexture(prop, t));

            return true;
        }
        public void AddTexture(TextureItem item)
        {
            _textures.Add(item);
        }

        List<Material> _materials = new List<Material>();
        public void AddMaterial(Material material)
        {
            var originalName = material.name;
            int i = 2;
            while (_materials.Any(x => x.name == material.name))
            {
                material.name = $"{originalName}({ i++})";
            }
            _materials.Add(material);
        }
        // public IList<Material> GetMaterials()
        // {
        //     return _materials;
        // }
        public Material GetMaterial(int index)
        {
            if (index < 0 || index >= _materials.Count) return null;
            return _materials[index];
        }

        public List<MeshWithMaterials> meshes = new List<MeshWithMaterials>();
        public void ShowMeshes()
        {
            foreach (var x in meshes)
            {
                foreach (var y in x.renderers)
                {
                    y.enabled = true;
                }
            }
        }

        public void EnableUpdateWhenOffscreen()
        {
            foreach (var x in meshes)
            {
                foreach (var r in x.renderers)
                {
                    var skinnedMeshRenderer = r as SkinnedMeshRenderer;
                    if (skinnedMeshRenderer != null)
                    {
                        skinnedMeshRenderer.updateWhenOffscreen = true;
                    }
                }
            }
        }

        public List<AnimationClip> animationClips = new List<AnimationClip>();
        #endregion

        public virtual IEnumerable<UnityEngine.Object> ObjectsForSubAsset()
        {
            HashSet<Texture2D> textures = new HashSet<Texture2D>();
            foreach (var x in _textures.SelectMany(y => y.GetTexturesForSaveAssets()))
            {
                if (!textures.Contains(x))
                {
                    textures.Add(x);
                }
            }
            foreach (var x in textures) { yield return x; }
            foreach (var x in _materials) { yield return x; }
            foreach (var x in meshes) { yield return x.mesh; }
            foreach (var x in animationClips) { yield return x; }
        }



        /// <summary>
        /// This function is used for clean up after create assets.
        /// </summary>
        /// <param name="destroySubAssets">Ambiguous arguments</param>
        [Obsolete("Use Dispose for runtime loader resource management")]
        public void Destroy(bool destroySubAssets)
        {
            if (root != null) GameObject.DestroyImmediate(root);
            if (destroySubAssets)
            {
#if UNITY_EDITOR
                foreach (var o in ObjectsForSubAsset())
                {
                    UnityEngine.Object.DestroyImmediate(o, true);
                }
#endif
            }
        }

        public void Dispose()
        {
            DestroyRootAndResources();
        }

        /// <summary>
        /// Destroy resources that created ImporterContext for runtime load.
        /// </summary>
        public void DestroyRootAndResources()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarningFormat("Dispose called in editor mode. This function is for runtime");
            }

            // Remove hierarchy
            if (root != null) GameObject.Destroy(root);

            // Remove resources. materials, textures meshes etc...
            foreach (var o in ObjectsForSubAsset())
            {
                UnityEngine.Object.DestroyImmediate(o, true);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Destroy the GameObject that became the basis of Prefab
        /// </summary>
        public void EditorDestroyRoot()
        {
            if (root != null) GameObject.DestroyImmediate(root);
        }

        /// <summary>
        /// Destroy assets that created ImporterContext. This function is clean up for imoprter error.
        /// </summary>
        public void EditorDestroyRootAndAssets()
        {
            // Remove hierarchy
            if (root != null) GameObject.DestroyImmediate(root);

            // Remove resources. materials, textures meshes etc...
            foreach (var o in ObjectsForSubAsset())
            {
                UnityEngine.Object.DestroyImmediate(o, true);
            }
        }
#endif
    }
}