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

    public class MoveDraggableObjectComponent : MonoBehaviour
    {
        public static bool canvasRenderModeIsOverlay = true;
        public static float cameraDistance = 100f;
        public static Camera mainCamera;

        private List<IDisposable> uiDisposables { get; set; } = new List<IDisposable>();

        private RectTransform Target { get; set; }
        private RectTransform myRectTransform { get; set; }

        private Action OnEndDragCallback { get; set; }
        private Action OnBeginDragCallback { get; set; }
        private Action OnDraggingCallback { get; set; }

        private Vector2 CurrentBeginDragMousePos { get; set; }

        [SerializeField] float alphaHitThreshold = 0;

        public MoveDraggableObjectComponent Ready(Action onDragging, 
                                                 Action onBeginDrag = null, 
                                                 Action onEndDrag = null,
                                                 bool isUsingDragRotate = true)
        {
            Target = this.transform.parent.GetComponent<RectTransform>();
            myRectTransform = this.GetComponent<RectTransform>();

            if (!isUsingDragRotate)
                Target = myRectTransform;

            OnEndDragCallback = onEndDrag;
            OnBeginDragCallback = onBeginDrag;
            OnDraggingCallback = onDragging;

            SubscribeEvents();

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
            Vector3 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));

            CurrentBeginDragMousePos = mousePos;
            GameObject gameObject = EventSystem.current.currentSelectedGameObject;
            OnBeginDragCallback?.Invoke();
        }

        private void OnDrag(PointerEventData eventData)
        {
            Vector3 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));

            Vector2 currentMousePos = mousePos;
            Vector2 diff = currentMousePos - CurrentBeginDragMousePos;

            if (Target != null)
            {
                Vector3 newPos = Target.position + new Vector3(diff.x, diff.y, 0);

                Target.position = newPos;

                //if (!Target.IsRectTransformInsideScreen())
                //{
                //    Target.position = oldPos;
                //}
                CurrentBeginDragMousePos = currentMousePos;
            }

            OnDraggingCallback?.Invoke();
        }

        private void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragCallback?.Invoke();
        }
    }

}

