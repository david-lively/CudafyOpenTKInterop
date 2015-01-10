using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Common.OpenGL
{
    /// <summary>
    /// Provides methods to generate OpenGL models, defined by vertex and index lists. 
    /// </summary>
    public static class GeometryProvider
    {
        /// <summary>
        /// Creates a quad in the X-Y plane (screen-aligned). OpenGL buffers are created and initialized
        /// </summary>
        /// <returns>
        /// A struct containing all of the information needed to draw the quad.
        /// </returns>
        /// <remarks>
        /// 
        /// v0----------v1
        /// |          / |
        /// | face 0 /   |
        /// |      /     |
        /// |    /       |
        /// |  /  face 1 |
        /// |/           |
        /// v3----------v2
        /// Triangle definitions are:
        /// 
        /// face 0: {v0, v1, v3 }
        /// face 1: {v1, v2, v3 |
        ///
        /// vertex buffer: {v0,v1,v2,v3}
        /// index buffer: {0,1,3, 1,2,3}
        /// 
        /// Don't forget to call .Delete() when shutting down to free up these GL resources.
        /// </remarks>
        public static IndexedModel GenerateUnitQuad()
        {
            /// half-width of the model. In this case, the quad will be scaled to 
            /// fit a unit cube centered at the origin. This makes it easy to predict
            /// how a scale factor in the world matrix (passed to the Effect which
            /// draws the model) will affect the screen size of the rendered model.
            var halfWidth = 0.5f;

            var depth = 0f;
            //vertex positions - clockwise starting from 10:30PM
            var vertices = new[] {
                new Vector3( -halfWidth, halfWidth, depth)
                ,new Vector3( halfWidth, halfWidth, depth)
                ,new Vector3( halfWidth,-halfWidth, depth)
                ,new Vector3(-halfWidth,-halfWidth, depth)
            };

            /// indices into the vertex list. Three indices define a triangle, and the quad
            /// consists of two triangles. 
            var indices = new short[] {
                0,1,2
                ,
                2,3,0
            }
            ;

            /// Allocate OpenGL buffers to hold the vertex and index lists
            var vertexBuffer = GenerateBuffer(BufferTarget.ArrayBuffer, ref vertices);
            var indexBuffer = GenerateBuffer(BufferTarget.ElementArrayBuffer, ref indices);

            /// generate a struct with the metadata required to render the model.
            /// These are passed to the OpenGL Render() calls later.
            var model = new IndexedModel()
            {
                IndexType = DrawElementsType.UnsignedShort
                ,
                VertexBuffer = vertexBuffer
                ,
                VertexCount = vertices.Length
                ,
                IndexBuffer = indexBuffer
                ,
                IndexCount = indices.Length
                ,
                PrimitiveType = BeginMode.Triangles

            };


            return model;
        }

        /// <summary>
        /// defines the geometry for a two-triangle, full-screen quad. This is useful for displaying a completely rendered
        /// scene generated in CUDA
        /// </summary>
        /// <returns></returns>
        public static IndexedModel FullScreenQuad()
        {
            /// vertex positions. This can be cast to a Vector3[] array, or passed as-is to GenerateBuffer.
            /// Each span of three values defines a single vertex in 3D space.
            var vertexComponents = new float[]
            {
                /// x0,y0,z0, ... xn,yn,zn
                -1,1,0
                ,
                1,1,0
                ,
                1,-1,0
                ,
                -1,1,0
            };

            /// index list that defines how the vertices are connected. 
            var indices = new[] { 0, 1, 2, 2, 3, 0 }.Select(intIndex => (short)intIndex).ToArray();

            var vertexBuffer = GenerateBuffer(BufferTarget.ArrayBuffer, ref vertexComponents);
            var indexBuffer = GenerateBuffer(BufferTarget.ElementArrayBuffer, ref indices);

            var model = new IndexedModel()
            {
                IndexType = DrawElementsType.UnsignedShort
                ,
                VertexBuffer = vertexBuffer
                ,
                VertexCount = vertexComponents.Length / 3
                ,
                IndexBuffer = indexBuffer
                ,
                IndexCount = indices.Length
                ,
                PrimitiveType = BeginMode.Triangles
            };

            return model;

        }


        /// <summary>
        /// generate a buffer and populate it with the given data.
        /// </summary>
        /// <remarks>
        /// The process for creating a vertex, index, or texture buffer is identitical, so herein it is thusly encapsulated.
        /// </remarks>
        /// <typeparam name="T">Type of data structure used for this buffer</typeparam>
        /// <param name="target">Which buffer we are populating</param>
        /// <param name="values">Linear array of values to send to OpenGL</param>
        /// <returns></returns>
        private static int GenerateBuffer<T>(BufferTarget target, ref T[] values) where T : struct
        {
            /// allocate the buffer
            /// 
            var handle = GL.GenBuffer();

            GL.BindBuffer(target, handle);

            /// populate it with our vertex data
            GL.BufferData(
                target
                , (IntPtr)(values.Length * Marshal.SizeOf(values[0]))
                , values
                , BufferUsageHint.StaticDraw
                );

            return handle;
        }


    }
}
