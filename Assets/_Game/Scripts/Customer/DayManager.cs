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
        [SerializeField] private DrinkPopup drinkPopup;
        [SerializeField] private ServeSequence serveSequence;
        [SerializeField] private CoinGainScreen coinGainScreen;
        [SerializeField] private EndingCoinSummary endingCoinSummary;
        [SerializeField] private DayIntro dayIntro;
        [SerializeField] private List<CustomerSO> allCustomers;
        [SerializeField] private int fareReward = 10;
        [SerializeField] private bool forceServiceStateOnStart;

        private Queue<CustomerSO> _queue = new Queue<CustomerSO>();
        private CustomerSO _currentCustomer;
        private bool _resolved;
        private DrinkResult _lastResult;
        private readonly List<CoinType> _coins = new List<CoinType>();
        private int _lastIntroDay = -1;

        private const int MaxCoinSlots = 3;

        public int CurrentDay { get; private set; } = 1;
        public int Fare { get; private set; }
        public int TotalCoins { get; private set; }
        public int GreatCoins { get; private set; }

        public int RemainingInQueue => _queue != null ? _queue.Count : 0;
        public CustomerSO CurrentCustomer => _currentCustomer;
        public bool IsResolved => _resolved;
        public int FareReward => fareReward;

        private void OnEnable()
        {
            EventBus.OnDrinkResult += HandleDrinkResult;
        }

        private void OnDisable()
        {
            EventBus.OnDrinkResult -= HandleDrinkResult;
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
                StartService(gm.ContinueStartDay, gm.ContinueStartFare, gm.ContinueStartTotalCoins, gm.ContinueStartGreatCoins);
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
            StartService(startDay, startFare, 0, 0);
        }

        public void StartService(int startDay, int startFare, int startTotalCoins, int startGreatCoins)
        {
            var totalDays = GameManager.Instance != null && GameManager.Instance.Config != null
                ? GameManager.Instance.Config.totalDays
                : 3;

            CurrentDay = Mathf.Clamp(startDay, 1, totalDays);
            Fare = Mathf.Max(0, startFare);
            TotalCoins = Mathf.Max(0, startTotalCoins);
            GreatCoins = Mathf.Max(0, startGreatCoins);
            _lastIntroDay = -1;
            RebuildCoinSlots();
            BuildQueueForDay(CurrentDay);
            SaveProgress();

            var gm = GameManager.Instance;
            if (gm != null && gm.StateMachine.Current != GameState.ServiceLoop)
            {
                gm.StateMachine.TryTransition(GameState.ServiceLoop);
            }

            EventBus.RaiseCoinsChanged(TotalCoins);
            EventBus.RaiseCoinSlotsChanged(_coins);
            NextCustomer();
        }

        private void RebuildCoinSlots()
        {
            _coins.Clear();
            var normals = Mathf.Max(0, TotalCoins - GreatCoins);
            var golds = Mathf.Max(0, GreatCoins);
            for (int i = 0; i < normals && _coins.Count < MaxCoinSlots; i++)
            {
                _coins.Add(CoinType.Normal);
            }
            for (int i = 0; i < golds && _coins.Count < MaxCoinSlots; i++)
            {
                _coins.Add(CoinType.Gold);
            }
        }

        private void AddCoinSlot(CoinType type)
        {
            if (_coins.Count < MaxCoinSlots)
            {
                _coins.Add(type);
            }
        }

        private void SaveProgress()
        {
            var data = new SaveData
            {
                day = CurrentDay,
                fare = Fare,
                lives = GameManager.Instance != null ? GameManager.Instance.Lives.Current : 3,
                totalCoins = TotalCoins,
                greatCoins = GreatCoins
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

            if (dayIntro != null && CurrentDay != _lastIntroDay)
            {
                _lastIntroDay = CurrentDay;
                dayIntro.Show(CurrentDay, SpawnCurrentCustomer);
                return;
            }

            SpawnCurrentCustomer();
        }

        private void SpawnCurrentCustomer()
        {
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
            ResolveAfterResult(success ? DrinkResult.Success : DrinkResult.Fail);
        }

        private void HandleDrinkResult(DrinkResult result)
        {
            if (_resolved)
            {
                return;
            }

            _resolved = true;
            SetServiceSub(ServiceSubState.Result);

            if (result == DrinkResult.GreatSuccess)
            {
                TotalCoins++;
                GreatCoins++;
                AddCoinSlot(CoinType.Gold);
            }
            else if (result == DrinkResult.Success)
            {
                TotalCoins++;
                AddCoinSlot(CoinType.Normal);
            }

            _lastResult = result;
            EventBus.RaiseCoinsChanged(TotalCoins);
            EventBus.RaiseCoinSlotsChanged(_coins);
            ResolveAfterResult(result);
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
            ResolveAfterResult(DrinkResult.Fail);
        }

        private void ResolveAfterResult(DrinkResult result)
        {
            if (crafting != null)
            {
                crafting.EndCraft();
            }

            ShowResultReaction(result);
        }

        private void ShowResultReaction(DrinkResult result)
        {
            if (resultToast != null)
            {
                resultToast.ShowResult(result, _currentCustomer, () => ShowCoinGain(result));
                return;
            }

            ShowCoinGain(result);
        }

        private void ShowCoinGain(DrinkResult result)
        {
            if (coinGainScreen != null)
            {
                coinGainScreen.Show(result, AfterToast);
                return;
            }

            AfterToast();
        }

        private void AfterToast()
        {
            if (spawner != null)
            {
                spawner.Clear();
            }

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
                var kind = ResolveEndingKind();
                gm?.SetEndingResult(kind, TotalCoins, GreatCoins);

                if (endingCoinSummary != null)
                {
                    endingCoinSummary.Show(TotalCoins, GreatCoins, () => gm?.StateMachine.TryTransition(GameState.Ending));
                }
                else
                {
                    gm?.StateMachine.TryTransition(GameState.Ending);
                }
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

        private EndingKind ResolveEndingKind()
        {
            if (GreatCoins == 3)
            {
                return EndingKind.A;
            }

            if (TotalCoins == 3)
            {
                return EndingKind.B;
            }

            // 코인이 3개 미만(0~2)이면 뱃삯을 못 벌어 배드엔딩으로 분기.
            return EndingKind.C;
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
