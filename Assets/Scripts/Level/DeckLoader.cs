using System.Collections.Generic;
using BPD.Core;
using BPD.Run;
using UnityEngine;

namespace BPD.Level
{
    /// <summary>
    /// Loads authored card decks from Resources by act; applies level-band offset and inclusion gates.
    /// Falls back to StubDeckFactory when missing.
    /// </summary>
    public static class DeckLoader
    {
        public const string DefaultDeckResourcePath = "Content/Decks/SO_Deck_Default";

        public static List<CardDefinition> LoadDefaultDeck(int cardCount, bool forceLoseAtEnd = false)
        {
            var config = GameConfig.LoadOrCreateRuntimeDefault();
            return LoadDeckForLevel(0, cardCount, config, forceLoseAtEnd);
        }

        public static List<CardDefinition> LoadDeckForLevel(
            int levelIndex,
            int cardCount,
            GameConfig config,
            bool forceLoseAtEnd = false,
            RunState run = null)
        {
            cardCount = Mathf.Max(1, cardCount);
            int actIndex = config.ResolveActIndex(levelIndex);
            int bandOffset = config.ResolveLevelBandOffset(levelIndex);
            var deckAsset = LoadDeckForAct(actIndex);
            if (deckAsset != null && deckAsset.Cards != null && deckAsset.Cards.Length > 0)
            {
                var cards = BuildCardList(deckAsset, cardCount, bandOffset, run);
                if (cards.Count > 0)
                {
                    if (forceLoseAtEnd)
                    {
                        cards[cards.Count - 1] = StubDeckFactory.CreateForceLoseVariant(cards[cards.Count - 1]);
                    }

                    return cards;
                }
            }

            return StubDeckFactory.CreateDefaultDeck(cardCount, forceLoseAtEnd);
        }

        static CardDeckDefinition LoadDeckForAct(int actIndex)
        {
            string actPath = $"Content/Decks/Act{actIndex + 1:00}/SO_Deck_Default";
            var deck = Resources.Load<CardDeckDefinition>(actPath);
            if (deck != null)
            {
                return deck;
            }

            return Resources.Load<CardDeckDefinition>(DefaultDeckResourcePath);
        }

        static List<CardDefinition> BuildCardList(
            CardDeckDefinition deckAsset,
            int cardCount,
            int bandOffset,
            RunState run)
        {
            var pool = new List<CardDefinition>(deckAsset.Cards.Length);
            foreach (var source in deckAsset.Cards)
            {
                if (source == null)
                {
                    continue;
                }

                if (run != null && !source.PassesGate(run))
                {
                    continue;
                }

                pool.Add(source);
            }

            if (pool.Count == 0)
            {
                foreach (var source in deckAsset.Cards)
                {
                    if (source != null)
                    {
                        pool.Add(source);
                    }
                }
            }

            var cards = new List<CardDefinition>(cardCount);
            if (pool.Count == 0)
            {
                return cards;
            }

            for (int i = 0; i < cardCount; i++)
            {
                int index = (bandOffset + i) % pool.Count;
                cards.Add(pool[index]);
            }

            return cards;
        }
    }
}
