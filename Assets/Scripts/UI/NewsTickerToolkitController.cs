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

        private const int MaxPendingHeadlines = 20;

        private UIDocument _uiDocument;

        private Label _newsLabel;
        private VisualElement _tickerTrack;

        private readonly List<string> _headlines = new List<string>();
        private readonly Queue<string> _pendingHeadlines = new Queue<string>();

        private bool _initialized;
        private bool _isScrolling;

        private bool _isStartupScroll;

        private float _currentX;
        private float _textWidth;
        private float _trackWidth;

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
            if (NewsSystem.Instance != null)
                NewsSystem.Instance.OnGlobalNewsPublished -= AddNews;
        }

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

            var history = NewsSystem.Instance.GlobalNewsHistory;
            if (history != null)
            {
                foreach (var news in history)
                    _headlines.Add(news.ToUpper());
            }
            if (_headlines.Count > 0)
                _isStartupScroll = true;

            if (_headlines.Count > 0)
                SetLabelText();

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
            yield return null;
            yield return null;

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

            if (_trackWidth <= 0f)
            {
                float retryWait = 0f;
                while (retryWait < 2f && _trackWidth <= 0f)
                {
                    yield return new WaitForSeconds(0.5f);
                    retryWait += 0.5f;
                    _trackWidth = float.IsNaN(_tickerTrack.resolvedStyle.width)
                        ? 0f : _tickerTrack.resolvedStyle.width;
                }
            }

            if (_trackWidth <= 0f)
            {
                var root = _uiDocument?.rootVisualElement;
                float panelW = root != null && !float.IsNaN(root.resolvedStyle.width)
                    ? root.resolvedStyle.width : 0f;
                _trackWidth = panelW > 0f ? panelW : 900f;
            }

            _currentX    = _trackWidth;
            _isScrolling = true;
            _initialized = true;

            if (!_isStartupScroll && _pendingHeadlines.Count > 0)
                StartCoroutine(ActivateWithPending());

}

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

public void AddNews(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            string formatted = message.ToUpper();

            if (_pendingHeadlines.Count >= MaxPendingHeadlines)
            {
                _pendingHeadlines.Dequeue();
}

            _pendingHeadlines.Enqueue(formatted);
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

private void Update()
        {
            if (!_initialized || !_isScrolling)
                return;

            if (_textWidth <= 0)
                return;

            _currentX -= scrollSpeed * Time.deltaTime;

            _newsLabel.style.translate = new Translate(_currentX, 0, 0);

            if (_currentX < -_textWidth)
            {
                if (_isStartupScroll)
                {
                    _isStartupScroll = false;
                    _headlines.Clear();
                    _newsLabel.text  = "";
                    _isScrolling     = false;
                    if (_pendingHeadlines.Count > 0)
                        StartCoroutine(ActivateWithPending());

                    return;
                }

                if (_pendingHeadlines.Count > 0)
                {
                    DrainPendingIntoHeadlines();
                    SetLabelText();
                    StartCoroutine(ResetScrollAfterLayout());
                }
                else
                {
                    _currentX = _trackWidth;
                }
            }
        }
    }
}