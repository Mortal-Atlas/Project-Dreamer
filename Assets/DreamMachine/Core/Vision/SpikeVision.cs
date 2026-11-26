using System;

namespace DreamMachine.Vision
{
    /// <summary>
    /// Turns a tiny grayscale eye into spike counts.
    /// This is the in-tick path. It does not call Python.
    /// The Brian2 batch script is an offline STDP pass over saved frames.
    /// </summary>
    public static class SpikeVision
    {
        public static int Encode(byte[] gray, int width, int height, float[] spikes, float threshold)
        {
            if (gray == null || spikes == null || width <= 0 || height <= 0) return 0;
            int n = width * height;
            if (n > gray.Length) n = gray.Length;
            if (n > spikes.Length) n = spikes.Length;
            int fired = 0;
            for (int i = 0; i < n; i++)
            {
                float rate = gray[i] / 255f;
                float spike = rate >= threshold ? rate : 0f;
                spikes[i] = spike;
                if (spike > 0f) fired++;
            }
            for (int i = n; i < spikes.Length; i++) spikes[i] = 0f;
            return fired;
        }

        public static void Downscale(byte[] src, int srcW, int srcH, byte[] dst, int dstW, int dstH)
        {
            if (src == null || dst == null || srcW <= 0 || dstW <= 0) return;
            for (int y = 0; y < dstH; y++)
            {
                int sy = y * srcH / dstH;
                if (sy >= srcH) sy = srcH - 1;
                for (int x = 0; x < dstW; x++)
                {
                    int sx = x * srcW / dstW;
                    if (sx >= srcW) sx = srcW - 1;
                    dst[y * dstW + x] = src[sy * srcW + sx];
                }
            }
        }
    }
}
