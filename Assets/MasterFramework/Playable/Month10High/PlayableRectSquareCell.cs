using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniRx;
using MasterFramework.Tutorial;

namespace MasterFramework.Playable.Month10High
{
    public enum CellSign
    {
        Empty,
        Horizontal,
        Vertical,
        Square
    }

    public enum ObjectState
    {
        Ready,
        Correct,
        Error
    }

    public class PlayableRectSquareCell : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, ILockableElement
    {
        public string ElementId => $"Cell_{RowIndex}_{ColumnIndex}";
        public bool IsLocked { get; set; } = false;

        private RectTransform _rectTransform;
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }

        [SerializeField] private Sprite[] signSprites;
        [SerializeField] private Image signImage;

        public Action<PlayableRectSquareCell> OnPointerDownCallback;
        public Action<PlayableRectSquareCell> OnPointerEnterCallback;

        public int RowIndex { get; private set; }
        public int ColumnIndex { get; private set; }
        public CellSign CellSign { get; private set; }
        public bool IsContained { get; set; }

        public readonly ReactiveProperty<ObjectState> State = new ReactiveProperty<ObjectState>(ObjectState.Ready);

        private void Awake()
        {
            // 부착된 Cell 자체의 Image 외에 자식 오브젝트 중 첫 번째 Image를 signImage로 자동 탐색 매핑
            if (signImage == null)
            {
                var images = GetComponentsInChildren<Image>(true);
                var myImage = GetComponent<Image>();
                foreach (var img in images)
                {
                    if (img != myImage)
                    {
                        signImage = img;
                        break;
                    }
                }
            }
        }

        public static readonly Color[] StateColors = new Color[]
        {
            new Color32(92, 92, 92, 255),    // Ready
            new Color32(0, 188, 248, 255),   // Correct
            new Color32(255, 90, 119, 255)   // Error
        };

        public void Ready(int rowIndex, int columnIndex, int signIndex)
        {
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
            CellSign = (CellSign)signIndex;
            IsContained = false;

            if (signImage != null)
            {
                if (signIndex != 0 && signSprites != null && signIndex - 1 < signSprites.Length)
                {
                    signImage.sprite = signSprites[signIndex - 1];
                    signImage.gameObject.SetActive(true);
                }
                else
                {
                    signImage.gameObject.SetActive(false);
                }

                signImage.color = StateColors[0];

                State.DistinctUntilChanged()
                    .Subscribe(changedState =>
                    {
                        signImage.color = StateColors[(int)changedState];
                    })
                    .AddTo(gameObject);
            }
        }

        public void ResetState()
        {
            State.Value = ObjectState.Ready;
        }

        private Image _cellImage;
        public void SetPresetBackgroundColor(bool isPreset)
        {
            if (_cellImage == null)
                _cellImage = GetComponent<Image>();
            if (_cellImage != null)
            {
                _cellImage.color = isPreset ? new Color32(204, 242, 254, 255) : Color.white;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsLocked) return;
            OnPointerDownCallback?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsLocked) return;
            OnPointerEnterCallback?.Invoke(this);
        }
    }
}
