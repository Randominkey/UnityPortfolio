using System.Collections.Generic;
using MasterFramework.Core;
using MasterFramework.Interaction;
using UnityEngine;

namespace MasterFramework.Playable.Month10High
{
    public class RectSquareRuleValidator : IRuleValidator
    {
        public string RuleName => "RectSquareRuleValidator";

        public ValidationResult Validate(IInteractable[] pieces, IGridSystem gridSystem)
        {
            var rsGrid = gridSystem as RectSquareGridSystem;
            if (rsGrid == null)
            {
                return ValidationResult.Incorrect("Invalid GridSystem type.");
            }

            List<string> offendingIds = new List<string>();

            foreach (var piece in pieces)
            {
                var frame = piece as PlayableRectSquareFrame;
                if (frame == null) continue;

                // 영역 내부의 기호 셀 목록 수집
                var signCells = rsGrid.GetSignCellsInArea(frame.StartCell, frame.EndCell);

                if (signCells.Count == 0)
                {
                    // 기호가 없는 사각형은 에러
                    offendingIds.Add(frame.UniqueId);
                }
                else if (signCells.Count == 1)
                {
                    var signCell = signCells[0];
                    int startRow = Mathf.Min(frame.StartCell.RowIndex, frame.EndCell.RowIndex);
                    int endRow = Mathf.Max(frame.StartCell.RowIndex, frame.EndCell.RowIndex);
                    int startCol = Mathf.Min(frame.StartCell.ColumnIndex, frame.EndCell.ColumnIndex);
                    int endCol = Mathf.Max(frame.StartCell.ColumnIndex, frame.EndCell.ColumnIndex);

                    int rowSize = endRow - startRow + 1;
                    int colSize = endCol - startCol + 1;

                    bool isCorrect = false;
                    switch (signCell.CellSign)
                    {
                        case CellSign.Horizontal:
                            isCorrect = colSize > rowSize;
                            break;
                        case CellSign.Vertical:
                            isCorrect = colSize < rowSize;
                            break;
                        case CellSign.Square:
                            isCorrect = colSize == rowSize;
                            break;
                    }

                    if (!isCorrect)
                    {
                        offendingIds.Add(frame.UniqueId);
                    }
                }
                else
                {
                    // 기호가 2개 이상 포함된 경우 에러
                    offendingIds.Add(frame.UniqueId);
                }
            }

            if (offendingIds.Count > 0)
            {
                return ValidationResult.Incorrect("Some frames do not satisfy the rules.", offendingIds);
            }

            return ValidationResult.Correct;
        }
    }
}
