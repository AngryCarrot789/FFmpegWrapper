using System;
using System.IO;
using FFmpeg.AutoGen;

namespace FFmpeg.Wrapper {

    public abstract unsafe class IOContext : FFObject {
        private AVIOContext* _ctx;

        public AVIOContext* Handle {
            get {
                ThrowIfDisposed();
                return _ctx;
            }
        }

        public bool CanRead { get; }
        public bool CanWrite { get; }
        public bool CanSeek => _ctx->seekable != 0;

        //Keep lambda refs to prevent them from being GC collected
        private avio_alloc_context_read_packet _readFn;
        private avio_alloc_context_write_packet _writeFn;
        private avio_alloc_context_seek _seekFn;

        public IOContext(int bufferSize, bool canRead, bool canWrite, bool canSeek) {
            var buffer = (byte*) ffmpeg.av_mallocz((ulong) bufferSize);
            if (canRead) _readFn = ReadBridge;
            if (canWrite) _writeFn = WriteBridge;
            if (canSeek) _seekFn = SeekBridge;

            _ctx = ffmpeg.avio_alloc_context(
                buffer, bufferSize, canWrite ? 1 : 0, null,
                _readFn, _writeFn, _seekFn
            );
        }

        private int ReadBridge(void* opaque, byte* buffer, int length) {
            int bytesRead = Read(new Span<byte>(buffer, length));
            return bytesRead > 0 ? bytesRead : ffmpeg.AVERROR_EOF;
        }

        private int WriteBridge(void* opaque, byte* buffer, int length) {
            Write(new ReadOnlySpan<byte>(buffer, length));
            return length;
        }

        private long SeekBridge(void* opaque, long offset, int whence) {
            if (whence == ffmpeg.AVSEEK_SIZE) {
                return GetLength() ?? ffmpeg.AVERROR(38); //ENOSYS
            }

            return Seek(offset, (SeekOrigin) whence);
        }

        /// <summary> Reads data from the underlying stream to <paramref name="buffer"/>. </summary>
        /// <returns>The number of bytes read. </returns>
        protected abstract int Read(Span<byte> buffer);

        /// <summary> Writes data to the underlying stream. </summary>
        protected abstract void Write(ReadOnlySpan<byte> buffer);

        /// <summary> Sets the position of the underlying stream. </summary>
        protected abstract long Seek(long offset, SeekOrigin origin);

        protected virtual long? GetLength() => null;

        protected override void Free() {
            if (_ctx != null) {
                ffmpeg.av_free(_ctx->buffer);
                fixed (AVIOContext** c = &_ctx)
                    ffmpeg.avio_context_free(c);
            }
        }

        protected void ThrowIfDisposed() {
            if (_ctx == null) {
                throw new ObjectDisposedException(nameof(IOContext));
            }
        }
    }
}