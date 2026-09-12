using Cysharp.Threading.Tasks;
using UniRx;

namespace MasterFramework.Core
{
    public enum StepProgressState
    {
        Ready = 0,
        Complete = 1,
        Fail = 2
    }

    /// <summary>
    /// Base lifecycle contract for any UI Presenter in the framework.
    /// Supports UniTask async initialization and view transitions.
    /// </summary>
    public interface IPresenter
    {
        UniTask InitializeAsync();
        UniTask EnterAsync();
        UniTask ExitAsync();
    }

    /// <summary>
    /// Contract for a multi-page Stage. Coordinates individual PageOrganizers.
    /// </summary>
    public interface IStageOrganizer
    {
        int StageIndex { get; set; }
        IReadOnlyReactiveProperty<bool> IsStageCleared { get; }
        
        UniTask InitializeStageAsync(int index);
        void SetStageData(UnityEngine.ScriptableObject data); // [NEW] 장르별 데이터 주입 공용 규약
        UniTask EnterStageAsync();
        UniTask ExitStageAsync();
        void RefreshStage();
    }

    /// <summary>
    /// Contract for an individual Page/Question.
    /// </summary>
    public interface IPageOrganizer
    {
        int PageIndex { get; set; }
        IReadOnlyReactiveProperty<StepProgressState> ProgressState { get; }
        
        UniTask InitializePageAsync(int index);
        void RefreshPage();
        bool CheckAnswer(); // True if correct, False if incorrect
    }

    /// <summary>
    /// Contract for stages or pages that support state serialization and restoration (Memento Pattern).
    /// </summary>
    public interface IStateRestorable
    {
        string SerializeState();
        void DeserializeAndRestoreState(string stateData);
    }
}
