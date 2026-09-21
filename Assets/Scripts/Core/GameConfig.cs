using System;
using BPD.Stats;
using UnityEngine;

namespace BPD.Core
{
    [CreateAssetMenu(fileName = "SO_GameConfig", menuName = "BPD/Config/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Stats")]
        [SerializeField] float startingStatValue = 50f;
        [SerializeField] float maxStatValue = 100f;
        [SerializeField] float failThreshold = 0f;
        [SerializeField] float cardsPerLevel = 5f;
        [SerializeField] float splitStabilizeFloor = 20f;

        [Header("Split routing (lowest first; tie-break order)")]
        [SerializeField] CoreStat[] splitTieBreakOrder =
        {
            CoreStat.Safety,
            CoreStat.Stability,
            CoreStat.Wholeness,
            CoreStat.Closeness
        };

        [Header("Progression")]
        [SerializeField] int xpPerCardChoice = 8;
        [SerializeField] int xpToCompleteLevel = 24;
        [SerializeField] int xpPerLevelWin = 20;
        [SerializeField] int xpPerSplitSuccess = 15;
        [SerializeField] int xpCostPerSymptomPoint = 10;
        [SerializeField] int maxSymptomSeverity = 10;
        [SerializeField] int dbtChargesOnUnlock = 2;
        [Tooltip("Target full run = 45 (3 acts × 15).")]
        [SerializeField] int levelsBeforeEnding = 45;
        [SerializeField] int levelsPerAct = 15;

        [Header("Therapy")]
        [SerializeField, Range(0f, 1f)] float memoryStabilityPenaltyFraction = 0.25f;

        [Header("Symptom modifiers (per reduced severity point from max)")]
        [SerializeField] float impulsivitySafetyDecayScalePerSeverity = 0.05f;
        [SerializeField] float abandonmentClosenessDecayScalePerSeverity = 0.04f;
        [SerializeField] float selfImageWholenessDecayScalePerSeverity = 0.04f;
        [SerializeField] float affectiveStabilityDecayScalePerSeverity = 0.04f;
        [SerializeField] float selfHarmSafetyDecayScalePerSeverity = 0.03f;
        [SerializeField] float paranoiaStabilityDecayScalePerSeverity = 0.03f;
        [SerializeField] float relationshipSwingScalePerSeverity = 0.05f;
        [SerializeField] float angerHitScalePerSeverity = 0.04f;
        [SerializeField] float emptinessSkillWholenessBonusPerReduced = 0.4f;

        public float StartingStatValue => startingStatValue;
        public float MaxStatValue => maxStatValue;
        public float FailThreshold => failThreshold;
        public float SplitStabilizeFloor => splitStabilizeFloor;
        public int CardsPerLevel => Mathf.Max(1, Mathf.RoundToInt(cardsPerLevel));
        public CoreStat[] SplitTieBreakOrder => splitTieBreakOrder;
        public int XpPerCardChoice => Mathf.Max(0, xpPerCardChoice);
        public int XpToCompleteLevel => Mathf.Max(1, xpToCompleteLevel);
        public int XpPerLevelWin => xpPerLevelWin;
        public int XpPerSplitSuccess => xpPerSplitSuccess;
        public int XpCostPerSymptomPoint => xpCostPerSymptomPoint;
        public int MaxSymptomSeverity => maxSymptomSeverity;
        public int DbtChargesOnUnlock => dbtChargesOnUnlock;
        public int LevelsBeforeEnding => Mathf.Max(1, levelsBeforeEnding);
        public int LevelsPerAct => Mathf.Max(1, levelsPerAct);

        public int ResolveActIndex(int levelIndex)
        {
            if (levelIndex < 0)
            {
                levelIndex = 0;
            }

            return levelIndex / LevelsPerAct;
        }

        /// <summary>Offset into act deck pool for this level (level-band rotation).</summary>
        public int ResolveLevelBandOffset(int levelIndex)
        {
            if (levelIndex < 0)
            {
                levelIndex = 0;
            }

            return levelIndex % LevelsPerAct;
        }

        public float MemoryStabilityPenaltyFraction => memoryStabilityPenaltyFraction;
        public float ImpulsivitySafetyDecayScalePerSeverity => impulsivitySafetyDecayScalePerSeverity;
        public float AbandonmentClosenessDecayScalePerSeverity => abandonmentClosenessDecayScalePerSeverity;
        public float SelfImageWholenessDecayScalePerSeverity => selfImageWholenessDecayScalePerSeverity;
        public float AffectiveStabilityDecayScalePerSeverity => affectiveStabilityDecayScalePerSeverity;
        public float SelfHarmSafetyDecayScalePerSeverity => selfHarmSafetyDecayScalePerSeverity;
        public float ParanoiaStabilityDecayScalePerSeverity => paranoiaStabilityDecayScalePerSeverity;
        public float RelationshipSwingScalePerSeverity => relationshipSwingScalePerSeverity;
        public float AngerHitScalePerSeverity => angerHitScalePerSeverity;
        public float EmptinessSkillWholenessBonusPerReduced => emptinessSkillWholenessBonusPerReduced;

        public static GameConfig LoadOrCreateRuntimeDefault()
        {
            var loaded = Resources.Load<GameConfig>("Config/SO_GameConfig");
            if (loaded != null)
            {
                return loaded;
            }

            return CreateInstance<GameConfig>();
        }
    }
}
