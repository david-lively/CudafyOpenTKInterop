using GASS.CUDA;
using OpenTK.Graphics.OpenGL;
using System;

namespace Common.OpenGL
{
    public struct CudaTexture
    {
        /// <summary>
        /// OpenGL handle to the texture object
        /// </summary>
        public int Handle;
        /// <summary>
        ///  width of the texture in pixels
        /// </summary>
        public int Width;
        /// <summary>
        /// height of the texture in pixels
        /// </summary>
        public int Height;

        /// <summary>
        /// Target, or 
        /// </summary>
        public TextureTarget Target;

        /// <summary>
        /// Basically, this defines the number of elements in the texture. 
        /// </summary>
        public PixelFormat Format;

        /// <summary>
        /// Element type of pixels in the texture - float, int, etc. 
        /// </summary>
        public PixelType PixelType;
        /// <summary>
        /// Yet another pixel format indicator, which honestly is kind of redundant
        /// </summary>
        public PixelInternalFormat InternalFormat;

        /// <summary>
        /// number of bytes required for each pixel
        /// </summary>
        public int BytesPerPixel;

        public int PixelBufferHandle;

        public cudaGraphicsResource CudaPixelBufferResource;

        public CudaTexture(
            int width
            , int height
            , PixelType pixelType = PixelType.Float
            , PixelFormat format = PixelFormat.Rgba
            , PixelInternalFormat internalFormat = PixelInternalFormat.Rgba32f
            , TextureTarget textureTarget = TextureTarget.Texture2D
            , int bytesPerPixel = 4 * sizeof(float)
            )
        {
            Handle = -1;
            PixelBufferHandle = -1;

            Width = width;
            Height = height;
            
            Target = textureTarget;

            PixelType = pixelType;
            Format = format;
            InternalFormat = internalFormat;

            BytesPerPixel = bytesPerPixel;

            CudaPixelBufferResource = new cudaGraphicsResource() { Pointer = IntPtr.Zero };
        }



        /// <summary>
        /// clean up the texture.
        /// </summary>
        public void Delete()
        {
            if (GL.IsTexture(Handle))
            {
                GL.DeleteTexture(Handle);
                Handle = 0;
            }

            if (GL.IsBuffer(PixelBufferHandle))
            {
                GL.DeleteBuffer(PixelBufferHandle);
                PixelBufferHandle = 0;
            }
        }
    }
}
