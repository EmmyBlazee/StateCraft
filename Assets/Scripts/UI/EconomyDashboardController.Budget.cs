using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using System.Linq;

namespace StateCraft.UI
{
    public partial class EconomyDashboardController
    {
        private void AdjustBudgetShare(ref float draft, float baseline, float delta, CountryDataSO country, BudgetAllocation alloc)
        {
            if (draft < 0f) draft = baseline;
            draft = Mathf.Clamp(draft + delta, 0f, 60f);
            
            string safeId = alloc.id.Replace(" ", "");
            if (_root?.Q<Label>($"Budget{safeId}_Pct") is Label l) l.text = $"{draft:F0}%"; 
            if (_root?.Q<ProgressBar>($"Budget{safeId}_Bar") is ProgressBar pb) pb.value = draft;
            float totalB = country.spending * country.exchangeRate;
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            if (_root?.Q<Label>($"Budget{safeId}_Value") is Label v) v.text = UiFormat.Money(totalB * (draft / 100f), sym);
        }

        private void SaveBudgetDraft(CountryDataSO country)
        {
            foreach (var kvp in _draftBudgets)
            {
                var alloc = country.budgetAllocations.FirstOrDefault(b => b.id == kvp.Key);
                if (alloc != null) alloc.sharePercentage = kvp.Value;
            }
            country.hasBudgetDraft = true;
            int turn = GameManager.Instance?.TurnSystem?.CurrentTurn ?? 0;
            country.lastBudgetDraftSummary = $"[Turn {turn}] Budget draft saved.";
            NotifyEconomy("Budget draft saved — use APPROVE BUDGET to commit.", Color.cyan);
        }

        private void ClearBudgetDrafts()
        {
            _draftBudgets.Clear();
        }

        private void ApproveBudgetPlan()
        {
            var gm = GameManager.Instance;
            var country = gm?.playerCountry;
            if (country == null) return;

            float totalPercentage = 0f;
            foreach (var kvp in _draftBudgets) totalPercentage += kvp.Value;
            foreach (var alloc in country.budgetAllocations) {
                if (!_draftBudgets.ContainsKey(alloc.id)) totalPercentage += alloc.sharePercentage;
            }

            float projectedSpending = country.income * (totalPercentage / 100f);
            float projectedDeficit = projectedSpending - country.income;

            if (projectedDeficit > 0f) {
                float projectedDeficitLocal = projectedDeficit * country.exchangeRate;
                float treasuryLocal = country.treasury * country.exchangeRate;
                if (projectedDeficitLocal > treasuryLocal) {
                    ShowEconomyModal("Deficit Unaffordable", 
                        $"You are projecting a deficit of {UiFormat.Money(projectedDeficitLocal, string.IsNullOrEmpty(country.currencySymbol) ? "?" : country.currencySymbol)} but only have {UiFormat.Money(treasuryLocal, string.IsNullOrEmpty(country.currencySymbol) ? "?" : country.currencySymbol)} in reserves. Check the Adviser panel to borrow money, or reduce allocations.", 
                        "", "UNDERSTOOD", () => {});
                    return;
                }
            }

            foreach (var kvp in _draftBudgets)
            {
                var alloc = country.budgetAllocations.FirstOrDefault(b => b.id == kvp.Key);
                if (alloc != null) alloc.sharePercentage = kvp.Value;
            }

            var mil = country.budgetAllocations.FirstOrDefault(b => b.id == "MILITARY");
            var intel = country.budgetAllocations.FirstOrDefault(b => b.id == "INTELLIGENCE");
            var edu = country.budgetAllocations.FirstOrDefault(b => b.id == "EDUCATION");

            float milShare = mil != null ? mil.sharePercentage : 0f;
            float intelShare = intel != null ? intel.sharePercentage : 0f;
            float eduShare = edu != null ? edu.sharePercentage : 0f;

            country.militaryBudgetUSD  = Mathf.Max(0.1f, country.income * (milShare / 100f));
            country.intelligenceBudget = Mathf.Max(0.1f, country.income * (intelShare / 100f));
            country.spending           = Mathf.Max(0f,   projectedSpending);

            int turn = GameManager.Instance?.TurnSystem?.CurrentTurn ?? 0;
            country.lastBudgetApprovalTurn = turn;

            if (projectedDeficit > 0f) {
                country.publicApproval     = Mathf.Max(0f, country.publicApproval - 5f);
                country.stability          = Mathf.Max(0f, country.stability      - 2f);
                country.lastBudgetDraftSummary = $"[Turn {turn}] Deficit budget approved: spending {totalPercentage:F0}%. Approval damaged.";
                NotifyEconomy("Deficit budget approved. Public approval damaged.", Color.red);
            } else {
                country.publicApproval     = Mathf.Min(100f, country.publicApproval + 1f);
                country.stability          = Mathf.Min(100f, country.stability      + 0.3f);
                country.lastBudgetDraftSummary = $"[Turn {turn}] Balanced budget approved: military {milShare:F0}%.";
                NotifyEconomy("Balanced budget approved and locked for the quarter.", Color.green);
            }

            country.hasBudgetDraft = false;
            ClearBudgetDrafts();
            
            AdviserEngine.Dismiss(country, "_refresh_"); 
            UpdateCurrentAdviserRecommendation(country);
            
            RefreshDisplay();
        }

        private void AutoAllocateBudget()
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            var dynamicWeights = AdviserEngine.GetAutoAllocateWeights(country);
            foreach(var alloc in country.budgetAllocations) {
                if (dynamicWeights.ContainsKey(alloc.id)) alloc.sharePercentage = dynamicWeights[alloc.id];
            }

            country.militaryBudgetUSD  = Mathf.Max(0.1f, country.income * 0.20f);
            country.intelligenceBudget = Mathf.Max(0.1f, country.income * 0.07f);
            country.securitySpending   = Mathf.Max(0.1f, country.income * 0.07f);
            country.spending           = Mathf.Max(0f,   country.income * 0.95f);
            country.publicApproval     = Mathf.Min(100f, country.publicApproval + 0.5f);
            country.stability          = Mathf.Min(100f, country.stability      + 0.5f);
            ClearBudgetDrafts();

            NotifyEconomy("Auto-allocation applied: balanced across all ministries.", Color.cyan);
            RefreshDisplay();
        }

        private void UpdateSubsidyLevel(float val)
        {
            if (GameManager.Instance?.playerCountry != null)
            {
                GameManager.Instance.playerCountry.subsidyLevel = val;
                if (_labelSubsidyLevel != null) _labelSubsidyLevel.text = $"{val:F0}%";
            }
        }

        private void RefreshBudgetTab(CountryDataSO country, string sym)
        {
            if (_root == null) return;
            if (_panelInfrastructure == null) return;
            var bp = _panelInfrastructure;
            country.EnsureBudgetAllocations();

            int currentTurn = GameManager.Instance?.TurnSystem?.CurrentTurn ?? 0;
            bool isBudgetLocked = currentTurn < country.lastBudgetApprovalTurn + 3;

            if (_budgetRowsContainer != null) {
                _budgetRowsContainer.style.display = isBudgetLocked ? DisplayStyle.None : DisplayStyle.Flex;
                var lockLabel = bp.Q<Label>("BudgetLockMessage");
                if (lockLabel == null) {
                    lockLabel = new Label();
                    lockLabel.name = "BudgetLockMessage";
                    lockLabel.style.color = new StyleColor(new Color(1f, 0.7f, 0.2f));
                    lockLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                    lockLabel.style.marginTop = 30;
                    lockLabel.style.marginBottom = 30;
                    lockLabel.style.whiteSpace = WhiteSpace.Normal;
                    lockLabel.style.fontSize = 14;
                    _budgetRowsContainer.parent.Insert(_budgetRowsContainer.parent.IndexOf(_budgetRowsContainer), lockLabel);
                }
                
                if (isBudgetLocked) {
                    int turnsLeft = (country.lastBudgetApprovalTurn + 3) - currentTurn;
                    lockLabel.text = $"?? Budget Locked\n\nYour allocations are actively fueling the nation.\nReturn in {turnsLeft} turns to reset your funding limits.";
                    lockLabel.style.display = DisplayStyle.Flex;
                } else {
                    lockLabel.style.display = DisplayStyle.None;
                }
            }

            var actionRows = bp.Query<VisualElement>(className: "budget-action-row").ToList();
            foreach(var r in actionRows) r.style.display = isBudgetLocked ? DisplayStyle.None : DisplayStyle.Flex;

            float baseSum = 0f;
            float draftSum = 0f;
            if (country.budgetAllocations != null) {
                foreach (var a in country.budgetAllocations) {
                    baseSum += a.sharePercentage;
                    draftSum += _draftBudgets.ContainsKey(a.id) ? _draftBudgets[a.id] : a.sharePercentage;
                }
            }
            float draftRatio = draftSum / 100f;

            float baseTotalB = country.income * country.exchangeRate;
            float totalExpB = baseTotalB * draftRatio;
            float revTotalB = baseTotalB;
            
            if (bp.Q<Label>("BudgetSummary_Revenue") is Label lRev) {
                lRev.text = UiFormat.Money(revTotalB, sym);
                lRev.RemoveFromClassList("negative");
                lRev.AddToClassList("positive");
            }

            if (bp.Q<Label>("BudgetSummary_Expenditure") is Label lExp) lExp.text = UiFormat.Money(totalExpB, sym);
            float balB = revTotalB - totalExpB;
            if (bp.Q<Label>("BudgetSummary_Balance") is Label lBal) {
                lBal.text = $"{(balB >= 0 ? "+" : "")}{UiFormat.Money(balB, sym)}";
                lBal.RemoveFromClassList("positive"); lBal.RemoveFromClassList("negative");
                lBal.AddToClassList(balB >= 0 ? "positive" : "negative");
            }
            if (bp.Q<Label>("BudgetSummary_Status") is Label lStatus) {
                string sText = balB >= 0 ? "BALANCED" : "DEFICIT";
                lStatus.text = sText;
                lStatus.RemoveFromClassList("positive"); lStatus.RemoveFromClassList("negative");
                lStatus.AddToClassList(balB >= 0 ? "positive" : "negative");
            }

            if (bp.Q<Label>("BudgetAvailable") is Label lAv) lAv.text = UiFormat.Money(revTotalB, sym) + " / quarter";
            if (bp.Q<Label>("BudgetLastBalance") is Label lLast) {
                float lastB = country.prevMonthBalance * country.exchangeRate;
                lLast.text = $"{(lastB >= 0 ? "+" : "")}{UiFormat.Money(lastB, sym)}";
            }

            country.EnsureBudgetAllocations();
            if (!_budgetRowsBuilt) BuildBudgetRows(country, sym);

            if (_budgetRowsContainer != null)
            {
                foreach (var alloc in country.budgetAllocations)
                {
                    float pct = _draftBudgets.ContainsKey(alloc.id) ? _draftBudgets[alloc.id] : alloc.sharePercentage;
                    string safeId = alloc.id.Replace(" ", "");
                    if (_budgetRowsContainer.Q<Label>($"Budget{safeId}_Value") is Label lblVal) lblVal.text = UiFormat.Money(baseTotalB * (pct / 100f), sym);
                    if (_budgetRowsContainer.Q<ProgressBar>($"Budget{safeId}_Bar") is ProgressBar bar) bar.value = pct;
                    if (_budgetRowsContainer.Q<Label>($"Budget{safeId}_Pct") is Label lblPct) lblPct.text = $"{pct:F0}%";
                }
            }

string[] dotColors = new string[] { "#E04040", "#7B5EAE", "#4A9EDD", "#3ACEA4", "#E0A030", "#5BAD5B", "#E07B30", "#8EC4E8" };
            if (bp.Q<Label>("BudgetStackedBar_Total") is Label lTotal) lTotal.text = UiFormat.Money(totalExpB, sym);
            var stackedBar = bp.Q<VisualElement>("BudgetStackedBar");
            var vLeg = bp.Q<VisualElement>("BudgetDonut_Legend");
            if (stackedBar != null) stackedBar.Clear();
            if (vLeg != null) vLeg.Clear();
            if (country.budgetAllocations != null && vLeg != null && stackedBar != null)
            {
                int colorIdx = 0;
                float unallocatedPct = 100f - draftSum;
                if (unallocatedPct < 0f) unallocatedPct = 0f;
                foreach (var alloc in country.budgetAllocations)
                {
                    float pct = _draftBudgets.ContainsKey(alloc.id) ? _draftBudgets[alloc.id] : alloc.sharePercentage;
                    if (pct <= 0) { colorIdx++; continue; }
                    string color = dotColors[colorIdx % dotColors.Length]; colorIdx++;
                    var barSegment = new VisualElement(); barSegment.style.flexGrow = pct; barSegment.style.backgroundColor = new StyleColor(HexToColor(color)); barSegment.style.height = Length.Percent(100f);
                    stackedBar.Add(barSegment);
                    var legRow = new VisualElement(); legRow.style.flexDirection = FlexDirection.Row; legRow.style.marginBottom = 3; legRow.style.alignItems = Align.Center;
                    var dot = new Label("●"); dot.style.color = new StyleColor(HexToColor(color)); dot.style.marginRight = 5; dot.style.fontSize = 10;
                    var nameLabel = new Label(alloc.title); nameLabel.style.color = new StyleColor(HexToColor(color)); nameLabel.style.flexGrow = 1; nameLabel.style.fontSize = 11;
                    var pctLabel = new Label(""+pct.ToString("F0")+"%"); pctLabel.style.color = new StyleColor(UnityEngine.Color.white); pctLabel.style.marginRight = 4; pctLabel.style.fontSize = 11;
                    var amtLabel = new Label("(" + UiFormat.Money(baseTotalB * pct / 100f, sym) + ")"); amtLabel.style.color = new StyleColor(new UnityEngine.Color(0.7f, 0.7f, 0.7f)); amtLabel.style.fontSize = 10;
                    legRow.Add(dot); legRow.Add(nameLabel); legRow.Add(pctLabel); legRow.Add(amtLabel);
                    vLeg.Add(legRow);
                }
                if (unallocatedPct > 0f) {
                    var emptySegment = new VisualElement(); emptySegment.style.flexGrow = unallocatedPct; emptySegment.style.backgroundColor = new StyleColor(new UnityEngine.Color(0,0,0,0));
                    stackedBar.Add(emptySegment);
                }
            }

            if (bp.Q<Label>("BudgetTrendChart") is Label lChart && country.budgetHistory != null) {
                lChart.text = "";
                lChart.Clear();
                lChart.style.flexDirection = FlexDirection.Row;
                lChart.style.alignItems = Align.FlexEnd;
                lChart.style.justifyContent = Justify.SpaceBetween;
                lChart.style.height = 70;
                lChart.style.paddingTop = 15;

                var hist = country.budgetHistory;
                
                float maxVal = 0.1f;
                foreach(float v in hist) { if (Mathf.Abs(v) > maxVal) maxVal = Mathf.Abs(v); }

                for (int i = 0; i < 6; i++) {
                    float val = 0f;
                    bool hasData = false;
                    
                    if (hist.Count > 0) {
                        int offset = 6 - hist.Count;
                        if (i >= offset) {
                            val = hist[i - offset];
                            hasData = true;
                        }
                    }

                    var barCont = new VisualElement();
                    barCont.style.width = Length.Percent(14f);
                    barCont.style.height = Length.Percent(100f);
                    barCont.style.justifyContent = Justify.FlexEnd;
                    barCont.style.alignItems = Align.Center;

                    var bar = new VisualElement();
                    bar.style.width = Length.Percent(100f);
                    
                    if (!hasData) {
                        bar.style.height = 4;
                        bar.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.5f));
                    } else {
                        float pct = Mathf.Clamp01(Mathf.Abs(val) / maxVal);
                        bar.style.height = Length.Percent(Mathf.Max(5f, pct * 100f));
                        if (val >= 0) bar.style.backgroundColor = new StyleColor(new Color(0.23f, 0.81f, 0.64f));
                        else bar.style.backgroundColor = new StyleColor(new Color(0.88f, 0.25f, 0.25f));
                    }
                    bar.style.borderTopLeftRadius = 2;
                    bar.style.borderTopRightRadius = 2;

                    barCont.Add(bar);
                    lChart.Add(barCont);
                }

                if (hist.Count == 0) {
                    var emptyLbl = new Label("No historical data available yet. Time must advance.");
                    emptyLbl.style.position = Position.Absolute;
                    emptyLbl.style.top = 25;
                    emptyLbl.style.left = 0;
                    emptyLbl.style.right = 0;
                    emptyLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                    emptyLbl.style.color = new Color(0.6f, 0.6f, 0.6f);
                    emptyLbl.style.fontSize = 11;
                    lChart.Add(emptyLbl);
                }
            }

            if (bp.Q<Label>("BudgetEffect_Growth") is Label lEg) lEg.text = $"Economic Growth\n{(country.gdpGrowthRate > 0 ? "+" : "")}{country.gdpGrowthRate:P1}";
            if (bp.Q<Label>("BudgetEffect_Military") is Label lEm) lEm.text = $"Military Readiness\n{country.militaryReadiness:F0}%";
            if (bp.Q<Label>("BudgetEffect_Inflation") is Label lEi) lEi.text = $"Inflation\n{country.inflationRate:P1}";
            if (bp.Q<Label>("BudgetEffect_Employment") is Label lEmp) lEmp.text = "Employment Base\nStable";
            if (bp.Q<Label>("BudgetEffect_Approval") is Label lEa) lEa.text = $"Approval Rating\n{country.publicApproval:F0}%";
            if (bp.Q<Label>("BudgetEffect_Debt") is Label lEd) lEd.text = $"Debt Change\n{(balB < 0 ? "+" : "")}{UiFormat.Money(Mathf.Abs(balB), sym)}";

            var alertsCont = _root.Q<VisualElement>("BudgetAlertsContainer");
            if (alertsCont != null) {
                while (alertsCont.childCount > 1) alertsCont.RemoveAt(1);
                var alerts = BuildUIAlerts(country).FindAll(a => a.Severity == AlertSeverity.Critical);
                int ct = 0;
                foreach (var a in alerts) {
                    if (ct >= 3) break;
                    var row = new VisualElement(); row.AddToClassList("budget-alert-row");
                    row.Add(new Label($"⚠ {a.Message}"));
                    var lblSev = new Label(a.Severity.ToString().ToUpper()); 
                    lblSev.AddToClassList(a.Severity == AlertSeverity.Critical ? "negative" : "warning");
                    row.Add(lblSev);
                    alertsCont.Add(row);
                    ct++;
                }
                if (ct == 0) {
                    var empty = new Label("No critical budget alerts."); empty.AddToClassList("budget-copy"); empty.style.marginTop = 10;
                    alertsCont.Add(empty);
                }
            }

            if (_currentAdviserRec != null) {
                if (_root.Q<Label>("BudgetAdviserBody") is Label lAdv) lAdv.text = _currentAdviserRec.Value.Rationale;
            }
        }

        private void BuildBudgetRows(CountryDataSO country, string sym)
        {
            if (_budgetRowsContainer == null)
            {
                Debug.LogWarning("[Budget] BuildBudgetRows: _budgetRowsContainer is null! Cannot build rows.");
                return;
            }
            country.EnsureBudgetAllocations();
            if (country.budgetAllocations == null || country.budgetAllocations.Count == 0)
            {
                Debug.LogWarning("[Budget] BuildBudgetRows: budgetAllocations is empty!");
                return;
            }

            _budgetRowsContainer.Clear();
            float baseSum = 0f;
            float draftSum = 0f;
            foreach (var a in country.budgetAllocations) {
                baseSum += a.sharePercentage;
                draftSum += _draftBudgets.ContainsKey(a.id) ? _draftBudgets[a.id] : a.sharePercentage;
            }
            float draftRatio = baseSum > 0f ? (draftSum / baseSum) : 1f;
            float totalB = country.spending * country.exchangeRate * draftRatio;
            foreach (var alloc in country.budgetAllocations)
            {
                var capturedAlloc = alloc;
                var row = new VisualElement(); row.AddToClassList("budget-row");

                string symIcon = "O";
                switch (capturedAlloc.id) {
                    case "MILITARY": symIcon = ""; break;
                    case "INTELLIGENCE": symIcon = ""; break;
                    case "EDUCATION": symIcon = "✎"; break;
                    case "HEALTH": symIcon = "✚"; break;
                    case "INFRASTRUCTURE": symIcon = "⌂"; break;
                    case "AGRICULTURE": symIcon = "✿"; break;
                    case "ENERGY": symIcon = "☼"; break;
                    case "WELFARE": symIcon = "♥"; break;
                }
                var icon = new Label(symIcon); icon.AddToClassList("budget-icon");
                if (capturedAlloc.id == "MILITARY") icon.AddToClassList("budget-icon-military");
                if (capturedAlloc.id == "INTELLIGENCE") icon.AddToClassList("budget-icon-intel");

                var deptGroup = new VisualElement(); deptGroup.AddToClassList("budget-dept");
                var lblTitle = new Label(capturedAlloc.title); lblTitle.AddToClassList("budget-row-title");
                var lblDesc  = new Label(capturedAlloc.description); lblDesc.AddToClassList("budget-copy");
                deptGroup.Add(lblTitle); deptGroup.Add(lblDesc);

                string safeId = capturedAlloc.id.Replace(" ", "");
                var amountGroup = new VisualElement(); amountGroup.AddToClassList("budget-amount");
                var lblVal = new Label { name = $"Budget{safeId}_Value" }; lblVal.AddToClassList("budget-value");
                var bar    = new ProgressBar { name = $"Budget{safeId}_Bar", highValue = 100f }; bar.AddToClassList("budget-bar");
                var lblPct = new Label { name = $"Budget{safeId}_Pct" };
                amountGroup.Add(lblVal); amountGroup.Add(bar); amountGroup.Add(lblPct);

                var btnMinus = new Button { name = "BudgetStepBtn", text = "\u2212" }; btnMinus.AddToClassList("budget-step");
                btnMinus.clicked += () => {
                    if (!_draftBudgets.ContainsKey(capturedAlloc.id)) _draftBudgets[capturedAlloc.id] = capturedAlloc.sharePercentage;
                    float d = _draftBudgets[capturedAlloc.id];
                    AdjustBudgetShare(ref d, capturedAlloc.sharePercentage, -1f, country, capturedAlloc);
                    _draftBudgets[capturedAlloc.id] = d;
                    RefreshBudgetTab(country, sym);
                };

                var btnPlus = new Button { name = "BudgetStepBtn", text = "+" }; btnPlus.AddToClassList("budget-step");
                btnPlus.clicked += () => {
                    if (!_draftBudgets.ContainsKey(capturedAlloc.id)) _draftBudgets[capturedAlloc.id] = capturedAlloc.sharePercentage;
                    float d = _draftBudgets[capturedAlloc.id];
                    AdjustBudgetShare(ref d, capturedAlloc.sharePercentage, 1f, country, capturedAlloc);
                    _draftBudgets[capturedAlloc.id] = d;
                    RefreshBudgetTab(country, sym);
                };

                float pct = capturedAlloc.sharePercentage;
                lblVal.text = UiFormat.Money(totalB * (pct / 100f), sym);
                bar.value = pct;
                lblPct.text = $"{pct:F0}%";

                row.Add(icon); row.Add(deptGroup); row.Add(btnMinus); row.Add(amountGroup); row.Add(btnPlus);
                _budgetRowsContainer.Add(row);
            }

            _budgetRowsBuilt = true;
            Debug.Log($"[Budget] BuildBudgetRows: {_budgetRowsContainer.childCount} rows built into container.");
        }

    }
}