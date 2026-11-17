using UnityEngine;
using UnityEngine.UIElements;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using StateCraft.Interfaces;
using System.Collections.Generic;

namespace StateCraft.UI
{
    public class EconomyDashboardController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _root;

        private Button _tabDashboard, _tabProduction, _tabIndustry, _tabInfrastructure, _tabConstruction;
        private VisualElement _panelDashboard, _panelProduction, _panelIndustry, _panelInfrastructure, _panelConstruction;
        private List<VisualElement> _allPanels = new List<VisualElement>();

private Label _headerCountryName, _headerCreditRating, _headerIncome, _headerExpense;
        private VisualElement _headerFlag;
        private Label _overviewAdviserName, _overviewAdviserTitle, _overviewAdviserBody;
        private Label _overviewAdviserBenefit1, _overviewAdviserBenefit2, _overviewAdviserBenefit3;
        private Label _overviewAdviserRisk1, _overviewAdviserRisk2, _overviewAdviserRisk3;
        private Button _overviewApproveButton, _overviewDeclineButton;
        private VisualElement _overviewAdviserImpactGrid, _overviewAdviserActionRow;

        private Label _valTreasury, _valGDP, _valGDPGrowth, _valInflation, _valExchange, _valBudgetBalance;
        private ProgressBar _debtRatioBar;
        
        private Slider _sliderTaxRate, _sliderSubsidyLevel;
        private Label _labelTaxRate, _labelSubsidyLevel;
        
        private Slider _sliderInterestRate, _sliderTariffRate, _sliderDebtService;
        private Label _labelInterestRate, _labelTariffRate, _labelDebtService;
        private Label _lblDebtServiceCost;
        private Button _btnBorrowDebt, _btnRepayDebt;

        private Label _valTotalDebt, _valDomesticDebt, _valForeignDebt;

private Label _valPopulation, _valPoverty, _valUnemployment, _valCorruption, _valApproval;
        private VisualElement _statusContainer;

        private VisualElement _incomeList, _expenseList;
        private Label _valNetBalance;

        private Label _economyBriefing;
        private VisualElement _economyLiveFeed;

        private VisualElement _productionSummary, _productionCardsContainer, _resourceDepositsList;
        private VisualElement _industrySummary, _industryCardsContainer;
        private VisualElement _infraSummary, _infraCardsContainer;
        private Label _infraSynergyText;
        private VisualElement _constructionSummary, _constructionCardsContainer, _blueprintCatalog;
        private Label _blueprintSectionTitle;
        private bool _blueprintsVisible;

        private VisualElement _econReportsPanel;
        private VisualElement _econSectorsGrid;
        private VisualElement _overviewProjectsPanel;

        private VisualElement _actionModal, _actionModalImpact;
        private Label _actionModalTitle, _actionModalBody;
        private Button _actionModalConfirm, _actionModalCancel, _actionModalClose;
        private System.Action _pendingModalAction;

        private static readonly string[] _filterCycle = { "ALL", "INFRASTRUCTURE", "ENERGY", "AGRICULTURE", "TECH", "DEFENCE" };
        private int _filterIndex = 0;

        private float _draftIncomeTax = -1f;
        private float _draftCorporateTax = -1f;
        private float _draftVAT = -1f;
        private float _draftExportDuty = -1f;

        private float _draftMilitary = -1f;
        private float _draftIntelligence = -1f;
        private float _draftEducation = -1f;
        private float _draftHealth = -1f;
        private float _draftInfrastructure = -1f;
        private float _draftAgriculture = -1f;
        private float _draftEnergy = -1f;
        private float _draftWelfare = -1f;

        private AdviserEngine.AdviserRecommendation? _currentAdviserRec;
        private string _currentAdviserTitle = "";
        private int _currentAdviserTurn = -1;

private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null) _root = _uiDocument.rootVisualElement;
            
            var dashboard = _root?.Q<VisualElement>("EconomyDashboard");
            if (dashboard != null) 
            {
                InitializeElements(dashboard);
                dashboard.style.display = DisplayStyle.None;
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
            {
                GameManager.Instance.TurnSystem.OnTurnEnded += RefreshDisplay;
            }
            RefreshDisplay();
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.TurnSystem != null)
            {
                GameManager.Instance.TurnSystem.OnTurnEnded -= RefreshDisplay;
            }
        }

        private void InitializeElements(VisualElement root)
        {
            _tabDashboard = root.Q<Button>("TabDashboard");
            _tabProduction = root.Q<Button>("TabProduction");
            _tabIndustry = root.Q<Button>("TabIndustry");
            _tabInfrastructure = root.Q<Button>("TabInfrastructure");
            _tabConstruction = root.Q<Button>("TabConstruction");

            _panelDashboard = root.Q<VisualElement>("PanelDashboard");
            _panelProduction = root.Q<VisualElement>("PanelProduction");
            _panelIndustry = root.Q<VisualElement>("PanelIndustry");
            _panelInfrastructure = root.Q<VisualElement>("PanelInfrastructure");
            _panelConstruction = root.Q<VisualElement>("PanelConstruction");

            _allPanels.Clear();
            _allPanels.AddRange(new[] { _panelDashboard, _panelProduction, _panelIndustry, _panelInfrastructure, _panelConstruction });

            _tabDashboard.clicked += () => SwitchTab(0);
            _tabProduction.clicked += () => SwitchTab(1);
            _tabIndustry.clicked += () => SwitchTab(2);
            _tabInfrastructure.clicked += () => SwitchTab(3);
            _tabConstruction.clicked += () => SwitchTab(4);

_headerCountryName = root.Q<Label>("CountryHeaderName");
            _headerFlag = root.Q<VisualElement>("CountryHeaderFlag");
            _headerCreditRating = root.Q<Label>("CreditRatingBadge");
            _headerIncome = root.Q<Label>("HeaderIncomeLabel");
            _headerExpense = root.Q<Label>("HeaderExpenseLabel");
            _overviewAdviserName = root.Q<Label>("OverviewAdviserName");
            _overviewAdviserTitle = root.Q<Label>("OverviewAdviserTitle");
            _overviewAdviserBody = root.Q<Label>("OverviewAdviserBody");
            _overviewAdviserBenefit1 = root.Q<Label>("OverviewAdviserBenefit1");
            _overviewAdviserBenefit2 = root.Q<Label>("OverviewAdviserBenefit2");
            _overviewAdviserBenefit3 = root.Q<Label>("OverviewAdviserBenefit3");
            _overviewAdviserRisk1 = root.Q<Label>("OverviewAdviserRisk1");
            _overviewAdviserRisk2 = root.Q<Label>("OverviewAdviserRisk2");
            _overviewAdviserRisk3 = root.Q<Label>("OverviewAdviserRisk3");
            _overviewAdviserImpactGrid = root.Q<VisualElement>("OverviewAdviserImpactGrid");
            _overviewAdviserActionRow = root.Q<VisualElement>("OverviewAdviserActionRow");
            _overviewApproveButton = root.Q<Button>("OverviewApprovePolicy");
            _overviewDeclineButton = root.Q<Button>("OverviewDeclinePolicy");

            _valTreasury = root.Q<Label>("ValTreasury");
            _valGDP = root.Q<Label>("ValGDP");
            _valGDPGrowth = root.Q<Label>("ValGDPGrowth");
            _valInflation = root.Q<Label>("ValInflation");
            _valExchange = root.Q<Label>("ValExchange");
            _valBudgetBalance = root.Q<Label>("ValBudgetBalance");
            _debtRatioBar = root.Q<ProgressBar>("DebtRatioBar");

            _sliderTaxRate = root.Q<Slider>("SliderTaxRate");
            _labelTaxRate = root.Q<Label>("LabelTaxRate");
            _sliderSubsidyLevel = root.Q<Slider>("SliderSubsidyLevel");
            _labelSubsidyLevel = root.Q<Label>("LabelSubsidyLevel");

            if (_sliderTaxRate != null) _sliderTaxRate.RegisterValueChangedCallback(evt => UpdateTaxRate(evt.newValue));
            if (_sliderSubsidyLevel != null) _sliderSubsidyLevel.RegisterValueChangedCallback(evt => UpdateSubsidyLevel(evt.newValue));

            _sliderInterestRate = root.Q<Slider>("SliderInterestRate");
            _labelInterestRate = root.Q<Label>("LabelInterestRate");
            _sliderTariffRate = root.Q<Slider>("SliderTariffRate");
            _labelTariffRate = root.Q<Label>("LabelTariffRate");
            _sliderDebtService = root.Q<Slider>("SliderDebtService");
            _labelDebtService = root.Q<Label>("LabelDebtService");
            _lblDebtServiceCost = root.Q<Label>("LblDebtServiceCost");
            _btnBorrowDebt = root.Q<Button>("BtnBorrowDebt");
            _btnRepayDebt = root.Q<Button>("BtnRepayDebt");

            _valTotalDebt = root.Q<Label>("ValTotalDebt");
            _valDomesticDebt = root.Q<Label>("ValDomesticDebt");
            _valForeignDebt = root.Q<Label>("ValForeignDebt");

            if (_sliderInterestRate != null) _sliderInterestRate.RegisterValueChangedCallback(evt => UpdateInterestRate(evt.newValue));
            if (_sliderTariffRate != null) _sliderTariffRate.RegisterValueChangedCallback(evt => UpdateTariffRate(evt.newValue));
            if (_sliderDebtService != null) _sliderDebtService.RegisterValueChangedCallback(evt => UpdateDebtService(evt.newValue));
            if (_btnBorrowDebt != null) _btnBorrowDebt.clicked += HandleBorrowDebt;
            if (_btnRepayDebt != null) _btnRepayDebt.clicked += HandleRepayDebt;

            _valApproval = root.Q<Label>("ValApproval");
            _statusContainer = root.Q<VisualElement>("StatusIndicators");
            _economyBriefing = root.Q<Label>("EconomyBriefing");
            _economyLiveFeed = root.Q<ScrollView>("EconomyLiveFeed");

            _incomeList = root.Q<VisualElement>("IncomeList");
            _expenseList = root.Q<VisualElement>("ExpenseList");
            _valNetBalance = root.Q<Label>("ValNetBalance");

            _productionSummary = root.Q<VisualElement>("ProductionSummary");
            _productionCardsContainer = root.Q<VisualElement>("ProductionCardsContainer");
            _resourceDepositsList = root.Q<VisualElement>("ResourceDepositsList");
            _industrySummary = root.Q<VisualElement>("IndustrySummary");
            _industryCardsContainer = root.Q<VisualElement>("IndustryCardsContainer");
            _infraSummary = root.Q<VisualElement>("InfraSummary");
            _infraSynergyText = root.Q<Label>("InfraSynergyText");
            _infraCardsContainer = root.Q<VisualElement>("InfraCardsContainer");
            _constructionSummary = root.Q<VisualElement>("ConstructionSummary");
            _constructionCardsContainer = root.Q<VisualElement>("ConstructionCardsContainer");
            _blueprintCatalog = root.Q<VisualElement>("BlueprintCatalog");
            _blueprintSectionTitle = root.Q<Label>("BlueprintSectionTitle");

            _actionModal = root.Q<VisualElement>("EconomyActionModal");
            _actionModalTitle = root.Q<Label>("EconomyModalTitle");
            _actionModalBody = root.Q<Label>("EconomyModalBody");
            _actionModalImpact = root.Q<VisualElement>("EconomyModalImpact");
            _actionModalConfirm = root.Q<Button>("EconomyModalConfirm");
            _actionModalCancel = root.Q<Button>("EconomyModalCancel");
            _actionModalClose = root.Q<Button>("EconomyModalClose");
            if (_actionModalConfirm != null) _actionModalConfirm.clicked += ConfirmEconomyModal;
            if (_actionModalCancel != null) _actionModalCancel.clicked += HideEconomyModal;
            if (_actionModalClose != null) _actionModalClose.clicked += HideEconomyModal;

            _econReportsPanel    = root.Q<VisualElement>("EconReportsPanel");
            _econSectorsGrid     = root.Q<VisualElement>("EconSectorsGrid");
            _overviewProjectsPanel = root.Q<VisualElement>("OverviewProjectsPanel");

            var btnViewSectors = root.Q<Label>("BtnViewAllSectors");
            if (btnViewSectors != null)
            {
                btnViewSectors.RegisterCallback<ClickEvent>(_ => SwitchTab(2));
                btnViewSectors.RegisterCallback<MouseEnterEvent>(_ =>
                    btnViewSectors.style.color = new StyleColor(new UnityEngine.Color(1f, 1f, 1f)));
                btnViewSectors.RegisterCallback<MouseLeaveEvent>(_ =>
                    btnViewSectors.style.color = new StyleColor(new UnityEngine.Color(25/255f, 226/255f, 255/255f)));
            }

            var btnViewProjects = root.Q<Label>("BtnViewAllProjects");
            if (btnViewProjects != null)
            {
                btnViewProjects.RegisterCallback<ClickEvent>(_ => SwitchTab(4));
                btnViewProjects.RegisterCallback<MouseEnterEvent>(_ =>
                    btnViewProjects.style.color = new StyleColor(new UnityEngine.Color(1f, 1f, 1f)));
                btnViewProjects.RegisterCallback<MouseLeaveEvent>(_ =>
                    btnViewProjects.style.color = new StyleColor(new UnityEngine.Color(25/255f, 226/255f, 255/255f)));
            }

            root.Q<Button>("BtnOpenBuildMenu")?.RegisterCallback<ClickEvent>(_ => ToggleBlueprints());
            root.Q<Button>("CloseEconomy").clicked += Hide;
            WireEconomyActionButtons(root);
        }

        private void WireEconomyActionButtons(VisualElement root)
        {
            root.Query<Button>().ForEach(btn =>
            {
                if (btn == null || string.IsNullOrEmpty(btn.text)) return;

                string name = btn.name;
                if (name == "CloseEconomy" ||
                    name == "TabDashboard" ||
                    name == "TabProduction" ||
                    name == "TabIndustry" ||
                    name == "TabInfrastructure" ||
                    name == "TabConstruction" ||
                    name == "BtnBorrowDebt" ||
                    name == "BtnRepayDebt" ||
                    name == "BtnOpenBuildMenu" ||
                    name == "EconomyModalConfirm" ||
                    name == "EconomyModalCancel" ||
                    name == "EconomyModalClose")
                {
                    return;
                }

                if (btn.text == "+" || btn.text == "−" || btn.text == "-")
                {
                    btn.clicked += () => AdjustTaxOrBudgetField(btn, btn.text == "+" ? 1f : -1f);
                }
                else
                {
                    btn.clicked += () => HandleEconomyAction(btn);
                }
            });
        }

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

            if (ctx.Contains("INCOME TAX"))
            {
                if (_draftIncomeTax < 0f) _draftIncomeTax = country.incomeTaxRate;
                _draftIncomeTax = Mathf.Clamp(_draftIncomeTax + delta, 0f, 50f);
                if (_root?.Q<Label>("TaxIncome_Rate") is Label l) l.text = $"{_draftIncomeTax:F1}%";
                if (_root?.Q<ProgressBar>("TaxIncome_Bar") is ProgressBar pb) pb.value = _draftIncomeTax;
                return;
            }
            if (ctx.Contains("VALUE ADDED TAX") || ctx.Contains("VAT"))
            {
                if (_draftVAT < 0f) _draftVAT = country.vatRate;
                _draftVAT = Mathf.Clamp(_draftVAT + delta, 0f, 30f);
                if (_root?.Q<Label>("TaxVAT_Rate") is Label l) l.text = $"{_draftVAT:F1}%";
                if (_root?.Q<ProgressBar>("TaxVAT_Bar") is ProgressBar pb) pb.value = _draftVAT;
                return;
            }
            if (ctx.Contains("EXPORT DUTY"))
            {
                if (_draftExportDuty < 0f) _draftExportDuty = country.exportDutyRate;
                _draftExportDuty = Mathf.Clamp(_draftExportDuty + delta, 0f, 30f);
                if (_root?.Q<Label>("TaxExport_Rate") is Label l) l.text = $"{_draftExportDuty:F1}%";
                if (_root?.Q<ProgressBar>("TaxExport_Bar") is ProgressBar pb) pb.value = _draftExportDuty;
                return;
            }
            if (ctx.Contains("IMPORT DUTY") || ctx.Contains("IMPORT TARIFF"))
            {
                if (_draftCorporateTax < 0f) _draftCorporateTax = country.importTariffRate;
                country.importTariffRate = Mathf.Clamp(country.importTariffRate + delta, 0f, 50f);
                if (_sliderTariffRate != null) _sliderTariffRate.SetValueWithoutNotify(country.importTariffRate);
                if (_labelTariffRate  != null) _labelTariffRate.text  = $"{country.importTariffRate:F0}%";
                NotifyEconomy($"Import Duty: {country.importTariffRate:F0}%", Color.cyan);
                return;
            }
            if (ctx.Contains("CORPORATE TAX"))
            {
                if (_draftCorporateTax < 0f) _draftCorporateTax = country.corporateTaxRate;
                _draftCorporateTax = Mathf.Clamp(_draftCorporateTax + delta, 0f, 50f);
                if (_root?.Q<Label>("TaxCorporate_Rate") is Label l) l.text = $"{_draftCorporateTax:F1}%";
                if (_root?.Q<ProgressBar>("TaxCorporate_Bar") is ProgressBar pb) pb.value = _draftCorporateTax;
                return;
            }

            if (ctx.Contains("MILITARY"))         { AdjustBudgetShare(ref _draftMilitary,       country.budgetMilitaryShare,       delta, country, "MILITARY");       return; }
            if (ctx.Contains("INTELLIGENCE"))      { AdjustBudgetShare(ref _draftIntelligence,   country.budgetIntelligenceShare,   delta, country, "INTELLIGENCE");    return; }
            if (ctx.Contains("EDUCATION"))         { AdjustBudgetShare(ref _draftEducation,      country.budgetEducationShare,      delta, country, "EDUCATION");       return; }
            if (ctx.Contains("HEALTH"))            { AdjustBudgetShare(ref _draftHealth,         country.budgetHealthShare,         delta, country, "HEALTH");          return; }
            if (ctx.Contains("INFRASTRUCTURE"))    { AdjustBudgetShare(ref _draftInfrastructure, country.budgetInfrastructureShare, delta, country, "INFRASTRUCTURE"); return; }
            if (ctx.Contains("AGRICULTURE"))       { AdjustBudgetShare(ref _draftAgriculture,    country.budgetAgricultureShare,    delta, country, "AGRICULTURE");     return; }
            if (ctx.Contains("ENERGY"))            { AdjustBudgetShare(ref _draftEnergy,         country.budgetEnergyShare,         delta, country, "ENERGY");          return; }
            if (ctx.Contains("SOCIAL WELFARE") || ctx.Contains("WELFARE"))
                                                  { AdjustBudgetShare(ref _draftWelfare,         country.budgetWelfareShare,        delta, country, "WELFARE");         return; }

            RefreshDisplay(); NotifyEconomy("Control adjusted.", Color.grey);
        }

        private void AdjustBudgetShare(ref float draft, float baseline, float delta, CountryDataSO country, string deptName)
        {
            if (draft < 0f) draft = baseline;
            draft = Mathf.Clamp(draft + delta, 0f, 60f);
            string properName = deptName[0] + deptName.Substring(1).ToLower(); if (deptName.Contains("WELFARE")) properName = "Welfare"; 
            if (_root?.Q<Label>($"Budget{properName}_Pct") is Label l) l.text = $"{draft:F0}%"; 
            if (_root?.Q<ProgressBar>($"Budget{properName}_Bar") is ProgressBar pb) pb.value = draft;
            float totalB = country.spending * country.exchangeRate;
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            if (_root?.Q<Label>($"Budget{properName}_Value") is Label v) v.text = UiFormat.Money(totalB * (draft / 100f), sym);
        }

        private void HandleEconomyAction(Button button)
        {
            HandleEconomyAction(button, button?.text ?? "");
        }

        private void HandleEconomyAction(Button button, string actionText)
        {
            if (string.IsNullOrEmpty(actionText)) return;

            string action = actionText.ToUpperInvariant();
            var country = GameManager.Instance?.playerCountry;

            if (action == "×" || action == "X")
            {
                VisualElement detailCard = FindAncestorWithClass(button, "projects-side-card");
                if (detailCard != null)
                {
                    detailCard.style.display = DisplayStyle.None;
                    NotifyEconomy("Selected project panel closed.", Color.cyan);
                }
                return;
            }

            if (action.Contains("FILTER"))
            {
                _filterIndex = (_filterIndex + 1) % _filterCycle.Length;
                string current = _filterCycle[_filterIndex];
                NotifyEconomy($"Showing projects: {current}", Color.cyan);
                if (country != null) PopulateConstruction(country,
                    string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol);
                return;
            }

            if (action.Contains("APPLY CHANGE"))
            {
                if (country == null) return;
                bool anyDraft = _draftIncomeTax    >= 0f || _draftCorporateTax >= 0f ||
                                _draftVAT          >= 0f || _draftExportDuty   >= 0f;
                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;

                float oldTax = country.taxRate;
                float newIncome  = _draftIncomeTax    >= 0f ? _draftIncomeTax    : country.incomeTaxRate;
                float newCorp    = _draftCorporateTax >= 0f ? _draftCorporateTax : country.corporateTaxRate;
                float newVAT     = _draftVAT          >= 0f ? _draftVAT          : country.vatRate;
                float newExport  = _draftExportDuty   >= 0f ? _draftExportDuty   : country.exportDutyRate;
                float newWeighted = (newIncome * 0.35f + newCorp * 0.30f + newVAT * 0.25f + newExport * 0.10f);
                float incomeΔ    = (newWeighted - oldTax) * country.gdp / 1200f * country.exchangeRate;
                float infΔ       = (newVAT - country.vatRate) * 0.001f;

                string impactText = anyDraft
                    ? $"Revenue Δ: {UiFormat.MoneySigned(incomeΔ, sym)}/mo | Inflation Δ: {(infΔ >= 0 ? "+" : "")}{infΔ * 100f:F2}% | Approval Δ: {(newWeighted > oldTax ? "-1%" : "+0.5%")}"
                    : "No draft changes pending — current slider values will be locked in.";

                ShowEconomyModal(
                    "Apply Tax Changes",
                    anyDraft
                        ? $"Commit new tax rates?\nIncome: {newIncome:F1}%  Corporate: {newCorp:F1}%  VAT: {newVAT:F1}%  Export Duty: {newExport:F1}%"
                        : "Confirm current slider values as the active tax policy.",
                    impactText,
                    "APPLY",
                    () => CommitTaxDrafts(country, sym));
                return;
            }

            if (action.Contains("RESET ALLOCATION") || action == "↻  RESET" || action.Contains(" RESET"))
            {
                ShowEconomyModal(
                    "Reset Controls",
                    "Reset economy controls to the current saved national policy values?",
                    "All draft changes will be discarded and sliders restored.",
                    "RESET",
                    () =>
                    {
                        ClearTaxDrafts();
                        ClearBudgetDrafts();
                        ResetPolicySliders();
                        NotifyEconomy("All controls reset to saved values.", Color.cyan);
                    });
                return;
            }

            if (action.Contains("APPROVE BUDGET"))
            {
                if (country == null) return;
                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
                float mil  = _draftMilitary      >= 0f ? _draftMilitary      : country.budgetMilitaryShare;
                float edu  = _draftEducation     >= 0f ? _draftEducation     : country.budgetEducationShare;
                float hlt  = _draftHealth        >= 0f ? _draftHealth        : country.budgetHealthShare;
                float inf2 = _draftInfrastructure>= 0f ? _draftInfrastructure: country.budgetInfrastructureShare;
                string impact = $"Military {mil:F0}% · Education {edu:F0}% · Health {hlt:F0}% · Infrastructure {inf2:F0}%";
                ShowEconomyModal("Approve Budget",
                    "Commit the current budget allocation to national accounts?",
                    impact,
                    "APPROVE",
                    () => ApproveBudgetPlan());
                return;
            }

            if (action.Contains("SAVE AS DRAFT"))
            {
                if (country == null) return;
                ShowEconomyModal(
                    "Save Draft Budget",
                    "Save current allocation as a budget draft?",
                    "Draft stored in national accounts. Use APPROVE BUDGET to make it active.",
                    "SAVE",
                    () => SaveBudgetDraft(country));
                return;
            }

            if (action.Contains("AUTO-ALLOCATE"))
            {
                ShowEconomyModal(
                    "Auto-Allocate Budget",
                    "Generate a balanced allocation weighted by current national priorities?",
                    "Infrastructure, education, energy, and health will be prioritised. Military holds at current level.",
                    "GENERATE",
                    () => AutoAllocateBudget());
                return;
            }

            if (action.Contains("INVEST NOW"))
            {
                ShowEconomyModal(
                    "Invest In Manufacturing",
                    "Upgrade the manufacturing sector as recommended by the adviser?",
                    "Spending treasury funds to raise manufacturing tier, add jobs, and boost GDP growth.",
                    "INVEST",
                    () => UpgradeIndustryById("Mfg"));
                return;
            }

            if (action.Contains("UPGRADE TO TIER"))
            {
                string industryId = InferIndustryId(button);
                ShowEconomyModal(
                    "Upgrade Industry",
                    actionText.Replace("\n", " "),
                    "Treasury funds will be deducted. Sector tier, workforce, and output projections will update.",
                    "QUEUE",
                    () => UpgradeIndustryById(industryId));
                return;
            }

            if (action.Contains("COMMISSION"))
            {
                string blueprintName = InferBlueprintName(button);
                ShowEconomyModal(
                    "Commission Project",
                    $"Commission {blueprintName} as a new national project?",
                    "10% mobilisation deposit deducted from treasury. Project enters the active construction pipeline.",
                    "COMMISSION",
                    () => CommissionBlueprint(blueprintName));
                return;
            }

            if (action.Contains("TAX REFORMS HISTORY") || action.Contains("REFORMS HISTORY"))
            {
                string log = "No reforms logged yet.";
                if (country?.taxReformLog != null && country.taxReformLog.Count > 0)
                    log = string.Join("\n", country.taxReformLog.GetRange(0, Mathf.Min(10, country.taxReformLog.Count)));
                ShowEconomyModal("Tax Reforms History", log, "Last 10 recorded tax policy changes.", "OK", null);
                return;
            }

            if (action.Contains("VIEW ALL POLICIES"))
            {
                string policies = "No active policies on record.";
                if (country?.activePolicies != null && country.activePolicies.Count > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var p in country.activePolicies)
                        sb.AppendLine($"[{p.type}] {p.name}  — cost {p.recurringCost:F2}B/mo");
                    policies = sb.ToString();
                }
                ShowEconomyModal("Active Policies", policies, "Live policy list from national accounts.", "OK", null);
                return;
            }

            if (action.Contains("VIEW ALL ALERTS"))
            {
                ShowEconomyModal("Economic Alerts", BuildAlertSummary(country),
                    "Live condition scan — alerts cleared when conditions improve.", "OK", null);
                return;
            }

            if (action.Contains("VIEW FULL REPORT") || action.Contains("VIEW DETAILED REPORT"))
            {
                ShowEconomyModal("Economy Report", BuildFullReport(country),
                    "Generated from current national accounts and projections.", "OK", null);
                return;
            }

            if (action.Contains("VIEW ADVISER NOTES"))
            {
                ShowEconomyModal("Adviser Notes", BuildAdviserNotes(country),
                    "Chief Economic Adviser recommendations from current state analysis.", "OK", null);
                return;
            }

            if (action.Contains("VIEW DETAILS"))
            {
                string details = BuildProjectCardDetail(button, country);
                ShowEconomyModal("Project Details", details,
                    "Live data from the national construction pipeline.", "OK", null);
                return;
            }

            if (action.Contains("VIEW"))
            {
                ShowEconomyModal("Report", actionText.Replace("\n", " "),
                    "Detailed data screens are generated from national accounts.", "OK", null);
                return;
            }

            if (action.Contains("APPROVE POLICY"))
            {
                if (country == null) return;
                if (!_currentAdviserRec.HasValue)
                {
                    ShowEconomyModal("Approve Policy", "No active adviser recommendation to approve.",
                        "All conditions are within normal parameters.", "OK", null);
                    return;
                }
                var rec = _currentAdviserRec.Value;
                ShowEconomyModal(
                    $"Approve: {rec.Title}",
                    $"{rec.Rationale}\n\n{rec.Benefits}",
                    rec.Risks + (rec.Cost > 0f ? $"\nCost: {rec.Cost:F0}B" : ""),
                    "APPROVE",
                    () =>
                    {
                        rec.Apply?.Invoke(country);
                        AdviserEngine.Dismiss(country, rec.Title);
                        _currentAdviserRec = null;
                        _currentAdviserTitle = "";
                        _currentAdviserTurn = -1;
                        NotifyEconomy($"Policy approved: {rec.Title}", Color.green);
                        RefreshDisplay();
                    });
                return;
            }

            if (action.Contains("DECLINE"))
            {
                if (country == null) return;
                string title = _currentAdviserRec.HasValue ? _currentAdviserRec.Value.Title : (_currentAdviserTitle ?? "Recommendation");
                ShowEconomyModal(
                    "Decline Recommendation",
                    $"Decline '{title}'?",
                    "This recommendation will be suppressed for 10 turns. No policy data is changed.",
                    "DECLINE",
                    () =>
                    {
                        AdviserEngine.Dismiss(country, title);
                        _currentAdviserRec = null;
                        _currentAdviserTitle = "";
                        _currentAdviserTurn = -1;
                        NotifyEconomy($"Recommendation declined: {title}", Color.yellow);
                        RefreshDisplay();
                    });
                return;
            }
        }

        private void ShowEconomyModal(string title, string body, string impact, string confirmText, System.Action onConfirm)
        {
            if (_actionModal == null)
            {
                onConfirm?.Invoke();
                return;
            }

            if (_actionModalTitle != null) _actionModalTitle.text = title;
            if (_actionModalBody != null) _actionModalBody.text = body;
            if (_actionModalConfirm != null) _actionModalConfirm.text = confirmText;
            if (_actionModalImpact != null)
            {
                _actionModalImpact.Clear();
                _actionModalImpact.Add(new Label(impact));
            }

            _pendingModalAction = onConfirm;
            _actionModal.style.display = DisplayStyle.Flex;
            _actionModal.BringToFront();
        }

        private static VisualElement FindAncestorWithClass(VisualElement element, string className)
        {
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current.ClassListContains(className)) return current;
            }

            return null;
        }

        private void HideEconomyModal()
        {
            _pendingModalAction = null;
            if (_actionModal != null) _actionModal.style.display = DisplayStyle.None;
        }

        private void ConfirmEconomyModal()
        {
            var action = _pendingModalAction;
            HideEconomyModal();
            action?.Invoke();
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

        private void SaveBudgetDraft(CountryDataSO country)
        {
            country.budgetMilitaryShare      = _draftMilitary      >= 0f ? _draftMilitary      : country.budgetMilitaryShare;
            country.budgetIntelligenceShare  = _draftIntelligence  >= 0f ? _draftIntelligence  : country.budgetIntelligenceShare;
            country.budgetEducationShare     = _draftEducation     >= 0f ? _draftEducation     : country.budgetEducationShare;
            country.budgetHealthShare        = _draftHealth        >= 0f ? _draftHealth        : country.budgetHealthShare;
            country.budgetInfrastructureShare= _draftInfrastructure>= 0f ? _draftInfrastructure: country.budgetInfrastructureShare;
            country.budgetAgricultureShare   = _draftAgriculture   >= 0f ? _draftAgriculture   : country.budgetAgricultureShare;
            country.budgetEnergyShare        = _draftEnergy        >= 0f ? _draftEnergy        : country.budgetEnergyShare;
            country.budgetWelfareShare       = _draftWelfare       >= 0f ? _draftWelfare       : country.budgetWelfareShare;
            country.hasBudgetDraft = true;
            int turn = GameManager.Instance?.TurnSystem?.CurrentTurn ?? 0;
            country.lastBudgetDraftSummary = $"[Turn {turn}] Budget draft saved.";
            NotifyEconomy("Budget draft saved — use APPROVE BUDGET to commit.", Color.cyan);
        }

        private void ClearBudgetDrafts()
        {
            _draftMilitary = _draftIntelligence = _draftEducation = _draftHealth =
            _draftInfrastructure = _draftAgriculture = _draftEnergy = _draftWelfare = -1f;
        }

        private void ApproveBudgetPlan()
        {
            var gm = GameManager.Instance;
            var country = gm?.playerCountry;
            if (country == null) return;

            country.budgetMilitaryShare       = _draftMilitary      >= 0f ? _draftMilitary      : country.budgetMilitaryShare;
            country.budgetIntelligenceShare   = _draftIntelligence  >= 0f ? _draftIntelligence  : country.budgetIntelligenceShare;
            country.budgetEducationShare      = _draftEducation     >= 0f ? _draftEducation     : country.budgetEducationShare;
            country.budgetHealthShare         = _draftHealth        >= 0f ? _draftHealth        : country.budgetHealthShare;
            country.budgetInfrastructureShare = _draftInfrastructure>= 0f ? _draftInfrastructure: country.budgetInfrastructureShare;
            country.budgetAgricultureShare    = _draftAgriculture   >= 0f ? _draftAgriculture   : country.budgetAgricultureShare;
            country.budgetEnergyShare         = _draftEnergy        >= 0f ? _draftEnergy        : country.budgetEnergyShare;
            country.budgetWelfareShare        = _draftWelfare       >= 0f ? _draftWelfare       : country.budgetWelfareShare;

            country.militaryBudgetUSD  = Mathf.Max(0.1f, country.income * (country.budgetMilitaryShare    / 100f));
            country.intelligenceBudget = Mathf.Max(0.1f, country.income * (country.budgetIntelligenceShare/ 100f));
            country.spending           = Mathf.Max(0f,   country.income * 0.96f);
            country.publicApproval     = Mathf.Min(100f, country.publicApproval + 1f);
            country.stability          = Mathf.Min(100f, country.stability      + 0.3f);
            country.hasBudgetDraft     = false;

            int turn = GameManager.Instance?.TurnSystem?.CurrentTurn ?? 0;
            country.lastBudgetDraftSummary = $"[Turn {turn}] Budget approved: military {country.budgetMilitaryShare:F0}%, edu {country.budgetEducationShare:F0}%.";
            ClearBudgetDrafts();

            NotifyEconomy("Budget approved and committed to national accounts.", Color.green);
            RefreshDisplay();
        }

        private void AutoAllocateBudget()
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            country.budgetMilitaryShare      = 20f;
            country.budgetIntelligenceShare  =  7f;
            country.budgetEducationShare     = 16f;
            country.budgetHealthShare        = 14f;
            country.budgetInfrastructureShare= 20f;
            country.budgetAgricultureShare   = 10f;
            country.budgetEnergyShare        =  9f;
            country.budgetWelfareShare       =  4f;

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

        private void ApproveFuelSubsidyPolicy()
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            country.subsidyLevel   = Mathf.Max(0f, country.subsidyLevel   - 1f);
            country.publicApproval = Mathf.Max(0f, country.publicApproval - 1f);
            country.inflationRate += 0.001f;
            if (_sliderSubsidyLevel != null) _sliderSubsidyLevel.SetValueWithoutNotify(country.subsidyLevel);
            if (_labelSubsidyLevel  != null) _labelSubsidyLevel.text = $"{country.subsidyLevel:F0}%";

            NotifyEconomy("Fuel subsidy reduction approved.", Color.green);
            RefreshDisplay();
        }

        private void UpgradeIndustryById(string industryId)
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null || string.IsNullOrEmpty(industryId)) return;

            foreach (var ind in EconomyTabContent.Industries)
            {
                if (ind.Id != industryId) continue;

                if (EconomyTabContent.TryUpgradeIndustry(country, ind, out string msg))
                {
                    NotifyEconomy(msg, Color.green);
                    RefreshDisplay();
                }
                else
                {
                    ShowEconomyModal("Upgrade Blocked", ind.Title, msg, "OK", null);
                }
                return;
            }

            ShowEconomyModal("Upgrade Unavailable", "No matching industry was found for this button.", "The card needs an industry identifier.", "OK", null);
        }

        private void CommissionBlueprint(string blueprintName)
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null || string.IsNullOrEmpty(blueprintName)) return;

            foreach (var bp in EconomyTabContent.Blueprints)
            {
                if (!BlueprintMatches(bp.Name, blueprintName)) continue;

                if (EconomyTabContent.TryStartProject(country, bp, out string msg))
                {
                    NotifyEconomy(msg, Color.green);
                    RefreshDisplay();
                }
                else
                {
                    ShowEconomyModal("Commission Blocked", bp.Name, msg, "OK", null);
                }
                return;
            }

            ShowEconomyModal("Blueprint Unavailable", blueprintName, "No matching blueprint exists in EconomyTabContent yet.", "OK", null);
        }

        private static bool BlueprintMatches(string definedName, string cardName)
        {
            string a = NormalizeKey(definedName);
            string b = NormalizeKey(cardName);
            if (a == b || a.Contains(b) || b.Contains(a)) return true;
            if (b.Contains("GASFIRED") && a.Contains("GASFIRED")) return true;
            if (b.Contains("COASTALHIGHWAY") && a.Contains("COASTALHIGHWAY")) return true;
            if (b.Contains("SATELLITE") && a.Contains("SATELLITE")) return true;
            if (b.Contains("IRRIGATION") && a.Contains("RICEBELT")) return true;
            return false;
        }

        private static string InferIndustryId(Button button)
        {
            string context = GetAncestorText(button, 5).ToUpperInvariant();
            if (context.Contains("OIL") || context.Contains("GAS")) return "Oil";
            if (context.Contains("MINING") || context.Contains("MINERALS")) return "Mining";
            if (context.Contains("AGRICULTURE")) return "Agri";
            if (context.Contains("MANUFACTURING")) return "Mfg";
            if (context.Contains("TECH") || context.Contains("FINTECH")) return "Tech";
            return "";
        }

        private static string InferBlueprintName(Button button)
        {
            string context = GetAncestorText(button, 4);
            string[] candidates =
            {
                "COASTAL HIGHWAY PHASE II",
                "GAS-FIRED POWER COMPLEX",
                "NORTHERN DEFENCE BASE",
                "COMMUNICATIONS SATELLITE",
                "NATIONAL IRRIGATION PROGRAM"
            };

            string upper = context.ToUpperInvariant();
            foreach (var candidate in candidates)
                if (upper.Contains(candidate)) return candidate;

            return "Selected Blueprint";
        }

        private static string GetAncestorText(VisualElement element, int depth)
        {
            var parts = new System.Text.StringBuilder();
            VisualElement current = element;
            for (int i = 0; i < depth && current != null; i++, current = current.parent)
            {
                current.Query<Label>().ForEach(label =>
                {
                    if (!string.IsNullOrEmpty(label.text))
                        parts.Append(label.text).Append(' ');
                });
            }
            return parts.ToString();
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char ch in value.ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            }
            return sb.ToString();
        }

        private void ResetPolicySliders()
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            if (_sliderTaxRate      != null) _sliderTaxRate.value      = country.taxRate;
            if (_sliderSubsidyLevel != null) _sliderSubsidyLevel.value = country.subsidyLevel;
            if (_sliderInterestRate != null) _sliderInterestRate.value = country.centralBankInterestRate;
            if (_sliderTariffRate   != null) _sliderTariffRate.value   = country.importTariffRate;
            if (_sliderDebtService  != null) _sliderDebtService.value  = country.debtServiceRatio;
        }

private static string BuildFullReport(CountryDataSO c)
        {
            if (c == null) return "No country data available.";
            string sym = string.IsNullOrEmpty(c.currencySymbol) ? "₦" : c.currencySymbol;
            float debtGDP = c.gdp > 0 ? (c.debt / c.gdp) * 100f : 0f;
            var gm = GameManager.Instance;
            float treasury = gm != null ? gm.playerBudget : c.treasury;
            float income   = c.income  * c.exchangeRate;
            float spending = c.spending * c.exchangeRate;
            float net      = income - spending;
            string balType = net >= 0 ? "SURPLUS" : "DEFICIT";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"GDP:          {UiFormat.Money(c.gdp * c.exchangeRate, sym)} (growth {c.gdpGrowthRate:P1})");
            sb.AppendLine($"Treasury:     {UiFormat.Money(treasury, sym)}");
            sb.AppendLine($"Budget:       {balType} {UiFormat.MoneySigned(net, sym)}/mo");
            sb.AppendLine($"Inflation:    {c.inflationRate:P1}  |  Interest Rate: {c.centralBankInterestRate:F1}%");
            sb.AppendLine($"Debt / GDP:   {debtGDP:F1}%  |  Credit: {c.creditRating}");
            sb.AppendLine($"Approval:     {c.publicApproval:F0}%  |  Stability: {c.stability:F0}%");
            sb.AppendLine($"Unemployment: {c.unemploymentRate:F0}%  |  Poverty:  {c.povertyRate:F0}%");
            sb.AppendLine($"Electricity:  {c.electricityLevel:F0}%  |  Food sec: {c.foodSecurity:F0}%");

            if (c.electricityLevel < 40f) sb.AppendLine("\n⚠ RISK: Power grid below 40% — industrial output constrained.");
            if (c.inflationRate > 0.08f)  sb.AppendLine($"\n⚠ RISK: Inflation {c.inflationRate:P1} — monetary tightening advised.");
            if (debtGDP > 60f)            sb.AppendLine($"\n⚠ RISK: Debt/GDP at {debtGDP:F0}% — above sustainability threshold.");
            if (treasury < 5f)            sb.AppendLine("\n⚠ RISK: Treasury critically low — consider emergency revenue action.");

            return sb.ToString();
        }

        private static string BuildAdviserNotes(CountryDataSO c)
        {
            if (c == null) return "No country data available.";
            var gm = GameManager.Instance;
            var controller = gm != null ? Object.FindObjectOfType<EconomyDashboardController>() : null;
            AdviserEngine.AdviserRecommendation? currentRec = controller != null ? controller.GetCurrentAdviserRecommendation(c) : null;
            var recs = AdviserEngine.GenerateRecommendations(c, 5);
            if (currentRec.HasValue)
            {
                recs.RemoveAll(r => r.Title == currentRec.Value.Title);
                recs.Insert(0, currentRec.Value);
            }

            if (recs.Count == 0)
                return "Chief Economic Adviser: All indicators within normal parameters. No urgent actions required at this time.";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Chief Economic Adviser Recommendations:\n");
            int i = 1;
            foreach (var r in recs)
            {
                sb.AppendLine($"{i}. [{r.Category}] {r.Title}");
                sb.AppendLine($"   {r.Rationale}");
                sb.AppendLine($"   {r.Benefits}");
                if (r.Cost > 0f) sb.AppendLine($"   Cost: {r.Cost:F0}B");
                sb.AppendLine();
                i++;
            }
            return sb.ToString();
        }

        public enum AlertSeverity { Info, Warning, Critical }
        public struct EconAlert { public string Category; public string Message; public AlertSeverity Severity; }

        private static string BuildAlertSummary(CountryDataSO c)
        {
            if (c == null) return "No country data available.";
            var alerts = new System.Collections.Generic.List<string>();
            float debtGDP = c.gdp > 0 ? (c.debt / c.gdp) * 100f : 0f;
            float treasury = GameManager.Instance?.playerBudget ?? c.treasury;

            if (debtGDP        > 60f)  alerts.Add($"⚠ DEBT WARNING — Debt/GDP at {debtGDP:F0}% (threshold 60%)");
            if (c.inflationRate > 0.08f) alerts.Add($"⚠ INFLATION ALERT — {c.inflationRate:P1} (target <4%)");
            if (c.electricityLevel < 40f) alerts.Add($"⚠ POWER CRISIS — Grid at {c.electricityLevel:F0}% coverage");
            if (c.stability    < 40f)  alerts.Add($"⚠ INSTABILITY WARNING — Stability {c.stability:F0}%");
            if (treasury       <  5f)  alerts.Add($"⚠ TREASURY CRITICAL — {UiFormat.Money(treasury)} remaining");
            if (c.foodSecurity < 40f)  alerts.Add($"⚠ FOOD SECURITY ALERT — {c.foodSecurity:F0}% (crisis <40%)");
            if (c.unemploymentRate > 25f) alerts.Add($"⚠ HIGH UNEMPLOYMENT — {c.unemploymentRate:F0}% (threshold 25%)");
            if (c.corruptionIndex > 60f)  alerts.Add($"ⓘ CORRUPTION HIGH — {c.corruptionIndex:F0}% treasury leakage");

            return alerts.Count > 0
                ? string.Join("\n", alerts)
                : "✓ No critical alerts. All economic indicators within acceptable ranges.";
        }

        private static System.Collections.Generic.List<EconAlert> BuildUIAlerts(CountryDataSO c)
        {
            var list = new System.Collections.Generic.List<EconAlert>();
            if (c == null) return list;
            float debtGDP = c.gdp > 0 ? (c.debt / c.gdp) * 100f : 0f;
            float treasury = GameManager.Instance?.playerBudget ?? c.treasury;

            if (debtGDP > 60f) list.Add(new EconAlert{ Category = "Debt", Message = $"Debt/GDP at {debtGDP:F0}%", Severity = AlertSeverity.Critical });
            if (c.inflationRate > 0.08f) list.Add(new EconAlert{ Category = "Inflation", Message = $"Inflation at {c.inflationRate:P1}", Severity = AlertSeverity.Critical });
            if (c.electricityLevel < 40f) list.Add(new EconAlert{ Category = "Power", Message = $"Grid at {c.electricityLevel:F0}%", Severity = AlertSeverity.Critical });
            if (c.stability < 40f) list.Add(new EconAlert{ Category = "Stability", Message = $"Stability at {c.stability:F0}%", Severity = AlertSeverity.Warning });
            if (treasury < 5f) list.Add(new EconAlert{ Category = "Treasury", Message = $"Treasury Critical", Severity = AlertSeverity.Critical });
            if (c.foodSecurity < 40f) list.Add(new EconAlert{ Category = "Agriculture", Message = $"Food Security {c.foodSecurity:F0}%", Severity = AlertSeverity.Warning });
            if (c.unemploymentRate > 25f) list.Add(new EconAlert{ Category = "Jobs", Message = $"Unemployment {c.unemploymentRate:F0}%", Severity = AlertSeverity.Warning });
            if (c.corruptionIndex > 60f) list.Add(new EconAlert{ Category = "Governance", Message = $"Corruption High", Severity = AlertSeverity.Info });
            return list;
        }

        private static string BuildProjectCardDetail(Button button, CountryDataSO c)
        {
            if (c == null) return "No project data available.";
            string ctx = GetAncestorText(button, 6).ToUpperInvariant();

            if (c.activeProjects != null)
            {
                foreach (var p in c.activeProjects)
                {
                    if (ctx.Contains(p.projectName.ToUpperInvariant()))
                    {
                        string sym = string.IsNullOrEmpty(c.currencySymbol) ? "₦" : c.currencySymbol;
                        float pct = p.totalMonths > 0 ? (p.totalMonths - p.monthsRemaining) / (float)p.totalMonths * 100f : 0f;
                        return $"{p.projectName}\n" +
                               $"Type: {p.projectType}  |  Region: {p.regionName}\n" +
                               $"Progress: {pct:F0}%  |  {p.monthsRemaining} months remaining\n" +
                               $"Monthly cost: {UiFormat.Money(p.totalCost / Mathf.Max(1, p.totalMonths), sym)}\n" +
                               $"Total cost: {UiFormat.Money(p.totalCost, sym)}\n" +
                               $"Stage: {p.stage}";
                    }
                }
            }

            string cardTitle = ctx.Length > 120 ? ctx.Substring(0, 120) : ctx;
            return $"Selected project detail:\n{cardTitle}\n(Live project data will populate once commissioned via Blueprint Library.)";
        }

        private void NotifyEconomy(string message, Color color)
        {
            NotificationManager.Instance?.AddNotification(message, color);
            Debug.Log($"[Economy UI] {message}");
        }

        private void SwitchTab(int index)
        {
            for (int i = 0; i < _allPanels.Count; i++)
            {
                bool activeTab = (i == index);
                _allPanels[i].style.display = activeTab ? DisplayStyle.Flex : DisplayStyle.None;
                _allPanels[i].pickingMode = activeTab ? PickingMode.Position : PickingMode.Ignore;
                if (activeTab) UIStateManager.FixScrollViews(_allPanels[i]);
            }
            
            _tabDashboard.RemoveFromClassList("active-tab");
            _tabProduction.RemoveFromClassList("active-tab");
            _tabIndustry.RemoveFromClassList("active-tab");
            _tabInfrastructure.RemoveFromClassList("active-tab");
            _tabConstruction.RemoveFromClassList("active-tab");

            if (index == 0) _tabDashboard.AddToClassList("active-tab");
            else if (index == 1) _tabProduction.AddToClassList("active-tab");
            else if (index == 2) _tabIndustry.AddToClassList("active-tab");
            else if (index == 3) _tabInfrastructure.AddToClassList("active-tab");
            else if (index == 4) _tabConstruction.AddToClassList("active-tab");
        }

        private void UpdateTaxRate(float val)
        {
            if (GameManager.Instance?.playerCountry != null)
            {
                GameManager.Instance.playerCountry.taxRate = val;
                if (_labelTaxRate != null) _labelTaxRate.text = $"{val:F0}%";
            }
        }

        private void UpdateSubsidyLevel(float val)
        {
            if (GameManager.Instance?.playerCountry != null)
            {
                GameManager.Instance.playerCountry.subsidyLevel = val;
                if (_labelSubsidyLevel != null) _labelSubsidyLevel.text = $"{val:F0}%";
            }
        }

        private void UpdateInterestRate(float val)
        {
            if (GameManager.Instance?.playerCountry != null)
            {
                GameManager.Instance.playerCountry.centralBankInterestRate = val;
                if (_labelInterestRate != null) _labelInterestRate.text = $"{val:F1}%";
            }
        }

        private void UpdateTariffRate(float val)
        {
            if (GameManager.Instance?.playerCountry != null)
            {
                GameManager.Instance.playerCountry.importTariffRate = val;
                if (_labelTariffRate != null) _labelTariffRate.text = $"{val:F0}%";
            }
        }

        private void UpdateDebtService(float val)
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;
            country.debtServiceRatio = val;
            if (_labelDebtService != null) _labelDebtService.text = $"{val:F0}%";
            RefreshDebtServiceCostLabel(country);
        }

        private void RefreshDebtServiceCostLabel(CountryDataSO country)
        {
            if (_lblDebtServiceCost == null || GameManager.Instance?.DebtSystem == null) return;
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            float interestUSD = GameManager.Instance.DebtSystem.CalculateInterest(country);
            float principalPaymentUSD = (country.debt * 0.01f) * (country.debtServiceRatio / 100f);
            float totalMonthlyUSD = interestUSD + principalPaymentUSD;
            float totalMonthlyLocal = (totalMonthlyUSD * country.exchangeRate);
            _lblDebtServiceCost.text = $"Monthly payment: {UiFormat.Money(totalMonthlyLocal, sym)} (interest + principal)";
        }

        private void HandleBorrowDebt()
        {
            var gm = GameManager.Instance;
            var country = gm?.playerCountry;
            if (country == null) return;

            float borrowAmt = 50f;
            float usdEquiv = borrowAmt / country.exchangeRate;
            
            country.domesticDebt += usdEquiv * 0.5f;
            country.foreignDebt += usdEquiv * 0.5f;
            country.debt = country.domesticDebt + country.foreignDebt;
            
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

            float repayAmt = 50f;
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

            gm.playerBudget -= repayAmt;
            country.treasury = gm.playerBudget;

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            NotificationManager.Instance?.AddNotification($"Repaid Debt: -{sym}{repayAmt:F0}B treasury cash used to pay down principal.", Color.green);
            NewsSystem.Instance?.PublishNews($"FINANCIAL: {country.countryName} repays {sym}{repayAmt:F0}B of outstanding debt obligations.");
            RefreshDisplay();
        }

public void Show()
        {
            var panel = _root.Q<VisualElement>("EconomyDashboard");
            if (panel != null)
            {
                panel.style.display = DisplayStyle.Flex;
                UIStateManager.Instance?.ApplySafeTransition(panel, true, 0.4f);
                RefreshDisplay();
                UIStateManager.FixScrollViews(panel);
            }
        }

        public void Hide()
        {
            if (MainDashboardToolkitController.Instance != null)
                MainDashboardToolkitController.Instance.HideAllPanels();
        }

        public void RefreshDisplay()
        {
            if (GameManager.Instance == null || GameManager.Instance.playerCountry == null) return;
            var gm = GameManager.Instance;
            var country = gm.playerCountry;
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;

            country.treasury = gm.playerBudget;

            if (_headerCountryName != null) _headerCountryName.text = country.countryName.ToUpper();
            
            if (_headerFlag != null)
            {
                string flagName = country.countryName.Replace(" ", "_");
                Sprite flagSprite = Resources.Load<Sprite>($"Flags/{flagName}");
                _headerFlag.style.backgroundImage = flagSprite != null ? new StyleBackground(flagSprite) : new StyleBackground();
            }

            if (_headerCreditRating != null) 
            {
                float debtRatio = country.gdp > 0 ? country.debt / country.gdp : 0f;
                string outlook = debtRatio > 0.8f || country.inflationRate > 0.15f ? "NEGATIVE" : (country.stability > 65f ? "STABLE" : "WATCH");
                _headerCreditRating.text = $"{country.creditRating} ({outlook})";
                if (country.creditRating.StartsWith("A")) _headerCreditRating.style.color = new Color(0, 230/255f, 118/255f);
                else if (country.creditRating.StartsWith("B")) _headerCreditRating.style.color = new Color(255/255f, 235/255f, 59/255f);
                else _headerCreditRating.style.color = new Color(255/255f, 82/255f, 82/255f);
            }

            if (_root.Q<Label>("DateMain") is Label lDateMain)
                lDateMain.text = gm?.TurnSystem != null ? gm.TurnSystem.CurrentDate : "—";
            if (_root.Q<Label>("DateSub") is Label lDateSub)
                lDateSub.text = System.DateTime.Now.ToString("dddd, HH:mm");

            if (_root.Q<Label>("ValInflationDelta") is Label lInfD)
            {
                float dInf = country.inflationRate - country.prevInflationRate;
                lInfD.text = $"{(dInf >= 0 ? "+" : "")}{dInf:P1} MoM";
                lInfD.RemoveFromClassList("positive"); lInfD.RemoveFromClassList("negative");
                lInfD.AddToClassList(dInf > 0.005f ? "negative" : "positive");
            }
            if (_root.Q<Label>("ValDebtRatio") is Label lDR)
            {
                float dRat = country.gdp > 0 ? country.debt / country.gdp : 0f;
                lDR.text = $"{dRat:P1} of GDP";
                lDR.RemoveFromClassList("warning"); lDR.RemoveFromClassList("negative"); lDR.RemoveFromClassList("positive");
                lDR.AddToClassList(dRat > 0.6f ? "negative" : dRat > 0.4f ? "warning" : "positive");
            }
            if (_root.Q<Label>("ValApprovalDelta") is Label lAppD)
            {
                float dApp = country.publicApproval - country.prevPublicApproval;
                lAppD.text = $"{(dApp >= 0 ? "+" : "")}{dApp:F1}% MoM";
                lAppD.RemoveFromClassList("positive"); lAppD.RemoveFromClassList("negative");
                lAppD.AddToClassList(dApp >= 0 ? "positive" : "negative");
            }

            float treasuryB = gm.playerBudget;
            float gdpB = country.gdp * country.exchangeRate;
            float incomeB = country.income * country.exchangeRate;
            float spendingB = country.spending * country.exchangeRate;
            
            float domesticInterestLocal =  gm.DebtSystem != null ? (gm.DebtSystem.CalculateDomesticInterest(country) * country.exchangeRate) : 0f;
            float foreignInterestUSD = gm.DebtSystem != null ? gm.DebtSystem.CalculateForeignInterest(country) : 0f;
            float serviceMultiplier = country.debtServiceRatio / 100f;
            float actualDomesticPaidLocal = domesticInterestLocal * serviceMultiplier;
            float actualForeignPaidUSD = foreignInterestUSD * serviceMultiplier;
            float foreignInterestLocal = actualForeignPaidUSD * country.exchangeRate;
            
            float totalOpCostLocal = 0f;
            if (country.activeOperations != null && country.activeOperations.Count > 0)
            {
                float totalOpCostUSD = 0f;
                foreach (var op in country.activeOperations)
                {
                    totalOpCostUSD += (op.costPerMonth > 0f) ? (op.costPerMonth / 1000f) : 0.05f;
                }
                totalOpCostLocal = totalOpCostUSD * country.exchangeRate;
            }

            float totalTradeNetLocal = 0f;
            if (gm.DiplomacySystem is DiplomacySystem diplo)
            {
                foreach (var deal in diplo.ActiveDeals)
                {
                    float dealLocal = (deal.monthlyPaymentUSD * country.exchangeRate) / 1e9f;
                    if (deal.sellerCountry == country.countryName) totalTradeNetLocal += dealLocal;
                    else if (deal.buyerCountry == country.countryName) totalTradeNetLocal -= dealLocal;
                }
            }
            if (country.activeTradeAgreements != null)
            {
                foreach (var agr in country.activeTradeAgreements)
                {
                    if (agr.status == TradeAgreementStatus.Active)
                    {
                        float valLocal = (agr.monthlyPaymentUSD * country.exchangeRate) / 1e9f;
                        if (agr.isSeller) totalTradeNetLocal += valLocal;
                        else totalTradeNetLocal -= valLocal;
                    }
                }
            }

            float netBalanceB = incomeB - spendingB - actualDomesticPaidLocal - foreignInterestLocal - totalOpCostLocal + totalTradeNetLocal;

            if (_valTreasury != null) _valTreasury.text = UiFormat.Money(treasuryB, sym);
            if (_valGDP != null) _valGDP.text = UiFormat.Money(gdpB, sym);
            if (_valGDPGrowth != null)
            {
                _valGDPGrowth.text = UiFormat.Percent(country.gdpGrowthRate) + DashboardLiveContent.TrendArrow(country.gdpGrowthRate, 0.03f, 0f);
                _valGDPGrowth.style.color = country.gdpGrowthRate >= 0 ? new Color(0, 230/255f, 118/255f) : new Color(255/255f, 82/255f, 82/255f);
            }
            if (_valInflation != null)
            {
                _valInflation.text = UiFormat.Percent(country.inflationRate) + DashboardLiveContent.TrendArrow(country.inflationRate, 0f, 0.1f);
                _valInflation.style.color = country.inflationRate > 0.1f ? new Color(255/255f, 82/255f, 82/255f) : Color.white;
            }
            if (_valExchange != null) _valExchange.text = $"{country.exchangeRate:N1} / USD";

            if (_valBudgetBalance != null) 
            {
                string type = netBalanceB >= 0 ? "SURPLUS" : "DEFICIT";
                _valBudgetBalance.text = $"{type} {UiFormat.MoneySigned(netBalanceB, sym)}";
                _valBudgetBalance.style.color = netBalanceB >= 0 ? new Color(0, 230/255f, 118/255f) : new Color(255/255f, 82/255f, 82/255f);
            }
            if (_headerIncome != null) _headerIncome.text = UiFormat.MoneySigned(incomeB, sym);
            if (_headerExpense != null) _headerExpense.text = UiFormat.Money(-spendingB, sym);
            if (_valNetBalance != null) _valNetBalance.text = UiFormat.MoneySigned(netBalanceB, sym);

            if (_debtRatioBar != null)
            {
                float ratio = country.gdp > 0 ? (country.debt / country.gdp) * 100f : 0f;
                _debtRatioBar.value = ratio;
                _debtRatioBar.title = $"{ratio:F0}%";
            }

            if (_valApproval != null) _valApproval.text = $"{country.publicApproval:F0}%";

            if (_sliderTaxRate != null) _sliderTaxRate.SetValueWithoutNotify(country.taxRate);
            if (_labelTaxRate != null) _labelTaxRate.text = $"{country.taxRate:F0}%";
            if (_sliderSubsidyLevel != null) _sliderSubsidyLevel.SetValueWithoutNotify(country.subsidyLevel);
            if (_labelSubsidyLevel != null) _labelSubsidyLevel.text = $"{country.subsidyLevel:F0}%";

            if (_sliderInterestRate != null) _sliderInterestRate.SetValueWithoutNotify(country.centralBankInterestRate);
            if (_labelInterestRate != null) _labelInterestRate.text = $"{country.centralBankInterestRate:F1}%";
            if (_sliderTariffRate != null) _sliderTariffRate.SetValueWithoutNotify(country.importTariffRate);
            if (_labelTariffRate != null) _labelTariffRate.text = $"{country.importTariffRate:F0}%";
            if (_sliderDebtService != null) _sliderDebtService.SetValueWithoutNotify(country.debtServiceRatio);
            if (_labelDebtService != null) _labelDebtService.text = $"{country.debtServiceRatio:F0}%";

            string debtSym = sym;
            float totalDebtLocal = (country.debt * country.exchangeRate);
            float domDebtLocal  = (country.domesticDebt  * country.exchangeRate);
            float forDebtLocal  = (country.foreignDebt   * country.exchangeRate);
            if (_valTotalDebt    != null) _valTotalDebt.text    = UiFormat.Money(totalDebtLocal, debtSym);
            if (_valDomesticDebt != null) _valDomesticDebt.text = UiFormat.Money(domDebtLocal,  debtSym);
            if (_valForeignDebt  != null) _valForeignDebt.text  = UiFormat.Money(forDebtLocal,  debtSym);
            RefreshDebtServiceCostLabel(country);

            if (_btnBorrowDebt != null) _btnBorrowDebt.text = $"BORROW {sym}50B";
            if (_btnRepayDebt != null)
            {
                _btnRepayDebt.text = $"REPAY {sym}50B";
                _btnRepayDebt.SetEnabled(gm.playerBudget >= 50f && country.debt > 0f);
            }

            UpdateCurrentAdviserRecommendation(country);
            DashboardLiveContent.PopulateStatusPills(_statusContainer, country, gm);
            if (_economyBriefing != null) _economyBriefing.text = DashboardLiveContent.BuildEconomyBriefing(country, gm);
            DashboardLiveContent.PopulateFeed(_economyLiveFeed, DashboardLiveContent.BuildEconomyFeed(country, gm));

            RefreshOverviewTab(country, sym, gm);
            PopulateLedger(country, sym, gm);
            PopulateProduction(country);
            RefreshIndustryTab(country, sym);
            PopulateInfra(country);
            RefreshTaxationTab(country, sym);
            RefreshBudgetTab(country, sym);
            RefreshProjectsTab(country, sym);
            if (_blueprintsVisible) PopulateBlueprintCatalog(country);
            UpdateOverviewAdviserCard(country);
        }

        private void UpdateCurrentAdviserRecommendation(CountryDataSO country)
        {
            if (country == null) return;

            int currentTurn = GameManager.Instance?.TurnSystem?.CurrentTurn ?? 0;
            if (_currentAdviserRec.HasValue && _currentAdviserTurn == currentTurn) return;

            var recs = AdviserEngine.GenerateRecommendations(country, 1);
            if (recs.Count > 0)
            {
                _currentAdviserRec = recs[0];
                _currentAdviserTitle = recs[0].Title;
                _currentAdviserTurn = currentTurn;
                return;
            }

            _currentAdviserRec = null;
            _currentAdviserTitle = "";
            _currentAdviserTurn = currentTurn;
        }

        private AdviserEngine.AdviserRecommendation? GetCurrentAdviserRecommendation(CountryDataSO country)
        {
            UpdateCurrentAdviserRecommendation(country);
            return _currentAdviserRec;
        }

        private void UpdateOverviewAdviserCard(CountryDataSO country)
        {
            if (country == null) return;

            var rec = GetCurrentAdviserRecommendation(country);
            bool hasRecommendation = rec.HasValue;

            if (!hasRecommendation)
            {
                if (_overviewAdviserName != null) _overviewAdviserName.text = "Dr. Adekunle Okafor";
                if (_overviewAdviserTitle != null) _overviewAdviserTitle.text = "No active recommendation";
                if (_overviewAdviserBody != null) _overviewAdviserBody.text = "All indicators are stable. The adviser will return when conditions change.";
                if (_overviewAdviserBenefit1 != null) _overviewAdviserBenefit1.text = " ";
                if (_overviewAdviserBenefit2 != null) _overviewAdviserBenefit2.text = " ";
                if (_overviewAdviserBenefit3 != null) _overviewAdviserBenefit3.text = " ";
                if (_overviewAdviserRisk1 != null) _overviewAdviserRisk1.text = " ";
                if (_overviewAdviserRisk2 != null) _overviewAdviserRisk2.text = " ";
                if (_overviewAdviserRisk3 != null) _overviewAdviserRisk3.text = " ";
            }
            else
            {
                var current = rec.Value;
                if (_overviewAdviserName != null) _overviewAdviserName.text = "Dr. Adekunle Okafor";
                if (_overviewAdviserTitle != null) _overviewAdviserTitle.text = current.Title;
                if (_overviewAdviserBody != null) _overviewAdviserBody.text = current.Rationale;

                string[] benefits = SplitLines(current.Benefits, 3);
                string[] risks = SplitLines(current.Risks, 3);
                if (_overviewAdviserBenefit1 != null) _overviewAdviserBenefit1.text = benefits[0];
                if (_overviewAdviserBenefit2 != null) _overviewAdviserBenefit2.text = benefits[1];
                if (_overviewAdviserBenefit3 != null) _overviewAdviserBenefit3.text = benefits[2];
                if (_overviewAdviserRisk1 != null) _overviewAdviserRisk1.text = risks[0];
                if (_overviewAdviserRisk2 != null) _overviewAdviserRisk2.text = risks[1];
                if (_overviewAdviserRisk3 != null) _overviewAdviserRisk3.text = risks[2];
            }

            DisplayStyle recommendationDisplay = hasRecommendation ? DisplayStyle.Flex : DisplayStyle.None;
            if (_overviewAdviserImpactGrid != null) _overviewAdviserImpactGrid.style.display = recommendationDisplay;
            if (_overviewAdviserActionRow != null) _overviewAdviserActionRow.style.display = recommendationDisplay;
            if (_overviewApproveButton != null) _overviewApproveButton.SetEnabled(hasRecommendation);
            if (_overviewDeclineButton != null) _overviewDeclineButton.SetEnabled(hasRecommendation);
        }

        private static string[] SplitLines(string text, int count)
        {
            var result = new string[count];
            for (int i = 0; i < count; i++) result[i] = " ";
            if (string.IsNullOrEmpty(text)) return result;

            var lines = text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < count && i < lines.Length; i++)
                result[i] = lines[i].Trim();
            return result;
        }

        private void ToggleBlueprints()
        {
            _blueprintsVisible = !_blueprintsVisible;
            ApplyBlueprintVisibility();
        }

        private void ShowBlueprintCatalog()
        {
            _blueprintsVisible = true;
            ApplyBlueprintVisibility();
            SwitchTab(4);
        }

        private void ApplyBlueprintVisibility()
        {
            var show = _blueprintsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (_blueprintCatalog != null) _blueprintCatalog.style.display = show;
            if (_blueprintSectionTitle != null) _blueprintSectionTitle.style.display = show;
            if (_blueprintsVisible && GameManager.Instance?.playerCountry != null)
                PopulateBlueprintCatalog(GameManager.Instance.playerCountry);
        }

        private void PopulateProduction(CountryDataSO country)
        {
            float foodNeed = country.population * 0.001f;
            float foodStock = country.invRiceTonnes + country.invCassavaTonnes;
            float foodMonths = foodNeed > 0 ? foodStock / foodNeed : 99f;

            if (_productionSummary != null)
            {
                _productionSummary.Clear();
                AddSummaryMetric(_productionSummary, "FOOD SECURITY", $"{country.foodSecurity:F0}%", EconomyTabContent.FoodSecurityColor(country));
                AddSummaryMetric(_productionSummary, "RESERVES", $"{foodMonths:F1} mo food", foodMonths >= 2f ? Color.green : Color.red);
                AddSummaryMetric(_productionSummary, "SOVEREIGNTY", $"{country.resourceSovereignty:F0}%", new Color(0, 210/255f, 1f));
                AddSummaryMetric(_productionSummary, "ENVIRONMENT", $"{country.environmentScore:F0}/100", Color.grey);
            }

            if (_productionCardsContainer == null) return;
            _productionCardsContainer.Clear();
            foreach (var comm in EconomyTabContent.Commodities)
                AddProductionCard(country, comm);

            if (_resourceDepositsList != null)
            {
                _resourceDepositsList.Clear();
                if (country.resources.Count == 0)
                {
                    _resourceDepositsList.Add(new Label("No mapped geological surveys.") { style = { color = Color.grey, fontSize = 9 } });
                }
                else
                {
                    foreach (var res in country.resources)
                        AddDepositChip(res);
                }
            }
        }

        private void AddSummaryMetric(VisualElement row, string label, string value, Color valueColor)
        {
            var col = new VisualElement() { style = { alignItems = Align.Center, minWidth = 80 } };
            col.Add(new Label(label) { style = { fontSize = 8, color = Color.grey } });
            col.Add(new Label(value) { style = { fontSize = 12, color = valueColor, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 4 } });
            row.Add(col);
        }

        private void AddProductionCard(CountryDataSO country, EconomyTabContent.CommodityDef comm)
        {
            float stock = comm.GetStock(country);
            float prod = comm.GetProduction(country);
            float cons = comm.GetConsumption(country);
            float net = prod - cons;
            string status = EconomyTabContent.StockpileStatus(stock, cons);
            Color statusColor = status.Contains("CRITICAL") || status == "LOW"
                ? new Color(255/255f, 82/255f, 82/255f)
                : (status.Contains("STRATEGIC") || status == "HEALTHY" ? new Color(0, 230/255f, 118/255f) : new Color(241/255f, 196/255f, 15/255f));

            var card = new VisualElement();
            card.AddToClassList("sub-panel");
            card.style.width = Length.Percent(48);
            card.style.marginBottom = 12;
            card.style.paddingTop = 14;
            card.style.paddingBottom = 14;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 8 } };
            header.Add(new Label(comm.Title) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } });
            header.Add(new Label(status) { style = { color = statusColor, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } });
            card.Add(header);

            card.Add(new Label(comm.Role) { style = { color = Color.grey, fontSize = 8, marginBottom = 6 } });
            card.Add(new Label($"STOCKPILE: {stock:N0} {comm.Unit}") { style = { color = new Color(0, 210/255f, 255/255f), fontSize = 11 } });

            var flowRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginTop = 8, justifyContent = Justify.SpaceBetween } };
            flowRow.Add(new Label($"+{prod:N0}/mo") { style = { color = new Color(0, 230/255f, 118/255f), fontSize = 9 } });
            flowRow.Add(new Label(cons > 0 ? $"-{cons:N0}/mo" : "—") { style = { color = new Color(255/255f, 82/255f, 82/255f), fontSize = 9 } });
            card.Add(flowRow);

            card.Add(new Label($"NET {(net >= 0 ? "+" : "")}{net:N0} {comm.Unit}/mo") { style = { color = Color.grey, fontSize = 8, marginTop = 4 } });

            float barVal = cons > 0 ? Mathf.Clamp(stock / (cons * 6f) * 100f, 0f, 100f) : Mathf.Min(stock > 0 ? 100f : 0f, 100f);
            card.Add(new ProgressBar() { value = barVal, highValue = 100, style = { height = 4, marginTop = 8 } });

            _productionCardsContainer.Add(card);
        }

        private void AddDepositChip(ResourceDeposit res)
        {
            var chip = new VisualElement();
            chip.AddToClassList("sub-panel");
            chip.style.width = Length.Percent(31);
            chip.style.marginBottom = 8;
            chip.style.paddingTop = 8;
            chip.style.paddingBottom = 8;
            chip.style.paddingLeft = 10;
            chip.style.paddingRight = 10;

            chip.Add(new Label(res.type.ToString().ToUpper()) { style = { color = Color.white, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
            chip.Add(new Label($"Abundance {res.abundance:F0}%") { style = { color = new Color(0, 210/255f, 1f), fontSize = 8, marginTop = 4 } });
            chip.Add(new ProgressBar() { value = res.abundance, highValue = 100, style = { height = 3, marginTop = 6 } });
            _resourceDepositsList.Add(chip);
        }

        private void PopulateLedger(CountryDataSO country, string sym, GameManager gm)
        {
            if (_incomeList == null || _expenseList == null) return;
            _incomeList.Clear();
            _expenseList.Clear();

float monthlyGDP = country.gdp / 12f;
            float taxEstUSD = monthlyGDP * (country.taxRate / 100f) * 0.35f;
            float taxEstB = taxEstUSD * country.exchangeRate;
            
            float tariffEstUSD = monthlyGDP * 0.015f * (country.importTariffRate / 100f);
            float tariffEstB = tariffEstUSD * country.exchangeRate;
            
            float resourceEstUSD = Mathf.Max(0f, country.income - taxEstUSD - tariffEstUSD);
            float resourceEstB = resourceEstUSD * country.exchangeRate;

            AddLedgerRow(_incomeList, "Tax Revenue", taxEstB, true, sym);
            AddLedgerRow(_incomeList, "Resource Dividends", resourceEstB, true, sym);
            if (country.importTariffRate > 0) AddLedgerRow(_incomeList, "Import Tariff Yield", tariffEstB, true, sym);
            
            if (country.foreignAidReceived > 0f)
            {
                float aidB = country.foreignAidReceived * country.exchangeRate;
                AddLedgerRow(_incomeList, "Foreign Aid", aidB, true, sym);
            }

            if (gm.DiplomacySystem is DiplomacySystem diplo)
            {
                foreach (var deal in diplo.ActiveDeals)
                {
                    float dealB = (deal.monthlyPaymentUSD * country.exchangeRate) / 1e9f;
                    if (deal.sellerCountry == country.countryName)
                        AddLedgerRow(_incomeList, $"Trade: {deal.resource} export", dealB, true, sym);
                    else if (deal.buyerCountry == country.countryName)
                        AddLedgerRow(_expenseList, $"Trade: {deal.resource} import", dealB, false, sym);
                }
            }

            if (country.activeTradeAgreements != null)
            {
                foreach (var agr in country.activeTradeAgreements)
                {
                    if (agr.status == TradeAgreementStatus.Active)
                    {
                        float valueInBillions = (agr.monthlyPaymentUSD * country.exchangeRate) / 1e9f;
                        if (agr.isSeller)
                        {
                            AddLedgerRow(_incomeList, $"Agrmt: {agr.resource} (to {agr.partnerCountry})", valueInBillions, true, sym);
                        }
                        else
                        {
                            AddLedgerRow(_expenseList, $"Agrmt: {agr.resource} (from {agr.partnerCountry})", valueInBillions, false, sym);
                        }
                    }
                }
            }

            float militaryB = country.militaryBudgetUSD * country.exchangeRate;
            AddLedgerRow(_expenseList, "Military Budget", militaryB, false, sym);

            if (country.activeOperations != null && country.activeOperations.Count > 0)
            {
                float totalOpCostUSD = 0f;
                foreach (var op in country.activeOperations)
                {
                    totalOpCostUSD += (op.costPerMonth > 0f) ? (op.costPerMonth / 1000f) : 0.05f;
                }
                float totalOpCostLocal = totalOpCostUSD * country.exchangeRate;
                AddLedgerRow(_expenseList, "Active Operations", totalOpCostLocal, false, sym);
            }

            float subsidyCostUSD = (country.gdp / 12f) * (country.subsidyLevel / 100f);
            float subsidyCostB = subsidyCostUSD * country.exchangeRate;
            AddLedgerRow(_expenseList, "Welfare Subsidies", subsidyCostB, false, sym);

            if (country.isImportingUranium)
            {
                float uraniumCostB = 0.05f * country.exchangeRate;
                AddLedgerRow(_expenseList, "Uranium Imports", uraniumCostB, false, sym);
            }

            if (gm.DebtSystem != null && country.debt > 0f)
            {
                float interestUSD = gm.DebtSystem.CalculateInterest(country);
                float principalPaymentUSD = (country.debt * 0.01f) * (country.debtServiceRatio / 100f);
                float totalMonthlyUSD = interestUSD + principalPaymentUSD;
                
                float totalDebtCostB = totalMonthlyUSD * country.exchangeRate;
                AddLedgerRow(_expenseList, "Central Bank Debt Servicing", totalDebtCostB, false, sym);
            }
        }

        private void AddLedgerRow(VisualElement container, string label, float value, bool isPositive, string sym)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 5;

            var name = new Label(label) { style = { color = Color.white, fontSize = 10 } };
            float signed = isPositive ? value : -value;
            var val = new Label(UiFormat.MoneySigned(signed, sym)) { style = { color = isPositive ? new Color(0, 230/255f, 118/255f) : new Color(255/255f, 82/255f, 82/255f), fontSize = 10 } };
            
            row.Add(name);
            row.Add(val);
            container.Add(row);
        }

        private void PopulateIndustry(CountryDataSO country, string sym)
        {
            float totalRev = 0f;
            foreach (var ind in EconomyTabContent.Industries)
                totalRev += ind.GetRevenueB(country);

            if (_industrySummary != null)
            {
                _industrySummary.Clear();
                _industrySummary.Add(new Label($"INDUSTRIAL OUTPUT · Est. {UiFormat.Money(totalRev, sym)}/mo across {EconomyTabContent.Industries.Length} sectors")
                {
                    style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 }
                });

                var barRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, height = 8 } };
                Color[] sectorColors =
                {
                    new Color(1f, 0.6f, 0.1f), new Color(0.6f, 0.6f, 0.65f), new Color(0.2f, 0.8f, 0.3f),
                    new Color(0.3f, 0.5f, 1f), new Color(0.7f, 0.4f, 1f)
                };
                for (int i = 0; i < EconomyTabContent.Industries.Length; i++)
                {
                    var ind = EconomyTabContent.Industries[i];
                    float share = totalRev > 0 ? ind.GetRevenueB(country) / totalRev : 0.2f;
                    var seg = new VisualElement() { style = { flexGrow = share, backgroundColor = sectorColors[i % sectorColors.Length], height = 8 } };
                    barRow.Add(seg);
                }
                _industrySummary.Add(barRow);
                _industrySummary.Add(new Label($"Unemployment {country.unemploymentRate:F0}% · Corruption leakage {country.corruptionIndex:F0}%")
                {
                    style = { color = Color.grey, fontSize = 8, marginTop = 8 }
                });
            }

            if (_industryCardsContainer == null) return;
            _industryCardsContainer.Clear();
            foreach (var ind in EconomyTabContent.Industries)
                AddIndustryCard(country, ind, sym);
        }

        private void AddIndustryCard(CountryDataSO country, EconomyTabContent.IndustryDef ind, string sym)
        {
            int level = ind.GetLevel(country);
            bool bullish = ind.IsBullish(country);
            float cost = ind.UpgradeCostB(country);
            float revenue = ind.GetRevenueB(country);

            var card = new VisualElement();
            card.AddToClassList("sub-panel");
            card.style.width = Length.Percent(48);
            card.style.marginBottom = 12;
            card.style.paddingTop = 14;
            card.style.paddingBottom = 14;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            header.Add(new Label(ind.Title) { style = { color = new Color(0, 210/255f, 1f), unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } });
            header.Add(new Label(bullish ? "▲ BULLISH" : "▼ PRESSURE") { style = { color = bullish ? new Color(0, 230/255f, 118/255f) : new Color(241/255f, 196/255f, 15/255f), fontSize = 8 } });
            card.Add(header);

            card.Add(new Label($"Tier {level}/10 · Workforce {(ind.GetWorkforce(country) / 1000f):F1}k") { style = { color = Color.white, fontSize = 10, marginTop = 6 } });
            card.Add(new Label($"Revenue ≈ {UiFormat.Money(revenue, sym)}/mo") { style = { color = new Color(0, 230/255f, 118/255f), fontSize = 10, marginTop = 4 } });
            card.Add(new Label($"Synergy: {ind.GetSynergy(country)}") { style = { color = Color.grey, fontSize = 8, marginTop = 6, whiteSpace = WhiteSpace.Normal } });

            card.Add(new ProgressBar() { value = level * 10f, highValue = 100, style = { height = 5, marginTop = 8 } });

            var btnRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginTop = 10, justifyContent = Justify.SpaceBetween } };
            var upgradeBtn = new Button(() =>
            {
                if (EconomyTabContent.TryUpgradeIndustry(country, ind, out string msg))
                    NotificationManager.Instance?.AddNotification(msg, Color.cyan);
                else
                    NotificationManager.Instance?.AddNotification(msg, Color.red);
                RefreshDisplay();
            }) { text = $"UPGRADE · {UiFormat.Money(cost, sym)}" };
            upgradeBtn.AddToClassList("button-premium");
            upgradeBtn.style.height = 26;
            upgradeBtn.style.fontSize = 8;
            upgradeBtn.style.flexGrow = 1;
            btnRow.Add(upgradeBtn);
            card.Add(btnRow);

            _industryCardsContainer.Add(card);
        }

        private void PopulateInfra(CountryDataSO country)
        {
            float score = EconomyTabContent.NationalInfraScore(country);

            if (_infraSummary != null)
            {
                _infraSummary.Clear();
                var top = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
                top.Add(new Label($"NATIONAL INFRASTRUCTURE INDEX") { style = { color = Color.grey, fontSize = 9, letterSpacing = 2 } });
                top.Add(new Label($"{score:F0}/100") { style = { color = score >= 60f ? new Color(0, 230/255f, 118/255f) : new Color(241/255f, 196/255f, 15/255f), fontSize = 22, unityFontStyleAndWeight = FontStyle.Bold } });
                _infraSummary.Add(top);
                _infraSummary.Add(new ProgressBar() { value = score, highValue = 100, style = { height = 8, marginTop = 8, marginBottom = 8 } });

                var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                AddSummaryMetric(row, "TRANSPORT", $"{(country.roadLevel + country.railLevel + country.portLevel) * 3.3f:F0}%", new Color(0, 210/255f, 1f));
                AddSummaryMetric(row, "ENERGY", $"{country.electricityLevel:F0}%", country.electricityLevel > 50f ? Color.green : Color.red);
                
                float cap = PowerGridManager.Instance != null ? PowerGridManager.Instance.CalculateCapacity(country) : 0f;
                float dem = PowerGridManager.Instance != null ? PowerGridManager.Instance.CalculateDemand(country) : 0f;
                float netPower = cap - dem;
                string netStr = netPower >= 0 ? $"+{netPower:F0} MW" : $"{netPower:F0} MW";
                AddSummaryMetric(row, "GRID BALANCE", netStr, netPower >= 0 ? new Color(0, 230/255f, 118/255f) : new Color(255/255f, 82/255f, 82/255f));
                
                AddSummaryMetric(row, "DIGITAL", $"{country.telecomLevel * 10f:F0}%", new Color(186/255f, 104/255f, 200/255f));
                _infraSummary.Add(row);
            }

            if (_infraSynergyText != null)
            {
                var synergies = new List<string>();
                if (country.portLevel > country.railLevel)
                    synergies.Add("Ports outpacing rail — consider Trans-National Rail.");
                if (country.electricityLevel < 50f && country.indManufacturing > 3)
                    synergies.Add("Factories constrained by weak grid.");
                if (country.roadLevel >= 5 && country.foodSecurity < 60f)
                    synergies.Add("Roads built but farms still isolated — invest agri.");
                if (country.telecomLevel >= 4 && country.indTechIT < 3)
                    synergies.Add("Digital backbone ready for tech park expansion.");
                if (country.hasNuclearPower)
                    synergies.Add("Nuclear baseload stabilizing industrial zones.");
                _infraSynergyText.text = synergies.Count > 0 ? string.Join(" ", synergies) : "All major networks operating within design parameters.";
            }

            if (_infraCardsContainer == null) return;
            _infraCardsContainer.Clear();

            EconomyTabContent.InfraCategory lastCat = (EconomyTabContent.InfraCategory)(-1);
            foreach (var def in EconomyTabContent.Infrastructure)
            {
                if (def.Category != lastCat)
                {
                    lastCat = def.Category;
                    _infraCardsContainer.Add(new Label(lastCat.ToString().ToUpper())
                    {
                        style = { color = Color.grey, fontSize = 9, letterSpacing = 2, marginTop = 8, marginBottom = 6 }
                    });
                }
                AddInfraCard(country, def);
            }

            AddPowerPlantBreakdown(country);
        }

        private void AddInfraCard(CountryDataSO country, EconomyTabContent.InfraDef def)
        {
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            float rating = def.GetRating(country);
            float cost = def.GetInvestCostB(country);
            string bottleneck = def.GetBottleneck(country);
            bool canInvest = def.CanInvest(country);

            var card = new VisualElement();
            card.AddToClassList("sub-panel");
            card.style.marginBottom = 10;
            card.style.paddingTop = 14;
            card.style.paddingBottom = 14;
            card.style.paddingLeft = 14;
            card.style.paddingRight = 14;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
            header.Add(new Label(def.Title) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } });
            header.Add(new Label(def.IconTag) { style = { color = new Color(0, 210/255f, 1f), fontSize = 8 } });
            card.Add(header);

            card.Add(new Label(def.GetStatus(country)) { style = { color = new Color(0, 210/255f, 1f), fontSize = 10, marginTop = 6 } });
            card.Add(new Label(def.GetEffect(country)) { style = { color = Color.grey, fontSize = 8, marginTop = 4, whiteSpace = WhiteSpace.Normal } });

            if (bottleneck != "—")
                card.Add(new Label($"⚠ {bottleneck}") { style = { color = new Color(255/255f, 171/255f, 64/255f), fontSize = 8, marginTop = 6 } });

            card.Add(new ProgressBar() { value = rating, highValue = 100, style = { height = 6, marginTop = 8 } });

            var btnRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, marginTop = 10, justifyContent = Justify.SpaceBetween } };
            var investBtn = new Button(() =>
            {
                if (EconomyTabContent.TryInvestInfra(country, def, out string msg))
                    NotificationManager.Instance?.AddNotification(msg, Color.cyan);
                else
                    NotificationManager.Instance?.AddNotification(msg, Color.red);
                RefreshDisplay();
            })
            { text = canInvest ? $"INVEST · {UiFormat.Money(cost, sym)}" : "MAX LEVEL" };
            investBtn.AddToClassList("button-premium");
            investBtn.style.height = 26;
            investBtn.style.fontSize = 8;
            investBtn.SetEnabled(canInvest);
            btnRow.Add(investBtn);

            if (def.Id == "Power")
            {
                var plantBtn = new Button(() => ShowBlueprintCatalog())
                { text = "BUILD PLANT" };
                plantBtn.AddToClassList("button-premium");
                plantBtn.style.height = 26;
                plantBtn.style.fontSize = 8;
                plantBtn.style.marginLeft = 8;
                btnRow.Add(plantBtn);
            }
            card.Add(btnRow);
            _infraCardsContainer.Add(card);
        }

        private void AddPowerPlantBreakdown(CountryDataSO c)
        {
            var panel = new VisualElement();
            panel.AddToClassList("sub-panel");
            panel.style.paddingTop = 12;
            panel.style.paddingBottom = 12;
            panel.style.paddingLeft = 14;
            panel.style.paddingRight = 14;
            panel.style.marginTop = 8;

            panel.Add(new Label("GENERATION MIX") { style = { color = Color.grey, fontSize = 9, letterSpacing = 2, marginBottom = 8 } });
            AddPlantRow(panel, "Gas", c.powerGasPlants, "120 MW each");
            AddPlantRow(panel, "Hydro", c.powerHydroPlants, "80 MW each");
            AddPlantRow(panel, "Nuclear", c.powerNuclearPlants, c.hasNuclearPower ? "400 MW · online" : "Requires uranium");
            AddPlantRow(panel, "Solar", c.powerSolarPlants, "25 MW each");
            AddPlantRow(panel, "Wind", c.powerWindPlants, "35 MW each");
            panel.Add(new Label($"Transmission efficiency {c.gridTransmissionEfficiency:P0} · Blackout risk {c.currentBlackoutRisk:P0}")
            {
                style = { color = Color.grey, fontSize = 8, marginTop = 8 }
            });
            _infraCardsContainer.Add(panel);
        }

        private static void AddPlantRow(VisualElement parent, string name, int count, string note)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 } };
            row.Add(new Label($"{name}: {count}") { style = { color = Color.white, fontSize = 9 } });
            row.Add(new Label(note) { style = { color = Color.grey, fontSize = 8 } });
            parent.Add(row);
        }

        private void PopulateConstruction(CountryDataSO country, string sym)
        {
            if (_constructionSummary != null)
            {
                _constructionSummary.Clear();
                float monthlySpend = 0f;
                int pipelineCount = 0;
                foreach (var p in country.activeProjects)
                {
                    if (p.stage == ProjectStage.Planned || p.stage == ProjectStage.Funded || p.stage == ProjectStage.UnderConstruction)
                    {
                        pipelineCount++;
                        if (p.totalMonths > 0) monthlySpend += p.totalCost / p.totalMonths;
                    }
                }

                _constructionSummary.Add(new Label($"QUEUE {pipelineCount}/5 · Burn {UiFormat.Money(monthlySpend, sym)}/mo · Treasury {UiFormat.Money(GameManager.Instance.playerBudget, sym)}")
                {
                    style = { color = Color.white, fontSize = 10 }
                });
            }

            if (_constructionCardsContainer == null) return;
            _constructionCardsContainer.Clear();

            int displayedCount = 0;
            foreach (var proj in country.activeProjects)
            {
                if (proj.stage == ProjectStage.Planned || proj.stage == ProjectStage.Funded || proj.stage == ProjectStage.UnderConstruction)
                {
                    AddConstructionProjectRow(country, proj, sym);
                    displayedCount++;
                }
            }

            if (displayedCount == 0)
            {
                _constructionCardsContainer.Add(new Label("No active sites — open blueprints to commission a megaproject.")
                {
                    style = { color = Color.grey, alignSelf = Align.Center, marginTop = 20, fontSize = 10, whiteSpace = WhiteSpace.Normal }
                });
            }
        }

        private void AddConstructionProjectRow(CountryDataSO country, ActiveProject proj, string sym)
        {
            var row = new VisualElement();
            row.AddToClassList("sub-panel");
            row.style.marginBottom = 8;
            row.style.paddingTop = 12;
            row.style.paddingBottom = 12;
            row.style.paddingLeft = 14;
            row.style.paddingRight = 14;

            var header = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            header.Add(new Label(proj.projectName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } });
            header.Add(new Label(proj.projectType.ToUpper()) { style = { color = new Color(0, 210/255f, 1f), fontSize = 8 } });
            row.Add(header);

            row.Add(new Label($"{proj.regionName} · Target: {proj.targetID}") { style = { color = Color.grey, fontSize = 8, marginTop = 4 } });
            float pct = proj.totalMonths > 0 ? (proj.totalMonths - proj.monthsRemaining) / (float)proj.totalMonths * 100f : 0f;
            row.Add(new Label($"{pct:F0}% complete · {proj.monthsRemaining} mo left · {UiFormat.Money(proj.totalCost / Mathf.Max(1, proj.totalMonths), sym)}/mo")
            {
                style = { color = Color.grey, fontSize = 8, marginTop = 4 }
            });
            row.Add(new ProgressBar()
            {
                value = proj.totalMonths - proj.monthsRemaining,
                highValue = Mathf.Max(1, proj.totalMonths),
                style = { height = 6, marginTop = 8 }
            });

            var cancelBtn = new Button(() =>
            {
                country.activeProjects.Remove(proj);
                NotificationManager.Instance?.AddNotification($"Cancelled: {proj.projectName}", new Color(255/255f, 171/255f, 64/255f));
                RefreshDisplay();
            }) { text = "CANCEL" };
            cancelBtn.AddToClassList("button-premium");
            cancelBtn.style.height = 24;
            cancelBtn.style.fontSize = 8;
            cancelBtn.style.marginTop = 8;
            cancelBtn.style.backgroundColor = new Color(255/255f, 82/255f, 82/255f, 0.15f);
            row.Add(cancelBtn);

            _constructionCardsContainer.Add(row);
        }

        private void PopulateBlueprintCatalog(CountryDataSO country)
        {
            if (_blueprintCatalog == null) return;
            _blueprintCatalog.Clear();

            foreach (var bp in EconomyTabContent.Blueprints)
            {
                var card = new VisualElement();
                card.AddToClassList("sub-panel");
                card.style.width = Length.Percent(48);
                card.style.marginBottom = 10;
                card.style.paddingTop = 12;
                card.style.paddingBottom = 12;
                card.style.paddingLeft = 12;
                card.style.paddingRight = 12;

                card.Add(new Label(bp.Name) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10 } });
                card.Add(new Label($"{bp.Type} · {bp.Months} mo · {bp.TotalCostB:N0}B total") { style = { color = new Color(0, 210/255f, 1f), fontSize = 8, marginTop = 4 } });
                card.Add(new Label(bp.Description) { style = { color = Color.grey, fontSize = 8, marginTop = 4, whiteSpace = WhiteSpace.Normal } });

                if (bp.TargetId == "PowerNuclear" && !country.isImportingUranium && !country.resources.Exists(r => r.type == MineralType.Uranium))
                    card.Add(new Label("Requires domestic uranium or import deal") { style = { color = new Color(255/255f, 171/255f, 64/255f), fontSize = 7, marginTop = 4 } });

                var bpCopy = bp;
                var startBtn = new Button(() =>
                {
                    if (EconomyTabContent.TryStartProject(country, bpCopy, out string msg))
                        NotificationManager.Instance?.AddNotification(msg, Color.green);
                    else
                        NotificationManager.Instance?.AddNotification(msg, Color.red);
                    RefreshDisplay();
                }) { text = "COMMISSION" };
                startBtn.AddToClassList("button-premium");
                startBtn.style.height = 24;
                startBtn.style.fontSize = 8;
                startBtn.style.marginTop = 8;
                card.Add(startBtn);

                _blueprintCatalog.Add(card);
            }
        }

private void RefreshTaxationTab(CountryDataSO country, string sym)
        {
            if (_root == null) return;
            
            float monthlyGDP = country.gdp / 12f * country.exchangeRate;

            var lblInc = _root.Q<Label>("TaxIncome_Rate");
            var barInc = _root.Q<ProgressBar>("TaxIncome_Bar");
            var revInc = _root.Q<Label>("TaxIncome_Revenue");
            if (lblInc != null) lblInc.text = $"{country.incomeTaxRate:F1}%";
            if (barInc != null) barInc.value = country.incomeTaxRate;
            float incRev = monthlyGDP * (country.incomeTaxRate / 100f) * 0.35f;
            if (revInc != null) revInc.text = UiFormat.Money(incRev, sym);

            var revCorp = _root.Q<Label>("TaxCorporate_Revenue");
            float corpRev = monthlyGDP * (country.corporateTaxRate / 100f) * 0.35f;
            if (revCorp != null) revCorp.text = UiFormat.Money(corpRev, sym);

            var lblVat = _root.Q<Label>("TaxVAT_Rate");
            var barVat = _root.Q<ProgressBar>("TaxVAT_Bar");
            var revVat = _root.Q<Label>("TaxVAT_Revenue");
            if (lblVat != null) lblVat.text = $"{country.vatRate:F1}%";
            if (barVat != null) barVat.value = country.vatRate;
            float vatRev = monthlyGDP * (country.vatRate / 100f) * 0.35f;
            if (revVat != null) revVat.text = UiFormat.Money(vatRev, sym);

            var revImp = _root.Q<Label>("TaxImport_Revenue");
            float impRev = monthlyGDP * 0.015f * (country.importTariffRate / 100f);
            if (revImp != null) revImp.text = UiFormat.Money(impRev, sym);

            var lblExp = _root.Q<Label>("TaxExport_Rate");
            var barExp = _root.Q<ProgressBar>("TaxExport_Bar");
            var revExp = _root.Q<Label>("TaxExport_Revenue");
            if (lblExp != null) lblExp.text = $"{country.exportDutyRate:F1}%";
            if (barExp != null) barExp.value = country.exportDutyRate;
            float expRev = monthlyGDP * 0.01f * (country.exportDutyRate / 100f);
            if (revExp != null) revExp.text = UiFormat.Money(expRev, sym);

            float total = incRev + corpRev + vatRev + impRev + expRev;
            var lblTot = _root.Q<Label>("TaxTotal_Revenue");
            if (lblTot != null) lblTot.text = UiFormat.Money(total, sym);
            
            var lblRatio = _root.Q<Label>("TaxTotal_Ratio");
            float gdpLocal = country.gdp * country.exchangeRate;
            if (lblRatio != null) lblRatio.text = $"{(gdpLocal > 0 ? ((total * 12f) / gdpLocal * 100f) : 0f):F1}%";
            var lblMoM = _root.Q<Label>("TaxTotal_MoMDelta");
            if (lblMoM != null) {
                float prevTaxRev = country.prevMonthlyTaxRevenue * country.exchangeRate;
                float diff = total - prevTaxRev;
                float pct = prevTaxRev > 0 ? (diff / prevTaxRev) * 100f : 0f;
                lblMoM.text = $"{(diff >= 0 ? "+" : "")}{UiFormat.Money(diff, sym)} ({(diff >= 0 ? "+" : "")}{pct:F1}%)";
                lblMoM.RemoveFromClassList("positive");
                lblMoM.RemoveFromClassList("negative");
                lblMoM.AddToClassList(diff >= 0 ? "positive" : "negative");
            }

            float compliance = Mathf.Clamp(100f - country.corruptionIndex, 10f, 100f);
            float eff = Mathf.Lerp(60f, 95f, country.stability / 100f);
            float gap = total * (1f - (eff / 100f));
            
            if (_root.Q<Label>("TaxCompliance_Rate") is Label lComp) lComp.text = $"{compliance:F0}%";
            if (_root.Q<Label>("TaxCollectionEff") is Label lEff) lEff.text = $"{eff:F0}%";
            if (_root.Q<Label>("TaxGap") is Label lGap) lGap.text = UiFormat.Money(gap, sym);
            if (_root.Q<Label>("TaxArrears") is Label lArr) lArr.text = UiFormat.Money(country.taxArrears * country.exchangeRate, sym);

            if (_root.Q<Label>("ComplianceDonut_Label") is Label lDonut) lDonut.text = $"{compliance:F0}%\nCompliance";
            if (_root.Q<Label>("ComplianceCompliant") is Label lC1) lC1.text = $"{compliance:F0}%";
            float nonComp = Mathf.Max(0f, 100f - compliance);
            float partial = nonComp * 0.5f;
            float outright = nonComp * 0.5f;
            if (_root.Q<Label>("CompliancePartial") is Label lC2) lC2.text = $"{partial:F0}%";
            if (_root.Q<Label>("ComplianceNonCompliant") is Label lC3) lC3.text = $"{outright:F0}%";

            var policiesCont = _root.Q<VisualElement>("TaxPoliciesContainer");
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
            var simTotal = _root.Q<Label>("TaxSim_Total");
            if (simTotal != null) simTotal.text = UiFormat.Money(total, sym);
        }

        private void RefreshBudgetTab(CountryDataSO country, string sym)
        {
            if (_root == null) return;
            
            float totalB = country.spending * country.exchangeRate;
            float revTotalB = country.income * country.exchangeRate;
            
            if (_root.Q<Label>("BudgetSummary_Revenue") is Label lRev) lRev.text = UiFormat.Money(revTotalB, sym);
            if (_root.Q<Label>("BudgetSummary_Expenditure") is Label lExp) lExp.text = UiFormat.Money(totalB, sym);
            float balB = revTotalB - totalB;
            if (_root.Q<Label>("BudgetSummary_Balance") is Label lBal) {
                lBal.text = $"{(balB >= 0 ? "+" : "")}{UiFormat.Money(balB, sym)}";
                lBal.RemoveFromClassList("positive"); lBal.RemoveFromClassList("negative");
                lBal.AddToClassList(balB >= 0 ? "positive" : "negative");
            }
            if (_root.Q<Label>("BudgetSummary_Status") is Label lStatus) {
                string sText = balB >= 0 ? "BALANCED" : "DEFICIT";
                lStatus.text = sText;
                lStatus.RemoveFromClassList("positive"); lStatus.RemoveFromClassList("negative");
                lStatus.AddToClassList(balB >= 0 ? "positive" : "negative");
            }

            if (_root.Q<Label>("BudgetAvailable") is Label lAv) lAv.text = UiFormat.Money(revTotalB, sym) + " / month";
            if (_root.Q<Label>("BudgetLastBalance") is Label lLast) {
                float lastB = country.prevMonthBalance * country.exchangeRate;
                lLast.text = $"{(lastB >= 0 ? "+" : "")}{UiFormat.Money(lastB, sym)}";
            }

            float milPct = _draftMilitary >= 0 ? _draftMilitary : country.budgetMilitaryShare;
            float infraPct = _draftInfrastructure >= 0 ? _draftInfrastructure : country.budgetInfrastructureShare;
            float eduPct = _draftEducation >= 0 ? _draftEducation : country.budgetEducationShare;
            float healthPct = _draftHealth >= 0 ? _draftHealth : country.budgetHealthShare;
            float energyPct = _draftEnergy >= 0 ? _draftEnergy : country.budgetEnergyShare;
            float agriPct = _draftAgriculture >= 0 ? _draftAgriculture : country.budgetAgricultureShare;
            float intelPct = _draftIntelligence >= 0 ? _draftIntelligence : country.budgetIntelligenceShare;
            float welPct = _draftWelfare >= 0 ? _draftWelfare : country.budgetWelfareShare;

            UpdateBudgetStaticRow("Military", milPct, totalB, sym);
            UpdateBudgetStaticRow("Intelligence", intelPct, totalB, sym);
            UpdateBudgetStaticRow("Education", eduPct, totalB, sym);
            UpdateBudgetStaticRow("Health", healthPct, totalB, sym);
            UpdateBudgetStaticRow("Infrastructure", infraPct, totalB, sym);
            UpdateBudgetStaticRow("Agriculture", agriPct, totalB, sym);
            UpdateBudgetStaticRow("Energy", energyPct, totalB, sym);
            UpdateBudgetStaticRow("Welfare", welPct, totalB, sym);

            if (_root.Q<Label>("BudgetDonut_Label") is Label lDonut) lDonut.text = $"{UiFormat.Money(totalB, sym)}\nTotal";
            if (_root.Q<VisualElement>("BudgetDonut_Legend") is VisualElement vLeg) {
                vLeg.Clear();
                vLeg.Add(new Label($"● Military                 {milPct:F0}%   ({UiFormat.Money(totalB * milPct / 100f, sym)})"));
                vLeg.Add(new Label($"● Infrastructure          {infraPct:F0}%   ({UiFormat.Money(totalB * infraPct / 100f, sym)})"));
                vLeg.Add(new Label($"● Education               {eduPct:F0}%   ({UiFormat.Money(totalB * eduPct / 100f, sym)})"));
                vLeg.Add(new Label($"● Health                  {healthPct:F0}%   ({UiFormat.Money(totalB * healthPct / 100f, sym)})"));
                vLeg.Add(new Label($"● Agriculture              {agriPct:F0}%   ({UiFormat.Money(totalB * agriPct / 100f, sym)})"));
                vLeg.Add(new Label($"● Energy                   {energyPct:F0}%   ({UiFormat.Money(totalB * energyPct / 100f, sym)})"));
                vLeg.Add(new Label($"● Intelligence             {intelPct:F0}%   ({UiFormat.Money(totalB * intelPct / 100f, sym)})"));
                vLeg.Add(new Label($"● Welfare                  {welPct:F0}%   ({UiFormat.Money(totalB * welPct / 100f, sym)})"));
            }

            if (_root.Q<Label>("BudgetTrendChart") is Label lChart && country.budgetHistory != null) {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Net Balance Trend (Last 6 Months)");
                foreach(float histB in country.budgetHistory) {
                    string symbol = histB >= 0 ? "■" : "□";
                    sb.AppendLine($"{symbol} {UiFormat.Money(histB * country.exchangeRate, sym)}");
                }
                if (country.budgetHistory.Count == 0) {
                    sb.AppendLine("No historical data available yet.");
                }
                lChart.text = sb.ToString();
            }

            if (_root.Q<Label>("BudgetEffect_Growth") is Label lEg) lEg.text = $"Economic Growth\n{(country.gdpGrowthRate > 0 ? "+" : "")}{country.gdpGrowthRate:P1}";
            if (_root.Q<Label>("BudgetEffect_Military") is Label lEm) lEm.text = $"Military Readiness\n{country.militaryReadiness:F0}%";
            if (_root.Q<Label>("BudgetEffect_Inflation") is Label lEi) lEi.text = $"Inflation\n{country.inflationRate:P1}";
            if (_root.Q<Label>("BudgetEffect_Employment") is Label lEmp) lEmp.text = "Employment Base\nStable";
            if (_root.Q<Label>("BudgetEffect_Approval") is Label lEa) lEa.text = $"Approval Rating\n{country.publicApproval:F0}%";
            if (_root.Q<Label>("BudgetEffect_Debt") is Label lEd) lEd.text = $"Debt Change\n{(balB < 0 ? "+" : "")}{UiFormat.Money(Mathf.Abs(balB), sym)}";

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

        private void UpdateBudgetStaticRow(string ministry, float pct, float totalSpendB, string sym)
        {
            var lblVal = _root.Q<Label>($"Budget{ministry}_Value");
            var bar = _root.Q<ProgressBar>($"Budget{ministry}_Bar");
            var lblPct = _root.Q<Label>($"Budget{ministry}_Pct");

            if (lblVal != null) lblVal.text = UiFormat.Money(totalSpendB * (pct / 100f), sym);
            if (bar != null) bar.value = pct;
            if (lblPct != null) lblPct.text = $"{pct:F0}%";
        }

        private void RefreshIndustryTab(CountryDataSO country, string sym)
        {
            if (_root == null) return;
            
            float totalRev = 0f;
            EconomyTabContent.IndustryDef topInd = default;
            EconomyTabContent.IndustryDef weakInd = default;
            float topRev = -1f;
            float weakRev = float.MaxValue;

            foreach (var ind in EconomyTabContent.Industries)
            {
                float rev = ind.GetRevenueB(country);
                totalRev += rev;
                if (rev > topRev) { topRev = rev; topInd = ind; }
                if (rev < weakRev) { weakRev = rev; weakInd = ind; }
            }

            if (_root.Q<Label>("IndustryOverview_Output") is Label lOut) lOut.text = UiFormat.Money(totalRev, sym) + " / mo";
            float pctGDP = country.gdp > 0 ? (totalRev / (country.gdp * country.exchangeRate / 12f)) * 100f : 0f;
            if (_root.Q<Label>("IndustryOverview_GDPPct") is Label lGdpPct) lGdpPct.text = $"Total Contribution to GDP: {pctGDP:F1}%";
            if (_root.Q<Label>("IndustryOverview_Meter") is Label lMeter) lMeter.text = $"{pctGDP:F1}% of GDP";
            if (_root.Q<ProgressBar>("IndustryOverview_Bar") is ProgressBar pgBar) pgBar.value = pctGDP;
            
            if (topInd.Id != null) {
                if (_root.Q<Label>("IndustryTop_Name") is Label lT) lT.text = topInd.Title;
                if (_root.Q<Label>("IndustryTop_Revenue") is Label lTR) lTR.text = $"{UiFormat.Money(topRev, sym)} / mo";
                if (_root.Q<Label>("IndustryTop_Growth") is Label lTG) lTG.text = topInd.IsBullish(country) ? "▲ Growing" : "▼ Slowing";
            }
            if (weakInd.Id != null) {
                if (_root.Q<Label>("IndustryWeak_Name") is Label lW) lW.text = weakInd.Title;
                if (_root.Q<Label>("IndustryWeak_Revenue") is Label lWR) lWR.text = $"{UiFormat.Money(weakRev, sym)} / mo";
            }

            foreach (var ind in EconomyTabContent.Industries)
            {
                int level = ind.GetLevel(country);
                float cost = ind.UpgradeCostB(country);
                float revenue = ind.GetRevenueB(country);
                float contrib = totalRev > 0 ? (revenue / totalRev) * 100f : 0f;

                var tier = _root.Q<Label>($"Industry{ind.Id}_Tier");
                if (tier != null) tier.text = $"Tier {level}/10  •  Workforce {(ind.GetWorkforce(country) / 1000f):F1}k";

                var btn = _root.Q<Button>($"Industry{ind.Id}_Btn");
                if (btn != null) btn.text = $"UPGRADE  •  {UiFormat.Money(cost, sym)}";

                var stat1 = _root.Q<Label>($"Industry{ind.Id}_Stat1");
                if (stat1 != null) stat1.text = $"Contribution to GDP\n{contrib:F0}%";
                
                var ringObj = _root.Q<Label>($"Ring{ind.Id}");
                if (ringObj != null) ringObj.text = $"{contrib:F0}%\n{ind.Title}";
            }

            if (_currentAdviserRec != null) {
                if (_root.Q<Label>("IndustryAdviserName") is Label aName) aName.text = "Dr. Adekunle Okafor";
                if (_root.Q<Label>("IndustryAdviserRec") is Label aRec) aRec.text = _currentAdviserRec.Value.Category;
                if (_root.Q<Label>("IndustryAdviserBody") is Label aBody) aBody.text = _currentAdviserRec.Value.Rationale;
            }

            var alertsCont = _root.Q<VisualElement>("IndustryAlertsContainer");
            if (alertsCont != null) {
                while (alertsCont.childCount > 1) alertsCont.RemoveAt(1);
                var alerts = BuildUIAlerts(country).FindAll(a => a.Severity != AlertSeverity.Info);
                int ct = 0;
                foreach (var a in alerts) {
                    if (ct >= 3) break;
                    var row = new VisualElement(); row.AddToClassList("industry-alert-row");
                    var icon = new Label("⚠"); icon.AddToClassList("industry-alert-icon");
                    icon.AddToClassList(a.Severity == AlertSeverity.Critical ? "negative" : "warning");
                    row.Add(icon);
                    var info = new VisualElement();
                    var title = new Label(a.Category); title.AddToClassList("industry-card-title"); info.Add(title);
                    var copy = new Label(a.Message); copy.AddToClassList("industry-copy"); info.Add(copy);
                    row.Add(info);
                    var view = new Label("VIEW"); view.AddToClassList("market"); row.Add(view);
                    alertsCont.Add(row);
                    ct++;
                }
                if (ct == 0) {
                    var empty = new Label("All industries operating nominally.");
                    empty.AddToClassList("industry-copy"); empty.style.marginTop = 10;
                    alertsCont.Add(empty);
                }
            }
        }

        private void RefreshProjectsTab(CountryDataSO country, string sym)
        {
            var grid = _root?.Q<ScrollView>("ProjectsActiveGrid");
            if (grid == null) return;
            grid.Clear();
            
            float monthlySpend = 0f;
            int pipelineCount = 0;
            float totalMonths = 0f;
            float totalCost = 0f;
            int jobsCreated = 0;
            float gdpBoost = 0f;

            foreach (var p in country.activeProjects)
            {
                if (p.stage == ProjectStage.Planned || p.stage == ProjectStage.Funded || p.stage == ProjectStage.UnderConstruction)
                {
                    pipelineCount++;
                    if (p.totalMonths > 0) monthlySpend += p.totalCost / p.totalMonths;
                    totalMonths += p.monthsRemaining;
                    totalCost += p.totalCost;
                    if (p.projectType != null) {
                        jobsCreated += p.projectType.ToLower().Contains("infra") ? 20000 : 10000;
                        gdpBoost += p.projectType.ToLower().Contains("infra") ? 0.8f : 0.4f;
                    }

                    AddConstructionProjectCard(grid, country, p, sym);
                }
            }

            if (_root.Q<Label>("ProjectsKPI_Count") is Label lK1) lK1.text = pipelineCount.ToString();
            if (_root.Q<Label>("ProjectsKPI_MonthlyCost") is Label lK2) lK2.text = UiFormat.Money(monthlySpend, sym);
            if (_root.Q<Label>("ProjectsKPI_GDPBoost") is Label lK3) lK3.text = $"+{gdpBoost:F1}%";
            if (_root.Q<Label>("ProjectsKPI_Jobs") is Label lK4) lK4.text = $"{jobsCreated:N0}";
            if (_root.Q<Label>("ProjectsKPI_AvgCompletion") is Label lK5) lK5.text = $"{(pipelineCount>0?totalMonths/pipelineCount:0f):F1} Months";

            if (_root.Q<Label>("ProjectsActiveHeading") is Label lAH) lAH.text = $"ACTIVE PROJECTS ({pipelineCount})";

            if (_constructionSummary != null)
            {
                _constructionSummary.Clear();
                _constructionSummary.Add(new Label($"QUEUE {pipelineCount}/5 • Burn {UiFormat.Money(monthlySpend, sym)}/mo • Treasury {UiFormat.Money(GameManager.Instance?.playerBudget ?? 0, sym)}")
                {
                    style = { color = Color.white, fontSize = 10 }
                });
            }

            if (_currentAdviserRec != null) {
                if (_root.Q<Label>("ProjectsAdviserBody") is Label lAdv) lAdv.text = _currentAdviserRec.Value.Rationale;
            }
        }

        private void AddConstructionProjectCard(VisualElement container, ActiveProject p, string sym) {
            AddConstructionProjectCard(container, GameManager.Instance?.playerCountry, p, sym);
        }

        private void AddConstructionProjectCard(VisualElement container, CountryDataSO country, ActiveProject p, string sym)
        {
            var card = new VisualElement();
            card.AddToClassList("project-card");

            float pct = p.totalMonths > 0 ? (p.totalMonths - p.monthsRemaining) / (float)p.totalMonths * 100f : 0f;
            float monthlyVal = p.totalMonths > 0 ? p.totalCost / p.totalMonths : p.totalCost;
            float spent = p.totalCost - (monthlyVal * p.monthsRemaining);

            var img = new VisualElement();
            img.AddToClassList("project-image");
            if (p.projectType.ToString().Contains("Infra")) img.AddToClassList("port");
            card.Add(img);

            card.Add(new Label(p.projectName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 10 } });
            card.Add(new Label($"{p.projectType.ToString().ToUpper()}") { style = { color = new Color(0, 210/255f, 1f), fontSize = 8, marginBottom = 8 } });

            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            row.Add(new ProgressBar() { value = pct, highValue = 100, style = { flexGrow = 1, height = 4 } });
            row.Add(new Label($"{pct:F0}%") { style = { color = Color.white, fontSize = 9, marginLeft = 6 } });
            card.Add(row);

            card.Add(new Label($"Total Cost: {UiFormat.Money(p.totalCost, sym)}\nSpent: {UiFormat.Money(spent, sym)}\nMonthly: {UiFormat.Money(monthlyVal, sym)}\nLeft: {p.monthsRemaining} Months") { style = { color = Color.grey, fontSize = 8, marginTop = 8, whiteSpace = WhiteSpace.Normal } });

card.RegisterCallback<ClickEvent>(evt => {
                if (_root.Q<Label>("SideProject_Title") is Label tLabel) tLabel.text = p.projectName.ToUpper();
                if (_root.Q<Label>("SideProject_Overview") is Label oLabel) oLabel.text = string.IsNullOrEmpty(p.description) ? "National strategic project driving growth across key economic sectors." : p.description;
                if (_root.Q<VisualElement>("SideProject_Info") is VisualElement infoView) {
                    infoView.Clear();
                    infoView.Add(new Label($"Category                         {p.projectType}") { style = { whiteSpace = WhiteSpace.NoWrap, marginBottom = 5 } });
                    if (!string.IsNullOrEmpty(p.regionName)) infoView.Add(new Label($"Location                         {p.regionName}") { style = { whiteSpace = WhiteSpace.NoWrap, marginBottom = 5 } });
                    infoView.Add(new Label($"Expected Completion              {p.monthsRemaining} Months") { style = { whiteSpace = WhiteSpace.NoWrap, marginBottom = 5 } });
                }
                if (_root.Q<VisualElement>("SideProject_Benefits") is VisualElement benefitsView) {
                    benefitsView.Clear();
                    float gBoost = (p.projectType != null && p.projectType.ToLower().Contains("infra")) ? 0.8f : 0.4f;
                    int jC = (p.projectType != null && p.projectType.ToLower().Contains("infra")) ? 20000 : 10000;
                    var lBoost = new Label($"GDP Growth                       +{gBoost:F1}%");
                    lBoost.AddToClassList("positive"); lBoost.style.marginBottom = 5;
                    benefitsView.Add(lBoost);
                    
                    var lJobs = new Label($"Jobs Created                     +{jC:N0}");
                    lJobs.AddToClassList("positive"); lJobs.style.marginBottom = 5;
                    benefitsView.Add(lJobs);
                }
            });

            container.Add(card);
        }

private void RefreshOverviewTab(CountryDataSO country, string sym, GameManager gm)
        {
            RefreshOverviewReports(country, gm);
            RefreshOverviewSectors(country, sym);
            RefreshOverviewProjects(country, sym);
            RefreshDebtFinancing(country, sym);
        }

        private void RefreshDebtFinancing(CountryDataSO country, string sym)
        {
            if (_root == null) return;
            
            float debtDom = country.domesticDebt * country.exchangeRate;
            float debtFor = country.foreignDebt * country.exchangeRate;
            float totalDebt = debtDom + debtFor;

            if (_root.Q<Label>("ValDomesticDebt") is Label lDom) lDom.text = UiFormat.Money(debtDom, sym);
            if (_root.Q<Label>("ValForeignDebt") is Label lFor) lFor.text = UiFormat.Money(debtFor, sym);
            
            if (_root.Q<ProgressBar>("DebtRatioBar") is ProgressBar pBar) {
                pBar.value = totalDebt > 0 ? (debtDom / totalDebt) * 100f : 0f;
            }

            if (_root.Q<Label>("LabelDebtService") is Label lDebtService) lDebtService.text = $"{country.debtServiceRatio:F0}%";
            if (_root.Q<Slider>("SliderDebtService") is Slider sDebtService) sDebtService.value = country.debtServiceRatio;
            
            float monthlyCost = totalDebt * 0.05f / 12f * (country.debtServiceRatio / 100f);
            if (_root.Q<Label>("LblDebtServiceCost") is Label lCost) {
                lCost.text = $"Monthly payment: {UiFormat.Money(monthlyCost, sym)} (interest + principal)";
            }
        }

        private void RefreshOverviewReports(CountryDataSO country, GameManager gm)
        {
            if (_econReportsPanel == null) return;

            while (_econReportsPanel.childCount > 1)
                _econReportsPanel.RemoveAt(1);

            var feed = DashboardLiveContent.BuildEconomyFeed(country, gm);
            int max = Mathf.Min(feed.Count, 4);

            for (int i = 0; i < max; i++)
            {
                var line = feed[i];

                string rowClass = ColorToReportClass(line.Color);

                string icon = rowClass == "alert"  ? FaIcons.Fire
                            : rowClass == "good"   ? FaIcons.ArrowTrendUp
                            : rowClass == "market" ? FaIcons.Globe
                            :                       FaIcons.Scroll;

                string iconColor = rowClass == "alert"  ? "fa-red"
                                 : rowClass == "good"   ? "fa-green"
                                 : rowClass == "market" ? "fa-cyan"
                                 :                       "fa-gold";

                string catClass = rowClass == "alert" ? "negative" : (rowClass == "good" ? "positive" : (rowClass == "market" ? "market" : "warning"));

                string catLabel = rowClass == "alert" ? "ECONOMIC ALERT"
                                : rowClass == "good"  ? "POSITIVE DEVELOPMENT"
                                : rowClass == "market"? "GLOBAL MARKET UPDATE"
                                :                       "POLICY UPDATE";

                string stamp = DashboardLiveContent.TurnStamp(gm);

                var row = new VisualElement();
                row.AddToClassList("econ-report");
                row.AddToClassList(rowClass);

                var iconLbl = new Label(icon);
                iconLbl.AddToClassList("econ-report-icon");
                iconLbl.AddToClassList("fa-icon");
                iconLbl.AddToClassList(iconColor);
                row.Add(iconLbl);

                var body = new VisualElement();
                body.AddToClassList("econ-report-body");

                var catLbl = new Label(catLabel);
                catLbl.AddToClassList(catClass);
                body.Add(catLbl);

                var titleLbl = new Label(line.Tag);
                titleLbl.AddToClassList("econ-report-title");
                body.Add(titleLbl);

                var copyLbl = new Label(line.Message);
                copyLbl.AddToClassList("econ-report-copy");
                body.Add(copyLbl);

                row.Add(body);

                var timeLbl = new Label(stamp);
                timeLbl.AddToClassList("econ-muted");
                row.Add(timeLbl);

                _econReportsPanel.Add(row);
            }
        }

        private static string ColorToReportClass(Color c)
        {
            if (c.r < 0.2f && c.g > 0.7f) return "good";
            if (c.r > 0.7f && c.g < 0.5f) return "alert";
            if (c.r > 0.7f && c.g > 0.6f) return "warning";
            return "market";
        }

        private void RefreshOverviewSectors(CountryDataSO country, string sym)
        {
            if (_econSectorsGrid == null) return;

            _econSectorsGrid.Clear();

            float totalRev = 0f;
            foreach (var ind in EconomyTabContent.Industries)
                totalRev += ind.GetRevenueB(country);

            var topSectors = new System.Collections.Generic.List<EconomyTabContent.IndustryDef>(EconomyTabContent.Industries);
            topSectors.Sort((a, b) => b.GetRevenueB(country).CompareTo(a.GetRevenueB(country)));
            int maxSectors = UnityEngine.Mathf.Min(topSectors.Count, 4);

            for (int i = 0; i < maxSectors; i++)
            {
                var ind = topSectors[i];
                float rev = ind.GetRevenueB(country);
                float contrib = totalRev > 0 ? (rev / totalRev) * 100f : 0f;
                float workforce = ind.GetWorkforce(country);
                bool bullish = ind.IsBullish(country);
                string workforceStr = workforce >= 1_000_000f
                    ? $"{workforce / 1_000_000f:F1}M"
                    : workforce >= 1000f
                        ? $"{workforce / 1000f:F1}k"
                        : $"{workforce:F0}";

                int origIdx = System.Array.IndexOf(EconomyTabContent.Industries, ind);
                string faIcon = (origIdx >= 0 && origIdx < FaIcons.SectorIcons.Length)
                    ? FaIcons.SectorIcons[origIdx]
                    : FaIcons.BullsEye;
                string safeTitle = ind.Title.Replace(" & MINERALS", "").Replace(" & FINTECH","").Replace(" & GAS", "").ToUpper();

                var card = new VisualElement();
                card.AddToClassList("econ-sector-card");
                if (i % 2 == 1) card.style.marginRight = 0;

                var titleRow = new VisualElement();
                titleRow.style.flexDirection = FlexDirection.Row;
                titleRow.style.alignItems = Align.Center;
                titleRow.style.marginBottom = 6;

                var faLbl = new Label(faIcon);
                faLbl.AddToClassList("fa-icon");
                faLbl.AddToClassList("fa-cyan");
                faLbl.AddToClassList("fa-sm");
                faLbl.style.marginRight = 5;
                titleRow.Add(faLbl);

                var titleLbl = new Label(safeTitle);
                titleLbl.AddToClassList("econ-sector-title");
                titleLbl.style.marginBottom = 0;
                titleRow.Add(titleLbl);
                card.Add(titleRow);

                float mockGrowth = bullish ? 5.2f : -1.4f;
                var contribLbl = new Label($"Contribution  {contrib:F0}%");
                contribLbl.style.marginTop = 2;
                card.Add(contribLbl);

                var trendIcon = new Label(bullish ? FaIcons.TrendUp : FaIcons.TrendDown);
                trendIcon.AddToClassList("fa-icon");
                trendIcon.AddToClassList("fa-sm");
                trendIcon.AddToClassList(bullish ? "fa-green" : "fa-red");
                trendIcon.style.marginRight = 4;

                var growthRow = new VisualElement();
                growthRow.style.flexDirection = FlexDirection.Row;
                growthRow.style.alignItems = Align.Center;
                growthRow.Add(trendIcon);
                var growthLbl = new Label(bullish ? $"+{mockGrowth}%" : $"{mockGrowth}%");
                growthLbl.AddToClassList(bullish ? "positive" : "negative");
                growthLbl.style.fontSize = 11;
                growthRow.Add(growthLbl);
                card.Add(growthRow);

                var empLbl = new Label($"Emp: {workforceStr}");
                empLbl.AddToClassList("econ-muted");
                card.Add(empLbl);

                _econSectorsGrid.Add(card);
            }
        }

        private void RefreshOverviewProjects(CountryDataSO country, string sym)
        {
            if (_overviewProjectsPanel == null) return;

            while (_overviewProjectsPanel.childCount > 1)
                _overviewProjectsPanel.RemoveAt(1);

            var active = new System.Collections.Generic.List<ActiveProject>();
            foreach (var p in country.activeProjects)
            {
                if (p.stage == ProjectStage.UnderConstruction ||
                    p.stage == ProjectStage.Funded ||
                    p.stage == ProjectStage.Planned)
                {
                    active.Add(p);
                    if (active.Count >= 3) break;
                }
            }

            if (active.Count == 0)
            {
                _overviewProjectsPanel.style.display = DisplayStyle.None;
                return;
            }
            _overviewProjectsPanel.style.display = DisplayStyle.Flex;

            foreach (var p in active)
            {
                float pct = p.totalMonths > 0
                    ? (p.totalMonths - p.monthsRemaining) / (float)p.totalMonths * 100f
                    : 0f;
                float monthlyCost = p.totalMonths > 0 ? p.totalCost / p.totalMonths : p.totalCost;

                string barClass = pct < 40f ? "econ-progress warning-bar" : "econ-progress";

                string thumbClass = p.projectType?.ToLower().Contains("tech") == true ? "tech"
                                  : p.projectType?.ToLower().Contains("rail") == true ? "rail"
                                  : "";

                var row = new VisualElement();
                row.AddToClassList("econ-project-row");

                var thumb = new VisualElement();
                thumb.AddToClassList("econ-project-thumb");
                if (!string.IsNullOrEmpty(thumbClass)) thumb.AddToClassList(thumbClass);
                row.Add(thumb);

                var info = new VisualElement();
                info.AddToClassList("econ-project-info");

                var nameLbl = new Label(p.projectName);
                nameLbl.AddToClassList("econ-report-title");
                info.Add(nameLbl);

                string typeDisplay = p.projectType;
                if (!string.IsNullOrEmpty(typeDisplay)) {
                    if (typeDisplay.ToLower().Contains("infra")) typeDisplay = "Infrastructure";
                    if (typeDisplay.ToLower().Contains("tech")) typeDisplay = "Technology";
                }

                var typeLbl = new Label(typeDisplay ?? "Project");
                typeLbl.AddToClassList("econ-muted");
                info.Add(typeLbl);

                var bar = new ProgressBar { value = pct, highValue = 100 };
                bar.AddToClassList("econ-progress");
                if (pct < 40f) bar.AddToClassList("warning-bar");
                info.Add(bar);

                row.Add(info);

                var costLbl = new Label($"{UiFormat.Money(monthlyCost, sym)}\n{p.monthsRemaining} mo left");
                costLbl.style.color = UnityEngine.Color.white;
                costLbl.style.unityTextAlign = UnityEngine.TextAnchor.MiddleRight;
                row.Add(costLbl);

                _overviewProjectsPanel.Add(row);
            }
        }
    }
}
