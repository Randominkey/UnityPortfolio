using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CMS.Util.MoveNRotate;

namespace CMS.Util.Drawing
{
    using UnityEngine.UI;
    using CMS.Util.Extend;
    using System;

    using UniRx;
    using UniRx.Triggers;

    using UnityEngine.UI.Extensions;

    using CMS.Util.ScreenInfo;
    using System.Linq;
    using CMS.Util.UI;
    using DG.Tweening;

    public class DrawingManager : MonoBehaviour
    {
        // made by KKIL

        public enum Type
        {
            None, Ruler, Protractor, Eraser, Pen, Highlight, Line, Stamp, Compass
        }


        public ReactiveProperty<Type> CurrentType { get; set; } = new ReactiveProperty<Type>();

        private List<IDisposable> uiDisposables = new List<IDisposable>();


        private RectTransform sourceTextureRenderRect;


        private bool canvasRenderModeIsOverlay = true;
        private float cameraDistance = 100f;
        private Camera mainCamera;


        //private Vector3 UILineRendererOffset = Vector3.zero;


        [Space]
        [SerializeField] private ToggleGroup itemToggleGroup;
        [SerializeField] private List<DrawingToggleComponent> itemToggles;
        [SerializeField] private Button undoButton;
        

        [SerializeField] private Sprite rulerHorizontalMoveNormalSprite;
        [SerializeField] private Sprite rulerHorizontalMovePressSprite;

        public Texture2D sourceTexture;

        private RawImage sourceRawImage;

        private Dictionary<int, PixelZipList> saveData;
        private PixelZipList currentPixelZipList;

        private Vector2 startDraggingMousePosition;
        private Vector2 protractorPosition;

        [SerializeField] private RectTransform drawingItems;


        private MoveDraggableObjectComponent ruler;
        private MoveDraggableObjectComponentForRulerPen rulerPen;
        private RectTransform rulerHorMove;
        private RectTransform rulerRectTransform;
        private Vector3 firstRulerPosition;
        private Image rulerSizeLine;
        private Text rulerSizeText;


        private MoveDraggableObjectComponent protractor;
        private MoveDraggableObjectComponentForProtractorPen protractorPen;


        private RectTransform eraserImage;

        private RectTransform compassImage;
        private RectTransform compassCenterPoint;
        private RectTransform compassStartPoint;
        private RectTransform compassEndPoint;
        private Image compassArcModeNoticeImage;
        private Text compassArcModeNoticeText;


        private Vector2 rulerInitialPosition = Vector2.zero;
        private bool rulerHorMoveOn = false;

        private Vector2 protractorInitialPosition = Vector2.zero;

        private GameObject lineObject;
        private UILineRenderer lineRenderer;

        private Vector3[] rectCorners = new Vector3[4];


        int _arc_add_mode = -1;

        public ReactiveProperty<int> compassArcAddMode = new ReactiveProperty<int>(-1);

        [SerializeField] private List<string> compassArcAddModeNoticeMessages;
        [SerializeField] private Color noticeColor;
        private List<bool> compassArcAddModeNoticeAlreadyShown = new List<bool>() { false, false, false };


        Arc_Item currentArcItem = new Arc_Item();

        Vector2 mousePointToCenter;
        Vector2 mousePointToStartPoint;


        private bool somethingDrawnInDragging;

        public Color CurrentColor { get; set; } = Color.magenta;

        public bool GetDrawingTogglesOn() => itemToggleGroup.AnyTogglesOn();

        public Texture2D GetReferenceSourceTexture() => sourceTexture;
        public Action OnEndedSourceTextureEdit { get; set; }
        public Texture2D SourceTexture
        {
            get
            {
                if (sourceTexture != null)
                    return Texture2D.Instantiate(sourceTexture);
                else
                    return null;
            }
            set
            {
                if (sourceTexture != null)
                    Texture2D.DestroyImmediate(sourceTexture);

                sourceTexture = Texture2D.Instantiate(value);
            }
        }

        public float LineThick { get; set; } = 6f;
        public float HighlightThick { get; set; } = 40f;
        public float EraserSize { get; set; } = 120f;
        public float CompassPointSize { get; set; } = 30f;

        [Space]
        [SerializeField] private Slider eraserSizeSlider;

        public float RulerScale
        {
            get
            {
                return ruler.transform.parent.localScale.x;
            }
            set
            {
                ruler.transform.parent.localScale = new Vector3(value, value, 1f);
            }
        }

        public float ProtractorScale
        {
            get
            {
                return protractor.transform.parent.localScale.x;
            }
            set
            {
                protractor.transform.parent.localScale = new Vector3(value, value, 1f);
            }
        }


        private float scaleRatio = 1f;

        public void SetRulerPenActive(bool active) => rulerPen.gameObject.SetActive(active);

        private Canvas GetParentCanvas(string name)
        {
            Canvas parentCanvas = null;

            Transform targetTransform = this.transform.parent;

            while (targetTransform.name != name)
            {
                if (targetTransform.parent == null)
                    return null;

                targetTransform = targetTransform.parent;
            }

            parentCanvas = targetTransform.GetComponent<Canvas>();

            return parentCanvas;
        }

        public void Initialize(RawImage initRawImage, Transform drawingItemParent = null, int initialSaveNumber = 0)
        {
            sourceRawImage = initRawImage;

            Canvas rootCanvas = GetParentCanvas("[Root]");

            if (rootCanvas)
            {
                mainCamera = rootCanvas.worldCamera;

                if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvasRenderModeIsOverlay = true;
                }
                else
                {
                    canvasRenderModeIsOverlay = false;
                    cameraDistance = rootCanvas.planeDistance;

                    MoveDraggableObjectComponent.mainCamera = mainCamera;
                    MoveDraggableObjectComponent.canvasRenderModeIsOverlay = canvasRenderModeIsOverlay;
                    MoveDraggableObjectComponent.cameraDistance = cameraDistance;

                    RotateDraggableObjectComponent.mainCamera = mainCamera;
                    RotateDraggableObjectComponent.canvasRenderModeIsOverlay = canvasRenderModeIsOverlay;
                    RotateDraggableObjectComponent.cameraDistance = cameraDistance;

                    MoveDraggableObjectComponentForRulerPen.mainCamera = mainCamera;
                    MoveDraggableObjectComponentForRulerPen.canvasRenderModeIsOverlay = canvasRenderModeIsOverlay;
                    MoveDraggableObjectComponentForRulerPen.cameraDistance = cameraDistance;

                    MoveDraggableObjectComponentForProtractorPen.mainCamera = mainCamera;
                    MoveDraggableObjectComponentForProtractorPen.canvasRenderModeIsOverlay = canvasRenderModeIsOverlay;
                    MoveDraggableObjectComponentForProtractorPen.cameraDistance = cameraDistance;
                }
            }

            scaleRatio = sourceRawImage.GetComponent<RectTransform>().lossyScale.y;

            ScreenUtils.OnChangeScreenInfo.AddListener((eventData) =>
            {
                scaleRatio = sourceRawImage.GetComponent<RectTransform>().lossyScale.y;
            });

            //LineThick = 6f * scaleRatio;
            //HighlightThick = 30f * scaleRatio;
            //EraserSize = 100f * scaleRatio;
            //CompassPointSize = 50f * scaleRatio;

            if (eraserSizeSlider != null)
            {
                eraserSizeSlider.onValueChanged.AddListener((value) =>
                {
                    EraserSize = value;
                    eraserImage.sizeDelta = new Vector2(value, value);
                });
            }


            GameObject drawingItemCopy = GameObject.Instantiate(drawingItems.gameObject);
            if (drawingItemParent != null)
                drawingItemCopy.transform.SetParent(drawingItemParent);
            else
                drawingItemCopy.transform.SetParent(sourceRawImage.transform.parent);
            //drawingItemCopy.transform.localPosition = Vector3.zero;
            drawingItemCopy.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            drawingItemCopy.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            drawingItemCopy.transform.localScale = Vector3.one;


            ruler = drawingItemCopy.transform.FindChildByRecursion("Ruler").GetComponent<MoveDraggableObjectComponent>();
            rulerRectTransform = ruler.transform.parent.GetComponent<RectTransform>();
            rulerSizeLine = drawingItemCopy.transform.FindChildByRecursion("RulerSizeLine").GetComponent<Image>();
            rulerSizeText = drawingItemCopy.transform.FindChildByRecursion("RulerSizeLabel").GetComponent<Text>();
            rulerPen = drawingItemCopy.transform.FindChildByRecursion("RulerPen").GetComponent<MoveDraggableObjectComponentForRulerPen>();
            rulerHorMove = drawingItemCopy.transform.FindChildByRecursion("RulerHorMove").GetComponent<RectTransform>();
            protractor = drawingItemCopy.transform.FindChildByRecursion("Protractor").GetComponent<MoveDraggableObjectComponent>();
            protractorPen = drawingItemCopy.transform.FindChildByRecursion("ProtractorPen").GetComponent<MoveDraggableObjectComponentForProtractorPen>();
            eraserImage = drawingItemCopy.transform.FindChildByRecursion("EraserImage").GetComponent<RectTransform>();
            compassImage = drawingItemCopy.transform.FindChildByRecursion("Compass").GetComponent<RectTransform>();
            compassCenterPoint = drawingItemCopy.transform.FindChildByRecursion("CenterPoint").GetComponent<RectTransform>();
            compassStartPoint = drawingItemCopy.transform.FindChildByRecursion("StartPoint").GetComponent<RectTransform>();
            compassEndPoint = drawingItemCopy.transform.FindChildByRecursion("EndPoint").GetComponent<RectTransform>();
            compassArcModeNoticeImage = drawingItemCopy.transform.FindChildByRecursion("CompassArcModeNoticeImage").GetComponent<Image>();
            compassArcModeNoticeText = compassArcModeNoticeImage.transform.GetChild(0).GetComponent<Text>();
            //Vector3 parentSize = drawingItemCopy.transform.parent.GetComponent<RectTransform>().sizeDelta;

            //compassArcModeNoticeImage.rectTransform.localPosition = new Vector3(0, -drawingItemCopy.transform.parent.GetComponent<RectTransform>().sizeDelta.y / 2.5f, 0);

            compassArcModeNoticeImage.color = new Color(noticeColor.r, noticeColor.g, noticeColor.b, 0.3f);
            compassArcModeNoticeText.GetComponent<NicerOutline>().effectColor = noticeColor;

            foreach (IDisposable disposable in uiDisposables)
                disposable.Dispose();

            uiDisposables.Clear();

            SetRulerSize(12);

            saveData = new Dictionary<int, PixelZipList>();
            RawImageSetting(initialSaveNumber);

            itemToggleGroup
                .ObserveEveryValueChanged(changedToggleGroup => itemToggleGroup.ActiveToggles().FirstOrDefault())
                .Select(selectedToggle => itemToggles.Where(o => o.TargetToggle.isOn))
                .Subscribe(curIsOnEnumerator =>
                {
                    DrawingToggleComponent isOnComp = curIsOnEnumerator.FirstOrDefault();

                    if (isOnComp)
                    {
                        CurrentType.Value = isOnComp.DrawingType;
                    }
                });

            itemToggleGroup
                .ObserveEveryValueChanged(changedToggleGroup => itemToggleGroup.ActiveToggles().FirstOrDefault())
                .Select(selectedToggle => itemToggles.Where(o => !o.TargetToggle.isOn))
                .Subscribe(notIsOnEnumerator =>
                {
                    List<DrawingToggleComponent> isOnToggles = itemToggles.Where(o => o.TargetToggle.isOn).ToList();

                    if (isOnToggles.Count == 0)
                    {
                        CurrentType.Value = Type.None;
                    }
                });


            undoButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    if (currentPixelZipList != null && currentPixelZipList.Count > 1)
                    {
                        PixelZip thisPixelZip = currentPixelZipList[currentPixelZipList.Count - 2];
                        sourceTexture.SetPixels32(thisPixelZip.GetPixels32());
                        sourceTexture.Apply();
                        currentPixelZipList.RemoveAt(currentPixelZipList.Count - 1);
                    }
                })
                .AddTo(undoButton.gameObject);


            protractor.GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;

            CurrentType.ObserveEveryValueChanged(_ => CurrentType.Value)
                .Subscribe(type =>
                {
                    switch (type)
                    {
                        case Type.Ruler:
                            {
                                ruler.transform.parent.gameObject.SetActive(true);
                                RectTransform rulerRectTransform = ruler.transform.parent.GetComponent<RectTransform>();

                                if (rulerInitialPosition == Vector2.zero)
                                    rulerInitialPosition = ruler.transform.parent.GetComponent<RectTransform>().position;
                                else
                                    rulerRectTransform.position = rulerInitialPosition;

                                rulerRectTransform.eulerAngles = new Vector3(0, 0, 0);
                            }
                            break;
                        case Type.Protractor:
                            {
                                protractor.transform.parent.gameObject.SetActive(true);
                                RectTransform protractorRectTransform = protractor.transform.parent.GetComponent<RectTransform>();

                                if (protractorInitialPosition == Vector2.zero)
                                    protractorInitialPosition = protractor.transform.parent.GetComponent<RectTransform>().position;
                                else
                                    protractorRectTransform.position = protractorInitialPosition;

                                protractorRectTransform.eulerAngles = new Vector3(0, 0, 0);
                            }
                            break;
                        case Type.Eraser:
                            eraserImage.gameObject.SetActive(true);
                            eraserImage.sizeDelta = new Vector2(EraserSize, EraserSize);

                            if (eraserSizeSlider != null)
                            {
                                eraserSizeSlider.gameObject.SetActive(true);
                                eraserSizeSlider.transform.DOKill();
                                eraserSizeSlider.transform.localScale = new Vector3(0.1f, 1f, 1f);
                                eraserSizeSlider.transform.DOScaleX(1f, 0.3f);
                            }
                            break;
                        case Type.Pen:
                            break;
                        case Type.Highlight:
                            break;
                        case Type.Line:
                            break;
                        case Type.Stamp:
                            break;
                        case Type.Compass:
                            {
                                compassImage.transform.parent.gameObject.SetActive(true);

                                _arc_add_mode = 1;
                                compassArcAddMode.Value = 1;
                                compassImage.gameObject.SetActive(false);
                                compassCenterPoint.gameObject.SetActive(false);
                                compassStartPoint.gameObject.SetActive(false);
                                compassEndPoint.gameObject.SetActive(false);
                            }
                            break;
                    }

                    if (type != Type.Ruler)
                        ruler.transform.parent.gameObject.SetActive(false);

                    if (type != Type.Protractor)
                        protractor.transform.parent.gameObject.SetActive(false);

                    if (type != Type.Compass)
                        compassImage.transform.parent.gameObject.SetActive(false);

                    if (type != Type.Eraser)
                    {
                        eraserImage.gameObject.SetActive(false);

                        if (eraserSizeSlider != null)
                        {
                            eraserSizeSlider.transform.DOKill();
                            eraserSizeSlider.transform.DOScaleX(0f, 0.3f);
                        }
                    }
                });


            rulerHorMove.GetComponent<Image>().OnPointerDownAsObservable()
                .Subscribe(_ =>
                {
                    Vector3 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));

                    rulerHorMoveOn = rulerHorMove.Contains(mousePos);

                    if (rulerHorMoveOn)
                    {
                        Image targetHorizontalMoveImage = rulerHorMove.GetComponent<Image>();
                        if (targetHorizontalMoveImage)
                            targetHorizontalMoveImage.sprite = rulerHorizontalMovePressSprite;

                        startDraggingMousePosition = mousePos;
                        firstRulerPosition = rulerRectTransform.position;
                    }
                })
                .AddTo(ruler.gameObject);


            ruler.Ready(() =>
            {
                if (rulerHorMoveOn)
                {
                    Vector2 unit = new Vector2(Mathf.Cos(rulerRectTransform.localEulerAngles.z * Mathf.Deg2Rad), Mathf.Sin(rulerRectTransform.localEulerAngles.z * Mathf.Deg2Rad));

                    Vector3 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));

                    float gap = Vector2.Dot((new Vector2(mousePos.x, mousePos.y) - startDraggingMousePosition), unit);

                    rulerRectTransform.position = firstRulerPosition + new Vector3(Mathf.Cos(rulerRectTransform.localEulerAngles.z * Mathf.Deg2Rad) * gap, Mathf.Sin(rulerRectTransform.localEulerAngles.z * Mathf.Deg2Rad) * gap, 0);
                }
            },
            null, null, true);
            ruler.transform.parent.GetComponent<RotateDraggableObjectComponent>().Ready(ruler.GetTarget(), null, null, null);

            rulerPen.Ready((draggedPenTransform =>
            {
                draggedPenTransform.GetWorldCorners(rectCorners);

                if (lineRenderer)
                {
                    lineRenderer.Points[1] = lineObject.transform.InverseTransformPoint(rectCorners[0]); // + UILineRendererOffset;
                    lineRenderer.SetAllDirty();
                }
            }),
            (draggedPenTransform) => // Begin Drag
            {
                draggedPenTransform.GetWorldCorners(rectCorners);
                startDraggingMousePosition = rectCorners[0];

                if (!lineObject)
                {
                    lineObject = new GameObject();
                    lineObject.transform.SetParent(sourceRawImage.transform.parent);
                    lineObject.transform.position = Vector3.zero;

                    //OffsetSetup();

                    lineObject.AddComponent<CanvasRenderer>();

                    Canvas lineObjectCanvas = lineObject.AddComponent<Canvas>();
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

                    RectTransform sourceImageParentTransform = sourceRawImage.transform.parent.GetComponent<RectTransform>();
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
                    lineRenderer.Points[0] = lineObject.transform.InverseTransformPoint(startDraggingMousePosition); // + UILineRendererOffset;
                }
            },
            () => // End Drag
            {
                bool somethingDrawn =
                    DrawLine(CurrentColor == Color.magenta ? Color.black : CurrentColor, startDraggingMousePosition, rectCorners[0], LineThick * scaleRatio);
                startDraggingMousePosition = Vector2.zero;

                if (somethingDrawn)
                {
                    SavePixels();
                    OnEndedSourceTextureEdit?.Invoke();
                }
                DestroyImmediate(lineRenderer.gameObject);
            }, false);




            protractor.Ready(null, null, null, true);
            protractor.transform.parent.GetComponent<RotateDraggableObjectComponent>().Ready(protractor.GetTarget(), null, null, null);

            protractorPen.Ready((draggedPenTransform =>
            {
                draggedPenTransform.GetWorldCorners(rectCorners);
                protractorPosition = (rectCorners[0] + rectCorners[3]) * 0.5f;
            }),
            (draggedPenTransform) => // Begin Drag
            {
                draggedPenTransform.GetWorldCorners(rectCorners);
                protractorPosition = (rectCorners[0] + rectCorners[3]) * 0.5f;
            },
            () => // End Drag
            {
                bool somethingDrawn =
                    DrawPoint(CurrentColor == Color.magenta ? Color.black : CurrentColor, protractorPosition, LineThick * scaleRatio);
                protractorPosition = Vector2.zero;

                if (somethingDrawn)
                {
                    SavePixels();
                    OnEndedSourceTextureEdit?.Invoke();
                }
            }, false);



            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    switch (CurrentType.Value)
                    {
                        case Type.None:
                            break;
                        case Type.Ruler:
                            if (Input.GetMouseButtonUp(0))
                            {
                                Image targetHorizontalMoveImage = rulerHorMove.GetComponent<Image>();
                                if (targetHorizontalMoveImage)
                                    targetHorizontalMoveImage.sprite = rulerHorizontalMoveNormalSprite;

                                rulerHorMoveOn = false;

                                if (lineRenderer != null)
                                    DestroyImmediate(lineRenderer.gameObject);
                            }
                            break;
                        case Type.Protractor:
                            break;
                        case Type.Eraser:
                            {
                                Vector3 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));

                                sourceTextureRenderRect.GetWorldCorners(rectCorners);
                                Vector2 newPosition = mousePos;

                                if (rectCorners[0].x > mousePos.x)
                                    newPosition.x = rectCorners[0].x;
                                else if (rectCorners[2].x < mousePos.x)
                                    newPosition.x = rectCorners[2].x;

                                if (rectCorners[0].y > mousePos.y)
                                    newPosition.y = rectCorners[0].y;
                                else if (rectCorners[2].y < mousePos.y)
                                    newPosition.y = rectCorners[2].y;

                                eraserImage.position = newPosition;
                            }
                            break;
                        case Type.Pen:
                            break;
                        case Type.Highlight:
                            break;
                        case Type.Line:
                            if (Input.GetMouseButtonUp(0))
                            {
                                if (lineRenderer != null)
                                    DestroyImmediate(lineRenderer.gameObject);
                            }
                            break;
                        case Type.Stamp:
                            break;
                        case Type.Compass:
                            if (Input.GetMouseButtonDown(0))
                            {
                                Vector3 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));
                                OnMouseDownForCompass(mousePos);
                            }
                            else if (Input.GetMouseButtonUp(0))
                            {
                                Vector3 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));
                                OnMouseUpForCompass(mousePos);
                            }
                            else if (Input.GetMouseButton(0))
                            {
                                Vector3 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));
                                OnMouseDragForCompass(mousePos);
                            }
                            break;
                        default:
                            break;
                    }
                }).AddTo(this);

            compassArcAddMode
                .DistinctUntilChanged()
                .Subscribe(changedMode =>
                {
                    switch (changedMode)
                    {
                        case 1:
                            if (!compassArcAddModeNoticeAlreadyShown[0])
                            {
                                compassArcAddModeNoticeAlreadyShown[0] = true;
                                compassArcModeNoticeImage.gameObject.SetActive(true);
                                compassArcModeNoticeText.text = compassArcAddModeNoticeMessages[0];
                            }
                            else
                            {
                                compassArcModeNoticeImage.gameObject.SetActive(false);
                            }
                            break;
                        case 2:
                            if (!compassArcAddModeNoticeAlreadyShown[1])
                            {
                                compassArcAddModeNoticeAlreadyShown[1] = true;
                                compassArcModeNoticeImage.gameObject.SetActive(true);
                                compassArcModeNoticeText.text = compassArcAddModeNoticeMessages[1];
                            }
                            else
                            {
                                compassArcModeNoticeImage.gameObject.SetActive(false);
                            }
                            break;
                        case 3:
                            if(!compassArcAddModeNoticeAlreadyShown[2])
                            {
                                compassArcAddModeNoticeAlreadyShown[2] = true;
                                compassArcModeNoticeImage.gameObject.SetActive(true);
                                compassArcModeNoticeText.text = compassArcAddModeNoticeMessages[2];
                            }
                            else
                            {
                                compassArcModeNoticeImage.gameObject.SetActive(false);
                            }
                            break;
                        
                        default:
                            compassArcModeNoticeImage.gameObject.SetActive(false);
                            break;
                    }

                    //Debug.Log(changedMode);
                });


            SubscribesMouseEvents();
        }

        public void RawImageSetting(int saveSlotNumber)
        {
            sourceTextureRenderRect = sourceRawImage.GetComponent<RectTransform>();

            if (sourceTextureRenderRect && saveData != null)
            {
                if (saveData.ContainsKey(saveSlotNumber))
                {
                    currentPixelZipList = saveData[saveSlotNumber];
                    Color32[] pixels = currentPixelZipList[currentPixelZipList.Count - 1].GetPixels32();

                    if (sourceRawImage.texture != null)
                    {
                        Texture2D texture2D = sourceRawImage.texture as Texture2D;
                        texture2D.Reinitialize(currentPixelZipList.width, currentPixelZipList.height);
                        sourceTexture = texture2D;
                    }
                    else
                    {
                        sourceTexture = new Texture2D(currentPixelZipList.width, currentPixelZipList.height);
                        sourceRawImage.texture = sourceTexture;
                    }

                    sourceTexture.SetPixels32(pixels);
                    sourceTexture.Apply();
                }
                else
                {
                    sourceTexture = new Texture2D((int)sourceTextureRenderRect.rect.width / 2, (int)sourceTextureRenderRect.rect.height / 2);
                    Color32[] clearColors = new Color32[sourceTexture.width * sourceTexture.height];
                    for (int i = 0; i < clearColors.Length; i++)
                    {
                        clearColors[i] = new Color32(255, 255, 255, 0);
                    }
                    sourceTexture.SetPixels32(clearColors);
                    sourceTexture.Apply();

                    sourceRawImage.texture = sourceTexture;

                    currentPixelZipList = new PixelZipList(sourceTexture.width, sourceTexture.height);
                    currentPixelZipList.Add(new PixelZip(sourceTexture.GetPixels32()));

                    saveData.Add(saveSlotNumber, currentPixelZipList);
                }

                sourceRawImage.color = Color.white;
                sourceRawImage.enabled = true;
            }
            else
            {
                Debug.Log("Raw Image is null");
                return;
            }
        }

        public Texture2D GetTexture() => sourceTexture;

        public bool DrawLine(Color color, Vector2 startPosition, Vector2 endPosition, float lineThick)
        {
            return sourceTexture.DrawLine(sourceTextureRenderRect.GetWorldSpaceRect(), color, startPosition, endPosition, lineThick);
        }
        public bool DrawPoint(Color color, Vector2 position, float size)
        {
            return sourceTexture.DrawPoint(sourceTextureRenderRect.GetWorldSpaceRect(), position, color, size);
        }
        public bool DrawHighlight(Color color, Vector2 startPosition, Vector2 endPosition, float height)
        {
            return sourceTexture.DrawHighlight(sourceTextureRenderRect.GetWorldSpaceRect(), color, startPosition, endPosition, height);
        }
        public bool DrawArc(Color color, Vector2 centerPoint, Vector2 startPoint, Vector2 endPoint, bool isClockwise, float lineThick)
        {
            return sourceTexture.DrawArc(sourceTextureRenderRect.GetWorldSpaceRect(), centerPoint, startPoint, endPoint, isClockwise, color, lineThick);
        }
        public bool Erase(Vector2 startPosition, Vector2 endPosition, float size)
        {
            if (Vector2.Distance(startPosition, endPosition) < 5f * scaleRatio)
            {
                return sourceTexture.Erase(sourceTextureRenderRect.GetWorldSpaceRect(), endPosition, size);
            }
            else
            {
                return sourceTexture.Erase(sourceTextureRenderRect.GetWorldSpaceRect(), startPosition, endPosition, size);
            }
        }


        public void SetItemToggleOn(DrawingManager.Type type, bool isOn)
        {
            itemToggles.FirstOrDefault(o => o.DrawingType == type).gameObject.SetActive(isOn);
        }


        public void ClearAll()
        {
            if (sourceTexture)
            {
                Color32[] pixels = sourceTexture.GetPixels32();

                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color32(255, 255, 255, 0);
                }

                sourceTexture.SetPixels32(pixels);
                sourceTexture.Apply();
            }
        }

        private void SubscribesMouseEvents()
        {
            if (!sourceRawImage.gameObject.GetComponent<ObservableBeginDragTrigger>())
                sourceRawImage.gameObject.AddComponent<ObservableBeginDragTrigger>();

            if (!sourceRawImage.gameObject.GetComponent<ObservableDragTrigger>())
                sourceRawImage.gameObject.AddComponent<ObservableDragTrigger>();

            if (!sourceRawImage.gameObject.GetComponent<ObservableEndDragTrigger>())
                sourceRawImage.gameObject.AddComponent<ObservableEndDragTrigger>();

            if (!sourceRawImage.gameObject.GetComponent<ObservablePointerUpTrigger>())
                sourceRawImage.gameObject.AddComponent<ObservablePointerUpTrigger>();

            uiDisposables.Add(
                sourceRawImage
                .OnPointerDownAsObservable()
                .Subscribe(eventData =>
                {
                    Vector3 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));

                    switch (CurrentType.Value)
                    {
                        case Type.Ruler:
                            break;
                        case Type.Protractor:
                            break;
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
                        case Type.Highlight:
                            startDraggingMousePosition = mousePos;
                            somethingDrawnInDragging = false;
                            break;
                        case Type.Line:
                            startDraggingMousePosition = mousePos;

                            if (!lineObject)
                            {
                                lineObject = new GameObject();
                                lineObject.transform.SetParent(sourceRawImage.transform.parent);
                                lineObject.transform.position = Vector3.zero;

                                //OffsetSetup();

                                lineObject.AddComponent<CanvasRenderer>();

                                Canvas lineObjectCanvas = lineObject.AddComponent<Canvas>();
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

                                RectTransform sourceImageParentTransform = sourceRawImage.transform.parent.GetComponent<RectTransform>();
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
                                lineRenderer.Points[0] = lineObject.transform.InverseTransformPoint(startDraggingMousePosition); // + UILineRendererOffset;
                                lineRenderer.Points[1] = lineObject.transform.InverseTransformPoint(startDraggingMousePosition); // + UILineRendererOffset;
                            }
                            break;
                        case Type.Stamp:
                            break;
                        case Type.Compass:
                            //OnMouseDownForCompass(mousePos, false);
                            break;
                    }
                }));

            uiDisposables.Add(sourceRawImage
                .gameObject
                .GetComponent<ObservableDragTrigger>()
                .OnDragAsObservable()
                .Subscribe(eventData =>
                {
                    Vector3 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));

                    switch (CurrentType.Value)
                    {
                        case Type.Ruler:
                            break;
                        case Type.Protractor:
                            break;
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
                                    DrawLine(CurrentColor == Color.magenta ? Color.black : CurrentColor, startDraggingMousePosition, mousePos, LineThick * scaleRatio);
                                startDraggingMousePosition = mousePos;

                                somethingDrawnInDragging = somethingDrawnInDragging || somethingDrawn;
                            }
                            break;
                        case Type.Highlight:
                            {
                                Vector3 horPosition = mousePos;
                                horPosition.y = startDraggingMousePosition.y;

                                bool somethingDrawn =
                                    DrawHighlight(CurrentColor == Color.magenta ? Color.green : CurrentColor, startDraggingMousePosition, horPosition, HighlightThick * scaleRatio);
                                startDraggingMousePosition = horPosition;

                                somethingDrawnInDragging = somethingDrawnInDragging || somethingDrawn;
                            }
                            break;
                        case Type.Line:
                            if (lineRenderer)
                            {
                                lineRenderer.Points[1] = lineObject.transform.InverseTransformPoint(mousePos); // + UILineRendererOffset;
                                lineRenderer.SetAllDirty();
                            }
                            break;
                        case Type.Stamp:
                            break;
                        case Type.Compass:
                            //OnMouseDragForCompass(mousePos);
                            break;
                    }
                }).AddTo(sourceRawImage));

            uiDisposables.Add(sourceRawImage
                .gameObject
                .GetComponent<ObservableEndDragTrigger>()
                .OnEndDragAsObservable()
                .Subscribe(eventData =>
                {
                    Vector3 mousePos = canvasRenderModeIsOverlay ? ChungdamScaler.CurrentMousePosition : mainCamera.ScreenToWorldPoint(new Vector3(ChungdamScaler.CurrentMousePosition.x, ChungdamScaler.CurrentMousePosition.y, cameraDistance));

                    switch (CurrentType.Value)
                    {
                        case Type.Ruler:
                            break;
                        case Type.Protractor:
                            break;
                        case Type.Eraser:
                            {
                                bool somethingDrawn =
                                    Erase(startDraggingMousePosition, mousePos, EraserSize * scaleRatio);
                                startDraggingMousePosition = Vector2.zero;

                                somethingDrawnInDragging = somethingDrawnInDragging || somethingDrawn;

                                if (somethingDrawnInDragging)
                                {
                                    OnEndedSourceTextureEdit?.Invoke();
                                    SavePixels();
                                }
                            }
                            break;
                        case Type.Pen:
                            {
                                bool somethingDrawn =
                                    DrawLine(CurrentColor == Color.magenta ? Color.black : CurrentColor, startDraggingMousePosition, mousePos, LineThick * scaleRatio);
                                startDraggingMousePosition = Vector2.zero;

                                somethingDrawnInDragging = somethingDrawnInDragging || somethingDrawn;

                                if (somethingDrawnInDragging)
                                {
                                    OnEndedSourceTextureEdit?.Invoke();
                                    SavePixels();
                                }
                            }
                            break;
                        case Type.Highlight:
                            {
                                Vector3 horPosition = mousePos;
                                horPosition.y = startDraggingMousePosition.y;

                                bool somethingDrawn =
                                    DrawHighlight(CurrentColor == Color.magenta ? Color.green : CurrentColor, startDraggingMousePosition, horPosition, HighlightThick * scaleRatio);
                                startDraggingMousePosition = Vector2.zero;

                                somethingDrawnInDragging = somethingDrawnInDragging || somethingDrawn;

                                if (somethingDrawnInDragging)
                                {
                                    OnEndedSourceTextureEdit?.Invoke();
                                    SavePixels();
                                }
                            }
                            break;
                        case Type.Line:
                            {
                                bool somethingDrawn =
                                    DrawLine(CurrentColor == Color.magenta ? Color.black : CurrentColor, startDraggingMousePosition, mousePos, LineThick * scaleRatio);
                                startDraggingMousePosition = Vector2.zero;

                                DestroyImmediate(lineRenderer.gameObject);

                                somethingDrawnInDragging = somethingDrawnInDragging || somethingDrawn;

                                if (somethingDrawnInDragging)
                                {
                                    OnEndedSourceTextureEdit?.Invoke();
                                    SavePixels();
                                }
                            }
                            break;
                        case Type.Stamp:
                            break;
                        case Type.Compass:
                            //OnMouseUpForCompass(mousePos);
                            break;
                    }
                }));

            uiDisposables.Add(
                sourceRawImage
                .gameObject
                .GetComponent<ObservablePointerUpTrigger>()
                .OnPointerUpAsObservable()
                .Subscribe(eventData =>
                {
                    switch (CurrentType.Value)
                    {
                        case Type.Ruler:
                            break;
                        case Type.Protractor:
                            break;
                        case Type.Eraser:
                            {
                                if (somethingDrawnInDragging)
                                {
                                    OnEndedSourceTextureEdit?.Invoke();
                                    SavePixels();
                                }
                            }
                            break;
                        case Type.Pen:
                            break;
                        case Type.Highlight:
                            break;
                        case Type.Line:
                            break;
                        case Type.Stamp:
                            break;
                        case Type.Compass:
                            break;
                    }
                }));
        }

        //private void OffsetSetup()
        //{
        //    if (canvasRenderModeIsOverlay)
        //        UILineRendererOffset = Vector3.zero;
        //    else
        //    {
        //        if (mainCamera.orthographic)
        //            UILineRendererOffset = new Vector3(LineThick, LineThick) * scaleRatio * 10f * 0.8f;
        //        else
        //            UILineRendererOffset = new Vector3(LineThick, LineThick) * scaleRatio * 0.8f;
        //    }
        //}

        private void OnMouseDownForCompass(Vector2 mousePosition)
        {
            // ���۽�
            if (_arc_add_mode == 1 && sourceTextureRenderRect.Contains(mousePosition))
            {
                //���� �� Ŭ�� ���

                Arc_Item new_arc = currentArcItem;
                //new_arc.CenterPoint = deScaleV2(gameVariables.GAME_COMMONS.FIRST_POS, gameVariables);
                new_arc.CenterPoint = mousePosition;

                compassCenterPoint.sizeDelta = new Vector2(CompassPointSize, CompassPointSize);
                compassCenterPoint.position = new_arc.CenterPoint;
                compassCenterPoint.gameObject.SetActive(true);


                //�������� �߰��� �ε����� �����Ѵ�.

                //���� �� Ŭ�� ��� ���·� ��ȯ�Ѵ�.
                _arc_add_mode = 2;
                compassArcAddMode.Value = 2;
                //MessageBoxController.Instance.Create("StartingCircleDrawingPos", true).Show(MessageBoxDefaultPositionType.Top, MessageBoxButtonType.AutoClosing, MessageBoxStateType.OnlyMessage, MessageBoxSizeType.Small,
                //                                                                            "���� �׸��� ������ ���� ���ϼ���.", 2.5f);

            }
            else if (_arc_add_mode == 2)
            {
                //���� �� Ŭ�� ���
                Arc_Item arc_item = currentArcItem;

                arc_item.StartPoint = mousePosition;

                compassStartPoint.sizeDelta = new Vector2(CompassPointSize, CompassPointSize);
                compassStartPoint.position = arc_item.StartPoint;
                compassStartPoint.gameObject.SetActive(true);

                float length = Vector2.Distance(arc_item.CenterPoint, arc_item.StartPoint) / scaleRatio;
                compassImage.sizeDelta = new Vector2(length, length * 0.1f);
                compassImage.position = arc_item.CenterPoint;
                compassImage.eulerAngles = new Vector3(0, 0, Vector2.SignedAngle(Vector2.right, arc_item.StartPoint - arc_item.CenterPoint));
                compassImage.gameObject.SetActive(true);


                //�� �� Ŭ�� ��� ���·� ��ȯ�Ѵ�.
                _arc_add_mode = 3;
                compassArcAddMode.Value = 3;

                //isBegin = true;
            }
            else if (_arc_add_mode == 3)
            {
                //���� �� Ŭ�� ���
                Arc_Item arc_item = currentArcItem;

                if (compassCenterPoint.Contains(mousePosition))
                {
                    _arc_add_mode = 1;
                    compassArcAddMode.Value = 1;
                    compassImage.gameObject.SetActive(false);
                    compassCenterPoint.gameObject.SetActive(false);
                    compassStartPoint.gameObject.SetActive(false);
                    compassEndPoint.gameObject.SetActive(false);

                    //MessageBoxController.Instance.Create("SelectingCircleCenter", true).Show(MessageBoxDefaultPositionType.Top, MessageBoxButtonType.AutoClosing, MessageBoxStateType.OnlyMessage, MessageBoxSizeType.Small,
                    //                                                                        "���� �߽��� ���ϼ���.", 2.5f);
                }
                else if (compassStartPoint.Contains(mousePosition))
                {
                    _arc_add_mode = 4;
                    compassArcAddMode.Value = 4;
                    compassEndPoint.sizeDelta = new Vector2(CompassPointSize, CompassPointSize);
                    compassEndPoint.position = arc_item.StartPoint;
                    compassEndPoint.gameObject.SetActive(true);

                    //somethingDrawn = false;
                }
                else if (compassImage.Contains(mousePosition))
                {
                    _arc_add_mode = 5;
                    compassArcAddMode.Value = 5;
                    mousePointToCenter = arc_item.CenterPoint - mousePosition;
                    mousePointToStartPoint = arc_item.StartPoint - mousePosition;
                }
            }
        }

        private void OnMouseDragForCompass(Vector2 mousePosition)
        {
            if (_arc_add_mode == 4)
            {
                //�� �� Ŭ�� ���
                Arc_Item arc_item = currentArcItem;

                arc_item.EndPoint = mousePosition;

                Vector2 f_vec = arc_item.StartPoint - arc_item.CenterPoint;
                Vector2 e_vec = arc_item.EndPoint - arc_item.CenterPoint;

                if ((V2Angle(f_vec) - V2Angle(e_vec) - arc_item.length) > 180)
                {
                    if (V2Angle(f_vec) - V2Angle(e_vec) - arc_item.length > 540)
                        arc_item.length = V2Angle(f_vec) - V2Angle(e_vec) - 720;
                    else
                        arc_item.length = V2Angle(f_vec) - V2Angle(e_vec) - 360;
                }
                else if ((V2Angle(f_vec) - V2Angle(e_vec) - arc_item.length) < -180)
                {
                    if (V2Angle(f_vec) - V2Angle(e_vec) - arc_item.length < -540)
                        arc_item.length = V2Angle(f_vec) - V2Angle(e_vec) + 720;
                    else
                        arc_item.length = V2Angle(f_vec) - V2Angle(e_vec) + 360;
                }
                else
                {
                    arc_item.length = V2Angle(f_vec) - V2Angle(e_vec);
                }

                arc_item.isClockwise = (arc_item.length >= 0);


                arc_item.EndPoint = CalculateArcPoints(arc_item.CenterPoint, arc_item.StartPoint, arc_item.EndPoint, arc_item.isClockwise, arc_item.length);


                compassEndPoint.position = arc_item.EndPoint;
                compassImage.eulerAngles = new Vector3(0, 0, Vector2.SignedAngle(Vector2.right, arc_item.EndPoint - arc_item.CenterPoint));
            }
            else if (_arc_add_mode == 5)
            {
                Arc_Item arc_item = currentArcItem;

                arc_item.CenterPoint = mousePosition + mousePointToCenter;

                arc_item.StartPoint = mousePosition + mousePointToStartPoint;

                compassImage.position = arc_item.CenterPoint;
                compassStartPoint.position = arc_item.StartPoint;

                compassCenterPoint.gameObject.SetActive(false);
            }
        }

        private void OnMouseUpForCompass(Vector2 mousePosition)
        {
            if (_arc_add_mode == 4)
            {
                //�� �� Ŭ�� ���
                Arc_Item arc_item = currentArcItem;

                Vector2 last_point = CalculateArcPoints(arc_item.CenterPoint, arc_item.StartPoint, arc_item.EndPoint, arc_item.isClockwise, arc_item.length);

                arc_item.EndPoint.x = last_point.x;
                arc_item.EndPoint.y = last_point.y;

                //�������� �Ÿ��� ���߾� ������ ��ġ�� �����Ѵ�.

                _arc_add_mode = 3;
                compassArcAddMode.Value = 3;

                //�ʱ� ���·� ��ȯ�Ѵ�.
                bool somethingDrawn =
                    DrawArc(CurrentColor == Color.magenta ? Color.black : CurrentColor, arc_item.CenterPoint, arc_item.StartPoint, arc_item.EndPoint, arc_item.isClockwise, LineThick * scaleRatio);


                if (somethingDrawn)
                {
                    SavePixels();
                    OnEndedSourceTextureEdit?.Invoke();
                }

                arc_item.StartPoint = arc_item.EndPoint;

                arc_item.EndPoint = new Vector2(-1, -1);
                arc_item.length = 0f;


                compassStartPoint.position = arc_item.StartPoint;
                compassEndPoint.gameObject.SetActive(false);

                DestroyImmediate(lineRenderer.gameObject);

                //_added_arc_list.Remove(arc_item);
            }
            else if (_arc_add_mode == 5)
            {
                Arc_Item arc_item = currentArcItem;

                arc_item.CenterPoint = mousePosition + mousePointToCenter;

                arc_item.StartPoint = mousePosition + mousePointToStartPoint;

                compassImage.position = arc_item.CenterPoint;
                compassCenterPoint.position = arc_item.CenterPoint;
                compassStartPoint.position = arc_item.StartPoint;

                compassCenterPoint.gameObject.SetActive(true);

                _arc_add_mode = 3;
                compassArcAddMode.Value = 3;
            }
        }


        private class Arc_Item
        {
            public Vector2 CenterPoint = new Vector2(-1, -1);
            public Vector2 StartPoint = new Vector2(-1, -1);
            public Vector2 EndPoint = new Vector2(-1, -1);
            public bool isClockwise;
            public float length;
        }

        private float V2Angle(Vector2 p_vector2)
        {
            if (p_vector2.x < 0)
            {
                return 360 - (Mathf.Atan2(p_vector2.x, p_vector2.y) * Mathf.Rad2Deg * -1);
            }
            else
            {
                return Mathf.Atan2(p_vector2.x, p_vector2.y) * Mathf.Rad2Deg;
            }
        }

        private Vector2 CalculateArcPoints(Vector2 center_point, Vector2 start_point, Vector2 end_point, bool isClock, float distance)
        {
            Vector2 v2_start = start_point - center_point;
            Vector2 v2_end = end_point - center_point;

            float ang_start = V2Angle(v2_start);
            float ang_end = V2Angle(v2_end);

            float radius = Vector2.Distance(start_point, center_point);


            if (!lineObject)
            {
                lineObject = new GameObject();
                lineObject.transform.SetParent(sourceRawImage.transform.parent);
                lineObject.transform.position = Vector3.zero;

                //OffsetSetup();

                lineObject.AddComponent<CanvasRenderer>();

                Canvas lineObjectCanvas = lineObject.AddComponent<Canvas>();
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
            }



            List<Vector2> points = new List<Vector2>();

            if (Mathf.Abs(distance) >= 360)
            {

                for (float i = ang_start; i <= ang_start + 360 + 1; i = i + 5)
                {
                    float deg1 = i - 90;

                    float angle1 = deg1 * Mathf.Deg2Rad;

                    float x1 = (float)(radius * System.Math.Cos(angle1));
                    float y1 = (float)(-radius * System.Math.Sin(angle1));

                    points.Add(new Vector2(x1 + center_point.x, y1 + center_point.y));
                }

            }
            else
            {
                if (isClock)
                {
                    if (ang_start < ang_end)
                        ang_end = ang_end - 360;

                    for (float i = ang_start; i >= ang_end - 5; i = i - 5)
                    {
                        float deg1 = i - 90;

                        if (i < ang_end)
                            deg1 = ang_end - 90;

                        float angle1 = deg1 * Mathf.Deg2Rad;

                        float x1 = (float)(radius * System.Math.Cos(angle1));
                        float y1 = (float)(-radius * System.Math.Sin(angle1));


                        points.Add(new Vector2(x1 + center_point.x, y1 + center_point.y));

                    }

                }
                else
                {
                    if (ang_start > ang_end)
                        ang_end = ang_end + 360;

                    for (float i = ang_start; i <= ang_end + 5; i = i + 5)
                    {
                        float deg1 = i - 90;

                        if (i > ang_end)
                            deg1 = ang_end - 90;

                        float angle1 = deg1 * Mathf.Deg2Rad;

                        float x1 = (float)(radius * System.Math.Cos(angle1));
                        float y1 = (float)(-radius * System.Math.Sin(angle1));

                        points.Add(new Vector2(x1 + center_point.x, y1 + center_point.y));

                    }

                }

            }


            if (lineRenderer)
            {
                Vector2[] rendererPoints = new Vector2[points.Count];

                for (int i = 0; i < points.Count; i++)
                {
                    rendererPoints[i] = lineObject.transform.InverseTransformPoint(points[i]); // + UILineRendererOffset;
                }

                lineRenderer.Points = rendererPoints;
            }

            return points[points.Count - 1];

        }
        public void SetRulerSize(int size)
        {
            if (size <= 1)
                return;

            List<Transform> deleteTargets = ruler.transform.GetComponentsInChildren<Transform>().Where(o => o.name == "CreatedRuler" || o.name == "CreatedText").ToList();
            foreach (Transform deleteTarget in deleteTargets)
            {
                DestroyImmediate(deleteTarget.gameObject);
            }


            float currentPosition = MoveDraggableObjectComponentForRulerPen.RulerMin - 21;
            float gap = 25f / (size - 1);
            float unitLength = (MoveDraggableObjectComponentForRulerPen.RulerMax - MoveDraggableObjectComponentForRulerPen.RulerMin) / size;

            for (int i = 0; i <= size; i++)
            {
                if (i != size)
                {
                    Image createdImage = Instantiate(rulerSizeLine);
                    Text createdText = Instantiate(rulerSizeText);

                    createdImage.gameObject.SetActive(true);
                    createdImage.transform.name = "CreatedRuler";
                    createdText.gameObject.SetActive(true);
                    createdText.transform.name = "CreatedText";

                    createdImage.transform.SetParent(ruler.transform);
                    createdText.transform.SetParent(ruler.transform);

                    createdImage.transform.localEulerAngles = Vector3.zero;
                    createdText.transform.localEulerAngles = Vector3.zero;

                    RectTransform createdImageRect = createdImage.GetComponent<RectTransform>();
                    createdImageRect.sizeDelta = new Vector2(unitLength + gap, 30);
                    createdImageRect.localScale = Vector3.one;

                    createdImage.transform.localPosition = new Vector3(currentPosition, 50, 0);


                    RectTransform createdTextRect = createdText.GetComponent<RectTransform>();
                    //createdTextRect.sizeDelta = new Vector2()
                    createdTextRect.localScale = Vector3.one;
                    createdText.transform.localPosition = new Vector3(currentPosition, 0, 0);
                    createdText.text = i.ToString();

                    //currentPosition += unitLength;
                }
                else
                {
                    Text createdText = Instantiate(rulerSizeText);
                    createdText.gameObject.SetActive(true);
                    createdText.transform.SetParent(ruler.transform);
                    createdText.transform.name = "CreatedText";

                    createdText.transform.localEulerAngles = Vector3.zero;

                    RectTransform createdTextRect = createdText.GetComponent<RectTransform>();
                    //createdTextRect.sizeDelta = new Vector2()
                    createdTextRect.localScale = Vector3.one;
                    createdText.transform.localPosition = new Vector3(currentPosition + 2, 0, 0);
                    createdText.text = i.ToString();
                }
                currentPosition += unitLength;
            }
        }


        public void SetRawImageObject(RawImage rawImage, int saveSlotNumber)
        {
            sourceRawImage = rawImage;

            foreach (IDisposable disposable in uiDisposables)
                disposable.Dispose();

            uiDisposables.Clear();
            SubscribesMouseEvents();

            RawImageSetting(saveSlotNumber);
        }


        public struct PixelZip
        {
            private uint[] zipPixels;

            public PixelZip(Color32[] pixels)
            {
                List<uint> intList = new List<uint>();

                uint count = 0;

                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a == 0)
                        count++;
                    else
                    {
                        if (count > 0)
                        {
                            count <<= 8;
                            intList.Add(count);
                            count = 0;
                        }

                        uint int32 = pixels[i].r;
                        int32 <<= 8;
                        int32 += pixels[i].g;
                        int32 <<= 8;
                        int32 += pixels[i].b;
                        int32 <<= 8;
                        int32 += pixels[i].a;

                        intList.Add(int32);
                    }
                }

                if (count > 0)
                {
                    count <<= 8;
                    intList.Add(count);
                }

                zipPixels = intList.ToArray();
            }

            public Color32[] GetPixels32()
            {
                List<Color32> colorList = new List<Color32>();

                for (int i = 0; i < zipPixels.Length; i++)
                {
                    uint rgb = zipPixels[i] >> 8;
                    byte alpha = (byte)zipPixels[i];

                    if (alpha == 0)
                    {
                        for (int k = 0; k < rgb; k++)
                        {
                            colorList.Add(new Color32(255, 255, 255, 0));
                        }
                    }
                    else
                    {
                        colorList.Add(new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, alpha));
                    }
                }

                return colorList.ToArray();
            }

            public int GetLength() => zipPixels.Length;
        }

        public class PixelZipList : List<PixelZip>
        {
            public int width;
            public int height;

            public PixelZipList(int width, int height)
            {
                this.width = width;
                this.height = height;
            }
        }

        public void SavePixels()
        {
            currentPixelZipList.Add(new PixelZip(sourceTexture.GetPixels32()));
            somethingDrawnInDragging = false;
        }


        public int GetCurrentSavePageCount()
        {
            return currentPixelZipList.Count;
        }

        public long TotalSaveDataMemory()
        {
            long count = 0;

            foreach (var item in saveData)
            {
                for (int i = 0; i < item.Value.Count; i++)
                {
                    count += item.Value[i].GetLength();
                }
            }

            return count;
        }

        public void RefreshCurrentRawImage()
        {
            PixelZip thisPixelZip = currentPixelZipList[0];
            sourceTexture.SetPixels32(thisPixelZip.GetPixels32());
            sourceTexture.Apply();

            while (currentPixelZipList.Count > 1)
            {
                currentPixelZipList.RemoveAt(1);
            }
        }

        public void RefreshAllRawImage()
        {
            RefreshCurrentRawImage();

            foreach (var item in saveData)
            {
                PixelZipList pixelZipList = item.Value;

                while (pixelZipList.Count > 1)
                {
                    pixelZipList.RemoveAt(1);
                }
            }
        }
    }

}

