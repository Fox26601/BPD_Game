using UnityEngine;

namespace BPD.Level
{
    /// <summary>
    /// Ordered list of cards for a level. Place under Resources/Content/Decks.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Deck_", menuName = "BPD/Content/Card Deck")]
    public sealed class CardDeckDefinition : ScriptableObject
    {
        public string Id = "deck_default";
        [Tooltip("1-based act index for authoring (Act01 = 1).")]
        public int ActIndex = 1;
        public CardDefinition[] Cards;
    }
}
