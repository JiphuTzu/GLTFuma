using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UMa.GLTF
{
    public class GLTFExporter : IDisposable
    {
        public GLTFRoot gltf;

        public bool useSparseAccessorForBlendShape
        {
            get;
            set;
        }

        public GameObject copy
        {
            get;
            protected set;
        }

        public List<Mesh> meshes
        {
            get;
            private set;
        }

        public List<Transform> nodes
        {
            get;
            private set;
        }

        public List<Material> materials
        {
            get;
            private set;
        }

        public TextureExportManager textureManager;

        protected virtual IMaterialExporter CreateMaterialExporter()
        {
            return new MaterialExporter();
        }

        /// <summary>
        /// このエクスポーターがサポートするExtension
        /// </summary>
        protected virtual IEnumerable<string> extensionUsed
        {
            get
            {
                yield return KHRMaterialUnlit.ExtensionName;
            }
        }

        public GLTFExporter(GLTFRoot gltf)
        {
            this.gltf = gltf;

            this.gltf.extensionsUsed.AddRange(extensionUsed);

            this.gltf.asset = new GLTFAsset
            {
                generator = "GLTFumaExporter",
                version = "2.0",
            };
        }

        // public static GLTFRoot Export(GameObject go)
        // {
        //     var gltf = new GLTFRoot();
        //     using (var exporter = new GLTFExporter(gltf))
        //     {
        //         exporter.Prepare(go);
        //         exporter.Export();
        //     }
        //     return gltf;
        // }

        public virtual void Prepare(GameObject go)
        {
            // コピーを作って、Z軸を反転することで左手系を右手系に変換する
            copy = GameObject.Instantiate(go);
            copy.transform.ReverseZRecursive();
        }

        public void Export()
        {
            FromGameObject(gltf, copy, useSparseAccessorForBlendShape);
        }

        public void Dispose()
        {
            if (Application.isEditor)
            {
                GameObject.DestroyImmediate(copy);
            }
            else
            {
                GameObject.Destroy(copy);
            }
        }

        #region Export
        static GLTFNode ExportNode(Transform x, List<Transform> nodes, List<Mesh> meshes, List<SkinnedMeshRenderer> skins)
        {
            var node = new GLTFNode
            {
                name = x.name,
                children = x.transform.GetChildren().Select(y => nodes.IndexOf(y)).ToArray(),
                rotation = x.transform.localRotation.ToArray(),
                translation = x.transform.localPosition.ToArray(),
                scale = x.transform.localScale.ToArray(),
            };

            // if (x.gameObject.activeInHierarchy)
            // {
                var meshFilter = x.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    node.mesh = meshes.IndexOf(meshFilter.sharedMesh);
                }

                var skinnredMeshRenderer = x.GetComponent<SkinnedMeshRenderer>();
                if (skinnredMeshRenderer != null)
                {
                    node.mesh = meshes.IndexOf(skinnredMeshRenderer.sharedMesh);
                    node.skin = skins.IndexOf(skinnredMeshRenderer);
                }
            // }

            return node;
        }

        void FromGameObject(GLTFRoot gltf, GameObject go, bool useSparseAccessorForMorphTarget = false)
        {
            var bytesBuffer = new ArrayByteBuffer(new byte[50 * 1024 * 1024]);
            var bufferIndex = gltf.AddBuffer(bytesBuffer);

            GameObject tmpParent = null;
            if (go.transform.childCount == 0)
            {
                tmpParent = new GameObject("tmpParent");
                go.transform.SetParent(tmpParent.transform, true);
                go = tmpParent;
            }

            try
            {

                nodes = go.transform.Traverse()
                    .Skip(1) // exclude root object for the symmetry with the importer
                    .ToList();

                #region Materials and Textures
                materials = nodes.SelectMany(x => x.GetSharedMaterials()).Where(x => x != null).Distinct().ToList();
                var unityTextures = materials.SelectMany(x => TextureIO.GetTextures(x)).Where(x => x.Texture != null).Distinct().ToList();

                textureManager = new TextureExportManager(unityTextures);

                var materialExporter = CreateMaterialExporter();
                gltf.materials = materials.Select(x => materialExporter.ExportMaterial(x, textureManager)).ToList();
                Debug.Log("image count = "+unityTextures.Count);
                for (int i = 0; i < unityTextures.Count; ++i)
                {
                    var unityTexture = unityTextures[i];
                    TextureIO.ExportTexture(gltf, bufferIndex, textureManager.GetExportTexture(i), unityTexture.TextureType);
                }
                #endregion


                #region Meshes
                var unityMeshes = nodes
                    .Select(x => new MeshWithRenderer
                    {
                        mesh = x.GetSharedMesh(),
                        rendererer = x.GetComponent<Renderer>(),
                    })
                    .Where(x =>
                    {
                        if (x.mesh == null)
                        {
                            return false;
                        }
                        if (x.rendererer.sharedMaterials == null
                        || x.rendererer.sharedMaterials.Length == 0)
                        {
                            return false;
                        }

                        return true;
                    })
                    .ToList();
                Debug.Log("unityMesher...."+unityMeshes.Count);
                MeshExporter.ExportMeshes(gltf, bufferIndex, unityMeshes, materials, useSparseAccessorForMorphTarget);
                meshes = unityMeshes.Select(x => x.mesh).ToList();
                #endregion

                #region Skins
                var unitySkins = nodes
                    .Select(x => x.GetComponent<SkinnedMeshRenderer>()).Where(x =>
                        x != null
                        && x.bones != null
                        && x.bones.Length > 0)
                    .ToList();
                gltf.nodes = nodes.Select(x => ExportNode(x, nodes, unityMeshes.Select(y => y.mesh).ToList(), unitySkins)).ToList();
                gltf.scenes = new List<GLTFScene>
                {
                    new GLTFScene
                    {
                        nodes = go.transform.GetChildren().Select(x => nodes.IndexOf(x)).ToArray(),
                    }
                };

                foreach (var x in unitySkins)
                {
                    var matrices = x.sharedMesh.bindposes.Select(y => y.ReverseZ()).ToArray();
                    var accessor = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, matrices, GLTFBufferTarget.NONE);

                    var skin = new GLTFSkin
                    {
                        inverseBindMatrices = accessor,
                        joints = x.bones.Select(y => nodes.IndexOf(y)).ToArray(),
                        skeleton = nodes.IndexOf(x.rootBone),
                    };
                    var skinIndex = gltf.skins.Count;
                    gltf.skins.Add(skin);

                    foreach (var z in nodes.Where(y => y.HasComponent(x)))
                    {
                        var nodeIndex = nodes.IndexOf(z);
                        var node = gltf.nodes[nodeIndex];
                        node.skin = skinIndex;
                    }
                }
                #endregion

#if UNITY_EDITOR
                #region Animations

                var clips = new List<AnimationClip>();
                var animator = go.GetComponent<Animator>();
                var animation = go.GetComponent<Animation>();
                if (animator != null)
                {
                    clips = AnimationExporter.GetAnimationClips(animator);
                }
                else if (animation != null)
                {
                    clips = AnimationExporter.GetAnimationClips(animation);
                }

                if (clips.Any())
                {
                    Debug.Log("export clips.."+clips.Count);
                    foreach (AnimationClip clip in clips)
                    {
                        var animationWithCurve = AnimationExporter.Export(clip, go.transform, nodes);

                        foreach (var kv in animationWithCurve.samplers)
                        {
                            var sampler = animationWithCurve.animation.samplers[kv.Key];
                            var inputAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, kv.Value.input);
                            sampler.input = inputAccessorIndex;

                            var outputAccessorIndex = gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, kv.Value.output);
                            sampler.output = outputAccessorIndex;
                            Debug.Log(sampler.interpolation+">>"+string.Join(",",kv.Value.output));

                            // modify accessors
                            var outputAccessor = gltf.accessors[outputAccessorIndex];
                            var channel = animationWithCurve.animation.channels.First(x => x.sampler == kv.Key);
                            switch (GLTFAnimationTarget.GetElementCount(channel.target.path))
                            {
                                case 1:
                                    outputAccessor.type = "SCALAR";
                                    //outputAccessor.count = ;
                                    break;
                                case 3:
                                    outputAccessor.type = "VEC3";
                                    outputAccessor.count /= 3;
                                    break;

                                case 4:
                                    outputAccessor.type = "VEC4";
                                    outputAccessor.count /= 4;
                                    break;

                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        animationWithCurve.animation.name = clip.name;
                        gltf.animations.Add(animationWithCurve.animation);
                    }
                }
                #endregion
#endif
            }
            finally
            {
                if (tmpParent != null)
                {
                    tmpParent.transform.GetChild(0).SetParent(null);
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(tmpParent);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(tmpParent);
                    }
                }
            }
        }
        #endregion
    }
}
