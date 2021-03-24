using System;
using UniJSON;
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
    [Serializable]
    public class GLTFScene : JsonSerializableBase
    {
        [JsonSchema(MinItems = 1),ItemJsonSchema(Minimum = 0)]
        public int[] nodes;

        public object extensions;
        public object extras;
        public string name;

        protected override void SerializeMembers(GLTFJsonFormatter f)
        {
            f.KeyValue(() => nodes);
        }
    }
}
