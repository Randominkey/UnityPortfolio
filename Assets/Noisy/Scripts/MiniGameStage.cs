using CMS.Template.UI.Stage;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace CMS.WeeklyGame
{
    public class MiniGameStage : StageOrganizer
    {
        [SerializeField] string password;
        [SerializeField] private MiniGame.MiniGameBoardBase board;
        [SerializeField] GameObject popupObject;

        public override void Ready()
        {

            board.Ready();



            checkCorrectButton
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    if (board.IsCorrect)
                    {
                        stampManager.GetStampControl().StampShow(Template.UI.Stamp.StampControl.Mode.CompleteOrFail, Template.UI.Stamp.StampControl.Status.WinOrComplete, stampPos);

#if UNITY_EDITOR
#elif UNITY_WEBGL
                        Core.UI.Base.UIRootBase.StageClear((int)stage, password);
#endif
                    }
                    else
                    {
                        stampManager.GetStampControl().StampShow(Template.UI.Stamp.StampControl.Mode.CompleteOrFail, Template.UI.Stamp.StampControl.Status.LoseOrFail, stampPos);
#if UNITY_EDITOR
#elif UNITY_WEBGL
                        Core.UI.Base.UIRootBase.StageFail((int)stage);
#endif
                    }
                });
            popupObject.SetActive(true);
        }

        public override void Refresh()
        {
            board.Refresh();
            popupObject.SetActive(true);
        }

    }
}