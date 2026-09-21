using BPD.Core;
using BPD.Run;
using UnityEngine;

namespace BPD.Core
{
    /// <summary>
    /// Session singleton holding the active run. Lives across screens via DontDestroyOnLoad.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public GameConfig Config { get; private set; }
        public RunState Run { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Config = GameConfig.LoadOrCreateRuntimeDefault();
        }

        public void StartRun(CharacterId character)
        {
            Run = RunState.CreateNew(character, Config);
            Run.PrepareLevelStart(Config);
        }

        public void EnsureRun(CharacterId fallback = CharacterId.CharacterA)
        {
            if (Run == null)
            {
                StartRun(fallback);
            }
        }
    }
}
