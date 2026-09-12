using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Portfolio.Playable.Gonu
{
    public class GonuStateButtonComponent : MonoBehaviour
    {
        public int index;
        public int initialState = -1;
        public Button button;
        public List<GameObject> gonuPieces = new List<GameObject>();
        public GameObject checkMarkObject;
        public ReactiveProperty<bool> isSelected = new ReactiveProperty<bool>(false);
        public ReactiveProperty<int> state = new ReactiveProperty<int>(-1);

        [Header("Pairing")]
        public List<GonuStateButtonComponent> originateButtonComponents = new List<GonuStateButtonComponent>();

        private CompositeDisposable _disposables = new CompositeDisposable();

        public void Ready(int index)
        {
            this.index = index;
            _disposables.Clear();

            if (button == null) button = GetComponent<Button>();

            // Ensure Button graphic receives raycasts
            var btnImage = GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.raycastTarget = true;
            }

            gonuPieces.Clear();
            checkMarkObject = null;

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                if (child.name.ToLower().Contains("checkmark"))
                {
                    checkMarkObject = child;
                    checkMarkObject.SetActive(false);
                }
                else
                {
                    gonuPieces.Add(child);
                }

                // Make sure child images do not block raycasting from the main Button
                var childImg = child.GetComponent<Image>();
                if (childImg != null)
                {
                    childImg.raycastTarget = false;
                }
                var childCanvasGroup = child.GetComponent<CanvasGroup>();
                if (childCanvasGroup != null)
                {
                    childCanvasGroup.blocksRaycasts = false;
                }
            }

            state
                .DistinctUntilChanged()
                .Subscribe(changedState =>
                {
                    for (int i = 0; i < gonuPieces.Count; i++)
                    {
                        if (gonuPieces[i] != null)
                        {
                            gonuPieces[i].SetActive(changedState == i);
                        }
                    }
                })
                .AddTo(_disposables);

            isSelected
                .DistinctUntilChanged()
                .Subscribe(isOn =>
                {
                    if (checkMarkObject != null)
                    {
                        checkMarkObject.SetActive(isOn);
                    }
                })
                .AddTo(_disposables);

            Refresh();
        }

        public void Refresh()
        {
            state.Value = initialState;
            isSelected.Value = false;
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}
