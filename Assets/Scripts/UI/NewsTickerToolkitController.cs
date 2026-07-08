using UnityEngine;
using UnityEngine.UIElements;
using StateCraft.Systems;
using System.Collections;
using System.Collections.Generic;

namespace StateCraft.UI
{
    public class NewsTickerToolkitController : MonoBehaviour
    {
        public static NewsTickerToolkitController Instance { get; private set; }

        [Header("Ticker Settings")]
        [SerializeField] private float scrollSpeed = 220f;

        // BUG 1 FIX: Hard cap on pending queue to prevent unbounded growth
        private const int MaxPendingHeadlines = 20;

        private UIDocument _uiDocument;

        private Label _newsLabel;
        private VisualElement _tickerTrack;

        private readonly List<string> _headlines = new List<string>();
        private readonly Queue<string> _pendingHeadlines = new Queue<string>();

        private bool _initialized;
        private bool _isScrolling;

        // Startup headlines (from GlobalNewsHistory) scroll exactly once, then the
        // ticker goes blank and waits silently until real in-game news arrives.
        private bool _isStartupScroll;

        private float _currentX;
        private float _textWidth;
        private float _trackWidth;

        // ─────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            _uiDocument = GetComponent<UIDocument>();
}

        private void OnEnable()
        {
            StopAllCoroutines();
            StartCoroutine(Initialize());
        }

        private void OnDisable()
        {
            _initialized = false;
            _isScrolling = false;
            if (NewsSystem.Instance != null)
                NewsSystem.Instance.OnGlobalNewsPublished -= AddNews;
        }

        // ─────────────────────────────────────────────
        // INIT
        // ─────────────────────────────────────────────

        private IEnumerator Initialize()
        {
            yield return new WaitForEndOfFrame();

            FindUI();

            if (_newsLabel == null || _tickerTrack == null)
            {
                Debug.LogError("[NewsTicker] UI elements missing.");
                yield break;
            }

            while (NewsSystem.Instance == null)
                yield return null;

            NewsSystem.Instance.OnGlobalNewsPublished += AddNews;

            // Load startup boot messages and flag them as the startup scroll
            var history = NewsSystem.Instance.GlobalNewsHistory;
            if (history != null)
            {
                foreach (var news in history)
                    _headlines.Add(news.ToUpper());
            }
            if (_headlines.Count > 0)
                _isStartupScroll = true;

            // Rebuild label with startup text (set before container becomes visible)
            if (_headlines.Count > 0)
                SetLabelText();

            // CRITICAL: Wait until UIStateManager makes the container visible (display != None)
            // UI Toolkit returns width=0 for display:none elements regardless of wait time.
            float visWait = 0f;
            while (visWait < 30f)
            {
                var display = _tickerTrack.resolvedStyle.display;
                var containerDisplay = _newsLabel.resolvedStyle.display;
                if (display != DisplayStyle.None && containerDisplay != DisplayStyle.None)
                    break;
                visWait += Time.deltaTime;
                yield return null;
            }
// Wait two frames for layout to settle after becoming visible
            yield return null;
            yield return null;

            // Now wait for widths to resolve and stabilise
            float waitTime = 0f;
            float lastWidth = 0f;
            while (waitTime < 5f)
            {
                _trackWidth = float.IsNaN(_tickerTrack.resolvedStyle.width) ? 0f : _tickerTrack.resolvedStyle.width;
                _textWidth  = float.IsNaN(_newsLabel.resolvedStyle.width) ? 0f : _newsLabel.resolvedStyle.width;
                
                bool textOk = _headlines.Count == 0 || (_textWidth > 0f && Mathf.Approximately(_textWidth, lastWidth));
                if (_trackWidth > 0f && textOk && !Mathf.Approximately(_trackWidth, 0f))
                    break;
                    
                lastWidth = _textWidth;
                waitTime += Time.deltaTime;
                yield return null;
            }

            // If trackWidth is still 0 the USS may not have a fixed width on TickerTrack.
            // Retry for up to 60 more seconds before falling back to the panel width.
            if (_trackWidth <= 0f)
            {
                float retryWait = 0f;
                while (retryWait < 2f && _trackWidth <= 0f) // Reduced wait time since fallback works well
                {
                    yield return new WaitForSeconds(0.5f);
                    retryWait += 0.5f;
                    _trackWidth = float.IsNaN(_tickerTrack.resolvedStyle.width)
                        ? 0f : _tickerTrack.resolvedStyle.width;
                }
            }

            // Final fallback: use the UIDocument panel width, or a hardcoded safe value.
            if (_trackWidth <= 0f)
            {
                // Try to read the panel's resolved width from the root element
                var root = _uiDocument?.rootVisualElement;
                float panelW = root != null && !float.IsNaN(root.resolvedStyle.width)
                    ? root.resolvedStyle.width : 0f;
                _trackWidth = panelW > 0f ? panelW : 900f;
            }

            _currentX    = _trackWidth;
            _isScrolling = true;
            _initialized = true;

            // Any news that arrived while we were waiting — activate immediately
            if (!_isStartupScroll && _pendingHeadlines.Count > 0)
                StartCoroutine(ActivateWithPending());

}

        // ─────────────────────────────────────────────
        // FIND UI
        // ─────────────────────────────────────────────

        private void FindUI()
        {
            var root = _uiDocument.rootVisualElement;

            _newsLabel   = root.Q<Label>("NewsText");
            _tickerTrack = root.Q<VisualElement>("TickerTrack");

            if (_newsLabel == null || _tickerTrack == null)
                return;

            _newsLabel.style.position   = Position.Absolute;
            _newsLabel.style.whiteSpace = WhiteSpace.NoWrap;
            _newsLabel.style.flexShrink = 0;
            _newsLabel.style.left       = 0;
            _newsLabel.text             = "";
        }

        // ─────────────────────────────────────────────
        // ADD NEWS  (called by NewsSystem for real in-game news)
        // ─────────────────────────────────────────────

        public void AddNews(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            string formatted = message.ToUpper();

            // BUG 1 FIX: Drop oldest pending headline when the queue is at capacity
            // so it never grows unbounded regardless of how fast news is generated.
            if (_pendingHeadlines.Count >= MaxPendingHeadlines)
            {
                _pendingHeadlines.Dequeue();
}

            _pendingHeadlines.Enqueue(formatted);
// If ticker is idle (startup done, nothing scrolling), kick it off now
            if (_initialized && !_isScrolling)
                StartCoroutine(ActivateWithPending());
        }

        private IEnumerator ActivateWithPending()
        {
            DrainPendingIntoHeadlines();
            SetLabelText();
            yield return StartCoroutine(UpdateWidthOnly());
            _currentX    = _trackWidth;
            _isScrolling = true;
        }

        // ─────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────

        private void SetLabelText()
        {
            if (_newsLabel == null) return;
            string finalText = string.Join("     •     ", _headlines) + "     •     ";
            _newsLabel.text = finalText;
            _newsLabel.MarkDirtyRepaint();
        }

        private void DrainPendingIntoHeadlines()
        {
            while (_pendingHeadlines.Count > 0)
                _headlines.Add(_pendingHeadlines.Dequeue());
            while (_headlines.Count > 10)
                _headlines.RemoveAt(0);
        }

        private IEnumerator ResetScrollAfterLayout()
        {
            // Wait two frames then wait for width to stabilise
            yield return null;
            yield return null;
            float waited = 0f;
            float last = 0f;
            while (waited < 2f)
            {
                float w = _newsLabel.resolvedStyle.width;
                if (w > 0f && Mathf.Approximately(w, last)) break;
                last = w;
                waited += Time.deltaTime;
                yield return null;
            }
            _textWidth  = _newsLabel.resolvedStyle.width;
            _trackWidth = _tickerTrack.resolvedStyle.width;
            _currentX   = _trackWidth;
}

        private IEnumerator UpdateWidthOnly()
        {
            yield return null;
            yield return null;
            float waited = 0f;
            float last = 0f;
            while (waited < 2f)
            {
                float w = _newsLabel.resolvedStyle.width;
                if (w > 0f && Mathf.Approximately(w, last)) break;
                last = w;
                waited += Time.deltaTime;
                yield return null;
            }
            _textWidth  = _newsLabel.resolvedStyle.width;
            _trackWidth = _tickerTrack.resolvedStyle.width;
        }

        // ─────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────

        private void Update()
        {
            if (!_initialized || !_isScrolling)
                return;

            if (_newsLabel == null || _tickerTrack == null)
                return;

            if (_textWidth <= 0)
                return;

            _currentX -= scrollSpeed * Time.deltaTime;

            _newsLabel.style.translate = new Translate(_currentX, 0, 0);

            // Text has fully scrolled off the left edge
            if (_currentX < -_textWidth)
            {
                if (_isStartupScroll)
                {
                    // Startup pass is done — clear everything and go silent
                    _isStartupScroll = false;
                    _headlines.Clear();
                    _newsLabel.text  = "";
                    _isScrolling     = false;
// If real news already queued during startup, activate immediately
                    if (_pendingHeadlines.Count > 0)
                        StartCoroutine(ActivateWithPending());

                    return;
                }

                // Normal loop — drain any pending headlines
                if (_pendingHeadlines.Count > 0)
                {
                    DrainPendingIntoHeadlines();
                    SetLabelText();
                    StartCoroutine(ResetScrollAfterLayout());
                }
                else
                {
                    // No new news — loop seamlessly
                    _currentX = _trackWidth;
                }
            }
        }
    }
}