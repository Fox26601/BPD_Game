using System;
using System.Collections.Generic;
using BPD.Run;
using BPD.Skills;
using BPD.Stats;
using UnityEngine;

namespace BPD.Level
{
    [Serializable]
    public struct StatDelta
    {
        public CoreStat Stat;
        public float Amount;
    }

    [Serializable]
    public struct StatEffects
    {
        public float Wholeness;
        public float Stability;
        public float Safety;
        public float Closeness;

        public StatDelta[] ToDeltas()
        {
            var list = new List<StatDelta>(4);
            AddIfNonZero(list, CoreStat.Wholeness, Wholeness);
            AddIfNonZero(list, CoreStat.Stability, Stability);
            AddIfNonZero(list, CoreStat.Safety, Safety);
            AddIfNonZero(list, CoreStat.Closeness, Closeness);
            return list.ToArray();
        }

        static void AddIfNonZero(List<StatDelta> list, CoreStat stat, float amount)
        {
            if (Mathf.Abs(amount) < 0.0001f)
            {
                return;
            }

            list.Add(new StatDelta { Stat = stat, Amount = amount });
        }
    }

    [Serializable]
    public struct VariableOp
    {
        public string Key;
        public int Delta;
    }

    [Serializable]
    public struct CardGate
    {
        [Tooltip("Hidden variable that must be >= MinValue (empty = ignore).")]
        public string HiddenKey;
        public int HiddenMinValue;

        [Tooltip("Relationship key that must be >= MinValue (empty = ignore).")]
        public string RelationshipKey;
        public int RelationshipMinValue;

        [Tooltip("If not None, symptom severity must be >= MinSeverity.")]
        public BpdSymptom RequiredSymptom;
        public int SymptomMinSeverity;
        public bool RequireSymptomCheck;

        public bool IsEmpty =>
            string.IsNullOrEmpty(HiddenKey) &&
            string.IsNullOrEmpty(RelationshipKey) &&
            !RequireSymptomCheck;

        public bool IsSatisfied(RunState run)
        {
            if (!string.IsNullOrEmpty(HiddenKey))
            {
                run.HiddenVariables.TryGetValue(HiddenKey, out var hidden);
                if (hidden < HiddenMinValue)
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(RelationshipKey))
            {
                run.Relationships.TryGetValue(RelationshipKey, out var rel);
                if (rel < RelationshipMinValue)
                {
                    return false;
                }
            }

            if (RequireSymptomCheck)
            {
                if (run.GetSymptomSeverity(RequiredSymptom) < SymptomMinSeverity)
                {
                    return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    public struct CardChoice
    {
        public string ActionName;
        public StatDelta[] StatDeltas;
        public VariableOp[] RelationshipOps;
        public VariableOp[] HiddenVariableOps;
        public bool TriggersLoseIfApplied;
        public bool IsSkillChoice;
    }

    /// <summary>
    /// Authoring surface for one level card. Fill event copy, left/right actions + deltas, optional skill path.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Card_", menuName = "BPD/Content/Card Definition")]
    public sealed class CardDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string Id = "card_stub_001";

        [Header("Event")]
        [TextArea(3, 8)]
        public string SituationText = "PLACEHOLDER: event description";

        [Header("Inclusion gate (optional)")]
        public CardGate InclusionGate;

        [Header("Left choice")]
        public string LeftActionName = "PLACEHOLDER left";
        public StatEffects LeftEffects;
        public VariableOp[] LeftRelationshipOps;
        public VariableOp[] LeftHiddenOps;
        public bool LeftTriggersLose;

        [Header("Right choice")]
        public string RightActionName = "PLACEHOLDER right";
        public StatEffects RightEffects;
        public VariableOp[] RightRelationshipOps;
        public VariableOp[] RightHiddenOps;
        public bool RightTriggersLose;

        [Header("Skill resolution (optional)")]
        public bool AllowSkill;
        public DbtSkillId RequiredSkill = DbtSkillId.None;
        public string SkillActionName = "PLACEHOLDER skill";
        public StatEffects SkillEffects;
        public VariableOp[] SkillRelationshipOps;
        public VariableOp[] SkillHiddenOps;

        public CardChoice Left => BuildChoice(LeftActionName, LeftEffects, LeftRelationshipOps, LeftHiddenOps, LeftTriggersLose, isSkill: false);
        public CardChoice Right => BuildChoice(RightActionName, RightEffects, RightRelationshipOps, RightHiddenOps, RightTriggersLose, isSkill: false);
        public CardChoice SkillChoice => BuildChoice(SkillActionName, SkillEffects, SkillRelationshipOps, SkillHiddenOps, triggersLose: false, isSkill: true);

        public bool PassesGate(RunState run) => InclusionGate.IsEmpty || InclusionGate.IsSatisfied(run);

        static CardChoice BuildChoice(
            string actionName,
            StatEffects effects,
            VariableOp[] relationshipOps,
            VariableOp[] hiddenOps,
            bool triggersLose,
            bool isSkill)
        {
            return new CardChoice
            {
                ActionName = actionName,
                StatDeltas = effects.ToDeltas(),
                RelationshipOps = relationshipOps,
                HiddenVariableOps = hiddenOps,
                TriggersLoseIfApplied = triggersLose,
                IsSkillChoice = isSkill
            };
        }
    }
}
