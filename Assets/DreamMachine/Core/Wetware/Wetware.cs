using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DreamMachine.Util;

namespace DreamMachine.Wetware
{
    public sealed class WetwareSnapshot
    {
        public string Kind;
        public float[] Weights;
        public int Cursor;
        public string ReplayPath;
    }

    /// <summary>
    /// Plasticity stand-in. A Brian2 replay file, or later a real organoid
    /// recording, can replace the simple Hebbian / LIF models behind this.
    /// </summary>
    public interface IWetware
    {
        string Kind { get; }
        void Step(float dt, float[] input);
        float[] Readout();
        void ApplyPlasticity(float reward);
        WetwareSnapshot Snapshot();
        void Restore(WetwareSnapshot snapshot);
    }

    /// <summary>
    /// Two-layer Hebbian weights. Reward strengthens co-active pairs.
    /// This is a toy of plasticity, not a claim about cortex.
    /// </summary>
    public sealed class HebbianWetware : IWetware
    {
        public readonly int Inputs;
        public readonly int Units;
        readonly float[] _weights;
        readonly float[] _rates;
        float[] _lastInput = new float[0];

        public HebbianWetware(int inputs, int units, int birthSeed)
        {
            Inputs = inputs;
            Units = units;
            _weights = new float[inputs * units];
            _rates = new float[units];
            var rng = new SeededRandom(birthSeed ^ 0x71E7);
            for (int i = 0; i < _weights.Length; i++)
            {
                _weights[i] = rng.Range(-0.15f, 0.15f);
            }
        }

        public string Kind { get { return "hebbian"; } }

        public void Step(float dt, float[] input)
        {
            _lastInput = input ?? new float[Inputs];
            for (int u = 0; u < Units; u++)
            {
                float sum = 0f;
                for (int i = 0; i < Inputs; i++)
                {
                    float x = i < _lastInput.Length ? _lastInput[i] : 0f;
                    sum += _weights[u * Inputs + i] * x;
                }
                float target = 1f / (1f + (float)Math.Exp(-sum));
                _rates[u] += (target - _rates[u]) * Math.Min(1f, dt * 8f);
            }
        }

        public float[] Readout()
        {
            var copy = new float[Units];
            Array.Copy(_rates, copy, Units);
            return copy;
        }

        public void ApplyPlasticity(float reward)
        {
            float lr = 0.05f * reward;
            for (int u = 0; u < Units; u++)
            {
                for (int i = 0; i < Inputs; i++)
                {
                    float pre = i < _lastInput.Length ? _lastInput[i] : 0f;
                    int idx = u * Inputs + i;
                    _weights[idx] += lr * pre * _rates[u] - 0.001f * _weights[idx];
                    if (_weights[idx] > 2f) _weights[idx] = 2f;
                    if (_weights[idx] < -2f) _weights[idx] = -2f;
                }
            }
        }

        public WetwareSnapshot Snapshot()
        {
            var copy = new float[_weights.Length];
            Array.Copy(_weights, copy, _weights.Length);
            return new WetwareSnapshot { Kind = Kind, Weights = copy };
        }

        public void Restore(WetwareSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Weights == null) return;
            int n = Math.Min(snapshot.Weights.Length, _weights.Length);
            Array.Copy(snapshot.Weights, _weights, n);
        }
    }

    /// <summary>
    /// A tiny leaky integrate-and-fire population with one shared input current.
    /// </summary>
    public sealed class LeakyIntegrateFireWetware : IWetware
    {
        public readonly int Units;
        readonly float[] _v;
        readonly float[] _spikes;
        readonly float[] _bias;
        float _lastCurrent;

        public LeakyIntegrateFireWetware(int units, int birthSeed)
        {
            Units = units;
            _v = new float[units];
            _spikes = new float[units];
            _bias = new float[units];
            var rng = new SeededRandom(birthSeed ^ 0x11F3);
            for (int i = 0; i < units; i++)
            {
                _v[i] = rng.Range(0f, 0.4f);
                _bias[i] = rng.Range(0.2f, 0.8f);
            }
        }

        public string Kind { get { return "lif"; } }

        public void Step(float dt, float[] input)
        {
            _lastCurrent = 0f;
            if (input != null)
            {
                for (int i = 0; i < input.Length; i++) _lastCurrent += input[i];
                if (input.Length > 0) _lastCurrent /= input.Length;
            }
            for (int i = 0; i < Units; i++)
            {
                float drive = _bias[i] + _lastCurrent * 2f;
                _v[i] += dt * ((0.2f - _v[i]) / 0.02f + drive * 40f);
                if (_v[i] >= 0.8f)
                {
                    _spikes[i] = 1f;
                    _v[i] = 0f;
                }
                else
                {
                    _spikes[i] = 0f;
                }
            }
        }

        public float[] Readout()
        {
            var copy = new float[Units];
            Array.Copy(_spikes, copy, Units);
            return copy;
        }

        public void ApplyPlasticity(float reward)
        {
            for (int i = 0; i < Units; i++)
            {
                _bias[i] += 0.02f * reward * (_spikes[i] - 0.2f);
                if (_bias[i] < 0f) _bias[i] = 0f;
                if (_bias[i] > 1.5f) _bias[i] = 1.5f;
            }
        }

        public WetwareSnapshot Snapshot()
        {
            var copy = new float[_bias.Length];
            Array.Copy(_bias, copy, _bias.Length);
            return new WetwareSnapshot { Kind = Kind, Weights = copy };
        }

        public void Restore(WetwareSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Weights == null) return;
            int n = Math.Min(snapshot.Weights.Length, _bias.Length);
            Array.Copy(snapshot.Weights, _bias, n);
        }
    }

    /// <summary>
    /// Plays spike and weight rows exported by the Brian2 sidecar.
    /// Columns: t,n0,n1,n2,n3,s0,s1,s2,s3
    /// </summary>
    public sealed class ReplayFileWetware : IWetware
    {
        public readonly List<float[]> Rows = new List<float[]>();
        int _cursor;
        float[] _readout = new float[0];
        public string PathLoaded = "";

        public string Kind { get { return "replay-file"; } }

        public static ReplayFileWetware LoadCsv(string path)
        {
            var wet = new ReplayFileWetware();
            wet.PathLoaded = path;
            wet.LoadText(File.ReadAllText(path));
            return wet;
        }

        public void LoadText(string csv)
        {
            Rows.Clear();
            string[] lines = csv.Replace("\r", "").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("t,") || line.StartsWith("#")) continue;
                string[] parts = line.Split(',');
                var row = new float[parts.Length];
                for (int c = 0; c < parts.Length; c++)
                {
                    float value;
                    if (!float.TryParse(parts[c], NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    {
                        value = 0f;
                    }
                    row[c] = value;
                }
                Rows.Add(row);
            }
            _cursor = 0;
            Step(0f, null);
        }

        public void Step(float dt, float[] input)
        {
            if (Rows.Count == 0)
            {
                _readout = new float[0];
                return;
            }
            float[] row = Rows[_cursor];
            // n0..n3 sit in columns 1..4 when the header is t,n0,n1,n2,n3,s0...
            int rates = Math.Max(0, row.Length - 1);
            int take = Math.Min(4, rates);
            _readout = new float[take];
            for (int i = 0; i < take; i++) _readout[i] = row[1 + i];
            _cursor++;
            if (_cursor >= Rows.Count) _cursor = 0;
        }

        public float[] Readout()
        {
            var copy = new float[_readout.Length];
            Array.Copy(_readout, copy, _readout.Length);
            return copy;
        }

        public void ApplyPlasticity(float reward)
        {
            // The recording already contains the plasticity. Live reward is ignored.
        }

        public WetwareSnapshot Snapshot()
        {
            return new WetwareSnapshot
            {
                Kind = Kind,
                Cursor = _cursor,
                ReplayPath = PathLoaded,
                Weights = Readout()
            };
        }

        public void Restore(WetwareSnapshot snapshot)
        {
            if (snapshot == null) return;
            _cursor = snapshot.Cursor;
            if (_cursor < 0) _cursor = 0;
            if (Rows.Count > 0 && _cursor >= Rows.Count) _cursor %= Rows.Count;
        }
    }
}
