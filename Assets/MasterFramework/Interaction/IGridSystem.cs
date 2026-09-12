using UnityEngine;

namespace MasterFramework.Interaction
{
    /// <summary>
    /// Contract for snapping structures (Square Grids, Hex Grids, or custom Anchor Maps).
    /// </summary>
    public interface IGridSystem
    {
        /// <summary>
        /// Attempts to get the nearest snap coordinate for a given world position.
        /// </summary>
        bool TryGetSnapPosition(Vector3 targetWorldPos, out Vector3 snappedWorldPos);

        /// <summary>
        /// Checks whether the snapped cells for a given interactable shape are occupied or outside bounds.
        /// </summary>
        bool IsOccupiedOrInvalid(IInteractable interactable, Vector3 targetWorldPos);

        /// <summary>
        /// Records that the interactable piece has occupied specific cells on the grid.
        /// </summary>
        void OccupyCells(IInteractable interactable, Vector3 snappedWorldPos);

        /// <summary>
        /// Releases all cells currently occupied by this interactable piece.
        /// </summary>
        void ReleaseCells(IInteractable interactable);
    }
}
