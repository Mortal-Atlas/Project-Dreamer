using System;

namespace DreamMachine.Body
{
    // Fixture-local contact centroid and measured pad loading. The motor policy has no object descriptor.
    public readonly struct GraspPatch
    {
        public readonly GripContactSense Contact;
        public readonly float Forward, Height;
        public GraspPatch(GripContactSense contact, float forward, float height)
        {
            Contact = contact; Forward = forward; Height = height;
        }

        internal bool Loaded => Contact.Normal >= GripCalibration.ContactForce && Contact.Duration >= .025f;
        internal bool Moving => Loaded && (Contact.Slip || Contact.TangentialSpeed >= .050f);
    }

    // Engineered search over small, bounded shifts. Search and load correction are transient.
    public sealed class TactileGrasp
    {
        readonly bool _usePlacementFeedback;
        int _stable, _missing;
        public float Cap { get; private set; }
        public float Lateral { get; private set; }
        public float Forward { get; private set; }
        public float Height { get; private set; }
        public float Pitch { get; private set; }
        public int Repositions { get; private set; }
        public int Corrections { get; private set; }
        public bool Ready => _stable >= 3;
        public bool Uncertain { get; private set; }
        public bool Lower => _missing >= 3;

        public TactileGrasp(float initialCap, bool usePlacementFeedback = true)
        {
            if (!(initialCap >= GripAcquisition.InitialCap && initialCap <= GripAcquisition.ReferenceCap))
                throw new ArgumentOutOfRangeException(nameof(initialCap));
            Cap = initialCap;
            _usePlacementFeedback = usePlacementFeedback;
        }

        public void Observe(GraspPatch left, GraspPatch right, bool suspended)
        {
            bool both = left.Loaded && right.Loaded;
            bool moving = left.Moving || right.Moving;
            Uncertain = !both || moving;
            _stable = Uncertain ? 0 : Math.Min(_stable + 1, 3);
            _missing = suspended && !both ? _missing + 1 : 0;
            float requested = Cap;
            if (suspended && Uncertain) requested += .65f;
            else if (moving) requested += .25f;
            else if (Ready && !suspended) requested = Math.Max(.90f, Cap);
            requested = Math.Min(GripAcquisition.ReferenceCap, requested);
            if (requested > Cap) { Cap = requested; Corrections++; }
        }

        public bool Reposition(GraspPatch left, GraspPatch right)
        {
            if (!_usePlacementFeedback || Repositions >= 2) return false;
            Repositions++;
            int sided = (right.Loaded ? 1 : 0) - (left.Loaded ? 1 : 0);
            bool both = left.Loaded && right.Loaded;
            if (both && Imbalanced(left, right)) sided = right.Contact.Normal > left.Contact.Normal ? 1 : -1;
            Lateral = Math.Clamp(Lateral + sided * .006f, -.012f, .012f);
            if (left.Loaded || right.Loaded)
            {
                float count = both ? 2f : 1f;
                float forward = ((left.Loaded ? left.Forward : 0f) + (right.Loaded ? right.Forward : 0f)) / count;
                float height = ((left.Loaded ? left.Height : 0f) + (right.Loaded ? right.Height : 0f)) / count;
                Forward = Math.Clamp(Forward + Math.Clamp(forward * .60f, -.009f, .009f), -.018f, .018f);
                Height = Math.Clamp(Height + Math.Clamp(height * .65f, -.005f, .008f), -.005f, .016f);
                Pitch = Math.Clamp(Pitch - forward * 220f, -6f, 6f);
            }
            else
            {
                Forward = Repositions == 1 ? .010f : -.007f;
                Height = .007f;
            }
            _stable = 0;
            return true;
        }

        public bool PoorPlacement(GraspPatch left, GraspPatch right) => left.Loaded && right.Loaded &&
            (left.Forward + right.Forward > .009f || Imbalanced(left, right));

        static bool Imbalanced(GraspPatch left, GraspPatch right) =>
            Math.Max(left.Contact.Normal, right.Contact.Normal) > 3f * Math.Min(left.Contact.Normal, right.Contact.Normal);
    }
}
