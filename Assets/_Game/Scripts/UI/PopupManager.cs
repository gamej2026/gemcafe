using UnityEngine;

namespace GemCafe.UI
{
    public class PopupManager : MonoBehaviour
    {
        [SerializeField] private Popup[] popups;

        public static PopupManager Instance { get; private set; }

        public bool IsAnyOpen
        {
            get
            {
                if (popups == null)
                {
                    return false;
                }

                for (int i = 0; i < popups.Length; i++)
                {
                    var popup = popups[i];
                    if (popup != null && popup.IsOpen)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        public bool IsOpen(PopupType type)
        {
            var popup = FindPopup(type);
            return popup != null && popup.IsOpen;
        }

        public bool Open(PopupType type)
        {
            var popup = FindPopup(type);
            if (popup == null || popup.IsOpen)
            {
                return false;
            }

            popup.Open();
            return true;
        }

        public void Close(PopupType type)
        {
            var popup = FindPopup(type);
            if (popup != null)
            {
                popup.Close();
            }
        }

        public void CloseAll()
        {
            if (popups == null)
            {
                return;
            }

            for (int i = 0; i < popups.Length; i++)
            {
                if (popups[i] != null)
                {
                    popups[i].Close();
                }
            }
        }

        private Popup FindPopup(PopupType type)
        {
            if (popups == null)
            {
                return null;
            }

            for (int i = 0; i < popups.Length; i++)
            {
                var popup = popups[i];
                if (popup != null && popup.Type == type)
                {
                    return popup;
                }
            }

            return null;
        }
    }
}
