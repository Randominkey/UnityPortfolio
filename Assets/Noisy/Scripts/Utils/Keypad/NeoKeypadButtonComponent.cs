using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CMS.Template.UI.Keypad
{
    public enum KeypadButtonType
    {
        String,
        AllClear,
        BackSpace,
        Space,
        Previous,
        Next,
        CustomizedContents,
    }
    public class NeoKeypadButtonComponent : MonoBehaviour
    {
        [Header("Button Property Setting")]
        public KeypadButtonType buttonType;

        [Space]
        [Header("Auto Mapping Components")]
        [SerializeField] private Image image;
        public Button button;
        public Text text;

        [Header("0: String / 1: AllClear / 2: BackSpace / 3: Space / 5: Previous / 6: Next")]
        [SerializeField] private Sprite[] buttonSprites;


        private void OnValidate()
        {
            SetButton();
        }

        public void SetButton()
        {
            image = GetComponent<Image>();
            button = GetComponent<Button>();
            text = GetComponentInChildren<Text>();
            
            switch (buttonType)
            {
                case KeypadButtonType.String:
                    text.text = name;
                    break;

                case KeypadButtonType.AllClear:
                case KeypadButtonType.BackSpace:
                case KeypadButtonType.Space:
                case KeypadButtonType.Previous:
                case KeypadButtonType.Next:
                    text.text = "";
                    break;

                case KeypadButtonType.CustomizedContents:
                    break;
                default:
                    break;
            }

            if (buttonType != KeypadButtonType.CustomizedContents)
                image.sprite = buttonSprites[(int)buttonType];

        }

    }
}