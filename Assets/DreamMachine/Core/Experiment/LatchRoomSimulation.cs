using System;
using System.Collections.Generic;
using DreamMachine.Sim;
using DreamMachine.Util;

namespace DreamMachine.Experiment
{
    public enum RoomObject { Latch, NoisePanel }
    public enum RoomAction { Press, Slide }
    public enum RoomEventKind { Perceived, Question, Approach, TeacherDemonstration, Contact, Action, Feedback, Memory, Night, Replay, Hypothesis, Wake, Probe }

    /// <summary>Immutable observer trace. Parent IDs refer to earlier events in the same run.</summary>
    public readonly struct RoomEvent
    {
        public int Id { get; }
        public int Tick { get; }
        public RoomEventKind Kind { get; }
        public RoomObject? Object { get; }
        public RoomAction? Action { get; }
        public int? ParentId { get; }
        public float? Prediction { get; }
        public bool? Effect { get; }
        public int? Position { get; }
        public bool Transfer { get; }

        internal RoomEvent(int id, int tick, RoomEventKind kind, RoomObject? obj, RoomAction? action,
            int? parentId, float? prediction, bool? effect, int? position, bool transfer)
        {
            Id = id;
            Tick = tick;
            Kind = kind;
            Object = obj;
            Action = action;
            ParentId = parentId;
            Prediction = prediction;
            Effect = effect;
            Position = position;
            Transfer = transfer;
        }
    }

    /// <summary>A fixed-step experimental fixture with a two-position latch and an unpredictable light.</summary>
    public sealed class LatchRoomSimulation
    {
        public const int TeacherTick = 40;
        public const int NightTick = (int)(WorldSimulation.AwakeLength / WorldSimulation.Dt);
        public const int WakeTick = (int)(WorldSimulation.DayLength / WorldSimulation.Dt);
        public const int TransferTick = WakeTick + 16;
        public const int EndTick = TransferTick + 16;

        readonly bool _curiosity;
        readonly bool _reflectiveReplay;
        readonly RoomAction _latchRule;
        readonly SeededRandom _environment;
        readonly SeededRandom _choice;
        readonly Prediction[] _predictions = new Prediction[4];
        readonly List<RoomEvent> _trace = new List<RoomEvent>();
        readonly List<RoomEvent> _memory = new List<RoomEvent>();
        RoomEvent? _hypothesis;
        RoomEvent? _approach;
        int _position;
        bool _noiseEffect;

        public int TickCount { get; private set; }
        public IReadOnlyList<RoomEvent> Trace { get; }
        public IReadOnlyList<RoomEvent> LivedMemory { get; }

        public float EffectProbability(RoomObject obj, RoomAction action) => _predictions[Index(obj, action)].Probability;

        public LatchRoomSimulation(int seed, bool curiosity, bool reflectiveReplay)
        {
            _curiosity = curiosity;
            _reflectiveReplay = reflectiveReplay;
            _environment = new SeededRandom(seed ^ 0x5e12a0);
            _latchRule = (RoomAction)_environment.NextInt(2);
            _choice = SeededRandom.FromBirth(seed ^ 0x1f346a);
            Trace = _trace.AsReadOnly();
            LivedMemory = _memory.AsReadOnly();
        }

        public void RunTicks(int count)
        {
            for (int i = 0; i < count && TickCount < EndTick; i++)
                Tick();
        }

        void Tick()
        {
            if (TickCount == 0)
            {
                Add(RoomEventKind.Perceived, RoomObject.Latch, position: -2);
                Add(RoomEventKind.Perceived, RoomObject.NoisePanel, position: 2);
            }
            if (TickCount == TeacherTick) Demonstrate();
            if (TickCount < NightTick && TickCount % 80 == 1) Investigate();
            if (TickCount == NightTick)
            {
                Add(RoomEventKind.Night);
                if (_reflectiveReplay) Reflect();
            }
            if (TickCount == WakeTick)
            {
                Add(RoomEventKind.Wake);
                Probe(false);
            }
            if (TickCount == TransferTick) Probe(true);
            if (_approach.HasValue) AdvanceApproach(_approach.Value);
            TickCount++;
        }

        void Investigate()
        {
            // Consume one environmental sample per opportunity in every condition.
            _noiseEffect = _environment.NextFloat() < 0.5f;
            int choice = _choice.NextInt(_predictions.Length);
            if (_curiosity)
            {
                for (int index = 0; index < _predictions.Length; index++)
                    if (_predictions[index].Interest > _predictions[choice].Interest) choice = index;
            }
            var obj = (RoomObject)(choice / 2);
            var action = (RoomAction)(choice % 2);
            int? question = null;
            if (_curiosity)
                question = Add(RoomEventKind.Question, obj, action, prediction: _predictions[choice].Probability).Id;
            StartApproach(obj, action, question, false);
        }

        void Demonstrate()
        {
            bool effect = Response(RoomObject.Latch, RoomAction.Press);
            Add(RoomEventKind.TeacherDemonstration, RoomObject.Latch, RoomAction.Press, effect: effect, position: -2);
            _predictions[Index(RoomObject.Latch, RoomAction.Press)].Observe(effect);
        }

        void Reflect()
        {
            for (int i = _memory.Count - 1; i >= 0; i--)
            {
                RoomEvent encounter = _memory[i];
                if (encounter.Object != RoomObject.Latch || encounter.Effect != false) continue;
                RoomAction alternative = encounter.Action == RoomAction.Press ? RoomAction.Slide : RoomAction.Press;
                RoomEvent replay = Add(RoomEventKind.Replay, encounter.Object, encounter.Action, encounter.Id,
                    effect: encounter.Effect, position: encounter.Position);
                _hypothesis = Add(RoomEventKind.Hypothesis, encounter.Object, alternative, replay.Id,
                    prediction: _predictions[Index(RoomObject.Latch, alternative)].Probability);
                return;
            }
        }

        void Probe(bool transfer)
        {
            float press = _predictions[Index(RoomObject.Latch, RoomAction.Press)].Probability;
            float slide = _predictions[Index(RoomObject.Latch, RoomAction.Slide)].Probability;
            RoomAction best = press >= slide ? RoomAction.Press : RoomAction.Slide;
            RoomAction action = transfer ? best : _hypothesis?.Action ?? best;
            int? parent = transfer ? null : _hypothesis?.Id;
            RoomEvent probe = Add(RoomEventKind.Probe, RoomObject.Latch, action, parent, transfer: transfer);
            StartApproach(RoomObject.Latch, action, probe.Id, transfer);
        }

        void StartApproach(RoomObject obj, RoomAction action, int? parent, bool transfer)
        {
            int location = obj == RoomObject.NoisePanel ? 2 : transfer ? 3 : -2;
            _approach = Add(RoomEventKind.Approach, obj, action, parent, position: location, transfer: transfer);
        }

        void AdvanceApproach(RoomEvent goal)
        {
            int target = goal.Position.Value;
            _position += Math.Sign(target - _position);
            if (_position != target) return;
            _approach = null;
            RoomObject obj = goal.Object.Value;
            RoomAction action = goal.Action.Value;
            int index = Index(obj, action);
            RoomEvent contact = Add(RoomEventKind.Contact, obj, action, goal.Id, position: _position, transfer: goal.Transfer);
            RoomEvent act = Add(RoomEventKind.Action, obj, action, contact.Id, _predictions[index].Probability,
                position: _position, transfer: goal.Transfer);
            bool effect = Response(obj, action);
            RoomEvent feedback = Add(RoomEventKind.Feedback, obj, action, act.Id, effect: effect,
                position: _position, transfer: goal.Transfer);
            _predictions[index].Observe(effect);
            _memory.Add(Add(RoomEventKind.Memory, obj, action, feedback.Id, effect: effect,
                position: _position, transfer: goal.Transfer));
        }

        bool Response(RoomObject obj, RoomAction action)
        {
            if (obj == RoomObject.NoisePanel) return _noiseEffect;
            return action == _latchRule;
        }

        static int Index(RoomObject obj, RoomAction action) => 2 * (int)obj + (int)action;

        RoomEvent Add(RoomEventKind kind, RoomObject? obj = null, RoomAction? action = null,
            int? parent = null, float? prediction = null, bool? effect = null, int? position = null, bool transfer = false)
        {
            var entry = new RoomEvent(_trace.Count + 1, TickCount, kind, obj, action, parent, prediction, effect, position, transfer);
            _trace.Add(entry);
            return entry;
        }

        struct Prediction
        {
            int _count;
            int _effects;
            float _fastLoss;
            float _slowLoss;

            public float Probability => (_effects + 1f) / (_count + 2f);
            public float Interest
            {
                get
                {
                    float consistency = _count < 3 ? 1f : Math.Abs(2f * _effects / _count - 1f);
                    return (0.15f + 0.85f * consistency) / (1f + _count) + Math.Max(0f, _slowLoss - _fastLoss);
                }
            }

            public void Observe(bool effect)
            {
                float error = (effect ? 1f : 0f) - Probability;
                float loss = error * error;
                if (_count == 0) _fastLoss = _slowLoss = loss;
                _fastLoss += 0.5f * (loss - _fastLoss);
                _slowLoss += 0.15f * (loss - _slowLoss);
                _count++;
                if (effect) _effects++;
            }
        }
    }
}
