using System;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MasterFramework.Core;

namespace MasterFramework.Navigation
{
    /// <summary>
    /// 게임 클리어 및 정답/오답 피드백을 총괄하는 공용 오버레이 서비스
    /// </summary>
    public class UniversalFeedbackOverlay : MonoBehaviour, IFeedbackStampService
    {
        [Header("Stamp Elements")]
        [SerializeField] private RectTransform stampContainer;
        [SerializeField] private Image stampImage;
        [SerializeField] private Text stampText;
        [SerializeField] private Text stampSubText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Visual Colors")]
        [SerializeField] private Color32 successColor = new Color32(34, 197, 94, 255); // Emerald Green
        [SerializeField] private Color32 failColor = new Color32(239, 68, 68, 255);    // Red

        public Action<StampType> OnStampShown { get; set; }

        private Font _defaultFont;

        private void Awake()
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        public async UniTask ShowStampAsync(StampType type)
        {
            gameObject.SetActive(true);
            OnStampShown?.Invoke(type);

            if (stampText != null)
            {
                stampText.font = _defaultFont;
                stampText.text = type == StampType.Complete ? "STAGE CLEAR!" : "TRY AGAIN!";
                stampText.color = type == StampType.Complete ? successColor : failColor;
            }

            if (stampSubText != null)
            {
                stampSubText.font = _defaultFont;
                stampSubText.text = type == StampType.Complete ? "PERFECT SOLUTION ★★★" : "Check the rule and retry";
                stampSubText.color = Color.white;
            }

            if (stampImage != null)
            {
                stampImage.color = type == StampType.Complete 
                    ? new Color32(20, 83, 45, 230) 
                    : new Color32(127, 29, 29, 230);
            }

            // Punch & Rotation Animation
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.2f);
            }

            if (stampContainer != null)
            {
                stampContainer.localScale = Vector3.one * 1.5f;
                stampContainer.localRotation = Quaternion.Euler(0, 0, type == StampType.Complete ? -8f : 8f);

                stampContainer.DOScale(1f, 0.35f).SetEase(Ease.OutBounce);
                stampContainer.DORotate(Vector3.zero, 0.35f).SetEase(Ease.OutBack);
            }

            // 대기 후 자동 페이드 아웃
            await UniTask.Delay(TimeSpan.FromSeconds(1.4f));

            if (canvasGroup != null)
            {
                await canvasGroup.DOFade(0f, 0.25f).AsyncWaitForCompletion();
            }

            gameObject.SetActive(false);
        }

        public void HideStamp()
        {
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
            }
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 룰 검증 실패 시 화면 또는 오브젝트를 흔드는 쉐이크 연출
        /// </summary>
        public void PlayShakeEffect(Transform targetTransform = null)
        {
            Transform t = targetTransform != null ? targetTransform : transform;
            t.DOKill();
            t.DOShakePosition(0.35f, new Vector3(15f, 0, 0), 14, 90f, false, true);
        }
    }
}
