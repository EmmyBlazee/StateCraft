using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace StateCraft.UI
{
    public class NewsTickerUI : MonoBehaviour
    {
        public TMP_Text tickerText;
        public float scrollSpeed = 100f;
        public float resetPosition = 1000f;
        public float startPosition = -1000f;

        private Queue<string> messageQueue = new Queue<string>();
        private string currentMessage;
        private RectTransform rectTransform;
        private float containerWidth;

        private void Start()
        {
            rectTransform = tickerText.GetComponent<RectTransform>();
            containerWidth = GetComponent<RectTransform>().rect.width;

            if (NotificationManager.Instance != null)
                NotificationManager.Instance.OnNotificationAdded += QueueMessage;

            if (Systems.NewsSystem.Instance != null)
            {
                Systems.NewsSystem.Instance.OnGlobalNewsPublished += (msg) => messageQueue.Enqueue(msg);
            }
        }

        private void QueueMessage(string message, Color color)
        {
            messageQueue.Enqueue(message);
        }

        private void Update()
        {
            if (currentMessage == null && messageQueue.Count > 0)
            {
                currentMessage = messageQueue.Dequeue();
                tickerText.text = currentMessage;
                // Start from the right side of the screen
                rectTransform.anchoredPosition = new Vector2(containerWidth, rectTransform.anchoredPosition.y);
            }

            if (currentMessage != null)
            {
                rectTransform.anchoredPosition += Vector2.left * scrollSpeed * Time.deltaTime;

                // If the text has scrolled all the way past the left edge
                if (rectTransform.anchoredPosition.x < -tickerText.preferredWidth)
                {
                    currentMessage = null;
                }
            }
        }
    }
}
