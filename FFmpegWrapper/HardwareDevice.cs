using System;
using FFmpeg.AutoGen;

namespace FFmpeg.Wrapper {

    public unsafe class HardwareDevice : FFObject {
        private AVBufferRef* _ctx;

        public AVBufferRef* Handle {
            get {
                ThrowIfDisposed();
                return _ctx;
            }
        }

        public AVHWDeviceContext* RawHandle {
            get {
                ThrowIfDisposed();
                return (AVHWDeviceContext*) _ctx->data;
            }
        }

        public AVHWDeviceType Type => RawHandle->type;

        public HardwareDevice(AVBufferRef* deviceCtx) {
            _ctx = deviceCtx;
        }

        public static HardwareDevice Create(AVHWDeviceType type) {
            AVBufferRef* ctx;
            if (ffmpeg.av_hwdevice_ctx_create(&ctx, type, null, null, 0) < 0) {
                return null;
            }

            return new HardwareDevice(ctx);
        }

        public HardwareFrameConstraints GetMaxFrameConstraints() {
            var desc = ffmpeg.av_hwdevice_get_hwframe_constraints(_ctx, null);
            var managedDesc = new HardwareFrameConstraints(desc);
            ffmpeg.av_hwframe_constraints_free(&desc);
            return managedDesc;
        }

        /// <param name="swFormat"> The pixel format identifying the actual data layout of the hardware frames. </param>
        /// <param name="initialSize"> Initial size of the frame pool. If a device type does not support dynamically resizing the pool, then this is also the maximum pool size. </param>
        public HardwareFramePool CreateFramePool(PictureFormat swFormat, int initialSize) {
            ThrowIfDisposed();

            var poolRef = ffmpeg.av_hwframe_ctx_alloc(_ctx);
            if (poolRef == null) {
                throw new OutOfMemoryException("Failed to allocate hardware frame pool");
            }

            var pool = (AVHWFramesContext*) poolRef->data;
            pool->format = GetDefaultSurfaceFormat();
            pool->sw_format = swFormat.PixelFormat;
            pool->width = swFormat.Width;
            pool->height = swFormat.Height;
            pool->initial_pool_size = initialSize;

            if (ffmpeg.av_hwframe_ctx_init(poolRef) < 0) {
                ffmpeg.av_buffer_unref(&poolRef);
                return null;
            }

            return new HardwareFramePool(poolRef);
        }

        private AVPixelFormat GetDefaultSurfaceFormat() {
            switch(Type) {
                case HWDeviceTypes.VDPAU:        return AVPixelFormat.AV_PIX_FMT_VDPAU;
                case HWDeviceTypes.Cuda:         return AVPixelFormat.AV_PIX_FMT_CUDA;
                case HWDeviceTypes.VAAPI:        return AVPixelFormat.AV_PIX_FMT_VAAPI;
                case HWDeviceTypes.DXVA2:        return AVPixelFormat.AV_PIX_FMT_DXVA2_VLD;
                case HWDeviceTypes.QSV:          return AVPixelFormat.AV_PIX_FMT_QSV;
                case HWDeviceTypes.D3D11VA:      return AVPixelFormat.AV_PIX_FMT_D3D11;
                case HWDeviceTypes.DRM:          return AVPixelFormat.AV_PIX_FMT_DRM_PRIME;
                case HWDeviceTypes.OpenCL:       return AVPixelFormat.AV_PIX_FMT_OPENCL;
                case HWDeviceTypes.Vulkan:       return AVPixelFormat.AV_PIX_FMT_VULKAN;
                case HWDeviceTypes.VideoToolbox: return AVPixelFormat.AV_PIX_FMT_VIDEOTOOLBOX;
                case HWDeviceTypes.MediaCodec:   return AVPixelFormat.AV_PIX_FMT_MEDIACODEC;
                default: throw new ArgumentOutOfRangeException(nameof(Type), "Type is out of range");
            }
        }

        protected override void Free() {
            if (_ctx != null) {
                fixed (AVBufferRef** ppCtx = &_ctx) {
                    ffmpeg.av_buffer_unref(ppCtx);
                }
            }
        }

        private void ThrowIfDisposed() {
            if (_ctx == null) {
                throw new ObjectDisposedException(nameof(HardwareDevice));
            }
        }
    }
}