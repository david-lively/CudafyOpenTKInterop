/*
 * CUDAfy OpenTK interop example
 * (c) 2013 David Lively (davidLively@gmail.com)
 * 
 */
using OpenTK.Graphics.OpenGL;

namespace Common.OpenGL
{
    /// <summary>
    /// collection of data needed to draw a model that uses a model with GL.DrawElements()
    /// </summary>
    public struct IndexedModel
    {
        /// <summary>
        /// GL handle to an ArrayBuffer containing vertex data
        /// </summary>
        public int VertexBuffer;
        /// <summary>
        /// number of vertices in ::VertexBuffer
        /// </summary>
        public int VertexCount;

        /// <summary>
        /// GL handle to an ElementArrayBuffer containing index data (defines faces in terms of the vertices in VertexBuffer)
        /// </summary>
        public int IndexBuffer;
        /// <summary>
        /// Number of indices in ::IndexBuffer
        /// </summary>
        public int IndexCount;

        /// <summary>
        /// Data type of the index buffer (short, int). The GPU uses this to determine how many bytes to grab for each index when drawing.
        /// </summary>
        public DrawElementsType IndexType;

        /// <summary>
        /// Type of primitive to draw - triangles by default.
        /// </summary>
        public BeginMode PrimitiveType;

        /// <summary>
        /// delete all of the GL resources used by this model.
        /// </summary>
        public void Delete()
        {
            if (GL.IsBuffer(VertexBuffer))
            {
                GL.DeleteBuffer(VertexBuffer);
                VertexBuffer = 0;
            }

            if (GL.IsBuffer(IndexBuffer))
            {
                GL.DeleteBuffer(IndexBuffer);
                IndexBuffer = 0;
            }
        
        }
    }
}
