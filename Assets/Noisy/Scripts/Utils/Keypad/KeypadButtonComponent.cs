using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace CMS.Template.UI.Keypad
{
    public class KeypadButtonComponent : MonoBehaviour
    {
        enum KeypadButtonType
        {
            String,
            AllClear,
            BackSpace,
            Space,
            CustomizedContents,
        }

        [Header("버튼 글씨/프리팹화 된 버튼들은 자동 제거")]
        [SerializeField] private Text text;
        [SerializeField] private Button button;
        [SerializeField] private Image image;

        [Header("커스텀 버튼일 때 쓸 키 값")]
        [SerializeField] private string content;


        [Header("쓰고싶은 버튼 타입 지정")]
        [SerializeField] private KeypadButtonType buttonType;

        [Header("0: 빈칸 / 1: 비우기 / 2: ← / 3: _ / 4: 커스텀")]
        [SerializeField] private Sprite[] buttonSprites;

        // Start is called before the first frame update
        void Start()
        {
            switch (buttonType)
            {
                case KeypadButtonType.String:
                    SetContent(GetText());
                    break;

                case KeypadButtonType.AllClear:
                    SetText("");
                    SetContent("all clear");
                    break;

                case KeypadButtonType.BackSpace:
                    SetText("");
                    SetContent("backspace");
                    break;

                case KeypadButtonType.Space:
                    SetText("");
                    SetContent("space");
                    break;

                case KeypadButtonType.CustomizedContents:
                    //개발자가 직접 구현
                    //text.text = "";                
                    //컨텐트는 인스펙터에서 직접 넣기
                    break;
                default:
                    break;
            }

            image.sprite = buttonSprites[(int)buttonType];

        }

        public void SetText(string text) => this.text.text = text;
        public string GetText() => text.text;
        public void SetContent(string content) => this.content = content;
        public string GetContent() => content;
        public Button GetButton() => button;

    }
}