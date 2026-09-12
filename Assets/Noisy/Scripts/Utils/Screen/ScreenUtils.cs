using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMS.Util.ScreenInfo
{
    using System;

    using UniRx;

    using CMS.Util.Extend;
    using UnityEngine.Events;
    using System.Diagnostics;

    public class OnChangeScreenAspectRatioEvent : UnityEvent<ScreenInfo> { };

    public class ScreenInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public Vector2 ScreenScaleRatio { get; set; } = new Vector2();

        public override string ToString()
        {
            return $"Width : {Width}\n" +
                   $"Height : {Height}\n" +
                   $"ScreenScaleRatio : {ScreenScaleRatio}";
        }
    }

    public static class ScreenUtils
    {
        private static IDisposable captureDispose;
        private static IDisposable changeScaleRatioDIspose;

        public static ScreenInfo ScreenInfo { get; private set; } = new ScreenInfo();

        private static Texture2D recyclingTexture;

        public static OnChangeScreenAspectRatioEvent OnChangeScreenInfo { get; set; } = new OnChangeScreenAspectRatioEvent();

        public static bool IsOverlayChungdamUI { get; set; } = false;
        public static Camera CurrentSceneCamera { get; set; }

        public static void CheckingChangeScreenInfo()
        {
            if (changeScaleRatioDIspose != null)
            {
                changeScaleRatioDIspose.Dispose();
                changeScaleRatioDIspose = null;
            }

            changeScaleRatioDIspose = Observable
                .FromCoroutine(o => ChangeScreenInfoCoroutine())
                .Subscribe();
        }

        public static void ScreenCapture(Action<Texture2D> onFinished, bool doScale = true)
        {
            if (captureDispose != null)
            {
                captureDispose.Dispose();
                captureDispose = null;
                //recyclingTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            }

            captureDispose = Observable
                                .FromCoroutine(o => ScreenCaptureCoroutine(null, onFinished, doScale))
                                .Subscribe();
        }
        public static void ScreenCapture(RectTransform area, Action<Texture2D> onFinished, bool doScale = true)
        {
            if (captureDispose != null)
            {
                captureDispose.Dispose();
                captureDispose = null;
            }

            captureDispose = Observable
                                .FromCoroutine(o => ScreenCaptureCoroutine(area, onFinished, doScale))
                                .Subscribe();
        }

        private static IEnumerator ScreenCaptureCoroutine(RectTransform area, Action<Texture2D> onFinished, bool doScale)
        {
            yield return new WaitForEndOfFrame();

            //Stopwatch check = new Stopwatch();
            //check.Start();
            if (area == null)
            {
                // 2021.9.28 JBK : TextureFormat ARGB32 -> RGB24 & mipchain = false
                Texture2D result = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                result.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                result.Apply();

                if (doScale)
                    result.Resize(854, 480, false);                

                onFinished?.Invoke(result);
            }

            else
            {
                Texture2D result = default;

                Canvas rootCanvas = area.GetComponentInParent<Canvas>().rootCanvas;

                if (rootCanvas)
                {
                    if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        result = new Texture2D((int)area.GetWorldSpaceRect().width, (int)area.GetWorldSpaceRect().height);
                        result.ReadPixels(area.GetWorldSpaceRect(), 0, 0);
                    }
                    else if (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                    {
                        Rect screenRect = area.GetScreenRect(CurrentSceneCamera);

                        result = new Texture2D((int)screenRect.width, (int)screenRect.height);
                        result.ReadPixels(screenRect, 0, 0);
                    }
                }


                if (doScale)
                {
                    //Texture2D scalingTexture = result.ScaleTexture(640, 360);
                    result.Apply();
                    result.Resize(640, 360, false);
                    onFinished?.Invoke(result);
                }
                else
                {
                    result.Apply();
                    onFinished?.Invoke(result);
                }
            }
        }

        private static IEnumerator ChangeScreenInfoCoroutine()
        {
            while (true)
            {
                if (ScreenInfo != null)
                {
                    if (ScreenInfo.Width != Screen.width ||
                        ScreenInfo.Height != Screen.height)
                    {
                        OnChangeScreenInfo?.Invoke(new ScreenInfo()
                        {
                            Width = Screen.width,
                            Height = Screen.height,
                            ScreenScaleRatio = new Vector2(Screen.width / 1920f, Screen.height / 1080f),
                        });
                    }
                }
                ScreenInfo.Width = Screen.width;
                ScreenInfo.Height = Screen.height;

                ScreenInfo.ScreenScaleRatio = new Vector2(Screen.width / 1920f, Screen.height / 1080f);

                yield return new WaitForEndOfFrame();
            }

        }
    }
}
