using System;
using System.Collections.Generic;
using DreamMachine.Sim;
using DreamMachine.Util;

namespace DreamMachine.Experiment
{
    // The visible cue and the attempted movement each have one bit per switch.
    public readonly struct DiscoveryAttempt
    {
        public int Id { get; }
        public int Tick { get; }
        public int Control { get; }
        public int Cue { get; }
        public int Movement { get; }
        public bool Effect { get; }
        public double Prediction { get; }
        public double InformationGain { get; }
        public int? ReplaySource { get; }

        internal DiscoveryAttempt(int id, int tick, int control, int cue, int movement, bool effect,
            double prediction, double informationGain, int? replaySource)
        {
            Id = id;
            Tick = tick;
            Control = control;
            Cue = cue;
            Movement = movement;
            Effect = effect;
            Prediction = prediction;
            InformationGain = informationGain;
            ReplaySource = replaySource;
        }
    }

    /// <summary>A question about one visible control, with a private model and append-only evidence.</summary>
    public sealed class DiscoveryQuestion
    {
        public const int MechanismCount = 16;
        readonly double[] _weights = new double[MechanismCount];
        readonly List<DiscoveryAttempt> _evidence = new List<DiscoveryAttempt>();
        double _noiseWeight = 0.5;

        public int Control { get; }
        public int ObservedTick => 0;
        public IReadOnlyList<DiscoveryAttempt> Evidence { get; }
        public double NoiseProbability => _noiseWeight;
        public double Confidence
        {
            get
            {
                double maximum = _noiseWeight;
                foreach (double weight in _weights) maximum = Math.Max(maximum, weight);
                return maximum;
            }
        }
        public bool Resolved => Confidence >= 0.9;
        public double MechanismProbability(int mechanism) => _weights[mechanism];

        internal DiscoveryQuestion(int control)
        {
            Control = control;
            for (int model = 0; model < _weights.Length; model++) _weights[model] = 0.5 / MechanismCount;
            Evidence = _evidence.AsReadOnly();
        }

        public double Predict(int cue, int movement)
        {
            double probability = _noiseWeight / 2;
            for (int model = 0; model < _weights.Length; model++)
                if (RequiredMovement(model, cue) == movement) probability += _weights[model];
            return probability;
        }

        public double InformationGain(int cue, int movement)
        {
            double probability = Predict(cue, movement);
            if (probability <= 0 || probability >= 1) return 0;
            double entropy = -probability * Math.Log(probability, 2) - (1 - probability) * Math.Log(1 - probability, 2);
            return Math.Max(0, entropy - _noiseWeight);
        }

        internal void Observe(DiscoveryAttempt attempt)
        {
            _noiseWeight = Math.Max(double.Epsilon, _noiseWeight / 2);
            double total = _noiseWeight;
            for (int model = 0; model < _weights.Length; model++)
            {
                bool agrees = (RequiredMovement(model, attempt.Cue) == attempt.Movement) == attempt.Effect;
                if (!agrees) _weights[model] = 0;
                total += _weights[model];
            }
            for (int model = 0; model < _weights.Length; model++) _weights[model] /= total;
            _noiseWeight /= total;
            _evidence.Add(attempt);
        }

        // Four bits encode the visible cue-to-movement response of each switch.
        internal static int RequiredMovement(int mechanism, int cue)
        {
            int first = (mechanism >> (cue & 1)) & 1;
            int second = (mechanism >> (2 + (cue >> 1))) & 1;
            return first | (second << 1);
        }
    }

    /// <summary>Two waking days and a held-out probe, separate from the original latch pilot.</summary>
    public sealed class DiscoveryRoom
    {
        const int RetentionCue = 0;
        public const int MinimumAttemptsPerDay = 3;
        public const int MaximumAttemptsPerDay = LatchRoomSimulation.NightTick - 1;
        public const int HeldOutCue = 2;
        public const int EndTick = LatchRoomSimulation.WakeTick * 2 + 1;
        static readonly int[] TrainingCues = { RetentionCue, 1, 3 };

        readonly bool _curiosity;
        readonly bool _replay;
        readonly int _attemptsPerDay;
        readonly int _interval;
        readonly int _lock;
        readonly int _mechanism;
        readonly SeededRandom _environment;
        readonly SeededRandom _choices;
        readonly DiscoveryQuestion[] _questions = { new DiscoveryQuestion(0), new DiscoveryQuestion(1) };
        readonly List<DiscoveryAttempt> _attempts = new List<DiscoveryAttempt>();
        DiscoveryAttempt? _replaySource;
        int _rehearsedMovement;
        int _dayAttempts;

        public int TickCount { get; private set; }
        public int AttemptsOnNoise { get; private set; }
        public int? FirstUnlockTick { get; private set; }
        public bool? Retained { get; private set; }
        public bool? Transferred { get; private set; }
        public int? RehearsalSourceId { get; private set; }
        public double MeanBrierLoss { get; private set; }
        public IReadOnlyList<DiscoveryAttempt> Attempts { get; }
        public IReadOnlyList<DiscoveryQuestion> Questions { get; }

        public DiscoveryRoom(int seed, int attemptsPerDay, bool curiosity, bool replay)
        {
            if (attemptsPerDay < MinimumAttemptsPerDay || attemptsPerDay > MaximumAttemptsPerDay)
                throw new ArgumentOutOfRangeException(nameof(attemptsPerDay));
            _attemptsPerDay = attemptsPerDay;
            _interval = LatchRoomSimulation.NightTick / (attemptsPerDay + 1);
            _curiosity = curiosity;
            _replay = replay;
            _environment = new SeededRandom(seed ^ 0x1b7ad29);
            _lock = _environment.NextInt(2);
            _mechanism = _environment.NextInt(DiscoveryQuestion.MechanismCount);
            _choices = SeededRandom.FromBirth(seed ^ 0x5ac9371);
            Attempts = _attempts.AsReadOnly();
            Questions = Array.AsReadOnly(_questions);
        }

        public void RunTicks(int count)
        {
            for (int i = 0; i < count && TickCount < EndTick; i++)
            {
                int time = TickCount % LatchRoomSimulation.WakeTick;
                if (TickCount == EndTick - 1) EvaluateProbes();
                else if (time == 0) _dayAttempts = 0;
                else if (time < LatchRoomSimulation.NightTick && time % _interval == 0 && _dayAttempts < _attemptsPerDay)
                    Investigate(TrainingCues[_dayAttempts++ % TrainingCues.Length]);
                else if (_replay && TickCount == LatchRoomSimulation.NightTick) Rehearse();
                TickCount++;
            }
        }

        void Investigate(int cue)
        {
            bool noiseEffect = _environment.NextFloat() < 0.5f;
            int candidate = _choices.NextInt(8);
            if (_curiosity)
            {
                for (int i = 0; i < 8; i++)
                    if (Gain(i, cue) > Gain(candidate, cue) + 1e-12) candidate = i;
            }
            int? source = null;
            if (_replaySource.HasValue && _replaySource.Value.Cue == cue)
            {
                DiscoveryAttempt remembered = _replaySource.Value;
                candidate = remembered.Control * 4 + _rehearsedMovement;
                source = remembered.Id;
                _replaySource = null;
            }
            int control = candidate / 4;
            int movement = candidate % 4;
            bool effect = control == _lock ? Correct(cue, movement) : noiseEffect;
            DiscoveryQuestion question = _questions[control];
            var attempt = new DiscoveryAttempt(_attempts.Count + 1, TickCount, control, cue, movement, effect,
                question.Predict(cue, movement), Gain(candidate, cue), source);
            _attempts.Add(attempt);
            question.Observe(attempt);
            if (control != _lock) AttemptsOnNoise++;
            else if (effect && !FirstUnlockTick.HasValue) FirstUnlockTick = TickCount;
            double error = attempt.Prediction - (effect ? 1 : 0);
            MeanBrierLoss += (error * error - MeanBrierLoss) / _attempts.Count;
        }

        void Rehearse()
        {
            // Practice a counterfactual derived from collected evidence. It never updates a question.
            double bestGain = 1e-12;
            foreach (DiscoveryAttempt attempt in _attempts)
            {
                if (attempt.Effect) continue;
                for (int move = 0; move < 4; move++)
                {
                    if (move == attempt.Movement) continue;
                    double gain = Gain(attempt.Control * 4 + move, attempt.Cue);
                    if (gain <= bestGain) continue;
                    bestGain = gain;
                    _replaySource = attempt;
                    _rehearsedMovement = move;
                    RehearsalSourceId = attempt.Id;
                }
            }
        }

        void EvaluateProbes()
        {
            // Both evaluation trials have a researcher-specified target. Neither feeds back into learning.
            Retained = Correct(RetentionCue, BestMovement(RetentionCue));
            Transferred = Correct(HeldOutCue, BestMovement(HeldOutCue));
        }

        int BestMovement(int cue)
        {
            int best = 0;
            for (int move = 1; move < 4; move++)
                if (_questions[_lock].Predict(cue, move) > _questions[_lock].Predict(cue, best)) best = move;
            return best;
        }

        double Gain(int candidate, int cue) => _questions[candidate / 4].InformationGain(cue, candidate % 4);
        bool Correct(int cue, int movement) => DiscoveryQuestion.RequiredMovement(_mechanism, cue) == movement;
    }
}
