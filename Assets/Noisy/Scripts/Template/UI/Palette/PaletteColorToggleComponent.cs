using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CMS.Template.UI.Palette
{
    using UnityEngine.UI;

    public class PaletteColorToggleComponent : MonoBehaviour
    {
        [SerializeField] private Image targetToggleBackgroundImage;
        [SerializeField] private Toggle targetToggle;

        public void SetColor(Color32 color) => targetToggleBackgroundImage.color = color;
        public Color32 GetColor() => targetToggleBackgroundImage.color;

        public Toggle GetTarget() => targetToggle;
    }
}
