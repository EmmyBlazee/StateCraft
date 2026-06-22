using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;

namespace StateCraft.Core
{
    /// <summary>
    /// Manages the two-phase splash screen:
    ///   Phase 1 – plays Devolution 1.mp4 on a RawImage/VideoPlayer fullscreen.
    ///   Phase 2 – fades in Splash.png with an animated loading indicator at bottom-right
    ///             while async-loading the main game scene.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class SplashController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("RawImage GameObject used to display the video texture.")]
        public UnityEngine.UI.RawImage videoDisplay;

        [Tooltip("CanvasGroup on the video layer — used to fade it out.")]
        public CanvasGroup videoCanvasGroup;

        [Tooltip("CanvasGroup on the splash image layer — fades in after video.")]
        public CanvasGroup splashCanvasGroup;

        [Tooltip("UIDocument that hosts the loading indicator UXML on the splash layer.")]
        public UIDocument splashUIDocument;

        [Header("Settings")]
        [Tooltip("Name of the scene to load after the splash.")]
        public string mainSceneName = "SampleScene";

        [Tooltip("Seconds to cross-fade between video and splash image.")]
        public float fadeDuration = 0.8f;

        [Tooltip("Minimum time (seconds) to show the splash before transitioning even if loading is already done.")]
        public float minSplashTime = 2.0f;

        // ── UI references ──────────────────────────────────────────────
        private VisualElement _loadingDot1, _loadingDot2, _loadingDot3;
        private Label _loadingLabel;
        private ProgressBar _loadingBar;

        private VideoPlayer _videoPlayer;
        private bool _videoFinished;
        private float _splashTimer;
        private AsyncOperation _loadOp;

        // ── Dot animation state ────────────────────────────────────────
        private float _dotTimer;
        private int _dotFrame;
        private const float DOT_INTERVAL = 0.4f;

        // ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            _videoPlayer = GetComponent<VideoPlayer>();
        }

        private IEnumerator Start()
        {
            // ── Ensure initial state ───────────────────────────────────
            if (videoCanvasGroup)  { videoCanvasGroup.alpha  = 1f; videoCanvasGroup.gameObject.SetActive(true); }
            if (splashCanvasGroup) { splashCanvasGroup.alpha = 0f; }

            // ── Bind UI ────────────────────────────────────────────────
            if (splashUIDocument != null)
            {
                var root = splashUIDocument.rootVisualElement;
                _loadingLabel = root.Q<Label>("LoadingLabel");
                _loadingBar   = root.Q<ProgressBar>("LoadingBar");
                _loadingDot1  = root.Q<VisualElement>("Dot1");
                _loadingDot2  = root.Q<VisualElement>("Dot2");
                _loadingDot3  = root.Q<VisualElement>("Dot3");

                // Hide loading layer until we're on the splash phase
                root.style.opacity = 0;
            }

            // ── Phase 1 : play the video ───────────────────────────────
            _videoPlayer.loopPointReached += OnVideoEnd;
            _videoPlayer.Play();

            yield return new WaitUntil(() => _videoFinished);

            // ── Start async scene load in the background ───────────────
            _loadOp = SceneManager.LoadSceneAsync(mainSceneName);
            _loadOp.allowSceneActivation = false;

            // ── Phase 2 : cross-fade to splash image ───────────────────
            yield return StartCoroutine(CrossFadeToSplash());

            // Show loading UI
            if (splashUIDocument != null)
                splashUIDocument.rootVisualElement.style.opacity = 1;

            _splashTimer = 0f;

            // ── Drive loading progress ─────────────────────────────────
            while (_loadOp.progress < 0.9f || _splashTimer < minSplashTime)
            {
                _splashTimer += Time.deltaTime;
                UpdateLoadingUI(_loadOp.progress / 0.9f);
                yield return null;
            }

            // Final frame — show 100 %
            UpdateLoadingUI(1f);
            if (_loadingLabel != null) _loadingLabel.text = "READY";
            yield return new WaitForSeconds(0.4f);

            // ── Activate the scene ─────────────────────────────────────
            _loadOp.allowSceneActivation = true;
        }

        private void Update()
        {
            // Animate the pulsing dots while on the splash phase
            if (_loadingDot1 == null) return;

            _dotTimer += Time.deltaTime;
            if (_dotTimer >= DOT_INTERVAL)
            {
                _dotTimer = 0f;
                _dotFrame = (_dotFrame + 1) % 4;
                SetDotActive(_loadingDot1, _dotFrame >= 1);
                SetDotActive(_loadingDot2, _dotFrame >= 2);
                SetDotActive(_loadingDot3, _dotFrame >= 3);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────

        private void OnVideoEnd(VideoPlayer vp)
        {
            _videoFinished = true;
        }

        private IEnumerator CrossFadeToSplash()
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                if (videoCanvasGroup)  videoCanvasGroup.alpha  = 1f - t;
                if (splashCanvasGroup) splashCanvasGroup.alpha = t;
                yield return null;
            }
            if (videoCanvasGroup)  videoCanvasGroup.alpha  = 0f;
            if (splashCanvasGroup) splashCanvasGroup.alpha = 1f;
            if (videoCanvasGroup)  videoCanvasGroup.gameObject.SetActive(false);
        }

        private void UpdateLoadingUI(float normalised)
        {
            if (_loadingBar != null)
                _loadingBar.value = normalised * 100f;

            if (_loadingLabel != null && normalised < 1f)
                _loadingLabel.text = $"LOADING  {Mathf.RoundToInt(normalised * 100f)}%";
        }

        private static void SetDotActive(VisualElement dot, bool active)
        {
            if (dot == null) return;
            dot.style.opacity = active ? 1f : 0.2f;
        }
    }
}
