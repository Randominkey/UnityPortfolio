using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using DG.Tweening;

namespace MasterFramework.Navigation
{
    /// <summary>
    /// 게임 규칙 및 가이드 안내용 공용 글래스모피즘 모달 팝업
    /// </summary>
    public class UniversalRuleModal : MonoBehaviour
    {
        [SerializeField] private RectTransform modalContainer;
        [SerializeField] private Image overlayBackground;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Image ruleIllustrationImage;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button overlayButton;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        private void Awake()
        {
            BindButtons();
        }

        public void BindButtons()
        {
            _disposables.Clear();

            if (closeButton != null)
            {
                closeButton.OnClickAsObservable()
                    .Subscribe(_ => Hide())
                    .AddTo(_disposables);
            }

            if (overlayButton != null)
            {
                overlayButton.OnClickAsObservable()
                    .Subscribe(_ => Hide())
                    .AddTo(_disposables);
            }
        }

        public void Show(string gameTitle, string ruleDescription, Sprite ruleSprite = null)
        {
            BindButtons();
            gameObject.SetActive(true);

            var defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (titleText != null)
            {
                titleText.font = defaultFont;
                titleText.text = $"📖 {gameTitle} - 게임 규칙";
            }

            if (descriptionText != null)
            {
                descriptionText.font = defaultFont;
                descriptionText.text = !string.IsNullOrEmpty(ruleDescription) 
                    ? ruleDescription 
                    : "각 스테이지의 목표 조건을 만족하도록 보드를 조작해 퍼즐을 완성하세요.";
            }

            if (ruleIllustrationImage != null)
            {
                if (ruleSprite != null)
                {
                    ruleIllustrationImage.gameObject.SetActive(true);
                    ruleIllustrationImage.sprite = ruleSprite;
                    ruleIllustrationImage.preserveAspect = true;
                }
                else
                {
                    ruleIllustrationImage.gameObject.SetActive(false);
                }
            }

            // Animate Modal Pop In
            if (modalContainer != null)
            {
                modalContainer.DOKill();
                modalContainer.localScale = Vector3.one * 0.8f;
                modalContainer.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }

            if (overlayBackground != null)
            {
                overlayBackground.DOKill();
                overlayBackground.color = new Color(0, 0, 0, 0);
                overlayBackground.DOFade(0.7f, 0.25f);
            }
        }

        public void Hide()
        {
            if (modalContainer != null)
            {
                modalContainer.DOKill();
                modalContainer.DOScale(0.85f, 0.15f).SetEase(Ease.InQuad);
            }

            if (overlayBackground != null)
            {
                overlayBackground.DOKill();
                overlayBackground.DOFade(0f, 0.15f).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}
