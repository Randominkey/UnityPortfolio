using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Lighthouse
{
    public enum CellType
    {
        Blank,
        O,
        X,
        Number,
    }

    public enum CellState
    {
        Ready,
        Correct,
        Error,
    }
    public class LighthouseCell : MonoBehaviour
    {
        public Button button;
        [SerializeField] private Image image;
        [SerializeField] private Sprite[] sprites;
        [SerializeField] private Text numberText;
        [HideInInspector] public ReactiveProperty<CellType> cellType = new ReactiveProperty<CellType>();
        [HideInInspector] public ReactiveProperty<CellState> cellState = new ReactiveProperty<CellState>();
        private int number;
        private List<LighthouseCell> observingTargetCells;

        static Color32 readyColor = new Color32(63, 107, 159, 225);
        static Color32 correctColor = new Color32(78, 206, 129, 225);
        static Color32 errorColor = new Color32(255, 108, 134, 225);

        public void Ready(int number)
        {
            this.number = number;

            if (number < 0)
            {
                button.interactable = true;

                button
                    .OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        cellType.Value = (CellType)(((int)cellType.Value + 1) % 3);
                    });

                numberText.text = "";

                cellType
                    .DistinctUntilChanged()
                    .Subscribe(type =>
                    {
                        image.sprite = sprites[(int)type];
                    });

                cellType.Value = CellType.Blank;
                cellState.Value = CellState.Correct;
            }
            else
            {
                button.interactable = false;
                numberText.text = number.ToString();

                cellType.Value = CellType.Number;

                cellState
                    .DistinctUntilChanged()
                    .Subscribe(state =>
                    {
                        switch (state)
                        {
                            case CellState.Ready:
                                numberText.color = readyColor;
                                break;
                            case CellState.Correct:
                                numberText.color = correctColor;
                                break;
                            case CellState.Error:
                                numberText.color = errorColor;
                                break;
                            default:
                                break;
                        }
                    });
            }
        }

        public void SetObservingTargetCellForNumberCell(List<LighthouseCell> observingTargetCells)
        {
            this.observingTargetCells = observingTargetCells;

            int count = observingTargetCells.Count;

            for (int i = 0; i < count; i++)
            {
                int index = i;
                observingTargetCells[index].cellType
                    .DistinctUntilChanged()
                    .Subscribe(type =>
                    {
                        Check();
                    });
            }
        }

        private void Check()
        {
            int lighthouseCount = 0;

            int count = observingTargetCells.Count;

            for (int i = 0; i < count; i++)
            {
                if (observingTargetCells[i].cellType.Value == CellType.O)
                {
                    lighthouseCount++;
                }
            }

            if (number > lighthouseCount)
            {
                //ready
                cellState.Value = CellState.Ready;
            }
            else if (number == lighthouseCount)
            {
                //correct
                cellState.Value = CellState.Correct;
            }
            else
            {
                //error
                cellState.Value = CellState.Error;
            }

        }

        public void Refresh()
        {
            if (number < 0)
            {
                cellType.Value = CellType.Blank;
            }
        }
    }
}