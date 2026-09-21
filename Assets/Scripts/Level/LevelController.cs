using System;
using System.Collections.Generic;
using BPD.Core;
using BPD.Run;
using BPD.Stats;
using UnityEngine;

namespace BPD.Level
{
    public enum LevelOutcome
    {
        InProgress,
        Won,
        Lost
    }

    public sealed class LevelController
    {
        readonly GameConfig _config;
        readonly ICardEffect _effect;
        readonly List<CardDefinition> _deck = new List<CardDefinition>();
        int _cardIndex;

        public CardDefinition CurrentCard { get; private set; }
        public LevelOutcome Outcome { get; private set; } = LevelOutcome.InProgress;
        public CoreStat? FailStat { get; private set; }
        public int CardsRemaining => Math.Max(0, _deck.Count - _cardIndex);

        /// <summary>Index of the card that triggered Split (unchanged while Lost).</summary>
        public int CrisisCardIndex => _cardIndex;

        public LevelController(GameConfig config, ICardEffect effect)
        {
            _config = config;
            _effect = effect;
        }

        public void Begin(IEnumerable<CardDefinition> cards, RunState run)
        {
            _deck.Clear();
            _deck.AddRange(cards);
            _cardIndex = 0;
            Outcome = LevelOutcome.InProgress;
            FailStat = null;
            run.PrepareLevelStart(_config);
            AdvanceCard();
        }

        public void ChooseLeft(RunState run) => ApplyChoice(run, CurrentCard?.Left);
        public void ChooseRight(RunState run) => ApplyChoice(run, CurrentCard?.Right);
        public void ChooseSkill(RunState run) => ApplyChoice(run, CurrentCard?.SkillChoice);

        /// <summary>After Split survive: same deck, same card (routine may repeat).</summary>
        public void ResumeAfterSplit()
        {
            Outcome = LevelOutcome.InProgress;
            FailStat = null;
            AdvanceCard();
        }

        void ApplyChoice(RunState run, CardChoice? choiceNullable)
        {
            if (Outcome != LevelOutcome.InProgress || CurrentCard == null || choiceNullable == null)
            {
                return;
            }

            var choice = choiceNullable.Value;
            _effect.Apply(run, choice, _config);

            int gained = _config.XpPerCardChoice;
            run.LevelXp += gained;
            run.Xp += gained;
            run.UnspentXp += gained;

            bool failedByStats = TryDetectFail(run, out var failStat);
            if (choice.TriggersLoseIfApplied || failedByStats)
            {
                Outcome = LevelOutcome.Lost;
                FailStat = failStat ?? ResolveLowestStat(run);
                CurrentCard = null;
                return;
            }

            _cardIndex++;
            bool deckEmpty = _cardIndex >= _deck.Count;
            bool xpComplete = run.LevelXp >= _config.XpToCompleteLevel;
            if (deckEmpty || xpComplete)
            {
                Outcome = LevelOutcome.Won;
                CurrentCard = null;
                return;
            }

            AdvanceCard();
        }

        void AdvanceCard()
        {
            CurrentCard = _cardIndex < _deck.Count ? _deck[_cardIndex] : null;
        }

        bool TryDetectFail(RunState run, out CoreStat? failStat)
        {
            failStat = null;
            foreach (CoreStat stat in Enum.GetValues(typeof(CoreStat)))
            {
                if (run.GetStat(stat) <= _config.FailThreshold)
                {
                    failStat = ResolveLowestStat(run);
                    return true;
                }
            }

            return false;
        }

        public CoreStat ResolveLowestStat(RunState run)
        {
            float lowest = float.MaxValue;
            CoreStat chosen = _config.SplitTieBreakOrder[0];

            foreach (var candidate in _config.SplitTieBreakOrder)
            {
                float value = run.GetStat(candidate);
                if (value < lowest)
                {
                    lowest = value;
                    chosen = candidate;
                }
            }

            foreach (CoreStat stat in Enum.GetValues(typeof(CoreStat)))
            {
                float value = run.GetStat(stat);
                if (value < lowest)
                {
                    lowest = value;
                    chosen = stat;
                }
                else if (Mathf.Approximately(value, lowest))
                {
                    int currentIndex = Array.IndexOf(_config.SplitTieBreakOrder, chosen);
                    int newIndex = Array.IndexOf(_config.SplitTieBreakOrder, stat);
                    if (newIndex >= 0 && (currentIndex < 0 || newIndex < currentIndex))
                    {
                        chosen = stat;
                    }
                }
            }

            return chosen;
        }
    }
}
