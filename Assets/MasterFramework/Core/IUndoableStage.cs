using System;
using UniRx;

namespace MasterFramework.Core
{
    /// <summary>
    /// Contract for stages that support Memento-based Undo / Redo operations.
    /// </summary>
    public interface IUndoableStage
    {
        bool CanUndo { get; }
        bool CanRedo { get; }
        IObservable<Unit> OnHistoryChanged { get; }

        void Undo();
        void Redo();
        void ClearHistory();
    }
}
