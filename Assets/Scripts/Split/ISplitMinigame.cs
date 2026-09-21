using System;
using BPD.Stats;

namespace BPD.Split
{
    public interface ISplitMinigame
    {
        CoreStat AssociatedStat { get; }
        string TitleKey { get; }
        string PromptKey { get; }
        int CurrentStepIndex { get; }
        int TotalSteps { get; }
        string CurrentStepPromptKey { get; }
        string SkillActionKey { get; }
        string FailActionKey { get; }
        string CrisisResourceKey { get; }
        void Begin(Action<bool> onComplete);
        void Tick(float deltaTime);
        void SubmitSuccess();
        void SubmitFailure();
        void Cancel();
    }
}
