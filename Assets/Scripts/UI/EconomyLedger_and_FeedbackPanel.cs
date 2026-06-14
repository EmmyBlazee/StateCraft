// ============================================================
// FILE: Scripts/Systems/EconomyLedger.cs  (NEW FILE)
// PURPOSE: Records WHY the economy changed each month so the
//          UI can display a human-readable breakdown panel.
//
// INTEGRATION:
//   1. Add field in GameManager:
//      public EconomyLedger Ledger { get; private set; } = new EconomyLedger();
//
//   2. In EconomySystem.UpdateEconomy(), at the very end, call:
//      GameManager.Instance?.Ledger?.Record(country, /* values passed below */);
//      (See the RecordThisMonth() call pattern in the ECONOMY PATCH section.)
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using StateCraft.Data;
using StateCraft.Core;

namespace StateCraft.Systems
{
    // ── One month of changes, broken into named line items ───
    [Serializable]
    public class EconomyLedgerEntry
    {
        public int    month;
        public float  openingTreasury;
        public float  closingTreasury;
        public float  netChange;          // closing - opening

        // Income breakdown
        public float  taxRevenue;
        public float  resourceIncome;
        public float  tariffRevenue;
        public float  tradeIncome;        // from active trade agreements (seller)
        public float  foreignAid;

        // Spending breakdown
        public float  militaryUpkeep;
        public float  subsidyCost;
        public float  debtInterest;
        public float  operationsCost;
        public float  constructionCost;
        public float  corruptionLeakage;
        public float  uraniumImportFee;

        // Shocks / one-offs
        public float  gdpGrowthEffect;
        public float  inflationEffect;
        public float  warCost;
        public float  eventCost;

        // One-line human explanation for each large change
        public List<string> explanations = new List<string>();
    }

    [Serializable]
    public struct LedgerSaveWrapper {
        public string countryName;
        public List<EconomyLedgerEntry> entries;
    }

    public class EconomyLedger
    {
        // Store the last 12 months per country
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


// ============================================================
// ECONOMY PATCH — Paste this block at the BOTTOM of
// EconomySystem.UpdateEconomy(), replacing or appending after
// the existing Debug.Log line.
// ============================================================
/*
// ── Build the ledger entry for this month ─────────────────
var gm = GameManager.Instance;
if (gm?.Ledger != null && country == gm.playerCountry)
{
    float subsidyCostCalc = (country.gdp / 12f) * (country.subsidyLevel / 100f);
    float milUpkeep       = country.militaryBudgetUSD;
    float constCost       = 0f;
    if (country.activeProjects != null)
        foreach (var p in country.activeProjects)
            if (p.stage == ProjectStage.UnderConstruction)
                constCost += p.totalCost / Mathf.Max(1f, (float)p.totalMonths);

    float debtInt = gm.DebtSystem != null
        ? (gm.DebtSystem.CalculateDomesticInterest(country) + gm.DebtSystem.CalculateForeignInterest(country)) * country.exchangeRate
        : 0f;

    float corruptLeak = CorruptionSystem.GetLeakageAmount(country);

    float tradeInc = 0f;
    if (country.activeTradeAgreements != null)
        foreach (var deal in country.activeTradeAgreements)
            if (deal.status == TradeAgreementStatus.Active && deal.isSeller)
                tradeInc += (deal.monthlyPaymentUSD * country.exchangeRate) / 1e9f;

    float opsCost = 0f;
    if (country.activeOperations != null)
        foreach (var op in country.activeOperations)
            opsCost += (op.costPerMonth > 0f ? op.costPerMonth / 1000f : 0.05f) * country.exchangeRate;

    float warCostCalc = 0f;
    if (gm.WarSystem?.GetWarStage(country) == WarStage.Conflict)
        warCostCalc = country.gdp * 0.025f; // 2.5% GDP/month at war

    float net = country.income - country.spending - debtInt - corruptLeak - opsCost;

    var entry = new EconomyLedgerEntry
    {
        month           = gm.TurnSystem?.CurrentTurn ?? 0,
        openingTreasury = country.treasury - net,
        closingTreasury = country.treasury,
        netChange       = net,
        taxRevenue      = country.income * 0.6f,   // approximate split
        resourceIncome  = country.income * 0.3f,
        tariffRevenue   = country.income * 0.1f,
        tradeIncome     = tradeInc,
        militaryUpkeep  = milUpkeep,
        subsidyCost     = subsidyCostCalc,
        debtInterest    = debtInt,
        operationsCost  = opsCost,
        constructionCost= constCost,
        corruptionLeakage= corruptLeak,
        uraniumImportFee= country.isImportingUranium ? 0.05f : 0f,
        gdpGrowthEffect = country.gdp * country.gdpGrowthRate / 12f,
        inflationEffect = country.inflationRate,
        warCost         = warCostCalc,
    };

    // Build explanations for significant line items
    if (Mathf.Abs(net) > 0.5f)
        entry.explanations.Add($"Net change {(net >= 0 ? "+" : "")}{net:F2}B: " +
                               $"income {country.income:F2}B minus spending {country.spending:F2}B");
    if (debtInt > 0.1f)
        entry.explanations.Add($"Debt service: −{debtInt:F2}B (interest payments)");
    if (corruptLeak > 0.05f)
        entry.explanations.Add($"Corruption: −{corruptLeak:F2}B leaked from treasury");
    if (milUpkeep > 0.1f)
        entry.explanations.Add($"Military upkeep: −{milUpkeep:F2}B (active forces)");
    if (tradeInc > 0.05f)
        entry.explanations.Add($"Trade exports: +{tradeInc:F2}B (active agreements)");
    if (opsCost > 0.05f)
        entry.explanations.Add($"Operations: −{opsCost:F2}B ({country.activeOperations?.Count ?? 0} active)");
    if (warCostCalc > 0.1f)
        entry.explanations.Add($"War economy: −{warCostCalc:F2}B GDP drain from active conflict");
    if (country.inflationRate > 0.05f)
        entry.explanations.Add($"High inflation ({country.inflationRate:P1}) devaluing currency");
    if (country.gdp * country.gdpGrowthRate / 12f < 0f)
        entry.explanations.Add($"GDP contraction: economy shrinking at {country.gdpGrowthRate:P2}/month");

    gm.Ledger.Record(country, entry);
}
*/
// ─── END ECONOMY PATCH BLOCK ─────────────────────────────────


// ============================================================
// FILE: Scripts/UI/EconomyFeedbackPanel.cs  (NEW FILE)
// Attach to the EconomyDashboard UIDocument GameObject.
// Reads from GameManager.Ledger and populates a "WHY" panel.
// ============================================================

namespace StateCraft.UI
{
    public class EconomyFeedbackPanel : MonoBehaviour
    {
        private UIDocument  _uiDoc;
        private VisualElement _feedbackRoot;  // name this "EconomyFeedbackPanel" in UXML

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

            // Net change line
            if (_netChangeLabel != null)
            {
                string sign = entry.netChange >= 0f ? "+" : "";
                _netChangeLabel.text = $"This Month: {sign}{entry.netChange:F2}B  " +
                                       $"({sym}{entry.openingTreasury:F1}B → {sym}{entry.closingTreasury:F1}B)";
                _netChangeLabel.style.color = entry.netChange >= 0f
                    ? new StyleColor(new Color(0.3f, 1f, 0.4f))
                    : new StyleColor(new Color(1f, 0.35f, 0.35f));
            }

            // Income breakdown
            if (_incomeBreakLabel != null)
            {
                _incomeBreakLabel.text =
                    $"INCOME  Tax {entry.taxRevenue:F2}B  " +
                    $"Resources {entry.resourceIncome:F2}B  " +
                    $"Tariffs {entry.tariffRevenue:F2}B  " +
                    $"Trade exports +{entry.tradeIncome:F2}B";
            }

            // Spending breakdown
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

            // Explanations scroll list
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

                // Show last 3 months trend
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

// ============================================================
// UXML SNIPPET — add inside EconomyDashboard.uxml, inside
// whatever container is appropriate (e.g., after the main
// economy stat grid):
//
// <ui:VisualElement name="EconomyFeedbackPanel" class="ledger-panel">
//   <ui:Label text="MONTHLY ECONOMY BREAKDOWN" class="ledger-header"/>
//   <ui:Label name="LedgerNetChange"   class="ledger-net"/>
//   <ui:Label name="LedgerIncomeBreak" class="ledger-income"/>
//   <ui:Label name="LedgerSpendBreak"  class="ledger-spend"/>
//   <ui:ScrollView name="LedgerExplanations" class="ledger-scroll"/>
// </ui:VisualElement>
//
// Suggested USS (add to Global.uss or EconomyDashboard.uss):
//   .ledger-panel { margin-top: 12px; border-top-width: 1px;
//                   border-top-color: rgba(255,255,255,0.15); padding-top: 8px; }
//   .ledger-header { font-size: 11px; color: rgba(255,255,255,0.5);
//                    -unity-font-style: bold; margin-bottom: 4px; }
//   .ledger-net   { font-size: 14px; -unity-font-style: bold; }
//   .ledger-income, .ledger-spend { font-size: 11px; color: rgba(255,255,255,0.7);
//                                   margin-top: 2px; }
//   .ledger-explanation { font-size: 11px; color: rgba(255,220,150,0.9);
//                         padding: 2px 0; }
//   .ledger-trend       { font-size: 10px; color: rgba(150,200,255,0.7);
//                         margin-top: 6px; -unity-font-style: italic; }
// ============================================================
