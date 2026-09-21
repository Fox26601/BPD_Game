using BPD.Core;
using BPD.Progression;
using BPD.Run;
using BPD.Stats;

namespace BPD.Level
{
    public sealed class DefaultCardEffect : ICardEffect
    {
        readonly ModifierRegistry _modifiers;

        public DefaultCardEffect(ModifierRegistry modifiers)
        {
            _modifiers = modifiers;
        }

        public void Apply(RunState run, CardChoice choice, GameConfig config)
        {
            if (choice.StatDeltas != null)
            {
                foreach (var delta in choice.StatDeltas)
                {
                    float amount = _modifiers.ScaleNegativeDelta(run, config, delta.Stat, delta.Amount);
                    run.ApplyStatDelta(delta.Stat, amount, config);
                }
            }

            if (choice.IsSkillChoice)
            {
                float bonus = _modifiers.SkillWholenessBonus(run, config);
                if (bonus > 0f)
                {
                    run.ApplyStatDelta(CoreStat.Wholeness, bonus, config);
                }
            }

            ApplyRelationshipOps(run, choice.RelationshipOps, config);
            ApplyOps(run.HiddenVariables, choice.HiddenVariableOps);
        }

        void ApplyRelationshipOps(RunState run, VariableOp[] ops, GameConfig config)
        {
            if (ops == null)
            {
                return;
            }

            foreach (var op in ops)
            {
                if (string.IsNullOrEmpty(op.Key))
                {
                    continue;
                }

                int scaled = _modifiers.ScaleRelationshipDelta(run, config, op.Delta);
                run.Relationships.TryGetValue(op.Key, out var current);
                run.Relationships[op.Key] = current + scaled;
            }
        }

        static void ApplyOps(System.Collections.Generic.Dictionary<string, int> map, VariableOp[] ops)
        {
            if (ops == null)
            {
                return;
            }

            foreach (var op in ops)
            {
                if (string.IsNullOrEmpty(op.Key))
                {
                    continue;
                }

                map.TryGetValue(op.Key, out var current);
                map[op.Key] = current + op.Delta;
            }
        }
    }
}
