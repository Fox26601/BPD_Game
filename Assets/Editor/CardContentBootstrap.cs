using BPD.Level;
using BPD.Skills;
using UnityEditor;
using UnityEngine;

namespace BPD.EditorTools
{
    /// <summary>
    /// Seeds PLACEHOLDER card + deck assets under Resources/Content when missing.
    /// </summary>
    public static class CardContentBootstrap
    {
        const string CardsFolder = "Assets/Resources/Content/Cards";
        const string DecksFolder = "Assets/Resources/Content/Decks";
        const string DeckPath = DecksFolder + "/SO_Deck_Default.asset";

        [MenuItem("BPD/Content/Seed Default Card Deck")]
        public static void SeedDefaultDeck()
        {
            EnsureFolders();
            var cards = new CardDefinition[5];
            for (int i = 0; i < 5; i++)
            {
                cards[i] = CreateOrUpdateCard(i + 1);
            }

            var deck = AssetDatabase.LoadAssetAtPath<CardDeckDefinition>(DeckPath);
            if (deck == null)
            {
                deck = ScriptableObject.CreateInstance<CardDeckDefinition>();
                AssetDatabase.CreateAsset(deck, DeckPath);
            }

            deck.Id = "deck_default";
            deck.ActIndex = 1;
            deck.Cards = cards;
            EditorUtility.SetDirty(deck);

            EnsureActDeck(1, cards);
            EnsureActDeck(2, cards);
            EnsureActDeck(3, cards);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("BPD: seeded default card deck at " + DeckPath);
        }

        [InitializeOnLoadMethod]
        static void EnsureDefaultContent()
        {
            if (AssetDatabase.LoadAssetAtPath<CardDeckDefinition>(DeckPath) != null)
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<CardDeckDefinition>(DeckPath) == null)
                {
                    SeedDefaultDeck();
                }
            };
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources/Content"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Content");
            }

            if (!AssetDatabase.IsValidFolder(CardsFolder))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Content", "Cards");
            }

            if (!AssetDatabase.IsValidFolder(DecksFolder))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Content", "Decks");
            }
        }

        static CardDefinition CreateOrUpdateCard(int index)
        {
            string path = $"{CardsFolder}/SO_Card_{index:000}.asset";
            var card = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
            if (card == null)
            {
                card = ScriptableObject.CreateInstance<CardDefinition>();
                AssetDatabase.CreateAsset(card, path);
            }

            card.Id = $"card_stub_{index:000}";
            StubDeckFactory.ApplyStubContent(card, index, forceLose: false);
            EditorUtility.SetDirty(card);
            return card;
        }

        static void EnsureActDeck(int actNumber, CardDefinition[] cards)
        {
            string actFolder = $"{DecksFolder}/Act{actNumber:00}";
            if (!AssetDatabase.IsValidFolder(actFolder))
            {
                AssetDatabase.CreateFolder(DecksFolder, $"Act{actNumber:00}");
            }

            string actDeckPath = $"{actFolder}/SO_Deck_Default.asset";
            var actDeck = AssetDatabase.LoadAssetAtPath<CardDeckDefinition>(actDeckPath);
            if (actDeck == null)
            {
                actDeck = ScriptableObject.CreateInstance<CardDeckDefinition>();
                AssetDatabase.CreateAsset(actDeck, actDeckPath);
            }

            actDeck.Id = $"deck_act{actNumber:00}";
            actDeck.ActIndex = actNumber;
            actDeck.Cards = cards;
            EditorUtility.SetDirty(actDeck);
        }
    }
}
