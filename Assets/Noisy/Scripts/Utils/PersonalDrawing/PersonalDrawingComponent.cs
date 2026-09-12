using System.Collections.Generic;
using UnityEngine;

namespace CMS.WeeklyGame.PersonalDrawingComponent
{
    using CMS.Util.Extend;
    using DG.Tweening;
    using UniRx;
    using UniRx.Triggers;
    using UnityEngine.UI;
    using UnityEngine.UI.Extensions;
    using static CMS.Util.Drawing.DrawingManager;
    using UniRx;

    public class PersonalDrawingComponent : MonoBehaviour
    {
        private enum Type
        {
            None, Pen, Line, Eraser,
        }

        private enum AlignmentDirection
        {
            Horizontal,
            Vertical,
            Custom,
        }

        [Space(10)]
        [SerializeField] private RawImage drawingRawImage;
        [SerializeField] private Color drawingColor;
        [SerializeField] private float cameraDistance;
        [SerializeField] private List<Color> colorList;

        [Space]
        [SerializeField] private AlignmentDirection alignmentDirection;
        [SerializeField] private GridLayoutGroup buttonBox;
        [SerializeField] private GridLayoutGroup eraserSizeSliderParent;

        [Space]
        [SerializeField] private Toggle penToggle;
        [SerializeField] private Toggle lineToggle;
        [SerializeField] private Toggle eraserToggle;

        [SerializeField] private RectTransform penImage;
        [SerializeField] private RectTransform eraserImage;

        [Space]
        [SerializeField] private Slider eraserSizeSlider;

        [Space]
        [SerializeField] private Button colorButton;
        [SerializeField] private List<Sprite> colorButtonSprites;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button undoButton;

        [Space]
        [SerializeField] private Button closeButton;

        [Space]
        [SerializeField] private Text pageText;



        private Texture2D sourceTexture;

        private Type mCurrentType = Type.None;
        private Type CurrentType
        {
            get { return mCurrentType; }
            set
            {
                mCurrentType = value;
                penImage.gameObject.SetActive(false);

                switch (mCurrentType)
                {
                    case Type.None:
                        drawingRawImage.raycastTarget = false;
                        break;

                    case Type.Eraser:
                        drawingRawImage.raycastTarget = true;

                        eraserImage.gameObject.SetActive(true);
                        eraserImage.sizeDelta = new Vector2(EraserSize, EraserSize);

                        if (eraserSizeSlider != null)
                        {
                            eraserSizeSlider.gameObject.SetActive(true);
                            eraserSizeSlider.transform.DOKill();
                            eraserSizeSlider.transform.localScale = new Vector3(1f, 0.1f, 1f);
                            eraserSizeSlider.transform.DOScaleY(1f, 0.3f);
                        }
                        break;

                    case Type.Pen:
                        drawingRawImage.raycastTarget = true;

                        penImage.gameObject.SetActive(true);
                        break;

                    case Type.Line:
                        drawingRawImage.raycastTarget = true;
                        break;
                }

                if (mCurrentType != Type.Pen)
                    penImage.gameObject.SetActive(false);

                if (mCurrentType != Type.Eraser)
                {
                    eraserImage.gameObject.SetActive(false);

                    if (eraserSizeSlider != null)
                    {
                        eraserSizeSlider.transform.DOKill();
                        eraserSizeSlider.transform.DOScaleY(0f, 0.3f)
                            .OnComplete(() => { eraserSizeSlider.gameObject.SetActive(false); });
                    }
                }
            }
        }

        private int colorIndex = 0;

        private int mCurrentPage = 1;
        private int CurrentPage
        {
            get { return mCurrentPage; }
            set
            {
                mCurrentPage = value;

                RawImageSetting(mCurrentPage);
                pageText.text = mCurrentPage.ToString();
            }
        }


        private Vector3 startDraggingMousePosition;
        private float LineThick { get; set; } = 6f;
        private float EraserSize { get; set; } = 120f;

        private float screenWidth = 0f;
        private float screenHeight = 0f;

        private float scaleRatio = 1f;

        private Rect rawImageRect;


        private GameObject lineObject;
        private UILineRenderer lineRenderer;


        private Dictionary<int, PixelZipList> saveData = new Dictionary<int, PixelZipList>();
        private PixelZipList currentPixelZipList;

        private bool somethingDrawnInDragging;

        private void OnValidate()
        {
            if (buttonBox == null || eraserSizeSliderParent == null) return;

            if (alignmentDirection == AlignmentDirection.Horizontal)
            {
                buttonBox.padding.right = 102;
                buttonBox.padding.bottom = 20;
                buttonBox.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                eraserSizeSliderParent.padding.right = 53;
                eraserSizeSliderParent.padding.bottom = 82;
            }
            else if(alignmentDirection == AlignmentDirection.Vertical)
            {
                buttonBox.padding.right = 20;
                buttonBox.padding.bottom = 102;
                buttonBox.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                eraserSizeSliderParent.padding.right = 92;
                eraserSizeSliderParent.padding.bottom = 108;
            }
            else
            {
                //custom
            }
        }

        public void Initiate()
        {
            OnValidate();

            CurrentPage = 1;
            colorIndex = 0;

            colorButton.GetComponent<Image>().sprite = colorButtonSprites[colorIndex];
            drawingColor = colorList[colorIndex];

            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    switch (CurrentType)
                    {
                        case Type.None:
                            break;
                        case Type.Pen:
                            {
                                Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));

                                if (rawImageRect.Contains(mousePos))
                                {
                                    penImage.gameObject.SetActive(true);
                                    penImage.position = mousePos;
                                }
                                else
                                    penImage.gameObject.SetActive(false);
                            }
                            break;
                        case Type.Eraser:
                            {
                                Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));

                                if (rawImageRect.Contains(mousePos))
                                {
                                    eraserImage.gameObject.SetActive(true);
                                    eraserImage.position = mousePos;
                                }
                                else
                                    eraserImage.gameObject.SetActive(false);
                            }
                            break;
                    }

                    ScreenCheck();
                })
                .AddTo(this.gameObject);

            ScreenCheck();

            RawImageDrawingSetting();

            ButtonSetting();
        }

        private void ScreenCheck()
        {
            if ((screenWidth != drawingRawImage.rectTransform.rect.width) || (screenHeight != drawingRawImage.rectTransform.rect.height))
            {
                screenWidth = drawingRawImage.rectTransform.rect.width;
                screenHeight = drawingRawImage.rectTransform.rect.height;

                RawImagePositionSetting();
                SetScaleAndSetRect();

                RefreshAll();
            }
        }

        private void RawImagePositionSetting()
        {
            Vector3 pos = transform.parent.position;
            //pos.z = -100f + cameraDistance;
            transform.position = Vector3.Lerp(pos, Camera.main.transform.position, 0.9f);
            transform.rotation = Quaternion.Normalize(Camera.main.transform.rotation);

            float scale = cameraDistance / 100f;
            transform.localScale = Vector3.one * scale;
            penImage.transform.localScale = Vector3.one * scale;
            eraserImage.transform.localScale = Vector3.one * scale;
        }

        private void ButtonSetting()
        {
            // Add color swap button
            colorButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    colorIndex++;

                    if (colorIndex >= colorList.Count)
                        colorIndex = 0;

                    colorButton.GetComponent<Image>().sprite = colorButtonSprites[colorIndex];
                    drawingColor = colorList[colorIndex];
                })
                .AddTo(this);

            penToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                    CurrentType = Type.Pen;
                else
                    CurrentType = Type.None;
            });


            lineToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                    CurrentType = Type.Line;
                else
                    CurrentType = Type.None;
            });

            eraserToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                    CurrentType = Type.Eraser;
                else
                    CurrentType = Type.None;
            });

            if (eraserSizeSlider != null)
            {
                eraserSizeSlider.onValueChanged.AddListener((value) =>
                {
                    EraserSize = value;
                    eraserImage.sizeDelta = new Vector2(value, value);
                });
            }
        }

        private void RawImageDrawingSetting()
        {
            drawingRawImage.OnPointerDownAsObservable().Where(o => Input.GetMouseButtonDown(0))
                .Subscribe(_ =>
                {
                    Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));

                    switch (CurrentType)
                    {
                        case Type.Eraser:
                            {
                                startDraggingMousePosition = mousePos;
                                somethingDrawnInDragging =
                                    Erase(startDraggingMousePosition, mousePos, EraserSize * scaleRatio);
                            }
                            break;

                        case Type.Pen:
                            startDraggingMousePosition = mousePos;
                            somethingDrawnInDragging = false;
                            break;

                        case Type.Line:
                            startDraggingMousePosition = mousePos;

                            if (!lineObject)
                            {
                                lineObject = new GameObject("Line");
                                lineObject.transform.SetParent(drawingRawImage.transform.parent);
                                lineObject.transform.position = drawingRawImage.transform.position;
                                lineObject.transform.localEulerAngles = Vector3.zero;

                                Canvas lineObjectCanvas = lineObject.AddComponent<Canvas>();
                                lineObject.AddComponent<CanvasRenderer>();      // Add component for Unity 2020.3.4f++
                                lineObjectCanvas.pixelPerfect = false;
                                lineObjectCanvas.overrideSorting = true;
                                lineObjectCanvas.sortingOrder = 9999;


                                lineRenderer = lineObject.AddComponent<UILineRenderer>();
                                if (lineRenderer)
                                {
                                    lineRenderer.LineThickness = LineThick * scaleRatio;
                                    lineRenderer.color = Color.magenta;
                                    lineRenderer.GetComponent<RectTransform>().pivot = Vector2.zero;
                                }

                                RectTransform sourceImageParentTransform = drawingRawImage.transform.parent.GetComponent<RectTransform>();
                                if (sourceImageParentTransform)
                                {
                                    RectTransform createdLineObjectTransform = lineObject.GetComponent<RectTransform>();
                                    if (createdLineObjectTransform)
                                    {
                                        Vector3 calcPos = sourceImageParentTransform.sizeDelta * (sourceImageParentTransform.pivot - createdLineObjectTransform.pivot);
                                        createdLineObjectTransform.localPosition += calcPos;
                                    }
                                }
                            }

                            if (lineRenderer)
                            {
                                lineRenderer.Points = new Vector2[2];
                                lineRenderer.Points[0] = lineObject.transform.InverseTransformPoint(startDraggingMousePosition);
                                lineRenderer.Points[1] = lineObject.transform.InverseTransformPoint(startDraggingMousePosition);
                            }
                            break;
                    }
                })
                .AddTo(drawingRawImage.gameObject);

            drawingRawImage.OnDragAsObservable()
                .Subscribe(_ =>
                {
                    Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));

                    switch (CurrentType)
                    {
                        case Type.Eraser:
                            {
                                bool somethingDrawn =
                                    Erase(startDraggingMousePosition, mousePos, EraserSize * scaleRatio);

                                startDraggingMousePosition = mousePos;

                                somethingDrawnInDragging = somethingDrawnInDragging || somethingDrawn;
                            }
                            break;

                        case Type.Pen:
                            {
                                bool somethingDrawn =
                                    DrawLine(drawingColor, startDraggingMousePosition, mousePos, LineThick * scaleRatio);

                                startDraggingMousePosition = mousePos;

                                somethingDrawnInDragging = somethingDrawnInDragging || somethingDrawn;
                            }
                            break;

                        case Type.Line:
                            if (lineRenderer)
                            {
                                lineRenderer.Points[1] = lineObject.transform.InverseTransformPoint(mousePos);
                                lineRenderer.SetAllDirty();
                            }
                            break;
                    }
                })
                .AddTo(drawingRawImage.gameObject);

            drawingRawImage.OnEndDragAsObservable()
                .Subscribe(_ =>
                {
                    Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cameraDistance));

                    switch (CurrentType)
                    {
                        case Type.Eraser:
                            {
                                bool somethingDrawn =
                                    Erase(startDraggingMousePosition, mousePos, EraserSize * scaleRatio);

                                startDraggingMousePosition = Vector2.zero;

                                somethingDrawnInDragging = somethingDrawnInDragging || somethingDrawn;

                                if (somethingDrawnInDragging)
                                    SavePixels();
                            }
                            break;

                        case Type.Pen:
                            {
                                bool somethingDrawn =
                                    DrawLine(drawingColor, startDraggingMousePosition, mousePos, LineThick * scaleRatio);

                                startDraggingMousePosition = Vector2.zero;

                                somethingDrawnInDragging = somethingDrawnInDragging || somethingDrawn;

                                if (somethingDrawnInDragging)
                                {
                                    SavePixels();
                                }
                            }
                            break;

                        case Type.Line:
                            {
                                bool somethingDrawn =
                                    DrawLine(drawingColor, startDraggingMousePosition, mousePos, LineThick * scaleRatio);

                                startDraggingMousePosition = Vector2.zero;

                                DestroyImmediate(lineRenderer.gameObject);

                                somethingDrawnInDragging = somethingDrawnInDragging || somethingDrawn;

                                if (somethingDrawnInDragging)
                                {
                                    SavePixels();
                                }
                            }
                            break;
                    }
                })
                .AddTo(drawingRawImage.gameObject);

            drawingRawImage.OnPointerUpAsObservable()
                .Subscribe(_ =>
                {
                    switch (CurrentType)
                    {
                        case Type.Eraser:
                            {
                                if (somethingDrawnInDragging)
                                {
                                    SavePixels();
                                }
                            }
                            break;

                        case Type.Pen:
                            break;

                        case Type.Line:
                            break;
                    }
                })
                .AddTo(drawingRawImage.gameObject);
        }

        private void SetScaleAndSetRect()
        {
            scaleRatio = drawingRawImage.rectTransform.lossyScale.x;
            rawImageRect = drawingRawImage.rectTransform.GetWorldSpaceRect();
        }

        public bool Erase(Vector2 startPosition, Vector2 endPosition, float size)
        {
            if (Vector2.Distance(startPosition, endPosition) < 5f * scaleRatio)
            {
                Texture2D sourceTexture = drawingRawImage.texture as Texture2D;

                return sourceTexture.Erase(rawImageRect, endPosition, size);
            }
            else
            {
                Texture2D sourceTexture = drawingRawImage.texture as Texture2D;

                return sourceTexture.Erase(rawImageRect, startPosition, endPosition, size);
            }
        }

        public bool DrawLine(Color color, Vector2 startPosition, Vector2 endPosition, float lineThick)
        {
            Texture2D sourceTexture = drawingRawImage.texture as Texture2D;

            return sourceTexture.DrawLine(rawImageRect, color, startPosition, endPosition, lineThick);
        }

        public void RawImageSetting(int saveSlotNumber)
        {
            if (saveData.ContainsKey(saveSlotNumber))
            {
                currentPixelZipList = saveData[saveSlotNumber];
                Color32[] pixels = currentPixelZipList[currentPixelZipList.Count - 1].GetPixels32();

                if (drawingRawImage.texture != null)
                {
                    Texture2D texture2D = drawingRawImage.texture as Texture2D;
                    texture2D.Reinitialize(currentPixelZipList.width, currentPixelZipList.height);
                    sourceTexture = texture2D;
                }
                else
                {
                    sourceTexture = new Texture2D(currentPixelZipList.width, currentPixelZipList.height);
                    drawingRawImage.texture = sourceTexture;
                }

                sourceTexture.SetPixels32(pixels);
                sourceTexture.Apply();
            }

            else
            {
                RectTransform sourceTextureRenderRect = drawingRawImage.GetComponent<RectTransform>();

                if (drawingRawImage.texture != null)
                {
                    Texture2D texture2D = drawingRawImage.texture as Texture2D;
                    texture2D.Reinitialize((int)sourceTextureRenderRect.rect.width / 2, (int)sourceTextureRenderRect.rect.height / 2);
                    sourceTexture = texture2D;
                }
                else
                {
                    sourceTexture = new Texture2D((int)sourceTextureRenderRect.rect.width / 2, (int)sourceTextureRenderRect.rect.height / 2);
                    drawingRawImage.texture = sourceTexture;
                }

                Color32[] clearColors = sourceTexture.GetPixels32();
                for (int i = 0; i < clearColors.Length; i++)
                {
                    clearColors[i] = new Color32(255, 255, 255, 0);
                }

                sourceTexture.SetPixels32(clearColors);
                sourceTexture.Apply();

                currentPixelZipList = new PixelZipList(sourceTexture.width, sourceTexture.height);
                currentPixelZipList.Add(new PixelZip(sourceTexture.GetPixels32()));

                saveData.Add(saveSlotNumber, currentPixelZipList);
            }

            drawingRawImage.color = Color.white;
            drawingRawImage.enabled = true;
        }

        public void RefreshCurrentRawImage()
        {
            if (currentPixelZipList.Count > 0)
            {
                PixelZip thisPixelZip = currentPixelZipList[0];
                Color32[] pixels = thisPixelZip.GetPixels32();

                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color32(255, 255, 255, 0);
                }

                sourceTexture.SetPixels32(pixels);
                sourceTexture.Apply();

                while (currentPixelZipList.Count > 1)
                {
                    currentPixelZipList.RemoveAt(1);
                }

                currentPixelZipList[0] = new PixelZip(pixels);
            }
        }

        public void RefreshAll()
        {
            if (sourceTexture)
            {
                if (drawingRawImage.texture == sourceTexture)
                    drawingRawImage.texture = null;

                Texture2D.Destroy(sourceTexture);
                sourceTexture = null;
            }

            foreach (var item in saveData)
            {
                PixelZipList pixelZipList = item.Value;

                pixelZipList.Clear();
            }

            saveData.Clear();

            CurrentPage = 1;
        }

        private void SavePixels()
        {
            currentPixelZipList.Add(new PixelZip(sourceTexture.GetPixels32()));
            somethingDrawnInDragging = false;

            if (currentPixelZipList.Count > 20)
            {
                currentPixelZipList.RemoveAt(0);
            }
        }

        public void PrevButtonClick()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
        }

        public void NextButtonClick()
        {
            if (CurrentPage < 20)
            {
                CurrentPage++;
            }
        }

        public void ClearButtonClick()
        {
            RefreshCurrentRawImage();
        }

        public void UndoButtonClick()
        {
            if (currentPixelZipList != null && currentPixelZipList.Count > 1)
            {
                PixelZip thisPixelZip = currentPixelZipList[currentPixelZipList.Count - 2];
                sourceTexture.SetPixels32(thisPixelZip.GetPixels32());
                sourceTexture.Apply();
                currentPixelZipList.RemoveAt(currentPixelZipList.Count - 1);
            }
        }


        public void OpenOrClose()
        {
            if (transform.parent.gameObject.activeSelf)
                transform.parent.gameObject.SetActive(false);
            else
                transform.parent.gameObject.SetActive(true);
        }

        public void OpenOrClose(bool isOpen)
        {
            transform.parent.gameObject.SetActive(isOpen);
        }

        public void SetDrawingColor(Color color)
        {
            drawingColor = color;
        }
    }
}

