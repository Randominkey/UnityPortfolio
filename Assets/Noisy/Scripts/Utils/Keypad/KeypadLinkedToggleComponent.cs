using CMS.Core.UI.Base;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace CMS.Template.UI.Keypad
{
    public class KeypadLinkedToggleComponent : UIRootBase
    {
        [SerializeField] KeypadComponent LinkedKeypadComponent;

        [Header("Auto mapping")]
        [SerializeField] Text label;
        [field: SerializeField] public Toggle Toggle { get; set; }

        [Header("Limit digit customize")]
        [SerializeField] bool useMyLimitDigit;
        [SerializeField] int myLimitDigit;
        int originalLimitDigit;

        public string LabelText() => label?.text;

        public void SetLabelText(string text) => label.text = text;

        public override void Subscribes()
        {
            if (!Toggle)
                Toggle = GetComponent<Toggle>();

            if (!Toggle.group)
                Toggle.group = GetComponentInParent<ToggleGroup>();

            if (!label)
                label = GetComponentInChildren<Text>();

            if (!LinkedKeypadComponent)
                Debug.Log("Keypad unlinked");
            else
                originalLimitDigit = LinkedKeypadComponent.limitDigit;

            label.text = "";

            if (LinkedKeypadComponent)
            {
                RegisterUIDisposable(
                    Toggle.OnValueChangedAsObservable()
                    .Subscribe(isOn =>
                    {
                        if (isOn)
                        {
                            LinkedKeypadComponent.SetHoldingString(label.text);
                            LinkedKeypadComponent.OpenKeypad();
                            if (useMyLimitDigit)
                                LinkedKeypadComponent.limitDigit = myLimitDigit;
                            else
                                LinkedKeypadComponent.limitDigit = originalLimitDigit;

                            LinkedKeypadComponent.OnButtonClicked = (pressedKey, holdingString) =>
                            {
                                if (Toggle.isOn)
                                    SetLabelText(holdingString);                                
                            };
                        }
                        else
                        {
                            LinkedKeypadComponent.CloseKeypad();

                            if (!Toggle.group.AnyTogglesOn() && useMyLimitDigit)
                                LinkedKeypadComponent.limitDigit = originalLimitDigit;
                        }
                    })
                );
            }
        }
    }
}