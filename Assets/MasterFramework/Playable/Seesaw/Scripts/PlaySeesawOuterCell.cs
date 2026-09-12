using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PlaySeesaw
{
    public enum CellState
    {
        Ready,
        Correct,
        Error,
        //OverFlow,
    }

    public class PlaySeesawOuterCell : MonoBehaviour
    {
        public PlaySeesawInnerCell[] observingInnerCells;
        public ReactiveProperty<CellState> state = new ReactiveProperty<CellState>();
        public int number;
        public Text text;
        //public Image backgroundCircleForCountCell;

        static Color32 readyColor = new Color32(108, 118, 120, 255);
        static Color32 correctColor = new Color32(0, 153, 202, 255);
        static Color32 errorColor = new Color32(246, 99, 124, 255);

        public void Ready(char key)
        {
            if (key == 'x')
            {
                text.text = "";
                number = -1;
            }
            else
            {
                text.text = key.ToString();
                number = int.Parse(key.ToString());
            }

            state
                .DistinctUntilChanged()
                .Subscribe(state =>
                {
                    switch (state)
                    {
                        case CellState.Ready:
                            text.color = readyColor;
                            //backgroundCircleForCountCell.color = readyColor;
                            break;
                        case CellState.Correct:
                            text.color = correctColor;
                            //backgroundCircleForCountCell.color = correctColor;
                            break;
                        case CellState.Error:
                            text.color = errorColor;
                            //backgroundCircleForCountCell.color = errorColor;
                            break;
                        default:
                            break;
                    }
                });

            state.Value = CellState.Ready;
        }

        public void Refresh()
        {
            UpdateInfoByText();
        }

        public void UpdateInfoByText()
        {
            if (number == -1)
            {
                //blank
                state.Value = CellState.Correct;
            }
            else
            {
                int tempNumber = 0;

                for (int i = 0; i < observingInnerCells.Length; i++)
                {
                    if (observingInnerCells[i] != null)
                    {
                        int cellNumber = observingInnerCells[i].number;

                        if (cellNumber > -1 && cellNumber < 4)
                        {
                            tempNumber += cellNumber;
                        }
                    }
                }

                if (tempNumber == number)
                {
                    state.Value = CellState.Correct;
                }
                else if (tempNumber > number)
                {
                    state.Value = CellState.Error;
                }
                else
                {
                    state.Value = CellState.Ready;
                }
            }
        }
    }
}