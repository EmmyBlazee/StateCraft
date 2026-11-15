using UnityEngine;
using TMPro;
using StateCraft.Map;
using StateCraft.Data;
using StateCraft.Core;
using StateCraft.Interfaces;

namespace StateCraft.UI
{
    public class CountryInfoPanelController : MonoBehaviour
    {
        [Header("UI Elements")]
        public TMP_Text nameText;
        public UnityEngine.UI.Image flagImage;
        public TMP_Text gdpText;
        public TMP_Text stabilityText;
        public TMP_Text debtText;
        public TMP_Text securityText;
        public TMP_Text warStageText;

        [Header("Bars")]
        public UnityEngine.UI.Slider stabilitySlider;
        public UnityEngine.UI.Slider securitySlider;

        [Header("Actions")]
        public UnityEngine.UI.Button investButton;
        public TMP_Text budgetDisplay;

        [Header("Animation")]
        public GameObject panelRoot;
        private UIAnimationHelper animHelper;

        private CountryDataSO currentCountry;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            animHelper = panelRoot.GetComponent<UIAnimationHelper>();

            if (panelRoot != gameObject) panelRoot.SetActive(false);
            
            if (budgetDisplay == null)
            {
                var budgetObj = GameObject.Find("Budget_Text");
                if (budgetObj != null) budgetDisplay = budgetObj.GetComponent<TMP_Text>();
            }
        }

        private void OnEnable()
        {
            if (MapManager.Instance != null)
            {
                MapManager.Instance.OnCountrySelected -= UpdatePanel;
                MapManager.Instance.OnCountrySelected += UpdatePanel;
            }
        }

        private void Start()
        {
            if (MapManager.Instance != null)
            {
                MapManager.Instance.OnCountrySelected -= UpdatePanel;
                MapManager.Instance.OnCountrySelected += UpdatePanel;
            }
        }

        private void OnDisable()
        {
            if (MapManager.Instance != null)
                MapManager.Instance.OnCountrySelected -= UpdatePanel;
        }

        private void UpdatePanel(CountryDataSO country)
        {
            currentCountry = country;
            if (panelRoot != null) 
            {
                if (animHelper != null) animHelper.AnimateIn();
                else panelRoot.SetActive(true);
            }

            if (investButton != null)
            {
                investButton.onClick.RemoveAllListeners();
                investButton.onClick.AddListener(InvestInCountry);
            }

            RefreshDisplay();
        }

        private void Update()
        {
            if (budgetDisplay != null && GameManager.Instance != null)
            {
                var pc = GameManager.Instance.playerCountry;
                string symbol = (pc != null && !string.IsNullOrEmpty(pc.currencySymbol)) ? pc.currencySymbol : "$";
                budgetDisplay.text = $"BUDGET: {UiFormat.Money(GameManager.Instance.playerBudget, symbol)}";
            }

            if (panelRoot != null && panelRoot.activeSelf)
            {
                RefreshDisplay();
            }
        }

        public void InvestInCountry()
        {
            if (currentCountry == null || GameManager.Instance == null) return;

            var pc = GameManager.Instance.playerCountry;
            float cost = 1.0f * (pc != null ? pc.exchangeRate : 1.0f);
            if (GameManager.Instance.playerBudget >= cost)
            {
                GameManager.Instance.playerBudget -= cost;
                currentCountry.gdp *= 1.02f;
                currentCountry.stability += 5f;
                currentCountry.stability = Mathf.Clamp(currentCountry.stability, 0, 100);
                RefreshDisplay();
            }
        }

        public void ClosePanel()
        {
            if (panelRoot != null)
            {
                if (animHelper != null) animHelper.AnimateOut();
                else panelRoot.SetActive(false);
            }
        }

        public void RefreshDisplay()
        {
            if (currentCountry == null || GameManager.Instance == null) return;

            if (nameText != null) nameText.text = currentCountry.countryName.ToUpper();
            
            if (flagImage != null)
            {
                flagImage.sprite = currentCountry.countryFlag;
                flagImage.enabled = currentCountry.countryFlag != null;
            }
            
            if (gdpText != null) 
            {
                float valB = (currentCountry.gdp * currentCountry.exchangeRate);
                gdpText.text = $"<color=#AAAAAA>GDP:</color> {UiFormat.Money(valB, currentCountry.currencySymbol)}";
            }
            
            if (stabilityText != null) 
                stabilityText.text = $"<color=#AAAAAA>STABILITY:</color> {currentCountry.stability:N0}%";
            
            if (debtText != null) 
            {
                float valB = (currentCountry.debt * currentCountry.exchangeRate);
                debtText.text = $"<color=#AAAAAA>DEBT:</color> {UiFormat.Money(valB, currentCountry.currencySymbol)}";
            }
            
            if (securityText != null) 
                securityText.text = $"<color=#AAAAAA>SECURITY:</color> {currentCountry.security:N0}%";

            if (stabilitySlider != null) stabilitySlider.value = currentCountry.stability / 100f;
            if (securitySlider != null) securitySlider.value = currentCountry.security / 100f;

            if (warStageText != null)
            {
                var stage = GameManager.Instance.WarSystem.GetWarStage(currentCountry);
                string stageName = stage.ToString().ToUpper();
                warStageText.text = $"<color=#AAAAAA>STATUS:</color> <color=#{GetStatusColor(stage)}>{stageName}</color>";
            }
        }

        private string GetStatusColor(StateCraft.Interfaces.WarStage stage)
        {
            if (stage == StateCraft.Interfaces.WarStage.Peace) return "00FF00";
            if (stage == StateCraft.Interfaces.WarStage.Conflict) return "FF0000";
            return "FFFF00";
        }
    }
}
