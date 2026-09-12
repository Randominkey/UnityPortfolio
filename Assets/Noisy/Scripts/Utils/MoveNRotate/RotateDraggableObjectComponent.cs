using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMS.Util.MoveNRotate
{
    using System;

    using UniRx;
    using UniRx.Triggers;
    using UnityEngine.EventSystems;

    using CMS.Util.Extend;
    using CMS.Util.UI;
    using UnityEngine.UI;

    public class RotateDraggableObjectComponent : MonoBehaviour
    {
        public static bool canvasRenderModeIsOverlay = true;
        public static float cameraDistance = 100f;
        public static Camera mainCamera;

        private List<IDisposable> uiDisposables { get; set; } = new List<IDisposable>();

        private RectTransform Target { get; set; }

        private Action OnEndDragCallback { get; set; }
        private Action OnBeginDragCallback { get; set; }
        private Action OnDraggingCallback { get; set; }

        private bool IsDragging { get; set; } = false;
        private float rotateVelocity { get; set; }
        private float rotationDamping { get; set; } = 2;

        private float preAngle;
        private RectTransform myRectTransform;
        private RectTransform rotatingTarget;

        private Vector2 firstMousePos;
        private Vector2 worldspacePivot;

        public bool IsBlockEvent { get; set; } = false;

        [SerializeField] float alphaHitThreshold = 0;


        public RotateDraggableObjectComponent Ready(RectTransform rotateTarget,
                                                    Action onDragging,
                                                    Action onBeginDrag = null,
                                                    Action onEndDrag = null,
                                                    RectTransform rotatingTarget = null)
        {
            Target = rotateTarget;
            this.rotatingTarget = rotatingTarget;

            SubscribeEvents();
            SubscribeUpdate();

            if (alphaHitThreshold > 0)
            {
                Image temp = GetComponent<Image>();
                if (temp != null)
                {
                    temp.alphaHitTestMinimumThreshold = alphaHitThreshold;
                }
            }

            return this;
        }

        private void SubscribeUpdate()
        {
            //uiDisposables.Add(Observable
            //    .EveryUpdate()
            //    .Where(_ => !IsDragging && !Mathf.Approximately(rotateVelocity, 0))
            //    .Subscribe(_ =>
            //    {
            //        float deltaVelocity = Mathf.Min(
            //                Mathf.Sign(rotateVelocity) * Time.deltaTime * rotationDamping,
            //                Mathf.Sign(rotateVelocity) * rotateVelocity
            //            );
            //        rotateVelocity -= deltaVelocity;
            //        Target.transform.Rotate(Vector3.back, rotateVelocity, Space.Self);
            //    }));
        }

        private void SubscribeEvents()
        {
            if (uiDisposables != null)
            {
                foreach (IDisposable disposable in uiDisposables)
                    disposable.Dispose();
            }

            uiDisposables = new List<IDisposable>();

            uiDisposables.Add(this
                .gameObject
                .AddComponent<ObservableBeginDragTrigger>()
                .OnBeginDragAsObservable()
                .Subscribe(OnBeginDrag));

            uiDisposables.Add(this
                .gameObject
                .AddComponent<ObservableDragTrigger>()
                .OnDragAsObservable()
                .Subscribe(OnDrag));

            uiDisposables.Add(this
                .gameObject
                .AddComponent<ObservableEndDragTrigger>()
                .OnEndDragAsObservable()
                .Subscribe(OnEndDrag));
        }

        private void OnBeginDrag(PointerEventData eventData)
        {
            Vector3 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));

            if (IsBlockEvent)
                return;

            IsDragging = true;
            preAngle = Target.eulerAngles.z;
            firstMousePos = mousePos;

            if (!myRectTransform)
            {
                myRectTransform = GetComponent<RectTransform>();

            }

            worldspacePivot = myRectTransform.position;

            OnBeginDragCallback?.Invoke();
        }

        private void OnDrag(PointerEventData eventData)
        {
            Vector2 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));
            
            if (IsBlockEvent)
                return;

            if (rotatingTarget != null)
            {
                rotatingTarget.transform.eulerAngles = new Vector3(rotatingTarget.transform.eulerAngles.x, rotatingTarget.transform.eulerAngles.y, -(preAngle + Vector2.SignedAngle(firstMousePos - worldspacePivot, mousePos - worldspacePivot)));
            }
            else
            {
                Target.transform.eulerAngles = new Vector3(Target.transform.eulerAngles.x, Target.transform.eulerAngles.y, preAngle + Vector2.SignedAngle(firstMousePos - worldspacePivot, mousePos - worldspacePivot));
            }

            OnDraggingCallback?.Invoke();
        }

        private void OnEndDrag(PointerEventData eventData)
        {
            if (IsBlockEvent)
                return;

            OnEndDragCallback?.Invoke();
            IsDragging = false;
        }


    }
}


