using BPD.Stats;

namespace BPD.Skills
{
    /// <summary>
    /// Maps Split fail fantasy → productive skill unlock on survive.
    /// </summary>
    public static class SplitSkillRewards
    {
        public static DbtSkillId SkillForFailStat(CoreStat failStat) => failStat switch
        {
            CoreStat.Wholeness => DbtSkillId.WiseMind,
            CoreStat.Stability => DbtSkillId.CheckTheFacts,
            CoreStat.Safety => DbtSkillId.Stop,
            CoreStat.Closeness => DbtSkillId.DearMan,
            _ => DbtSkillId.WiseMind
        };
    }
}
