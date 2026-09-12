using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMS.Util.Extend
{
    using System;
    using UniRx;

    public static class ExtendCanvasGroup
    {
        public static void Fade(this CanvasGroup target, bool isFadeIn, Action onFinished)
        {
            Observable.FromCoroutine(observer => FadeRoutine(target, isFadeIn), false)
                .Subscribe(_ => 
                {
                    Debug.Log("Fade OnNext");
                },
                () => 
                {
                    if (onFinished != null)
                        onFinished.Invoke();
                });
        }

        private static IEnumerator FadeRoutine(CanvasGroup target, bool isFadeIn)
        {
            float timer = 0f;

            while (timer <= 1)
            {
                yield return null;

                timer += Time.unscaledDeltaTime * 2f;
                target.alpha = Mathf.Lerp(isFadeIn ? 0 : 1, isFadeIn ? 1 : 0, timer);

            }
        }
    }

}

