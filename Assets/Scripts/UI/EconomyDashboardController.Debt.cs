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
        private void UpdateInterestRate(float val)
        {
            if (GameManager.Instance?.playerCountry != null)
            {
                GameManager.Instance.playerCountry.centralBankInterestRate = val;
                if (_labelInterestRate != null) _labelInterestRate.text = $"{val:F1}%";
            }
        }

        private void UpdateDebtService(float val)
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;
            country.debtServiceRatio = val;
            if (_labelDebtService != null) _labelDebtService.text = $"{val:F0}%";
            // Update monthly cost preview
            RefreshDebtServiceCostLabel(country);
        }

        private void RefreshDebtServiceCostLabel(CountryDataSO country)
        {
            if (_lblDebtServiceCost == null || GameManager.Instance?.DebtSystem == null) return;
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            float interestUSD = GameManager.Instance.DebtSystem.CalculateInterest(country);
            float principalPaymentUSD = (country.debt * 0.01f) * (country.debtServiceRatio / 100f); // 1% of principal × ratio
            float totalMonthlyUSD = interestUSD + principalPaymentUSD;
            float totalMonthlyLocal = (totalMonthlyUSD * country.exchangeRate);
            _lblDebtServiceCost.text = $"Monthly payment: {UiFormat.Money(totalMonthlyLocal, sym)} (interest + principal)";
        }

        private void HandleBorrowDebt()
        {
            var gm = GameManager.Instance;
            var country = gm?.playerCountry;
            if (country == null) return;

            float borrowAmt = AdviserEngine.GetDynamicBorrowAmount(country);
            // Convert to USD equivalent for the debt principal pools
            float usdEquiv = borrowAmt / country.exchangeRate;
            
            // Add to principal
            country.domesticDebt += usdEquiv * 0.5f;
            country.foreignDebt += usdEquiv * 0.5f;
            country.debt = country.domesticDebt + country.foreignDebt;
            
            // Add to treasury
            gm.playerBudget += borrowAmt;
            country.treasury = gm.playerBudget;

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            NotificationManager.Instance?.AddNotification($"Issued Sovereign Bonds: +{sym}{borrowAmt:F0}B raised. Debt increased.", Color.yellow);
            NewsSystem.Instance?.PublishNews($"FINANCIAL: {country.countryName} issues {sym}{borrowAmt:F0}B in sovereign debt to fund the treasury.");
            RefreshDisplay();
        }

        private void HandleRepayDebt()
        {
            var gm = GameManager.Instance;
            var country = gm?.playerCountry;
            if (country == null) return;

            float repayAmt = AdviserEngine.GetDynamicRepayAmount(country);
            if (gm.playerBudget < repayAmt)
            {
                NotificationManager.Instance?.AddNotification("Insufficient treasury funds to repay debt.", Color.red);
                return;
            }

            float totalDebtUSD = country.debt;
            if (totalDebtUSD <= 0f)
            {
                NotificationManager.Instance?.AddNotification("No outstanding sovereign debt to repay.", Color.yellow);
                return;
            }

            float repayUSD = repayAmt / country.exchangeRate;
            
            // Pay off domestic debt first, then foreign debt
            if (country.domesticDebt >= repayUSD)
            {
                country.domesticDebt -= repayUSD;
            }
            else
            {
                float remainder = repayUSD - country.domesticDebt;
                country.domesticDebt = 0f;
                country.foreignDebt = Mathf.Max(0f, country.foreignDebt - remainder);
            }
            country.debt = country.domesticDebt + country.foreignDebt;

            // Deduct from budget
            gm.playerBudget -= repayAmt;
            country.treasury = gm.playerBudget;

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            NotificationManager.Instance?.AddNotification($"Repaid Debt: -{sym}{repayAmt:F0}B treasury cash used to pay down principal.", Color.green);
            NewsSystem.Instance?.PublishNews($"FINANCIAL: {country.countryName} repays {sym}{repayAmt:F0}B of outstanding debt obligations.");
            RefreshDisplay();
        }

        private void RefreshDebtFinancing(CountryDataSO country, string sym)
        {
            if (_root == null) return;
            
            float debtDom = country.domesticDebt * country.exchangeRate;
            float debtFor = country.foreignDebt * country.exchangeRate;
            float totalDebt = debtDom + debtFor;

            if (_root.Q<Label>("ValDomesticDebt") is Label lDom) lDom.text = UiFormat.Money(debtDom, sym);
            if (_root.Q<Label>("ValForeignDebt") is Label lFor) lFor.text = UiFormat.Money(debtFor, sym);
            
            // The progress bar sits between Domestic and Foreign, so it represents the composition ratio.
            if (_root.Q<ProgressBar>("DebtRatioBar") is ProgressBar pBar) {
                pBar.value = totalDebt > 0 ? (debtDom / totalDebt) * 100f : 0f;
            }

            if (_root.Q<Label>("LabelDebtService") is Label lDebtService) lDebtService.text = $"{country.debtServiceRatio:F0}%";
            if (_root.Q<Slider>("SliderDebtService") is Slider sDebtService) sDebtService.value = country.debtServiceRatio;
            
            // Calculate a synthetic monthly payment display based on the ratio
            float monthlyCost = totalDebt * 0.05f / 12f * (country.debtServiceRatio / 100f);
            if (_root.Q<Label>("LblDebtServiceCost") is Label lCost) {
                lCost.text = $"Monthly payment: {UiFormat.Money(monthlyCost, sym)} (interest + principal)";
            }
        }

    }
}