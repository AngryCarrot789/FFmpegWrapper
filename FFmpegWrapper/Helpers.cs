﻿using System;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace FFmpeg.Wrapper {

    internal static unsafe class Helpers {
        public static unsafe string ErrorString(int errno) {
            byte* buf = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE + 1];
            ffmpeg.av_strerror(errno, buf, ffmpeg.AV_ERROR_MAX_STRING_SIZE);
            return Marshal.PtrToStringAnsi((IntPtr) buf);
        }

        public static int CheckError(this int errno) {
            if (errno < 0 && errno != ffmpeg.EAGAIN && errno != ffmpeg.AVERROR_EOF) {
                ThrowError(errno);
            }

            return errno;
        }

        public static int CheckError(this int errno, string msg) {
            if (errno < 0 && errno != ffmpeg.EAGAIN && errno != ffmpeg.AVERROR_EOF) {
                ThrowError(errno, msg);
            }

            return errno;
        }

        public static Exception ThrowError(this int errno, string msg = null) {
            msg = msg ?? "Operation failed";
            throw new InvalidOperationException(msg + ": " + ErrorString(errno));
        }

        public static ReadOnlySpan<T> GetSpanFromSentinelTerminatedPtr<T>(T* ptr, T terminator) where T : unmanaged {
            int len = 0;

            while (ptr != null && !ptr[len].Equals(terminator)) {
                len++;
            }

            return new ReadOnlySpan<T>(ptr, len);
        }

        public static long? GetPTS(long pts) => pts != ffmpeg.AV_NOPTS_VALUE ? (long?) pts : null;
        public static void SetPTS(ref long pts, long? value) => pts = value ?? ffmpeg.AV_NOPTS_VALUE;

        public static TimeSpan? GetTimeSpan(long pts, AVRational timeBase) {
            if (pts == ffmpeg.AV_NOPTS_VALUE) {
                return null;
            }

            long ticks = ffmpeg.av_rescale_q(pts, timeBase, new AVRational() {num = 1, den = (int) TimeSpan.TicksPerSecond});
            return TimeSpan.FromTicks(ticks);
        }
    }
}