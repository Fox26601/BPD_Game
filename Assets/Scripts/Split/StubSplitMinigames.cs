using System;
using BPD.Stats;

namespace BPD.Split
{
    /// <summary>
    /// Multi-step DBT exercise: player must pick the skillful action on every step to survive.
    /// </summary>
    public abstract class GuidedSplitMinigame : ISplitMinigame
    {
        Action<bool> _onComplete;
        bool _active;
        int _step;

        protected abstract string[] StepPromptKeys { get; }

        public abstract CoreStat AssociatedStat { get; }
        public abstract string TitleKey { get; }
        public abstract string PromptKey { get; }
        public virtual string SkillActionKey => "ui.split.btn_skill";
        public virtual string FailActionKey => "ui.split.btn_fail";
        public virtual string CrisisResourceKey => null;

        public int CurrentStepIndex => _step;
        public int TotalSteps => StepPromptKeys.Length;
        public string CurrentStepPromptKey =>
            _step >= 0 && _step < StepPromptKeys.Length ? StepPromptKeys[_step] : PromptKey;

        public bool IsActive => _active;

        public void Begin(Action<bool> onComplete)
        {
            _onComplete = onComplete;
            _active = true;
            _step = 0;
        }

        public void Tick(float deltaTime)
        {
            // Guided splits are event-driven.
        }

        public void SubmitSuccess()
        {
            if (!_active)
            {
                return;
            }

            _step++;
            if (_step >= TotalSteps)
            {
                Complete(true);
            }
        }

        public void SubmitFailure() => Complete(false);

        public void Cancel()
        {
            _active = false;
            _onComplete = null;
        }

        void Complete(bool success)
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke(success);
        }
    }

    public sealed class SafetySplitMinigame : GuidedSplitMinigame
    {
        static readonly string[] Steps =
        {
            "split.safety.step1",
            "split.safety.step2",
            "split.safety.step3"
        };

        protected override string[] StepPromptKeys => Steps;
        public override CoreStat AssociatedStat => CoreStat.Safety;
        public override string TitleKey => "split.safety.title";
        public override string PromptKey => "split.safety.prompt";
        public override string SkillActionKey => "ui.split.btn.stop";
        public override string CrisisResourceKey => "ui.crisis.resources";
    }

    public sealed class StabilitySplitMinigame : GuidedSplitMinigame
    {
        static readonly string[] Steps =
        {
            "split.stability.step1",
            "split.stability.step2",
            "split.stability.step3"
        };

        protected override string[] StepPromptKeys => Steps;
        public override CoreStat AssociatedStat => CoreStat.Stability;
        public override string TitleKey => "split.stability.title";
        public override string PromptKey => "split.stability.prompt";
        public override string SkillActionKey => "ui.split.btn.facts";
    }

    public sealed class WholenessSplitMinigame : GuidedSplitMinigame
    {
        static readonly string[] Steps =
        {
            "split.wholeness.step1",
            "split.wholeness.step2",
            "split.wholeness.step3"
        };

        protected override string[] StepPromptKeys => Steps;
        public override CoreStat AssociatedStat => CoreStat.Wholeness;
        public override string TitleKey => "split.wholeness.title";
        public override string PromptKey => "split.wholeness.prompt";
        public override string SkillActionKey => "ui.split.btn.wise";
    }

    public sealed class ClosenessSplitMinigame : GuidedSplitMinigame
    {
        static readonly string[] Steps =
        {
            "split.closeness.step1",
            "split.closeness.step2",
            "split.closeness.step3"
        };

        protected override string[] StepPromptKeys => Steps;
        public override CoreStat AssociatedStat => CoreStat.Closeness;
        public override string TitleKey => "split.closeness.title";
        public override string PromptKey => "split.closeness.prompt";
        public override string SkillActionKey => "ui.split.btn.dearman";
    }
}
