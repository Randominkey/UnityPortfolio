using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MasterFramework.Interaction;
using MasterFramework.Tutorial;

namespace MasterFramework.Playable.Month10High
{
    public class PlayableRectSquareFrame : MonoBehaviour, IInteractable, IPointerEnterHandler, IPointerExitHandler, ILockableElement
    {
        public string ElementId => UniqueId;
        public bool IsLocked { get; set; } = false;

        [SerializeField] private Button frameButton;
        [SerializeField] private GameObject xObject;

        public string UniqueId { get; private set; }
        public Transform Transform => transform;

        private readonly ReactiveProperty<InteractionState> _currentState = new ReactiveProperty<InteractionState>(InteractionState.Idle);
        public IReadOnlyReactiveProperty<InteractionState> CurrentState => _currentState;

        public bool IsMovable { get; set; } = false;
        public bool IsRotatable { get; set; } = false;
        public bool IsCollidable { get; set; } = false;
        public bool IsDeletable { get; set; } = true;

        public PlayableRectSquareCell StartCell { get; private set; }
        public PlayableRectSquareCell EndCell { get; set; }

        public readonly ReactiveProperty<ObjectState> State = new ReactiveProperty<ObjectState>(ObjectState.Ready);
        public Action<PlayableRectSquareFrame> OnDestroyCallback;

        public bool NeedUpdate { get; set; } = false;
        public bool IsSetted { get; private set; } = false;

        private Vector3 _lastStartPos;
        private Vector3 _lastEndPos;
        private Vector2 _lastCellSize;


        public void Ready(PlayableRectSquareCell startCell)
        {
            UniqueId = Guid.NewGuid().ToString();
            StartCell = startCell;

            if (frameButton == null)
                frameButton = GetComponent<Button>();

            frameButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    if (IsLocked) return;
                    if (IsDeletable)
                    {
                        Destroy(gameObject);
                    }
                })
                .AddTo(gameObject);

            frameButton.image.color = PlayableRectSquareCell.StateColors[0];
            frameButton.image.raycastTarget = false;

            State.DistinctUntilChanged()
                .Subscribe(changedState =>
                {
                    frameButton.image.color = PlayableRectSquareCell.StateColors[(int)changedState];
                })
                .AddTo(gameObject);

            if (xObject != null)
                xObject.SetActive(false);
        }

        private void Update()
        {
            if (StartCell != null)
            {
                Vector3 currentStartPos = StartCell.RectTransform.position;
                Vector3 currentEndPos = EndCell != null ? EndCell.RectTransform.position : currentStartPos;
                Vector2 currentCellSize = StartCell.RectTransform.rect.size;

                if (NeedUpdate || currentStartPos != _lastStartPos || currentEndPos != _lastEndPos || currentCellSize != _lastCellSize)
                {
                    if (EndCell == null)
                    {
                        frameButton.image.rectTransform.sizeDelta = currentCellSize;
                        frameButton.image.rectTransform.position = currentStartPos;
                    }
                    else
                    {
                        Vector2 diff = new Vector2(
                            Mathf.Abs(StartCell.ColumnIndex - EndCell.ColumnIndex),
                            Mathf.Abs(StartCell.RowIndex - EndCell.RowIndex)
                        );

                        frameButton.image.rectTransform.sizeDelta = (diff + Vector2.one) * currentCellSize.x + diff * 4;
                        frameButton.image.rectTransform.position = (currentStartPos + currentEndPos) / 2f;
                    }

                    _lastStartPos = currentStartPos;
                    _lastEndPos = currentEndPos;
                    _lastCellSize = currentCellSize;
                    NeedUpdate = false;
                }
            }
        }

        public void FixAndSet()
        {
            frameButton.image.raycastTarget = true;
            IsSetted = true;
        }

        public void OnPointerDown(Vector3 clickPosition) { }
        public void OnDrag(Vector3 newPosition) { }
        public void OnPointerUp() { }
        public void Rotate(float angle) { }
        public void Flip(bool horizontal) { }
        public void SnapTo(Vector3 snapPosition) { }

        private void OnDestroy()
        {
            OnDestroyCallback?.Invoke(this);
        }

        public void SetActiveXObject(bool active)
        {
            if (xObject != null && IsSetted && IsDeletable)
            {
                xObject.SetActive(active);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsLocked) return;
            SetActiveXObject(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetActiveXObject(false);
        }
    }
}
