using System.Collections.Generic;
using UnityEngine;

namespace GemCafe.Core
{
    public class GameStateMachine
    {
        private static readonly Dictionary<GameState, GameState[]> _allowed = new Dictionary<GameState, GameState[]>
        {
            { GameState.Lobby, new[] { GameState.IntroStage1 } },
            { GameState.IntroStage1, new[] { GameState.CafeIntro } },
            { GameState.CafeIntro, new[] { GameState.Tutorial, GameState.ServiceLoop } },
            { GameState.Tutorial, new[] { GameState.ServiceLoop } },
            { GameState.ServiceLoop, new[] { GameState.DayEnd, GameState.GameOver } },
            { GameState.DayEnd, new[] { GameState.ServiceLoop, GameState.Ending } },
            { GameState.Ending, new[] { GameState.Lobby } },
            { GameState.GameOver, new[] { GameState.Lobby } }
        };

        public GameState Current { get; private set; } = GameState.Lobby;

        public ServiceSubState ServiceSub { get; private set; } = ServiceSubState.None;

        public bool CanTransition(GameState to)
        {
            if (!_allowed.TryGetValue(Current, out var nextStates) || nextStates == null)
            {
                return false;
            }

            for (int i = 0; i < nextStates.Length; i++)
            {
                if (nextStates[i] == to)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryTransition(GameState to)
        {
            if (!CanTransition(to))
            {
                Debug.LogWarning($"Invalid transition: {Current} -> {to}");
                return false;
            }

            var from = Current;
            Current = to;
            EventBus.RaiseStateChanged(from, to);
            return true;
        }

        public void Restore(GameState state)
        {
            var from = Current;
            Current = state;
            EventBus.RaiseStateChanged(from, state);
        }

        public void SetServiceSub(ServiceSubState sub)
        {
            ServiceSub = sub;
        }
    }
}