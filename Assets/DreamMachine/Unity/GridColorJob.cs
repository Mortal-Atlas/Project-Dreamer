using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace DreamMachine.UnityBridge
{
    /// <summary>
    /// Colors the ground overlay from the sim grid. This is view work.
    /// The simulation itself stays in the shared C# kernel so fast-forward
    /// cannot drift from the headless server.
    /// </summary>
    [BurstCompile]
    public struct GridColorJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> Temperature;
        [ReadOnly] public NativeArray<float> Water;
        [ReadOnly] public NativeArray<float> Smoke;
        public NativeArray<float> Rgba;

        public void Execute(int index)
        {
            float temp = Temperature[index];
            float water = Water[index];
            float smoke = Smoke[index];
            float r = 0.15f;
            float g = 0.35f;
            float b = 0.2f;
            if (temp > 80f)
            {
                float heat = (temp - 80f) / 400f;
                if (heat > 1f) heat = 1f;
                r = 0.4f + 0.6f * heat;
                g = 0.15f * (1f - heat);
                b = 0.02f;
            }
            if (water > 0.08f)
            {
                r = 0.1f;
                g = 0.35f;
                b = 0.75f;
            }
            if (smoke > 0.05f)
            {
                float s = smoke > 1f ? 1f : smoke;
                r = r * (1f - s) + 0.45f * s;
                g = g * (1f - s) + 0.45f * s;
                b = b * (1f - s) + 0.48f * s;
            }
            int o = index * 4;
            Rgba[o] = r;
            Rgba[o + 1] = g;
            Rgba[o + 2] = b;
            Rgba[o + 3] = 1f;
        }
    }
}
