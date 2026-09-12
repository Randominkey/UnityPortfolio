using CMS.Core.UI.Base;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace CMS.Template.UI.Keypad
{

    [Serializable]
    public class KeypadComponent : UIRootBase
    {
        private enum KeypadInputMode
        {
            removeFromForward,
            disable,
            removeFromBackward,

        };


        //[SerializeField] RectTransform keypadFullBoardRectTransform;

        [SerializeField] RectTransform myRectTransform;

        [Header("Screen Area")]
        [SerializeField] RectTransform keypadScreenAreaRectTransform;
        [SerializeField] Text keypadScreenText;

        [Header("Keypad Button Area")]
        [SerializeField] Transform keypadButtonArea;
        [SerializeField] RectTransform keypadButtonAreaRectTransform;
        [SerializeField] GridLayoutGroup keypadButtonAreaGridLayoutGroup;
        [Header("Auto Listing")]
        /// <summary> Auto Created List </summary>
        [SerializeField] List<KeypadButtonComponent> keypadButtons = new List<KeypadButtonComponent>();

        //[SerializeField] private Button keypadCloseButton;

        [SerializeField] bool doesSceenExist;
        [field: SerializeField] public int limitDigit;
        [SerializeField] KeypadInputMode keypadInputMode;
        private Vector3 initializePosition;
        bool isOpen;

        [Header("holdingString initial setting")]
        [SerializeField] string holdingString;

        [field: SerializeField] public bool IsAutoSettingScreenLayout { get; private set; } = true;




        public delegate void OnKeypadButtonClicked(string pressedKeyContent, string holdingString);
        public OnKeypadButtonClicked OnButtonClicked { get; set; }
        public Action CloseKeypadCallback { get; set; }

        public ReactiveProperty<string> pressedKeyContent;

        public bool isOpenAtStart = false;

        public void SetHoldingString(string input)
        {
            holdingString = input;
            keypadScreenText.text = input;
        }

        public void Initialize()
        {
            initializePosition = keypadButtonArea.localPosition;

            if (IsAutoSettingScreenLayout)
                ScreenSetting();

            keypadScreenText.text = holdingString;

            //SetCloseButton();
            SetButtonList();

            //버튼 자동 맵핑

            //keypadFullBoardRectTransform
            //    .DOScale(0, 0)
            //    .SetEase(Ease.Linear)
            //    .Play()
            //    .OnComplete(() =>
            //    {
            //    });
            myRectTransform = GetComponent<RectTransform>();

            if (!isOpenAtStart)
                myRectTransform.localScale = Vector3.zero;

            myRectTransform.gameObject.SetActive(true);

            isOpen = false;

            if (pressedKeyContent == null)
                pressedKeyContent = new ReactiveProperty<string>("");


            SetButtonObservers();
        }

        public override void Subscribes()
        {
            Initialize();
        }

        public void ScreenSetting()
        {
            //screen area calculate
            int constraintCount = keypadButtonAreaGridLayoutGroup.constraintCount;
            Vector2 cellSize = keypadButtonAreaGridLayoutGroup.cellSize;
            Vector2 spacing = keypadButtonAreaGridLayoutGroup.spacing;
            RectOffset padding = keypadButtonAreaGridLayoutGroup.padding;
            float leftPlusRight = padding.left + padding.right;

            if (doesSceenExist)
            {
                padding.top = 138;

                if (keypadButtonAreaGridLayoutGroup.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
                {
                    //keypadFullBoardRectTransform.sizeDelta
                    //    = new Vector2(constraintCount * cellSize.x + (constraintCount - 1) * spacing.x + leftPlusRight, 92);
                    keypadScreenAreaRectTransform.sizeDelta
                        = new Vector2(constraintCount * cellSize.x + (constraintCount - 1) * spacing.x + leftPlusRight - 65, 88);
                    //keypadScreenAreaRectTransform.localPosition = new Vector3.zero;
                }
                else if (keypadButtonAreaGridLayoutGroup.constraint == GridLayoutGroup.Constraint.FixedRowCount)
                {
                    //keypadFullBoardRectTransform.sizeDelta
                    //    = new Vector2(keypadButtonArea.childCount / constraintCount * cellSize.x + (keypadButtonArea.childCount / constraintCount - 1) * spacing.x + leftPlusRight, 92);
                    keypadScreenAreaRectTransform.sizeDelta
                        = new Vector2(keypadButtonArea.childCount / constraintCount * cellSize.x + (keypadButtonArea.childCount / constraintCount - 1) * spacing.x + leftPlusRight - 65, 88);
                }

                keypadScreenAreaRectTransform.gameObject.SetActive(true);
            }
            else
            {
                padding.top = 30;
                keypadScreenAreaRectTransform.gameObject.SetActive(false);
            }
        }

        //public void SetCloseButton()
        //{
        //    RegisterUIDisposable(keypadCloseButton
        //        .OnClickAsObservable()
        //        .Subscribe(_ => {
        //            CloseKeypad();
        //        })
        //        );
        //}

        public void SetButtonList()
        {
            //keypadButtons.Clear();
            //for (int i = 0; i < keypadButtonArea.childCount; i++)
            //{
            //    keypadButtons.Add(keypadButtonArea.GetChild(i).GetComponent<KeypadButtonComponent>());
            //}
            keypadButtons = keypadButtonArea.GetComponentsInChildren<KeypadButtonComponent>().ToList();
        }

        public List<KeypadButtonComponent> GetButtonList() => keypadButtons;


        void SetButtonObservers()
        {
            if (keypadButtons.Count > 0)
            {
                foreach (KeypadButtonComponent button in keypadButtons)
                {
                    RegisterUIDisposable(button
                        .GetButton()
                        .OnClickAsObservable()
                        .Select(_ => button.GetContent())
                        .Subscribe(content =>
                        {
                            switch (content)
                            {
                                case "all clear":
                                    holdingString = "";
                                    break;

                                case "backspace":
                                    if (holdingString.Length > 0)
                                    {
                                        holdingString = holdingString.Remove(holdingString.Length - 1);
                                    }
                                    break;

                                case "space":
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

                                //for customized contents if you will

                                default: //for type: String
                                    if (holdingString.Length + content.Length <= limitDigit)
                                    {
                                        holdingString += content;
                                    }
                                    else
                                    {
                                        switch (keypadInputMode)
                                        {
                                            case KeypadInputMode.removeFromForward:
                                                string temp = holdingString + content;
                                                holdingString = "";
                                                for (int i = temp.Length - limitDigit; i < temp.Length; i++)
                                                {
                                                    holdingString += temp[i];
                                                }
                                                break;
                                            case KeypadInputMode.disable:
                                                break;
                                            case KeypadInputMode.removeFromBackward:
                                                if (limitDigit - content.Length >= 0)
                                                {
                                                    temp = holdingString.Remove(limitDigit - content.Length);
                                                    holdingString = temp + content;
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    break;
                            }

                            pressedKeyContent.Value = content;

                            keypadScreenText.text = holdingString;

                            OnButtonClicked?.Invoke(pressedKeyContent.Value, holdingString);

                        }));
                }

            }

        }

        public void OpenOrCloseKeypad()
        {

            if (isOpen)
                CloseKeypad();
            else
                OpenKeypad();
        }

        public void OpenKeypad()
        {
            if (myRectTransform == null)
                myRectTransform = GetComponent<RectTransform>();

            DOTween.Kill(myRectTransform);
            gameObject.SetActive(true);

            myRectTransform
                .DOScale(1, 0.2f)
                .SetEase(Ease.InSine)
                .Play()
                .OnComplete(() =>
                {
                    isOpen = true;
                });
        }

        public void CloseKeypad()
        {
            if (myRectTransform == null)
                myRectTransform = GetComponent<RectTransform>();

            CloseKeypadCallback?.Invoke();

            DOTween.Kill(myRectTransform);
            myRectTransform
                   .DOScale(0, 0.2f)
                   .SetEase(Ease.InSine)
                   .Play()
                   .OnComplete(() =>
                   {
                       myRectTransform.gameObject.SetActive(false);

                       isOpen = false;
                   });
        }

        public int LimitDigit { get => limitDigit; set => limitDigit = value; }

        public RectTransform MyRectTransform { get => myRectTransform; }


        public override void Show(Action onFinished = null, bool IsRunningAnimation = true)
        {
            base.Show(onFinished, IsRunningAnimation);

            //RootRectTransform
            //    .DOScale(1, 0.5f)
            //    .SetEase(Ease.Linear)
            //    .Play()
            //    .OnComplete(() =>
            //    {

            //        onFinished?.Invoke();
            //    });
        }

        public override void Hide(Action onFinished = null, bool IsRunningAnimation = true)
        {
            base.Hide(onFinished, IsRunningAnimation);
            //RootRectTransform
            //    .DOScale(0, 0.5f)
            //    .SetEase(Ease.Linear)
            //    .Play()
            //    .OnComplete(() =>
            //    {
            //        Root.enabled = false;
            //        onFinished?.Invoke();
            //    });
        }

        public Vector2 GetSizeOfKeypad()
        {
            return keypadButtonArea.GetComponent<RectTransform>().sizeDelta;
        }

        public void MoveToInitialPosition()
        {
            keypadButtonArea.localPosition = initializePosition;
        }
        public void MovePosition(Vector3 endPostion, float time, Ease ease = Ease.InSine)
        {
            keypadButtonArea.DOMove(endPostion, time).SetEase(ease);
        }
    }
}