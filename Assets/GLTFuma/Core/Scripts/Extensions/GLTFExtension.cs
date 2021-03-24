using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


namespace UMa.GLTF
{
    [Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct UShort4
    {
        public ushort x;
        public ushort y;
        public ushort z;
        public ushort w;

        public UShort4(ushort x, ushort y, ushort z, ushort w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

    public static class GLTFExtension
    {
        struct ComponentVector
        {
            public GLTFComponentType type;
            public int count;

            public ComponentVector(GLTFComponentType type, int count)
            {
                this.type = type;
                this.count = count;
            }
        }

        static Dictionary<Type, ComponentVector> ComponentTypeMap = new Dictionary<Type, ComponentVector>
        {
            { typeof(Vector2), new ComponentVector(GLTFComponentType.FLOAT, 2) },
            { typeof(Vector3), new ComponentVector(GLTFComponentType.FLOAT, 3) },
            { typeof(Vector4), new ComponentVector(GLTFComponentType.FLOAT, 4) },
            { typeof(UShort4), new ComponentVector(GLTFComponentType.UNSIGNED_SHORT, 4) },
            { typeof(Matrix4x4), new ComponentVector(GLTFComponentType.FLOAT, 16) },
            { typeof(Color), new ComponentVector(GLTFComponentType.FLOAT, 4) },
        };

        static GLTFComponentType GetComponentType<T>()
        {
            var cv = default(ComponentVector);
            if (ComponentTypeMap.TryGetValue(typeof(T), out cv))
            {
                return cv.type;
            }
            else if (typeof(T) == typeof(uint))
            {
                return GLTFComponentType.UNSIGNED_INT;
            }
            else if (typeof(T) == typeof(float))
            {
                return GLTFComponentType.FLOAT;
            }
            else
            {
                throw new NotImplementedException(typeof(T).Name);
            }
        }

        static string GetAccessorType<T>()
        {
            var cv = default(ComponentVector);
            if (ComponentTypeMap.TryGetValue(typeof(T), out cv))
            {
                switch (cv.count)
                {
                    case 2: return "VEC2";
                    case 3: return "VEC3";
                    case 4: return "VEC4";
                    case 16: return "MAT4";
                    default: throw new Exception();
                }
            }
            else
            {
                return "SCALAR";
            }
        }

        static int GetAccessorElementCount<T>()
        {
            var cv = default(ComponentVector);
            if (ComponentTypeMap.TryGetValue(typeof(T), out cv))
            {
                return cv.count;
            }
            else
            {
                return 1;
            }
        }

        public static int ExtendBufferAndGetAccessorIndex<T>(this GLTFRoot gltf, int bufferIndex, T[] array,
            GLTFBufferTarget target = GLTFBufferTarget.NONE) where T : struct
        {
            return gltf.ExtendBufferAndGetAccessorIndex(bufferIndex, new ArraySegment<T>(array), target);
        }

        public static int ExtendBufferAndGetAccessorIndex<T>(this GLTFRoot gltf, int bufferIndex,
            ArraySegment<T> array,
            GLTFBufferTarget target = GLTFBufferTarget.NONE) where T : struct
        {
            if (array.Count == 0)
            {
                return -1;
            }
            var viewIndex = ExtendBufferAndGetViewIndex(gltf, bufferIndex, array, target);

            // index buffer's byteStride is unnecessary
            gltf.bufferViews[viewIndex].byteStride = 0;

            var accessorIndex = gltf.accessors.Count;
            gltf.accessors.Add(new GLTFAccessor
            {
                bufferView = viewIndex,
                byteOffset = 0,
                componentType = GetComponentType<T>(),
                type = GetAccessorType<T>(),
                count = array.Count,
            });
            return accessorIndex;
        }

        public static int ExtendBufferAndGetViewIndex<T>(this GLTFRoot gltf, int bufferIndex,
            T[] array,
            GLTFBufferTarget target = GLTFBufferTarget.NONE) where T : struct
        {
            return ExtendBufferAndGetViewIndex(gltf, bufferIndex, new ArraySegment<T>(array), target);
        }

        public static int ExtendBufferAndGetViewIndex<T>(this GLTFRoot gltf, int bufferIndex,
            ArraySegment<T> array,
            GLTFBufferTarget target = GLTFBufferTarget.NONE) where T : struct
        {
            if (array.Count == 0)
            {
                return -1;
            }
            var view = gltf.buffers[bufferIndex].Append(array, target);
            var viewIndex = gltf.bufferViews.Count;
            gltf.bufferViews.Add(view);
            return viewIndex;
        }

        public static int ExtendSparseBufferAndGetAccessorIndex<T>(this GLTFRoot gltf, int bufferIndex,
            int accessorCount,
            T[] sparseValues, int[] sparseIndices, int sparseViewIndex,
            GLTFBufferTarget target = GLTFBufferTarget.NONE) where T : struct
        {
            return ExtendSparseBufferAndGetAccessorIndex(gltf, bufferIndex, 
                accessorCount,
                new ArraySegment<T>(sparseValues), sparseIndices, sparseViewIndex,
                target);
        }

        public static int ExtendSparseBufferAndGetAccessorIndex<T>(this GLTFRoot gltf, int bufferIndex,
            int accessorCount,
            ArraySegment<T> sparseValues, int[] sparseIndices, int sparseIndicesViewIndex,
            GLTFBufferTarget target = GLTFBufferTarget.NONE) where T : struct
        {
            if (sparseValues.Count == 0)
            {
                return -1;
            }
            var sparseValuesViewIndex = ExtendBufferAndGetViewIndex(gltf, bufferIndex, sparseValues, target);
            var accessorIndex = gltf.accessors.Count;
            gltf.accessors.Add(new GLTFAccessor
            {
                byteOffset = 0,
                componentType = GetComponentType<T>(),
                type = GetAccessorType<T>(),
                count = accessorCount,

                sparse = new GLTFSparse
                {
                    count=sparseIndices.Length,
                    indices = new GLTFSparseIndices
                    {
                        bufferView = sparseIndicesViewIndex,
                        componentType = GLTFComponentType.UNSIGNED_INT
                    },
                    values = new GLTFSparseValues
                    {
                        bufferView = sparseValuesViewIndex,                       
                    }
                }
            });
            return accessorIndex;
        }
    }
}
