using UnityEngine;
using UnityEngine.UIElements;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using StateCraft.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace StateCraft.UI
{
    [DefaultExecutionOrder(-10)]
    public partial class EconomyDashboardController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _dashboardRoot;

        // Tabs
        private Button _tabDashboard, _tabProduction, _tabIndustry, _tabInfrastructure, _tabConstruction;
        private VisualElement _panelDashboard, _panelProduction, _panelIndustry, _panelInfrastructure, _panelConstruction;
        private List<VisualElement> _allPanels = new List<VisualElement>();


        // Header
        private Label _headerCountryName, _headerCreditRating, _headerIncome, _headerExpense;
        private VisualElement _headerFlag;
        private Label _overviewAdviserName, _overviewAdviserTitle, _overviewAdviserBody;
        private Label _overviewAdviserBenefit1, _overviewAdviserBenefit2, _overviewAdviserBenefit3;
        private Label _overviewAdviserRisk1, _overviewAdviserRisk2, _overviewAdviserRisk3;
        private Button _overviewApproveButton, _overviewDeclineButton;
        private VisualElement _overviewAdviserImpactGrid, _overviewAdviserActionRow;

        // Dashboard Metrics - Financial
        private Label _valTreasury, _valGDP, _valGDPGrowth, _valInflation, _valExchange, _valBudgetBalance;
        private ProgressBar _debtRatioBar;
        
        // Policy Controls
        private Slider _sliderTaxRate, _sliderSubsidyLevel;
        private Label _labelTaxRate, _labelSubsidyLevel;
        
        // New Policy Controls
        private Slider _sliderInterestRate, _sliderTariffRate, _sliderDebtService;
        private Label _labelInterestRate, _labelTariffRate, _labelDebtService;
        private Label _lblDebtServiceCost;
        private Button _btnBorrowDebt, _btnRepayDebt;
        private Slider _sliderDebtTransaction;
        private Label _labelDebtTransaction;

        // Tax Simulator State
        private string _simSelectedTaxKey = "CORPORATE"; // which row's preview shows in the Tax Impact Simulator panel
        // Each tax row's own pending (not-yet-applied) rate, independent of the others.
        // Populated lazily — a row that hasn't been touched falls back to the country's actual rate.
        private readonly Dictionary<string, float> _taxDraftRates = new Dictionary<string, float>();
        private bool   _simButtonsWired   = false;

        // Debt display
        private Label _valTotalDebt, _valDomesticDebt, _valForeignDebt;


        // Dashboard Metrics - Social
        private Label _valPopulation, _valPoverty, _valUnemployment, _valCorruption, _valApproval;
        private VisualElement _statusContainer;

        // Ledger
        private VisualElement _incomeList, _expenseList;
        private Label _valNetBalance;

        // Live content
        private Label _economyBriefing;
        private VisualElement _economyLiveFeed;

        // Sub-tab panels
        private VisualElement _productionSummary, _productionCardsContainer, _resourceDepositsList;
        private VisualElement _industryAlertsContainer;

        // Industries tab — same caching pattern as the Taxation tab fix: resolved once
        // (with a null-retry on first refresh, in case the panel was display:none at Awake)
        // instead of being re-queried from _root every single refresh.
        private Label _lblIndOverviewOutput, _lblIndOverviewGDPPct, _lblIndOverviewMeter;
        private ProgressBar _pbIndOverview;
        private Label _lblIndTopName, _lblIndTopRevenue, _lblIndTopGrowth;
        private Label _lblIndWeakName, _lblIndWeakRevenue;
        private Label _lblIndAdviserName, _lblIndAdviserRec, _lblIndAdviserBody;

        private class IndustryRowRefs
        {
            public Label Tier, Stat1, Stat2, Stat3, Stat4, Growth, Outlook, Pressure, Benefits, Ring;
            public Button Btn;
        }
        private readonly Dictionary<string, IndustryRowRefs> _industryRowRefs = new Dictionary<string, IndustryRowRefs>();
        private readonly HashSet<string> _industryIdsLogged = new HashSet<string>();
        private VisualElement _infraSummary, _infraCardsContainer;
        private Label _infraSynergyText;
        private VisualElement _constructionSummary, _constructionCardsContainer, _blueprintCatalog;
        private Label _blueprintSectionTitle;
        private bool _blueprintsVisible;

        // Overview tab live panels
        private VisualElement _econReportsPanel;
        private VisualElement _econSectorsGrid;
        private VisualElement _overviewProjectsPanel;

        // Shared action modal
        private VisualElement _actionModal, _actionModalImpact;
        private Label _actionModalTitle, _actionModalBody;
        private Button _actionModalConfirm, _actionModalCancel, _actionModalClose;
        private System.Action _pendingModalAction;

        // Filter state for Projects tab
        private static readonly string[] _filterCycle = { "ALL", "INFRASTRUCTURE", "ENERGY", "AGRICULTURE", "TECH", "DEFENCE" };
        private int _filterIndex = 0;

        // Draft tax values (staged before APPLY CHANGE)
        private float _draftIncomeTax = -1f;
        private float _draftCorporateTax = -1f;
        private float _draftVAT = -1f;
        private float _draftExportDuty = -1f;

        // Budget share draft (staged before APPROVE BUDGET)
        private Dictionary<string, float> _draftBudgets = new Dictionary<string, float>();
        private VisualElement _budgetRowsContainer;

        // Cached tax-row widget references, keyed by tax key (INCOME/CORPORATE/VAT/IMPORT/EXPORT).
        // Resolved once in WireTaxRow() instead of being re-queried from _root on every refresh —
        // mirrors the existing _budgetRowsContainer caching pattern above.
        private readonly Dictionary<string, (Label rateLabel, Slider bar, Label revenueLabel, Label baseLabel, Label shareLabel)> _taxRowRefs
            = new Dictionary<string, (Label rateLabel, Slider bar, Label revenueLabel, Label baseLabel, Label shareLabel)>();

        // Taxation tab summary / compliance / simulator labels — cached once in
        // SetupTaxSimulatorInteractiveButtons instead of re-queried from _root on every refresh.
        private Label _lblTaxTotalRevenue, _lblTaxTotalRatio, _lblTaxTotalMoM;
        private Label _lblTaxComplianceRate, _lblTaxCollectionEff, _lblTaxGap, _lblTaxArrears;
        private Label _lblComplianceDonut, _lblComplianceCompliant, _lblCompliancePartial, _lblComplianceNonCompliant;
        private ComplianceDonutElement _complianceDonut;
        private Label _lblSimSelectedTax, _lblSimCurrentRate, _lblSimNewRate, _lblSimRevenue, _lblSimGDPImpact, _lblSimApproval, _lblTaxSimTotal, _lblTaxSimAdvisory;
        private Label _lblSimConfidence, _lblSimInvestment, _lblSimEmployment, _lblSimInflation;
        private VisualElement _taxPoliciesContainer;
        private bool _budgetRowsBuilt = false;

        // Current top adviser recommendation for this session
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
                _dashboardRoot = dashboard;
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
            // Guard: _root may not yet be assigned if OnEnable fires before Awake completes
            if (_root != null) RefreshDisplay();
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
            // Tabs
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
            _budgetRowsContainer = root.Q<VisualElement>("BudgetRowsContainer");
            Debug.Log($"[Budget Init] BudgetRowsContainer={((_budgetRowsContainer != null) ? "FOUND" : "NULL")}");

            _allPanels.Clear();
            _allPanels.AddRange(new[] { _panelDashboard, _panelProduction, _panelIndustry, _panelInfrastructure, _panelConstruction });

            _tabDashboard.clicked += () => SwitchTab(0);
            _tabProduction.clicked += () => SwitchTab(1);
            _tabIndustry.clicked += () => SwitchTab(2);
            _tabInfrastructure.clicked += () => SwitchTab(3);
            _tabConstruction.clicked += () => SwitchTab(4);


            // Header
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

            // Financial
            _valTreasury = root.Q<Label>("ValTreasury");
            _valGDP = root.Q<Label>("ValGDP");
            _valGDPGrowth = root.Q<Label>("ValGDPGrowth");
            _valInflation = root.Q<Label>("ValInflation");
            _valExchange = root.Q<Label>("ValExchange");
            _valBudgetBalance = root.Q<Label>("ValBudgetBalance");
            _debtRatioBar = root.Q<ProgressBar>("DebtRatioBar");

            // Policy Controls
            _sliderTaxRate = root.Q<Slider>("SliderTaxRate");
            _labelTaxRate = root.Q<Label>("LabelTaxRate");
            _sliderSubsidyLevel = root.Q<Slider>("SliderSubsidyLevel");
            _labelSubsidyLevel = root.Q<Label>("LabelSubsidyLevel");

            if (_sliderTaxRate != null) _sliderTaxRate.RegisterValueChangedCallback(evt => UpdateTaxRate(evt.newValue));
            if (_sliderSubsidyLevel != null) _sliderSubsidyLevel.RegisterValueChangedCallback(evt => UpdateSubsidyLevel(evt.newValue));

            // New Policy Controls
            _sliderInterestRate = root.Q<Slider>("SliderInterestRate");
            _labelInterestRate = root.Q<Label>("LabelInterestRate");
            _sliderTariffRate = root.Q<Slider>("SliderTariffRate");
            _labelTariffRate = root.Q<Label>("LabelTariffRate");
            _sliderDebtService = root.Q<Slider>("SliderDebtService");
            _labelDebtService = root.Q<Label>("LabelDebtService");
            _lblDebtServiceCost = root.Q<Label>("LblDebtServiceCost");
            _btnBorrowDebt = root.Q<Button>("BtnBorrowDebt");
            _btnRepayDebt = root.Q<Button>("BtnRepayDebt");

            _sliderDebtTransaction = root.Q<Slider>("SliderDebtTransaction");
            _labelDebtTransaction = root.Q<Label>("LabelDebtTransaction");

            if (_sliderDebtTransaction != null)
            {
                _sliderDebtTransaction.RegisterValueChangedCallback(evt =>
                {
                    if (_labelDebtTransaction != null)
                    {
                        var country = GameManager.Instance?.playerCountry;
                        string sym = country != null && !string.IsNullOrEmpty(country.currencySymbol) ? country.currencySymbol : "₦";
                        _labelDebtTransaction.text = $"{sym}{evt.newValue:F0}B";
                    }
                    RefreshDisplay();
                });
            }

            // Debt breakdown labels
            _valTotalDebt = root.Q<Label>("ValTotalDebt");
            _valDomesticDebt = root.Q<Label>("ValDomesticDebt");
            _valForeignDebt = root.Q<Label>("ValForeignDebt");

            if (_sliderInterestRate != null) _sliderInterestRate.RegisterValueChangedCallback(evt => UpdateInterestRate(evt.newValue));
            if (_sliderTariffRate != null) _sliderTariffRate.RegisterValueChangedCallback(evt => UpdateTariffRate(evt.newValue));
            if (_sliderDebtService != null) _sliderDebtService.RegisterValueChangedCallback(evt => UpdateDebtService(evt.newValue));
            if (_btnBorrowDebt != null) _btnBorrowDebt.clicked += HandleBorrowDebt;
            if (_btnRepayDebt != null) _btnRepayDebt.clicked += HandleRepayDebt;

            // Tax Simulator Interactivity
            SetupTaxSimulatorInteractiveButtons(root);

            // Social
            _valApproval = root.Q<Label>("ValApproval");
            _statusContainer = root.Q<VisualElement>("StatusIndicators");
            _economyBriefing = root.Q<Label>("EconomyBriefing");
            _economyLiveFeed = root.Q<ScrollView>("EconomyLiveFeed");

            // Ledger
            _incomeList = root.Q<VisualElement>("IncomeList");
            _expenseList = root.Q<VisualElement>("ExpenseList");
            _valNetBalance = root.Q<Label>("ValNetBalance");

            _productionSummary = root.Q<VisualElement>("ProductionSummary");
            _productionCardsContainer = root.Q<VisualElement>("ProductionCardsContainer");
            _resourceDepositsList = root.Q<VisualElement>("ResourceDepositsList");
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

            // Overview live panels
            _econReportsPanel    = root.Q<VisualElement>("EconReportsPanel");
            _econSectorsGrid     = root.Q<VisualElement>("EconSectorsGrid");
            _overviewProjectsPanel = root.Q<VisualElement>("OverviewProjectsPanel");

            // "VIEW ALL SECTORS ›" — jumps to the Industries tab
            var btnViewSectors = root.Q<Label>("BtnViewAllSectors");
            if (btnViewSectors != null)
            {
                btnViewSectors.RegisterCallback<ClickEvent>(_ => SwitchTab(2));
                btnViewSectors.RegisterCallback<MouseEnterEvent>(_ =>
                    btnViewSectors.style.color = new StyleColor(new UnityEngine.Color(1f, 1f, 1f)));
                btnViewSectors.RegisterCallback<MouseLeaveEvent>(_ =>
                    btnViewSectors.style.color = new StyleColor(new UnityEngine.Color(25/255f, 226/255f, 255/255f)));
            }

            // "VIEW ALL PROJECTS ›" — jumps to the Projects tab
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
                    name == "EconomyModalClose" ||
                    name == "BudgetStepBtn" ||
                    btn.ClassListContains("tax-step-btn") ||
                    btn.ClassListContains("tax-apply-btn") ||
                    btn.ClassListContains("tax-reset-btn"))
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

        // Routes +/- clicks to the correct tax draft field or budget share field
        // by scanning ancestor label text.


        private void HandleEconomyAction(Button button)
        {
            HandleEconomyAction(button, button?.text ?? "");
        }

        private void HandleEconomyAction(Button button, string actionText)
        {
            if (string.IsNullOrEmpty(actionText)) return;

            string action = actionText.ToUpperInvariant();
            var country = GameManager.Instance?.playerCountry;

            // ── Close project side-panel ────────────────────────────────────
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

            // ── FILTER (cycles project type) ────────────────────────────────
            if (action.Contains("FILTER"))
            {
                _filterIndex = (_filterIndex + 1) % _filterCycle.Length;
                string current = _filterCycle[_filterIndex];
                NotifyEconomy($"Showing projects: {current}", Color.cyan);
                // Re-populate the projects section with the new filter
                if (country != null) PopulateConstruction(country,
                    string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol);
                return;
            }

            // ── APPLY CHANGE — commit draft tax values ──────────────────────
            if (action.Contains("APPLY CHANGE"))
            {
                if (country == null) return;
                bool anyDraft = _draftIncomeTax    >= 0f || _draftCorporateTax >= 0f ||
                                _draftVAT          >= 0f || _draftExportDuty   >= 0f;
                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;

                // Build impact preview
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

            // ── RESET ───────────────────────────────────────────────────────
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

            // ── APPROVE BUDGET ──────────────────────────────────────────────
            if (action.Contains("APPROVE BUDGET"))
            {
                if (country == null) return;
                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
                // Build summary of what will be committed
                float GetAlloc(string id) => _draftBudgets.ContainsKey(id) ? _draftBudgets[id] : (country.budgetAllocations.FirstOrDefault(b => b.id == id)?.sharePercentage ?? 0f);
                float mil  = GetAlloc("MILITARY");
                float edu  = GetAlloc("EDUCATION");
                float hlt  = GetAlloc("HEALTH");
                float inf2 = GetAlloc("INFRASTRUCTURE");
                string impact = $"Military {mil:F0}% · Education {edu:F0}% · Health {hlt:F0}% · Infrastructure {inf2:F0}%";
                ShowEconomyModal("Approve Budget",
                    "Commit the current budget allocation to national accounts?",
                    impact,
                    "APPROVE",
                    () => ApproveBudgetPlan());
                return;
            }

            // ── SAVE AS DRAFT ───────────────────────────────────────────────
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

            // ── AUTO-ALLOCATE ───────────────────────────────────────────────
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

            // ── INVEST NOW ──────────────────────────────────────────────────
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

            // ── UPGRADE TO TIER ─────────────────────────────────────────────
            if (action.Contains("UPGRADE TO TIER") || (button.name != null && button.name.StartsWith("Industry") && button.name.EndsWith("_Btn")))
            {
                string industryId = button.name != null && button.name.StartsWith("Industry") ? button.name.Replace("Industry", "").Replace("_Btn", "") : InferIndustryId(button);
                var gm = GameManager.Instance;
                var db = gm?.EconomyDatabase;
                var ind = db?.industries.Find(i => i.id == industryId);
                float cost = ind != null && country != null ? StateCraft.Systems.EconomyLogicEvaluator.GetIndustryUpgradeCost(country, ind) : 0f;
                string sym = country != null && !string.IsNullOrEmpty(country.currencySymbol) ? country.currencySymbol : "₦";

                ShowEconomyModal(
                    "Upgrade Industry",
                    $"Upgrade {ind?.title ?? "Sector"} to next tier?",
                    $"Treasury funds of {UiFormat.Money(cost, sym)} will be deducted. Sector tier, workforce, and output projections will update.",
                    "UPGRADE",
                    () => UpgradeIndustryById(industryId));
                return;
            }

            // ── COMMISSION blueprint ─────────────────────────────────────────
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

            // ── TAX REFORMS HISTORY ─────────────────────────────────────────
            if (action.Contains("TAX REFORMS HISTORY") || action.Contains("REFORMS HISTORY"))
            {
                string log = "No reforms logged yet.";
                if (country?.taxReformLog != null && country.taxReformLog.Count > 0)
                    log = string.Join("\n", country.taxReformLog.GetRange(0, Mathf.Min(10, country.taxReformLog.Count)));
                ShowEconomyModal("Tax Reforms History", log, "Last 10 recorded tax policy changes.", "OK", null);
                return;
            }

            // ── VIEW ALL POLICIES ────────────────────────────────────────────
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

            // ── VIEW ALL ALERTS ──────────────────────────────────────────────
            if (action.Contains("VIEW ALL ALERTS"))
            {
                ShowEconomyModal("Economic Alerts", BuildAlertSummary(country),
                    "Live condition scan — alerts cleared when conditions improve.", "OK", null);
                return;
            }

            // ── VIEW FULL REPORT / DETAILED REPORT ──────────────────────────
            if (action.Contains("VIEW FULL REPORT") || action.Contains("VIEW DETAILED REPORT"))
            {
                ShowEconomyModal("Economy Report", BuildFullReport(country),
                    "Generated from current national accounts and projections.", "OK", null);
                return;
            }

            // ── VIEW ADVISER NOTES ───────────────────────────────────────────
            if (action.Contains("VIEW ADVISER NOTES"))
            {
                ShowEconomyModal("Adviser Notes", BuildAdviserNotes(country),
                    "Chief Economic Adviser recommendations from current state analysis.", "OK", null);
                return;
            }

            // ── VIEW DETAILS (project cards) ─────────────────────────────────
            if (action.Contains("VIEW DETAILS"))
            {
                string details = BuildProjectCardDetail(button, country);
                ShowEconomyModal("Project Details", details,
                    "Live data from the national construction pipeline.", "OK", null);
                return;
            }

            // ── VIEW (generic fallback) ──────────────────────────────────────
            if (action.Contains("VIEW"))
            {
                ShowEconomyModal("Report", actionText.Replace("\n", " "),
                    "Detailed data screens are generated from national accounts.", "OK", null);
                return;
            }

            // ── APPROVE POLICY ───────────────────────────────────────────────
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

            // ── DECLINE ──────────────────────────────────────────────────────
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

        // ── Commit tax drafts ────────────────────────────────────────────────


        // ── Save budget draft ────────────────────────────────────────────────








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

            if (_sliderSubsidyLevel != null) _sliderSubsidyLevel.value = country.subsidyLevel;
            if (_sliderInterestRate != null) _sliderInterestRate.value = country.centralBankInterestRate;
            if (_sliderDebtService  != null) _sliderDebtService.value  = country.debtServiceRatio;
        }

        // ── Report builder helpers ───────────────────────────────────────────

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

            // Top risk
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

            // Try to match to an active project by name
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

            // Static UXML card detail — pull from ancestor text
            string cardTitle = ctx.Length > 120 ? ctx.Substring(0, 120) : ctx;
            return $"Selected project detail:\n{cardTitle}\n(Live project data will populate once commissioned via Blueprint Library.)";
        }

        private void NotifyEconomy(string message, Color color)
        {
            NotificationManager.Instance?.AddNotification(message, color);
            Debug.Log($"[Economy UI] {message}");
        }

        private static UnityEngine.Color HexToColor(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
                return new UnityEngine.Color(r / 255f, g / 255f, b / 255f);
            }
            return UnityEngine.Color.white;
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
            else if (index == 2)
            {
                _tabIndustry.AddToClassList("active-tab");
                var country = GameManager.Instance?.playerCountry;
                if (country != null)
                {
                    string sym = string.IsNullOrEmpty(country.currencySymbol) ? "\u20a6" : country.currencySymbol;
                    RefreshIndustryTab(country, sym);
                }
            }
            else if (index == 3)
            {
                _tabInfrastructure.AddToClassList("active-tab");
                var country = GameManager.Instance?.playerCountry;
                if (country != null)
                {
                    string sym = string.IsNullOrEmpty(country.currencySymbol) ? "\u20a6" : country.currencySymbol;
                    if (!_budgetRowsBuilt) BuildBudgetRows(country, sym);
                    RefreshBudgetTab(country, sym);
                }
            }
            else if (index == 4)
            {
                _tabConstruction.AddToClassList("active-tab");
                // Panel is now Flex — safe to resolve hidden elements and refresh
                var country4 = GameManager.Instance?.playerCountry;
                if (country4 != null)
                {
                    string sym4 = string.IsNullOrEmpty(country4.currencySymbol) ? "\u20a6" : country4.currencySymbol;
                    InitConstructionTab();          // re-resolve _liveBlueprintGrid now the panel is visible
                    RefreshProjectsTab(country4, sym4);
                }
            }
        }










        public void Show()
        {
            var panel = _dashboardRoot;
            if (panel != null)
            {
                panel.style.display = DisplayStyle.Flex;
                UIStateManager.Instance?.ApplySafeTransition(panel, true, 0.4f);
                RefreshDisplay();
                UIStateManager.FixScrollViews(panel); // Fix scrollbars in builds after UI refresh
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

            // Keep treasury in sync with HUD (playerBudget is authoritative, in billions)
            country.treasury = gm.playerBudget;

            // Header
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

            // Header date box
            if (_dashboardRoot?.Q<Label>("DateMain") is Label lDateMain)
                lDateMain.text = gm?.TurnSystem != null ? gm.TurnSystem.CurrentDate : "—";
            if (_dashboardRoot?.Q<Label>("DateSub") is Label lDateSub)
                lDateSub.text = System.DateTime.Now.ToString("dddd, HH:mm");

            // KPI strip: MoM deltas & debt ratio
            if (_dashboardRoot?.Q<Label>("ValInflationDelta") is Label lInfD)
            {
                float dInf = country.inflationRate - country.prevInflationRate;
                lInfD.text = $"{(dInf >= 0 ? "+" : "")}{dInf:P1} MoM";
                lInfD.RemoveFromClassList("positive"); lInfD.RemoveFromClassList("negative");
                lInfD.AddToClassList(dInf > 0.005f ? "negative" : "positive");
            }
            if (_dashboardRoot?.Q<Label>("ValDebtRatio") is Label lDR)
            {
                float dRat = country.gdp > 0 ? country.debt / country.gdp : 0f;
                lDR.text = $"{dRat:P1} of GDP";
                lDR.RemoveFromClassList("warning"); lDR.RemoveFromClassList("negative"); lDR.RemoveFromClassList("positive");
                lDR.AddToClassList(dRat > 0.6f ? "negative" : dRat > 0.4f ? "warning" : "positive");
            }
            if (_dashboardRoot?.Q<Label>("ValApprovalDelta") is Label lAppD)
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
            
            // Reconstruct the exact net change as the GameManager calculates it (with interest)
            float domesticInterestLocal =  gm.DebtSystem != null ? (gm.DebtSystem.CalculateDomesticInterest(country) * country.exchangeRate) : 0f;
            float foreignInterestUSD = gm.DebtSystem != null ? gm.DebtSystem.CalculateForeignInterest(country) : 0f;
            float serviceMultiplier = country.debtServiceRatio / 100f;
            float actualDomesticPaidLocal = domesticInterestLocal * serviceMultiplier;
            float actualForeignPaidUSD = foreignInterestUSD * serviceMultiplier;
            float foreignInterestLocal = actualForeignPaidUSD * country.exchangeRate;
            
            // Active Operations Maintenance
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

            // Bring in Trade Deal / Trade Agreement net changes so they reflect in the summary
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

            if (_sliderSubsidyLevel != null) _sliderSubsidyLevel.SetValueWithoutNotify(country.subsidyLevel);
            if (_labelSubsidyLevel != null) _labelSubsidyLevel.text = $"{country.subsidyLevel:F0}%";

            // Seed tax & tariff sliders from live country data
            if (_sliderTaxRate != null) _sliderTaxRate.SetValueWithoutNotify(country.corporateTaxRate);
            if (_labelTaxRate != null) _labelTaxRate.text = $"{country.corporateTaxRate:F1}%";
            if (_sliderTariffRate != null) _sliderTariffRate.SetValueWithoutNotify(country.importTariffRate);
            if (_labelTariffRate != null) _labelTariffRate.text = $"{country.importTariffRate:F1}%";

            // Refresh new sliders
            if (_sliderInterestRate != null) _sliderInterestRate.SetValueWithoutNotify(country.centralBankInterestRate);
            if (_labelInterestRate != null) _labelInterestRate.text = $"{country.centralBankInterestRate:F1}%";
            if (_sliderDebtService != null) _sliderDebtService.SetValueWithoutNotify(country.debtServiceRatio);
            if (_labelDebtService != null) _labelDebtService.text = $"{country.debtServiceRatio:F0}%";

            // Debt breakdown display
            string debtSym = sym;
            float totalDebtLocal = (country.debt * country.exchangeRate);
            float domDebtLocal  = (country.domesticDebt  * country.exchangeRate);
            float forDebtLocal  = (country.foreignDebt   * country.exchangeRate);
            if (_valTotalDebt    != null) _valTotalDebt.text    = UiFormat.Money(totalDebtLocal, debtSym);
            if (_valDomesticDebt != null) _valDomesticDebt.text = UiFormat.Money(domDebtLocal,  debtSym);
            if (_valForeignDebt  != null) _valForeignDebt.text  = UiFormat.Money(forDebtLocal,  debtSym);
            RefreshDebtServiceCostLabel(country);

            // Update Borrow/Repay button texts based on currency symbol
            float txAmt = _sliderDebtTransaction != null ? _sliderDebtTransaction.value : 50f;
            
            if (_btnBorrowDebt != null) _btnBorrowDebt.text = $"BORROW {sym}{txAmt:F0}B";
            if (_btnRepayDebt != null)
            {
                _btnRepayDebt.text = $"REPAY {sym}{txAmt:F0}B";
                // Only enable repayment if player has money and has debt to repay
                _btnRepayDebt.SetEnabled(gm.playerBudget >= txAmt && country.debt > 0f);
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


















    


        // Compliance donut is drawn via ComplianceDonutElement.cs


        // ── Tax Simulator Helpers ─────────────────────────────────────────────



        private float GetSimTaxRate(CountryDataSO c, string key) => key switch {
            "INCOME"    => c.incomeTaxRate,
            "CORPORATE" => c.corporateTaxRate,
            "VAT"       => c.vatRate,
            "IMPORT"    => c.importTariffRate,
            "EXPORT"    => c.exportDutyRate,
            _           => c.corporateTaxRate
        };

        // Returns this row's own pending draft rate, seeding it from the actual country
        // rate the first time it's accessed. Each key is tracked independently, so editing
        // one tax row never touches another row's value.










        // ── Overview Tab live data ───────────────────────────────────────────
    }
}
