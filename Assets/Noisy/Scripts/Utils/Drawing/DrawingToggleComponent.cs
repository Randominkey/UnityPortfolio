using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMS.Util.Drawing
{
    using UnityEngine.UI;
    public class DrawingToggleComponent : MonoBehaviour
    {
        [field : SerializeField] public DrawingManager.Type DrawingType { get; private set; }
        [field : SerializeField] public Toggle TargetToggle { get; private set; }
    }

}
