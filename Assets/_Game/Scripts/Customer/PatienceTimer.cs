using GemCafe.Core;
using GemCafe.UI;
using UnityEngine;

namespace GemCafe.Customer
{
    public class PatienceTimer : MonoBehaviour
    {
        [SerializeField] private float fallbackPatience = 60f;

        private float _current;
        private float _max;

        public bool IsRunning { get; private set; }
        public float Ratio01 => _max > 0f ? Mathf.Clamp01(_current / _max) : 0f;

        public void Begin(float maxSeconds)
        {
            _max = maxSeconds > 0f ? maxSeconds : fallbackPatience;
            _current = _max;
            IsRunning = true;
            EventBus.RaisePatienceChanged(1f);
        }

        public void Pause()
        {
            IsRunning = false;
        }

        public void Resume()
        {
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
            _current = 0f;
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            var gm = GameManager.Instance;
            if (gm == null)
            {
                return;
            }

            var sub = gm.StateMachine.ServiceSub;
            if (sub != ServiceSubState.OrderDialogue && sub != ServiceSubState.Crafting)
            {
                return;
            }

            if (PopupManager.Instance != null && PopupManager.Instance.IsOpen(PopupType.Settings))
            {
                return;
            }

            _current -= Time.deltaTime;
            EventBus.RaisePatienceChanged(Ratio01);

            if (_current <= 0f)
            {
                _current = 0f;
                IsRunning = false;
                EventBus.RaisePatienceDepleted();
            }
        }
    }
}
