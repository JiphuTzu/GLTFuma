using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UMa.GLTF
{
    public class TransformWithSkin
    {
        public Transform transform;
        public GameObject gameObject { get { return transform.gameObject; } }
        public int? skinIndex;
    }
    public interface INodeImporter
    {
        GameObject ImportNode(GLTFNode node);
        TransformWithSkin BuildHierarchy(GLTFImporter context, int i);
        void FixCoordinate(GLTFRoot gltf, List<TransformWithSkin> nodes);
        void SetupSkinning(GLTFRoot gltf, List<TransformWithSkin> nodes, int i);
    }
    public class NodeImporter : INodeImporter
    {
        public GameObject ImportNode(GLTFNode node)
        {
            var nodeName = node.name;
            if (!string.IsNullOrEmpty(nodeName) && nodeName.Contains("/"))
            {
                Debug.LogWarningFormat("node {0} contains /. replace _", node.name);
                nodeName = nodeName.Replace("/", "_");
            }
            var go = new GameObject(nodeName);

            //
            // transform
            //
            if (node.translation != null && node.translation.Length > 0)
            {
                go.transform.localPosition = node.translation.ToVector3();
            }
            if (node.rotation != null && node.rotation.Length > 0)
            {
                go.transform.localRotation = node.rotation.ToQuaternion();
            }
            if (node.scale != null && node.scale.Length > 0)
            {
                go.transform.localScale = node.scale.ToVector3();
            }
            if (node.matrix != null && node.matrix.Length > 0)
            {
                var m = node.matrix.ToMatrix();
                go.transform.localRotation = m.ExtractRotation();
                go.transform.localPosition = m.ExtractPosition();
                go.transform.localScale = m.ExtractScale();
            }
            return go;
        }



        public TransformWithSkin BuildHierarchy(GLTFImporter context, int i)
        {
            var go = context.nodes[i].gameObject;
            if (string.IsNullOrEmpty(go.name))
            {
                go.name = $"node{i:000}";
            }

            var nodeWithSkin = new TransformWithSkin
            {
                transform = go.transform,
            };

            //
            // build hierachy
            //
            var node = context.gltf.nodes[i];
            if (node.children != null)
            {
                foreach (var child in node.children)
                {
                    // node has local transform
                    context.nodes[child].transform.SetParent(context.nodes[i].transform, false);
                }
            }

            //
            // attach mesh
            //
            //Debug.Log("node mesh ... "+node.name+" == "+node.mesh);
            if (node.mesh != -1)
            {
                var mesh = context.meshes[node.mesh];
                if (mesh.mesh.blendShapeCount == 0 && node.skin == -1)
                {
                    // without blendshape and bone skinning
                    var filter = go.AddComponent<MeshFilter>();
                    filter.sharedMesh = mesh.mesh;
                    var renderer = go.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = mesh.materials;
                    // invisible in loading
                    renderer.enabled = false;
                    mesh.renderers.Add(renderer);
                }
                else
                {
                    var renderer = go.AddComponent<SkinnedMeshRenderer>();

                    if (node.skin != -1)
                    {
                        nodeWithSkin.skinIndex = node.skin;
                    }

                    renderer.sharedMesh = mesh.mesh;
                    renderer.sharedMaterials = mesh.materials;
                    // invisible in loading
                    renderer.enabled = false;
                    mesh.renderers.Add(renderer);
                }
            }

            return nodeWithSkin;
        }

        //
        // fix node's coordinate. z-back to z-forward
        //
        public void FixCoordinate(GLTFRoot gltf, List<TransformWithSkin> nodes)
        {
            var globalTransformMap = nodes.ToDictionary(x => x.transform, x => new PosRot
            {
                position = x.transform.position,
                rotation = x.transform.rotation,
            });
            foreach (var x in gltf.rootnodes)
            {
                // fix nodes coordinate
                // reverse Z in global
                var t = nodes[x].transform;
                //t.SetParent(root.transform, false);

                foreach (var transform in t.Traverse())
                {
                    var g = globalTransformMap[transform];
                    transform.position = g.position.ReverseZ();
                    transform.rotation = g.rotation.ReverseZ();
                }
            }
        }

        public void SetupSkinning(GLTFRoot gltf, List<TransformWithSkin> nodes, int i)
        {
            var x = nodes[i];
            var skinnedMeshRenderer = x.transform.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                var mesh = skinnedMeshRenderer.sharedMesh;
                if (x.skinIndex.HasValue)
                {
                    if (mesh == null) throw new Exception();
                    if (skinnedMeshRenderer == null) throw new Exception();

                    if (x.skinIndex.Value < gltf.skins.Count)
                    {
                        var skin = gltf.skins[x.skinIndex.Value];

                        skinnedMeshRenderer.sharedMesh = null;

                        var joints = skin.joints.Select(y => nodes[y].transform).ToArray();
                        skinnedMeshRenderer.bones = joints;

                        if (skin.skeleton >= 0 && skin.skeleton < nodes.Count)
                        {
                            skinnedMeshRenderer.rootBone = nodes[skin.skeleton].transform;
                        }

                        if (skin.inverseBindMatrices != -1)
                        {
                            // BlendShape only ?
#if false
                            // https://docs.unity3d.com/ScriptReference/Mesh-bindposes.html
                            var hipsParent = nodes[0].Transform;
                            var calculatedBindPoses = joints.Select(y => y.worldToLocalMatrix * hipsParent.localToWorldMatrix).ToArray();
                            mesh.bindposes = calculatedBindPoses;
#else
                            var bindPoses = gltf.GetArrayFromAccessor<Matrix4x4>(skin.inverseBindMatrices)
                                .Select(y => y.ReverseZ()).ToArray();
                            mesh.bindposes = bindPoses;
#endif
                        }

                        skinnedMeshRenderer.sharedMesh = mesh;
                    }
                }
            }
        }
    }
}