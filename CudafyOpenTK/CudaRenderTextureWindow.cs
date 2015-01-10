using Common.Cuda;
using Common.OpenGL;
using Cudafy;
using Cudafy.Host;
using Cudafy.Translator;
using GASS.CUDA;
using GASS.CUDA.Types;
using GASS.Types;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Common
{
    /// <summary>
    /// uses a Cudafy kernel to create a simple texture, and renders it in an OpenTK window.
    /// </summary>
    public class CudaRenderTextureWindow : GameWindow
    {

        #region CUDA resources

        /// <summary>
        /// CUDAFY GPGPU reference
        /// </summary>
        private GPGPU _device;

        #endregion

        #region OpenGL resources

        #region texture

        /// <summary>
        /// struct containing some texture-specific information, 
        /// such as GL handles, CUDA resource handle, etc.
        /// </summary>
        private CudaTexture _texture;

        #endregion

        #region geometry

        /// <summary>
        /// a simple quad model rendered by OpenTK with the CUDA-generated texture
        /// </summary>
        private IndexedModel _quad;

        #endregion

        /// <summary>
        /// a compiled GLSL program used to render the CUDA output
        /// </summary>
        private Effect _effect;

        #endregion

        #region general resource management

        /// <summary>
        /// items that need disposing at shutdown
        /// </summary>
        private List<IDisposable> _disposables;

        #endregion

        #region initialization

        public CudaRenderTextureWindow()
            : base(800, 600)
        {
            _disposables = new List<IDisposable>();

            InitializeOpenGL();
            InitializeCuda();

            RenderFrame += MainWindow_RenderFrame;
            UpdateFrame += MainWindow_UpdateFrame;
            Resize += MainWindow_Resize;

            Console.WriteLine("Starting OpenTK draw + update loop.");
        }

        void MainWindow_Resize(object sender, EventArgs e)
        {
            GL.Viewport(ClientRectangle);

            /// set a projection matrix so we can control the near and far clip planes. Otherwise, the OpenGL geometry will be clipped
            /// and won't display in the viewport.
            _effect.ProjectionMatrix = Matrix4.CreateOrthographic(Width, Height, 1, 10000);

            /// scale quad to 3/4 of the height of the window and correct aspect ratio so the texture will be rendered correctly

            _effect.WorldMatrix = Matrix4.CreateScale(1,_texture.Height * 1f / _texture.Width, 1) * Matrix4.CreateScale(base.Height * 0.9f);
        }


        /// <summary>
        /// Initialize Cudafy and whatever resources it needs.
        /// </summary>
        private void InitializeCuda()
        {
            Console.WriteLine("Initializing CUDA");

            /// This hasn't been tested with OpenCL!
            _device = CudafyHost.GetDevice(eGPUType.Cuda);

            _disposables.Add(_device);

            CudafyTranslator.Language = eLanguage.Cuda;

            var module = CudafyTranslator.Cudafy();

            _device.LoadModule(module);

            CudaRT.GLSetGLDevice(_device.DeviceId);
            _device.SetCurrentContext();

            /// register the OpenGL pixel buffer (where our texture data is stored) with CUDA.
            /// At Update time, it'll also need to be "mapped" to a CUDA graphics resource before a kernel can write to it
            CudaRT.GraphicsGLRegisterBuffer(ref _texture.CudaPixelBufferResource, (uint)_texture.PixelBufferHandle, 0);

            Console.WriteLine("Done");
        }

        private void InitializeOpenGL()
        {
            Console.WriteLine("Initializing OpenGL");

            /// compile some shaders
            _effect = new Effect(@"OpenGL\GLSL\TextureShader.vert", @"OpenGL\GLSL\TextureShader.frag");

            /// move the quad 10 units away on the Z axis so it is inside of the 1---10000 clip volume set by the projectino matrix (OnResize handler)
            _effect.ViewMatrix = Matrix4.CreateTranslation(0, 0, -10f);

            _disposables.Add(_effect);
            var blockSize = new dim3(16,16);

            /// make sure the window dimensions are a multiple of the requested block size.
            Width = (int)(Math.Ceiling(Width * 1f /  blockSize.x) * blockSize.x);
            Height = (int)(Math.Ceiling(Height * 1f /  blockSize.x) * blockSize.x);

            /// create and initialize the texture object. CudaTexture automaticall sets this
            /// to a 4 component float texture (Red+Green+Blue+Alpha)
            _texture = new CudaTexture(
                Width
                , Height
                );

            Console.WriteLine("Creating a {0} x {1} texture, using {2} bytes"
                , _texture.Width
                , _texture.Height
                , _texture.Width * _texture.Height * _texture.BytesPerPixel
                );


            #region pixel buffer initialization

            /// allocate a buffer handle (also called a "name" in OpenGL documentation)
            _texture.PixelBufferHandle = GL.GenBuffer();

            /// set the buffer as active so we can allocate space for it. PixelUnpackBuffer
            /// indicates that pixel data will be written to the buffer, and used
            /// for a texture. It also indicates what type of buffer we're binding.
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _texture.PixelBufferHandle);

            /// allocate space for the buffer on the device
            GL.BufferData(
                BufferTarget.PixelUnpackBuffer
                , new IntPtr(_texture.Width * _texture.Height * _texture.BytesPerPixel)
                , IntPtr.Zero
                , BufferUsageHint.DynamicCopy
                );

            #endregion

            #region texture initialization

            /// allocate the texture object. This doesn't allocate any storage, since the
            /// PixelBuffer ("PBO") is already used for that.
            _texture.Handle = GL.GenTexture();

            /// attach the texture to the effect (shader).
            _effect.Texture = _texture.Handle;

            /// bind the texture so we can set some parameters
            GL.BindTexture(_texture.Target, _texture.Handle);

            /// give OpenGL some texture metadata needed for sampling
            GL.TexImage2D(
                _texture.Target
                , 0
                , _texture.InternalFormat
                , _texture.Width
                , _texture.Height
                , 0
                , _texture.Format
                , _texture.PixelType
                , IntPtr.Zero
                );


            /// specify type of interpolation. 
            const int glLinear = 9729;

            GL.TexParameterI(_texture.Target
                , TextureParameterName.TextureMinFilter
                , new[] { glLinear });

            GL.TexParameterI(_texture.Target
                , TextureParameterName.TextureMagFilter
                , new[] { glLinear });

            #endregion

            #region OpenGL geometry
            /// generate lists of vertices and indices defining a screen-aligned quad. Although
            /// we're displaying the CUDA-generated texture as 2D, we still need some geometry to
            /// render to tell OpenGL where it needs to be displayed.
            _quad = GeometryProvider.GenerateUnitQuad();

            #endregion

            Console.WriteLine("Done");
        }

        #endregion

        #region OpenGL

        /// <summary>
        /// application "time" passed to CUDA to control texture animation. 
        /// </summary>
        /// <remarks>
        /// In the sample, this is used as a phase offset for sin() and cos() calls.
        /// </remarks>
        private float _time = 0;


        /// <summary>
        /// draw the updated textured quad with OpenTK
        /// </summary>
        /// <remarks>
        /// This is pretty standard OpenGL / OpenTK code for drawing a model with a shader. 
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_RenderFrame(object sender, FrameEventArgs e)
        {
            /// clear the frame buffer to black
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            /// bind the pixel buffer object 
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _texture.PixelBufferHandle);
            /// bind the CUDA-generated texture
            GL.BindTexture(_texture.Target, _texture.Handle);

            /// Copy the CUDA-generated data in the pixel buffer to the texture (all happens on the GPU, so it's fast)
            /// Note: use GL.TexSubImage2D if you only want to update a portion of the target texture
            GL.TexImage2D(
                 _texture.Target
                 , 0
                 , _texture.InternalFormat
                 , _texture.Width
                 , _texture.Height
                 , 0
                 , _texture.Format
                 , _texture.PixelType
                 , IntPtr.Zero
                 );

            /// tell OpenGL that it should use a vertex array
            GL.Enable(EnableCap.VertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _quad.VertexBuffer);

            /// the demo only uses vertex positions - texture coordinates are calculated in the GLSL shader
            GL.VertexPointer(3, VertexPointerType.Float, Vector3.SizeInBytes, 0);

            /// We're using indexed geometry, so go ahead and tell GL about the index buffer
            GL.Enable(EnableCap.IndexArray);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _quad.IndexBuffer);

            /// bind the GLSL program and set shader uniform parameters (matrices, etc.)
            _effect.Bind();

            /// draw everything
            GL.DrawElements(
                _quad.PrimitiveType
                , _quad.IndexCount
                , _quad.IndexType
                , 0
                );

            /// swap front and back buffers to show our newly rendered frame
            SwapBuffers();
        }


        /// <summary>
        /// call CUDA to update the texture contents
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_UpdateFrame(object sender, FrameEventArgs e)
        {
            /// let CUDA update the texture
            _device.SetCurrentContext();

            /// allocate a cudaStream structure. We're not using more than one, but this 
            /// is required for the Cuda runtime calls we're using.
            var stream = new cudaStream();

            /// calculate grid and block sizes. 
            var blockSize = new dim3(16, 16);

            var gridSize = GetGridSize(_texture.Width, _texture.Height, ref blockSize);

            /// Tell CUDA runtime to map the texture's pixel buffer into its address space
            CudaRT.MapResource(ref stream, ref _texture.CudaPixelBufferResource);

            ///
            var pixelPointer = new CUdeviceptr();
            var size = new SizeT();

            /// get a device pointer to the mapped resource that we can pass the pixel buffer to our CUDAfy kernel. 
            CudaRT.ResourceGetMappedPointer(ref pixelPointer, ref size, _texture.CudaPixelBufferResource);

            Debug.Assert(
                size == _texture.Width * _texture.Height * _texture.BytesPerPixel
                , "Cuda runtime returned a buffer size different than that of the texture's pixel buffer"
                );

#if SIMPLE
            /// red/green fade texture, no animation
            _gpu.Launch(gridSize, blockSize)
                .SimpleTextureGenerator(
                pixelPointer
                );
#else
            /// scrolling sin-ish texture
            _device.Launch(gridSize, blockSize)
                .CudaRender(
                _time++ / 10f
                , pixelPointer
                );
#endif
            CudaRT.GraphicsUnmapResources(ref stream, ref _texture.CudaPixelBufferResource);

        }

        #endregion

        #region CUDA

        /// <summary>
        /// calculate grid dimensions based on texture size and block size.
        /// </summary>
        /// <param name="textureWidth"></param>
        /// <param name="textureHeight"></param>
        /// <param name="blockSize"></param>
        /// <param name="gridSize"></param>
        private dim3 GetGridSize(int textureWidth, int textureHeight, ref dim3 blockSize)
        {
            var gridSize = new dim3(_texture.Width / blockSize.x, _texture.Height / blockSize.y);

            /// make sure our selected block / grid sizes cover the whole texture
            if (blockSize.x * gridSize.x != _texture.Width
                ||
                blockSize.y * gridSize.y != _texture.Height)
                throw new Exception("Texture size should be a multiple of 16. Or, adjust grid/block dim to ensure that the entire texture will be covered");

            return gridSize;
        }



        /// <summary>
        /// uses CUDA to generate a texture with a funny rainbow/ribbon pattern 
        /// </summary>
        /// <param name="thread">Cudafy thread</param>
        /// <param name="time">time / phase offset to animate the texture</param>
        /// <param name="pixels">target array - contains 4x the number of texture pixels(texels), each float is one color component</param>
        [Cudafy]
        public static void CudaRender(GThread thread, float time, float[] pixels)
        {
            /// color component offsets. The input array is actually a float4 array - each
            /// element is a color component, not an entire texel.
            const int FLOATS_PER_TEXEL = 4;

            var textureWidth = thread.gridDim.x * thread.blockDim.x;
            var textureHeight = thread.gridDim.y * thread.blockDim.y;

            var centerX = textureWidth / 2f;
            var centerY = textureHeight / 2f;

            /// get grid/block coordinates, which are coords within the 2D Float4 version of the input pixel array
            var x = thread.get_global_id(0);
            var y = thread.get_global_id(1);

            /// calculate linear offset for the red component of the destination texel - y * width + x
            var texelOffset =
                y * textureWidth
                + x
                ;

            /// scale up by the size of a texel (each texel is stored as four sequential floats in the pixels[] array in RGBA order
            texelOffset *= FLOATS_PER_TEXEL;

            /// distance from this texel to the center of the texture
            var centerDistance = GMath.Sqrt(GMath.Pow(x - centerX, 2) + GMath.Pow(y - centerY, 2));

            /// add time as a phase offset to "scroll" everything in the X direction
            var red = GMath.Cos(time + centerDistance * 10f / textureWidth) * GMath.Sin(time + centerDistance * 1f / textureWidth) / 2 + 0.5f;

            /// write some color values
            /// red
            pixels[texelOffset] = red;

            /// green
            pixels[texelOffset + 1] = 0;

            /// blue
            pixels[texelOffset + 2] = 1 - red;

            /// alpha
            pixels[texelOffset + 3] = 1f;
        }


        /// <summary>
        /// build a simple texture that fades red from left to right, and green from top to bottom.
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="pixels"></param>
        [Cudafy]
        public static void SimpleTextureGenerator(GThread thread, float[] pixels)
        {
            var x = thread.get_global_id(0);
            var y = thread.get_global_id(1);

            var width = thread.get_global_size(0);
            var height = thread.get_global_size(1);

            var linearOffset = y * width + x;
            var texelStartIndex = linearOffset * 4; /// 4 array elements per color in  the expected RGBA float texture

            var red = x * 1f / width;
            var green = y * 1f / height;

            /// red
            pixels[texelStartIndex + 0] = red;
            /// green
            pixels[texelStartIndex + 1] = green;
            /// blue
            pixels[texelStartIndex + 2] = 0;
            /// alpha
            pixels[texelStartIndex + 3] = 1;

        }
        #endregion

        #region disposal

        /// <summary>
        /// clean up OpenGL and CUDA resources
        /// </summary>
        /// <param name="manual"></param>
        protected override void Dispose(bool manual)
        {
            if (manual)
            {

                if (_device != null && !_device.IsDisposed)
                {
                    _device.FreeAll();
                    _device.HostFreeAll();
                }

                _texture.Delete();
                _quad.Delete();

                foreach (var disposable in _disposables)
                    disposable.Dispose();

                _disposables.Clear();

            }

            base.Dispose(manual);
        }

        #endregion
    }
}
