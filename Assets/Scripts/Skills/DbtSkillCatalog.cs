using System;
using System.Collections.Generic;
using UnityEngine;

namespace BPD.Skills
{
    /// <summary>
    /// Resolves skill display data. Prefers Resources assets; falls back to built-in table for every enum id.
    /// </summary>
    public static class DbtSkillCatalog
    {
        public const string ResourcesFolder = "Content/Skills";

        static Dictionary<DbtSkillId, DbtSkillDefinition> _cache;
        static bool _loaded;

        public static void ResetCache()
        {
            _cache = null;
            _loaded = false;
        }

        public static DbtSkillDefinition Get(DbtSkillId id)
        {
            EnsureLoaded();
            if (id == DbtSkillId.None)
            {
                return null;
            }

            return _cache.TryGetValue(id, out var def) ? def : null;
        }

        public static string GetDisplayName(DbtSkillId id)
        {
            var def = Get(id);
            return def != null ? def.DisplayName : id.ToString();
        }

        public static string GetDescription(DbtSkillId id)
        {
            var def = Get(id);
            return def != null ? def.Description : string.Empty;
        }

        public static int GetChargesOnUnlock(DbtSkillId id, int configFallback)
        {
            var def = Get(id);
            if (def != null && def.ChargesOnUnlock > 0)
            {
                return def.ChargesOnUnlock;
            }

            return Math.Max(1, configFallback);
        }

        public static IReadOnlyCollection<DbtSkillId> AllSkillIds
        {
            get
            {
                EnsureLoaded();
                return _cache.Keys;
            }
        }

        static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _cache = new Dictionary<DbtSkillId, DbtSkillDefinition>();
            var assets = Resources.LoadAll<DbtSkillDefinition>(ResourcesFolder);
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset != null && asset.Id != DbtSkillId.None)
                    {
                        _cache[asset.Id] = asset;
                    }
                }
            }

            foreach (DbtSkillId id in Enum.GetValues(typeof(DbtSkillId)))
            {
                if (id == DbtSkillId.None || _cache.ContainsKey(id))
                {
                    continue;
                }

                _cache[id] = CreateBuiltin(id);
            }

            _loaded = true;
        }

        static DbtSkillDefinition CreateBuiltin(DbtSkillId id)
        {
            var def = ScriptableObject.CreateInstance<DbtSkillDefinition>();
            def.Id = id;
            def.hideFlags = HideFlags.HideAndDontSave;
            ApplyBuiltin(def);
            return def;
        }

        static void ApplyBuiltin(DbtSkillDefinition def)
        {
            switch (def.Id)
            {
                case DbtSkillId.Stop:
                    Fill(def, "STOP", DbtSkillModule.DistressTolerance,
                        "Stop. Take a step back. Observe. Proceed mindfully — pause before the urge chooses for you.", 2);
                    break;
                case DbtSkillId.Tipp:
                    Fill(def, "TIPP", DbtSkillModule.DistressTolerance,
                        "Change Temperature, use Intense exercise, Paced breathing, or Paired muscle relaxation to turn the volume down on crisis.", 2);
                    break;
                case DbtSkillId.UrgeSurfing:
                    Fill(def, "Urge Surfing", DbtSkillModule.DistressTolerance,
                        "Ride the urge like a wave. It rises, peaks, and falls — you do not have to act on the crest.", 2);
                    break;
                case DbtSkillId.Grounding54321:
                    Fill(def, "5-4-3-2-1 Grounding", DbtSkillModule.DistressTolerance,
                        "Name 5 things you see, 4 you feel, 3 you hear, 2 you smell, 1 you taste — return to this moment.", 2);
                    break;
                case DbtSkillId.SelfSoothing:
                    Fill(def, "Self-Soothing", DbtSkillModule.DistressTolerance,
                        "Care for your senses gently — warmth, scent, sound, touch — without making the crisis worse.", 2);
                    break;
                case DbtSkillId.RadicalAcceptance:
                    Fill(def, "Radical Acceptance", DbtSkillModule.DistressTolerance,
                        "Accept reality as it is in this moment. Acceptance is not approval; it is ending the fight with what already is.", 2);
                    break;
                case DbtSkillId.Observe:
                    Fill(def, "Observe", DbtSkillModule.Mindfulness,
                        "Notice thoughts, feelings, and sensations without grabbing them or pushing them away.", 2);
                    break;
                case DbtSkillId.Describe:
                    Fill(def, "Describe", DbtSkillModule.Mindfulness,
                        "Put words to what you notice — facts first, labels that stick less than judgments.", 2);
                    break;
                case DbtSkillId.Participate:
                    Fill(def, "Participate", DbtSkillModule.Mindfulness,
                        "Throw yourself into the one activity in front of you. Be in it, not only watching it.", 2);
                    break;
                case DbtSkillId.WiseMind:
                    Fill(def, "Wise Mind", DbtSkillModule.Mindfulness,
                        "Find the middle path where Emotion Mind and Reasonable Mind meet — intuition plus facts.", 2);
                    break;
                case DbtSkillId.OneMindfully:
                    Fill(def, "One-Mindfully", DbtSkillModule.Mindfulness,
                        "Do one thing at a time with your full attention. Multitasking scatters regulation.", 2);
                    break;
                case DbtSkillId.NameTheEmotion:
                    Fill(def, "Name the Emotion", DbtSkillModule.EmotionRegulation,
                        "Put a precise name on what you feel. Naming reduces the blur of overwhelm.", 2);
                    break;
                case DbtSkillId.CheckTheFacts:
                    Fill(def, "Check the Facts", DbtSkillModule.EmotionRegulation,
                        "Ask whether the emotion fits the facts of the situation — or a story built on fear.", 2);
                    break;
                case DbtSkillId.OppositeAction:
                    Fill(def, "Opposite Action", DbtSkillModule.EmotionRegulation,
                        "When the emotion urges a harmful action and does not fit the facts, act opposite — gently, fully.", 2);
                    break;
                case DbtSkillId.BuildPositiveExperiences:
                    Fill(def, "Build Positive Experiences", DbtSkillModule.EmotionRegulation,
                        "Schedule small moments that matter. A life worth living is built in ordinary days.", 2);
                    break;
                case DbtSkillId.Please:
                    Fill(def, "PLEASE", DbtSkillModule.EmotionRegulation,
                        "Treat PhysicaL illness, balance Eating, avoid mood-Altering substances, prioritize Sleep, Exercise — reduce vulnerability.", 2);
                    break;
                case DbtSkillId.EmotionsAreTemporary:
                    Fill(def, "Emotions Are Temporary", DbtSkillModule.Psychoeducation,
                        "Feelings intensify and pass. You can survive this wave without treating it as forever.", 2);
                    break;
                case DbtSkillId.Values:
                    Fill(def, "Values", DbtSkillModule.Psychoeducation,
                        "Ask what matters to you beyond the urge — let values steer the next small step.", 2);
                    break;
                case DbtSkillId.DearMan:
                    Fill(def, "DEAR MAN", DbtSkillModule.Interpersonal,
                        "Describe, Express, Assert, Reinforce — stay Mindful, Appear confident, Negotiate when you ask for what you need.", 2);
                    break;
                case DbtSkillId.Give:
                    Fill(def, "GIVE", DbtSkillModule.Interpersonal,
                        "Be Gentle, act Interested, Validate, keep an Easy manner — protect the relationship while you speak.", 2);
                    break;
                case DbtSkillId.Fast:
                    Fill(def, "FAST", DbtSkillModule.Interpersonal,
                        "Be Fair, no unnecessary Apologies, Stick to values, stay Truthful — keep self-respect in the conversation.", 2);
                    break;
                case DbtSkillId.Validation:
                    Fill(def, "Validation", DbtSkillModule.Interpersonal,
                        "Acknowledge that feelings make sense given history and context — without agreeing that every urge must win.", 2);
                    break;
                case DbtSkillId.Boundaries:
                    Fill(def, "Boundaries", DbtSkillModule.Interpersonal,
                        "Name limits clearly. Closeness without boundaries becomes fusion or resentment.", 2);
                    break;
                case DbtSkillId.AskForSupport:
                    Fill(def, "Ask for Support", DbtSkillModule.Interpersonal,
                        "Reach out with a clear ask. Needing people is human — isolation is not the only option.", 2);
                    break;
                case DbtSkillId.Repair:
                    Fill(def, "Repair", DbtSkillModule.Interpersonal,
                        "After rupture, return with accountability and care. Repair is a skill, not a performance of perfection.", 2);
                    break;
                case DbtSkillId.SelfValidation:
                    Fill(def, "Self-Validation", DbtSkillModule.Psychoeducation,
                        "Tell yourself the feeling is understandable. You can validate pain and still choose a wiser next step.", 2);
                    break;
                case DbtSkillId.RecoveryIsntLinear:
                    Fill(def, "Recovery Isn't Linear", DbtSkillModule.Psychoeducation,
                        "Setbacks are part of the path. Progress includes returning to skills after hard days.", 2);
                    break;
                case DbtSkillId.NeedsArentTheEnemy:
                    Fill(def, "Needs Aren't the Enemy", DbtSkillModule.Psychoeducation,
                        "Needs signal what matters. Shame about needing does not make the need disappear.", 2);
                    break;
                case DbtSkillId.MultipleThingsCanBeTrue:
                    Fill(def, "Multiple Things Can Be True", DbtSkillModule.Psychoeducation,
                        "You can feel hurt and still care. Dialectics: two truths can sit together without erasing either.", 2);
                    break;
                default:
                    Fill(def, def.Id.ToString(), DbtSkillModule.Psychoeducation,
                        "PLACEHOLDER: educational DBT skill description.", 2);
                    break;
            }
        }

        static void Fill(DbtSkillDefinition def, string name, DbtSkillModule module, string description, int charges)
        {
            def.DisplayName = name;
            def.Module = module;
            def.Description = description;
            def.ChargesOnUnlock = charges;
            def.IsProductive = true;
        }
    }
}
