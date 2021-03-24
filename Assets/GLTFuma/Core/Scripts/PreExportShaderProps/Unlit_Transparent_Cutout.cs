using System.Collections.Generic;


namespace UMa.GLTF.ShaderPropExporter
{
    public static partial class PreShaderPropExporter
    {
        [PreExportShader]
        static KeyValuePair<string, ShaderProps> Unlit_Transparent_Cutout 
        {
            get 
            {
                return new KeyValuePair<string, ShaderProps>(
                    "Unlit/Transparent Cutout",
                    new ShaderProps
                    {
                        Properties = new ShaderProperty[]{
new ShaderProperty("_MainTex", ShaderPropertyType.TexEnv)
,new ShaderProperty("_Cutoff", ShaderPropertyType.Range)

                        }
                    }
                );
            }
        }
    }
}
