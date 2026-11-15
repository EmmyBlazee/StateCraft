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
        private void AdjustTaxOrBudgetField(Button button, float delta)
        {
            Slider slider = button.parent?.Q<Slider>();
            if (slider != null)
            {
                slider.value = Mathf.Clamp(slider.value + delta, slider.lowValue, slider.highValue);
                return;
            }

            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            string ctx = GetAncestorText(button, 4).ToUpperInvariant();

var alloc = country.budgetAllocations.FirstOrDefault(b => ctx.Contains(b.id) || ctx.Contains(b.title.ToUpperInvariant()));
            if (alloc != null)
            {
                if (!_draftBudgets.ContainsKey(alloc.id)) _draftBudgets[alloc.id] = alloc.sharePercentage;
                float currentDraft = _draftBudgets[alloc.id];
                AdjustBudgetShare(ref currentDraft, alloc.sharePercentage, delta, country, alloc);
                _draftBudgets[alloc.id] = currentDraft;
                return;
            }

            RefreshDisplay(); NotifyEconomy("Control adjusted.", Color.grey);
        }

        private void CommitTaxDrafts(CountryDataSO country, string sym)
        {
            if (_draftIncomeTax    >= 0f) country.incomeTaxRate    = _draftIncomeTax;
            if (_draftCorporateTax >= 0f) country.corporateTaxRate = _draftCorporateTax;
            if (_draftVAT          >= 0f) country.vatRate           = _draftVAT;
            if (_draftExportDuty   >= 0f) country.exportDutyRate    = _draftExportDuty;

            country.taxRate = country.incomeTaxRate   * 0.35f
                            + country.corporateTaxRate* 0.30f
                            + country.vatRate          * 0.25f
                            + country.exportDutyRate   * 0.10f;

            country.publicApproval = Mathf.Max(0f, country.publicApproval - 1f);
            country.inflationRate += country.vatRate * 0.0005f;

            string entry = $"Tax reform committed — Income:{country.incomeTaxRate:F1}% Corp:{country.corporateTaxRate:F1}% VAT:{country.vatRate:F1}% Export:{country.exportDutyRate:F1}% (blended {country.taxRate:F1}%)";
            if (country.taxReformLog == null) country.taxReformLog = new System.Collections.Generic.List<string>();
            int turn = GameManager.Instance?.TurnSystem?.CurrentTurn ?? 0;
            country.taxReformLog.Insert(0, $"[Turn {turn}] {entry}");
            if (country.taxReformLog.Count > 20) country.taxReformLog.RemoveAt(country.taxReformLog.Count - 1);
            country.lastTaxReformSummary = entry;

            ClearTaxDrafts();
            NotifyEconomy("Tax changes applied and logged to reform history.", Color.green);
            RefreshDisplay();
        }

        private void ClearTaxDrafts()
        {
            _draftIncomeTax = _draftCorporateTax = _draftVAT = _draftExportDuty = -1f;
        }

        private void UpdateTaxRate(float val)
        {
            if (GameManager.Instance?.playerCountry != null)
            {
                var country = GameManager.Instance.playerCountry;
                country.taxRate = val;
                country.incomeTaxRate = val;
                country.corporateTaxRate = val;
                if (_labelTaxRate != null) _labelTaxRate.text = $"{val:F0}%";
            }
        }

        private void RefreshTaxationTab(CountryDataSO country, string sym)
        {
            if (_root == null) { Debug.LogError("[TaxUI] RefreshTaxationTab — _root is NULL"); return; }

            if (_lblTaxTotalRevenue    == null) _lblTaxTotalRevenue        = _root.Q<Label>("TaxTotal_Revenue");
            if (_lblTaxTotalRatio      == null) _lblTaxTotalRatio          = _root.Q<Label>("TaxTotal_Ratio");
            if (_lblTaxTotalMoM        == null) _lblTaxTotalMoM            = _root.Q<Label>("TaxTotal_MoMDelta");
            if (_lblTaxComplianceRate  == null) _lblTaxComplianceRate      = _root.Q<Label>("TaxCompliance_Rate");
            if (_lblTaxCollectionEff   == null) _lblTaxCollectionEff       = _root.Q<Label>("TaxCollectionEff");
            if (_lblTaxGap             == null) _lblTaxGap                 = _root.Q<Label>("TaxGap");
            if (_lblTaxArrears         == null) _lblTaxArrears             = _root.Q<Label>("TaxArrears");
            if (_lblComplianceDonut    == null) _lblComplianceDonut        = _root.Q<Label>("ComplianceDonut_Label");
            if (_lblComplianceCompliant    == null) _lblComplianceCompliant    = _root.Q<Label>("ComplianceCompliant");
            if (_lblCompliancePartial      == null) _lblCompliancePartial      = _root.Q<Label>("CompliancePartial");
            if (_lblComplianceNonCompliant == null) _lblComplianceNonCompliant = _root.Q<Label>("ComplianceNonCompliant");
            if (_lblSimSelectedTax     == null) _lblSimSelectedTax         = _root.Q<Label>("SimSelectedTax");
            if (_lblSimCurrentRate     == null) _lblSimCurrentRate         = _root.Q<Label>("SimCurrentRate");
            if (_lblSimNewRate         == null) _lblSimNewRate             = _root.Q<Label>("SimNewRate");
            if (_lblSimRevenue         == null) _lblSimRevenue             = _root.Q<Label>("SimRevenue");
            if (_lblSimGDPImpact       == null) _lblSimGDPImpact           = _root.Q<Label>("SimGDPImpact");
            if (_lblSimApproval        == null) _lblSimApproval            = _root.Q<Label>("SimApproval");
            if (_lblSimConfidence      == null) _lblSimConfidence          = _root.Q<Label>("SimConfidence");
            if (_lblSimInvestment      == null) _lblSimInvestment          = _root.Q<Label>("SimInvestment");
            if (_lblSimEmployment      == null) _lblSimEmployment          = _root.Q<Label>("SimEmployment");
            if (_lblSimInflation       == null) _lblSimInflation           = _root.Q<Label>("SimInflation");
            if (_lblTaxSimTotal        == null) _lblTaxSimTotal            = _root.Q<Label>("TaxSim_Total");
            if (_lblTaxSimAdvisory     == null) _lblTaxSimAdvisory         = _root.Q<Label>("TaxSim_Advisory");
            if (_taxPoliciesContainer  == null) _taxPoliciesContainer      = _root.Q<VisualElement>("TaxPoliciesContainer");

            Debug.Log($"[TaxUI] RefreshTaxationTab — simulatorPreviewKey={_simSelectedTaxKey}");

            float draftInc  = GetDraftRate(country, "INCOME");
            float draftCorp = GetDraftRate(country, "CORPORATE");
            float draftVat  = GetDraftRate(country, "VAT");
            float draftImp  = GetDraftRate(country, "IMPORT");
            float draftExp  = GetDraftRate(country, "EXPORT");

            var (lblInc, barInc, revInc, baseInc, shareInc) = _taxRowRefs.TryGetValue("INCOME", out var incRef) ? incRef : (null, null, null, null, null);
            Debug.Log($"[TaxUI]   TaxIncome_Rate label={(lblInc != null ? "FOUND" : "NULL")}  TaxIncome_Bar slider={(barInc != null ? "FOUND" : "NULL")}");

            if (lblInc != null) lblInc.text = $"{draftInc:F1}%";
            if (barInc != null) barInc.SetValueWithoutNotify(draftInc);
            float incRev = StateCraft.Systems.EconomyLogicEvaluator.GetTaxRevenue(country, "Income", draftInc) * country.exchangeRate;
            if (revInc != null) revInc.text = UiFormat.Money(incRev, sym);

            var (lblCorp, barCorp, revCorp, baseCorp, shareCorp) = _taxRowRefs.TryGetValue("CORPORATE", out var corpRefTuple) ? corpRefTuple : (null, null, null, null, null);
            float corpRev = StateCraft.Systems.EconomyLogicEvaluator.GetTaxRevenue(country, "Corporate", draftCorp) * country.exchangeRate;
            if (revCorp != null) revCorp.text = UiFormat.Money(corpRev, sym);
            Debug.Log($"[TaxUI]   TaxCorporate_Rate label={(lblCorp != null ? "FOUND" : "NULL")}  TaxCorporate_Bar slider={(barCorp != null ? "FOUND" : "NULL")}");
            if (lblCorp != null) lblCorp.text = $"{draftCorp:F1}%";
            if (barCorp != null) barCorp.SetValueWithoutNotify(draftCorp);

            var (lblVat, barVat, revVatLbl, baseVat, shareVat) = _taxRowRefs.TryGetValue("VAT", out var vatRefTuple) ? vatRefTuple : (null, null, null, null, null);
            if (lblVat != null) lblVat.text = $"{draftVat:F1}%";
            if (barVat != null) barVat.SetValueWithoutNotify(draftVat);
            float vatRev = StateCraft.Systems.EconomyLogicEvaluator.GetTaxRevenue(country, "VAT", draftVat) * country.exchangeRate;
            if (revVatLbl != null) revVatLbl.text = UiFormat.Money(vatRev, sym);

            var (lblImp, barImp, revImpLbl, baseImp, shareImp) = _taxRowRefs.TryGetValue("IMPORT", out var impRefTuple) ? impRefTuple : (null, null, null, null, null);
            float impRev = StateCraft.Systems.EconomyLogicEvaluator.GetTaxRevenue(country, "Import", draftImp) * country.exchangeRate;
            if (revImpLbl != null) revImpLbl.text = UiFormat.Money(impRev, sym);
            if (lblImp != null) lblImp.text = $"{draftImp:F1}%";
            if (barImp != null) barImp.SetValueWithoutNotify(draftImp);

            var (lblExp, barExp, revExpLbl, baseExp, shareExp) = _taxRowRefs.TryGetValue("EXPORT", out var expRefTuple) ? expRefTuple : (null, null, null, null, null);
            if (lblExp != null) lblExp.text = $"{draftExp:F1}%";
            if (barExp != null) barExp.SetValueWithoutNotify(draftExp);
            float expRev = StateCraft.Systems.EconomyLogicEvaluator.GetTaxRevenue(country, "Export", draftExp) * country.exchangeRate;
            if (revExpLbl != null) revExpLbl.text = UiFormat.Money(expRev, sym);

            float total = incRev + corpRev + vatRev + impRev + expRev;
            if (_lblTaxTotalRevenue != null) _lblTaxTotalRevenue.text = UiFormat.Money(total, sym);

            float gdpLocal = country.gdp * country.exchangeRate;
            if (_lblTaxTotalRatio != null) _lblTaxTotalRatio.text = $"{(gdpLocal > 0 ? ((total * 12f) / gdpLocal * 100f) : 0f):F1}%";

            if (baseInc   != null) baseInc.text   = $"Base: {country.incomeTaxRate:F1}%";
            if (shareInc  != null) shareInc.text  = $"{(total > 0 ? (incRev / total * 100f) : 0f):F1}% of Total Revenue";

            if (baseCorp  != null) baseCorp.text  = $"Base: {country.corporateTaxRate:F1}%";
            if (shareCorp != null) shareCorp.text = $"{(total > 0 ? (corpRev / total * 100f) : 0f):F1}% of Total Revenue";

            if (baseVat   != null) baseVat.text   = $"Base: {country.vatRate:F1}%";
            if (shareVat  != null) shareVat.text  = $"{(total > 0 ? (vatRev / total * 100f) : 0f):F1}% of Total Revenue";

            if (baseImp   != null) baseImp.text   = $"Base: {country.importTariffRate:F1}%";
            if (shareImp  != null) shareImp.text  = $"{(total > 0 ? (impRev / total * 100f) : 0f):F1}% of Total Revenue";

            if (baseExp   != null) baseExp.text   = $"Base: {country.exportDutyRate:F1}%";
            if (shareExp  != null) shareExp.text  = $"{(total > 0 ? (expRev / total * 100f) : 0f):F1}% of Total Revenue";
            if (_lblTaxTotalMoM != null) {
                float prevTaxRev = country.prevMonthlyTaxRevenue * country.exchangeRate;
                float diff = total - prevTaxRev;
                float pct = prevTaxRev > 0 ? (diff / prevTaxRev) * 100f : 0f;
                _lblTaxTotalMoM.text = $"{(diff >= 0 ? "+" : "")}{UiFormat.Money(diff, sym)} ({(diff >= 0 ? "+" : "")}{pct:F1}%)";
                _lblTaxTotalMoM.RemoveFromClassList("positive");
                _lblTaxTotalMoM.RemoveFromClassList("negative");
                _lblTaxTotalMoM.AddToClassList(diff >= 0 ? "positive" : "negative");
            }

            float compliance = Mathf.Clamp(100f - country.corruptionIndex, 10f, 100f);
            float eff = Mathf.Lerp(60f, 95f, country.stability / 100f);
            float collectionEff = eff / 100f;
            float gap = total * (1f - (compliance / 100f) * collectionEff);

            if (_lblTaxComplianceRate != null) _lblTaxComplianceRate.text = $"{compliance:F0}%";
            if (_lblTaxCollectionEff  != null) _lblTaxCollectionEff.text  = $"{eff:F0}%";
            if (_lblTaxGap            != null) _lblTaxGap.text            = UiFormat.Money(gap, sym);
            if (_lblTaxArrears        != null) _lblTaxArrears.text        = UiFormat.Money(country.taxArrears * country.exchangeRate, sym);

            if (_lblComplianceDonut != null) _lblComplianceDonut.text = $"{compliance:F0}%\nCompliance";
            if (_lblComplianceCompliant    != null) _lblComplianceCompliant.text    = $"{compliance:F0}%";
            float nonComp  = Mathf.Max(0f, 100f - compliance);
            float partial  = nonComp * 0.5f;
            float outright = nonComp * 0.5f;
            if (_lblCompliancePartial      != null) _lblCompliancePartial.text      = $"{partial:F0}%";
            if (_lblComplianceNonCompliant != null) _lblComplianceNonCompliant.text = $"{outright:F0}%";
            _complianceDonut?.SetValues(compliance, partial, outright);

            var policiesCont = _taxPoliciesContainer;
            if (policiesCont != null) {
                while (policiesCont.childCount > 1) policiesCont.RemoveAt(1);

                int added = 0;
                if (country.activePolicies != null) {
                    foreach (var p in country.activePolicies) {
                        if (p.type == PolicyType.Economy) {
                            var row = new VisualElement(); row.AddToClassList("tax-policy-row");
                            var icon = new Label("▣"); icon.AddToClassList("tax-policy-icon"); row.Add(icon);
                            
                            var info = new VisualElement();
                            var title = new Label(p.policyName); title.AddToClassList("tax-row-title"); info.Add(title);
                            var copy = new Label("Active economic reform policy."); copy.AddToClassList("tax-row-copy"); info.Add(copy);
                            row.Add(info);

                            var stat = new Label($"ACTIVE\nTURN {p.turnEnacted}");
                            stat.AddToClassList("positive");
                            row.Add(stat);

                            policiesCont.Add(row);
                            added++;
                        }
                    }
                }
                if (added == 0) {
                    var empty = new Label("No active tax reforms.");
                    empty.AddToClassList("econ-muted");
                    empty.style.marginTop = 10;
                    policiesCont.Add(empty);
                }
            }

            RefreshTaxSimulator(country, sym, total);

        }

        private void RefreshTaxSimulator(CountryDataSO country, string sym, float total)
        {
            if (_root == null) return;

            float draftRate     = GetDraftRate(country, _simSelectedTaxKey);
            float currentRate   = GetSimTaxRate(country, _simSelectedTaxKey);
            float rateDelta     = draftRate - currentRate;
            float monthlyGDP    = country.gdp / 12f * country.exchangeRate;

            float revenueImpact = _simSelectedTaxKey switch {
                "INCOME"    => monthlyGDP * (rateDelta / 100f) * 0.35f,
                "CORPORATE" => monthlyGDP * (rateDelta / 100f) * 0.35f,
                "VAT"       => monthlyGDP * (rateDelta / 100f) * 0.35f,
                "IMPORT"    => monthlyGDP * 0.015f * (rateDelta / 100f),
                "EXPORT"    => monthlyGDP * 0.010f * (rateDelta / 100f),
                _           => 0f
            };

            float gdpImpact     = -rateDelta * 0.02f;
            float approvalImpact = -rateDelta * 0.5f;

            float confidenceImpact = _simSelectedTaxKey switch {
                "CORPORATE" => -rateDelta * 0.6f,
                "IMPORT"    => -rateDelta * 0.25f,
                "EXPORT"    => -rateDelta * 0.3f,
                "INCOME"    => -rateDelta * 0.2f,
                "VAT"       => -rateDelta * 0.15f,
                _           => -rateDelta * 0.3f
            };

            float investmentImpact = _simSelectedTaxKey switch {
                "CORPORATE" => -rateDelta * 0.8f,
                "IMPORT"    => -rateDelta * 0.3f,
                "EXPORT"    => -rateDelta * 0.2f,
                "INCOME"    => -rateDelta * 0.1f,
                "VAT"       => -rateDelta * 0.1f,
                _           => -rateDelta * 0.15f
            };

            float laborForce = country.population * 0.45f;
            float employmentSensitivity = _simSelectedTaxKey switch {
                "CORPORATE" => 0.00002f,
                "IMPORT"    => 0.00001f,
                "EXPORT"    => 0.000012f,
                "INCOME"    => 0.000008f,
                "VAT"       => 0.000006f,
                _           => 0.000008f
            };
            float employmentImpact = -rateDelta * laborForce * employmentSensitivity;

            float inflationImpact = _simSelectedTaxKey switch {
                "VAT"       => rateDelta * 0.08f,
                "IMPORT"    => rateDelta * 0.05f,
                "EXPORT"    => rateDelta * 0.02f,
                "CORPORATE" => rateDelta * 0.015f,
                "INCOME"    => rateDelta * 0.01f,
                _           => rateDelta * 0.02f
            };

            if (_lblSimSelectedTax != null)
                _lblSimSelectedTax.text = $"SELECTED TAX: {_simSelectedTaxKey} TAX";

            if (_lblSimCurrentRate != null)
                _lblSimCurrentRate.text = $"{currentRate:F1}%";

            if (_lblSimNewRate != null) {
                _lblSimNewRate.text = $"{draftRate:F1}%";
                _lblSimNewRate.RemoveFromClassList("positive"); _lblSimNewRate.RemoveFromClassList("negative");
                _lblSimNewRate.AddToClassList(rateDelta > 0f ? "negative" : "positive");
            }

            if (_lblSimRevenue != null) {
                _lblSimRevenue.text = $"{(revenueImpact >= 0 ? "+" : "")}{UiFormat.Money(revenueImpact, sym)}";
                _lblSimRevenue.RemoveFromClassList("positive"); _lblSimRevenue.RemoveFromClassList("negative");
                _lblSimRevenue.AddToClassList(revenueImpact >= 0 ? "positive" : "negative");
            }

            if (_lblSimGDPImpact != null) {
                _lblSimGDPImpact.text = $"{(gdpImpact >= 0 ? "+" : "")}{gdpImpact:F2}%";
                _lblSimGDPImpact.RemoveFromClassList("positive"); _lblSimGDPImpact.RemoveFromClassList("negative");
                _lblSimGDPImpact.AddToClassList(gdpImpact >= 0 ? "positive" : "negative");
            }

            if (_lblSimApproval != null) {
                _lblSimApproval.text = $"{(approvalImpact >= 0 ? "+" : "")}{approvalImpact:F1}%";
                _lblSimApproval.RemoveFromClassList("positive"); _lblSimApproval.RemoveFromClassList("negative");
                _lblSimApproval.AddToClassList(approvalImpact >= 0 ? "positive" : "negative");
            }

            if (_lblSimConfidence != null) {
                _lblSimConfidence.text = $"{(confidenceImpact >= 0 ? "+" : "")}{confidenceImpact:F1}%";
                _lblSimConfidence.RemoveFromClassList("positive"); _lblSimConfidence.RemoveFromClassList("negative");
                _lblSimConfidence.AddToClassList(confidenceImpact >= 0 ? "positive" : "negative");
            }

            if (_lblSimInvestment != null) {
                _lblSimInvestment.text = $"{(investmentImpact >= 0 ? "+" : "")}{investmentImpact:F1}%";
                _lblSimInvestment.RemoveFromClassList("positive"); _lblSimInvestment.RemoveFromClassList("negative");
                _lblSimInvestment.AddToClassList(investmentImpact >= 0 ? "positive" : "negative");
            }

            if (_lblSimEmployment != null) {
                _lblSimEmployment.text = $"{(employmentImpact >= 0 ? "+" : "")}{employmentImpact:N0}";
                _lblSimEmployment.RemoveFromClassList("positive"); _lblSimEmployment.RemoveFromClassList("negative");
                _lblSimEmployment.AddToClassList(employmentImpact >= 0 ? "positive" : "negative");
            }

            if (_lblSimInflation != null) {
                _lblSimInflation.text = $"{(inflationImpact >= 0 ? "+" : "")}{inflationImpact:F2}%";
                _lblSimInflation.RemoveFromClassList("positive"); _lblSimInflation.RemoveFromClassList("negative");
                _lblSimInflation.AddToClassList(inflationImpact > 0f ? "negative" : "positive");
            }

            if (_lblTaxSimAdvisory != null) {
                if (Mathf.Abs(rateDelta) < 0.01f) {
                    _lblTaxSimAdvisory.text = "Adjust the slider to simulate economic impact.";
                } else if (rateDelta > 0f) {
                    _lblTaxSimAdvisory.text = _simSelectedTaxKey switch {
                        "INCOME"    => "Higher income tax reduces disposable income, lowering public approval but boosting revenue.",
                        "CORPORATE" => "Higher corporate tax may discourage investment and slow economic growth.",
                        "VAT"       => "Higher VAT increases prices for consumers, hurting public approval.",
                        "IMPORT"    => "High import duties protect local industries but may increase consumer prices.",
                        "EXPORT"    => "High export duties generate revenue but reduce international competitiveness.",
                        _           => "Higher taxes may slow economic growth and reduce public approval."
                    };
                } else {
                    _lblTaxSimAdvisory.text = _simSelectedTaxKey switch {
                        "INCOME"    => "Lower income tax boosts public approval but reduces government revenue.",
                        "CORPORATE" => "Lower corporate tax encourages business investment but reduces immediate revenue.",
                        "VAT"       => "Lowering VAT stimulates consumer spending but costs the state significant tax revenue.",
                        "IMPORT"    => "Lower import duties increase trade volume and reduce consumer prices.",
                        "EXPORT"    => "Lower export duties increase trade volume and foreign relations.",
                        _           => "Lower taxes stimulate growth but reduce immediate government revenue."
                    };
                }
            }

            if (_lblTaxSimTotal != null) _lblTaxSimTotal.text = UiFormat.Money(total + revenueImpact, sym);
        }

        private void SetupTaxSimulatorInteractiveButtons(VisualElement root)
        {
            if (_simButtonsWired) return;
            _simButtonsWired = true;

            Debug.Log("[TaxUI] SetupTaxSimulatorInteractiveButtons — starting wiring");

            WireTaxRow(root, "INCOME",    "TaxIncome_Minus",    "TaxIncome_Plus",    "TaxIncome_Bar",    "TaxIncome_Rate");
            WireTaxRow(root, "CORPORATE", "TaxCorporate_Minus", "TaxCorporate_Plus", "TaxCorporate_Bar", "TaxCorporate_Rate");
            WireTaxRow(root, "VAT",       "TaxVAT_Minus",       "TaxVAT_Plus",       "TaxVAT_Bar",       "TaxVAT_Rate");
            WireTaxRow(root, "IMPORT",    "TaxImport_Minus",    "TaxImport_Plus",    "TaxImport_Bar",    "TaxImport_Rate");
            WireTaxRow(root, "EXPORT",    "TaxExport_Minus",    "TaxExport_Plus",    "TaxExport_Bar",    "TaxExport_Rate");

            _lblTaxTotalRevenue        = root.Q<Label>("TaxTotal_Revenue");
            _lblTaxTotalRatio          = root.Q<Label>("TaxTotal_Ratio");
            _lblTaxTotalMoM            = root.Q<Label>("TaxTotal_MoMDelta");
            _lblTaxComplianceRate      = root.Q<Label>("TaxCompliance_Rate");
            _lblTaxCollectionEff       = root.Q<Label>("TaxCollectionEff");
            _lblTaxGap                 = root.Q<Label>("TaxGap");
            _lblTaxArrears             = root.Q<Label>("TaxArrears");
            _lblComplianceDonut        = root.Q<Label>("ComplianceDonut_Label");
            _lblComplianceCompliant    = root.Q<Label>("ComplianceCompliant");
            _lblCompliancePartial      = root.Q<Label>("CompliancePartial");
            _lblComplianceNonCompliant = root.Q<Label>("ComplianceNonCompliant");
            var donutContainer = root.Q<VisualElement>("ComplianceDonutRoot");
            Debug.Log($"[TaxUI] ComplianceDonutRoot query — {(donutContainer != null ? "FOUND, applying border override" : "NULL — element not found, check the UXML name")}");
            if (donutContainer != null)
            {
                donutContainer.style.borderLeftWidth = 0;
                donutContainer.style.borderRightWidth = 0;
                donutContainer.style.borderTopWidth = 0;
                donutContainer.style.borderBottomWidth = 0;
                donutContainer.schedule.Execute(() => {
                    Debug.Log($"[TaxUI] ComplianceDonutRoot resolved border widths after override — L:{donutContainer.resolvedStyle.borderLeftWidth} R:{donutContainer.resolvedStyle.borderRightWidth} T:{donutContainer.resolvedStyle.borderTopWidth} B:{donutContainer.resolvedStyle.borderBottomWidth}");
                });
            }
            if (donutContainer != null && _complianceDonut == null)
            {
                _complianceDonut = new ComplianceDonutElement();
                donutContainer.Insert(0, _complianceDonut);
            }
            _lblSimSelectedTax         = root.Q<Label>("SimSelectedTax");
            _lblSimCurrentRate         = root.Q<Label>("SimCurrentRate");
            _lblSimNewRate             = root.Q<Label>("SimNewRate");
            _lblSimRevenue             = root.Q<Label>("SimRevenue");
            _lblSimGDPImpact           = root.Q<Label>("SimGDPImpact");
            _lblSimApproval            = root.Q<Label>("SimApproval");
            _lblSimConfidence          = root.Q<Label>("SimConfidence");
            _lblSimInvestment          = root.Q<Label>("SimInvestment");
            _lblSimEmployment          = root.Q<Label>("SimEmployment");
            _lblSimInflation           = root.Q<Label>("SimInflation");
            _lblTaxSimTotal            = root.Q<Label>("TaxSim_Total");
            _lblTaxSimAdvisory         = root.Q<Label>("TaxSim_Advisory");
            _taxPoliciesContainer      = root.Q<VisualElement>("TaxPoliciesContainer");

            Debug.Log("[TaxUI] Summary/Compliance/Simulator labels cached — " +
                $"Total:{(_lblTaxTotalRevenue!=null?"OK":"NULL")} Ratio:{(_lblTaxTotalRatio!=null?"OK":"NULL")} MoM:{(_lblTaxTotalMoM!=null?"OK":"NULL")} " +
                $"Compliance:{(_lblTaxComplianceRate!=null?"OK":"NULL")} Eff:{(_lblTaxCollectionEff!=null?"OK":"NULL")} Gap:{(_lblTaxGap!=null?"OK":"NULL")} Arrears:{(_lblTaxArrears!=null?"OK":"NULL")} " +
                $"Donut:{(_lblComplianceDonut!=null?"OK":"NULL")} Compliant:{(_lblComplianceCompliant!=null?"OK":"NULL")} Partial:{(_lblCompliancePartial!=null?"OK":"NULL")} NonCompliant:{(_lblComplianceNonCompliant!=null?"OK":"NULL")} " +
                $"SimTotal:{(_lblTaxSimTotal!=null?"OK":"NULL")} PoliciesContainer:{(_taxPoliciesContainer!=null?"OK":"NULL")}");

            var applyBtn = root.Query<Button>(className: "tax-apply-btn").First();
            var resetBtn = root.Query<Button>(className: "tax-reset-btn").First();
            Debug.Log($"[TaxUI] ApplyBtn={(applyBtn != null ? "FOUND" : "NULL")}  ResetBtn={(resetBtn != null ? "FOUND" : "NULL")}");
            applyBtn?.RegisterCallback<ClickEvent>(_ => ApplySimulatorTaxChange());
            resetBtn?.RegisterCallback<ClickEvent>(_ => ResetSimulatorTaxChange());

            Debug.Log("[TaxUI] SetupTaxSimulatorInteractiveButtons — wiring complete");
        }

        private void WireTaxRow(VisualElement root, string taxKey, string minusName, string plusName, string sliderName, string rateLabelName)
        {
            var minus  = root.Q<Button>(minusName);
            var plus   = root.Q<Button>(plusName);
            var slider = root.Q<Slider>(sliderName);
            var rate   = root.Q<Label>(rateLabelName);

            string prefix  = rateLabelName.EndsWith("_Rate") ? rateLabelName.Substring(0, rateLabelName.Length - "_Rate".Length) : rateLabelName;
            var revenue = root.Q<Label>(prefix + "_Revenue");
            var baseLbl = root.Q<Label>(prefix + "_Base");
            var shareLbl= root.Q<Label>(prefix + "_Share");

            _taxRowRefs[taxKey] = (rate, slider, revenue, baseLbl, shareLbl);

            Debug.Log($"[TaxUI] WireTaxRow [{taxKey}]  minus={minusName}:{(minus != null ? "OK" : "NULL")}  plus={plusName}:{(plus != null ? "OK" : "NULL")}  slider={sliderName}:{(slider != null ? "OK" : "NULL")}  rate={rateLabelName}:{(rate != null ? "OK" : "NULL")}  revenue={prefix}_Revenue:{(revenue != null ? "OK" : "NULL")}  base={prefix}_Base:{(baseLbl != null ? "OK" : "NULL")}  share={prefix}_Share:{(shareLbl != null ? "OK" : "NULL")}");

            if (minus != null)
            {
                minus.clicked += () =>
                {
                    var c = GameManager.Instance?.playerCountry;
                    if (c == null) { Debug.LogWarning($"[TaxUI] {minusName} clicked but playerCountry is null"); return; }
                    _simSelectedTaxKey = taxKey;
                    float before = GetDraftRate(c, taxKey);
                    _taxDraftRates[taxKey] = Mathf.Clamp(before - 0.5f, 0f, 60f);
                    Debug.Log($"[TaxUI] {minusName} clicked — {taxKey} draft {before:F1}% → {_taxDraftRates[taxKey]:F1}%");
                    string sym = string.IsNullOrEmpty(c.currencySymbol) ? "₦" : c.currencySymbol;
                    RefreshTaxationTab(c, sym);
                };
            }
            else Debug.LogError($"[TaxUI] MISSING BUTTON: {minusName} — check UXML name matches exactly");

            if (plus != null)
            {
                plus.clicked += () =>
                {
                    var c = GameManager.Instance?.playerCountry;
                    if (c == null) { Debug.LogWarning($"[TaxUI] {plusName} clicked but playerCountry is null"); return; }
                    _simSelectedTaxKey = taxKey;
                    float before = GetDraftRate(c, taxKey);
                    _taxDraftRates[taxKey] = Mathf.Clamp(before + 0.5f, 0f, 60f);
                    Debug.Log($"[TaxUI] {plusName} clicked — {taxKey} draft {before:F1}% → {_taxDraftRates[taxKey]:F1}%");
                    string sym = string.IsNullOrEmpty(c.currencySymbol) ? "₦" : c.currencySymbol;
                    RefreshTaxationTab(c, sym);
                };
            }
            else Debug.LogError($"[TaxUI] MISSING BUTTON: {plusName} — check UXML name matches exactly");

            if (slider != null)
            {
                slider.RegisterValueChangedCallback(evt =>
                {
                    if (Mathf.Approximately(evt.previousValue, evt.newValue)) return;
                    var c = GameManager.Instance?.playerCountry;
                    if (c == null) { Debug.LogWarning($"[TaxUI] {sliderName} changed but playerCountry is null"); return; }
                    Debug.Log($"[TaxUI] {sliderName} dragged — {taxKey} {evt.previousValue:F1}% → {evt.newValue:F1}%");
                    _simSelectedTaxKey = taxKey;
                    _taxDraftRates[taxKey] = evt.newValue;
                    string sym = string.IsNullOrEmpty(c.currencySymbol) ? "₦" : c.currencySymbol;
                    RefreshTaxationTab(c, sym);
                });
            }
            else Debug.LogError($"[TaxUI] MISSING SLIDER: {sliderName} — check UXML name matches exactly");
        }

        private float GetDraftRate(CountryDataSO c, string key)
        {
            if (!_taxDraftRates.TryGetValue(key, out var draft))
            {
                draft = GetSimTaxRate(c, key);
                _taxDraftRates[key] = draft;
            }
            return draft;
        }

        private void SetSimTaxRate(CountryDataSO c, string key, float val) {
            switch (key) {
                case "INCOME":    c.incomeTaxRate    = val; c.taxRate = (c.incomeTaxRate + c.corporateTaxRate) / 2f; break;
                case "CORPORATE": c.corporateTaxRate = val; c.taxRate = (c.incomeTaxRate + c.corporateTaxRate) / 2f; break;
                case "VAT":       c.vatRate          = val; break;
                case "IMPORT":    c.importTariffRate = val; break;
                case "EXPORT":    c.exportDutyRate   = val; break;
            }
        }

        private void ApplySimulatorTaxChange() {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            float oldRate = GetSimTaxRate(country, _simSelectedTaxKey);
            float newRate = GetDraftRate(country, _simSelectedTaxKey);
            if (Mathf.Approximately(oldRate, newRate)) return;

            ShowEconomyModal(
                "Apply Tax Change",
                $"Change {_simSelectedTaxKey} tax rate from {oldRate:F1}% to {newRate:F1}%?",
                "Changes will affect projected revenue and public approval. Takes effect next month.",
                "APPLY",
                () => {
                    SetSimTaxRate(country, _simSelectedTaxKey, newRate);
                    country.prevMonthlyTaxRevenue = country.income;
                    NotifyEconomy($"{_simSelectedTaxKey} tax rate changed to {newRate:F1}%. Takes effect next month.", new UnityEngine.Color(1f, 0.8f, 0.2f));
                    RefreshDisplay();
                }
            );
        }

        private void ResetSimulatorTaxChange() {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;
            _taxDraftRates[_simSelectedTaxKey] = GetSimTaxRate(country, _simSelectedTaxKey);
            RefreshTaxationTab(country, string.IsNullOrEmpty(country.currencySymbol) ? "?" : country.currencySymbol);
        }

    }
}