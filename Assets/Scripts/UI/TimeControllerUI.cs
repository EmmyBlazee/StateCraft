using UnityEngine;
using TMPro;
using StateCraft.Core;

namespace StateCraft.UI
{
    public class TimeControllerUI : MonoBehaviour
    {
        public TMP_Text dateText;
        public float fastSpeed = 1f;
        public float normalSpeed = 5f;

        [Header("Button Visuals")]
        public UnityEngine.UI.Image pauseIcon;
        public UnityEngine.UI.Image playIcon;
        public UnityEngine.UI.Image fastIcon;
        public Color activeColor = Color.white;
        public Color inactiveColor = new Color(1, 1, 1, 0.3f);

        private void Start()
        {
            HighlightButton(playIcon);
        }

        private void Update()
        {
            if (GameManager.Instance == null || dateText == null) return;

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
