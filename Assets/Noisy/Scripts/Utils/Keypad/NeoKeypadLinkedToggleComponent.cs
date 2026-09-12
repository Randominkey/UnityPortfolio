using CMS.Template.UI.OptionalTab;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using CMS.Core.UI.Base;

namespace CMS.Template.UI.Keypad
{
    public enum LinkMode
    {
        LinkHoldingString,
        LinkPressedButton,
    }

    public class NeoKeypadLinkedToggleComponent : MonoBehaviour
    {
        [Header("Essential component which need mapping")]
        public NeoKeypadComponent LinkedKeypadComponent;

        [Header("Auto mapping")]
        public Text label;
        public string initialLabelText = "";
        public Toggle toggle;
        public NeoKeypadLinkedToggleGroupComponent neoKeypadLinkedToggleGroupComponent;

        [Header("Customize")]
        [SerializeField] bool readyWhenAwake = true;
        [SerializeField] bool useMyLimitDigit;
        [SerializeField] int myLimitDigit;
        int originalLimitDigit;
        [SerializeField] LinkMode linkMode = LinkMode.LinkHoldingString;
        [SerializeField] bool remainKeypadOpenWhenDeselected = false;
        private bool alreadyReadied = false;

        public Text Label
        {
            get
            {
                if (label == null)
                    label = GetComponentInChildren<Text>();
                return label;
            }
        }
        public Toggle Toggle
        {
            get
            {
                if (!toggle)
                    toggle = GetComponent<Toggle>();
                return toggle;
            }
        }

        private void Awake()
        {
            if (readyWhenAwake)
            {
                Ready();
            }
        }

        public void Ready()
        {
            if (!alreadyReadied)
            {

                if (!toggle)
                    toggle = GetComponent<Toggle>();

                if (!toggle.group)
                    toggle.group = GetComponentInParent<ToggleGroup>();

                if (!toggle.group)
                    Debug.Log("Need toggle group");
                else
                {
                    neoKeypadLinkedToggleGroupComponent = toggle.group.GetComponent<NeoKeypadLinkedToggleGroupComponent>();
                    if (neoKeypadLinkedToggleGroupComponent == null)
                    {
                        neoKeypadLinkedToggleGroupComponent = toggle.group.gameObject.AddComponent<NeoKeypadLinkedToggleGroupComponent>();
                    }
                    neoKeypadLinkedToggleGroupComponent.AddNeoKeypadLinkedToggle(this);
                }

                if (!label)
                    label = GetComponentInChildren<Text>();

                if (!LinkedKeypadComponent)
                    Debug.Log("Keypad unlinked");
                else
                    originalLimitDigit = LinkedKeypadComponent.limitDigit;

                label.text = initialLabelText;

                if (LinkedKeypadComponent)
                {
                    toggle.OnValueChangedAsObservable()
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

                                LinkedKeypadComponent.OnButtonClicked += ButtonClick;

                            }
                            else
                            {
                                LinkedKeypadComponent.OnButtonClicked -= ButtonClick;

                                if (!remainKeypadOpenWhenDeselected)
                                    LinkedKeypadComponent.CloseKeypad();

                                if (!toggle.group.AnyTogglesOn() && useMyLimitDigit)
                                    LinkedKeypadComponent.limitDigit = originalLimitDigit;
                            }
                        });
                }


                alreadyReadied = true;
            }
            //throw new System.NotImplementedException();
        }

        private void ButtonClick(KeypadButtonType keyType, string buttonName, string holdingString)
        {
            switch (keyType)
            {
                case KeypadButtonType.Previous:
                case KeypadButtonType.Next:
                    neoKeypadLinkedToggleGroupComponent.PrevOrNext(keyType);
                    break;
                default:
                    if (toggle.isOn)
                    {
                        switch (linkMode)
                        {
                            case LinkMode.LinkHoldingString:
                                label.text = holdingString;
                                break;
                            case LinkMode.LinkPressedButton:
                                if (keyType == KeypadButtonType.String)
                                {
                                    LinkedKeypadComponent.SetHoldingString(buttonName);
                                    label.text = buttonName;
                                }
                                else if (keyType == KeypadButtonType.AllClear)
                                {
                                    LinkedKeypadComponent.SetHoldingString("");
                                    label.text = "";
                                }
                                break;
                            default:
                                break;
                        }

                    }
                    break;
            }
        }
    }
}