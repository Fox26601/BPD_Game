using System;
using UnityEngine;

namespace BPD.Therapy
{
    [Serializable]
    public sealed class TherapyCardDefinition
    {
        public string Id;
        public TherapyCardKind Kind;
        public string TextKey;
        public string MemoryId;
        public string SkillUnlockId;
    }

    [Serializable]
    public sealed class TherapyEmotionRecipe
    {
        public string SituationId;
        public string ThoughtId;
        public string EmotionId;
    }

    [Serializable]
    public sealed class TherapyNeedRecipe
    {
        public string EmotionId;
        public string BehaviorId;
        public string NeedId;
    }

    /// <summary>
    /// Data-driven therapy content. Place under Resources/Content/Therapy/.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_TherapyCatalog", menuName = "BPD/Content/Therapy Catalog")]
    public sealed class TherapyCatalogDefinition : ScriptableObject
    {
        public TherapyCardDefinition[] Cards = Array.Empty<TherapyCardDefinition>();
        public TherapyEmotionRecipe[] EmotionRecipes = Array.Empty<TherapyEmotionRecipe>();
        public TherapyNeedRecipe[] NeedRecipes = Array.Empty<TherapyNeedRecipe>();
    }
}
