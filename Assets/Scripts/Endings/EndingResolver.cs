using System;
using System.Collections.Generic;
using BPD.Core;
using BPD.Localization;
using BPD.Run;
using BPD.Stats;

namespace BPD.Endings
{
    /// <summary>
    /// Picks run ending fantasy from Split survives, symptom work, therapy progress, and character background.
    /// Weights locked in Docs/Canon-Mechanics.md.
    /// </summary>
    public static class EndingResolver
    {
        const int SplitSurviveWeight = 3;
        const int SymptomHalfWeight = 1;
        const int MemoryWeight = 2;
        const int EmotionUnlockWeight = 2;
        const int NeedUnlockWeight = 2;
        const int SkillUnlockWeight = 1;

        public static CoreStat ResolveDominantFantasy(RunState run, GameConfig config)
        {
            var scores = new Dictionary<CoreStat, int>();
            foreach (CoreStat stat in Enum.GetValues(typeof(CoreStat)))
            {
                scores[stat] = 0;
            }

            foreach (CoreStat stat in Enum.GetValues(typeof(CoreStat)))
            {
                scores[stat] += run.GetSplitSurviveCount(stat) * SplitSurviveWeight;
            }

            int symptomReduced = run.CountSymptomPointsReduced(config);
            scores[CoreStat.Stability] += symptomReduced * SymptomHalfWeight;
            scores[CoreStat.Wholeness] += symptomReduced * SymptomHalfWeight;

            scores[CoreStat.Wholeness] += run.TraumaticMemoriesUnlocked * MemoryWeight;
            scores[CoreStat.Stability] += run.UnlockedEmotions.Count * EmotionUnlockWeight;
            scores[CoreStat.Closeness] += run.UnlockedNeeds.Count * NeedUnlockWeight;
            scores[CoreStat.Wholeness] += run.UnlockedSkills.Count * SkillUnlockWeight;

            ApplyCharacterBias(run.Character, scores);

            return PickBest(scores, config);
        }

        static void ApplyCharacterBias(CharacterId character, Dictionary<CoreStat, int> scores)
        {
            switch (character)
            {
                case CharacterId.CharacterA:
                    scores[CoreStat.Safety] += 2;
                    scores[CoreStat.Wholeness] += 1;
                    break;
                case CharacterId.CharacterB:
                    scores[CoreStat.Stability] += 2;
                    scores[CoreStat.Wholeness] += 1;
                    break;
                case CharacterId.CharacterC:
                    scores[CoreStat.Closeness] += 2;
                    scores[CoreStat.Safety] += 1;
                    break;
            }
        }

        static CoreStat PickBest(Dictionary<CoreStat, int> scores, GameConfig config)
        {
            CoreStat chosen = config.SplitTieBreakOrder[0];
            int best = int.MinValue;

            foreach (var candidate in config.SplitTieBreakOrder)
            {
                int score = scores[candidate];
                if (score > best)
                {
                    best = score;
                    chosen = candidate;
                }
            }

            foreach (CoreStat stat in Enum.GetValues(typeof(CoreStat)))
            {
                int score = scores[stat];
                if (score > best)
                {
                    best = score;
                    chosen = stat;
                }
                else if (score == best)
                {
                    int currentIndex = Array.IndexOf(config.SplitTieBreakOrder, chosen);
                    int newIndex = Array.IndexOf(config.SplitTieBreakOrder, stat);
                    if (newIndex >= 0 && (currentIndex < 0 || newIndex < currentIndex))
                    {
                        chosen = stat;
                    }
                }
            }

            return chosen;
        }

        public static string TitleKey(CoreStat fantasy) => fantasy switch
        {
            CoreStat.Safety => "ending.safety.title",
            CoreStat.Stability => "ending.stability.title",
            CoreStat.Wholeness => "ending.wholeness.title",
            CoreStat.Closeness => "ending.closeness.title",
            _ => "ending.wholeness.title"
        };

        public static string BodyKey(CoreStat fantasy, CharacterId character)
        {
            string suffix = character switch
            {
                CharacterId.CharacterA => ".a",
                CharacterId.CharacterB => ".b",
                CharacterId.CharacterC => ".c",
                _ => ".a"
            };
            string keyed = BaseBodyKey(fantasy) + suffix;
            string resolved = Loc.Get(keyed);
            if (!resolved.StartsWith("[", StringComparison.Ordinal))
            {
                return keyed;
            }

            return BaseBodyKey(fantasy);
        }

        static string BaseBodyKey(CoreStat fantasy) => fantasy switch
        {
            CoreStat.Safety => "ending.safety.body",
            CoreStat.Stability => "ending.stability.body",
            CoreStat.Wholeness => "ending.wholeness.body",
            CoreStat.Closeness => "ending.closeness.body",
            _ => "ending.wholeness.body"
        };
    }
}
