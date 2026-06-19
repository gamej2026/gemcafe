using System.Collections;
using System.Collections.Generic;
using GemCafe.Core;
using GemCafe.Crafting;
using GemCafe.Data;
using GemCafe.Dialogue;
using GemCafe.UI;
using UnityEngine;

namespace GemCafe.Customer
{
    public class DayManager : MonoBehaviour
    {
        [SerializeField] private CustomerSpawner spawner;
        [SerializeField] private DialogueRunner dialogue;
        [SerializeField] private CraftingController crafting;
        [SerializeField] private PatienceTimer patience;
        [SerializeField] private ResultToast resultToast;
        [SerializeField] private ScreenTransition craftTransition;
        [SerializeField] private List<CustomerSO> allCustomers;
        [SerializeField] private int fareReward = 10;
        [SerializeField] private bool forceServiceStateOnStart;

        private Queue<CustomerSO> _queue = new Queue<CustomerSO>();
        private CustomerSO _currentCustomer;
        private bool _resolved;

        public int CurrentDay { get; private set; } = 1;
        public int Fare { get; private set; }

        public int RemainingInQueue => _queue != null ? _queue.Count : 0;
        public CustomerSO CurrentCustomer => _currentCustomer;
        public bool IsResolved => _resolved;
        public int FareReward => fareReward;

        private void OnEnable()
        {
            EventBus.OnDrinkCompleted += HandleDrinkCompleted;
            EventBus.OnPatienceDepleted += HandlePatienceDepleted;
        }

        private void OnDisable()
        {
            EventBus.OnDrinkCompleted -= HandleDrinkCompleted;
            EventBus.OnPatienceDepleted -= HandlePatienceDepleted;
        }

        private void Start()
        {
            var gm = GameManager.Instance;
            if (forceServiceStateOnStart && gm != null && gm.StateMachine.Current != GameState.ServiceLoop)
            {
                gm.StateMachine.Restore(GameState.ServiceLoop);
                StartService(1, 0);
                return;
            }

            if (gm != null && gm.StateMachine.Current == GameState.ServiceLoop)
            {
                StartService(gm.ContinueStartDay, gm.ContinueStartFare);
                return;
            }

            StartService();
        }

        public void StartService()
        {
            StartService(1, 0);
        }

        public void StartService(int startDay, int startFare)
        {
            var totalDays = GameManager.Instance != null && GameManager.Instance.Config != null
                ? GameManager.Instance.Config.totalDays
                : 3;

            CurrentDay = Mathf.Clamp(startDay, 1, totalDays);
            Fare = Mathf.Max(0, startFare);
            BuildQueueForDay(CurrentDay);
            SaveProgress();

            var gm = GameManager.Instance;
            if (gm != null && gm.StateMachine.Current != GameState.ServiceLoop)
            {
                gm.StateMachine.TryTransition(GameState.ServiceLoop);
            }

            NextCustomer();
        }

        private void SaveProgress()
        {
            var data = new SaveData
            {
                day = CurrentDay,
                fare = Fare,
                lives = GameManager.Instance != null ? GameManager.Instance.Lives.Current : 3
            };

            SaveSystem.Save(data);
        }

        private void BuildQueueForDay(int day)
        {
            _queue = new Queue<CustomerSO>();

            if (allCustomers == null)
            {
                return;
            }

            for (int i = 0; i < allCustomers.Count; i++)
            {
                var customer = allCustomers[i];
                if (customer != null && customer.day == day)
                {
                    _queue.Enqueue(customer);
                }
            }
        }

        private void NextCustomer()
        {
            if (_queue == null || _queue.Count == 0)
            {
                EndDay();
                return;
            }

            _currentCustomer = _queue.Dequeue();
            _resolved = false;
            SetServiceSub(ServiceSubState.CustomerEnter);

            if (spawner == null)
            {
                OnCustomerArrived();
                return;
            }

            spawner.Spawn(_currentCustomer, OnCustomerArrived);
        }

        private void OnCustomerArrived()
        {
            if (_currentCustomer == null)
            {
                NextCustomer();
                return;
            }

            SetServiceSub(ServiceSubState.OrderDialogue);

            if (dialogue == null)
            {
                OnOrderDialogueDone();
                return;
            }

            dialogue.Play(_currentCustomer.orderDialogue, OnOrderDialogueDone);
        }

        private void OnOrderDialogueDone()
        {
            if (_currentCustomer == null)
            {
                NextCustomer();
                return;
            }

            SetServiceSub(ServiceSubState.Crafting);

            if (patience != null)
            {
                patience.Begin(_currentCustomer.patience);
            }

            if (craftTransition != null)
            {
                craftTransition.SlideIn(() => BeginCraftForCurrentCustomer());
                return;
            }

            BeginCraftForCurrentCustomer();
        }

        private void BeginCraftForCurrentCustomer()
        {
            if (crafting == null || _currentCustomer == null)
            {
                return;
            }

            crafting.BeginCraft(_currentCustomer.targetRecipe);
        }

        private void HandleDrinkCompleted(RecipeSO result)
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;

            if (patience != null)
            {
                patience.Stop();
            }

            SetServiceSub(ServiceSubState.Result);

            bool success = result != null;
            if (success)
            {
                Fare += fareReward;
            }
            else
            {
                GameManager.Instance?.Lives.Lose(1);
            }

            ResolveAfterResult(success);
        }

        private void HandlePatienceDepleted()
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;
            SetServiceSub(ServiceSubState.Result);
            GameManager.Instance?.Lives.Lose(1);
            ResolveAfterResult(false);
        }

        private void ResolveAfterResult(bool success)
        {
            if (crafting != null)
            {
                crafting.EndCraft();
            }

            if (resultToast != null)
            {
                resultToast.ShowResult(success, AfterToast);
                return;
            }

            AfterToast();
        }

        private void AfterToast()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.Lives.IsDead)
            {
                gm.GameOver();
                return;
            }

            if (spawner != null)
            {
                spawner.Clear();
            }

            craftTransition?.SlideOut();
            NextCustomer();
        }

        private void EndDay()
        {
            var gm = GameManager.Instance;
            gm?.StateMachine.TryTransition(GameState.DayEnd);
            EventBus.RaiseDayCompleted(CurrentDay);

            var total = gm != null && gm.Config != null ? gm.Config.totalDays : 3;
            if (CurrentDay >= total)
            {
                gm?.StateMachine.TryTransition(GameState.Ending);
            }
            else
            {
                CurrentDay++;
                BuildQueueForDay(CurrentDay);
                SaveProgress();
                gm?.StateMachine.TryTransition(GameState.ServiceLoop);
                NextCustomer();
            }
        }

        private void SetServiceSub(ServiceSubState s)
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                return;
            }

            gm.StateMachine.SetServiceSub(s);
        }
    }
}
