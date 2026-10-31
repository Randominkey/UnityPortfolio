using CMS.Template.UI.Keypad;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace MiniGame.PlaySeesaw
{
    public class PlaySeesawInnerCell : MonoBehaviour
    {
        public NeoKeypadLinkedToggleComponent nKLT;
        [field: SerializeField] public ReactiveProperty<CellState> cellState = new ReactiveProperty<CellState>();
        public GameObject leverObject;
        public int number;

        static Color32 readyColor = new Color32(95, 95, 95, 255);
        static Color32 correctColor = new Color32(0, 153, 202, 255);
        static Color32 errorColor = new Color32(246, 99, 124, 255);

        public void Ready(NeoKeypadComponent NeoKeypad)
        {
            nKLT = GetComponent<NeoKeypadLinkedToggleComponent>();

            nKLT.LinkedKeypadComponent = NeoKeypad;

            nKLT.Ready();

            cellState
                .DistinctUntilChanged()
                .Subscribe(state =>
                {
                    switch (state)
                    {
                        case CellState.Ready:
                            nKLT.Label.color = readyColor;
                            break;
                        case CellState.Correct:
                            nKLT.Label.color = correctColor;
                            break;
                        case CellState.Error:
                            nKLT.Label.color = errorColor;
                            break;
                        //case CellState.OverFlow:
                        //    break;
                        default:
                            break;
                    }
                });
        }

        public void Refresh(char initialKey)
        {
            switch (initialKey)
            {
                case 'x':
                    nKLT.Label.text = "";
                    leverObject.SetActive(false);
                    break;
                case '4':
                    nKLT.Label.text = "4";
                    leverObject.SetActive(true);
                    break;
                default:
                    nKLT.Label.text = initialKey.ToString();
                    leverObject.SetActive(false);
                    break;
            }

            UpdateInfoByText();
        }

        public void UpdateInfoByText()
        {
            cellState.Value = CellState.Ready;

            string nKLTText = nKLT.Label.text;

            if (string.IsNullOrEmpty(nKLTText))
            {
                number = -1;
                leverObject.SetActive(false);
            }
            else
            {
                //lever
                if (nKLTText == "4")
                {
                    nKLT.Label.color = Color.clear;
                    leverObject.SetActive(true);
                }
                else
                {
                    nKLT.Label.color = readyColor;
                    leverObject.SetActive(false);
                }

                number = int.Parse(nKLTText);
            }
        }
    }
}