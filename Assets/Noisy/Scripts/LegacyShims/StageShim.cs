using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CMS.Template.UI.Stage
{
    public enum Stage
    {
        Stage1 = 0,
        Stage2 = 1,
        Stage3 = 2,
        Stage4 = 3,
        Stage5 = 4,
        Stage6 = 5,
    }

    public enum ReadyCompleteFailState
    {
        Ready = 0,
        Complete = 1,
        Fail = 2,
    }

    public abstract class StageOrganizer : MonoBehaviour
    {
        [SerializeField] protected Button checkCorrectButton;
        [SerializeField] protected Stage stage;
        [SerializeField] protected Transform stampPos;
        [SerializeField] protected CMS.Template.UI.Stamp.StampManager stampManager;

        public abstract void Ready();
        public abstract void Refresh();

        public virtual void Init() { }
        public virtual void SetStage(Stage targetStage) { stage = targetStage; }
        public virtual void CheckCorrectButtonSubscribe() { }

        protected virtual void StageClear() { }
        protected virtual void StageFail() { }
        protected virtual void StageClear(int stageIndex, string password = "") { }
        protected virtual void StageFail(int stageIndex) { }
    }

    public abstract class StageBase : MonoBehaviour
    {
        public virtual void Ready() { }
        public virtual void Refresh() { }
    }
}

namespace CMS.WeeklyGame.Stage
{
    public abstract class StageOrganizer : CMS.Template.UI.Stage.StageOrganizer
    {
    }
}
