using UnityEngine;
using System.Collections;

namespace StateCraft.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIAnimationHelper : MonoBehaviour
    {
        public enum AnimationType { SlideRight, SlideLeft, ScaleUp, FadeOnly }
        
        [Header("Settings")]
        public AnimationType animationType = AnimationType.FadeOnly;
        public float duration = 0.3f;
        public float slideDistance = 100f;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector2 originalPosition;
        private Vector3 originalScale;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
            originalPosition = rectTransform.anchoredPosition;
            originalScale = transform.localScale;
        }

        public void AnimateIn()
        {
            StopAllCoroutines();
            gameObject.SetActive(true);
            StartCoroutine(InRoutine());
        }

        public void AnimateOut()
        {
            StopAllCoroutines();
            StartCoroutine(OutRoutine());
        }

        private IEnumerator InRoutine()
        {
            float elapsed = 0;
            canvasGroup.alpha = 0;

            if (animationType == AnimationType.SlideLeft)
                rectTransform.anchoredPosition = originalPosition + new Vector2(slideDistance, 0);
            else if (animationType == AnimationType.SlideRight)
                rectTransform.anchoredPosition = originalPosition + new Vector2(-slideDistance, 0);
            else if (animationType == AnimationType.ScaleUp)
                transform.localScale = originalScale * 0.8f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float curve = Mathf.SmoothStep(0, 1, t);

                canvasGroup.alpha = curve;

                if (animationType == AnimationType.SlideLeft || animationType == AnimationType.SlideRight)
                    rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, originalPosition, curve);
                else if (animationType == AnimationType.ScaleUp)
                    transform.localScale = Vector3.Lerp(transform.localScale, originalScale, curve);

                yield return null;
            }

            canvasGroup.alpha = 1;
            rectTransform.anchoredPosition = originalPosition;
            transform.localScale = originalScale;
        }

        private IEnumerator OutRoutine()
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1 - (elapsed / duration);
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
