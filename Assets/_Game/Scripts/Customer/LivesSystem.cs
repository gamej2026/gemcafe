using System;
using GemCafe.Core;
using UnityEngine;

namespace GemCafe.Customer
{
    public class LivesSystem
    {
        public int Current { get; private set; }

        public event Action OnDeath;

        public LivesSystem(int starting)
        {
            Current = starting;
        }

        public void Lose(int amount = 1)
        {
            Current = Mathf.Max(0, Current - amount);
            EventBus.RaiseLivesChanged(Current);

            if (Current <= 0)
            {
                OnDeath?.Invoke();
            }
        }

        public void Reset(int starting)
        {
            Current = starting;
            EventBus.RaiseLivesChanged(Current);
        }

        public bool IsDead => Current <= 0;
    }
}