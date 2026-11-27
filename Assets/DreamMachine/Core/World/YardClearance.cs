using System;

namespace DreamMachine.World
{
    /// <summary>Conservative swept ground-plane clearance for the fixed yard props and house.</summary>
    public static class YardClearance
    {
        public const float PropStartX = -4.25f, PropStartZ = -1.5f, PropSpacing = 1.7f;
        public const int Columns = 6, Rows = 4;
        public const float HouseHalfX = 1.2f, HouseHalfZ = 0.8f, HouseZ = -1.4f;

        public static float MotionFraction(float x, float z, float dx, float dz, float radius)
        {
            if (dx == 0f && dz == 0f) return 1f;
            float hit = 1f;
            float left = Math.Min(x, x + dx) - radius, right = Math.Max(x, x + dx) + radius;
            float back = Math.Min(z, z + dz) - radius, front = Math.Max(z, z + dz) + radius;
            if (left < HouseHalfX && right > -HouseHalfX && back < HouseZ + HouseHalfZ && front > HouseZ - HouseHalfZ)
                hit = EnterBox(x, z - HouseZ, dx, dz, HouseHalfX + radius, HouseHalfZ + radius);
            const float propRadius = 0.30f;
            if (left > PropStartX + (Columns - 1) * PropSpacing + propRadius || right < PropStartX - propRadius ||
                back > PropStartZ + (Rows - 1) * PropSpacing + propRadius || front < PropStartZ - propRadius) return hit;
            int x0 = Math.Max(0, -(int)Math.Floor((PropStartX - left + propRadius) / PropSpacing));
            int x1 = Math.Min(Columns - 1, (int)Math.Floor((right + propRadius - PropStartX) / PropSpacing));
            int z0 = Math.Max(0, -(int)Math.Floor((PropStartZ - back + propRadius) / PropSpacing));
            int z1 = Math.Min(Rows - 1, (int)Math.Floor((front + propRadius - PropStartZ) / PropSpacing));
            float size = radius + propRadius;
            float a = dx * dx + dz * dz;
            for (int row = z0; row <= z1; row++)
            for (int column = x0; column <= x1; column++)
            {
                float ox = x - (PropStartX + column * PropSpacing), oz = z - (PropStartZ + row * PropSpacing);
                float b = ox * dx + oz * dz;
                if (b >= 0f) continue; // Retreat from an overlap in a legacy save is permitted.
                float c = ox * ox + oz * oz - size * size;
                if (c <= 0f) return 0f;
                float discriminant = b * b - a * c;
                if (discriminant <= 0f) continue;
                hit = Math.Min(hit, Math.Max(0f, (-b - (float)Math.Sqrt(discriminant)) / a - 0.0001f));
            }
            return hit;
        }

        static float EnterBox(float x, float z, float dx, float dz, float halfX, float halfZ)
        {
            if (Math.Abs(x) < halfX && Math.Abs(z) < halfZ)
            {
                // Allow only movement toward the closest exit when loading an old overlap.
                bool towardExit = halfX - Math.Abs(x) < halfZ - Math.Abs(z) ? x * dx > 0 : z * dz > 0;
                return towardExit ? 1f : 0f;
            }
            float enter = 0f, leave = 1f;
            if (!ClipAxis(x, dx, halfX, ref enter, ref leave) || !ClipAxis(z, dz, halfZ, ref enter, ref leave)) return 1f;
            return leave > 0f ? Math.Max(0f, enter - 0.0001f) : 1f;
        }

        static bool ClipAxis(float start, float delta, float bound, ref float enter, ref float leave)
        {
            if (delta == 0f) return Math.Abs(start) <= bound;
            float a = (-bound - start) / delta, b = (bound - start) / delta;
            enter = Math.Max(enter, Math.Min(a, b));
            leave = Math.Min(leave, Math.Max(a, b));
            return enter <= leave;
        }
    }
}
