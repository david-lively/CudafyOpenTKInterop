using GASS.CUDA;
using GASS.CUDA.Types;
using GASS.Types;
using System;
using System.Runtime.InteropServices;

namespace Common.Cuda
{
    /// <summary>
    /// provides some Cuda Runtime imports that are missing or incorrect in the Cudafy.NET dll.
    /// </summary>
    public static class CudaRT
    {
#warning change the DLL name here depending on 64- or 32-bit compilation.
        /// use 64-bit CUDA DLL
        internal const string CUDART_DLL_NAME = "cudart64_50_35";

        /// use 32-bit CUDA DLL
        //internal const string CUDART_DLL_NAME = "cudart32_50_35";


        #region imports from CUDA runtime DLL

        [DllImport(CUDART_DLL_NAME)]
        private static extern cudaError cudaGraphicsGLRegisterBuffer(ref cudaGraphicsResource resource, uint buffer, uint Flags);

        [DllImport(CUDART_DLL_NAME)]
        private static extern cudaError cudaGLSetGLDevice(int deviceId);

        [DllImport(CUDART_DLL_NAME)]
        public static extern cudaError cudaGraphicsMapResources(int count, ref cudaGraphicsResource resources, cudaStream stream);

        [DllImport(CUDART_DLL_NAME)]
        private extern static cudaError cudaGraphicsUnmapResources(int count, ref cudaGraphicsResource resources, cudaStream stream);

        [DllImport(CUDART_DLL_NAME)]
        private static extern cudaError cudaGraphicsResourceGetMappedPointer(ref CUdeviceptr devPtr, ref SizeT size, cudaGraphicsResource resource);


        #endregion

        #region error handling

        /// <summary>
        /// verify that a CUDA call returned "success," or break.
        /// </summary>
        /// <param name="err"></param>
        public static void CheckError(cudaError err)
        {
            if (err != cudaError.cudaSuccess)
                throw new Exception(string.Format("Error when registering GL texture with CUDA runtime: {0}",err));
        }


        /// <summary>
        /// cudaGraphicsGLRegisterBuffer
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="buffer"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static void GraphicsGLRegisterBuffer(ref cudaGraphicsResource resource, uint buffer, uint flags)
        {
            CheckError(cudaGraphicsGLRegisterBuffer(ref resource, buffer, flags));
        }

        /// <summary>
        /// cudaGraphicsMapResources
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public static void MapResource(ref cudaStream stream, ref cudaGraphicsResource resource)
        {
            CheckError(cudaGraphicsMapResources(1, ref resource, stream));
        }

        public static void GraphicsUnmapResources(ref cudaStream stream, ref cudaGraphicsResource resource)
        {
            CheckError(cudaGraphicsUnmapResources(1, ref resource, stream));
            //CheckError(cudaGraphicsUnregisterResource(ref resource));
        }

        public static void ResourceGetMappedPointer(ref CUdeviceptr devPtr, ref SizeT size, cudaGraphicsResource resource)
        {
            CheckError(cudaGraphicsResourceGetMappedPointer(ref devPtr, ref size, resource));
        }



        /// <summary>
        /// cudaGLSetGLDevice
        /// </summary>
        /// <param name="deviceId"></param>
        public static void GLSetGLDevice(int deviceId)
        {
            CheckError(cudaGLSetGLDevice(deviceId));
        }



        #endregion
    }
}
