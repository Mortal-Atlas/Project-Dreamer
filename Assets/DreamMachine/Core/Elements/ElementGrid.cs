using System;

namespace DreamMachine.Elements
{
    /// <summary>
    /// Coarse yard grid: fire, water, ice, steam, and smoke.
    /// One set of arrays, one scratch buffer, no per-tick allocation.
    /// </summary>
    public sealed class ElementGrid
    {
        public const byte Air = 0;
        public const byte Wood = 1;
        public const byte Stone = 2;
        public const byte IceBlock = 3;

        public readonly int Width;
        public readonly int Height;
        public readonly float[] Temperature;
        public readonly float[] Fuel;
        public readonly float[] Oxygen;
        public readonly float[] Water;
        public readonly float[] Ice;
        public readonly float[] Steam;
        public readonly float[] Smoke;
        public readonly byte[] Material;
        public readonly byte[] Burning;
        readonly float[] _scratch;
        readonly float[] _scratchB;
        public float WindX = 0.15f;
        public float WindY = 0.05f;

        internal void WriteCheckpoint(System.IO.BinaryWriter writer)
        {
            writer.Write(WindX);
            writer.Write(WindY);
            for (int i = 0; i < Temperature.Length; i++) writer.Write(Temperature[i]);
            for (int i = 0; i < Fuel.Length; i++) writer.Write(Fuel[i]);
            for (int i = 0; i < Oxygen.Length; i++) writer.Write(Oxygen[i]);
            for (int i = 0; i < Water.Length; i++) writer.Write(Water[i]);
            for (int i = 0; i < Ice.Length; i++) writer.Write(Ice[i]);
            for (int i = 0; i < Steam.Length; i++) writer.Write(Steam[i]);
            for (int i = 0; i < Smoke.Length; i++) writer.Write(Smoke[i]);
            for (int i = 0; i < Material.Length; i++) writer.Write(Material[i]);
            for (int i = 0; i < Burning.Length; i++) writer.Write(Burning[i]);
        }

        internal void ReadCheckpoint(System.IO.BinaryReader reader)
        {
            WindX = reader.ReadSingle();
            WindY = reader.ReadSingle();
            for (int i = 0; i < Temperature.Length; i++) Temperature[i] = reader.ReadSingle();
            for (int i = 0; i < Fuel.Length; i++) Fuel[i] = reader.ReadSingle();
            for (int i = 0; i < Oxygen.Length; i++) Oxygen[i] = reader.ReadSingle();
            for (int i = 0; i < Water.Length; i++) Water[i] = reader.ReadSingle();
            for (int i = 0; i < Ice.Length; i++) Ice[i] = reader.ReadSingle();
            for (int i = 0; i < Steam.Length; i++) Steam[i] = reader.ReadSingle();
            for (int i = 0; i < Smoke.Length; i++) Smoke[i] = reader.ReadSingle();
            for (int i = 0; i < Material.Length; i++) Material[i] = reader.ReadByte();
            for (int i = 0; i < Burning.Length; i++) Burning[i] = reader.ReadByte();
        }

        public ElementGrid(int width, int height)
        {
            if (width < 4) width = 4;
            if (height < 4) height = 4;
            Width = width;
            Height = height;
            int n = width * height;
            Temperature = new float[n];
            Fuel = new float[n];
            Oxygen = new float[n];
            Water = new float[n];
            Ice = new float[n];
            Steam = new float[n];
            Smoke = new float[n];
            Material = new byte[n];
            Burning = new byte[n];
            _scratch = new float[n];
            _scratchB = new float[n];
            for (int i = 0; i < n; i++)
            {
                Temperature[i] = 20f;
                Oxygen[i] = 1f;
            }
        }

        public static ElementGrid CreateNursery(int width, int height, int seed)
        {
            var grid = new ElementGrid(width, height);
            grid.WindX = ((seed & 1) == 0 ? 0.2f : -0.12f);
            grid.WindY = 0.08f;
            int wood = grid.Index(width * 3 / 4, height / 2);
            grid.Material[wood] = Wood;
            grid.Fuel[wood] = 1.4f;
            grid.Temperature[wood] = 360f;
            grid.Burning[wood] = 1;
            if (wood + 1 < width * height && (wood + 1) % width != 0)
            {
                grid.Material[wood + 1] = Wood;
                grid.Fuel[wood + 1] = 1.1f;
            }
            int pond = grid.Index(width / 5, height / 2);
            grid.Water[pond] = 0.9f;
            grid.Temperature[pond] = 16f;
            int ice = grid.Index(width / 2, height / 5);
            grid.Material[ice] = IceBlock;
            grid.Ice[ice] = 1f;
            grid.Temperature[ice] = -4f;
            return grid;
        }

        public int Index(int x, int y)
        {
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x >= Width) x = Width - 1;
            if (y >= Height) y = Height - 1;
            return y * Width + x;
        }

        public void CellAt(float worldX, float worldZ, float halfExtent, out int x, out int y)
        {
            float u = (worldX + halfExtent) / (halfExtent * 2f);
            float v = (worldZ + halfExtent) / (halfExtent * 2f);
            if (u < 0f) u = 0f;
            if (v < 0f) v = 0f;
            if (u > 0.999f) u = 0.999f;
            if (v > 0.999f) v = 0.999f;
            x = (int)(u * Width);
            y = (int)(v * Height);
        }

        public void Pour(int index, float amount)
        {
            if (index < 0 || index >= Water.Length || amount <= 0f) return;
            Water[index] += amount;
            Temperature[index] -= amount * 45f;
            if (Temperature[index] < -5f) Temperature[index] = -5f;
            Steam[index] += amount * 0.15f;
            if (Water[index] > 0.2f) Burning[index] = 0;
        }

        public int NearestBurning(int x, int y, int radius)
        {
            int best = -1;
            int bestD = radius * radius + 1;
            int y0 = y - radius;
            if (y0 < 0) y0 = 0;
            int y1 = y + radius;
            if (y1 >= Height) y1 = Height - 1;
            int x0 = x - radius;
            if (x0 < 0) x0 = 0;
            int x1 = x + radius;
            if (x1 >= Width) x1 = Width - 1;
            for (int yy = y0; yy <= y1; yy++)
            {
                for (int xx = x0; xx <= x1; xx++)
                {
                    int i = yy * Width + xx;
                    if (Burning[i] == 0) continue;
                    int dx = xx - x;
                    int dy = yy - y;
                    int d = dx * dx + dy * dy;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = i;
                    }
                }
            }
            return best;
        }

        public void Step(float dt)
        {
            if (dt < 0f) dt = 0f;
            if (dt > 0.2f) dt = 0.2f;
            int n = Temperature.Length;
            for (int i = 0; i < n; i++)
            {
                if (Ice[i] > 0.01f && Temperature[i] > 0f)
                {
                    float melt = Ice[i] < dt * 0.5f * Temperature[i] ? Ice[i] : dt * 0.5f * Temperature[i];
                    Ice[i] -= melt;
                    Water[i] += melt;
                    Temperature[i] -= melt * 8f;
                }
                if (Water[i] > 0.01f && Temperature[i] > 100f)
                {
                    float boil = dt * 0.45f;
                    if (boil > Water[i]) boil = Water[i];
                    Water[i] -= boil;
                    Steam[i] += boil;
                    Temperature[i] -= boil * 40f;
                }
                if (Steam[i] > 0.01f && Temperature[i] < 80f)
                {
                    float cond = Steam[i] < dt * 0.35f ? Steam[i] : dt * 0.35f;
                    Steam[i] -= cond;
                    Water[i] += cond;
                }
                bool wet = Water[i] > 0.25f;
                if (Burning[i] != 0)
                {
                    if (Fuel[i] <= 0.02f || Oxygen[i] < 0.08f || wet) Burning[i] = 0;
                    else
                    {
                        float burn = dt * 0.4f;
                        if (burn > Fuel[i]) burn = Fuel[i];
                        Fuel[i] -= burn;
                        Oxygen[i] -= burn * 0.45f;
                        if (Oxygen[i] < 0f) Oxygen[i] = 0f;
                        Temperature[i] += burn * 160f;
                        Smoke[i] += burn * 0.9f;
                    }
                }
                else if (!wet && Fuel[i] > 0.12f && Oxygen[i] > 0.18f && Temperature[i] > Ignition(Material[i]))
                {
                    Burning[i] = 1;
                }
                Oxygen[i] += dt * 0.12f * (1f - Oxygen[i]);
                if (Temperature[i] > 900f) Temperature[i] = 900f;
                if (Temperature[i] < -20f) Temperature[i] = -20f;
            }
            Array.Clear(_scratch, 0, n);
            for (int i = 0; i < n; i++)
            {
                if (Burning[i] == 0) continue;
                int x = i % Width;
                int y = i / Width;
                if (x > 0) _scratch[i - 1] += dt * 90f;
                if (x + 1 < Width) _scratch[i + 1] += dt * 90f;
                if (y > 0) _scratch[i - Width] += dt * 90f;
                if (y + 1 < Height) _scratch[i + Width] += dt * 90f;
            }
            for (int i = 0; i < n; i++) Temperature[i] += _scratch[i];
            Diffuse(Temperature, _scratch, 0.18f);
            Array.Copy(_scratch, Temperature, n);
            Flow(Water, _scratch, dt);
            Array.Copy(_scratch, Water, n);
            Advect(Smoke, _scratch, dt, 0.55f);
            Array.Copy(_scratch, Smoke, n);
            Advect(Steam, _scratchB, dt, 0.4f);
            Array.Copy(_scratchB, Steam, n);
        }

        static float Ignition(byte material)
        {
            if (material == Wood) return 230f;
            if (material == IceBlock) return 999f;
            if (material == Stone) return 999f;
            return 280f;
        }

        void Diffuse(float[] src, float[] dst, float rate)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    float sum = src[i];
                    int count = 1;
                    if (x > 0) { sum += src[i - 1]; count++; }
                    if (x + 1 < Width) { sum += src[i + 1]; count++; }
                    if (y > 0) { sum += src[i - Width]; count++; }
                    if (y + 1 < Height) { sum += src[i + Width]; count++; }
                    float mean = sum / count;
                    dst[i] = src[i] + (mean - src[i]) * rate;
                }
            }
        }

        void Flow(float[] src, float[] dst, float dt)
        {
            Array.Clear(dst, 0, dst.Length);
            float move = dt * 1.6f;
            if (move > 0.45f) move = 0.45f;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    float stay = src[i];
                    if (stay <= 0.05f)
                    {
                        dst[i] += stay;
                        continue;
                    }
                    if (stay > 0.05f)
                    {
                        int best = i;
                        float bestV = stay;
                        if (x > 0 && src[i - 1] < bestV) { best = i - 1; bestV = src[i - 1]; }
                        if (x + 1 < Width && src[i + 1] < bestV) { best = i + 1; bestV = src[i + 1]; }
                        if (y > 0 && src[i - Width] < bestV) { best = i - Width; bestV = src[i - Width]; }
                        if (y + 1 < Height && src[i + Width] < bestV) { best = i + Width; bestV = src[i + Width]; }
                        float sent = (stay - bestV) * move;
                        if (sent < 0f) sent = 0f;
                        if (best == i) sent = 0f;
                        dst[i] += stay - sent;
                        dst[best] += sent;
                    }
                }
            }
        }

        void Advect(float[] src, float[] dst, float dt, float rise)
        {
            Array.Clear(dst, 0, dst.Length);
            float riseFrac = dt * rise;
            if (riseFrac > 0.35f) riseFrac = 0.35f;
            float windFrac = dt * 0.8f;
            if (windFrac > 0.25f) windFrac = 0.25f;
            int windStepX = WindX > 0.05f ? 1 : (WindX < -0.05f ? -1 : 0);
            int windStepY = WindY > 0.05f ? 1 : (WindY < -0.05f ? -1 : 0);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    float mass = src[i];
                    if (mass <= 0.0001f) continue;
                    mass *= 1f - dt * 0.08f;
                    float stay = mass * (1f - riseFrac - windFrac * 0.5f);
                    if (stay < 0f) stay = 0f;
                    dst[i] += stay;
                    float rose = mass * riseFrac;
                    int up = y + 1 < Height ? i + Width : i;
                    dst[up] += rose;
                    int wx = x + windStepX;
                    int wy = y + windStepY;
                    if (wx < 0) wx = 0;
                    if (wy < 0) wy = 0;
                    if (wx >= Width) wx = Width - 1;
                    if (wy >= Height) wy = Height - 1;
                    dst[wy * Width + wx] += mass - stay - rose;
                }
            }
        }

        public void Sum(out float temp, out float fuel, out float water, out float smoke, out float steam, out int burning)
        {
            temp = 0f;
            fuel = 0f;
            water = 0f;
            smoke = 0f;
            steam = 0f;
            burning = 0;
            for (int i = 0; i < Temperature.Length; i++)
            {
                temp += Temperature[i];
                fuel += Fuel[i];
                water += Water[i];
                smoke += Smoke[i];
                steam += Steam[i];
                burning += Burning[i];
            }
        }
    }
}
