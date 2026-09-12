using CMS.Core.UI.Base;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace CMS.Template.UI.Keypad
{
    public class KeypadManager : UIRootBase
    {
        public List<KeypadComponent> keypadComponents;
        [SerializeField] private Button keypadButton;

        private int selectedKeypadIndex = 0;

        public KeypadComponent GetSelectedKeypadComponent()
        {
            if (keypadComponents.Count > 0 && selectedKeypadIndex < keypadComponents.Count)
            {
                if (selectedKeypadIndex < keypadComponents.Count)
                {
                    return keypadComponents[selectedKeypadIndex];
                }
                else
                {
                    //디버깅용 오류 메시지
                    Debug.Log("Your selectedKeypadIndex is out of range.");
                    return null;
                }
            }
            else
            {
                //디버깅용 오류 메시지
                Debug.Log("Keypad Components are empty.");
                return null;
            }

        }

        public void SetSelectedKeypadIndex(int selectedKeypadIndex)
        {
            if (selectedKeypadIndex > -1 && selectedKeypadIndex < keypadComponents.Count)
            {
                this.selectedKeypadIndex = selectedKeypadIndex;
            }
            else
            {
                //디버깅용 오류 메시지
                Debug.Log("Your selectedKeypadIndex is out of range.");
            }
        }

        public override void Subscribes()
        {
            RegisterUIDisposable(keypadButton
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    if (keypadComponents.Count > 0)
                    {
                        if (selectedKeypadIndex < keypadComponents.Count)
                        {
                            keypadComponents[selectedKeypadIndex].OpenOrCloseKeypad();
                        }
                        else
                        {
                        //디버깅용 오류 메시지
                        Debug.Log("Your selectedKeypadIndex is out of range.");
                        }
                    }
                    else
                    {
                    //디버깅용 오류 메시지
                    Debug.Log("Keypad Components are empty.");
                    }
                }
                )
                );
        }
    }
}