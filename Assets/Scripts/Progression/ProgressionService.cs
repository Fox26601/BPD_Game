using System;
using BPD.Core;
using BPD.Level;
using BPD.Run;
using BPD.Skills;

namespace BPD.Progression
{
    public sealed class ProgressionService
    {
        readonly GameConfig _config;

        public ProgressionService(GameConfig config)
        {
            _config = config;
        }

        public void GrantLevelWinXp(RunState run)
        {
            GrantXp(run, _config.XpPerLevelWin);
        }

        public void GrantSplitSuccessXp(RunState run)
        {
            GrantXp(run, _config.XpPerSplitSuccess);
        }

        public void GrantXp(RunState run, int amount)
        {
            run.Xp += amount;
            run.UnspentXp += amount;
        }

        public bool TryReduceSymptom(RunState run, BpdSymptom symptom)
        {
            int severity = run.GetSymptomSeverity(symptom);
            if (severity <= 0 || run.UnspentXp < _config.XpCostPerSymptomPoint)
            {
                return false;
            }

            run.UnspentXp -= _config.XpCostPerSymptomPoint;
            run.SymptomSeverity[symptom] = severity - 1;
            return true;
        }

        public void UnlockSkill(RunState run, DbtSkillId skill)
        {
            if (skill == DbtSkillId.None)
            {
                return;
            }

            if (!run.UnlockedSkills.Contains(skill))
            {
                run.UnlockedSkills.Add(skill);
            }

            int chargesOnUnlock = DbtSkillCatalog.GetChargesOnUnlock(skill, _config.DbtChargesOnUnlock);
            run.SkillCharges.TryGetValue(skill, out var charges);
            run.SkillCharges[skill] = Math.Max(charges, chargesOnUnlock);
        }

        public bool TryConsumeSkillCharge(RunState run, DbtSkillId skill)
        {
            if (!run.SkillCharges.TryGetValue(skill, out var charges) || charges <= 0)
            {
                return false;
            }

            run.SkillCharges[skill] = charges - 1;
            return true;
        }

        public bool HasUsableSkill(RunState run)
        {
            foreach (var pair in run.SkillCharges)
            {
                if (pair.Value > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanUseSkillOnCard(RunState run, CardDefinition card)
        {
            if (card == null || !card.AllowSkill)
            {
                return false;
            }

            return ResolveSkillForCard(run, card) != DbtSkillId.None;
        }

        public DbtSkillId ResolveSkillForCard(RunState run, CardDefinition card)
        {
            if (card == null || !card.AllowSkill)
            {
                return DbtSkillId.None;
            }

            if (card.RequiredSkill != DbtSkillId.None)
            {
                if (run.SkillCharges.TryGetValue(card.RequiredSkill, out var charges) && charges > 0)
                {
                    return card.RequiredSkill;
                }

                return DbtSkillId.None;
            }

            return FirstUsableSkill(run);
        }

        public int GetCharges(RunState run, DbtSkillId skill)
        {
            if (skill == DbtSkillId.None)
            {
                return 0;
            }

            run.SkillCharges.TryGetValue(skill, out var charges);
            return Math.Max(0, charges);
        }

        public DbtSkillId FirstUsableSkill(RunState run)
        {
            foreach (var skill in run.UnlockedSkills)
            {
                if (run.SkillCharges.TryGetValue(skill, out var charges) && charges > 0)
                {
                    return skill;
                }
            }

            return DbtSkillId.None;
        }

        public void AdvanceToNextLevel(RunState run)
        {
            run.LevelIndex++;
            run.PendingLevelRestart = false;
        }

        public void MarkLevelCleared(RunState run)
        {
            run.LevelsCleared++;
        }

        public bool HasReachedEnding(RunState run)
        {
            return run.LevelsCleared >= _config.LevelsBeforeEnding;
        }

        public void MarkLevelRestart(RunState run)
        {
            run.PendingLevelRestart = true;
        }
    }
}
