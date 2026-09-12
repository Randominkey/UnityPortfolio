using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;

using UniRx;
using UniRx.Triggers;

using DG.Tweening;
using UnityEngine.UI;

using CMS.Util.MoveNRotate;
using System.Linq;

namespace CMS.Template.UI.Keypad
{
    [RequireComponent(typeof(GridLayoutGroup))]
    [Serializable]
    public class NeoKeypadComponent : MonoBehaviour
    {
        private enum KeypadInputMode
        {
            removeFromForward,
            disable,
            removeFromBackward,

        };
        [Header("Essential component which need mapping")]
        [SerializeField] RectTransform screenRectTransform;

        [Header("Keypad Property Setting")]
        [SerializeField] KeypadInputMode keypadInputMode;
        [SerializeField] string holdingString;
        public int limitDigit;
        [SerializeField] bool doesSceenExist;
        [SerializeField] bool startWithOpen = false;

        [Space]
        [Header("Grid Setting")]
        [SerializeField] RectOffset padding;
        [SerializeField] Vector2 cellSize;
        [SerializeField] Vector2 spacing;
        [SerializeField] GridLayoutGroup.Corner startCorner;
        [SerializeField] GridLayoutGroup.Axis startAxis;
        [SerializeField] TextAnchor childAlignment;
        [SerializeField] GridLayoutGroup.Constraint constraint;
        [SerializeField] int constraintCount;

        [Space]
        [Header("Auto Mapping Components")]
        public RectTransform rectTransform;
        [SerializeField] GridLayoutGroup gridLayoutGroup;
        [SerializeField] Text screenText;
        [SerializeField] List<NeoKeypadButtonComponent> keypadButtons = new List<NeoKeypadButtonComponent>();

        private Vector3 initializePosition;
        //bool isOpen;

        public delegate void OnKeypadButtonClicked(KeypadButtonType pressedKeyContent, string buttonName, string holdingString);
        public OnKeypadButtonClicked OnButtonClicked { get; set; }
        public Action CloseKeypadCallback { get; set; }
        
        public void SetHoldingString(string input)
        {
            holdingString = input;
            screenText.text = input;
        }

        private void OnValidate()
        {
            rectTransform = GetComponent<RectTransform>();
            initializePosition = rectTransform.localPosition;
            keypadButtons = GetComponentsInChildren<NeoKeypadButtonComponent>().ToList();
            SetGrid();
            screenText.text = holdingString;
        }

        private void Awake()
        {
            Ready();
        }

        public void Ready()
        {
            rectTransform = GetComponent<RectTransform>();

            initializePosition = transform.localPosition;
            
            keypadButtons = GetComponentsInChildren<NeoKeypadButtonComponent>().ToList();
            
            SetGrid();

            screenText.text = holdingString;

            foreach (var keypadButton in keypadButtons)
            {
                keypadButton.SetButton();
            }

            SetButtonObservers();

            if (startWithOpen)
            {
                rectTransform.localScale = Vector3.one;
                //isOpen = true;
            }
            else
            {
                rectTransform.localScale = Vector3.zero;
                //isOpen = false;
            }
            

            rectTransform.gameObject.SetActive(true);

            
        }

        
        public void SetGrid()
        {
            gridLayoutGroup = GetComponent<GridLayoutGroup>();
            //screenAreaRectTransform = GetComponentInChildren<LayoutElement>().GetComponent<RectTransform>();
            screenText = screenRectTransform.GetComponentInChildren<Text>();

            screenRectTransform.gameObject.SetActive(doesSceenExist);

            gridLayoutGroup.padding.top = doesSceenExist ? padding.top + (int)(screenRectTransform.sizeDelta.y + spacing.y) : padding.top;
            gridLayoutGroup.padding.bottom = padding.bottom;
            gridLayoutGroup.padding.left = padding.left;
            gridLayoutGroup.padding.right = padding.right;

            gridLayoutGroup.cellSize = cellSize;
            gridLayoutGroup.spacing = spacing;
            gridLayoutGroup.startCorner = startCorner;
            gridLayoutGroup.startAxis = startAxis;
            gridLayoutGroup.childAlignment = childAlignment;
            gridLayoutGroup.constraint = constraint;
            gridLayoutGroup.constraintCount = constraintCount;

            switch (constraint)
            {
                case GridLayoutGroup.Constraint.Flexible:
                    break;
                case GridLayoutGroup.Constraint.FixedColumnCount:
                    rectTransform.sizeDelta =
                        new Vector2(padding.left + constraintCount * cellSize.x + (constraintCount - 1) * spacing.x + padding.right,
                        gridLayoutGroup.padding.top + (keypadButtons.Count / constraintCount + 1) * cellSize.y + (keypadButtons.Count / constraintCount) * spacing.y + padding.bottom - (keypadButtons.Count % constraintCount == 0 ? cellSize.y + spacing.y: 0));
                    break;
                case GridLayoutGroup.Constraint.FixedRowCount:
                    rectTransform.sizeDelta =
                        new Vector2(padding.left + (keypadButtons.Count / constraintCount + 1) * cellSize.x + (keypadButtons.Count / constraintCount) * spacing.x + padding.right - (keypadButtons.Count % constraintCount == 0 ? cellSize.x + spacing.x : 0),
                        gridLayoutGroup.padding.top + constraintCount * cellSize.y + (constraintCount - 1) * spacing.y + padding.bottom);
                    break;
                default:
                    break;
            }

            screenRectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x - padding.right - padding.left, screenRectTransform.sizeDelta.y);
            screenRectTransform.localPosition = new Vector3(-screenRectTransform.sizeDelta.x / 2, rectTransform.sizeDelta.y / 2 - padding.top, 0);
        }

        void SetButtonObservers()
        {
            if (keypadButtons.Count > 0)
            {
                foreach (var keypadButton in keypadButtons)
                {
                    keypadButton
                        .button
                        .OnClickAsObservable()                        
                        .Subscribe(_ =>
                        {
                            switch (keypadButton.buttonType)
                            {
                                case KeypadButtonType.String:
                                    if (holdingString.Length + keypadButton.name.Length <= limitDigit)
                                    {
                                        holdingString += keypadButton.name;
                                    }
                                    else
                                    {
                                        switch (keypadInputMode)
                                        {
                                            case KeypadInputMode.removeFromForward:
                                                string temp = holdingString + keypadButton.name;
                                                holdingString = "";
                                                for (int i = temp.Length - limitDigit; i < temp.Length; i++)
                                                {
                                                    holdingString += temp[i];
                                                }
                                                break;
                                            case KeypadInputMode.disable:
                                                break;
                                            case KeypadInputMode.removeFromBackward:
                                                if (limitDigit - keypadButton.name.Length >= 0)
                                                {
                                                    temp = holdingString.Remove(limitDigit - keypadButton.name.Length);
                                                    holdingString = temp + keypadButton.name;
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    break;

                                case KeypadButtonType.AllClear:
                                    holdingString = "";
                                    break;

                                case KeypadButtonType.BackSpace:
                                    if (holdingString.Length > 0)
                                        holdingString = holdingString.Remove(holdingString.Length - 1);
                                    break;

                                case KeypadButtonType.Space:
                                    if (holdingString.Length < limitDigit)
                                    {
                                        holdingString += " ";
                                    }
                                    else
                                    {
                                        switch (keypadInputMode)
                                        {
                                            case KeypadInputMode.removeFromForward:
                                                string temp = holdingString + " ";
                                                holdingString = "";
                                                for (int i = 1; i < temp.Length; i++)
                                                {
                                                    holdingString += temp[i];
                                                }
                                                break;
                                            case KeypadInputMode.disable:
                                                break;
                                            case KeypadInputMode.removeFromBackward:
                                                holdingString.Remove(holdingString.Length - 1);
                                                holdingString += " ";
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    break;

                                case KeypadButtonType.Previous:
                                    break;

                                case KeypadButtonType.Next:
                                    break;

                                case KeypadButtonType.CustomizedContents:
                                    break;

                                default:
                                    break;
                            }

                            screenText.text = holdingString;

                            OnButtonClicked?.Invoke(keypadButton.buttonType, keypadButton.name, holdingString);

                        });
                }

            }

        }

        public void OpenKeypad()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            DOTween.Kill(rectTransform);
            gameObject.SetActive(true);

            rectTransform
                .DOScale(1, 0.2f)
                .SetEase(Ease.InSine)
                .Play()
                .OnComplete(() =>
                {
                    //isOpen = true;
                });
        }

        public void CloseKeypad()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            CloseKeypadCallback?.Invoke();

            DOTween.Kill(rectTransform);
            rectTransform
                   .DOScale(0, 0.2f)
                   .SetEase(Ease.InSine)
                   .Play()
                   .OnComplete(() =>
                   {
                       rectTransform.gameObject.SetActive(false);

                       //isOpen = false;
                   });
        }

        public void MoveToInitialPosition()
        {
            transform.localPosition = initializePosition;
        }
        public void MovePosition(Vector3 endPostion, float time, Ease ease = Ease.InSine)
        {
            transform.DOMove(endPostion, time).SetEase(ease);
        }
    }
}