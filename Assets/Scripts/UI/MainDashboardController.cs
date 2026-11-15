using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StateCraft.Core;
using StateCraft.Data;

namespace StateCraft.UI
{
    public class MainDashboardController : MonoBehaviour
    {
        [Header("UI Elements")]
        public TMP_Text countryNameText;
        public TMP_Text gdpText;
        public TMP_Text stabilityText;
        public TMP_Text debtText;
        public TMP_Text turnText;
        public TMP_Text budgetText;

        [Header("Panels")]
        public GameObject infoPanel;
        public GameObject newsPanel;
        public GameObject diplomacyPanel;
        public GameObject globalDashboardPanel;

        [Header("Prefabs (Optional for dynamic spawning)")]
        public GameObject infoPrefab;
        public GameObject newsPrefab;
        public GameObject diplomacyPrefab;
        public GameObject globalDashboardPrefab;
        public Transform uiParent;

        [Header("Buttons")]
        public Button nextTurnButton;

        private void Start()
        {
            if (FindObjectOfType<MainDashboardToolkitController>() != null) { gameObject.SetActive(false); return; }

            if (nextTurnButton != null)
                nextTurnButton.onClick.AddListener(OnNextTurnClicked);

            UpdateUI();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded += UpdateUI;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnTurnEnded -= UpdateUI;
        }

        public void UpdateUI()
        {
            CountryDataSO playerCountry = GameManager.Instance.playerCountry;
            if (playerCountry == null) return;

            countryNameText.text = playerCountry.countryName;
            gdpText.text = $"GDP: ${playerCountry.gdp:N0}";
            stabilityText.text = $"STABILITY: {playerCountry.stability:F0}%";
            
            string symbol = string.IsNullOrEmpty(playerCountry.currencySymbol) ? "$" : playerCountry.currencySymbol;
            float debtLocal = playerCountry.debt * playerCountry.exchangeRate;
            debtText.text = $"DEBT: {UiFormat.MoneyRaw(debtLocal, symbol)}";
            
            turnText.text = $"{GameManager.Instance.TurnSystem.CurrentDate}";
            if (budgetText != null)
                budgetText.text = $"TREASURY: {UiFormat.Money(GameManager.Instance.playerBudget, symbol)}";
        }

        public void ToggleInfoPanel() => TogglePanel(ref infoPanel, infoPrefab);
        public void ToggleNewsPanel() => TogglePanel(ref newsPanel, newsPrefab);
        public void ToggleDiplomacyPanel() => TogglePanel(ref diplomacyPanel, diplomacyPrefab);
        public void ToggleGlobalDashboard() => TogglePanel(ref globalDashboardPanel, globalDashboardPrefab);

        private void TogglePanel(ref GameObject panel, GameObject prefab)
        {
            if (panel == null && prefab != null)
            {
                panel = Instantiate(prefab, uiParent != null ? uiParent : transform.parent);
                panel.SetActive(true);
}
            else if (panel != null)
            {
                panel.SetActive(!panel.activeSelf);
}
            else
            {
                Debug.LogWarning("DASHBOARD: Tried to toggle panel but both instance and prefab are null!");
            }
        }

        private void OnNextTurnClicked()
        {
            GameManager.Instance.EndTurn();
        }
    }
}
