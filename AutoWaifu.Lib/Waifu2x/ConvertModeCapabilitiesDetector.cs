using Cudafy;
using Cudafy.Host;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    public class ConvertModeCapabilitiesDetector
    {
        static bool? CachedIsGpuAvailable = null;
        static bool? CachedIsCudnnAvailable = null;

        public bool IsCpuAvailable => true;

        /// <summary>
        /// Detects whether or not there is an attached CUDA-capable card. CUDA-only
        /// due to waifu2x-caffe only supporting CUDA at the time of writing.
        /// </summary>
        public bool IsGpuAvailable
        {
            get
            {
                if (CachedIsGpuAvailable.HasValue)
                    return CachedIsGpuAvailable.Value;

                try
                {
                    CudafyModule module = new CudafyModule();
                    GPGPU gpu = CudafyHost.GetDevice(eGPUType.Cuda);
                    CachedIsGpuAvailable = gpu != null;
                }
                catch
                {
                    CachedIsGpuAvailable = false;
                }

                return CachedIsGpuAvailable.Value;
            }
        }



        public bool IsCudnnAvailable
        {
            get
            {
                if (!IsGpuAvailable)
                    return false;

                if (CachedIsCudnnAvailable.HasValue)
                    return CachedIsCudnnAvailable.Value;

                CudafyModule module = new CudafyModule();
                GPGPU gpu = CudafyHost.GetDevice(eGPUType.Cuda);
                var architecture = gpu.GetArchitecture();

                //  Hackish way to detect cuDNN support, minimum
                //      required GPU is GTX-400 series, which has at least 5.0 support
                CachedIsCudnnAvailable = architecture == eArchitecture.sm_50 ||
                                         architecture == eArchitecture.sm_52;

                return CachedIsCudnnAvailable.Value;
            }
        }



    }
}
