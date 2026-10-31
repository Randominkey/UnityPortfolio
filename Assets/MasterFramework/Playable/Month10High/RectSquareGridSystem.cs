using System.Collections.Generic;
using MasterFramework.Interaction;
using UnityEngine;

namespace MasterFramework.Playable.Month10High
{
    public class RectSquareGridSystem : MonoBehaviour, IGridSystem
    {
        private PlayableRectSquareCell[,] _cells;
        private int _rows;
        private int _cols;

        public void InitializeGrid(PlayableRectSquareCell[,] cells)
        {
            _cells = cells;
            _rows = cells.GetLength(0);
            _cols = cells.GetLength(1);
        }

        public bool TryGetSnapPosition(Vector3 targetWorldPos, out Vector3 snappedWorldPos)
        {
            PlayableRectSquareCell nearest = GetNearestCell(targetWorldPos);
            if (nearest != null)
            {
                snappedWorldPos = nearest.RectTransform.position;
                return true;
            }
            snappedWorldPos = Vector3.zero;
            return false;
        }

        public bool IsOccupiedOrInvalid(IInteractable interactable, Vector3 targetWorldPos)
        {
            var frame = interactable as PlayableRectSquareFrame;
            if (frame == null) return true;

            PlayableRectSquareCell targetEndCell = GetNearestCell(targetWorldPos);
            if (targetEndCell == null) return true;

            int startRow = frame.StartCell.RowIndex;
            int startCol = frame.StartCell.ColumnIndex;
            int endRow = targetEndCell.RowIndex;
            int endCol = targetEndCell.ColumnIndex;

            int rowMin = Mathf.Min(startRow, endRow);
            int rowMax = Mathf.Max(startRow, endRow);
            int colMin = Mathf.Min(startCol, endCol);
            int colMax = Mathf.Max(startCol, endCol);

            for (int r = rowMin; r <= rowMax; r++)
            {
                for (int c = colMin; c <= colMax; c++)
                {
                    if (_cells[r, c].IsContained)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void OccupyCells(IInteractable interactable, Vector3 snappedWorldPos)
        {
            var frame = interactable as PlayableRectSquareFrame;
            if (frame == null || frame.StartCell == null || frame.EndCell == null) return;

            SetContainedState(frame.StartCell, frame.EndCell, true);
        }

        public void ReleaseCells(IInteractable interactable)
        {
            var frame = interactable as PlayableRectSquareFrame;
            if (frame == null || frame.StartCell == null || frame.EndCell == null) return;

            SetContainedState(frame.StartCell, frame.EndCell, false);
        }

        public List<PlayableRectSquareCell> GetSignCellsInArea(PlayableRectSquareCell start, PlayableRectSquareCell end)
        {
            List<PlayableRectSquareCell> signCells = new List<PlayableRectSquareCell>();
            if (start == null || end == null) return signCells;

            int rowMin = Mathf.Min(start.RowIndex, end.RowIndex);
            int rowMax = Mathf.Max(start.RowIndex, end.RowIndex);
            int colMin = Mathf.Min(start.ColumnIndex, end.ColumnIndex);
            int colMax = Mathf.Max(start.ColumnIndex, end.ColumnIndex);

            for (int r = rowMin; r <= rowMax; r++)
            {
                for (int c = colMin; c <= colMax; c++)
                {
                    if (_cells[r, c].CellSign != CellSign.Empty)
                    {
                        signCells.Add(_cells[r, c]);
                    }
                }
            }
            return signCells;
        }

        public void SetContainedState(PlayableRectSquareCell start, PlayableRectSquareCell end, bool state)
        {
            if (start == null || end == null) return;

            int rowMin = Mathf.Min(start.RowIndex, end.RowIndex);
            int rowMax = Mathf.Max(start.RowIndex, end.RowIndex);
            int colMin = Mathf.Min(start.ColumnIndex, end.ColumnIndex);
            int colMax = Mathf.Max(start.ColumnIndex, end.ColumnIndex);

            for (int r = rowMin; r <= rowMax; r++)
            {
                for (int c = colMin; c <= colMax; c++)
                {
                    _cells[r, c].IsContained = state;
                }
            }
        }

        public PlayableRectSquareCell GetNearestCell(Vector3 worldPos)
        {
            PlayableRectSquareCell nearest = null;
            float minDist = float.MaxValue;

            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    float dist = Vector3.Distance(_cells[r, c].RectTransform.position, worldPos);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = _cells[r, c];
                    }
                }
            }
            return nearest;
        }

        public bool CheckAllContained()
        {
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    if (!_cells[r, c].IsContained)
                        return false;
                }
            }
            return true;
        }

        public void ClearAllContained()
        {
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    _cells[r, c].IsContained = false;
                    _cells[r, c].ResetState();
                }
            }
        }
    }
}
