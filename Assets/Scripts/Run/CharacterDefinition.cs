using UnityEngine;

namespace BPD.Run
{
    [CreateAssetMenu(fileName = "SO_Character_", menuName = "BPD/Content/Character Background")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        public CharacterId Id;
        public string DisplayNameKey = "ui.character.a";
        public string StrengthsKey = "character.stub.strengths";
        public string WeaknessesKey = "character.stub.weaknesses";
        public string[] ImpulsivityTags = { "PLACEHOLDER" };
    }
}
