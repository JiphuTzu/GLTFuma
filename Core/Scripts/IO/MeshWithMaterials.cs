using System.Collections.Generic;
using UnityEngine;


namespace UMa.GLTF
{
    public class MeshWithMaterials
    {
        public Mesh mesh;
        public Material[] materials;
        public int[] materialIndices;
        public List<Renderer> renderers=new List<Renderer>(); // SkinnedMeshRenderer or MeshRenderer
    }
}
