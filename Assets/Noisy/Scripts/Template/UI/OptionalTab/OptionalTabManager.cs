using System.Collections.Generic;
using UnityEngine;

namespace CMS.Template.UI.OptionalTab
{
    using CMS.Core.UI.Base;
    using CMS.Template.UI.Keypad;
    using CMS.Template.UI.Palette;
    using CMS.Util.Drawing;
    using CMS.WeeklyGame;
    using CMS.WeeklyGame.PersonalDrawingComponent;
    using DG.Tweening;
    using System;
    using System.Linq;
    using UniRx;
    using UnityEngine.UI;

    public class OptionalTabManager : UIRootBase
    {
        public enum UIType
        {
            Drawing,
            Palette,
            Foundation,

            KeypadButton,
            SaveButton,
            GalleryButton,
            RetryButton,
        }        

        private bool IsShowing { get; set; }

        
        [Space]
        [Header("[ Tab toggles ]")]
        [SerializeField] private ToggleGroup tabToggleGroup;
        [SerializeField] private List<OptionalTabToggleComponent> tabToggles;

        [Space]
        [Header("[ Managers ]")]
        [SerializeField] private PaletteManager paletteManager;
        [SerializeField] private KeypadManager keypadManager;
        [SerializeField] private DrawingManager drawingManager;
        [SerializeField] private PersonalDrawingComponent drawingComponent;

        [Space]
        [Header("Other Buttons")]
        [SerializeField] private Button retryButton;
        public Action OnRetry { get; set; }

        [SerializeField] private Button saveButton;
        public Action OnSave { get; set; }

        [SerializeField] private Button galleryButton;
        public Action OnOpenGallery { get; set; }

        [SerializeField] private Button keypadButton;

        [Space]
        [Header("Others")]
        [SerializeField] private RectTransform tabBackground;

        // public BlueMonsterGuide ILearningGuide;

        //public BlueMonsterGuide ILearningGuide
        //{
        //    get
        //    {
        //        if (iLearningGuide == null)
        //            iLearningGuide = transform.GetComponentsInChildrenForEveryActive<BlueMonsterGuide>().FirstOrDefault();

        //        return iLearningGuide;
        //    }
        //}

        public override void Subscribes()
        {
            ClearUIDisposable();

            if (!RootRectTransform)
                base.RefreshTargetRootComponentValues();

            RootRectTransform
                .DOAnchorPosX(tabBackground.rect.width, 0.3f)
                .Play();
            
            SubscribeToggles();
            SubscribeButtons();

            if (paletteManager)
                paletteManager.Init();
        }

        #region Getter
        public PaletteManager GetPaletteManager() => paletteManager;
        public KeypadManager GetKeypadManager() => keypadManager;
        public DrawingManager GetDrawingManager() => drawingManager;
        #endregion

        public void OpenTab(UIType type)
        {
            OptionalTabToggleComponent curTabComp = tabToggles.FirstOrDefault(o => o.uiType == type);

            if (curTabComp != null && curTabComp.toggle.isActiveAndEnabled)
            {
                curTabComp.toggle.isOn = true;
            }
            else
            {
                Debug.LogWarning($"not active toggle.... target name is <color=red><b> [ {type} ] </b></color>");
            }
        }

        public void AllIsActiveTab(bool isActive)
        {
            foreach (OptionalTabToggleComponent toggleComponent in tabToggles)
            {
                toggleComponent.toggle.gameObject.SetActive(isActive);
            }
            keypadButton.interactable = isActive;

            if (!RootRectTransform)
                base.RefreshTargetRootComponentValues();

            RootRectTransform
                .DOLocalMoveX(100, 0.3f)
                .SetEase(Ease.InOutSine)
                .Play();
        }
        public void IsActiveTab(UIType type, bool isActive)
        {
            switch (type)
            {
                case UIType.KeypadButton:
                    {
                        keypadButton.interactable = isActive;
                    }
                    break;
                case UIType.GalleryButton:
                    {
                        galleryButton.interactable = isActive;
                    }
                    break;
                case UIType.SaveButton:
                    {
                        saveButton.interactable = isActive;
                    }
                    break;
                case UIType.Drawing:
                case UIType.Palette:
                case UIType.Foundation:
                    {
                        OptionalTabToggleComponent curTabComp = tabToggles.FirstOrDefault(o => o.uiType == type);

                        if (curTabComp != null)
                        {
                            curTabComp.toggle.gameObject.SetActive(isActive);
                            curTabComp.buttonsRoot.gameObject.SetActive(isActive);

                            curTabComp.toggle.isOn = isActive;
                        }

                        if (isActive)
                        {
                            RootRectTransform
                                .DOAnchorPosX(0, 0.3f)
                                .Play();
                        }
                    }
                    break;

                case UIType.RetryButton:
                    {
                        retryButton.gameObject.SetActive(isActive);
                        break;
                    }
            }
        }

        private void SubscribeButtons()
        {
            RegisterUIDisposable(retryButton
                .OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(2))
                .Subscribe(_ =>
                {
                    OnRetry?.Invoke();
                }));

            RegisterUIDisposable(saveButton
                .OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(2))
                .Subscribe(_ =>
                {
                    OnSave?.Invoke();
                }));

            RegisterUIDisposable(galleryButton
                .OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(2))
                .Subscribe(_ =>
                {
                    OnOpenGallery?.Invoke();
                }));

            RegisterUIDisposable(keypadButton
                .OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(2))
                .Subscribe(_ =>
                {
                    keypadManager.Show();
                }));
        }

        private void SubscribeToggles()
        {
            IObservable<IEnumerable<OptionalTabToggleComponent>> isOnObservable = tabToggleGroup
                .ObserveEveryValueChanged(changedToggleGroup => tabToggleGroup.ActiveToggles().FirstOrDefault())
                .Select(selectedToggle => tabToggles.Where(o => o.toggle.isOn));

            IObservable<IEnumerable<OptionalTabToggleComponent>> isDisableOnObservable = tabToggleGroup
                .ObserveEveryValueChanged(changedToggleGroup => tabToggleGroup.ActiveToggles().FirstOrDefault())
                .Select(selectedToggle => tabToggles.Where(o => !o.toggle.isOn));

            RegisterUIDisposable(isOnObservable
                .Subscribe(isOnToggleEnumeration =>
                {
                    OptionalTabToggleComponent curOnToggleComponent = isOnToggleEnumeration.FirstOrDefault();

                    if (curOnToggleComponent)
                    {
                        List<OptionalTabToggleComponent> otherToggleComponents = tabToggles.Where(o => !o.toggle.isOn).ToList();
                        foreach (OptionalTabToggleComponent other in otherToggleComponents)
                            other.buttonsRoot.SetActive(false);
                        curOnToggleComponent.buttonsRoot.SetActive(true);

                        if (!IsShowing)
                        {
                            IsShowing = true;

                            DOTween.Kill(RootRectTransform);
                            RootRectTransform
                            .DOAnchorPosX(0, 0.3f)
                            .SetEase(Ease.InOutSine)
                            .Play();
                        }
                    }
                }));

            RegisterUIDisposable(isDisableOnObservable
                .Subscribe(isOnToggleEnumeration =>
                {
                    OptionalTabToggleComponent curOnToggleComponent = isOnToggleEnumeration.FirstOrDefault();

                    if (curOnToggleComponent)
                    {
                        List<OptionalTabToggleComponent> currentIsOnToggles = tabToggles.Where(o => o.toggle.isOn).ToList();
                        if (currentIsOnToggles.Count == 0)
                        {
                            IsShowing = false;
                            DOTween.Kill(RootRectTransform);
                            RootRectTransform
                             .DOAnchorPosX(100, 0.3f)
                             .SetEase(Ease.InOutSine)
                             .Play();

                        }
                    }
                }));
        }

        public PersonalDrawingComponent GetPersonalDrawingComponent() => drawingComponent;

        public override void Show(Action onFinished = null, bool IsRunningAnimation = true)
        {
            base.Show(onFinished, IsRunningAnimation);

            IsShowing = true;

            RootRectTransform
                .DOLocalMoveX(0, 0.3f)
                .SetEase(Ease.InOutSine)
                .Play();
        }
        public override void Hide(Action onFinished = null, bool IsRunningAnimation = true)
        {
            base.Hide(onFinished, IsRunningAnimation);

            IsShowing = false;

            RootRectTransform
                .DOLocalMoveX(100, 0.3f)
                .SetEase(Ease.InOutSine)
                .Play();
        }

    }

}

