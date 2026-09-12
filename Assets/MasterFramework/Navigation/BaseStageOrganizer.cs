using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;
using Zenject;
using MasterFramework.Core;

namespace MasterFramework.Navigation
{
    public class BaseStageOrganizer : MonoBehaviour, IStageOrganizer
    {
        [Header("Stage Settings")]
        [SerializeField] private List<BasePageOrganizer> pageOrganizers = new List<BasePageOrganizer>();
        
        // Injected Services
        protected ISoundService SoundService;
        protected IPopupService PopupService;
        protected IFeedbackStampService FeedbackStampService;

        // Reactive Properties
        public int StageIndex { get; set; }
        public IReadOnlyReactiveProperty<bool> IsStageCleared => _isStageCleared;
        protected readonly BoolReactiveProperty _isStageCleared = new BoolReactiveProperty(false);

        protected void SetStageCleared(bool cleared)
        {
            _isStageCleared.Value = cleared;
        }
        
        private readonly ReactiveProperty<int> _currentPageIndex = new ReactiveProperty<int>(0);
        private readonly List<StepProgressState> _pageProgressStates = new List<StepProgressState>();
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(ISoundService soundService, IPopupService popupService, IFeedbackStampService feedbackStampService)
        {
            SoundService = soundService;
            PopupService = popupService;
            FeedbackStampService = feedbackStampService;
        }

        public virtual async UniTask InitializeStageAsync(int index)
        {
            StageIndex = index;
            _isStageCleared.Value = false;

            // Automatically scan child objects for page organizers if list is empty
            if (pageOrganizers.Count == 0)
            {
                pageOrganizers = GetComponentsInChildren<BasePageOrganizer>(true).ToList();
            }

            _pageProgressStates.Clear();
            for (int i = 0; i < pageOrganizers.Count; i++)
            {
                _pageProgressStates.Add(StepProgressState.Ready);
                await pageOrganizers[i].InitializePageAsync(i);
            }

            // Monitor active page transitions
            _currentPageIndex
                .DistinctUntilChanged()
                .Subscribe(ActivatePage)
                .AddTo(_disposables);
        }

        public virtual void SetStageData(UnityEngine.ScriptableObject data)
        {
            // Base organizer doesn't process SO data by default
        }

        public virtual async UniTask EnterStageAsync()
        {
            gameObject.SetActive(true);
            ActivatePage(_currentPageIndex.Value);
            await UniTask.CompletedTask;
        }

        public virtual async UniTask ExitStageAsync()
        {
            gameObject.SetActive(false);
            _disposables.Clear();
            await UniTask.CompletedTask;
        }

        public virtual void RefreshStage()
        {
            int activeIndex = _currentPageIndex.Value;
            if (activeIndex >= 0 && activeIndex < pageOrganizers.Count)
            {
                pageOrganizers[activeIndex].RefreshPage();
            }
        }

        private void ActivatePage(int pageIndex)
        {
            for (int i = 0; i < pageOrganizers.Count; i++)
            {
                pageOrganizers[i].gameObject.SetActive(i == pageIndex);
            }
            
            Debug.Log($"[Stage {StageIndex}] Switched to page {pageIndex}");
        }

        /// <summary>
        /// Called when the player clicks the "Check Answer" button on the HUD.
        /// </summary>
        public void EvaluateCurrentPage()
        {
            int activeIndex = _currentPageIndex.Value;
            if (activeIndex < 0 || activeIndex >= pageOrganizers.Count) return;

            bool isCorrect = pageOrganizers[activeIndex].CheckAnswer();
            if (isCorrect)
            {
                _pageProgressStates[activeIndex] = StepProgressState.Complete;
                SoundService.PlaySFX("sfx_stamp_win");
                PopupService.ShowToast("정답입니다!", 1.5f);
                FeedbackStampService.ShowStampAsync(StampType.Complete).Forget();
                
                CheckStageCompletion();
            }
            else
            {
                _pageProgressStates[activeIndex] = StepProgressState.Fail;
                SoundService.PlaySFX("sfx_stamp_lose");
                PopupService.ShowToast("다시 생각해보세요.", 1.5f);
                FeedbackStampService.ShowStampAsync(StampType.Fail).Forget();
            }
        }

        private void CheckStageCompletion()
        {
            // If all pages are marked Complete, the stage is cleared
            bool allComplete = _pageProgressStates.All(state => state == StepProgressState.Complete);
            if (allComplete)
            {
                _isStageCleared.Value = true;
                Debug.Log($"[Stage {StageIndex}] Stage completed!");
            }
        }

        public void SetCurrentPage(int index)
        {
            if (index >= 0 && index < pageOrganizers.Count)
            {
                _currentPageIndex.Value = index;
            }
        }

        protected virtual void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}
