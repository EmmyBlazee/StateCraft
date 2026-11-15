using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using StateCraft.Interfaces;

namespace StateCraft.UI
{
    public partial class MilitaryDashboardController
    {
        private void PopulateProjects(CountryDataSO country)
        {
            if (_activeProjectsList == null || _availableProjectsList == null) return;
            _activeProjectsList.Clear();
            _availableProjectsList.Clear();

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;

            if (country.militaryProjects == null || country.militaryProjects.Count == 0)
            {
                var emptyLabel = new Label("No active projects. Commission a directive below.");
                emptyLabel.style.color = Color.grey;
                emptyLabel.style.fontSize = 10;
                emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                emptyLabel.style.marginTop = 20;
                _activeProjectsList.Add(emptyLabel);
            }
            else
            {
                foreach (var proj in country.militaryProjects)
                {
                    var card = new VisualElement();
                    card.AddToClassList("sub-panel");
                    card.style.marginBottom = 12;
                    card.style.paddingTop = 14;
                    card.style.paddingBottom = 14;
                    card.style.paddingLeft = 16;
                    card.style.paddingRight = 16;

                    Color branchColor = proj.branch switch
                    {
                        "Army"     => new Color(255/255f, 152/255f, 0),
                        "Navy"     => new Color(0.1f, 0.5f, 0.9f),
                        "AirForce" => new Color(156/255f, 39/255f, 176/255f),
                        "RDF"      => new Color(255/255f, 82/255f, 82/255f),
                        _          => Color.grey
                    };

                    var headerRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 8 } };
                    var nameLabel = new Label(proj.projectName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } };
                    var branchBadge = new Label(proj.branch.ToUpper()) { style = { color = branchColor, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } };
                    branchBadge.style.backgroundColor = new Color(branchColor.r, branchColor.g, branchColor.b, 0.15f);
                    branchBadge.style.paddingLeft = 6; branchBadge.style.paddingRight = 6;
                    branchBadge.style.paddingTop = 2; branchBadge.style.paddingBottom = 2;
                    branchBadge.style.borderTopLeftRadius = 3; branchBadge.style.borderTopRightRadius = 3;
                    branchBadge.style.borderBottomLeftRadius = 3; branchBadge.style.borderBottomRightRadius = 3;
                    headerRow.Add(nameLabel);
                    headerRow.Add(branchBadge);
                    card.Add(headerRow);

                    float elapsed = Mathf.Max(0, proj.totalMonths - proj.monthsRemaining);
                    float pct = proj.totalMonths > 0 ? (elapsed / proj.totalMonths) * 100f : 0f;
                    var barBg = new VisualElement() { style = { height = 6, backgroundColor = new Color(0, 0, 0, 0.4f), marginBottom = 6 } };
                    barBg.style.borderTopLeftRadius = 3; barBg.style.borderTopRightRadius = 3;
                    barBg.style.borderBottomLeftRadius = 3; barBg.style.borderBottomRightRadius = 3;
                    var fill = new VisualElement() { style = { width = Length.Percent(pct), height = Length.Percent(100), backgroundColor = branchColor } };
                    fill.style.borderTopLeftRadius = 3; fill.style.borderTopRightRadius = 3;
                    fill.style.borderBottomLeftRadius = 3; fill.style.borderBottomRightRadius = 3;
                    barBg.Add(fill);
                    card.Add(barBg);

                    var infoRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                    infoRow.Add(new Label($"⏱ {proj.monthsRemaining} months remaining") { style = { color = new Color(255/255f, 152/255f, 0), fontSize = 9 } });
                    infoRow.Add(new Label($"+{proj.strengthGain:F0}% Strength on completion") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                    card.Add(infoRow);

                    _activeProjectsList.Add(card);
                }
            }

            var catalog = new System.Collections.Generic.List<(string name, string branch, float strengthGain, float costBillions, int months, string desc)>
            {
                ("Iron Fist Armour Programme",  "Army",     12f, 0.80f, 12, "Procure 50 MBTs & 100 APCs. +12% Army Strength."),
                ("Artillery Modernization",     "Army",     8f,  0.40f,  8, "Upgrade field artillery systems. +8% Army Strength."),
                ("Infantry Combat System",      "Army",     6f,  0.25f,  6, "Advanced body armour & small arms. +6% Army Strength."),
                ("Rapid Reaction Force Uplift", "Army",     5f,  0.30f,  6, "Pre-position stockpiles for fast deployment. +5% Army."),
                ("Blue-Water Fleet Expansion",  "Navy",     15f, 2.50f, 18, "Commission 2 Destroyers & 1 Frigate. +15% Navy Strength."),
                ("Submarine Stealth Pack",      "Navy",     10f, 1.80f, 14, "Acquire 2 SSK attack submarines. +10% Navy Strength."),
                ("Coastal Defence Network",     "Navy",     7f,  0.60f,  9, "Mine layers, patrol boats, radar. +7% Navy Strength."),
                ("Naval Air Integration",       "Navy",     8f,  1.00f, 12, "Carrier-capable aircraft & EW systems. +8% Navy Strength."),
                ("Generation-5 Fighter Block",  "AirForce", 18f, 3.50f, 24, "Procure 12 5th-gen multi-role jets. +18% Air Strength."),
                ("Strategic Bomber Fleet",      "AirForce", 14f, 2.80f, 20, "4 long-range strategic bombers. +14% Air Strength."),
                ("UAV Swarm Programme",         "AirForce", 9f,  0.70f, 10, "50 tactical & 10 MALE UAVs. +9% Air Strength."),
                ("Air Defence Umbrella",        "AirForce", 11f, 1.20f, 12, "S-400 class SAM network. +11% Air Strength."),
                ("Black Sabre Spec-Ops",        "RDF",      20f, 0.50f,  8, "Elite tier-1 operators & equipment. +20% RDF Readiness."),
                ("SIGINT & Cyber Warfare",      "RDF",      12f, 0.35f,  6, "Electronic warfare & signals intel. +12% RDF Readiness."),
                ("Hostage Rescue Unit",         "RDF",      8f,  0.20f,  4, "Dedicated HRU with transport helis. +8% RDF Readiness."),
            };

            var activeNames = new System.Collections.Generic.HashSet<string>();
            if (country.militaryProjects != null)
                foreach (var p in country.militaryProjects) activeNames.Add(p.projectName);

            foreach (var (name, branch, strengthGain, costBillions, months, desc) in catalog)
            {
                float localCost = costBillions * country.exchangeRate;
                bool isActive   = activeNames.Contains(name);
                bool canAfford  = GameManager.Instance != null && GameManager.Instance.playerBudget >= localCost;

                Color branchColor = branch switch
                {
                    "Army"     => new Color(255/255f, 152/255f, 0),
                    "Navy"     => new Color(0.1f, 0.5f, 0.9f),
                    "AirForce" => new Color(156/255f, 39/255f, 176/255f),
                    "RDF"      => new Color(255/255f, 82/255f, 82/255f),
                    _          => Color.grey
                };

                var card = new VisualElement();
                card.AddToClassList("sub-panel");
                card.style.marginBottom = 12;
                card.style.paddingTop = 14;
                card.style.paddingBottom = 14;
                card.style.paddingLeft = 16;
                card.style.paddingRight = 16;
                card.style.opacity = isActive ? 0.45f : 1.0f;

                var headerRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6 } };
                var nameLabel = new Label(name) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11, flexShrink = 1 } };
                var branchBadge = new Label(branch.ToUpper()) { style = { color = branchColor, fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold } };
                branchBadge.style.backgroundColor = new Color(branchColor.r, branchColor.g, branchColor.b, 0.15f);
                branchBadge.style.paddingLeft = 6; branchBadge.style.paddingRight = 6;
                branchBadge.style.paddingTop = 2; branchBadge.style.paddingBottom = 2;
                branchBadge.style.borderTopLeftRadius = 3; branchBadge.style.borderTopRightRadius = 3;
                branchBadge.style.borderBottomLeftRadius = 3; branchBadge.style.borderBottomRightRadius = 3;
                headerRow.Add(nameLabel);
                headerRow.Add(branchBadge);
                card.Add(headerRow);

                var descLabel = new Label(desc) { style = { color = Color.grey, fontSize = 9, whiteSpace = WhiteSpace.Normal, marginBottom = 8 } };
                card.Add(descLabel);

                var statsRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 10 } };
                statsRow.Add(new Label($"Cost: {UiFormat.Money(localCost, sym)}") { style = { color = canAfford ? new Color(0.14f, 0.93f, 0.34f) : new Color(255/255f, 82/255f, 82/255f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                statsRow.Add(new Label($"Duration: {months} months") { style = { color = Color.grey, fontSize = 9 } });
                statsRow.Add(new Label($"+{strengthGain:F0}% on completion") { style = { color = new Color(0.1f, 0.5f, 0.9f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                card.Add(statsRow);

                var btn = new Button();
                btn.AddToClassList("button-premium");
                btn.style.height = 26;
                btn.style.fontSize = 9;

                if (isActive)
                {
                    btn.text = "ALREADY IN PROGRESS";
                    btn.SetEnabled(false);
                }
                else if (!canAfford)
                {
                    btn.text = $"INSUFFICIENT FUNDS — Need {UiFormat.Money(localCost, sym)}";
                    btn.SetEnabled(false);
                }
                else
                {
                    string capName = name;
                    string capBranch = branch;
                    float capStrength = strengthGain;
                    float capCost = costBillions;
                    int capMonths = months;
                    btn.text = $"COMMISSION PROJECT — {UiFormat.Money(localCost, sym)}";
                    btn.clicked += () =>
                    {
                        if (GameManager.Instance == null) return;
                        if (GameManager.Instance.playerBudget < localCost)
                        {
                            NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need {UiFormat.Money(localCost, sym)} to commission {capName}.", Color.red);
                            return;
                        }
                        GameManager.Instance.playerBudget -= localCost;
                        country.treasury = GameManager.Instance.playerBudget;

                        var proj = new MilitaryProject
                        {
                            projectName    = capName,
                            branch         = capBranch,
                            strengthGain   = capStrength,
                            costBillions   = capCost,
                            monthsRemaining = capMonths,
                            totalMonths    = capMonths
                        };
                        country.militaryProjects.Add(proj);

                        NotificationManager.Instance?.AddNotification($"PROJECT COMMISSIONED: {capName} — {capMonths} months to delivery. ({UiFormat.Money(localCost, sym)} allocated)", new Color(0.1f, 0.5f, 0.9f));
                        NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} commissions strategic project: {capName}.");
                        RefreshMilitaryData();
                    };
                }
                card.Add(btn);
                _availableProjectsList.Add(card);
            }
        }

    }
}
