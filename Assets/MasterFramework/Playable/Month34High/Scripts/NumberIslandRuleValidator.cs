using System.Collections.Generic;
using MasterFramework.Core;
using MasterFramework.Interaction;
using UnityEngine;

namespace MasterFramework.Playable.Month34High
{
    public struct RectArea
    {
        public int rowMin;
        public int rowMax;
        public int colMin;
        public int colMax;

        public RectArea(int rMin, int rMax, int cMin, int cMax)
        {
            rowMin = rMin;
            rowMax = rMax;
            colMin = cMin;
            colMax = cMax;
        }
    }

    public class NumberIslandRuleValidator : IRuleValidator
    {
        public string RuleName => "NumberIslandRuleValidator";

        public ValidationResult Validate(IInteractable[] pieces, IGridSystem gridSystem)
        {
            // Default interface contract; gameplay stages call the specialized ValidateIslandGrid below
            return ValidationResult.Correct;
        }

        public ValidationResult ValidateIslandGrid(NumberIslandCell[,] cells, int rowCount, int columnCount)
        {
            bool isCorrect = true;
            List<RectArea> rectangles = new List<RectArea>();

            // 1. Reset state
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < columnCount; c++)
                {
                    cells[r, c].cellState.Value = CellState.Ready;
                    cells[r, c].containedChecked = false;
                }
            }

            // 2. Validate Land Islands & check Blanks
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < columnCount; c++)
                {
                    CellType type = cells[r, c].cellType.Value;

                    if (type == CellType.Blank)
                    {
                        isCorrect = false;
                    }
                    else if (type == CellType.Land)
                    {
                        if (!cells[r, c].containedChecked)
                        {
                            List<NumberIslandCell> connected = new List<NumberIslandCell>();
                            connected.Add(cells[r, c]);
                            FloodFillLand(cells, rowCount, columnCount, connected);

                            int rMin = r, rMax = r;
                            int cMin = c, cMax = c;
                            List<int> numbers = new List<int>();

                            for (int i = 0; i < connected.Count; i++)
                            {
                                var cell = connected[i];
                                if (cell.row < rMin) rMin = cell.row;
                                if (cell.row > rMax) rMax = cell.row;
                                if (cell.column < cMin) cMin = cell.column;
                                if (cell.column > cMax) cMax = cell.column;

                                if (cell.number > 0)
                                {
                                    numbers.Add(cell.number);
                                }
                            }

                            int expectedArea = (rMax - rMin + 1) * (cMax - cMin + 1);
                            bool isRectangle = (connected.Count == expectedArea);

                            if (numbers.Count > 1)
                            {
                                // 하나의 섬에 숫자가 2개 이상이면 오답
                                isCorrect = false;
                                SetCellsState(connected, CellState.Error);
                            }
                            else if (numbers.Count == 1)
                            {
                                int targetNum = numbers[0];
                                if (isRectangle && targetNum == connected.Count)
                                {
                                    // 완벽한 직사각형 섬
                                    SetCellsState(connected, CellState.Correct);
                                    rectangles.Add(new RectArea(rMin, rMax, cMin, cMax));
                                }
                                else
                                {
                                    isCorrect = false;
                                    if (targetNum < connected.Count)
                                    {
                                        SetCellsState(connected, CellState.Error);
                                    }
                                }
                            }
                            else
                            {
                                // 숫자가 없는 단독 셀 섬(1x1)
                                if (connected.Count == 1)
                                {
                                    SetCellsState(connected, CellState.Correct);
                                    rectangles.Add(new RectArea(rMin, rMax, cMin, cMax));
                                }
                                else
                                {
                                    isCorrect = false;
                                }
                            }
                        }
                    }
                    else if (type == CellType.Sea)
                    {
                        // 2x2 바다 정사각형 방지 검사
                        if (r < rowCount - 1 && c < columnCount - 1)
                        {
                            if (cells[r + 1, c].cellType.Value == CellType.Sea &&
                                cells[r, c + 1].cellType.Value == CellType.Sea &&
                                cells[r + 1, c + 1].cellType.Value == CellType.Sea)
                            {
                                cells[r, c].cellState.Value = CellState.Error;
                                cells[r + 1, c].cellState.Value = CellState.Error;
                                cells[r, c + 1].cellState.Value = CellState.Error;
                                cells[r + 1, c + 1].cellState.Value = CellState.Error;
                                isCorrect = false;
                            }
                        }
                    }
                }
            }

            // 3. 섬(직사각형) 간 대각선 단일 네트워크 연결 검사
            int rectCount = rectangles.Count;
            if (isCorrect && rectCount > 0)
            {
                List<int> connectedIndices = new List<int> { 0 };
                int checkIndex = -1;

                while (++checkIndex < connectedIndices.Count)
                {
                    int currentIdx = connectedIndices[checkIndex];
                    RectArea cur = rectangles[currentIdx];

                    Vector2Int leftTopDiag = new Vector2Int(cur.rowMin - 1, cur.colMin - 1);
                    Vector2Int rightTopDiag = new Vector2Int(cur.rowMin - 1, cur.colMax + 1);
                    Vector2Int leftBottomDiag = new Vector2Int(cur.rowMax + 1, cur.colMin - 1);
                    Vector2Int rightBottomDiag = new Vector2Int(cur.rowMax + 1, cur.colMax + 1);

                    for (int j = 0; j < rectCount; j++)
                    {
                        if (j != currentIdx && !connectedIndices.Contains(j))
                        {
                            RectArea target = rectangles[j];

                            if ((leftTopDiag.x == target.rowMax && leftTopDiag.y == target.colMax) ||
                                (rightTopDiag.x == target.rowMax && rightTopDiag.y == target.colMin) ||
                                (leftBottomDiag.x == target.rowMin && leftBottomDiag.y == target.colMax) ||
                                (rightBottomDiag.x == target.rowMin && rightBottomDiag.y == target.colMin))
                            {
                                connectedIndices.Add(j);
                            }
                        }
                    }
                }

                if (connectedIndices.Count != rectCount)
                {
                    isCorrect = false;
                }
            }
            else if (rectCount == 0)
            {
                isCorrect = false;
            }

            return isCorrect ? ValidationResult.Correct : ValidationResult.Incorrect("Numbers and islands do not match rules.");
        }

        private void FloodFillLand(NumberIslandCell[,] cells, int rowCount, int colCount, List<NumberIslandCell> connected)
        {
            bool newAdded = false;

            for (int i = 0; i < connected.Count; i++)
            {
                var cur = connected[i];
                if (!cur.containedChecked)
                {
                    cur.containedChecked = true;
                    int r = cur.row;
                    int c = cur.column;

                    // Up
                    if (r > 0 && !cells[r - 1, c].containedChecked && cells[r - 1, c].cellType.Value == CellType.Land && !connected.Contains(cells[r - 1, c]))
                    {
                        connected.Add(cells[r - 1, c]);
                        newAdded = true;
                    }
                    // Down
                    if (r < rowCount - 1 && !cells[r + 1, c].containedChecked && cells[r + 1, c].cellType.Value == CellType.Land && !connected.Contains(cells[r + 1, c]))
                    {
                        connected.Add(cells[r + 1, c]);
                        newAdded = true;
                    }
                    // Left
                    if (c > 0 && !cells[r, c - 1].containedChecked && cells[r, c - 1].cellType.Value == CellType.Land && !connected.Contains(cells[r, c - 1]))
                    {
                        connected.Add(cells[r, c - 1]);
                        newAdded = true;
                    }
                    // Right
                    if (c < colCount - 1 && !cells[r, c + 1].containedChecked && cells[r, c + 1].cellType.Value == CellType.Land && !connected.Contains(cells[r, c + 1]))
                    {
                        connected.Add(cells[r, c + 1]);
                        newAdded = true;
                    }
                }
            }

            if (newAdded)
            {
                FloodFillLand(cells, rowCount, colCount, connected);
            }
        }

        private void SetCellsState(List<NumberIslandCell> cellList, CellState state)
        {
            for (int i = 0; i < cellList.Count; i++)
            {
                cellList[i].cellState.Value = state;
            }
        }
    }
}
