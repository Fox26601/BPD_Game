using BPD.Core;
using BPD.Run;
using BPD.Stats;
using UnityEngine;

namespace BPD.Progression
{
    /// <summary>
    /// Applies symptom-severity modifiers to card deltas and relationship swings (Canon-Mechanics matrix).
    /// </summary>
    public sealed class ModifierRegistry
    {
        public float ScaleNegativeDelta(RunState run, GameConfig config, CoreStat stat, float amount)
        {
            if (amount >= 0f)
            {
                return amount;
            }

            float scale = 1f;
            switch (stat)
            {
                case CoreStat.Safety:
                    scale *= SeveritySoftScale(run, config, BpdSymptom.Impulsivity, config.ImpulsivitySafetyDecayScalePerSeverity);
                    scale *= SeveritySoftScale(run, config, BpdSymptom.SelfHarmSuicidality, config.SelfHarmSafetyDecayScalePerSeverity);
                    break;
                case CoreStat.Closeness:
                    scale *= SeveritySoftScale(run, config, BpdSymptom.FearOfAbandonment, config.AbandonmentClosenessDecayScalePerSeverity);
                    scale *= SeveritySoftScale(run, config, BpdSymptom.IntenseAnger, config.AngerHitScalePerSeverity);
                    break;
                case CoreStat.Wholeness:
                    scale *= SeveritySoftScale(run, config, BpdSymptom.UnstableSelfImage, config.SelfImageWholenessDecayScalePerSeverity);
                    break;
                case CoreStat.Stability:
                    scale *= SeveritySoftScale(run, config, BpdSymptom.AffectiveInstability, config.AffectiveStabilityDecayScalePerSeverity);
                    scale *= SeveritySoftScale(run, config, BpdSymptom.ParanoiaDissociation, config.ParanoiaStabilityDecayScalePerSeverity);
                    scale *= SeveritySoftScale(run, config, BpdSymptom.IntenseAnger, config.AngerHitScalePerSeverity * 0.5f);
                    break;
            }

            return amount * Mathf.Clamp(scale, 0.25f, 1.25f);
        }

        /// <summary>GDD example: lower Impulsivity → Safety meter drops more slowly.</summary>
        public float GetSafetyNegativeDeltaScale(RunState run, GameConfig config)
        {
            float sample = ScaleNegativeDelta(run, config, CoreStat.Safety, -1f);
            return Mathf.Abs(sample);
        }

        public int ScaleRelationshipDelta(RunState run, GameConfig config, int delta)
        {
            if (delta == 0)
            {
                return 0;
            }

            float scale = SeveritySoftScale(run, config, BpdSymptom.UnstableRelationships, config.RelationshipSwingScalePerSeverity);
            float scaled = delta * Mathf.Clamp(scale, 0.25f, 1.25f);
            if (scaled > 0f)
            {
                return Mathf.Max(1, Mathf.RoundToInt(scaled));
            }

            if (scaled < 0f)
            {
                return Mathf.Min(-1, Mathf.RoundToInt(scaled));
            }

            return 0;
        }

        public float SkillWholenessBonus(RunState run, GameConfig config)
        {
            int reduced = config.MaxSymptomSeverity - run.GetSymptomSeverity(BpdSymptom.ChronicEmptiness);
            if (reduced <= 0)
            {
                return 0f;
            }

            return reduced * config.EmptinessSkillWholenessBonusPerReduced;
        }

        static float SeveritySoftScale(RunState run, GameConfig config, BpdSymptom symptom, float perPoint)
        {
            int severity = run.GetSymptomSeverity(symptom);
            float max = config.MaxSymptomSeverity;
            float reduced = (max - severity) * perPoint;
            return 1f - reduced;
        }
    }
}
