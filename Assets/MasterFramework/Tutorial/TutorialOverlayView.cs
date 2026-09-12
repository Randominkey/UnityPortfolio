using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace MasterFramework.Tutorial
{
    public class TutorialOverlayView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Text dialogueText;
        [SerializeField] private Button overlayNextButton;
        [SerializeField] private RectTransform bubbleRect;
        
        [Header("Highlight Mask")]
        [SerializeField] private Image highlightMaskImage; // Screen mask (e.g. cutout shader or simple panel)
        [SerializeField] private RectTransform highlightOutlineRect;

        private void Awake()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (highlightOutlineRect != null) highlightOutlineRect.gameObject.SetActive(false);
        }

        public void ShowDialogue(string text, Vector2 screenPos)
        {
            if (dialoguePanel == null || dialogueText == null) return;

            dialogueText.text = text;
            dialoguePanel.SetActive(true);

            if (bubbleRect != null)
            {
                if (screenPos == Vector2.zero)
                {
                    // Center the bubble if no target is specified
                    bubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
                    bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
                    bubbleRect.pivot = new Vector2(0.5f, 0.5f);
                    bubbleRect.anchoredPosition = Vector2.zero;
                }
                else
                {
                    // Place near target with an offset (e.g. above target)
                    bubbleRect.anchorMin = Vector2.zero;
                    bubbleRect.anchorMax = Vector2.zero;
                    bubbleRect.pivot = new Vector2(0.5f, 0f);
                    
                    // Convert screen position to local position in parent canvas if needed, 
                    // or set anchoredPosition directly if Canvas is ScreenSpace-Overlay
                    bubbleRect.anchoredPosition = screenPos + new Vector2(0, 100);
                }
            }
        }

        public void HighlightTarget(Transform target, Vector2 screenPos)
        {
            if (highlightOutlineRect == null) return;

            highlightOutlineRect.gameObject.SetActive(true);
            highlightOutlineRect.anchorMin = Vector2.zero;
            highlightOutlineRect.anchorMax = Vector2.zero;
            highlightOutlineRect.pivot = new Vector2(0.5f, 0.5f);
            highlightOutlineRect.anchoredPosition = screenPos;

            // Automatically scale outline to match target if target is UGUI RectTransform
            var targetRect = target.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                highlightOutlineRect.sizeDelta = targetRect.sizeDelta + new Vector2(20, 20);
            }
            else
            {
                highlightOutlineRect.sizeDelta = new Vector2(120, 120);
            }
        }

        public void ClearHighlight()
        {
            if (highlightOutlineRect != null)
            {
                highlightOutlineRect.gameObject.SetActive(false);
            }
        }

        public void Hide()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            ClearHighlight();
        }

        public async UniTask WaitNextButtonClickAsync(CancellationToken cancellationToken)
        {
            if (overlayNextButton == null)
            {
                // Fallback to delay if no button is assigned
                await UniTask.Delay(2000, cancellationToken: cancellationToken);
                return;
            }

            overlayNextButton.gameObject.SetActive(true);
            await overlayNextButton.OnClickAsObservable()
                .First()
                .ToUniTask(cancellationToken: cancellationToken);
        }
    }
}
