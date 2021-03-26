using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniJSON;

namespace UMa.GLTF
{
    [Serializable]
    public class GLTFRoot : JsonSerializableBase, IEquatable<GLTFRoot>
    {
        [JsonSchema(Required = true)]
        public GLTFAsset asset = new GLTFAsset();
        [JsonSchema(Dependencies = new string[] { "scenes" }, Minimum = 0)]
        public int scene;

        [JsonSchema(MinItems = 1)]
        public List<GLTFScene> scenes = new List<GLTFScene>();

        #region Buffer      
        [JsonSchema(MinItems = 1)]
        public List<GLTFBuffer> buffers = new List<GLTFBuffer>();
        public int AddBuffer(IBytesBuffer bytesBuffer)
        {
            var index = buffers.Count;
            buffers.Add(new GLTFBuffer(bytesBuffer));
            return index;
        }

        [JsonSchema(MinItems = 1)]
        public List<GLTFBufferView> bufferViews = new List<GLTFBufferView>();
        public int AddBufferView(GLTFBufferView view)
        {
            var index = bufferViews.Count;
            bufferViews.Add(view);
            return index;
        }

        [JsonSchema(MinItems = 1)]
        public List<GLTFAccessor> accessors = new List<GLTFAccessor>();

        T[] GetAttrib<T>(GLTFAccessor accessor, GLTFBufferView view) where T : struct
        {
            return GetAttrib<T>(accessor.count, accessor.byteOffset, view);
        }
        T[] GetAttrib<T>(int count, int byteOffset, GLTFBufferView view) where T : struct
        {
            var attrib = new T[count];
            //
            var segment = buffers[view.buffer].GetBytes();
            var bytes = new ArraySegment<Byte>(segment.Array, segment.Offset + view.byteOffset + byteOffset, count * view.byteStride);
            bytes.MarshalCoyTo(attrib);
            return attrib;
        }

        public ArraySegment<Byte> GetViewBytes(int bufferView)
        {
            var view = bufferViews[bufferView];
            var segment = buffers[view.buffer].GetBytes();
            return new ArraySegment<byte>(segment.Array, segment.Offset + view.byteOffset, view.byteLength);
        }

        IEnumerable<int> GetIndices(GLTFAccessor accessor, out int count)
        {
            count = accessor.count;
            var view = bufferViews[accessor.bufferView];
            switch ((GLTFComponentType)accessor.componentType)
            {
                case GLTFComponentType.UNSIGNED_BYTE:
                    {
                        return GetAttrib<Byte>(accessor, view).Select(x => (int)(x));
                    }

                case GLTFComponentType.UNSIGNED_SHORT:
                    {
                        return GetAttrib<UInt16>(accessor, view).Select(x => (int)(x));
                    }

                case GLTFComponentType.UNSIGNED_INT:
                    {
                        return GetAttrib<UInt32>(accessor, view).Select(x => (int)(x));
                    }
            }
            throw new NotImplementedException("GetIndices: unknown componenttype: " + accessor.componentType);
        }

        IEnumerable<int> GetIndices(GLTFBufferView view, int count, int byteOffset, GLTFComponentType componentType)
        {
            switch (componentType)
            {
                case GLTFComponentType.UNSIGNED_BYTE:
                    {
                        return GetAttrib<Byte>(count, byteOffset, view).Select(x => (int)(x));
                    }

                case GLTFComponentType.UNSIGNED_SHORT:
                    {
                        return GetAttrib<UInt16>(count, byteOffset, view).Select(x => (int)(x));
                    }

                case GLTFComponentType.UNSIGNED_INT:
                    {
                        return GetAttrib<UInt32>(count, byteOffset, view).Select(x => (int)(x));
                    }
            }
            throw new NotImplementedException("GetIndices: unknown componenttype: " + componentType);
        }

        public int[] GetIndices(int accessorIndex)
        {
            int count;
            var result = GetIndices(accessors[accessorIndex], out count);
            var indices = new int[count];

            // flip triangles
            var it = result.GetEnumerator();
            {
                for (int i = 0; i < count; i += 3)
                {
                    it.MoveNext(); indices[i + 2] = it.Current;
                    it.MoveNext(); indices[i + 1] = it.Current;
                    it.MoveNext(); indices[i] = it.Current;
                }
            }

            return indices;
        }

        public T[] GetArrayFromAccessor<T>(int accessorIndex) where T : struct
        {
            var vertexAccessor = accessors[accessorIndex];

            if (vertexAccessor.count <= 0) return new T[] { };

            var result = (vertexAccessor.bufferView != -1)
                ? GetAttrib<T>(vertexAccessor, bufferViews[vertexAccessor.bufferView])
                : new T[vertexAccessor.count]
                ;

            var sparse = vertexAccessor.sparse;
            if (sparse != null && sparse.count > 0)
            {
                // override sparse values
                var indices = GetIndices(bufferViews[sparse.indices.bufferView], sparse.count, sparse.indices.byteOffset, sparse.indices.componentType);
                var values = GetAttrib<T>(sparse.count, sparse.values.byteOffset, bufferViews[sparse.values.bufferView]);

                var it = indices.GetEnumerator();
                for (int i = 0; i < sparse.count; ++i)
                {
                    it.MoveNext();
                    result[it.Current] = values[i];
                }
            }
            return result;
        }

        public float[] GetArrayFromAccessorAsFloat(int accessorIndex)
        {
            var vertexAccessor = accessors[accessorIndex];

            if (vertexAccessor.count <= 0) return new float[] { };

            var bufferCount = vertexAccessor.count * vertexAccessor.typeCount;
            var result = (vertexAccessor.bufferView != -1)
                    ? GetAttrib<float>(bufferCount, vertexAccessor.byteOffset, bufferViews[vertexAccessor.bufferView])
                    : new float[bufferCount]
                ;

            var sparse = vertexAccessor.sparse;
            if (sparse != null && sparse.count > 0)
            {
                // override sparse values
                var indices = GetIndices(bufferViews[sparse.indices.bufferView], sparse.count, sparse.indices.byteOffset, sparse.indices.componentType);
                var values = GetAttrib<float>(sparse.count * vertexAccessor.typeCount, sparse.values.byteOffset, bufferViews[sparse.values.bufferView]);

                var it = indices.GetEnumerator();
                for (int i = 0; i < sparse.count; ++i)
                {
                    it.MoveNext();
                    result[it.Current] = values[i];
                }
            }
            return result;
        }
        #endregion

        [JsonSchema(MinItems = 1)]
        public List<GLTFTexture> textures = new List<GLTFTexture>();

        [JsonSchema(MinItems = 1)]
        public List<GLTFTextureSampler> samplers = new List<GLTFTextureSampler>();
        public GLTFTextureSampler GetSampler(int index)
        {
            if (samplers.Count == 0)
            {
                samplers.Add(new GLTFTextureSampler()); // default sampler
            }

            return samplers[index];
        }

        [JsonSchema(MinItems = 1)]
        public List<GLTFImage> images = new List<GLTFImage>();

        public int GetImageIndexFromTextureIndex(int textureIndex)
        {
            return textures[textureIndex].source;
        }

        public GLTFImage GetImageFromTextureIndex(int textureIndex)
        {
            return images[GetImageIndexFromTextureIndex(textureIndex)];
        }

        public GLTFTextureSampler GetSamplerFromTextureIndex(int textureIndex)
        {
            var samplerIndex = textures[textureIndex].sampler;
            return GetSampler(samplerIndex);
        }

        public ArraySegment<Byte> GetImageBytes(IStorage storage, int imageIndex, out string textureName, out string url)
        {
            var image = images[imageIndex];
            if (string.IsNullOrEmpty(image.uri))
            {
                url = null;
                //
                // use buffer view (GLB)
                //
                //m_imageBytes = ToArray(byteSegment);
                textureName = !string.IsNullOrEmpty(image.name) ? image.name : string.Format("{0:00}#GLB", imageIndex);
                return GetViewBytes(image.bufferView);
            }
            else if (image.uri.StartsWith("data:"))
            {
                url = null;
                textureName = !string.IsNullOrEmpty(image.name) ? image.name : string.Format("{0:00}#Base64Embeded", imageIndex);
                return storage.Get(image.uri);
            }
            else
            {
                url = image.uri;
                textureName = !string.IsNullOrEmpty(image.name) ? image.name : Path.GetFileNameWithoutExtension(image.uri);
                return storage.Get(image.uri);
            }
        }

        [JsonSchema(MinItems = 1)]
        public List<GLTFMaterial> materials = new List<GLTFMaterial>();
        public string GetUniqueMaterialName(int index)
        {
            if (materials.Any(x => string.IsNullOrEmpty(x.name))
                || materials.Select(x => x.name).Distinct().Count() != materials.Count)
            {
                return String.Format("{0:00}_{1}", index, materials[index].name);
            }
            else
            {
                return materials[index].name;
            }
        }

        public bool MaterialHasVertexColor(GLTFMaterial material)
        {
            if (material == null)
            {
                return false;
            }

            var materialIndex = materials.IndexOf(material);
            if (materialIndex == -1)
            {
                return false;
            }

            return MaterialHasVertexColor(materialIndex);
        }

        [JsonSchema(MinItems = 1)]
        public List<GLTFMesh> meshes = new List<GLTFMesh>();

        public bool MaterialHasVertexColor(int materialIndex)
        {
            if (materialIndex < 0 || materialIndex >= materials.Count)
            {
                return false;
            }

            var hasVertexColor = meshes.SelectMany(x => x.primitives).Any(x => x.material == materialIndex && x.hasVertexColor);
            return hasVertexColor;
        }

        [JsonSchema(MinItems = 1)]
        public List<GLTFNode> nodes = new List<GLTFNode>();

        [JsonSchema(MinItems = 1)]
        public List<GLTFSkin> skins = new List<GLTFSkin>();


        public int[] rootnodes
        {
            get
            {
                return scenes[scene].nodes;
            }
        }

        [JsonSchema(MinItems = 1)]
        public List<GLTFAnimation> animations = new List<GLTFAnimation>();

        [JsonSchema(MinItems = 1)]
        public List<GLTFCamera> cameras = new List<GLTFCamera>();

        [JsonSchema(MinItems = 1)]
        public List<string> extensionsUsed = new List<string>();

        [JsonSchema(MinItems = 1)]
        public List<string> extensionsRequired = new List<string>();

        public GLTFExtensions extensions = new GLTFExtensions();
        public GLTFExtras extras = new GLTFExtras();

        public override string ToString()
        {
            return string.Format("{0}", asset);
        }

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            if (extensionsUsed.Count > 0)
            {
                f.KeyValue(() => extensionsUsed);
            }
            if (extensions.count > 0)
            {
                f.KeyValue(() => extensions);
            }
            if (extras.count > 0)
            {
                f.KeyValue(() => extras);
            }

            f.KeyValue(() => asset);

            // buffer
            if (buffers.Any())
            {
                f.KeyValue(() => buffers);
            }
            if (bufferViews.Any())
            {
                f.Key("bufferViews");
                f.GLTFValue(bufferViews);
            }
            if (accessors.Any())
            {
                f.Key("accessors");
                f.GLTFValue(accessors);
            }

            // materials
            if (images.Any())
            {
                f.Key("images");
                f.GLTFValue(images);
                if (samplers.Count == 0)
                {
                    samplers.Add(new GLTFTextureSampler());
                }
            }

            if (samplers.Any())
            {
                f.Key("samplers");
                f.GLTFValue(samplers);
            }

            if (textures.Any())
            {
                f.Key("textures");
                f.GLTFValue(textures);
            }
            if (materials.Any())
            {
                f.Key("materials");
                f.GLTFValue(materials);
            }

            // meshes
            if (meshes.Any())
            {
                f.KeyValue(() => meshes);
            }
            if (skins.Any())
            {
                f.KeyValue(() => skins);
            }

            // scene
            if (nodes.Any())
            {
                f.KeyValue(() => nodes);
            }
            if (scenes.Any())
            {
                f.KeyValue(() => scenes);
                if (scene >= 0)
                {
                    f.KeyValue(() => scene);
                }
            }

            // animations
            if (animations.Any())
            {
                f.Key("animations");
                f.GLTFValue(animations);
            }
        }

        public bool Equals(GLTFRoot other)
        {
            return
                textures.SequenceEqual(other.textures)
                && samplers.SequenceEqual(other.samplers)
                && images.SequenceEqual(other.images)
                && materials.SequenceEqual(other.materials)
                && meshes.SequenceEqual(other.meshes)
                && nodes.SequenceEqual(other.nodes)
                && skins.SequenceEqual(other.skins)
                && scene == other.scene
                && scenes.SequenceEqual(other.scenes)
                && animations.SequenceEqual(other.animations)
                ;
        }

        bool UsedExtension(string key)
        {
            if (extensionsUsed.Contains(key))
            {
                return true;
            }

            return false;
        }

        static Utf8String s_extensions = Utf8String.From("extensions");

        void Traverse(ListTreeNode<JsonValue> node, JsonFormatter f, Utf8String parentKey)
        {
            if (node.IsMap())
            {
                f.BeginMap();
                foreach (var kv in node.ObjectItems())
                {
                    if (parentKey == s_extensions)
                    {
                        if (!UsedExtension(kv.Key.GetString()))
                        {
                            continue;
                        }
                    }
                    f.Key(kv.Key.GetUtf8String());
                    Traverse(kv.Value, f, kv.Key.GetUtf8String());
                }
                f.EndMap();
            }
            else if (node.IsArray())
            {
                f.BeginList();
                foreach (var x in node.ArrayItems())
                {
                    Traverse(x, f, default(Utf8String));
                }
                f.EndList();
            }
            else
            {
                f.Value(node);
            }
        }

        string RemoveUnusedExtensions(string json)
        {
            var f = new JsonFormatter();

            Traverse(JsonParser.Parse(json), f, default(Utf8String));

            return f.ToString();
        }

        public byte[] ToGlbBytes(bool UseUniJSONSerializer = false)
        {
            string json;
            if (UseUniJSONSerializer)
            {
                json = JsonSchema.FromType(GetType()).Serialize(this);
            }
            else
            {
                json = ToJson();
            }

            RemoveUnusedExtensions(json);

            return json.Join(buffers[0].GetBytes());
        }
        public byte[] ToBinary()
        {
            return buffers[0].GetBytes().ToArray();
        }
    }
}