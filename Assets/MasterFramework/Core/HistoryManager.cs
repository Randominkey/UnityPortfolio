using System;
using System.Collections.Generic;
using UniRx;

namespace MasterFramework.Core
{
    /// <summary>
    /// Generic Memento Snapshot History Manager for Undo/Redo/Replay operations.
    /// Thread-safe and Reactive-enabled.
    /// </summary>
    public class HistoryManager<TState>
    {
        private readonly Stack<TState> _undoStack = new Stack<TState>();
        private readonly Stack<TState> _redoStack = new Stack<TState>();
        private readonly Subject<Unit> _onHistoryChanged = new Subject<Unit>();

        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;
        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public IObservable<Unit> OnHistoryChanged => _onHistoryChanged;

        /// <summary>
        /// Records the current state snapshot before performing a new action.
        /// Clears the redo stack.
        /// </summary>
        public void RecordState(TState state)
        {
            _undoStack.Push(state);
            _redoStack.Clear();
            _onHistoryChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// Reverts to the previous state. Pushes the currentState onto the redo stack.
        /// </summary>
        public TState Undo(TState currentState)
        {
            if (!CanUndo) return currentState;

            _redoStack.Push(currentState);
            TState previousState = _undoStack.Pop();
            _onHistoryChanged.OnNext(Unit.Default);
            return previousState;
        }

        /// <summary>
        /// Re-applies the next state from the redo stack. Pushes the currentState onto the undo stack.
        /// </summary>
        public TState Redo(TState currentState)
        {
            if (!CanRedo) return currentState;

            _undoStack.Push(currentState);
            TState nextState = _redoStack.Pop();
            _onHistoryChanged.OnNext(Unit.Default);
            return nextState;
        }

        /// <summary>
        /// Clears all undo/redo history.
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _onHistoryChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// Returns all states in chronological order for replay.
        /// </summary>
        public List<TState> GetAllStates()
        {
            var list = new List<TState>(_undoStack);
            list.Reverse();
            return list;
        }
    }
}
