using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CMS.WeeklyGame.Presenter
{
    public abstract class PresenterBase : MonoBehaviour
    {
        [SerializeField] protected CMS.Template.UI.OptionalTab.OptionalTabManager tabManager;
        [SerializeField] protected Transform rulePopup;
        [SerializeField] protected Button ruleButton;
        [SerializeField] protected int stage;
        [SerializeField] protected List<RawImage> rawImages = new List<RawImage>();
        [SerializeField] protected CMS.Template.UI.Stage.StageOrganizer currentStageOrganizer;
        [SerializeField] protected List<CMS.Template.UI.Stage.StageOrganizer> stageOrganizers = new List<CMS.Template.UI.Stage.StageOrganizer>();
        [SerializeField] protected CMS.Template.UI.Stamp.StampManager stampManager;

        protected virtual void Start() { }
        protected virtual void Initialize() { }
        protected virtual void OnDestroy() { }
        public virtual void Refresh() { }

        protected virtual void NeoTabSetting() { }
        protected virtual void NeoOnStageChanged(int stage) { }
        protected virtual void NeoClearData() { }
        protected virtual void NeoSubscribes() { }
        protected virtual void CheckCorrectButtonSubscribe() { }

        protected virtual void StageClearCall() { }
        protected virtual void StageFailCall() { }
        protected virtual void StageClearCall(int stageIndex, string password = "") { }
        protected virtual void StageFailCall(int stageIndex) { }
    }
}

namespace CMS.Template.UI.Presenter
{
    public abstract class PresenterBase : CMS.WeeklyGame.Presenter.PresenterBase
    {
    }
}
