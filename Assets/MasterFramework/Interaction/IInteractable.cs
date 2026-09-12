using UniRx;
using UnityEngine;

namespace MasterFramework.Interaction
{
    public enum InteractionState
    {
        Idle,
        Hovered,
        Dragging,
        Rotating
    }

    /// <summary>
    /// Core interface for any object that can be dragged, rotated, flipped, or snapped to grids.
    /// </summary>
    public interface IInteractable
    {
        string UniqueId { get; }
        Transform Transform { get; }
        IReadOnlyReactiveProperty<InteractionState> CurrentState { get; }
        
        bool IsMovable { get; set; }
        bool IsRotatable { get; set; }
        bool IsCollidable { get; set; }

        void OnPointerDown(Vector3 clickPosition);
        void OnDrag(Vector3 newPosition);
        void OnPointerUp();
        
        void Rotate(float angle);
        void Flip(bool horizontal);
        void SnapTo(Vector3 snapPosition);
    }
}
