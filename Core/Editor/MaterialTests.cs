using NUnit.Framework;


namespace UMa.GLTF
{
    public class MaterialTests
    {
        [Test]
        public void UnlitShaderImportTest()
        {
            var shaderStore = new ShaderStore(null);

            {
                // OPAQUE/Color
                var shader = shaderStore.GetShader(new GLTFMaterial
                {
                    alphaMode = "OPAQUE",
                    pbrMetallicRoughness = new GLTFPbrMetallicRoughness
                    {
                        baseColorFactor = new float[] { 1, 0, 0, 1 },
                    },
                    extensions = new GLTFMaterialExtensions
                    {
                        KHR_materials_unlit = new KHRMaterialUnlit { }
                    }
                });
                Assert.AreEqual("UniGLTF/UniUnlit", shader.name);
            }

            {
                // OPAQUE/Texture
                var shader = shaderStore.GetShader(new GLTFMaterial
                {
                    alphaMode = "OPAQUE",
                    pbrMetallicRoughness = new GLTFPbrMetallicRoughness
                    {
                        baseColorTexture = new GLTFMaterialBaseColorTextureInfo(),
                    },
                    extensions = new GLTFMaterialExtensions
                    {
                        KHR_materials_unlit = new KHRMaterialUnlit { }
                    }
                });
                Assert.AreEqual("UniGLTF/UniUnlit", shader.name);
            }

            {
                // OPAQUE/Color/Texture
                var shader = shaderStore.GetShader(new GLTFMaterial
                {
                    alphaMode = "OPAQUE",
                    pbrMetallicRoughness = new GLTFPbrMetallicRoughness
                    {
                        baseColorFactor = new float[] { 1, 0, 0, 1 },
                        baseColorTexture = new GLTFMaterialBaseColorTextureInfo(),
                    },
                    extensions = new GLTFMaterialExtensions
                    {
                        KHR_materials_unlit = new KHRMaterialUnlit { }
                    }
                });
                Assert.AreEqual("UniGLTF/UniUnlit", shader.name);
            }

            {
                // BLEND/Color
                var shader = shaderStore.GetShader(new GLTFMaterial
                {
                    alphaMode = "BLEND",
                    pbrMetallicRoughness = new GLTFPbrMetallicRoughness
                    {
                        baseColorFactor = new float[] { 1, 0, 0, 1 },
                    },
                    extensions = new GLTFMaterialExtensions
                    {
                        KHR_materials_unlit = new KHRMaterialUnlit { }
                    }
                });
                Assert.AreEqual("UniGLTF/UniUnlit", shader.name);
            }

            {
                // BLEND/Texture
                var shader = shaderStore.GetShader(new GLTFMaterial
                {
                    alphaMode = "BLEND",
                    pbrMetallicRoughness = new GLTFPbrMetallicRoughness
                    {
                        baseColorTexture = new GLTFMaterialBaseColorTextureInfo(),
                    },
                    extensions = new GLTFMaterialExtensions
                    {
                        KHR_materials_unlit = new KHRMaterialUnlit { }
                    }
                });
                Assert.AreEqual("UniGLTF/UniUnlit", shader.name);
            }

            {
                // BLEND/Color/Texture
                var shader = shaderStore.GetShader(new GLTFMaterial
                {
                    alphaMode = "BLEND",
                    pbrMetallicRoughness = new GLTFPbrMetallicRoughness
                    {
                        baseColorFactor = new float[] { 1, 0, 0, 1 },
                        baseColorTexture = new GLTFMaterialBaseColorTextureInfo(),
                    },
                    extensions = new GLTFMaterialExtensions
                    {
                        KHR_materials_unlit = new KHRMaterialUnlit { }
                    }
                });
                Assert.AreEqual("UniGLTF/UniUnlit", shader.name);
            }

            {
                // MASK/Texture
                var shader = shaderStore.GetShader(new GLTFMaterial
                {
                    alphaMode = "MASK",
                    pbrMetallicRoughness = new GLTFPbrMetallicRoughness
                    {
                        baseColorTexture = new GLTFMaterialBaseColorTextureInfo(),
                    },
                    extensions = new GLTFMaterialExtensions
                    {
                        KHR_materials_unlit = new KHRMaterialUnlit { }
                    }
                });
                Assert.AreEqual("UniGLTF/UniUnlit", shader.name);
            }

            {
                // MASK/Color/Texture
                var shader = shaderStore.GetShader(new GLTFMaterial
                {
                    alphaMode = "MASK",
                    pbrMetallicRoughness = new GLTFPbrMetallicRoughness
                    {
                        baseColorFactor = new float[] { 1, 0, 0, 1 },
                        baseColorTexture = new GLTFMaterialBaseColorTextureInfo(),
                    },
                    extensions = new GLTFMaterialExtensions
                    {
                        KHR_materials_unlit = new KHRMaterialUnlit { }
                    }
                });
                Assert.AreEqual("UniGLTF/UniUnlit", shader.name);
            }

            {
                // default
                var shader = shaderStore.GetShader(new GLTFMaterial
                {
                    extensions = new GLTFMaterialExtensions
                    {
                        KHR_materials_unlit = new KHRMaterialUnlit { }
                    }
                });
                Assert.AreEqual("UniGLTF/UniUnlit", shader.name);
            }
        }

        [Test]
        public void MaterialImportTest()
        {
            var shaderStore = new ShaderStore(null);
            var materialImporter = new MaterialImporter(shaderStore, null);

            {
                var material = materialImporter.CreateMaterial(0, new GLTFMaterial
                {

                });
                Assert.AreEqual("Standard", material.shader.name);
            }
        }
    }
}
