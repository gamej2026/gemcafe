using System.Collections;
using System.Collections.Generic;
using GemCafe.Core;
using GemCafe.Crafting;
using GemCafe.Data;
using GemCafe.Dialogue;
using GemCafe.Ending;
using GemCafe.Tutorial;
using GemCafe.UI;
using UnityEngine;

namespace GemCafe.Customer
{
    public class DayManager : MonoBehaviour
    {
        [SerializeField] private CustomerSpawner spawner;
        [SerializeField] private DialogueRunner dialogue;
        [SerializeField] private CraftingController crafting;
        [SerializeField] private ResultToast resultToast;
        [SerializeField] private ScreenTransition craftTransition;
        [SerializeField] private DrinkPopup drinkPopup;
        [SerializeField] private ServeSequence serveSequence;
        [SerializeField] private CoinGainScreen coinGainScreen;
        [SerializeField] private EndingCoinSummary endingCoinSummary;
        [SerializeField] private DayIntro dayIntro;
        [SerializeField] private CustomerCsvTable customerTable;
        [SerializeField] private List<CustomerSO> allCustomers;
        [SerializeField] private int fareReward = 10;
        [SerializeField] private bool forceServiceStateOnStart;
        [TextArea] [SerializeField] private string greatReactionLine = "만족스러워";
        [TextArea] [SerializeField] private string successReactionLine = "나쁘지 않아";
        [TextArea] [SerializeField] private string failReactionLine = "실망스럽군";

        private Queue<CustomerSO> _queue = new Queue<CustomerSO>();
        private CustomerSO _currentCustomer;
        private bool _resolved;
        private DrinkResult _lastResult;
        private readonly List<CoinType> _coins = new List<CoinType>();
        private int _lastIntroDay = -1;
        private bool _customersLoaded;

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
            // 튜토리얼 중에는 Cafe 가 Additive 배경으로만 떠 있으므로
            // 실제 서비스를 자동 시작하지 않는다(손님/저장 없음).
            if (TutorialContext.IsActive)
            {
                return;
            }

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

            EnsureCustomersFromTable();

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
            // 튜토리얼 진행 결과는 절대 저장하지 않는다(안전망).
            if (TutorialContext.IsActive)
            {
                return;
            }

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

        private void EnsureCustomersFromTable()
        {
            if (_customersLoaded || customerTable == null)
            {
                return;
            }

            _customersLoaded = true;
            var loaded = customerTable.Load();
            if (loaded != null && loaded.Count > 0)
            {
                allCustomers = loaded;
            }
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
            if (dialogue != null && _currentCustomer != null)
            {
                var line = BuildReactionLine(result);
                dialogue.Play(new[] { line }, FadeOutCustomerThenNext);
                return;
            }

            FadeOutCustomerThenNext();
        }

        private DialogueLine BuildReactionLine(DrinkResult result)
        {
            string speaker = "손님";
            Sprite portrait = null;

            if (_currentCustomer != null && _currentCustomer.orderDialogue != null && _currentCustomer.orderDialogue.Length > 0)
            {
                speaker = _currentCustomer.orderDialogue[0].speakerId;
                portrait = _currentCustomer.orderDialogue[0].portrait;
            }

            return new DialogueLine
            {
                speakerId = speaker,
                text = ResolveReactionText(result),
                portrait = portrait
            };
        }

        private string ResolveReactionText(DrinkResult result)
        {
            if (_currentCustomer != null)
            {
                if (result == DrinkResult.GreatSuccess && !string.IsNullOrEmpty(_currentCustomer.greatSuccessLine))
                {
                    return _currentCustomer.greatSuccessLine;
                }

                if (result == DrinkResult.Success && !string.IsNullOrEmpty(_currentCustomer.successLine))
                {
                    return _currentCustomer.successLine;
                }

                if (result == DrinkResult.Fail && !string.IsNullOrEmpty(_currentCustomer.failLine))
                {
                    return _currentCustomer.failLine;
                }
            }

            if (result == DrinkResult.GreatSuccess)
            {
                return !string.IsNullOrEmpty(greatReactionLine) ? greatReactionLine : "만족스러워";
            }

            if (result == DrinkResult.Success)
            {
                return !string.IsNullOrEmpty(successReactionLine) ? successReactionLine : "나쁘지 않아";
            }

            return !string.IsNullOrEmpty(failReactionLine) ? failReactionLine : "실망스럽군";
        }

        private void FadeOutCustomerThenNext()
        {
            if (spawner != null)
            {
                spawner.FadeOutAndClear(NextCustomer);
                return;
            }

            NextCustomer();
        }

        private void EndDay()
        {
            // 튜토리얼 중에는 하루 종료/엔딩/저장 로직을 타지 않는다.
            if (TutorialContext.IsActive)
            {
                return;
            }

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
                    endingCoinSummary.Show(TotalCoins, GreatCoins, EndingFlow.EnterEndingScene);
                }
                else
                {
                    EndingFlow.EnterEndingScene();
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
