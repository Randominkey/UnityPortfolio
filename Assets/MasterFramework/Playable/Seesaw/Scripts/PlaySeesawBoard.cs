using CMS.Template.UI.Keypad;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PlaySeesaw
{
    public enum MoveDirection
    {
        Up,
        Down,
        Left,
        Right,
    }

    public class PlaySeesawBoard : MiniGameBoardBase
    {
        [SerializeField] private int cellSideLength;
        [Header("x: blank / 4: lever")]
        [SerializeField] private string[] map;
        [SerializeField] private string rowNumbers;
        [SerializeField] private string columnNumbers;
        [SerializeField] private string[] frameMap;

        private int rowCount;
        private int columnCount;
        [SerializeField] private GridLayoutGroup innerGrid;
        [SerializeField] private GridLayoutGroup countRowGrid;
        [SerializeField] private GridLayoutGroup countColGrid;
        [SerializeField] private Transform frameParent;

        [SerializeField] private PlaySeesawInnerCell innerCellPrefab;
        [SerializeField] private PlaySeesawOuterCell outerCellPrefab;
        [SerializeField] private PlaySeesawFrame framePrefab;
        private PlaySeesawInnerCell[,] innerCells;
        private PlaySeesawOuterCell[] rowOuterCells;
        private PlaySeesawOuterCell[] colOuterCells;
        private Dictionary<char, PlaySeesawFrame> frames = new Dictionary<char, PlaySeesawFrame>();

        [SerializeField] private NeoKeypadComponent neoKeypadComponent;
        [SerializeField] private List<Button> uDLRArrowButtons;

        private ReactiveProperty<int> currentRow = new ReactiveProperty<int>();
        private readonly ReactiveProperty<int> currentColumn = new ReactiveProperty<int>();

        public override void Ready()
        {
            rowCount = map.Length;
            columnCount = map[0].Length;

            innerGrid.cellSize = new Vector2(cellSideLength, cellSideLength);
            countRowGrid.cellSize = new Vector2(94, cellSideLength);
            countColGrid.cellSize = new Vector2(cellSideLength, 72);

            innerGrid.constraintCount = columnCount;

            innerCells = new PlaySeesawInnerCell[rowCount, columnCount];
            rowOuterCells = new PlaySeesawOuterCell[rowCount];
            colOuterCells = new PlaySeesawOuterCell[columnCount];

            //outer cell
            for (int i = 0; i < rowCount; i++)
            {
                int row = i;
                rowOuterCells[row] = Instantiate(outerCellPrefab, countRowGrid.transform);
                rowOuterCells[row].observingInnerCells = new PlaySeesawInnerCell[columnCount];
            }

            for (int j = 0; j < columnCount; j++)
            {
                int col = j;
                colOuterCells[col] = Instantiate(outerCellPrefab, countColGrid.transform);
                colOuterCells[col].observingInnerCells = new PlaySeesawInnerCell[rowCount];
            }

            for (int i = 0; i < rowCount; i++)
            {
                int row = i;

                for (int j = 0; j < columnCount; j++)
                {
                    int col = j;

                    //inner cell


                    innerCells[row, col] = Instantiate(innerCellPrefab, innerGrid.transform);

                    if (map[row][col] != 'x')
                    {
                        innerCells[row, col].nKLT.Toggle.targetGraphic.color = new Color32(220, 220, 220, 255);
                    }

                    innerCells[row, col].Ready(neoKeypadComponent);

                    innerCells[row, col].nKLT.Toggle
                        .OnValueChangedAsObservable()
                        .Subscribe(isOn =>
                        {
                            if (isOn)
                            {
                                currentRow.Value = row;
                                currentColumn.Value = col;
                            }
                        });

                    rowOuterCells[row].observingInnerCells[col] = innerCells[row, col];
                    colOuterCells[col].observingInnerCells[row] = innerCells[row, col];
                }

                rowOuterCells[row].Ready(rowNumbers[row]);
            }


            for (int j = 0; j < columnCount; j++)
            {
                int col = j;
                colOuterCells[col].Ready(columnNumbers[col]);
            }

            //frame
            Dictionary<char, List<Vector2Int>> frameGoupIndices = new Dictionary<char, List<Vector2Int>>();

            for (int i = 0; i < rowCount; i++)
            {
                int row = i;

                for (int j = 0; j < columnCount; j++)
                {
                    int col = j;

                    char key = frameMap[row][col];

                    if (!frames.ContainsKey(key))
                    {
                        PlaySeesawFrame frame = Instantiate(framePrefab, frameParent);

                        frame.Ready();

                        frames.Add(key, frame);

                        List<Vector2Int> indices = new List<Vector2Int>();

                        frameGoupIndices.Add(key, indices);
                    }

                    frames[key].AddObservingCell(innerCells[row, col]);
                    frameGoupIndices[key].Add(new Vector2Int(row, col));
                }
            }

            foreach (var indices in frameGoupIndices)
            {
                int length = indices.Value.Count;

                Vector2Int diff = indices.Value[length - 1] - indices.Value[0];

                if (diff.x == 0)
                {
                    //same row == vertical

                    frames[indices.Key].image.rectTransform.sizeDelta = (Vector2.right * diff.y + Vector2.one) * (cellSideLength + innerGrid.spacing.x);

                }
                else if (diff.y == 0)
                {
                    //same column == horizontal

                    frames[indices.Key].image.rectTransform.sizeDelta = (Vector2.up * diff.x + Vector2.one) * (cellSideLength + innerGrid.spacing.x);
                }

                Vector2Int sum = (indices.Value[length - 1] + indices.Value[0]);

                frames[indices.Key].image.rectTransform.localPosition = new Vector2(sum.y - columnCount + 1, -sum.x + rowCount - 1) * (cellSideLength + innerGrid.spacing.x) / 2;
            }

            frameParent.SetAsLastSibling();

            neoKeypadComponent.OnButtonClicked = (keypadButtonType, buttonName, holdingString) =>
            {
                StartCoroutine(NeoKeypadButtonClicked());
            };

            currentRow
                .DistinctUntilChanged()
                .Subscribe(currentRowValue =>
                {
                    innerCells[currentRowValue, currentColumn.Value].nKLT.Toggle.isOn = true;
                    //if (currentColumn.Value < columnCount)
                    //{
                    //    if (currentRowValue < rowCount)
                    //    {
                    //    }
                    //}
                });

            currentColumn
                .DistinctUntilChanged()
                .Subscribe(currentColumnValue =>
                {
                    innerCells[currentRow.Value, currentColumnValue].nKLT.Toggle.isOn = true;
                    //if (currentRow.Value < rowCount)
                    //{
                    //    if (currentColumnValue < columnCount)
                    //    {
                    //    }
                    //}
                });

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                uDLRArrowButtons[index]
                    .OnPointerDownAsObservable()
                    .Subscribe(_ => MoveCursor((MoveDirection)index));
            }

            Refresh();

            currentRow.Value = 0;
            currentRow.Value = 0;
        }

        private void MoveCursor(MoveDirection moveDirection)
        {
            switch (moveDirection)
            {
                case MoveDirection.Up:
                    if (currentRow.Value > 0)
                    {
                        currentRow.Value--;
                    }
                    break;

                case MoveDirection.Down:
                    if (currentRow.Value < rowCount - 1)
                    {
                        currentRow.Value++;
                    }
                    break;

                case MoveDirection.Left:
                    if (currentColumn.Value > 0)
                    {
                        currentColumn.Value--;
                    }
                    break;

                case MoveDirection.Right:
                    if (currentColumn.Value < columnCount - 1)
                    {
                        currentColumn.Value++;
                    }
                    break;

                default:
                    break;
            }
        }

        public override void Refresh()
        {
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    if (innerCells[i, j] != null)
                    {
                        innerCells[i, j].Refresh(map[i][j]);
                    }
                }

                rowOuterCells[i].Refresh();
            }

            for (int j = 0; j < columnCount; j++)
            {
                colOuterCells[j].Refresh();
            }

            StartCoroutine(NeoKeypadButtonClicked());
        }

        private IEnumerator NeoKeypadButtonClicked()
        {
            yield return new WaitForEndOfFrame();

            IsCorrect = true;

            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    char mapKey = map[i][j];

                    if (mapKey != 'x')
                    {
                        innerCells[i, j].nKLT.Label.text = mapKey.ToString();
                    }

                    innerCells[i, j].UpdateInfoByText();
                }
            }

            //for (int i = 0; i < rowCount; i++)
            //{
            //    int row = i;

            //    for (int j = 0; j < columnCount; j++)
            //    {
            //        int col = j;

            //        if (innerCells[row, col] != null && innerCells[row, col].number > 0)
            //        {
            //            bool error = false;

            //            if (i > 0)
            //            {
            //                if (innerCells[row - 1, col] != null && innerCells[row - 1, col].number > 0)
            //                {
            //                    error = true;
            //                    innerCells[row - 1, col].cellState.Value = CellState.Error;
            //                }


            //                if (j > 0 && innerCells[row - 1, col - 1] != null && innerCells[row - 1, col - 1].number > 0)
            //                {
            //                    error = true;
            //                    innerCells[row - 1, col + 1].cellState.Value = CellState.Error;
            //                }
            //            }

            //            if (i < rowCount - 1)
            //            {
            //                if (innerCells[row + 1, col] != null && innerCells[row + 1, col].number > 0)
            //                {
            //                    error = true;
            //                    innerCells[row + 1, col].cellState.Value = CellState.Error;
            //                }

            //                if (j < columnCount - 1 && innerCells[row + 1, col + 1] != null && innerCells[row + 1, col + 1].number > 0)
            //                {
            //                    error = true;
            //                    innerCells[row + 1, col + 1].cellState.Value = CellState.Error;
            //                }
            //            }

            //            if (j > 0)
            //            {
            //                if (innerCells[row, col - 1] != null && innerCells[row, col - 1].number > 0)
            //                {
            //                    error = true;
            //                    innerCells[row, col - 1].cellState.Value = CellState.Error;
            //                }

            //                if (i < rowCount - 1 && innerCells[row + 1, col - 1] != null && innerCells[row + 1, col - 1].number > 0)
            //                {
            //                    error = true;
            //                    innerCells[row + 1, col - 1].cellState.Value = CellState.Error;
            //                }
            //            }

            //            if (j < columnCount - 1)
            //            {
            //                if (innerCells[row, col + 1] != null && innerCells[row, col + 1].number > 0)
            //                {
            //                    error = true;
            //                    innerCells[row, col + 1].cellState.Value = CellState.Error;
            //                }

            //                if (i > 0 && innerCells[row - 1, col + 1] != null && innerCells[row - 1, col + 1].number > 0)
            //                {
            //                    error = true;
            //                    innerCells[row - 1, col + 1].cellState.Value = CellState.Error;
            //                }
            //            }

            //            if (error)
            //            {
            //                innerCells[row, col].cellState.Value = CellState.Error;
            //                IsCorrect = false;
            //            }
            //        }
            //    }
            //}


            for (int i = 0; i < rowCount; i++)
            {
                rowOuterCells[i].UpdateInfoByText();

                if (IsCorrect && rowOuterCells[i].state.Value != CellState.Correct)
                {
                    IsCorrect = false;
                }
            }

            for (int j = 0; j < columnCount; j++)
            {
                colOuterCells[j].UpdateInfoByText();

                if (IsCorrect && colOuterCells[j].state.Value != CellState.Correct)
                {
                    IsCorrect = false;
                }
            }

            foreach (var frame in frames)
            {
                frame.Value.UpdateInfoByText();

                if (frame.Value.state.Value != CellState.Correct)
                {
                    IsCorrect = false;
                }
            }
        }
    }
}