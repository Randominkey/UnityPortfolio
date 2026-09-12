using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using DG.Tweening;

namespace CMS.Core.UI.Base
{
    /// <summary>
    /// Portfolio Clean UIRootBase
    /// 원래의 지저분한 WebGL 브릿지(DllImport)는 스터빙하고, 
    /// UI Canvas/RectTransform 캐싱 및 UniRx CompositeDisposable 기반의 구독 수명주기 관리 로직만 현대화하여 복구한 베이스 클래스입니다.
    /// </summary>
    public abstract class UIRootBase : MonoBehaviour
    {
        protected Canvas Root { get; private set; }
        protected CanvasGroup RootGroup { get; private set; }
        protected RectTransform RootRectTransform { get; private set; }

        // UniRx 표준 CompositeDisposable을 활용하여 메모리 누수를 원천 차단
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public abstract void Subscribes();

        protected void RegisterUIDisposable(IDisposable disposable)
        {
            if (disposable != null)
            {
                _disposables.Add(disposable);
            }
        }

        protected void ClearUIDisposable()
        {
            _disposables.Clear(); // 구독은 해제하되 CompositeDisposable 자체는 재사용 가능하도록 비움
        }

        protected void RefreshTargetRootComponentValues()
        {
            if (!Root)
            {
                bool isPrevActiveState = gameObject.activeInHierarchy;
                gameObject.SetActive(true);

                Root = GetComponent<Canvas>();
                RootRectTransform = GetComponent<RectTransform>();
                RootGroup = GetComponentInParent<CanvasGroup>();

                if (!RootGroup)
                {
                    RootGroup = transform.root.GetComponent<CanvasGroup>();
                }

                gameObject.SetActive(isPrevActiveState);
            }
        }

        private void Start()
        {
            Root = GetComponent<Canvas>();
            RootRectTransform = GetComponent<RectTransform>();
            RootGroup = GetComponentInParent<CanvasGroup>();
            
            if (!RootGroup)
            {
                RootGroup = transform.root.GetComponent<CanvasGroup>();
            }

            Subscribes();
        }

        public void Destroy(bool isChangeTargetParent = false)
        {
            ClearUIDisposable();

            if (isChangeTargetParent && RootRectTransform && RootRectTransform.transform.parent)
            {
                GameObject.Destroy(RootRectTransform.transform.parent.gameObject);
            }
            else if (RootRectTransform && RootRectTransform.gameObject)
            {
                GameObject.Destroy(RootRectTransform.gameObject);
            }
        }

        public virtual void Show(Action onFinished = null, bool IsRunningAnimation = true)
        {
            PreparationShow();
            onFinished?.Invoke();
        }

        public virtual void Hide(Action onFinished = null, bool IsRunningAnimation = true)
        {
            PreparationHide();
            onFinished?.Invoke();
        }

        protected virtual void PreparationHide()
        {
            if (!Root)
            {
                RefreshTargetRootComponentValues();
            }

            if (Root)
            {
                DOTween.Kill(Root);
            }
            if (RootRectTransform)
            {
                DOTween.Kill(RootRectTransform);
            }

            DOTween.CompleteAll();

            // Hide 시점에 모든 UniRx 이벤트 구독을 안전하게 끊어 메모리 누수 방지
            ClearUIDisposable();
        }

        protected virtual void PreparationShow()
        {
            if (!Root)
            {
                RefreshTargetRootComponentValues();
            }

            if (Root)
            {
                DOTween.Kill(Root);
                Root.enabled = true;

                if (!Root.gameObject.activeInHierarchy)
                {
                    Root.gameObject.SetActive(true);
                }
            }
            if (RootRectTransform)
            {
                DOTween.Kill(RootRectTransform);
            }

            Subscribes();
        }

        protected virtual void PreparationShow(bool subscribable)
        {
            if (!Root)
            {
                RefreshTargetRootComponentValues();
            }

            if (Root)
            {
                DOTween.Kill(Root);
                Root.enabled = true;

                if (!Root.gameObject.activeInHierarchy)
                {
                    Root.gameObject.SetActive(true);
                }
            }
            if (RootRectTransform)
            {
                DOTween.Kill(RootRectTransform);
            }
            
            if (subscribable)
            {
                Subscribes();
            }
        }

        #region Legacy WebGL Web Platform API Stubs (저작권 보호 및 에디터 safe 컴파일용 스텁)

        public static void SetStageCount(int stageCount) { }
        public static void SetStageCount(int stageCount, int[] hasStageClearList) { }
        protected void StageClearCall(int stage, string password) { }
        protected void StageFailCall(int stage) { }

        #endregion
    }
}
