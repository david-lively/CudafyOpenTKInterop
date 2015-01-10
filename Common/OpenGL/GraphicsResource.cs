
namespace Common.OpenGL
{
    /// <summary>
    /// provides some common items for GL resources (shaders, textures, etc.)
    /// </summary>
    public abstract class GraphicsResource : Disposable
    {
        /// <summary>
        /// GL Handle ("name") for this resource, typically obtained from a GL.GenXXXX call
        /// </summary>
        public int Handle { get; protected set; }

        public virtual void Bind()
        {
        }

        public virtual void Unbind()
        {
        }

    }
}
