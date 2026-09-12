using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMS.Util.Extend
{
    using CMS.Util.ScreenInfo;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public enum AnchorPresets
    {
        TopLeft,
        TopCenter,
        TopRight,

        MiddleLeft,
        MiddleCenter,
        MiddleRight,

        BottomLeft,
        BottonCenter,
        BottomRight,
        BottomStretch,

        VertStretchLeft,
        VertStretchRight,
        VertStretchCenter,

        HorStretchTop,
        HorStretchMiddle,
        HorStretchBottom,

        StretchAll
    }

    public enum PivotPresets
    {
        TopLeft,
        TopCenter,
        TopRight,

        MiddleLeft,
        MiddleCenter,
        MiddleRight,

        BottomLeft,
        BottomCenter,
        BottomRight,
    }

    public static class ExtendRectTransform
    {
        private static readonly Vector3[] corners = new Vector3[4];

        internal static Rect GetScreenRect(this RectTransform self, PointerEventData data)
        {
            return self.GetScreenRect(data.pressEventCamera);
        }

        internal static Rect GetScreenRect(this RectTransform self)
        {
            var canvas = self.GetComponentInParent<Canvas>();
            return self.GetScreenRect(canvas.worldCamera);
        }

        internal static Rect GetScreenRect(this RectTransform self, Camera camera)
        {
            self.GetWorldCorners(corners);
            if (camera != null)
            {
                corners[0] = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                corners[2] = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            }

            var rect = new Rect
            {
                x = corners[0].x,
                y = corners[0].y
            };
            rect.width = corners[2].x - rect.x;
            rect.height = corners[2].y - rect.y;
            return rect;
        }

        internal static void SetAnchor(this RectTransform source, AnchorPresets allign, int offsetX = 0, int offsetY = 0)
        {
            source.anchoredPosition = new Vector3(offsetX, offsetY, 0);

            switch (allign)
            {
                case (AnchorPresets.TopLeft):
                    {
                        source.anchorMin = new Vector2(0, 1);
                        source.anchorMax = new Vector2(0, 1);
                        break;
                    }
                case (AnchorPresets.TopCenter):
                    {
                        source.anchorMin = new Vector2(0.5f, 1);
                        source.anchorMax = new Vector2(0.5f, 1);
                        break;
                    }
                case (AnchorPresets.TopRight):
                    {
                        source.anchorMin = new Vector2(1, 1);
                        source.anchorMax = new Vector2(1, 1);
                        break;
                    }

                case (AnchorPresets.MiddleLeft):
                    {
                        source.anchorMin = new Vector2(0, 0.5f);
                        source.anchorMax = new Vector2(0, 0.5f);
                        break;
                    }
                case (AnchorPresets.MiddleCenter):
                    {
                        source.anchorMin = new Vector2(0.5f, 0.5f);
                        source.anchorMax = new Vector2(0.5f, 0.5f);
                        break;
                    }
                case (AnchorPresets.MiddleRight):
                    {
                        source.anchorMin = new Vector2(1, 0.5f);
                        source.anchorMax = new Vector2(1, 0.5f);
                        break;
                    }

                case (AnchorPresets.BottomLeft):
                    {
                        source.anchorMin = new Vector2(0, 0);
                        source.anchorMax = new Vector2(0, 0);
                        break;
                    }
                case (AnchorPresets.BottonCenter):
                    {
                        source.anchorMin = new Vector2(0.5f, 0);
                        source.anchorMax = new Vector2(0.5f, 0);
                        break;
                    }
                case (AnchorPresets.BottomRight):
                    {
                        source.anchorMin = new Vector2(1, 0);
                        source.anchorMax = new Vector2(1, 0);
                        break;
                    }

                case (AnchorPresets.HorStretchTop):
                    {
                        source.anchorMin = new Vector2(0, 1);
                        source.anchorMax = new Vector2(1, 1);
                        break;
                    }
                case (AnchorPresets.HorStretchMiddle):
                    {
                        source.anchorMin = new Vector2(0, 0.5f);
                        source.anchorMax = new Vector2(1, 0.5f);
                        break;
                    }
                case (AnchorPresets.HorStretchBottom):
                    {
                        source.anchorMin = new Vector2(0, 0);
                        source.anchorMax = new Vector2(1, 0);
                        break;
                    }

                case (AnchorPresets.VertStretchLeft):
                    {
                        source.anchorMin = new Vector2(0, 0);
                        source.anchorMax = new Vector2(0, 1);
                        break;
                    }
                case (AnchorPresets.VertStretchCenter):
                    {
                        source.anchorMin = new Vector2(0.5f, 0);
                        source.anchorMax = new Vector2(0.5f, 1);
                        break;
                    }
                case (AnchorPresets.VertStretchRight):
                    {
                        source.anchorMin = new Vector2(1, 0);
                        source.anchorMax = new Vector2(1, 1);
                        break;
                    }

                case (AnchorPresets.StretchAll):
                    {
                        source.anchorMin = new Vector2(0, 0);
                        source.anchorMax = new Vector2(1, 1);
                        break;
                    }
            }
        }

        internal static void SetPivot(this RectTransform source, PivotPresets preset)
        {

            switch (preset)
            {
                case (PivotPresets.TopLeft):
                    {
                        source.pivot = new Vector2(0, 1);
                        break;
                    }
                case (PivotPresets.TopCenter):
                    {
                        source.pivot = new Vector2(0.5f, 1);
                        break;
                    }
                case (PivotPresets.TopRight):
                    {
                        source.pivot = new Vector2(1, 1);
                        break;
                    }

                case (PivotPresets.MiddleLeft):
                    {
                        source.pivot = new Vector2(0, 0.5f);
                        break;
                    }
                case (PivotPresets.MiddleCenter):
                    {
                        source.pivot = new Vector2(0.5f, 0.5f);
                        break;
                    }
                case (PivotPresets.MiddleRight):
                    {
                        source.pivot = new Vector2(1, 0.5f);
                        break;
                    }

                case (PivotPresets.BottomLeft):
                    {
                        source.pivot = new Vector2(0, 0);
                        break;
                    }
                case (PivotPresets.BottomCenter):
                    {
                        source.pivot = new Vector2(0.5f, 0);
                        break;
                    }
                case (PivotPresets.BottomRight):
                    {
                        source.pivot = new Vector2(1, 0);
                        break;
                    }
            }
        }

        internal static bool IsRectTransformInsideScreen(this RectTransform rectTransform)
        {
            bool isInside = false;
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            int visibleCorners = 0;
            Rect rect = new Rect(0, 0, Screen.width, Screen.height);
            foreach (Vector3 corner in corners)
            {
                if (rect.Contains(corner))
                {
                    visibleCorners++;
                }
            }
            if (visibleCorners == 4)
            {
                isInside = true;
            }
            return isInside;
        }

        internal static bool IsRectTransformInsideScreen(this RectTransform rectTransform, RectTransform checkingRootArea)
        {
            //renderCamera.pixelRect

            bool isInside = false;
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            int visibleCorners = 0;
            Rect rect = checkingRootArea.GetWorldSpaceRect();
            foreach (Vector3 corner in corners)
            {
                if (rect.Contains(corner))
                {
                    visibleCorners++;
                }
            }
            if (visibleCorners == 4)
            {
                isInside = true;
            }
            return isInside;
        }

        internal static Rect GetWorldSpaceRect(this RectTransform rectTransform)
        {
            Rect sourceRect = rectTransform.rect;
            sourceRect.position = rectTransform.TransformPoint(sourceRect.position);
            sourceRect.size = rectTransform.TransformVector(sourceRect.size);

            return sourceRect;
        }

        internal static bool Contains(this RectTransform rectTransform, Vector3 position, bool isOnCamera = false)
        {
            if (!isOnCamera)
            {
                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);

                if (Vector2.Dot(corners[1] - corners[0], position - corners[0]) >= 0 &&
                    Vector2.Dot(corners[2] - corners[1], position - corners[1]) >= 0 &&
                    Vector2.Dot(corners[3] - corners[2], position - corners[2]) >= 0 &&
                    Vector2.Dot(corners[0] - corners[3], position - corners[3]) >= 0)
                    return true;

                return false;
            }
            else
            {
                position.z = 100;
                Vector3 newPos = ScreenUtils.CurrentSceneCamera.ScreenToWorldPoint(position);

                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);

                if (Vector2.Dot(corners[1] - corners[0], newPos - corners[0]) >= 0 &&
                    Vector2.Dot(corners[2] - corners[1], newPos - corners[1]) >= 0 &&
                    Vector2.Dot(corners[3] - corners[2], newPos - corners[2]) >= 0 &&
                    Vector2.Dot(corners[0] - corners[3], newPos - corners[3]) >= 0)
                    return true;

                return false;
            }
        }
    }

}
