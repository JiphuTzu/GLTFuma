using System;


namespace UMa.GLTF
{
    public class GLTFumaException : Exception
    {
        public GLTFumaException(string fmt, params object[] args) : this(string.Format(fmt, args)) { }
        public GLTFumaException(string msg) : base(msg) { }
    }

    public class GLTFumaNotSupportedException : GLTFumaException
    {
        public GLTFumaNotSupportedException(string fmt, params object[] args) : this(string.Format(fmt, args)) { }
        public GLTFumaNotSupportedException(string msg) : base(msg) { }
    }
}
