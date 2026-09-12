using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMS.Util.Extend
{
    public static class ExtendColor
    {
        /// <summary>
        /// Base64 String Convert / 테스트 완료 (w09-05 컨텐츠)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        internal static string ToStringBytes(this Texture2D source, bool isUsingPNG)
        {
            string result = "";
            byte[] sourceBytes = null;
            //Stopwatch check = new Stopwatch();
            //check.Start();
            if (isUsingPNG)
                sourceBytes = source.EncodeToPNG();

            // 2021.9.28 JBK : JPG Quality = 30
            else
                sourceBytes = source.EncodeToJPG(30);
            //check.Stop();

            //UnityEngine.Debug.Log($"Encode : {check.ElapsedMilliseconds}");
            //check.Reset();
            if (sourceBytes != null && sourceBytes.Length > 0)
            {
                //check.Start();
                result = System.Convert.ToBase64String(sourceBytes);
                //check.Stop();

                //UnityEngine.Debug.Log($"Base64Conv : {check.ElapsedMilliseconds}");
            }
            return result;
        }

        /// <summary>
        /// Use only Base64 String /  테스트 완료 (w09-05 컨텐츠)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        internal static Texture2D ToTexture(this string source)
        {
            Texture2D result = new Texture2D(1, 1);
            result.name = "string To Texture2D ParseTexture";
            byte[] sourceBytes = System.Convert.FromBase64String(source);

            bool isSuccess = result.LoadImage(sourceBytes);
            result.Apply();

            if (isSuccess)
                return result;
            else
                return null;
        }

        internal static Color ToBlendLine(this Color baseColor, Color upperColor)
        {
            float r = baseColor.r * (1f - upperColor.a) + upperColor.r * upperColor.a;
            float g = baseColor.g * (1f - upperColor.a) + upperColor.g * upperColor.a;
            float b = baseColor.b * (1f - upperColor.a) + upperColor.b * upperColor.a;
            float a = baseColor.a > upperColor.a ? baseColor.a : upperColor.a;

            return new Color(r, g, b, a);
        }
        internal static Color ToBlendHighlightLine(this Color baseColor, Color upperColor)
        {
            if (baseColor.a < 0.4f)
            {
                float r = baseColor.r * 0.2f + upperColor.r * 0.8f;
                float g = baseColor.g * 0.2f + upperColor.g * 0.8f;
                float b = baseColor.b * 0.2f + upperColor.b * 0.8f;
                float a = upperColor.a;
                return new Color(r, g, b, a);
            }
            else
                return baseColor;
        }
    }

    public static class ExtendTexture
    {
        internal static bool Erase(this Texture2D source, Rect worldTargetAreaRect, Vector2 startPosition, Vector2 endPosition, float size)
        {
            Vector2Int convStartPosition = GetConvertTexturePosition(source, worldTargetAreaRect, startPosition);
            Vector2Int convEndPosition = GetConvertTexturePosition(source, worldTargetAreaRect, endPosition);

            float radius = size * source.height / worldTargetAreaRect.height * 0.5f;

            return EraseInTexture(source, convStartPosition, convEndPosition, radius);
        }
        internal static bool Erase(this Texture2D source, Rect worldTargetAreaRect, Vector2 point, float size)
        {
            float radius = size * source.height / worldTargetAreaRect.height * 0.5f;

            return EraseInTexturePoint(source, GetConvertTexturePosition(source, worldTargetAreaRect, point), radius);
        }

        private static bool EraseInTexture(Texture2D source, Vector2Int startPosition, Vector2Int endPosition, float radius)
        {
            bool somethingDrawn = false;

            float lineWidth = radius;

            float a = startPosition.y - endPosition.y;
            float b = endPosition.x - startPosition.x;
            float c = startPosition.x * endPosition.y - startPosition.y * endPosition.x;

            float length = Mathf.Sqrt((startPosition.x - endPosition.x) * (startPosition.x - endPosition.x) + (startPosition.y - endPosition.y) * (startPosition.y - endPosition.y));

            float dumy;
            float l1;
            float l2;

            int xMin = Mathf.Max(Mathf.Min(startPosition.x, endPosition.x) - (int)lineWidth, 0);
            int xMax = Mathf.Min(Mathf.Max(startPosition.x, endPosition.x) + (int)lineWidth + 1, source.width);
            int yMin = Mathf.Max(Mathf.Min(startPosition.y, endPosition.y) - (int)lineWidth, 0);
            int yMax = Mathf.Min(Mathf.Max(startPosition.y, endPosition.y) + (int)lineWidth + 1, source.height);

            if (xMax - xMin > 0 && yMax - yMin > 0)
            {
                int width = xMax - xMin;
                Color[] pixels = source.GetPixels(xMin, yMin, xMax - xMin, yMax - yMin);

                float ratio = 1f / Mathf.Sqrt(a * a + b * b);

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < yMax - yMin; y++)
                    {
                        dumy = (a * (x + xMin) + b * (y + yMin) + c) * ratio;

                        if (dumy < 0)
                            dumy = -dumy;

                        if (dumy < lineWidth * 1.0f)
                        {
                            l1 = Vector2Int.Distance(startPosition, new Vector2Int(x + xMin, y + yMin));
                            l2 = Vector2Int.Distance(endPosition, new Vector2Int(x + xMin, y + yMin));

                            if (l1 < length && l2 < length)
                            {
                                if (pixels[y * width + x].a != 0)
                                {
                                    pixels[y * width + x] = new Color(1f, 1f, 1f, 0f);
                                    somethingDrawn = true;
                                }
                            }
                            else if (l1 < lineWidth || l2 < lineWidth)
                            {
                                if (pixels[y * width + x].a != 0)
                                {
                                    pixels[y * width + x] = new Color(1f, 1f, 1f, 0f);
                                    somethingDrawn = true;
                                }
                            }
                        }

                    }
                }

                source.SetPixels(xMin, yMin, xMax - xMin, yMax - yMin, pixels);
                source.Apply();
            }

            return somethingDrawn;
        }

        private static bool EraseInTexturePoint(Texture2D source, Vector2Int point, float radius)
        {
            bool somethingDrawn = false;
            float l;

            int xMin = Mathf.Max(point.x - (int)radius, 0);
            int xMax = Mathf.Min(point.x + (int)radius, source.width);
            int yMin = Mathf.Max(point.y - (int)radius, 0);
            int yMax = Mathf.Min(point.y + (int)radius, source.height);

            if (xMax - xMin > 0 && yMax - yMin > 0)
            {
                int width = xMax - xMin;

                Color[] pixels = source.GetPixels(xMin, yMin, width, yMax - yMin);

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < yMax - yMin; y++)
                    {
                        l = Vector2Int.Distance(point, new Vector2Int(x + xMin, y + yMin));

                        if (l < radius * 1.0f)
                        {
                            if (pixels[width * y + x].a != 0)
                            {
                                pixels[width * y + x] = new Color(1f, 1f, 1f, 0f);
                                somethingDrawn = true;
                            }
                        }
                    }
                }
                source.SetPixels(xMin, yMin, width, yMax - yMin, pixels);
                source.Apply();
            }

            return somethingDrawn;
        }


        internal static bool DrawLine(this Texture2D source, Rect worldTargetAreaRect, Color color, Vector2 startPosition, Vector2 endPosition, float lineThick)
        {
            Vector2Int convStartPosition = GetConvertTexturePosition(source, worldTargetAreaRect, startPosition);
            Vector2Int convEndPosition = GetConvertTexturePosition(source, worldTargetAreaRect, endPosition);

            float halfLineThick = lineThick * source.height / worldTargetAreaRect.height * 0.5f;

            return DrawLineInTexture(source, convStartPosition, convEndPosition, color, halfLineThick);
        }

        private static bool DrawLineInTexture(Texture2D source, Vector2Int startPosition, Vector2Int endPosition, Color color, float halfLineThick)
        {
            bool somethingDrawn = false;

            float a = startPosition.y - endPosition.y;
            float b = endPosition.x - startPosition.x;
            float c = startPosition.x * endPosition.y - startPosition.y * endPosition.x;

            float length = Mathf.Sqrt((startPosition.x - endPosition.x) * (startPosition.x - endPosition.x) + (startPosition.y - endPosition.y) * (startPosition.y - endPosition.y));

            float dumy;
            float l1;
            float l2;

            float aRatio = 0.8f / (halfLineThick * halfLineThick);

            int xMin = Mathf.Max(Mathf.Min(startPosition.x, endPosition.x) - (int)halfLineThick, 0);
            int xMax = Mathf.Min(Mathf.Max(startPosition.x, endPosition.x) + (int)halfLineThick + 1, source.width);
            int yMin = Mathf.Max(Mathf.Min(startPosition.y, endPosition.y) - (int)halfLineThick, 0);
            int yMax = Mathf.Min(Mathf.Max(startPosition.y, endPosition.y) + (int)halfLineThick + 1, source.height);

            if (xMax - xMin > 0 && yMax - yMin > 0)
            {
                Color[] pixels = source.GetPixels(xMin, yMin, xMax - xMin, yMax - yMin);
                int pixNum;

                float ratio = 1f / Mathf.Sqrt(a * a + b * b);

                for (int x = xMin; x < xMax; x++)
                {
                    for (int y = yMin; y < yMax; y++)
                    {
                        dumy = (a * x + b * y + c) * ratio;

                        if (dumy < 0f)
                            dumy = -dumy;

                        if (dumy < halfLineThick * 1.0f)
                        {
                            l1 = Vector2Int.Distance(startPosition, new Vector2Int(x, y));
                            l2 = Vector2Int.Distance(endPosition, new Vector2Int(x, y));

                            pixNum = (x - xMin) + (y - yMin) * (xMax - xMin);

                            if (l1 < length + halfLineThick && l2 < length + halfLineThick)
                            {
                                if (l1 > length)
                                {
                                    if (l2 < halfLineThick * 1.0f)
                                    {
                                        pixels[pixNum] = pixels[pixNum].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * l2 * l2));
                                        somethingDrawn = true;
                                    }
                                }
                                else if (l2 > length)
                                {
                                    if (l1 < halfLineThick * 1.0f)
                                    {
                                        pixels[pixNum] = pixels[pixNum].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * l1 * l1));
                                        somethingDrawn = true;
                                    }
                                }
                                else
                                {
                                    pixels[pixNum] = pixels[pixNum].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * dumy * dumy));
                                    somethingDrawn = true;
                                }

                            }
                        }
                    }
                }
                source.SetPixels(xMin, yMin, xMax - xMin, yMax - yMin, pixels);
                source.Apply();
            }

            return somethingDrawn;
        }


        internal static bool DrawHighlight(this Texture2D source, Rect worldTargetAreaRect, Color color, Vector2 startPosition, Vector2 endPosition, float height)
        {
            Vector2Int convStartPosition = GetConvertTexturePosition(source, worldTargetAreaRect, startPosition);
            Vector2Int convEndPosition = GetConvertTexturePosition(source, worldTargetAreaRect, endPosition);

            float halfLineThick = height * source.height / worldTargetAreaRect.height * 0.5f;

            return DrawHighlightInTexture(source, convStartPosition, convEndPosition, color, halfLineThick);
        }

        private static bool DrawHighlightInTexture(Texture2D source, Vector2Int startPosition, Vector2Int endPosition, Color color, float halfHeight)
        {
            bool somethingDrawn = false;

            Color aColor = new Color(color.r, color.g, color.b, 0.3f);

            float a = startPosition.y - endPosition.y;
            float b = endPosition.x - startPosition.x;
            float c = startPosition.x * endPosition.y - startPosition.y * endPosition.x;

            float dumy;

            int xMin = Mathf.Max(Mathf.Min(startPosition.x, endPosition.x), 0);
            int xMax = Mathf.Min(Mathf.Max(startPosition.x, endPosition.x), source.width);
            int yMin = Mathf.Max(Mathf.Min(startPosition.y, endPosition.y) - (int)halfHeight, 0);
            int yMax = Mathf.Min(Mathf.Max(startPosition.y, endPosition.y) + (int)halfHeight + 1, source.height);

            if (xMax - xMin > 0 && yMax - yMin > 0)
            {
                Color[] pixels = source.GetPixels(xMin, yMin, xMax - xMin, yMax - yMin);
                int pixNum;

                for (int x = xMin; x < xMax; x++)
                {
                    dumy = (a * x + c) / (-b);

                    for (int y = yMin; y < yMax; y++)
                    {
                        pixNum = (x - xMin) + (y - yMin) * (xMax - xMin);

                        if (x >= xMin && x <= xMax && Mathf.Abs(y - dumy) < halfHeight)
                        {
                            pixels[pixNum] = pixels[pixNum].ToBlendHighlightLine(aColor);
                            somethingDrawn = true;
                        }
                    }
                }
                source.SetPixels(xMin, yMin, xMax - xMin, yMax - yMin, pixels);
                source.Apply();
            }

            return somethingDrawn;
        }

        internal static bool DrawArc(this Texture2D texture, Rect rect, Vector2 center_point, Vector2 start_point, Vector2 end_point, bool isClock, Color color, float thick)
        {
            Vector2Int c = GetConvertTexturePosition(texture, rect, center_point);
            Vector2Int s = GetConvertTexturePosition(texture, rect, start_point);
            Vector2Int e = GetConvertTexturePosition(texture, rect, end_point);

            return DrawArcInTexture(texture, c, s, e, isClock, color, thick * 0.5f * texture.height / rect.height);
        }

        private static bool DrawArcInTexture(Texture2D texture, Vector2Int centerP, Vector2Int startP, Vector2Int endP, bool isClock, Color color, float thick)
        {
            bool somethingDrawn = false;

            float radius = Vector2Int.Distance(centerP, startP);
            float stAngle = int2Angle(new int[] { startP[0] - centerP[0], startP[1] - centerP[1] });
            float enAngle = int2Angle(new int[] { endP[0] - centerP[0], endP[1] - centerP[1] });

            float aRatio = 0.8f / (thick * thick);

            float sL;
            float eL;

            int tWidth = texture.width;
            int tHeight = texture.height;

            int xMin = Mathf.Max(centerP[0] - (int)radius - (int)thick - 1, 0);
            int xMax = Mathf.Min(centerP[0] + (int)radius + (int)thick + 1, tWidth);
            int yMin = Mathf.Max(centerP[1] - (int)radius - (int)thick - 1, 0);
            int yMax = Mathf.Min(centerP[1] + (int)radius + (int)thick + 1, tHeight);

            if (xMax - xMin > 0 && yMax - yMin > 0)
            {
                Color[] pixels = texture.GetPixels(xMin, yMin, xMax - xMin, yMax - yMin);

                if (!isClock && stAngle >= enAngle)
                {
                    for (int x = xMin; x < xMax; x++)
                    {
                        for (int y = yMin; y < yMax; y++)
                        {
                            float angle = int2Angle(new int[] { x - centerP[0], y - centerP[1] });
                            float dumy = Mathf.Abs(Vector2Int.Distance(centerP, new Vector2Int(x, y)) - radius);

                            if (angle < stAngle && angle > enAngle)
                            {
                                sL = Vector2Int.Distance(startP, new Vector2Int(x, y));
                                eL = Vector2Int.Distance(endP, new Vector2Int(x, y));

                                if (sL < thick * 1.0f)
                                {
                                    somethingDrawn = true;
                                    pixels[(y - yMin) * (xMax - xMin) + x - xMin] = pixels[(y - yMin) * (xMax - xMin) + x - xMin].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * sL * sL));
                                }
                                else if (eL < thick * 1.0f)
                                {
                                    somethingDrawn = true;
                                    pixels[(y - yMin) * (xMax - xMin) + x - xMin] = pixels[(y - yMin) * (xMax - xMin) + x - xMin].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * eL * eL));
                                }
                            }
                            else
                            {
                                if (dumy < thick * 1.0f)
                                {
                                    somethingDrawn = true;
                                    pixels[(y - yMin) * (xMax - xMin) + x - xMin] = pixels[(y - yMin) * (xMax - xMin) + x - xMin].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * dumy * dumy));
                                }
                            }

                        }
                    }
                }
                else if (isClock && stAngle <= enAngle)
                {
                    for (int x = xMin; x < xMax; x++)
                    {
                        for (int y = yMin; y < yMax; y++)
                        {
                            float angle = int2Angle(new int[] { x - centerP[0], y - centerP[1] });
                            float dumy = Mathf.Abs(Vector2Int.Distance(centerP, new Vector2Int(x, y)) - radius);

                            if (angle > stAngle && angle < enAngle)
                            {
                                sL = Vector2Int.Distance(startP, new Vector2Int(x, y));
                                eL = Vector2Int.Distance(endP, new Vector2Int(x, y));

                                if (sL < thick * 1.0f)
                                {
                                    somethingDrawn = true;
                                    pixels[(y - yMin) * (xMax - xMin) + x - xMin] = pixels[(y - yMin) * (xMax - xMin) + x - xMin].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * sL * sL));
                                }
                                else if (eL < thick * 1.0f)
                                {
                                    somethingDrawn = true;
                                    pixels[(y - yMin) * (xMax - xMin) + x - xMin] = pixels[(y - yMin) * (xMax - xMin) + x - xMin].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * eL * eL));
                                }
                            }
                            else
                            {
                                if (dumy < thick * 1.0f)
                                {
                                    somethingDrawn = true;
                                    pixels[(y - yMin) * (xMax - xMin) + x - xMin] = pixels[(y - yMin) * (xMax - xMin) + x - xMin].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * dumy * dumy));
                                }
                            }
                        }
                    }
                }
                else if (!isClock)
                {
                    for (int x = xMin; x < xMax; x++)
                    {
                        for (int y = yMin; y < yMax; y++)
                        {
                            float angle = int2Angle(new int[] { x - centerP[0], y - centerP[1] });
                            float dumy = Mathf.Abs(Vector2Int.Distance(centerP, new Vector2Int(x, y)) - radius);

                            if (angle > stAngle && angle < enAngle)
                            {
                                if (dumy < thick * 1.0f)
                                {
                                    somethingDrawn = true;
                                    pixels[(y - yMin) * (xMax - xMin) + x - xMin] = pixels[(y - yMin) * (xMax - xMin) + x - xMin].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * dumy * dumy));
                                }
                            }
                            else
                            {
                                sL = Vector2Int.Distance(startP, new Vector2Int(x, y));
                                eL = Vector2Int.Distance(endP, new Vector2Int(x, y));

                                if (sL < thick * 1.0f)
                                {
                                    somethingDrawn = true;
                                    pixels[(y - yMin) * (xMax - xMin) + x - xMin] = pixels[(y - yMin) * (xMax - xMin) + x - xMin].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * sL * sL));
                                }
                                else if (eL < thick * 1.0f)
                                {
                                    somethingDrawn = true;
                                    pixels[(y - yMin) * (xMax - xMin) + x - xMin] = pixels[(y - yMin) * (xMax - xMin) + x - xMin].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * eL * eL));
                                }
                            }
                        }
                    }
                }
                else if (isClock)
                {
                    for (int x = xMin; x < xMax; x++)
                    {
                        for (int y = yMin; y < yMax; y++)
                        {
                            float angle = int2Angle(new int[] { x - centerP[0], y - centerP[1] });
                            float dumy = Mathf.Abs(Vector2Int.Distance(centerP, new Vector2Int(x, y)) - radius);

                            if (angle < stAngle && angle > enAngle)
                            {
                                if (dumy < thick * 1.0f)
                                {
                                    somethingDrawn = true;
                                    pixels[(y - yMin) * (xMax - xMin) + x - xMin] = pixels[(y - yMin) * (xMax - xMin) + x - xMin].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * dumy * dumy));
                                }
                            }
                            else
                            {
                                sL = Vector2Int.Distance(startP, new Vector2Int(x, y));
                                eL = Vector2Int.Distance(endP, new Vector2Int(x, y));

                                if (sL < thick * 1.0f)
                                {
                                    somethingDrawn = true;
                                    pixels[(y - yMin) * (xMax - xMin) + x - xMin] = pixels[(y - yMin) * (xMax - xMin) + x - xMin].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * sL * sL));
                                }
                                else if (eL < thick * 1.0f)
                                {
                                    somethingDrawn = true;
                                    pixels[(y - yMin) * (xMax - xMin) + x - xMin] = pixels[(y - yMin) * (xMax - xMin) + x - xMin].ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * eL * eL));
                                }
                            }
                        }
                    }
                }

                texture.SetPixels(xMin, yMin, xMax - xMin, yMax - yMin, pixels);

            }

            bool somethingDrawnOnPoint =
                DrawPointInTexture(texture, centerP, color, thick);

            texture.Apply();

            return somethingDrawn || somethingDrawnOnPoint;
        }

        internal static bool DrawPoint(this Texture2D texture, Rect rect, Vector2 point, Color color, float thick)
        {
            Vector2Int p = GetConvertTexturePosition(texture, rect, point);

            bool somethingDrawn = DrawPointInTexture(texture, p, color, thick * 0.5f * texture.height / rect.height);

            texture.Apply();

            return somethingDrawn;
        }

        private static bool DrawPointInTexture(Texture2D texture, Vector2Int point, Color color, float thick)
        {
            bool somethingDrawn = false;

            float lineWidth = thick;

            float l;

            float aRatio = 0.8f / (lineWidth * lineWidth);

            int sX = Mathf.Max(point[0] - (int)(lineWidth + 1), 0);
            int eX = Mathf.Min(point[0] + (int)(lineWidth + 2), texture.width);
            int sY = Mathf.Max(point[1] - (int)(lineWidth + 1), 0);
            int eY = Mathf.Min(point[1] + (int)(lineWidth + 2), texture.height);

            for (int x = sX; x < eX; x++)
            {
                for (int y = sY; y < eY; y++)
                {
                    l = Vector2Int.Distance(point, new Vector2Int(x, y));

                    if (l < lineWidth * 1.0f)
                    {
                        texture.SetPixel(x, y, texture.GetPixel(x, y).ToBlendLine(new Color(color.r, color.g, color.b, 1f - aRatio * l * l)));
                        somethingDrawn = true;
                    }
                }
            }

            return somethingDrawn;
        }


        private static float int2Angle(int[] int2)
        {
            if (int2[0] < 0)
            {
                return 360 - (Mathf.Atan2(int2[0], int2[1]) * Mathf.Rad2Deg * -1);
            }
            else
            {
                return Mathf.Atan2(int2[0], int2[1]) * Mathf.Rad2Deg;
            }
        }


        private static Vector2Int GetConvertTexturePosition(Texture2D source, Rect worldTargetAreaRect, Vector2 position)
        {
            Vector2Int result = new Vector2Int();

            result.x = (int)((position.x - worldTargetAreaRect.x) * source.width / worldTargetAreaRect.width);
            result.y = (int)((position.y - worldTargetAreaRect.y) * source.height / worldTargetAreaRect.height);

            return result;
        }

        internal static Texture2D ScaleTexture(this Texture2D source, int width, int height, bool disposeSourceTexture = true)
        {
            Texture2D result = new Texture2D(width, height, source.format, true);
            Color[] rpixels = result.GetPixels(0);
            float incX = (1.0f / (float)width);
            float incY = (1.0f / (float)height);
            for (int px = 0; px < rpixels.Length; px++)
            {
                rpixels[px] = source.GetPixelBilinear(incX * ((float)px % width), incY * ((float)Mathf.Floor(px / width)));
            }
            result.SetPixels(rpixels, 0);
            result.Apply();

            if (disposeSourceTexture)
                Texture2D.DestroyImmediate(source);

            return result;
        }

        internal static void Resize(this Texture2D texture2D, int targetX, int targetY, bool mipmap = true, FilterMode filter = FilterMode.Bilinear)
        {
            //create a temporary RenderTexture with the target size
            RenderTexture rt = RenderTexture.GetTemporary(targetX, targetY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            //set the active RenderTexture to the temporary texture so we can read from it
            RenderTexture.active = rt;

            //Copy the texture data on the GPU - this is where the magic happens [(;]
            Graphics.Blit(texture2D, rt);
            //resize the texture to the target values (this sets the pixel data as undefined)
            texture2D.Reinitialize(targetX, targetY, texture2D.format, mipmap);
            texture2D.filterMode = filter;

            try
            {
                //reads the pixel values from the temporary RenderTexture onto the resized texture
                texture2D.ReadPixels(new Rect(0.0f, 0.0f, targetX, targetY), 0, 0);
                //actually upload the changed pixels to the graphics card
                texture2D.Apply();
            }
            catch
            {
                UnityEngine.Debug.LogError("Read/Write is not enabled on texture " + texture2D.name);
            }


            RenderTexture.ReleaseTemporary(rt);
        }

        internal static Texture2D ScaleTexture(this Texture2D source, float scaleFactor, bool disposeSourceTexture = true)
        {
            if (scaleFactor == 1f)
            {
                return source;
            }
            else if (scaleFactor == 0f)
            {
                return Texture2D.blackTexture;
            }

            int _newWidth = Mathf.RoundToInt(source.width * scaleFactor);
            int _newHeight = Mathf.RoundToInt(source.height * scaleFactor);

            Color[] _scaledTexPixels = new Color[_newWidth * _newHeight];

            for (int _yCord = 0; _yCord < _newHeight; _yCord++)
            {
                float _vCord = _yCord / (_newHeight * 1f);
                int _scanLineIndex = _yCord * _newWidth;

                for (int _xCord = 0; _xCord < _newWidth; _xCord++)
                {
                    float _uCord = _xCord / (_newWidth * 1f);

                    _scaledTexPixels[_scanLineIndex + _xCord] = source.GetPixelBilinear(_uCord, _vCord);
                }
            }
            Texture2D result = new Texture2D(_newWidth, _newHeight, source.format, false);
            result.SetPixels(_scaledTexPixels, 0);
            result.Apply();

            if (disposeSourceTexture)
                Texture2D.DestroyImmediate(source);

            return result;
        }


        internal static Vector2Int FloodFillBySize(this Texture2D texture, Color sourceColor, Vector2 pointWorldPosition, int size,
                                                   Rect drawingBoardRect, Color[] fillableColors = null, Action drawCallback = null)
        {
            if (size >= 41)
            {
                size = 41;
            }
            else if (size >= 14)
            {
                size = 14;
            }
            else if (size >= 5)
            {
                size = 5;
            }
            else if (size >= 2)
            {
                size = 2;
            }
            else
            {
                size = 1;
            }

            Vector2Int point = GetConvertTexturePosition(texture, drawingBoardRect, pointWorldPosition);

            FillTexture(texture, sourceColor, size, point, fillableColors, drawCallback);

            return point;
        }

        internal static void FloodFillBySize(this Texture2D texture, Color sourceColor, Vector2Int positionInTexture, int size,
                                             Color[] fillableColors = null, Action drawCallback = null)
        {
            if (size >= 41)
            {
                size = 41;
            }
            else if (size >= 14)
            {
                size = 14;
            }
            else if (size >= 5)
            {
                size = 5;
            }
            else if (size >= 2)
            {
                size = 2;
            }
            else
            {
                size = 1;
            }

            FillTexture(texture, sourceColor, size, positionInTexture, fillableColors, drawCallback);
        }

        private static void FillTexture(Texture2D texture, Color sourceColor, int size, Vector2Int point,
                                              Color[] fillableColors = null, Action drawCallback = null)
        {
            var targetColor = texture.GetPixel(point.x, point.y);

            if (fillableColors != null)
            {
                bool isFillable = false;

                foreach (Color item in fillableColors)
                {
                    if (item == targetColor)
                    {
                        isFillable = true;
                        break;
                    }
                }

                if (!isFillable)
                    return;
            }

            if (!isGray(targetColor))
            {
                if (!IsSameColor(targetColor, sourceColor))
                {
                    drawCallback?.Invoke();
                    //SoundController.Instance.Play(SoundController.AudioType.MenuSimpleClick);

                    var q = new Queue<PointS>();
                    PointS newPoint = new PointS((short)point.x, (short)point.y, (byte)size);

                    if (IsUniformColor(texture, newPoint))
                    {
                        q.Enqueue(newPoint);
                    }
                    else
                    {
                        int s = size;
                        while (s > 1)
                        {
                            s = (s + 1) / 3;
                            newPoint = new PointS((short)point.x, (short)point.y, (byte)s);

                            if (IsUniformColor(texture, newPoint))
                            {
                                q.Enqueue(newPoint);
                                break;
                            }
                        }
                    }

                    FloodFillBySize(texture, sourceColor, targetColor, q);

                    texture.Apply();
                }
                else
                {
                    //MessageBoxController.Instance.Show(MessageBoxDefaultPositionType.Top, MessageBoxButtonType.AutoClosing, MessageBoxStateType.OnlyMessage, MessageBoxSizeType.Small,
                    //                                    "이미 같은 색이 있습니다.", 1.0f);
                }
            }
            else
            {
                //MessageBoxController.Instance.Show(MessageBoxDefaultPositionType.Top, MessageBoxButtonType.AutoClosing, MessageBoxStateType.OnlyMessage, MessageBoxSizeType.Small,
                //                                        "그 곳에는 색을 칠할 수 없습니다.", 1.0f);
            }

            bool isGray(Color color)
            {
                if (IsSameColor(color, Color.white))
                {
                    return false;
                }
                else
                {
                    float distance = Mathf.Abs(color.r - color.g) + Mathf.Abs(color.g - color.b) + Mathf.Abs(color.b - color.r);
                    return (distance < 0.1f);
                }
            }
        }

        private static void FloodFillBySize(Texture2D texture, Color sourceColor, Color currentTargetColor, Queue<PointS> ps)
        {
            int iterations = 0;

            while (ps.Count > 0)
            {
                PointS newPoint;

                var point = ps.Dequeue();
                var x1 = point.x;
                var y1 = point.y;
                var size = point.size;

                if (!IsSameColor(texture.GetPixel(x1, y1), currentTargetColor))
                {
                    continue;
                }

                SetTexturePixels(texture, point, sourceColor);

                newPoint = new PointS((short)(x1 + size * 2 - 1), y1, (byte)size);
                if (IsUniformColor(texture, newPoint))
                {
                    ps.Enqueue(newPoint);
                }
                else
                {
                    SeperateEnq(texture, "right", new PointS(x1, y1, (byte)size));
                }

                newPoint = new PointS((short)(x1 - size * 2 + 1), y1, (byte)size);
                if (IsUniformColor(texture, newPoint))
                {
                    ps.Enqueue(newPoint);
                }
                else
                {
                    SeperateEnq(texture, "left", new PointS(x1, y1, (byte)size));
                }

                newPoint = new PointS(x1, (short)(y1 + size * 2 - 1), (byte)size);
                if (IsUniformColor(texture, newPoint))
                {
                    ps.Enqueue(newPoint);
                }
                else
                {
                    SeperateEnq(texture, "up", new PointS(x1, y1, (byte)size));
                }

                newPoint = new PointS(x1, (short)(y1 - size * 2 + 1), (byte)size);
                if (IsUniformColor(texture, newPoint))
                {
                    ps.Enqueue(newPoint);
                }
                else
                {
                    SeperateEnq(texture, "down", new PointS(x1, y1, (byte)size));
                }

                iterations++;
            }

            if (dummyPoints.Count > 0)
            {
                FloodFillBySize(texture, sourceColor, currentTargetColor, dummyPoints);
            }
            else
                texture.Apply();
        }

        private static void SeperateEnq(Texture2D texture, string direction, PointS p)
        {
            int size = p.size;
            short x1 = p.x;
            short y1 = p.y;

            PointS newPoint;

            switch (direction)
            {
                case "right":

                    if (size > 1)
                    {
                        for (int i = -1; i < 2; i++)
                        {
                            newPoint = new PointS((short)(x1 + size - 1 + (size + 1) / 3), (short)(y1 + (size + 1) / 3 * i), (byte)((size + 1) / 3));
                            if (IsUniformColor(texture, newPoint))
                            {
                                dummyPoints.Enqueue(newPoint);
                            }
                            else
                            {
                                SeperateEnq(texture, direction, new PointS((short)(x1 + size - (size + 1) / 3), (short)(y1 + (size + 1) / 3 * i), (byte)((size + 1) / 3)));
                            }
                        }
                    }

                    break;
                case "left":

                    if (size > 1)
                    {
                        for (int i = -1; i < 2; i++)
                        {
                            newPoint = new PointS((short)(x1 - size + 1 - (size + 1) / 3), (short)(y1 + (size + 1) / 3 * i), (byte)((size + 1) / 3));
                            if (IsUniformColor(texture, newPoint))
                            {
                                dummyPoints.Enqueue(newPoint);
                            }
                            else
                            {
                                SeperateEnq(texture, direction, new PointS((short)(x1 - size + (size + 1) / 3), (short)(y1 + (size + 1) / 3 * i), (byte)((size + 1) / 3)));
                            }
                        }
                    }

                    break;
                case "up":

                    if (size > 1)
                    {
                        for (int i = -1; i < 2; i++)
                        {
                            newPoint = new PointS((short)(x1 + (size + 1) / 3 * i), (short)(y1 + size - 1 + (size + 1) / 3), (byte)((size + 1) / 3));
                            if (IsUniformColor(texture, newPoint))
                            {
                                dummyPoints.Enqueue(newPoint);
                            }
                            else
                            {
                                SeperateEnq(texture, direction, new PointS((short)(x1 + (size + 1) / 3 * i), (short)(y1 + size - (size + 1) / 3), (byte)((size + 1) / 3)));
                            }
                        }
                    }

                    break;
                case "down":

                    if (size > 1)
                    {
                        for (int i = -1; i < 2; i++)
                        {
                            newPoint = new PointS((short)(x1 + (size + 1) / 3 * i), (short)(y1 - size + 1 - (size + 1) / 3), (byte)((size + 1) / 3));
                            if (IsUniformColor(texture, newPoint))
                            {
                                dummyPoints.Enqueue(newPoint);
                            }
                            else
                            {
                                SeperateEnq(texture, direction, new PointS((short)(x1 + (size + 1) / 3 * i), (short)(y1 - size + (size + 1) / 3), (byte)((size + 1) / 3)));
                            }
                        }
                    }

                    break;
            }
        }

        private static bool IsUniformColor(Texture2D texture, PointS p)
        {
            int size = p.size;

            if (p.x - size + 1 < 0 || p.x + size - 1 >= texture.width)
            {
                return false;
            }
            if (p.y - size + 1 < 0 || p.y + size - 1 >= texture.height)
            {
                return false;
            }

            Color[] pixels;
            Color centerPixel = texture.GetPixel(p.x, p.y);

            pixels = texture.GetPixels(p.x - size + 1, p.y - size + 1, size * 2 - 1, size * 2 - 1);

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i] != centerPixel)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSameColor(Color a, Color b)
        {
            float distance = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a);
            return (distance < 0.3f);
        }

        private static void SetTexturePixels(Texture2D texture, PointS p, Color color)
        {
            int size = p.size;

            Color[] colors = new Color[(size * 2 - 1) * (size * 2 - 1)];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }

            texture.SetPixels(p.x - size + 1, p.y - size + 1, size * 2 - 1, size * 2 - 1, colors);
        }

        private static Queue<PointS> dummyPoints = new Queue<PointS>();

        private struct PointS
        {
            public short x;
            public short y;
            public byte size;

            public PointS(short x, short y, byte s)
            {
                this.x = x;
                this.y = y;
                this.size = s;
            }
        }

    }

}

