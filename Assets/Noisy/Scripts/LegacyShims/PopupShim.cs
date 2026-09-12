using System;
using UnityEngine;

namespace CMS.Template.UI.Popup
{
    public enum PopupType
    {
        None = 0,
        Win = 1,
        Lose = 2,
        Draw = 3,
        Rule = 4,
        Clear = 5,
        Fail = 6,
    }

    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance;
        public virtual void Show(PopupType type) { }
        public virtual void Hide() { }
        public virtual void Hide(PopupType type) { }
    }

    public class PopupBase : MonoBehaviour
    {
        public virtual void Show() { }
        public virtual void Hide() { }
    }
}
