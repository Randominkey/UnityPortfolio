using UnityEngine;
using UniRx;
using System;

namespace MasterFramework.Interaction
{
    public class DragDropSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Camera eventCamera;
        [SerializeField] private LayerMask interactableLayer;
        
        // Optional grid system for snap rules
        private IGridSystem _activeGridSystem;
        private IInteractable _selectedInteractable;
        private Vector3 _dragOffset;
        private Vector3 _originalPosition;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public IObservable<IInteractable> OnPieceSelected => _onPieceSelected;
        private readonly Subject<IInteractable> _onPieceSelected = new Subject<IInteractable>();

        public IObservable<IInteractable> OnPieceDropped => _onPieceDropped;
        private readonly Subject<IInteractable> _onPieceDropped = new Subject<IInteractable>();

        private void Start()
        {
            if (eventCamera == null)
            {
                eventCamera = Camera.main;
            }

            // Monitor update loops for input checks via UniRx (eliminates giant Update method)
            Observable.EveryUpdate()
                .Subscribe(_ => HandleMouseInput())
                .AddTo(_disposables);
        }

        public void RegisterGridSystem(IGridSystem gridSystem)
        {
            _activeGridSystem = gridSystem;
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                TrySelectPiece();
            }
            else if (Input.GetMouseButton(0) && _selectedInteractable != null)
            {
                DragPiece();
            }
            else if (Input.GetMouseButtonUp(0) && _selectedInteractable != null)
            {
                DropPiece();
            }
        }

        private void TrySelectPiece()
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, Mathf.Infinity, interactableLayer);

            if (hit.collider != null)
            {
                var interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null && interactable.IsMovable)
                {
                    _selectedInteractable = interactable;
                    _originalPosition = _selectedInteractable.Transform.position;
                    _dragOffset = mouseWorldPos - _originalPosition;
                    
                    _selectedInteractable.OnPointerDown(mouseWorldPos);
                    _onPieceSelected.OnNext(_selectedInteractable);
                    
                    // Release any cells previously occupied by this piece before dragging
                    _activeGridSystem?.ReleaseCells(_selectedInteractable);
                }
            }
        }

        private void DragPiece()
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector3 targetPosition = mouseWorldPos - _dragOffset;
            
            // Basic drag movement
            _selectedInteractable.OnDrag(targetPosition);
        }

        private void DropPiece()
        {
            Vector3 finalPosition = _selectedInteractable.Transform.position;
            bool snapSuccess = false;

            if (_activeGridSystem != null)
            {
                // Try snapping
                if (_activeGridSystem.TryGetSnapPosition(finalPosition, out Vector3 snappedPos))
                {
                    // Check if placement is valid (not occupied / inside boundary)
                    if (!_activeGridSystem.IsOccupiedOrInvalid(_selectedInteractable, snappedPos))
                    {
                        _selectedInteractable.SnapTo(snappedPos);
                        _activeGridSystem.OccupyCells(_selectedInteractable, snappedPos);
                        snapSuccess = true;
                        Debug.Log($"[DragDropSystem] Piece snapped successfully at {snappedPos}");
                    }
                }
            }

            if (!snapSuccess)
            {
                // Return to original position if snap failed or grid doesn't exist
                _selectedInteractable.SnapTo(_originalPosition);
                _activeGridSystem?.OccupyCells(_selectedInteractable, _originalPosition);
                Debug.Log("[DragDropSystem] Snapping failed. Returned piece to original position.");
            }

            _selectedInteractable.OnPointerUp();
            _onPieceDropped.OnNext(_selectedInteractable);
            _selectedInteractable = null;
        }

        private Vector3 GetMouseWorldPosition()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Mathf.Abs(eventCamera.transform.position.z);
            return eventCamera.ScreenToWorldPoint(mousePos);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
            _onPieceSelected.Dispose();
            _onPieceDropped.Dispose();
        }
    }
}
