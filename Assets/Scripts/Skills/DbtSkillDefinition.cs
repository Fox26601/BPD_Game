using UnityEngine;

namespace BPD.Skills
{
    public enum DbtSkillModule
    {
        Mindfulness = 0,
        DistressTolerance = 1,
        EmotionRegulation = 2,
        Interpersonal = 3,
        Psychoeducation = 4
    }

    [CreateAssetMenu(fileName = "SO_Skill_", menuName = "BPD/Content/DBT Skill")]
    public sealed class DbtSkillDefinition : ScriptableObject
    {
        public DbtSkillId Id = DbtSkillId.None;
        public string DisplayName = "PLACEHOLDER skill";
        public DbtSkillModule Module = DbtSkillModule.Mindfulness;
        [TextArea(2, 6)]
        public string Description = "PLACEHOLDER description";
        public int ChargesOnUnlock = 2;
        public bool IsProductive = true;
    }
}
