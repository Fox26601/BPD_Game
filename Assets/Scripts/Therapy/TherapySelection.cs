namespace BPD.Therapy
{
    public enum TherapySelectionPhase
    {
        EmotionPairing,
        NeedPairing
    }

    /// <summary>
    /// Slot-based pair selection: one card per role. Toggle same card off; replace same Kind.
    /// Emotion phase: Situation + Thought. Need phase: Emotion + Behavior.
    /// </summary>
    public static class TherapySelection
    {
        public static void ApplyClick(
            TherapySelectionPhase phase,
            TherapyCard clicked,
            ref TherapyCard slotA,
            ref TherapyCard slotB)
        {
            if (clicked == null)
            {
                return;
            }

            if (!TryResolveSlot(phase, clicked.Kind, out bool isSlotA))
            {
                return;
            }

            if (isSlotA)
            {
                slotA = ToggleOrReplace(slotA, clicked);
            }
            else
            {
                slotB = ToggleOrReplace(slotB, clicked);
            }
        }

        public static bool HasCompletePair(TherapyCard slotA, TherapyCard slotB) =>
            slotA != null && slotB != null;

        public static bool IsSelected(TherapyCard slotA, TherapyCard slotB, TherapyCard card)
        {
            if (card == null || string.IsNullOrEmpty(card.Id))
            {
                return false;
            }

            return SameId(slotA, card) || SameId(slotB, card);
        }

        public static void Clear(ref TherapyCard slotA, ref TherapyCard slotB)
        {
            slotA = null;
            slotB = null;
        }

        static TherapyCard ToggleOrReplace(TherapyCard current, TherapyCard clicked)
        {
            if (SameId(current, clicked))
            {
                return null;
            }

            return clicked;
        }

        static bool TryResolveSlot(TherapySelectionPhase phase, TherapyCardKind kind, out bool isSlotA)
        {
            isSlotA = false;
            switch (phase)
            {
                case TherapySelectionPhase.EmotionPairing:
                    if (kind == TherapyCardKind.Situation)
                    {
                        isSlotA = true;
                        return true;
                    }

                    if (kind == TherapyCardKind.Thought)
                    {
                        isSlotA = false;
                        return true;
                    }

                    return false;

                case TherapySelectionPhase.NeedPairing:
                    if (kind == TherapyCardKind.Emotion)
                    {
                        isSlotA = true;
                        return true;
                    }

                    if (kind == TherapyCardKind.Behavior)
                    {
                        isSlotA = false;
                        return true;
                    }

                    return false;

                default:
                    return false;
            }
        }

        static bool SameId(TherapyCard a, TherapyCard b) =>
            a != null && b != null && string.Equals(a.Id, b.Id, System.StringComparison.Ordinal);
    }
}
