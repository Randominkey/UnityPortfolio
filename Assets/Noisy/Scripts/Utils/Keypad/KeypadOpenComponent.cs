using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UniRx;
using CMS.Core.UI.Base;
using CMS.Template.UI.Keypad;

public class KeypadOpenComponent : UIRootBase
{
    [SerializeField] private Transform UIRootTransform;

    public override void Subscribes()
    {
        RegisterUIDisposable(GetComponent<Button>()
            .OnClickAsObservable()
            .Subscribe(_ =>
            {
                //if (UIRootTransform)
                    //UIRootTransform.GetComponent<KeypadComponent>().OpenOrCloseKeypad();
            }
            )
            );
    }
}
