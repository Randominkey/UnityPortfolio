using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMS.Util.MoveNRotate
{
    using System;

    using UniRx;
    using UniRx.Triggers;

    using UnityEngine.UI;
    using UnityEngine.EventSystems;

    using CMS.Util.Extend;
    using CMS.Util.UI;

    public class MoveDraggableObjectComponentForProtractorPen : MonoBehaviour
    {
        public static bool canvasRenderModeIsOverlay = true;
        public static float cameraDistance = 100f;
        public static Camera mainCamera;
        private List<IDisposable> uiDisposables { get; set; } = new List<IDisposable>();

        private RectTransform Target { get; set; }
        private RectTransform myRectTransform { get; set; }
        private RectTransform parentTransform { get; set; }

        private Action OnEndDragCallback { get; set; }
        private Action<RectTransform> OnBeginDragCallback { get; set; }
        private Action<RectTransform> OnDraggingCallback { get; set; }

        private Vector2 CurrentBeginDragMousePos { get; set; }

        public MoveDraggableObjectComponentForProtractorPen Ready(Action<RectTransform> onDragging,
                                                 Action<RectTransform> onBeginDrag = null,
                                                 Action onEndDrag = null,
                                                 bool isUsingDragRotate = true)
        {
            Target = this.transform.parent.GetComponent<RectTransform>();
            myRectTransform = this.GetComponent<RectTransform>();
            parentTransform = this.transform.parent.GetComponent<RectTransform>();

            if (!isUsingDragRotate)
                Target = myRectTransform;

            OnEndDragCallback = onEndDrag;
            OnBeginDragCallback = onBeginDrag;
            OnDraggingCallback = onDragging;

            SubscribeEvents();

            return this;
        }

        public RectTransform GetTarget() => Target;
        public RectTransform GetMyRectTransform() => myRectTransform;

        private void SubscribeEvents()
        {
            if (uiDisposables != null)
            {
                foreach (IDisposable disposable in uiDisposables)
                    disposable.Dispose();
            }

            uiDisposables = new List<IDisposable>();

            ObservableBeginDragTrigger observableBeginDragTrigger = this.gameObject.GetComponent<ObservableBeginDragTrigger>();
            if (observableBeginDragTrigger == null)
            {
                uiDisposables.Add(this
                            .gameObject
                            .AddComponent<ObservableBeginDragTrigger>()
                            .OnBeginDragAsObservable()
                            .Subscribe(OnBeginDrag));
            }
            else
            {
                uiDisposables.Add(observableBeginDragTrigger
                            .OnBeginDragAsObservable()
                            .Subscribe(OnBeginDrag));
            }

            ObservableDragTrigger observableDragTrigger = this.gameObject.GetComponent<ObservableDragTrigger>();
            if (observableDragTrigger == null)
            {
                uiDisposables.Add(this
                            .gameObject
                            .AddComponent<ObservableDragTrigger>()
                            .OnDragAsObservable()
                            .Subscribe(OnDrag));
            }
            else
            {
                uiDisposables.Add(observableDragTrigger
                            .OnDragAsObservable()
                            .Subscribe(OnDrag));
            }

            ObservableEndDragTrigger observableEndDragTrigger = this.gameObject.GetComponent<ObservableEndDragTrigger>();
            if (observableEndDragTrigger == null)
            {
                uiDisposables.Add(this
                            .gameObject
                            .AddComponent<ObservableEndDragTrigger>()
                            .OnEndDragAsObservable()
                            .Subscribe(OnEndDrag));
            }
            else
            {
                uiDisposables.Add(observableEndDragTrigger
                            .OnEndDragAsObservable()
                            .Subscribe(OnEndDrag));
            }
        }

        private void OnBeginDrag(PointerEventData eventData)
        {
            Vector2 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));

            CurrentBeginDragMousePos = mousePos;
            GameObject gameObject = EventSystem.current.currentSelectedGameObject;
            OnBeginDragCallback?.Invoke(myRectTransform);
        }

        private void OnDrag(PointerEventData eventData)
        {
            Vector2 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));

            //Vector2 localMousePos = myRectTransform.InverseTransformPoint(eventData.position);
            Vector2 diff = parentTransform.InverseTransformVector(mousePos - CurrentBeginDragMousePos);

            if (Target != null)
            {
                Vector3 newPos = Target.localPosition + new Vector3(diff.x, diff.y, 0);

                if (newPos.y >= 0)
                {
                    float radius = 256f;
                    float angle = Vector2.SignedAngle(Vector2.right, newPos);

                    Target.localPosition = radius * new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
                    Target.localEulerAngles = new Vector3(0, 0, angle - 90f);
                }

                CurrentBeginDragMousePos = mousePos;
            }

            OnDraggingCallback?.Invoke(myRectTransform);
        }

        private void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragCallback?.Invoke();
        }
    }

}

