using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace MasterFramework.Playable.Month34High
{
    public enum CellType
    {
        Blank = 0,
        Land = 1,
        Sea = 2,
    }

    public enum CellState
    {
        Ready = 0,
        Correct = 1,
        Error = 2,
    }

    public class NumberIslandCell : MonoBehaviour
    {
        public Button button;
        [SerializeField] private Image image;
        [SerializeField] private Sprite[] sprites;
        [SerializeField] private GameObject correctObject;
        [SerializeField] private GameObject errorObject;
        [SerializeField] private Text numberText;

        [HideInInspector] public int number;
        [HideInInspector] public ReactiveProperty<CellType> cellType = new ReactiveProperty<CellType>();
        [HideInInspector] public ReactiveProperty<CellState> cellState = new ReactiveProperty<CellState>();

        private static readonly Color32 readyColor = new Color32(95, 95, 95, 255);
        private static readonly Color32 correctColor = new Color32(0, 119, 255, 255);
        private static readonly Color32 errorColor = new Color32(242, 113, 115, 255);

        public bool containedChecked;
        public int row;
        public int column;

        public Action<NumberIslandCell> OnCellClicked;
        private CompositeDisposable _disposables = new CompositeDisposable();

        public void Ready(int row, int column)
        {
            this.row = row;
            this.column = column;
            this.number = 0;
            this.containedChecked = false;

            _disposables.Clear();

            if (button != null)
            {
                button.interactable = true;
                button.OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        if (number != 0) return; // 고정된 힌트 셀은 변경 불가
                        
                        // Blank -> Land -> Sea -> Blank 순환
                        cellType.Value = (CellType)(((int)cellType.Value + 1) % 3);
                        OnCellClicked?.Invoke(this);
                    })
                    .AddTo(_disposables);
            }

            cellType
                .DistinctUntilChanged()
                .Subscribe(type =>
                {
                    if (image != null && sprites != null && (int)type < sprites.Length)
                    {
                        image.sprite = sprites[(int)type];
                    }
                })
                .AddTo(_disposables);

            cellType.Value = CellType.Blank;

            cellState
                .DistinctUntilChanged()
                .Subscribe(state =>
                {
                    if (correctObject != null) correctObject.SetActive(state == CellState.Correct);
                    if (errorObject != null) errorObject.SetActive(state == CellState.Error);

                    if (numberText != null)
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
                        }
                    }
                })
                .AddTo(_disposables);

            cellState.Value = CellState.Ready;

            if (numberText != null)
            {
                numberText.text = "";
            }
        }

        public void SetAsNumberLand(int number)
        {
            if (button != null) button.interactable = false;

            this.number = number;
            cellType.Value = CellType.Land;

            if (numberText != null)
            {
                numberText.text = number.ToString();
            }
        }

        public void Refresh()
        {
            if (number == 0)
            {
                cellType.Value = CellType.Blank;
            }
            cellState.Value = CellState.Ready;
        }

        public void SetType(CellType type)
        {
            if (number == 0)
            {
                cellType.Value = type;
            }
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}