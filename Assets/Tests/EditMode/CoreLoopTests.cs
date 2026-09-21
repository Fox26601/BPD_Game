using System;
using BPD.Core;
using BPD.Endings;
using BPD.Level;
using BPD.Localization;
using BPD.Progression;
using BPD.Run;
using BPD.Skills;
using BPD.Split;
using BPD.Stats;
using BPD.Therapy;
using NUnit.Framework;
using UnityEngine;

namespace BPD.Tests.EditMode
{
    public sealed class CoreLoopTests
    {
        GameConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameConfig>();
            DbtSkillCatalog.ResetCache();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
            DbtSkillCatalog.ResetCache();
        }

        [Test]
        public void Therapy_SituationPlusThought_UnlocksEmotion()
        {
            var engine = TherapyMergeEngine.CreateStub();
            var situation = engine.StubCards.Find(c => c.Id == "therapy_stub_situation_01");
            var thought = engine.StubCards.Find(c => c.Id == "therapy_stub_thought_01");

            var result = engine.TryMerge(situation, thought);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(TherapyCardKind.Emotion, result.Produced.Kind);
            Assert.IsFalse(result.Produced.Locked);
        }

        [Test]
        public void Therapy_EmotionPlusBehavior_UnlocksNeed()
        {
            var engine = TherapyMergeEngine.CreateStub();
            var situation = engine.StubCards.Find(c => c.Id == "therapy_stub_situation_01");
            var thought = engine.StubCards.Find(c => c.Id == "therapy_stub_thought_01");
            var behavior = engine.StubCards.Find(c => c.Id == "therapy_stub_behavior_01");
            var emotion = engine.TryMerge(situation, thought).Produced;

            var result = engine.TryMerge(emotion, behavior);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(TherapyCardKind.Need, result.Produced.Kind);
            Assert.AreEqual("WiseMind", result.Produced.SkillUnlockId);
        }

        [Test]
        public void Therapy_NeedMerge_GrantsSkillChargesOnRun()
        {
            var engine = TherapyMergeEngine.CreateStub();
            var situation = engine.StubCards.Find(c => c.Id == "therapy_stub_situation_01");
            var thought = engine.StubCards.Find(c => c.Id == "therapy_stub_thought_01");
            var behavior = engine.StubCards.Find(c => c.Id == "therapy_stub_behavior_01");
            var emotion = engine.TryMerge(situation, thought).Produced;
            var need = engine.TryMerge(emotion, behavior).Produced;

            Assert.IsTrue(Enum.TryParse(need.SkillUnlockId, out DbtSkillId skill));
            Assert.AreEqual(DbtSkillId.WiseMind, skill);

            var run = RunState.CreateNew(CharacterId.CharacterA, _config);
            var progression = new ProgressionService(_config);
            progression.UnlockSkill(run, skill);

            Assert.Contains(skill, run.UnlockedSkills);
            Assert.Greater(progression.GetCharges(run, skill), 0);
        }

        [Test]
        public void Therapy_MemoryUnlock_IsDistinctPerId()
        {
            var run = RunState.CreateNew(CharacterId.CharacterA, _config);
            Assert.IsTrue(run.TryUnlockMemory("memory_stub_01"));
            Assert.IsFalse(run.TryUnlockMemory("memory_stub_01"));
            Assert.AreEqual(1, run.TraumaticMemoriesUnlocked);
            Assert.IsTrue(run.TryUnlockMemory("memory_stub_02"));
            Assert.AreEqual(2, run.TraumaticMemoriesUnlocked);
        }

        [Test]
        public void TherapySelection_ToggleSameSituationOff()
        {
            var a = new TherapyCard { Id = "s1", Kind = TherapyCardKind.Situation, TextKey = "a" };
            TherapyCard slotA = null;
            TherapyCard slotB = null;

            TherapySelection.ApplyClick(TherapySelectionPhase.EmotionPairing, a, ref slotA, ref slotB);
            Assert.AreSame(a, slotA);
            Assert.IsNull(slotB);

            TherapySelection.ApplyClick(TherapySelectionPhase.EmotionPairing, a, ref slotA, ref slotB);
            Assert.IsNull(slotA);
            Assert.IsNull(slotB);
        }

        [Test]
        public void TherapySelection_ReplaceSameKindSituation()
        {
            var a = new TherapyCard { Id = "s1", Kind = TherapyCardKind.Situation, TextKey = "a" };
            var b = new TherapyCard { Id = "s2", Kind = TherapyCardKind.Situation, TextKey = "b" };
            TherapyCard slotA = null;
            TherapyCard slotB = null;

            TherapySelection.ApplyClick(TherapySelectionPhase.EmotionPairing, a, ref slotA, ref slotB);
            TherapySelection.ApplyClick(TherapySelectionPhase.EmotionPairing, b, ref slotA, ref slotB);

            Assert.AreSame(b, slotA);
            Assert.IsNull(slotB);
            Assert.IsFalse(TherapySelection.IsSelected(slotA, slotB, a));
            Assert.IsTrue(TherapySelection.IsSelected(slotA, slotB, b));
        }

        [Test]
        public void TherapySelection_SituationAndThought_FillBothSlots()
        {
            var situation = new TherapyCard { Id = "s1", Kind = TherapyCardKind.Situation, TextKey = "s" };
            var thought = new TherapyCard { Id = "t1", Kind = TherapyCardKind.Thought, TextKey = "t" };
            TherapyCard slotA = null;
            TherapyCard slotB = null;

            TherapySelection.ApplyClick(TherapySelectionPhase.EmotionPairing, situation, ref slotA, ref slotB);
            TherapySelection.ApplyClick(TherapySelectionPhase.EmotionPairing, thought, ref slotA, ref slotB);

            Assert.IsTrue(TherapySelection.HasCompletePair(slotA, slotB));
            Assert.AreSame(situation, slotA);
            Assert.AreSame(thought, slotB);
        }

        [Test]
        public void TherapySelection_NeedPhase_EmotionAndBehavior()
        {
            var emotion = new TherapyCard { Id = "e1", Kind = TherapyCardKind.Emotion, TextKey = "e" };
            var behavior = new TherapyCard { Id = "b1", Kind = TherapyCardKind.Behavior, TextKey = "b" };
            TherapyCard slotA = emotion;
            TherapyCard slotB = null;

            TherapySelection.ApplyClick(TherapySelectionPhase.NeedPairing, behavior, ref slotA, ref slotB);
            Assert.IsTrue(TherapySelection.HasCompletePair(slotA, slotB));

            TherapySelection.ApplyClick(TherapySelectionPhase.NeedPairing, emotion, ref slotA, ref slotB);
            Assert.IsNull(slotA);
            Assert.AreSame(behavior, slotB);
        }

        [Test]
        public void Therapy_TryGetCard_FindsRegisteredStubCards()
        {
            var engine = TherapyMergeEngine.CreateStub();
            Assert.IsTrue(engine.TryGetCard("therapy_stub_situation_01", out var situation));
            Assert.AreEqual(TherapyCardKind.Situation, situation.Kind);
            Assert.IsTrue(engine.TryGetCard("therapy_stub_emotion_01", out var emotion));
            Assert.AreEqual(TherapyCardKind.Emotion, emotion.Kind);
            Assert.IsFalse(engine.TryGetCard("missing_card", out _));
        }

        [Test]
        public void Therapy_EmotionLookup_AllowsNeedMergeAfterUnlock()
        {
            var engine = TherapyMergeEngine.CreateStub();
            var situation = engine.StubCards.Find(c => c.Id == "therapy_stub_situation_02");
            var thought = engine.StubCards.Find(c => c.Id == "therapy_stub_thought_02");
            var behavior = engine.StubCards.Find(c => c.Id == "therapy_stub_behavior_02");
            var emotion = engine.TryMerge(situation, thought).Produced;
            Assert.IsTrue(engine.TryGetCard(emotion.Id, out var stored));
            stored.Locked = false;

            var result = engine.TryMerge(stored, behavior);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(TherapyCardKind.Need, result.Produced.Kind);
            Assert.AreEqual("Tipp", result.Produced.SkillUnlockId);
        }

        [Test]
        public void TherapyCatalog_LoadsFromJsonAsset()
        {
            var engine = TherapyMergeEngine.LoadOrCreateStub();
            Assert.IsNotNull(engine);
            Assert.GreaterOrEqual(engine.StubCards.Count, 9);
            var situation = engine.StubCards.Find(c => c.Id == "therapy_stub_situation_01");
            var thought = engine.StubCards.Find(c => c.Id == "therapy_stub_thought_01");
            Assert.IsNotNull(situation);
            Assert.IsNotNull(thought);
            Assert.IsTrue(engine.TryMerge(situation, thought).Success);
        }

        [Test]
        public void CardEffect_AppliesStatDeltas()
        {
            var run = RunState.CreateNew(CharacterId.CharacterA, _config);
            var modifiers = new ModifierRegistry();
            var effect = new DefaultCardEffect(modifiers);
            float before = run.GetStat(CoreStat.Closeness);

            effect.Apply(run, new CardChoice
            {
                StatDeltas = new[] { new StatDelta { Stat = CoreStat.Closeness, Amount = 7f } }
            }, _config);

            Assert.AreEqual(before + 7f, run.GetStat(CoreStat.Closeness), 0.01f);
        }

        [Test]
        public void CardEffect_AppliesHiddenAndRelationshipOps()
        {
            var run = RunState.CreateNew(CharacterId.CharacterA, _config);
            var effect = new DefaultCardEffect(new ModifierRegistry());
            effect.Apply(run, new CardChoice
            {
                StatDeltas = Array.Empty<StatDelta>(),
                HiddenVariableOps = new[] { new VariableOp { Key = "abandon_alarm", Delta = 2 } },
                RelationshipOps = new[] { new VariableOp { Key = "friend_a", Delta = 3 } }
            }, _config);

            Assert.AreEqual(2, run.HiddenVariables["abandon_alarm"]);
            Assert.AreEqual(3, run.Relationships["friend_a"]);
        }

        [Test]
        public void ImpulsivityReduction_SlowsSafetyDecay()
        {
            var run = RunState.CreateNew(CharacterId.CharacterB, _config);
            var modifiers = new ModifierRegistry();
            float atMax = modifiers.GetSafetyNegativeDeltaScale(run, _config);

            run.SymptomSeverity[BpdSymptom.Impulsivity] = 0;
            float atMin = modifiers.GetSafetyNegativeDeltaScale(run, _config);

            Assert.Less(atMin, atMax);
        }

        [Test]
        public void SymptomMatrix_SoftensMatchingNegativeDeltas()
        {
            var run = RunState.CreateNew(CharacterId.CharacterA, _config);
            var modifiers = new ModifierRegistry();
            float closenessAtMax = modifiers.ScaleNegativeDelta(run, _config, CoreStat.Closeness, -10f);
            float wholenessAtMax = modifiers.ScaleNegativeDelta(run, _config, CoreStat.Wholeness, -10f);
            float stabilityAtMax = modifiers.ScaleNegativeDelta(run, _config, CoreStat.Stability, -10f);

            run.SymptomSeverity[BpdSymptom.FearOfAbandonment] = 0;
            run.SymptomSeverity[BpdSymptom.UnstableSelfImage] = 0;
            run.SymptomSeverity[BpdSymptom.AffectiveInstability] = 0;

            Assert.Greater(modifiers.ScaleNegativeDelta(run, _config, CoreStat.Closeness, -10f), closenessAtMax);
            Assert.Greater(modifiers.ScaleNegativeDelta(run, _config, CoreStat.Wholeness, -10f), wholenessAtMax);
            Assert.Greater(modifiers.ScaleNegativeDelta(run, _config, CoreStat.Stability, -10f), stabilityAtMax);
        }

        [Test]
        public void EmptinessReduction_AddsSkillWholenessBonus()
        {
            var run = RunState.CreateNew(CharacterId.CharacterA, _config);
            var modifiers = new ModifierRegistry();
            Assert.AreEqual(0f, modifiers.SkillWholenessBonus(run, _config), 0.01f);
            run.SymptomSeverity[BpdSymptom.ChronicEmptiness] = 0;
            Assert.Greater(modifiers.SkillWholenessBonus(run, _config), 0f);
        }

        [Test]
        public void Level_LoseWhenStatHitsFloor()
        {
            var run = RunState.CreateNew(CharacterId.CharacterC, _config);
            var level = new LevelController(_config, new DefaultCardEffect(new ModifierRegistry()));
            var deck = StubDeckFactory.CreateDefaultDeck(1, forceLoseAtEnd: true);
            level.Begin(deck, run);
            level.ChooseLeft(run);

            Assert.AreEqual(LevelOutcome.Lost, level.Outcome);
            Assert.IsNotNull(level.FailStat);
        }

        [Test]
        public void Level_WinsWhenXpThresholdReached()
        {
            var run = RunState.CreateNew(CharacterId.CharacterA, _config);
            var level = new LevelController(_config, new DefaultCardEffect(new ModifierRegistry()));
            var deck = StubDeckFactory.CreateDefaultDeck(5, forceLoseAtEnd: false);
            level.Begin(deck, run);

            int choices = 0;
            while (level.Outcome == LevelOutcome.InProgress && choices < 10)
            {
                level.ChooseRight(run);
                choices++;
            }

            Assert.AreEqual(LevelOutcome.Won, level.Outcome);
            Assert.GreaterOrEqual(run.LevelXp, _config.XpToCompleteLevel);
            Assert.Less(choices, 5);
        }

        [Test]
        public void SplitSurvive_UnlocksMappedSkillWithCharges()
        {
            var run = RunState.CreateNew(CharacterId.CharacterA, _config);
            var progression = new ProgressionService(_config);
            var skill = SplitSkillRewards.SkillForFailStat(CoreStat.Safety);

            progression.UnlockSkill(run, skill);
            run.RecordSplitSurvive(CoreStat.Safety);

            Assert.Contains(skill, run.UnlockedSkills);
            Assert.AreEqual(DbtSkillId.Stop, skill);
            Assert.Greater(progression.GetCharges(run, skill), 0);
            Assert.AreEqual(1, run.GetSplitSurviveCount(CoreStat.Safety));
        }

        [Test]
        public void GuidedSplit_RequiresAllStepsToSurvive()
        {
            var split = new SafetySplitMinigame();
            bool? result = null;
            split.Begin(success => result = success);

            Assert.AreEqual(3, split.TotalSteps);
            split.SubmitSuccess();
            Assert.IsNull(result);
            Assert.AreEqual(1, split.CurrentStepIndex);
            split.SubmitSuccess();
            Assert.IsNull(result);
            split.SubmitSuccess();
            Assert.IsTrue(result.HasValue && result.Value);
        }

        [Test]
        public void GuidedSplit_FailOnAnyStepCollapses()
        {
            var split = new ClosenessSplitMinigame();
            bool? result = null;
            split.Begin(success => result = success);
            split.SubmitSuccess();
            split.SubmitFailure();
            Assert.IsTrue(result.HasValue && !result.Value);
        }

        [Test]
        public void EndingResolver_UsesDominantSplitFantasy()
        {
            var run = RunState.CreateNew(CharacterId.CharacterB, _config);
            run.RecordSplitSurvive(CoreStat.Closeness);
            run.RecordSplitSurvive(CoreStat.Closeness);
            run.RecordSplitSurvive(CoreStat.Safety);

            var fantasy = EndingResolver.ResolveDominantFantasy(run, _config);
            Assert.AreEqual(CoreStat.Closeness, fantasy);
            Assert.AreEqual("ending.closeness.title", EndingResolver.TitleKey(fantasy));
        }

        [Test]
        public void EndingResolver_WeightsTherapyAndCharacter()
        {
            var run = RunState.CreateNew(CharacterId.CharacterC, _config);
            run.UnlockedNeeds.Add("therapy_stub_need_01");
            run.UnlockedNeeds.Add("therapy_stub_need_02");

            var fantasy = EndingResolver.ResolveDominantFantasy(run, _config);
            Assert.AreEqual(CoreStat.Closeness, fantasy);
        }

        [Test]
        public void Level_ResumeAfterSplit_KeepsCrisisCard()
        {
            var run = RunState.CreateNew(CharacterId.CharacterA, _config);
            var level = new LevelController(_config, new DefaultCardEffect(new ModifierRegistry()));
            var deck = StubDeckFactory.CreateDefaultDeck(3, forceLoseAtEnd: false);
            deck[0] = StubDeckFactory.CreateStubCard(99, forceLose: true);
            level.Begin(deck, run);
            string idBefore = level.CurrentCard.Id;
            level.ChooseLeft(run);

            Assert.AreEqual(LevelOutcome.Lost, level.Outcome);
            Assert.AreEqual(0, level.CrisisCardIndex);

            level.ResumeAfterSplit();

            Assert.AreEqual(LevelOutcome.InProgress, level.Outcome);
            Assert.AreEqual(idBefore, level.CurrentCard.Id);
        }

        [Test]
        public void DeckLoader_UsesActPathForLevelIndex()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var deck = DeckLoader.LoadDeckForLevel(0, 3, config);
            Assert.AreEqual(3, deck.Count);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void DeckLoader_AppliesLevelBandOffset()
        {
            Assert.AreEqual(0, _config.ResolveLevelBandOffset(0));
            Assert.AreEqual(1, _config.ResolveLevelBandOffset(1));
            Assert.AreEqual(0, _config.ResolveActIndex(0));
            Assert.AreEqual(1, _config.ResolveActIndex(_config.LevelsPerAct));
        }

        [Test]
        public void CardGate_FiltersByHiddenVariable()
        {
            var run = RunState.CreateNew(CharacterId.CharacterA, _config);
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            card.InclusionGate = new CardGate { HiddenKey = "flag", HiddenMinValue = 1 };
            Assert.IsFalse(card.PassesGate(run));
            run.HiddenVariables["flag"] = 1;
            Assert.IsTrue(card.PassesGate(run));
            UnityEngine.Object.DestroyImmediate(card);
        }

        [Test]
        public void CardDefinition_BuildsChoiceWithOps()
        {
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            card.LeftActionName = "Answer immediately";
            card.LeftEffects = new StatEffects { Closeness = 5f, Stability = -3f };
            card.LeftHiddenOps = new[] { new VariableOp { Key = "rush", Delta = 1 } };
            card.AllowSkill = true;
            card.SkillActionName = "Wise Mind pause";
            card.SkillEffects = new StatEffects { Stability = 6f };

            Assert.AreEqual("Answer immediately", card.Left.ActionName);
            Assert.AreEqual(2, card.Left.StatDeltas.Length);
            Assert.AreEqual(1, card.Left.HiddenVariableOps.Length);
            Assert.IsTrue(card.SkillChoice.IsSkillChoice);
            Assert.AreEqual(6f, card.SkillChoice.StatDeltas[0].Amount, 0.01f);

            UnityEngine.Object.DestroyImmediate(card);
        }

        [Test]
        public void DbtCatalog_CoversEveryNonNoneSkill()
        {
            foreach (DbtSkillId id in Enum.GetValues(typeof(DbtSkillId)))
            {
                if (id == DbtSkillId.None)
                {
                    continue;
                }

                var def = DbtSkillCatalog.Get(id);
                Assert.IsNotNull(def, id.ToString());
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.DisplayName), id.ToString());
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.Description), id.ToString());
                Assert.Greater(def.ChargesOnUnlock, 0, id.ToString());
            }
        }

        [Test]
        public void GameConfig_TargetsFullRunScale()
        {
            Assert.AreEqual(45, _config.LevelsBeforeEnding);
            Assert.AreEqual(15, _config.LevelsPerAct);
        }

        [Test]
        public void MemoryPenalty_ReducesStabilityAtLevelStart()
        {
            var run = RunState.CreateNew(CharacterId.CharacterA, _config);
            run.TraumaticMemoriesUnlocked = 1;
            float before = run.GetStat(CoreStat.Stability);
            run.PrepareLevelStart(_config);
            Assert.AreEqual(before * 0.75f, run.GetStat(CoreStat.Stability), 0.01f);
        }

        [Test]
        public void LevelRestart_RestoresSnapshot()
        {
            var run = RunState.CreateNew(CharacterId.CharacterA, _config);
            run.PrepareLevelStart(_config);
            run.SetStat(CoreStat.Safety, 5f, _config);
            run.LevelXp = 16;
            run.RestoreLevelStartSnapshot(_config);

            Assert.AreEqual(_config.StartingStatValue, run.GetStat(CoreStat.Safety), 0.01f);
            Assert.AreEqual(0, run.LevelXp);
        }

        [Test]
        public void SymptomCatalog_HasKeysForAllNine()
        {
            foreach (BpdSymptom symptom in Enum.GetValues(typeof(BpdSymptom)))
            {
                Assert.IsFalse(string.IsNullOrEmpty(SymptomCatalog.DisplayNameKey(symptom)));
                Assert.IsFalse(Loc.Get(SymptomCatalog.DisplayNameKey(symptom)).StartsWith("["));
            }
        }
    }
}
