
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using StateCraft.Data;
using StateCraft.Core;

namespace StateCraft.Systems
{
    [Serializable]
    public class EconomyLedgerEntry
    {
        public int    month;
        public float  openingTreasury;
        public float  closingTreasury;
        public float  netChange;

        public float  taxRevenue;
        public float  resourceIncome;
        public float  tariffRevenue;
        public float  tradeIncome;
        public float  foreignAid;

        public float  militaryUpkeep;
        public float  subsidyCost;
        public float  debtInterest;
        public float  operationsCost;
        public float  constructionCost;
        public float  corruptionLeakage;
        public float  uraniumImportFee;

        public float  gdpGrowthEffect;
        public float  inflationEffect;
        public float  warCost;
        public float  eventCost;

        public List<string> explanations = new List<string>();
    }

    [Serializable]
    public struct LedgerSaveWrapper {
        public string countryName;
        public List<EconomyLedgerEntry> entries;
    }

    public class EconomyLedger
    {
        private Dictionary<string, List<EconomyLedgerEntry>> _history
            = new Dictionary<string, List<EconomyLedgerEntry>>(StringComparer.OrdinalIgnoreCase);

        public void Record(CountryDataSO country, EconomyLedgerEntry entry)
        {
            if (!_history.TryGetValue(country.countryName, out var list))
            {
                list = new List<EconomyLedgerEntry>();
                _history[country.countryName] = list;
            }
            list.Add(entry);
            if (list.Count > 12) list.RemoveAt(0);
        }

        public List<EconomyLedgerEntry> GetHistory(string countryName)
        {
            _history.TryGetValue(countryName, out var list);
            return list ?? new List<EconomyLedgerEntry>();
        }

        public EconomyLedgerEntry GetLastMonth(string countryName)
        {
            var list = GetHistory(countryName);
            return list.Count > 0 ? list[list.Count - 1] : null;
        }

        public List<LedgerSaveWrapper> ExportLedger()
        {
            var list = new List<LedgerSaveWrapper>();
            foreach(var kvp in _history)
                list.Add(new LedgerSaveWrapper { countryName = kvp.Key, entries = kvp.Value });
            return list;
        }

        public void ImportLedger(List<LedgerSaveWrapper> data)
        {
            _history.Clear();
            if (data == null) return;
            foreach (var w in data)
                _history[w.countryName] = w.entries;
        }
    }
}

namespace StateCraft.UI
{
    public class EconomyFeedbackPanel : MonoBehaviour
    {
        private UIDocument  _uiDoc;
        private VisualElement _feedbackRoot;

        private Label       _netChangeLabel;
        private Label       _incomeBreakLabel;
        private Label       _spendBreakLabel;
        private ScrollView  _explanationList;

        private void Awake()
        {
            _uiDoc = GetComponent<UIDocument>();
            if (_uiDoc == null) return;

            var root = _uiDoc.rootVisualElement;
            _feedbackRoot     = root?.Q<VisualElement>("EconomyFeedbackPanel");
            _netChangeLabel   = _feedbackRoot?.Q<Label>("LedgerNetChange");
            _incomeBreakLabel = _feedbackRoot?.Q<Label>("LedgerIncomeBreak");
            _spendBreakLabel  = _feedbackRoot?.Q<Label>("LedgerSpendBreak");
            _explanationList  = _feedbackRoot?.Q<ScrollView>("LedgerExplanations");
        }

        private void OnEnable()
        {
            if (GameManager.Instance?.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnMonthEnded += RefreshFeedback;
            RefreshFeedback();
        }

        private void OnDisable()
        {
            if (GameManager.Instance?.TurnSystem != null)
                GameManager.Instance.TurnSystem.OnMonthEnded -= RefreshFeedback;
        }

        public void RefreshFeedback()
        {
            if (_feedbackRoot == null) return;

            var gm      = GameManager.Instance;
            var player  = gm?.playerCountry;
            var ledger  = gm?.Ledger;
            if (player == null || ledger == null) return;

            var entry = ledger.GetLastMonth(player.countryName);
            if (entry == null)
            {
                if (_netChangeLabel != null)
                    _netChangeLabel.text = "No data yet — end the first month to see breakdown.";
                return;
            }

            string sym = string.IsNullOrEmpty(player.currencySymbol) ? "$" : player.currencySymbol;

            if (_netChangeLabel != null)
            {
                string sign = entry.netChange >= 0f ? "+" : "";
                _netChangeLabel.text = $"This Month: {sign}{entry.netChange:F2}B  " +
                                       $"({sym}{entry.openingTreasury:F1}B → {sym}{entry.closingTreasury:F1}B)";
                _netChangeLabel.style.color = entry.netChange >= 0f
                    ? new StyleColor(new Color(0.3f, 1f, 0.4f))
                    : new StyleColor(new Color(1f, 0.35f, 0.35f));
            }

            if (_incomeBreakLabel != null)
            {
                _incomeBreakLabel.text =
                    $"INCOME  Tax {entry.taxRevenue:F2}B  " +
                    $"Resources {entry.resourceIncome:F2}B  " +
                    $"Tariffs {entry.tariffRevenue:F2}B  " +
                    $"Trade exports +{entry.tradeIncome:F2}B";
            }

            if (_spendBreakLabel != null)
            {
                _spendBreakLabel.text =
                    $"SPENDING  Military {entry.militaryUpkeep:F2}B  " +
                    $"Subsidies {entry.subsidyCost:F2}B  " +
                    $"Debt int. {entry.debtInterest:F2}B  " +
                    $"Ops {entry.operationsCost:F2}B  " +
                    $"Corruption -{entry.corruptionLeakage:F2}B" +
                    (entry.warCost > 0f ? $"  WAR -{entry.warCost:F2}B" : "");
            }

            if (_explanationList != null)
            {
                _explanationList.Clear();
                if (entry.explanations.Count == 0)
                {
                    var noChange = new Label("Economy stable — no significant changes this month.");
                    noChange.AddToClassList("ledger-explanation");
                    _explanationList.Add(noChange);
                }
                else
                {
                    foreach (var line in entry.explanations)
                    {
                        var lbl = new Label("• " + line);
                        lbl.AddToClassList("ledger-explanation");
                        _explanationList.Add(lbl);
                    }
                }

                var history = ledger.GetHistory(player.countryName);
                if (history.Count >= 2)
                {
                    var trend = new Label("");
                    trend.AddToClassList("ledger-trend");
                    var parts = new List<string>();
                    int start = Mathf.Max(0, history.Count - 3);
                    for (int i = start; i < history.Count; i++)
                    {
                        string s = history[i].netChange >= 0f
                            ? $"+{history[i].netChange:F1}B"
                            : $"{history[i].netChange:F1}B";
                        parts.Add($"M{history[i].month}: {s}");
                    }
                    trend.text = "3-Month trend: " + string.Join("  →  ", parts);
                    _explanationList.Add(trend);
                }
            }
        }
    }
}

