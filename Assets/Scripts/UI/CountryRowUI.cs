using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StateCraft.Data;
using StateCraft.Map;

namespace StateCraft.UI
{
    public class CountryRowUI : MonoBehaviour
    {
        public Image flagImage;
        public TMP_Text nameText;
        public Button viewButton;

        private CountryDataSO countryData;

        public void Setup(CountryDataSO data)
        {
            countryData = data;
            nameText.text = data.countryName.ToUpper();
            if (flagImage != null) flagImage.sprite = data.countryFlag;

            viewButton.onClick.RemoveAllListeners();
            viewButton.onClick.AddListener(OnViewClicked);
        }

        private void OnViewClicked()
        {
            // This tells the Map to select and jump to this country!
            if (MapManager.Instance != null)
            {
                MapManager.Instance.SelectCountry(countryData);
            }
        }
    }
}
