using BPD.Skills;
using UnityEngine;

namespace BPD.Level
{
    /// <summary>
    /// Builds PLACEHOLDER stub decks when SO content is missing.
    /// </summary>
    public static class StubDeckFactory
    {
        public static System.Collections.Generic.List<CardDefinition> CreateDefaultDeck(int cardCount, bool forceLoseAtEnd = false)
        {
            var cards = new System.Collections.Generic.List<CardDefinition>(cardCount);
            for (int i = 0; i < cardCount; i++)
            {
                bool isLast = i == cardCount - 1 && forceLoseAtEnd;
                cards.Add(CreateStubCard(i + 1, isLast));
            }

            return cards;
        }

        public static CardDefinition CreateForceLoseVariant(CardDefinition source)
        {
            var card = CreateStubCard(99, forceLose: true);
            if (source != null)
            {
                card.Id = source.Id + "_force_lose";
                card.SituationText = source.SituationText;
                card.InclusionGate = source.InclusionGate;
                card.RightActionName = source.RightActionName;
                card.RightEffects = source.RightEffects;
                card.RightRelationshipOps = source.RightRelationshipOps;
                card.RightHiddenOps = source.RightHiddenOps;
                card.AllowSkill = source.AllowSkill;
                card.RequiredSkill = source.RequiredSkill;
                card.SkillActionName = source.SkillActionName;
                card.SkillEffects = source.SkillEffects;
                card.SkillRelationshipOps = source.SkillRelationshipOps;
                card.SkillHiddenOps = source.SkillHiddenOps;
            }

            return card;
        }

        public static CardDefinition CreateStubCard(int index, bool forceLose = false)
        {
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            card.Id = $"card_stub_{index:000}";
            card.name = card.Id;
            ApplyStubContent(card, index, forceLose);
            return card;
        }

        public static void ApplyStubContent(CardDefinition card, int index, bool forceLose = false)
        {
            switch (index)
            {
                case 1:
                    card.SituationText = "PLACEHOLDER: A message lights up. Your chest tightens.";
                    card.LeftActionName = "Answer immediately";
                    card.RightActionName = "Wait and breathe";
                    card.SkillActionName = "Wise Mind pause";
                    card.RequiredSkill = DbtSkillId.WiseMind;
                    break;
                case 2:
                    card.SituationText = "PLACEHOLDER: Plans change without warning.";
                    card.LeftActionName = "Push closer";
                    card.RightActionName = "Withdraw";
                    card.SkillActionName = "Check the Facts";
                    card.RequiredSkill = DbtSkillId.CheckTheFacts;
                    break;
                case 3:
                    card.SituationText = "PLACEHOLDER: Emptiness arrives mid-afternoon.";
                    card.LeftActionName = "Reach for a rush";
                    card.RightActionName = "Name the feeling";
                    card.SkillActionName = "Opposite Action";
                    card.RequiredSkill = DbtSkillId.OppositeAction;
                    break;
                case 4:
                    card.SituationText = "PLACEHOLDER: Someone misunderstands you.";
                    card.LeftActionName = "Defend hard";
                    card.RightActionName = "Ask for clarity";
                    card.SkillActionName = "DEAR MAN";
                    card.RequiredSkill = DbtSkillId.DearMan;
                    break;
                case 5:
                    card.SituationText = "PLACEHOLDER: Night falls. The urge is loud.";
                    card.LeftActionName = "Act on the urge";
                    card.RightActionName = "Urge surf";
                    card.SkillActionName = "STOP";
                    card.RequiredSkill = DbtSkillId.Stop;
                    break;
                default:
                    card.SituationText = $"PLACEHOLDER: Stub event {index:000}.";
                    card.LeftActionName = "Left choice";
                    card.RightActionName = "Right choice";
                    card.SkillActionName = "DBT skill";
                    card.RequiredSkill = DbtSkillId.WiseMind;
                    break;
            }

            card.AllowSkill = true;
            card.LeftTriggersLose = forceLose;
            card.RightTriggersLose = false;

            if (forceLose)
            {
                card.LeftEffects = new StatEffects { Safety = -100f };
                card.LeftActionName = "Answer immediately";
            }
            else
            {
                card.LeftEffects = new StatEffects { Closeness = 5f, Stability = -3f };
            }

            card.RightEffects = new StatEffects { Wholeness = 4f, Safety = -4f };
            card.SkillEffects = new StatEffects { Stability = 6f, Safety = 2f };
        }
    }
}
