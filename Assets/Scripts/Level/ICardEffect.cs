using BPD.Level;
using BPD.Run;

namespace BPD.Level
{
    public interface ICardEffect
    {
        void Apply(RunState run, CardChoice choice, Core.GameConfig config);
    }
}
