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

    //[RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
    public class MoveDraggableObjectVelocityComponent : MonoBehaviour
    {
        private List<IDisposable> uiDisposables { get; set; } = new List<IDisposable>();

        private RectTransform MyRectTransform { get; set; }

        private Rigidbody2D TargetRigidBody { get; set; }

        private Action OnEndDragCallback { get; set; }
        private Action OnBeginDragCallback { get; set; }
        private Action OnDraggingCallback { get; set; }

        private Vector2 ClickedOffset { get; set; }

        private bool IsDragging { get; set; }

        public MoveDraggableObjectVelocityComponent Ready(Action onDragging,
                                                         Action onBeginDrag = null,
                                                         Action onEndDrag = null,
                                                         bool isUsingDragRotate = true)
        {
            MyRectTransform = this.GetComponent<RectTransform>();

            if (isUsingDragRotate)
                TargetRigidBody = this.transform.parent.GetComponent<Rigidbody2D>();
            else
                TargetRigidBody = this.GetComponent<Rigidbody2D>();

            if (TargetRigidBody == null)
            {
                bool prevActiveParentObject = this.transform.parent.gameObject.activeInHierarchy;
                this.transform.parent.gameObject.SetActive(true);

                if (isUsingDragRotate)
                    TargetRigidBody = this.transform.parent.GetComponent<Rigidbody2D>();
                else
                    TargetRigidBody = this.GetComponent<Rigidbody2D>();

                this.transform.parent.gameObject.SetActive(prevActiveParentObject);
            }

            OnEndDragCallback = onEndDrag;
            OnBeginDragCallback = onBeginDrag;
            OnDraggingCallback = onDragging;

            SubscribeEvents();

            return this;
        }

        public RectTransform GetMyRectTransform() => MyRectTransform;

        private void SubscribeEvents()
        {
            if (uiDisposables != null)
            {
                foreach (IDisposable disposable in uiDisposables)
                    disposable.Dispose();
            }

            uiDisposables = new List<IDisposable>();

            uiDisposables.Add(Observable
                .EveryUpdate()
                .Select(_ => ChungdamScaler.CurrentMousePosition)
                .Subscribe(currentMousePosition =>
                {
                    if (IsDragging)
                    {
                        TargetRigidBody.bodyType = RigidbodyType2D.Dynamic;
                        TargetRigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;

                        //TargetRigidBody.MovePosition(currentMousePosition);
                        Vector2 direction = new Vector2(currentMousePosition.x, currentMousePosition.y) - MyRectTransform.GetWorldSpaceRect().center - ClickedOffset;

                        TargetRigidBody.linearVelocity = direction.magnitude * 4.5f < 300 ? direction * 4.5f : direction.normalized * 4.5f * 300;
                    }
                }));

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
            ClickedOffset = eventData.position - MyRectTransform.GetWorldSpaceRect().center;

            GameObject gameObject = EventSystem.current.currentSelectedGameObject;
            OnBeginDragCallback?.Invoke();

            IsDragging = true;
        }

        private void OnDrag(PointerEventData eventData)
        {
            //if (TargetRigidBody)
            //{
            //    //Debug.Log("d");

            //    //TargetRigidBody.bodyType = RigidbodyType2D.Dynamic;
            //    //TargetRigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;

            //    //Vector2 ditection = eventData.position - MyRectTransform.GetWorldSpaceRect().center - ClickedOffset;

            //    ////TargetRigidBody.MovePosition(eventData.position);
            //    //TargetRigidBody.velocity = ditection * 3f;//eventData.position.magnitude < 1f ? eventData.position * 300f : eventData.position.normalized * 300f;
            //}    
        }

        private void OnEndDrag(PointerEventData eventData)
        {
            IsDragging = false;
            ClickedOffset = Vector2.zero;
            OnEndDragCallback?.Invoke();
            if (TargetRigidBody)
            {
                TargetRigidBody.linearVelocity = Vector2.zero;
                TargetRigidBody.bodyType = RigidbodyType2D.Static;
                TargetRigidBody.constraints = RigidbodyConstraints2D.FreezeAll;

            }
        }
    }

}
