using System;
using UnityEngine;

namespace CMS.Template.UI.Stamp
{
    public class StampManager : MonoBehaviour
    {
        public StampControl GetStampControl() => new StampControl();
    }

    public class StampControl
    {
        public enum Mode
        {
            CompleteOrFail,
            Score,
        }

        public enum Status
        {
            WinOrComplete,
            LoseOrFail,
        }

        public enum StampingPosition
        {
            Center = 0,
            TopLeft = 1,
            TopRight = 2,
            BottomLeft = 3,
            BottomRight = 4,
            Custom = 5,
        }

        public void StampShow(Mode mode, Status status, Transform pos = null) { }
        public void StampShow(Mode mode, Status status, Vector3 pos) { }
        public void StampShow(Mode mode, Status status, StampingPosition pos) { }
        public void StampHide() { }
    }
}
