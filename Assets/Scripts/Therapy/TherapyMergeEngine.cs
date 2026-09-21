using System;
using System.Collections.Generic;
using UnityEngine;

namespace BPD.Therapy
{
    /// <summary>
    /// Rule 1: Situation + Thought → Emotion.
    /// Rule 2: Emotion + Behavior → Need (may unlock a DBT skill).
    /// Loads from Resources/Content/Therapy when present; otherwise stub recipes.
    /// </summary>
    public sealed class TherapyMergeEngine
    {
        public const string ResourcesFolder = "Content/Therapy";

        readonly Dictionary<string, TherapyCard> _emotionRecipes = new Dictionary<string, TherapyCard>(StringComparer.Ordinal);
        readonly Dictionary<string, TherapyCard> _needRecipes = new Dictionary<string, TherapyCard>(StringComparer.Ordinal);
        readonly Dictionary<string, TherapyCard> _cardsById = new Dictionary<string, TherapyCard>(StringComparer.Ordinal);

        public void RegisterEmotionRecipe(string situationId, string thoughtId, TherapyCard emotion)
        {
            _emotionRecipes[Key(situationId, thoughtId)] = emotion;
            RegisterCard(emotion);
        }

        public void RegisterNeedRecipe(string emotionId, string behaviorId, TherapyCard need)
        {
            _needRecipes[Key(emotionId, behaviorId)] = need;
            RegisterCard(need);
        }

        public void RegisterCard(TherapyCard card)
        {
            if (card == null || string.IsNullOrEmpty(card.Id))
            {
                return;
            }

            _cardsById[card.Id] = card;
        }

        public bool TryGetCard(string id, out TherapyCard card)
        {
            card = null;
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            if (_cardsById.TryGetValue(id, out card) && card != null)
            {
                return true;
            }

            if (StubCards != null)
            {
                for (int i = 0; i < StubCards.Count; i++)
                {
                    var candidate = StubCards[i];
                    if (candidate != null && string.Equals(candidate.Id, id, StringComparison.Ordinal))
                    {
                        card = candidate;
                        _cardsById[id] = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        public TherapyMergeResult TryMerge(TherapyCard a, TherapyCard b)
        {
            if (a == null || b == null)
            {
                return Fail("therapy.error.null");
            }

            if (TryPair(a, b, TherapyCardKind.Situation, TherapyCardKind.Thought, out var situation, out var thought))
            {
                if (_emotionRecipes.TryGetValue(Key(situation.Id, thought.Id), out var emotion))
                {
                    emotion.Locked = false;
                    return new TherapyMergeResult { Success = true, Produced = Clone(emotion) };
                }

                return Fail("therapy.error.no_emotion_recipe");
            }

            if (TryPair(a, b, TherapyCardKind.Emotion, TherapyCardKind.Behavior, out var emotionCard, out var behavior))
            {
                if (emotionCard.Locked)
                {
                    return Fail("therapy.error.emotion_locked");
                }

                if (_needRecipes.TryGetValue(Key(emotionCard.Id, behavior.Id), out var need))
                {
                    need.Locked = false;
                    return new TherapyMergeResult { Success = true, Produced = Clone(need) };
                }

                return Fail("therapy.error.no_need_recipe");
            }

            return Fail("therapy.error.invalid_pair");
        }

        static TherapyCard Clone(TherapyCard source)
        {
            return new TherapyCard
            {
                Id = source.Id,
                Kind = source.Kind,
                TextKey = source.TextKey,
                Locked = source.Locked,
                MemoryId = source.MemoryId,
                SkillUnlockId = source.SkillUnlockId
            };
        }

        static bool TryPair(
            TherapyCard a,
            TherapyCard b,
            TherapyCardKind kindA,
            TherapyCardKind kindB,
            out TherapyCard first,
            out TherapyCard second)
        {
            if (a.Kind == kindA && b.Kind == kindB)
            {
                first = a;
                second = b;
                return true;
            }

            if (a.Kind == kindB && b.Kind == kindA)
            {
                first = b;
                second = a;
                return true;
            }

            first = null;
            second = null;
            return false;
        }

        static string Key(string a, string b) => $"{a}|{b}";

        static TherapyMergeResult Fail(string key) =>
            new TherapyMergeResult { Success = false, FailureReasonKey = key };

        public static TherapyMergeEngine LoadOrCreateStub()
        {
            var catalogs = Resources.LoadAll<TherapyCatalogDefinition>(ResourcesFolder);
            if (catalogs != null && catalogs.Length > 0)
            {
                var engine = new TherapyMergeEngine();
                var cards = new List<TherapyCard>();
                foreach (var catalog in catalogs)
                {
                    if (catalog == null)
                    {
                        continue;
                    }

                    ApplyCatalog(engine, catalog, cards);
                }

                if (cards.Count > 0)
                {
                    engine.StubCards = cards;
                    return engine;
                }
            }

            var json = Resources.Load<TextAsset>(ResourcesFolder + "/therapy_catalog_PLACEHOLDER");
            if (json != null && !string.IsNullOrWhiteSpace(json.text))
            {
                var fromJson = TryLoadFromJson(json.text);
                if (fromJson != null)
                {
                    return fromJson;
                }
            }

            return CreateStub();
        }

        static TherapyMergeEngine TryLoadFromJson(string json)
        {
            TherapyJsonRoot root;
            try
            {
                root = JsonUtility.FromJson<TherapyJsonRoot>(json);
            }
            catch
            {
                return null;
            }

            if (root == null || root.cards == null || root.cards.Length == 0)
            {
                return null;
            }

            var byId = new Dictionary<string, TherapyCard>(StringComparer.Ordinal);
            var cards = new List<TherapyCard>();
            foreach (var src in root.cards)
            {
                if (src == null || string.IsNullOrEmpty(src.id))
                {
                    continue;
                }

                var kind = ParseKind(src.kind);
                var card = new TherapyCard
                {
                    Id = src.id,
                    Kind = kind,
                    TextKey = src.textKey,
                    Locked = kind is TherapyCardKind.Emotion or TherapyCardKind.Need,
                    MemoryId = src.memoryId,
                    SkillUnlockId = src.skillUnlockId
                };
                byId[card.Id] = card;
                cards.Add(card);
            }

            var engine = new TherapyMergeEngine();
            foreach (var card in cards)
            {
                engine.RegisterCard(card);
            }

            if (root.emotionRecipes != null)
            {
                foreach (var recipe in root.emotionRecipes)
                {
                    if (recipe == null || !byId.TryGetValue(recipe.emotionId, out var emotion))
                    {
                        continue;
                    }

                    engine.RegisterEmotionRecipe(recipe.situationId, recipe.thoughtId, emotion);
                }
            }

            if (root.needRecipes != null)
            {
                foreach (var recipe in root.needRecipes)
                {
                    if (recipe == null || !byId.TryGetValue(recipe.needId, out var need))
                    {
                        continue;
                    }

                    engine.RegisterNeedRecipe(recipe.emotionId, recipe.behaviorId, need);
                }
            }

            if (cards.Count == 0)
            {
                return null;
            }

            engine.StubCards = cards;
            return engine;
        }

        static TherapyCardKind ParseKind(string kind)
        {
            return Enum.TryParse(kind, true, out TherapyCardKind parsed)
                ? parsed
                : TherapyCardKind.Situation;
        }

        [Serializable]
        sealed class TherapyJsonRoot
        {
            public TherapyJsonCard[] cards;
            public TherapyJsonEmotionRecipe[] emotionRecipes;
            public TherapyJsonNeedRecipe[] needRecipes;
        }

        [Serializable]
        sealed class TherapyJsonCard
        {
            public string id;
            public string kind;
            public string textKey;
            public string memoryId;
            public string skillUnlockId;
        }

        [Serializable]
        sealed class TherapyJsonEmotionRecipe
        {
            public string situationId;
            public string thoughtId;
            public string emotionId;
        }

        [Serializable]
        sealed class TherapyJsonNeedRecipe
        {
            public string emotionId;
            public string behaviorId;
            public string needId;
        }

        static void ApplyCatalog(TherapyMergeEngine engine, TherapyCatalogDefinition catalog, List<TherapyCard> cards)
        {
            var byId = new Dictionary<string, TherapyCard>(StringComparer.Ordinal);
            if (catalog.Cards != null)
            {
                foreach (var def in catalog.Cards)
                {
                    if (def == null || string.IsNullOrEmpty(def.Id))
                    {
                        continue;
                    }

                    var card = new TherapyCard
                    {
                        Id = def.Id,
                        Kind = def.Kind,
                        TextKey = def.TextKey,
                        Locked = def.Kind is TherapyCardKind.Emotion or TherapyCardKind.Need,
                        MemoryId = def.MemoryId,
                        SkillUnlockId = def.SkillUnlockId
                    };
                    byId[card.Id] = card;
                    cards.Add(card);
                    engine.RegisterCard(card);
                }
            }

            if (catalog.EmotionRecipes != null)
            {
                foreach (var recipe in catalog.EmotionRecipes)
                {
                    if (recipe == null || !byId.TryGetValue(recipe.EmotionId, out var emotion))
                    {
                        continue;
                    }

                    engine.RegisterEmotionRecipe(recipe.SituationId, recipe.ThoughtId, emotion);
                }
            }

            if (catalog.NeedRecipes != null)
            {
                foreach (var recipe in catalog.NeedRecipes)
                {
                    if (recipe == null || !byId.TryGetValue(recipe.NeedId, out var need))
                    {
                        continue;
                    }

                    engine.RegisterNeedRecipe(recipe.EmotionId, recipe.BehaviorId, need);
                }
            }
        }

        public static TherapyMergeEngine CreateStub()
        {
            var engine = new TherapyMergeEngine();
            var cards = new List<TherapyCard>();

            AddPair(engine, cards, 1, "WiseMind");
            AddPair(engine, cards, 2, "Tipp");
            AddPair(engine, cards, 3, "Repair");

            engine.StubCards = cards;
            return engine;
        }

        static void AddPair(TherapyMergeEngine engine, List<TherapyCard> cards, int index, string skillUnlockId)
        {
            string n = index.ToString("00");
            var situation = new TherapyCard
            {
                Id = $"therapy_stub_situation_{n}",
                Kind = TherapyCardKind.Situation,
                TextKey = $"therapy.stub.situation.{n}"
            };
            var thought = new TherapyCard
            {
                Id = $"therapy_stub_thought_{n}",
                Kind = TherapyCardKind.Thought,
                TextKey = $"therapy.stub.thought.{n}"
            };
            var behavior = new TherapyCard
            {
                Id = $"therapy_stub_behavior_{n}",
                Kind = TherapyCardKind.Behavior,
                TextKey = $"therapy.stub.behavior.{n}"
            };
            var emotion = new TherapyCard
            {
                Id = $"therapy_stub_emotion_{n}",
                Kind = TherapyCardKind.Emotion,
                TextKey = $"therapy.stub.emotion.{n}",
                Locked = true,
                MemoryId = index == 1 ? "memory_stub_01" : null
            };
            var need = new TherapyCard
            {
                Id = $"therapy_stub_need_{n}",
                Kind = TherapyCardKind.Need,
                TextKey = $"therapy.stub.need.{n}",
                Locked = true,
                SkillUnlockId = skillUnlockId
            };

            engine.RegisterEmotionRecipe(situation.Id, thought.Id, emotion);
            engine.RegisterNeedRecipe(emotion.Id, behavior.Id, need);
            engine.RegisterCard(situation);
            engine.RegisterCard(thought);
            engine.RegisterCard(behavior);
            cards.Add(situation);
            cards.Add(thought);
            cards.Add(behavior);
            cards.Add(emotion);
            cards.Add(need);
        }

        public List<TherapyCard> StubCards { get; private set; } = new List<TherapyCard>();
    }
}
