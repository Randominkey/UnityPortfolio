using CMS.Util.ScreenInfo;
using CMS.Util.UI;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace CMS.WeeklyGame.PersonalDrawingComponent
{
    public class ButtonBoxComponent : MonoBehaviour
    {
        [SerializeField] private float cameraDistance;

        private Vector3 startDragOffset;
        private bool startDragging = false;

        void Start()
        {
            foreach (Transform child in transform)
            {
                Button button = child.GetComponent<Button>();
                Toggle toggle = child.GetComponent<Toggle>();

                try
                {
                    if (button && button.image != null) button.image.alphaHitTestMinimumThreshold = 0.1f;
                    if (toggle && toggle.image != null) toggle.image.alphaHitTestMinimumThreshold = 0.1f;
                }
                catch (System.InvalidOperationException ex)
                {
                    Debug.LogWarning($"[ButtonBoxComponent] AlphaHitTest setting failed: {ex.Message}");
                }
            }

            Image image = GetComponent<Image>();
            RectTransform parent = transform.parent.GetComponent<RectTransform>();

            image.OnPointerDownAsObservable()
                .Subscribe(_ =>
                {
                    startDragging = true;
                    Vector3 mousePos = ScreenUtils.CurrentSceneCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));

                    startDragOffset = transform.position - mousePos;
                })
                .AddTo(image.gameObject);

            image.OnDragAsObservable()
                .Subscribe(_ =>
                {
                    if (startDragging)
                    {
                        Vector3 mousePos = ScreenUtils.CurrentSceneCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));

                        transform.position = mousePos + startDragOffset;

                        Vector2 thisSize = image.rectTransform.sizeDelta;
                        Vector3[] parentCorners = new Vector3[4];
                        parent.GetLocalCorners(parentCorners);

                        Vector3 newPosition = transform.localPosition;

                        if (newPosition.x < parentCorners[0].x + thisSize.x * 0.5f)
                            newPosition.x = parentCorners[0].x + thisSize.x * 0.5f;
                        else if (newPosition.x > parentCorners[2].x - thisSize.x * 0.5f)
                            newPosition.x = parentCorners[2].x - thisSize.x * 0.5f;

                        if (newPosition.y < parentCorners[0].y + thisSize.y * 0.5f)
                            newPosition.y = parentCorners[0].y + thisSize.y * 0.5f;
                        else if (newPosition.y > parentCorners[2].y - thisSize.y * 0.5f)
                            newPosition.y = parentCorners[2].y - thisSize.y * 0.5f;

                        transform.localPosition = newPosition;
                    }
                })
                .AddTo(image.gameObject);

            image.OnPointerUpAsObservable()
                .Subscribe(_ =>
                {
                    startDragging = false;
                })
                .AddTo(image.gameObject);
        }
    }
}