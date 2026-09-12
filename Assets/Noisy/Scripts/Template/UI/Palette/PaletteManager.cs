using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CMS.Util.MoveNRotate;

namespace CMS.Template.UI.Palette
{
    using UnityEngine.UI;
    using DG.Tweening;
    using UniRx;
    using System;

    using System.Linq;

    public class PaletteManager : MonoBehaviour
    {
        public ReactiveProperty<Color32> SelectedColor { get; private set; } = new ReactiveProperty<Color32>();

        [Header("Setting colors")]
        [SerializeField] private List<Color32> usingColors = new List<Color32>();

        [Space]
        [Header("Select color objects")]
        [SerializeField] private ToggleGroup colorPaletteTogglesGroup;
        [SerializeField] private PaletteColorToggleComponent colorPaletteToggleBase;
        [SerializeField] private List<PaletteColorToggleComponent> colorPaletteToggles = new List<PaletteColorToggleComponent>();

        public void Init() => SetUsingColors(usingColors.ConvertAll(o => new Color32(o.r, o.g, o.b, 255)));

        public void SetUsingColors(List<Color32> colors)
        {
            usingColors.Clear();
            usingColors = colors;

            foreach (PaletteColorToggleComponent toggle in colorPaletteToggles)
            {
                colorPaletteTogglesGroup.UnregisterToggle(toggle.GetTarget());
                DestroyImmediate(toggle.gameObject);
            }

            colorPaletteToggles.Clear();

            foreach (Color32 color in colors)
            {
                PaletteColorToggleComponent toggleComponent = Instantiate(colorPaletteToggleBase, colorPaletteTogglesGroup.transform);
                toggleComponent.SetColor(color);

                toggleComponent.GetTarget().group = colorPaletteTogglesGroup;

                toggleComponent.transform.GetComponent<RectTransform>().localScale = Vector3.one;

                colorPaletteTogglesGroup.RegisterToggle(toggleComponent.GetTarget());

                colorPaletteToggles.Add(toggleComponent);
            }

            if (colorPaletteToggles.Count > 0)
                colorPaletteToggles[0].GetTarget().isOn = true;

            SubscribesToggleGroups();
        }

        private void SubscribesToggleGroups()
        {
            colorPaletteTogglesGroup
                .ObserveEveryValueChanged(changedToggleGroup => colorPaletteTogglesGroup.ActiveToggles().FirstOrDefault())
                .Select(selectedToggle => colorPaletteToggles.Where(o => o.GetTarget().isOn))
                .Subscribe(isOnToggleEnumeration =>
                {
                    PaletteColorToggleComponent isSelectedColorComp = isOnToggleEnumeration.FirstOrDefault();

                    if (isSelectedColorComp)
                    {
                        SelectedColor.Value = isSelectedColorComp.GetColor();
                    }
                })
                .AddTo(this);
        }
    }
}
