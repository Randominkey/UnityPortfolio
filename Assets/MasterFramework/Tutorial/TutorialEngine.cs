using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UniRx;
using Zenject;

namespace MasterFramework.Tutorial
{
    public class TutorialEngine : MonoBehaviour
    {
        [Inject] private Core.ISoundService _soundService;
        [Inject] private Core.IPopupService _popupService;
        
        [SerializeField] private Camera uiCamera;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private TutorialOverlayView overlayView;
        
        private readonly List<ILockableElement> _registeredElements = new List<ILockableElement>();
        private readonly Dictionary<string, Transform> _registeredTransforms = new Dictionary<string, Transform>();

        public void RegisterElement(ILockableElement element, Transform t)
        {
            if (element == null || t == null) return;
            
            if (!_registeredElements.Contains(element))
            {
                _registeredElements.Add(element);
            }
            _registeredTransforms[element.ElementId] = t;
        }

        public void UnregisterAll()
        {
            _registeredElements.Clear();
            _registeredTransforms.Clear();
        }

        public async UniTask StartTutorialAsync(TutorialSequence sequence, CancellationToken cancellationToken)
        {
            if (sequence == null || overlayView == null) return;

            foreach (var step in sequence.steps)
            {
                await ExecuteStepAsync(step, cancellationToken);
            }
            overlayView.Hide();
        }

        private async UniTask ExecuteStepAsync(TutorialStep step, CancellationToken cancellationToken)
        {
            // 1. 강제 플레이 제어 (Locking)
            if (step.lockAllOtherInteractions)
            {
                foreach (var element in _registeredElements)
                {
                    element.IsLocked = (element.ElementId != step.targetHighlightId);
                }
            }

            // 2. 말풍선 정렬 & 하이라이팅 (WorldToScreen Point 변환)
            if (!string.IsNullOrEmpty(step.targetHighlightId) && _registeredTransforms.TryGetValue(step.targetHighlightId, out var target))
            {
                Vector2 screenPos = GetScreenPosition(target);
                overlayView.HighlightTarget(target, screenPos);
                overlayView.ShowDialogue(step.dialogueText, screenPos);
            }
            else
            {
                overlayView.ClearHighlight();
                overlayView.ShowDialogue(step.dialogueText, Vector2.zero); // 화면 중앙 노출
            }

            // 3. 자체 비동기 트윈 연출 구동
            List<UniTask> activeTweens = new List<UniTask>();
            foreach (var anim in step.animations)
            {
                if (_registeredTransforms.TryGetValue(anim.targetId, out var animTarget))
                {
                    activeTweens.Add(PlayTweenAsync(animTarget, anim, cancellationToken));
                }
            }

            // 4. 완료 조건 분기 및 비동기 대기
            if (step.endTriggerType == TutorialActionType.WaitUserInteraction)
            {
                // MessageBroker를 통한 특정 이벤트 및 피스 ID 검증 구독 대기
                await MessageBroker.Default.Receive<PiecePlacedEvent>()
                    .Where(evt => evt.PieceId == step.expectedEventPieceId)
                    .First()
                    .ToUniTask(cancellationToken: cancellationToken);
            }
            else
            {
                // 화면 빈곳이나 다음 버튼 클릭 대기
                await overlayView.WaitNextButtonClickAsync(cancellationToken);
            }

            // 5. 정리 (트윈 완료 대기 및 잠금 해제)
            await UniTask.WhenAll(activeTweens).SuppressCancellationThrow();
            
            foreach (var element in _registeredElements)
            {
                element.IsLocked = false; // 기본 복원
            }
        }

        private Vector2 GetScreenPosition(Transform target)
        {
            bool isUI = target.GetComponent<RectTransform>() != null;
            Camera targetCam = isUI ? uiCamera : worldCamera;
            
            if (targetCam == null)
            {
                targetCam = Camera.main;
            }

            if (targetCam == null)
            {
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }
            
            Vector3 screenPos = targetCam.WorldToScreenPoint(target.position);
            return new Vector2(screenPos.x, screenPos.y);
        }

        private async UniTask PlayTweenAsync(Transform target, TweenAnimationData anim, CancellationToken cancellationToken)
        {
            if (anim.delay > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(anim.delay), cancellationToken: cancellationToken);
            }

            Vector3 startVal = Vector3.zero;
            if (anim.propertyType == TweenPropertyType.Position) startVal = target.position;
            else if (anim.propertyType == TweenPropertyType.Scale) startVal = target.localScale;
            else if (anim.propertyType == TweenPropertyType.Rotation) startVal = target.eulerAngles;

            float elapsed = 0f;
            while (elapsed < anim.duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / anim.duration);
                t = ApplyEase(t, anim.ease);

                if (target == null) return;

                if (anim.propertyType == TweenPropertyType.Position)
                {
                    target.position = Vector3.Lerp(startVal, anim.targetValue, t);
                }
                else if (anim.propertyType == TweenPropertyType.Scale)
                {
                    target.localScale = Vector3.Lerp(startVal, anim.targetValue, t);
                }
                else if (anim.propertyType == TweenPropertyType.Rotation)
                {
                    target.eulerAngles = Vector3.Lerp(startVal, anim.targetValue, t);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            
            if (target == null) return;
            if (anim.propertyType == TweenPropertyType.Position) target.position = anim.targetValue;
            else if (anim.propertyType == TweenPropertyType.Scale) target.localScale = anim.targetValue;
            else if (anim.propertyType == TweenPropertyType.Rotation) target.eulerAngles = anim.targetValue;
        }

        private float ApplyEase(float t, EaseType ease)
        {
            switch (ease)
            {
                case EaseType.InQuad:
                    return t * t;
                case EaseType.OutQuad:
                    return t * (2f - t);
                case EaseType.InOutQuad:
                    return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
                case EaseType.InCubic:
                    return t * t * t;
                case EaseType.OutCubic:
                    return (--t) * t * t + 1f;
                case EaseType.InOutCubic:
                    return t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f;
                case EaseType.Linear:
                default:
                    return t;
            }
        }
    }
}
