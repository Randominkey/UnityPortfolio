using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PlaySeesaw
{
    public class PlaySeesawFrame : MonoBehaviour
    {
        public List<PlaySeesawInnerCell> observingInnerCells;
        public ReactiveProperty<CellState> state = new ReactiveProperty<CellState>();
        public Image image;

        static Color32 readyColor = new Color32(70, 129, 96, 255);
        static Color32 correctColor = new Color32(0, 173, 227, 255);
        static Color32 errorColor = new Color32(255, 129, 151, 255);

        public void Ready()
        {
            observingInnerCells = new List<PlaySeesawInnerCell>();

            state
                .DistinctUntilChanged()
                .Subscribe(state =>
                {
                    switch (state)
                    {
                        case CellState.Ready:
                            image.color = readyColor;
                            break;
                        case CellState.Correct:
                            image.color = correctColor;
                            break;
                        case CellState.Error:
                            image.color = errorColor;
                            break;
                        default:
                            break;
                    }
                });

            UpdateInfoByText();
        }

        public void AddObservingCell(PlaySeesawInnerCell target)
        {
            observingInnerCells.Add(target);
        }

        public void UpdateInfoByText()
        {
            int length = observingInnerCells.Count;

            int leverIndex = -1;

            for (int i = 0; i < length; i++)
            {
                if (observingInnerCells[i].number == 4)
                {
                    if (leverIndex == -1)
                    {
                        leverIndex = i;
                    }
                    else
                    {
                        leverIndex = length;
                    }
                }
            }

            //no lever
            if (leverIndex == -1)
            {
                state.Value = CellState.Ready;
            }
            //tip lever, more than 1 lever error
            else if (leverIndex == 0 || leverIndex == length - 1 || leverIndex == length)
            {
                state.Value = CellState.Error;
            }
            else
            {
                int prevWeightSum = 0;
                int nextWeightSum = 0;
                bool emptyCellExist = false;


                for (int i = leverIndex - 1; i > -1; i--)
                {
                    int number = observingInnerCells[i].number;

                    prevWeightSum += (leverIndex - i) * number;

                    if (number == -1)
                    {
                        emptyCellExist = true;
                        break;
                    }
                }

                for (int i = leverIndex + 1; i < length; i++)
                {
                    int number = observingInnerCells[i].number;

                    nextWeightSum += (i - leverIndex) * number;

                    if (number == -1)
                    {
                        emptyCellExist = true;
                        break;
                    }
                }

                if (emptyCellExist)
                {
                    state.Value = CellState.Ready;
                }
                else
                {
                    if (prevWeightSum == nextWeightSum)
                    {
                        state.Value = CellState.Correct;
                    }
                    else
                    {
                        state.Value = CellState.Error;
                    }
                }
            }
        }
    }
}