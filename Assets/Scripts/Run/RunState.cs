using System;
using System.Collections.Generic;
using BPD.Core;
using BPD.Skills;
using BPD.Stats;
using UnityEngine;

namespace BPD.Run
{
    /// <summary>
    /// Runtime run data. Not Unity-serialized (avoids UAC1009 on Dictionary/HashSet).
    /// Persist via custom save format when needed.
    /// </summary>
    public sealed class RunState
    {
        public CharacterId Character;
        public int LevelIndex;
        public int Xp;
        public int UnspentXp;
        public int LevelXp;
        public int LevelsCleared;
        public bool PendingLevelRestart;
        public int TraumaticMemoriesUnlocked;
        public Dictionary<CoreStat, float> Stats { get; } = new Dictionary<CoreStat, float>();
        public Dictionary<BpdSymptom, int> SymptomSeverity { get; } = new Dictionary<BpdSymptom, int>();
        public Dictionary<string, int> HiddenVariables { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> Relationships { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
        public List<DbtSkillId> UnlockedSkills { get; } = new List<DbtSkillId>();
        public Dictionary<DbtSkillId, int> SkillCharges { get; } = new Dictionary<DbtSkillId, int>();
        public Dictionary<CoreStat, int> SplitSurviveCounts { get; } = new Dictionary<CoreStat, int>();
        public HashSet<string> UnlockedNeeds { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> UnlockedEmotions { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> UnlockedMemoryIds { get; } = new HashSet<string>(StringComparer.Ordinal);

        readonly Dictionary<CoreStat, float> _levelStartStats = new Dictionary<CoreStat, float>();

        public static RunState CreateNew(CharacterId character, GameConfig config)
        {
            var run = new RunState
            {
                Character = character,
                LevelIndex = 0,
                Xp = 0,
                UnspentXp = 0,
                LevelXp = 0,
                LevelsCleared = 0,
                PendingLevelRestart = false,
                TraumaticMemoriesUnlocked = 0
            };

            foreach (CoreStat stat in Enum.GetValues(typeof(CoreStat)))
            {
                run.Stats[stat] = config.StartingStatValue;
                run.SplitSurviveCounts[stat] = 0;
            }

            foreach (BpdSymptom symptom in Enum.GetValues(typeof(BpdSymptom)))
            {
                run.SymptomSeverity[symptom] = config.MaxSymptomSeverity;
            }

            return run;
        }

        public float GetStat(CoreStat stat)
        {
            return Stats.TryGetValue(stat, out var value) ? value : 0f;
        }

        public void SetStat(CoreStat stat, float value, GameConfig config)
        {
            Stats[stat] = Mathf.Clamp(value, 0f, config.MaxStatValue);
        }

        public void ApplyStatDelta(CoreStat stat, float delta, GameConfig config)
        {
            SetStat(stat, GetStat(stat) + delta, config);
        }

        public int GetSymptomSeverity(BpdSymptom symptom)
        {
            return SymptomSeverity.TryGetValue(symptom, out var value) ? value : 0;
        }

        public void PrepareLevelStart(GameConfig config)
        {
            LevelXp = 0;
            if (TraumaticMemoriesUnlocked > 0)
            {
                float penalty = config.MemoryStabilityPenaltyFraction * TraumaticMemoriesUnlocked;
                float current = GetStat(CoreStat.Stability);
                SetStat(CoreStat.Stability, current * (1f - penalty), config);
            }

            CaptureLevelStartSnapshot();
        }

        public void CaptureLevelStartSnapshot()
        {
            _levelStartStats.Clear();
            foreach (var pair in Stats)
            {
                _levelStartStats[pair.Key] = pair.Value;
            }
        }

        public void RestoreLevelStartSnapshot(GameConfig config)
        {
            foreach (CoreStat stat in Enum.GetValues(typeof(CoreStat)))
            {
                float value = _levelStartStats.TryGetValue(stat, out var snapped)
                    ? snapped
                    : config.StartingStatValue;
                SetStat(stat, value, config);
            }

            LevelXp = 0;
            PendingLevelRestart = false;
        }

        public void RecordSplitSurvive(CoreStat failStat)
        {
            SplitSurviveCounts.TryGetValue(failStat, out var count);
            SplitSurviveCounts[failStat] = count + 1;
        }

        public int GetSplitSurviveCount(CoreStat stat)
        {
            return SplitSurviveCounts.TryGetValue(stat, out var count) ? count : 0;
        }

        public int CountSymptomPointsReduced(GameConfig config)
        {
            int total = 0;
            foreach (BpdSymptom symptom in Enum.GetValues(typeof(BpdSymptom)))
            {
                int current = GetSymptomSeverity(symptom);
                total += Math.Max(0, config.MaxSymptomSeverity - current);
            }

            return total;
        }

        /// <summary>Unlocks a traumatic memory once per id; updates TraumaticMemoriesUnlocked count.</summary>
        public bool TryUnlockMemory(string memoryId)
        {
            if (string.IsNullOrEmpty(memoryId) || !UnlockedMemoryIds.Add(memoryId))
            {
                return false;
            }

            TraumaticMemoriesUnlocked = UnlockedMemoryIds.Count;
            return true;
        }
    }
}
