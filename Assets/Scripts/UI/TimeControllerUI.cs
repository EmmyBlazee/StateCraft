using UnityEngine;
using TMPro;
using StateCraft.Core;

namespace StateCraft.UI
{
    public class TimeControllerUI : MonoBehaviour
    {
        public TMP_Text dateText;
        public float fastSpeed = 1f; // Seconds per month in fast mode
        public float normalSpeed = 5f; // Seconds per month in normal mode

        [Header("Button Visuals")]
        public UnityEngine.UI.Image pauseIcon;
        public UnityEngine.UI.Image playIcon;
        public UnityEngine.UI.Image fastIcon;
        public Color activeColor = Color.white;
        public Color inactiveColor = new Color(1, 1, 1, 0.3f);

        private void Start()
        {
            // Set initial highlight
            HighlightButton(playIcon);
        }

        private void Update()
        {
            if (GameManager.Instance == null || dateText == null) return;

            // Update the date display from the GameManager's turn system
            dateText.text = GameManager.Instance.TurnSystem.CurrentDate;
        }

        public void PauseGame()
        {
            GameManager.Instance.isRealTime = false;
            HighlightButton(pauseIcon);
}

        public void PlayNormal()
        {
            GameManager.Instance.isRealTime = true;
            GameManager.Instance.turnDuration = normalSpeed;
            HighlightButton(playIcon);
}

        public void PlayFast()
        {
            GameManager.Instance.isRealTime = true;
            GameManager.Instance.turnDuration = fastSpeed;
            HighlightButton(fastIcon);
}

        private void HighlightButton(UnityEngine.UI.Image target)
        {
            if (pauseIcon != null) pauseIcon.color = inactiveColor;
            if (playIcon != null) playIcon.color = inactiveColor;
            if (fastIcon != null) fastIcon.color = inactiveColor;

            if (target != null) target.color = activeColor;
        }
    }
}
