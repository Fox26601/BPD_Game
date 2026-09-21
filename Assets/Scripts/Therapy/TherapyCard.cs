using System;

namespace BPD.Therapy
{
    public enum TherapyCardKind
    {
        Situation,
        Thought,
        Behavior,
        Emotion,
        Need
    }

    [Serializable]
    public sealed class TherapyCard
    {
        public string Id;
        public TherapyCardKind Kind;
        public string TextKey;
        public bool Locked;
        public string MemoryId;
        public string SkillUnlockId;
    }

    public sealed class TherapyMergeResult
    {
        public bool Success;
        public TherapyCard Produced;
        public string FailureReasonKey;
    }
}
