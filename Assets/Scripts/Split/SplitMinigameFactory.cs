using System.Collections.Generic;
using BPD.Stats;

namespace BPD.Split
{
    public static class SplitMinigameFactory
    {
        static readonly Dictionary<CoreStat, ISplitMinigame> Map = new Dictionary<CoreStat, ISplitMinigame>
        {
            { CoreStat.Closeness, new ClosenessSplitMinigame() },
            { CoreStat.Safety, new SafetySplitMinigame() },
            { CoreStat.Stability, new StabilitySplitMinigame() },
            { CoreStat.Wholeness, new WholenessSplitMinigame() }
        };

        public static ISplitMinigame Create(CoreStat failStat)
        {
            return Map.TryGetValue(failStat, out var game) ? game : Map[CoreStat.Safety];
        }
    }
}
