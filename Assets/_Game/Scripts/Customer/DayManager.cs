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
        [SerializeField] private CafeMainDialogTable mainDialogTable;
        [SerializeField] private List<CustomerSO> allCustomers;
        [SerializeField] private int fareReward = 10;
        [SerializeField] private bool forceServiceStateOnStart;
        [TextArea] [SerializeField] private string greatReactionLine = "ÎßåÏ°±?ä§?ü¨?õå";
        [TextArea] [SerializeField] private string successReactionLine = "?Çò?ÅòÏß? ?ïä?ïÑ";
        [TextArea] [SerializeField] private string failReactionLine = "?ã§ÎßùÏä§?üΩÍµ?";

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
            // ?äú?Ü†Î¶¨Ïñº Ï§ëÏóê?äî Cafe Í∞? Additive Î∞∞Í≤Ω?úºÎ°úÎßå ?ñ† ?ûà?úºÎØ?Î°?
            // ?ã§?†ú ?ÑúÎπÑÏä§Î•? ?ûê?èô ?ãú?ûë?ïòÏß? ?ïä?äî?ã§(?Üê?ãò/????û• ?óÜ?ùå).
            if (TutorialContext.IsActive)
            {
                return;
            }

            AudioManager.Instance?.PlayCafeBgm();

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
            for (int i = 0; i < MaxCoinSlots; i++)
            {
                _coins.Add(CoinType.None);
            }

            // Continue start uses aggregate counts only, so restored slots are best-effort.
            var normals = Mathf.Max(0, TotalCoins - GreatCoins);
            var golds = Mathf.Max(0, GreatCoins);
            int idx = 0;
            for (int i = 0; i < normals && idx < MaxCoinSlots; i++, idx++)
            {
                _coins[idx] = CoinType.Normal;
            }
            for (int i = 0; i < golds && idx < MaxCoinSlots; i++, idx++)
            {
                _coins[idx] = CoinType.Gold;
            }
        }

        private void SetCoinForCurrentDay(CoinType type)
        {
            var dayIndex = CurrentDay - 1;
            if (dayIndex < 0 || dayIndex >= MaxCoinSlots)
            {
                return;
            }

            while (_coins.Count < MaxCoinSlots)
            {
                _coins.Add(CoinType.None);
            }

            _coins[dayIndex] = type;
        }

        private void SaveProgress()
        {
            // ?äú?Ü†Î¶¨Ïñº ÏßÑÌñâ Í≤∞Í≥º?äî ?†à??? ????û•?ïòÏß? ?ïä?äî?ã§(?ïà?†ÑÎß?).
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

            // ÎØ∏ÎãàÍ≤åÏûÑ ÏßÑÏûÖ ?†Ñ ????ôî: cafe_MainDialog_Source ?ùò '?ùºÎ∞?' Î∂ÑÍ∏∞ ?Ç¨?ö©(?óÜ?úºÎ©? Í∏∞Ï°¥ Ï£ºÎ¨∏ ????Ç¨).
            if (PlayMainDialog(CafeMainDialogTable.BranchNormal, OnOrderDialogueDone))
            {
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
                SetCoinForCurrentDay(CoinType.Gold);
            }
            else if (result == DrinkResult.Success)
            {
                TotalCoins++;
                SetCoinForCurrentDay(CoinType.Normal);
            }
            else
            {
                SetCoinForCurrentDay(CoinType.None);
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
                // ÎØ∏ÎãàÍ≤åÏûÑ Í≤∞Í≥º ????ôî: Í≤∞Í≥º?óê ?î∞Î•? Î∂ÑÍ∏∞(????Ñ±Í≥?/?Ñ±Í≥?/?ã§?å®) ?Ç¨?ö©(?óÜ?úºÎ©? Í∏∞Ï°¥ ?ïú Ï§? Î∞òÏùë).
                if (PlayMainDialog(BranchForResult(result), FadeOutCustomerThenNext))
                {
                    return;
                }

                var line = BuildReactionLine(result);
                dialogue.Play(new[] { line }, FadeOutCustomerThenNext);
                return;
            }

            FadeOutCustomerThenNext();
        }

        // cafe_MainDialog_Source ?ùò (?òÑ?û¨ ?Ç†Ïß?, Î∂ÑÍ∏∞) ????Ç¨ Î¨∂Ïùå?ùÑ ?û¨?Éù?ïú?ã§.
        // ?Üê?ãò?ù¥ ÎßêÌïò?äî Ï§ÑÏóê?Ñú?äî ?ï¥?ãπ Ï§ÑÏùò Í∞êÏ†ï ?ä§?îÑ?ùº?ù¥?ä∏Î°? ?Üê?ãò ?ù¥ÎØ∏Ï??Î•? ÍµêÏ≤¥?ïú?ã§.
        // ?ôî?ûê Ï¥àÏÉÅ?ôî/Î∞∞Í≤Ω ?îîÎ∞? ?óÜ?ù¥ ????Ç¨ ?Öç?ä§?ä∏ + ?Üê?ãò ?ù¥ÎØ∏Ï??Îß? Î≥¥Ïó¨Ï§??ã§.
        // ?û¨?Éù?ï† ????Ç¨Í∞? ?ûà?úºÎ©? true, ?óÜ?úºÎ©?(?è¥Î∞? ?ïÑ?öî) false Î•? Î∞òÌôò?ïú?ã§.
        private bool PlayMainDialog(string branch, System.Action onComplete)
        {
            if (mainDialogTable == null || dialogue == null)
            {
                return false;
            }

            var lines = mainDialogTable.GetLines(CurrentDay, branch);
            if (lines == null || lines.Count == 0)
            {
                return false;
            }

            var dlg = new DialogueLine[lines.Count];
            var sprites = new Sprite[lines.Count];
            for (int i = 0; i < lines.Count; i++)
            {
                dlg[i] = new DialogueLine
                {
                    speakerId = lines[i].speaker,
                    text = lines[i].text,
                    portrait = null
                };
                sprites[i] = lines[i].isCustomerLine ? lines[i].customerSprite : null;
            }

            System.Action<int> onLineShown = idx =>
            {
                if (idx >= 0 && idx < sprites.Length && sprites[idx] != null && spawner != null)
                {
                    spawner.SetPortraitSprite(sprites[idx]);
                }
            };

            dialogue.Play(dlg, onComplete, true, onLineShown, false);
            return true;
        }

        private static string BranchForResult(DrinkResult result)
        {
            if (result == DrinkResult.GreatSuccess)
            {
                return CafeMainDialogTable.BranchGreatSuccess;
            }

            if (result == DrinkResult.Success)
            {
                return CafeMainDialogTable.BranchSuccess;
            }

            return CafeMainDialogTable.BranchFail;
        }

        private DialogueLine BuildReactionLine(DrinkResult result)
        {
            string speaker = "?Üê?ãò";
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
                return !string.IsNullOrEmpty(greatReactionLine) ? greatReactionLine : "ÎßåÏ°±?ä§?ü¨?õå";
            }

            if (result == DrinkResult.Success)
            {
                return !string.IsNullOrEmpty(successReactionLine) ? successReactionLine : "?Çò?ÅòÏß? ?ïä?ïÑ";
            }

            return !string.IsNullOrEmpty(failReactionLine) ? failReactionLine : "?ã§ÎßùÏä§?üΩÍµ?";
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
            // ?äú?Ü†Î¶¨Ïñº Ï§ëÏóê?äî ?ïòÎ£? Ï¢ÖÎ£å/?óî?î©/????û• Î°úÏßÅ?ùÑ ???Ïß? ?ïä?äî?ã§.
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
                    endingCoinSummary.Show(_coins, EndingFlow.EnterEndingScene);
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

            // ÏΩîÏù∏?ù¥ 3Í∞? ÎØ∏Îßå(0~2)?ù¥Î©? Î±ÉÏÇØ?ùÑ Î™? Î≤åÏñ¥ Î∞∞Îìú?óî?î©?úºÎ°? Î∂ÑÍ∏∞.
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
