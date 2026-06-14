using UnityEngine;
using TMPro;
using StateCraft.Data;

namespace StateCraft.UI
{
    public class CountryLabel3D : MonoBehaviour
    {
        public TextMeshPro nameText;
        public SpriteRenderer flagRenderer;
        public Transform stabilityBar; // A simple quad/plane that scales based on stability

        private CountryDataSO data;

        public void Setup(CountryDataSO countryData)
        {
            data = countryData;
            UpdateVisuals();
        }

        private void Update()
        {
            // Billboard effect: Make the label always face the camera
            if (Camera.main != null)
            {
                transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                                 Camera.main.transform.rotation * Vector3.up);
            }

            // Optional: Update stability bar scale in real-time
            if (data != null && stabilityBar != null)
            {
                float stabilityPercent = data.stability / 100f;
                stabilityBar.localScale = new Vector3(stabilityPercent, 1, 1);
            }
        }

        public void UpdateVisuals()
        {
            if (data == null) return;

            if (nameText != null) nameText.text = data.countryName.ToUpper();
            if (flagRenderer != null) flagRenderer.sprite = data.countryFlag;
        }
    }
}
