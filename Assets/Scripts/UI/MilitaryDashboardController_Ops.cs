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
        private void ShowBlockadeModal()
        {
            // Placeholder: Typically pops up a list of enemies, for now just auto-blockade test target
            var gm = GameManager.Instance;
            if (gm == null || gm.playerCountry == null) return;
            var target = gm.GetCountryByName(gm.playerCountry.warEnemyName);
            if (target != null && gm.WarSystem is Systems.WarSystem warSys)
            {
                warSys.DeclareBlockade(gm.playerCountry, target);
                RefreshMilitaryData();
            }
            else
            {
                 NotificationManager.Instance?.AddNotification("Must be at war with a target to declare a blockade.", Color.yellow);
            }
        }

        private void PopulateBlockades(CountryDataSO country)
        {
            if (_blockadesContainer == null) return;
            _blockadesContainer.Clear();

            var gm = GameManager.Instance;
            if (gm == null) return;

            // Find blockades we are ENFORCING
            bool foundAny = false;
            foreach (var enemy in gm.allCountries)
            {
                if (enemy == country || enemy.activeBlockades == null) continue;
                foreach (var b in enemy.activeBlockades.FindAll(x => x.blockerCountry == country.countryName))
                {
                    foundAny = true;
                    var row = new VisualElement() { style = { marginBottom = 6, paddingTop = 4, paddingBottom = 4, paddingLeft = 4, paddingRight = 4, backgroundColor = new Color(0.1f, 0.5f, 0.9f, 0.2f), borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } };
                    row.Add(new Label($"BLOCKADING: {b.targetCountry}") { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
                    row.Add(new Label($"Ships Assigned: {b.frigatesAssigned} Frig / {b.destroyersAssigned} Des") { style = { color = new Color(200/255f, 200/255f, 200/255f), fontSize = 9 } });
                    row.Add(new Label($"Trade Suppressed: {b.tradeSuppressionPercent}%") { style = { color = new Color(255/255f, 82/255f, 82/255f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                    _blockadesContainer.Add(row);
                }
            }

            if (!foundAny)
            {
                _blockadesContainer.Add(new Label("No Active Blockades Ordered.") { style = { color = Color.grey, fontSize = 10, unityFontStyleAndWeight = FontStyle.Italic } });
            }
        }

        private void PopulateTheatre(CountryDataSO country, GameManager gm)
        {
            if (_theatreStatusList == null) return;
            _theatreStatusList.Clear();

            if (country.regions.Count == 0)
            {
                _theatreStatusList.Add(new Label("No regional theatre data.") { style = { color = Color.grey } });
                return;
            }

            foreach (var region in country.regions)
            {
                string status = region.security >= 70f ? "SECURE" : (region.security >= 45f ? "CONTESTED" : "CRITICAL");
                Color c = region.security >= 70f ? new Color(0.14f, 0.93f, 0.34f)
                    : (region.security >= 45f ? new Color(0.9f, 0.6f, 0f) : new Color(255/255f, 82/255f, 82/255f));

                var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6 } };
                row.Add(new Label(region.regionName) { style = { color = Color.white, fontSize = 11 } });
                row.Add(new Label($"{status} · Sec {region.security:F0}% · Stab {region.stability:F0}%") { style = { color = c, fontSize = 10 } });
                _theatreStatusList.Add(row);
            }
        }

        private void PopulateOperations(CountryDataSO country, GameManager gm)
        {
            if (_activeOpsList == null) return;
            _activeOpsList.Clear();

            // ── WAR FRONT STATUS PANEL ──────────────────────────────────────
            // Show active war fronts at the top of the operations panel
            if (gm?.WarSystem != null && gm.allCountries != null)
            {
                bool atWar = false;
                foreach (var other in gm.allCountries)
                {
                    if (other == country) continue;
                    if (gm.WarSystem.GetWarStage(other) != StateCraft.Interfaces.WarStage.Conflict) continue;
                    atWar = true;

                    // Build front status card
                    var frontCard = new VisualElement();
                    frontCard.style.marginBottom      = 12;
                    frontCard.style.paddingTop        = 14; frontCard.style.paddingBottom = 14;
                    frontCard.style.paddingLeft       = 14; frontCard.style.paddingRight  = 14;
                    frontCard.style.backgroundColor   = new Color(0.12f, 0.04f, 0.04f, 0.98f);
                    frontCard.style.borderTopWidth    = 1; frontCard.style.borderBottomWidth = 1;
                    frontCard.style.borderLeftWidth   = 3; frontCard.style.borderRightWidth  = 1;
                    frontCard.style.borderLeftColor   = new Color(1f, 0.25f, 0.25f, 0.9f);
                    frontCard.style.borderTopColor    = new Color(0.4f, 0.1f, 0.1f, 0.6f);
                    frontCard.style.borderBottomColor = new Color(0.4f, 0.1f, 0.1f, 0.6f);
                    frontCard.style.borderRightColor  = new Color(0.4f, 0.1f, 0.1f, 0.6f);
                    frontCard.style.borderTopLeftRadius    = 4; frontCard.style.borderTopRightRadius    = 4;
                    frontCard.style.borderBottomLeftRadius = 4; frontCard.style.borderBottomRightRadius = 4;

                    // Header
                    var fHeader = new VisualElement();
                    fHeader.style.flexDirection  = FlexDirection.Row;
                    fHeader.style.justifyContent = Justify.SpaceBetween;
                    fHeader.style.marginBottom   = 8;

                    fHeader.Add(new Label($"⚔️  ACTIVE FRONT: {other.countryName.ToUpper()}") {
                        style = { color = new Color(255/255f, 82/255f, 82/255f), fontSize = 11,
                                  unityFontStyleAndWeight = FontStyle.Bold }
                    });

                    string warPhase = country.warTurnsActive <= 2 ? "MOBILISATION" :
                                      country.warTurnsActive <= 6 ? "OFFENSIVE" :
                                      country.warTurnsActive <= 18 ? "ATTRITION" : "GRINDING WAR";
                    var phaseLabel = new Label($"MONTH {country.warTurnsActive} — {warPhase}");
                    phaseLabel.style.color     = new Color(0.9f, 0.7f, 0.2f);
                    phaseLabel.style.fontSize  = 8;
                    phaseLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    fHeader.Add(phaseLabel);
                    frontCard.Add(fHeader);

                    // Force comparison bar
                    float myPower    = CalculateFrontPower(country, other.countryName);
                    float enemyPower = CalculateFrontPower(other, country.countryName);
                    float totalPower = Mathf.Max(myPower + enemyPower, 1f);
                    float myPct      = myPower / totalPower;

                    var barLabel = new VisualElement();
                    barLabel.style.flexDirection  = FlexDirection.Row;
                    barLabel.style.justifyContent = Justify.SpaceBetween;
                    barLabel.style.marginBottom   = 3;
                    barLabel.Add(new Label($"OUR COMBAT POWER: {myPower:N0}") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 8 } });
                    barLabel.Add(new Label($"ENEMY COMBAT POWER: {enemyPower:N0}") { style = { color = new Color(255/255f, 82/255f, 82/255f), fontSize = 8 } });
                    frontCard.Add(barLabel);

                    // Power ratio bar — green for us, red for them
                    var barBg = new VisualElement();
                    barBg.style.height = 8; barBg.style.flexDirection = FlexDirection.Row;
                    barBg.style.borderTopLeftRadius    = 4; barBg.style.borderTopRightRadius    = 4;
                    barBg.style.borderBottomLeftRadius = 4; barBg.style.borderBottomRightRadius = 4;
                    barBg.style.marginBottom = 10; barBg.style.overflow = Overflow.Hidden;

                    var ourBar = new VisualElement();
                    ourBar.style.width           = Length.Percent(myPct * 100f);
                    ourBar.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f, 0.9f);
                    barBg.Add(ourBar);

                    var enemyBar = new VisualElement();
                    enemyBar.style.width           = Length.Percent((1f - myPct) * 100f);
                    enemyBar.style.backgroundColor = new Color(1f, 0.32f, 0.32f, 0.9f);
                    barBg.Add(enemyBar);
                    frontCard.Add(barBg);

                    // Stats row
                    var statsRow = new VisualElement();
                    statsRow.style.flexDirection  = FlexDirection.Row;
                    statsRow.style.flexWrap       = Wrap.Wrap;
                    statsRow.style.marginBottom   = 8;

                    void AddStat(string label, string val, Color col)
                    {
                        var s = new VisualElement();
                        s.style.marginRight = 16; s.style.marginBottom = 4;
                        s.Add(new Label(label) { style = { color = new Color(0.5f, 0.5f, 0.6f), fontSize = 7 } });
                        s.Add(new Label(val)   { style = { color = col, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                        statsRow.Add(s);
                    }

                    AddStat("EXHAUSTION",     $"{country.warExhaustion:F0}%",
                        country.warExhaustion > 70f ? new Color(255/255f, 82/255f, 82/255f) :
                        country.warExhaustion > 40f ? new Color(0.9f, 0.6f, 0f) : Color.green);
                    AddStat("WILL TO FIGHT",  $"{country.warWillToFight:F0}%",
                        country.warWillToFight < 30f ? new Color(255/255f, 82/255f, 82/255f) : Color.white);
                    AddStat("TOTAL KIA",      $"{country.totalKIA:N0}",
                        country.totalKIA > 5000 ? new Color(255/255f, 82/255f, 82/255f) : Color.white);
                    AddStat("AMMO",           $"{country.reserveAmmo:F0}",
                        country.reserveAmmo < 30f ? new Color(255/255f, 82/255f, 82/255f) : Color.white);
                    AddStat("FUEL",           $"{country.reserveFuel:F0}",
                        country.reserveFuel < 20f ? new Color(255/255f, 82/255f, 82/255f) : Color.white);
                    frontCard.Add(statsRow);

                    // Enemy stats comparison
                    var enemyRow = new VisualElement();
                    enemyRow.style.flexDirection  = FlexDirection.Row;
                    enemyRow.style.justifyContent = Justify.SpaceBetween;
                    enemyRow.style.marginBottom   = 8;
                    enemyRow.Add(new Label($"Enemy exhaustion: {other.warExhaustion:F0}%  |  Enemy will: {other.warWillToFight:F0}%  |  Enemy KIA: {other.totalKIA:N0}") {
                        style = { color = new Color(0.55f, 0.55f, 0.65f), fontSize = 8 }
                    });
                    frontCard.Add(enemyRow);

                    // Advantage / momentum message
                    string momMsg;
                    Color  momCol;
                    if (myPower > enemyPower * 1.5f) { momMsg = "▲ WE HOLD THE INITIATIVE — press the advantage"; momCol = Color.green; }
                    else if (enemyPower > myPower * 1.5f) { momMsg = "▼ ENEMY HAS THE INITIATIVE — reinforce or pull back"; momCol = new Color(255/255f, 82/255f, 82/255f); }
                    else if (myPower > enemyPower) { momMsg = "▲ SLIGHT ADVANTAGE — keep deploying to consolidate"; momCol = new Color(0.14f, 0.93f, 0.34f); }
                    else if (enemyPower > myPower) { momMsg = "▼ SLIGHT DISADVANTAGE — deploy reinforcements"; momCol = new Color(0.9f, 0.6f, 0f); }
                    else { momMsg = "◼ STALEMATE — neither side advancing"; momCol = Color.yellow; }

                    frontCard.Add(new Label(momMsg) { style = { color = momCol, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });

                    _activeOpsList.Add(frontCard);
                }

                // Supply warning banner if at war and ammo/fuel critical
                if (atWar && (country.reserveAmmo < 30f || country.reserveFuel < 20f))
                {
                    var supplyWarn = new VisualElement();
                    supplyWarn.style.backgroundColor = new Color(0.3f, 0.1f, 0f, 0.9f);
                    supplyWarn.style.paddingTop = 8; supplyWarn.style.paddingBottom = 8;
                    supplyWarn.style.paddingLeft = 12; supplyWarn.style.paddingRight = 12;
                    supplyWarn.style.marginBottom = 10;
                    supplyWarn.style.borderTopLeftRadius = 4; supplyWarn.style.borderTopRightRadius = 4;
                    supplyWarn.style.borderBottomLeftRadius = 4; supplyWarn.style.borderBottomRightRadius = 4;
                    string warns = "";
                    if (country.reserveAmmo < 30f) warns += $"⚠ AMMO {country.reserveAmmo:F0}  ";
                    if (country.reserveFuel < 20f) warns += $"⚠ FUEL {country.reserveFuel:F0}  ";
                    supplyWarn.Add(new Label($"SUPPLY CRITICAL: {warns.Trim()} — Resupply via Logistics tab") {
                        style = { color = new Color(1f, 0.6f, 0.2f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold }
                    });
                    _activeOpsList.Add(supplyWarn);
                }
            }

            var ops = new List<MilitaryOperation>(country.activeOperations);

            if (ops.Count == 0)
            {
                _activeOpsList.Add(new Label("No active operations — theatre quiet.") { style = { color = Color.grey, marginTop = 12 } });
                return;
            }

            foreach (var op in ops)
            {
                var card = new VisualElement();
                card.AddToClassList("sub-panel");
                card.style.paddingTop = 12;
                card.style.paddingBottom = 12;
                card.style.paddingLeft = 12;
                card.style.paddingRight = 12;
                card.style.marginBottom = 8;

                // Color-code border defense ops distinctly
                bool isBorderDefense = op.type == "DEFEND BORDERS";
                if (isBorderDefense)
                {
                    card.style.borderLeftWidth = 3;
                    card.style.borderLeftColor = new Color(0.14f, 0.93f, 0.34f, 0.9f);
                }

                card.Add(new Label(op.opName) { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 11 } });
                card.Add(new Label($"{op.type} · {op.regionName}") { style = { color = Color.grey, fontSize = 9, marginTop = 4 } });

                string forceDetails = $"Deployment: {op.deployedTroops:N0} Troops";
                if (op.deployedRDF > 0) forceDetails += $" · {op.deployedRDF} RDF";
                if (op.deployedTanks > 0) forceDetails += $" · {op.deployedTanks} Tanks";
                if (op.deployedAPCs > 0) forceDetails += $" · {op.deployedAPCs} APCs";
                if (op.deployedFighters > 0) forceDetails += $" · {op.deployedFighters} Jets";
                if (op.deployedBombers > 0) forceDetails += $" · {op.deployedBombers} Bombers";
                if (op.deployedUAVs > 0) forceDetails += $" · {op.deployedUAVs} UAVs";
                if (op.deployedHelis > 0) forceDetails += $" · {op.deployedHelis} Helis";
                if (op.deployedFrigates > 0) forceDetails += $" · {op.deployedFrigates} Frigates";
                if (op.deployedSubmarines > 0) forceDetails += $" · {op.deployedSubmarines} Subs";
                card.Add(new Label(forceDetails) { style = { color = new Color(200/255f, 200/255f, 200/255f), fontSize = 8, marginTop = 2 } });

                var footer = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 8 } };
                string displayStatus = op.turnsToDeploy > 0 ? $"DEPLOYING ({op.turnsToDeploy}m)" : op.status.ToUpper();
                Color statusColor = op.turnsToDeploy > 0 ? new Color(0.9f, 0.6f, 0f) : new Color(0.1f, 0.5f, 0.9f);
                footer.Add(new Label(displayStatus) { style = { color = statusColor, fontSize = 9 } });
                
                float opCostB = (op.costPerMonth / 1000f) * country.exchangeRate; // e.g. 50M becomes 0.05B USD converted to local currency
                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
                footer.Add(new Label($"{sym}{opCostB:F2}B/mo") { style = { color = Color.grey, fontSize = 8 } });
                footer.Add(new Label($"{op.deployedTroops:N0} INF") { style = { color = Color.grey, fontSize = 8 } });
                card.Add(footer);

                // If it is a custom launched operation, add a DECOMMISSION button
                if (country.activeOperations.Contains(op))
                {
                    var actionRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 8 } };

                    var reinforceBtn = new Button() { text = "REINFORCE" };
                    reinforceBtn.AddToClassList("button-premium");
                    reinforceBtn.style.height = 22;
                    reinforceBtn.style.fontSize = 8;
                    reinforceBtn.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f, 0.4f);
                    reinforceBtn.style.width = Length.Percent(48);
                    
                    var captureOp = op;
                    reinforceBtn.clicked += () => ShowReinforceModal(captureOp);
                    actionRow.Add(reinforceBtn);

                    var terminateBtn = new Button() { text = "DECOMMISSION OP" };
                    terminateBtn.AddToClassList("button-premium");
                    terminateBtn.style.height = 22;
                    terminateBtn.style.fontSize = 8;
                    terminateBtn.style.width = Length.Percent(48);
                    
                    terminateBtn.clicked += () =>
                    {
                        // Return deployed forces to standing pool!
                        country.unitInfantry += captureOp.deployedTroops;
                        country.rdfStats.personnel += captureOp.deployedRDF;
                        country.unitAttackHelis += captureOp.deployedHelis;
                        country.unitTanks += captureOp.deployedTanks;
                        country.unitAPCs += captureOp.deployedAPCs;
                        country.unitFighters += captureOp.deployedFighters;
                        country.unitBombers += captureOp.deployedBombers;
                        country.unitUAVs += captureOp.deployedUAVs;
                        country.unitFrigates += captureOp.deployedFrigates;
                        country.unitSubmarines += captureOp.deployedSubmarines;

                        string retDetails = $"{captureOp.deployedTroops:N0} Troops";
                        if (captureOp.deployedRDF > 0) retDetails += $", {captureOp.deployedRDF} RDF";
                        if (captureOp.deployedTanks > 0) retDetails += $", {captureOp.deployedTanks} Tanks";
                        if (captureOp.deployedAPCs > 0) retDetails += $", {captureOp.deployedAPCs} APCs";
                        if (captureOp.deployedFighters > 0) retDetails += $", {captureOp.deployedFighters} Jets";
                        if (captureOp.deployedBombers > 0) retDetails += $", {captureOp.deployedBombers} Bombers";
                        if (captureOp.deployedUAVs > 0) retDetails += $", {captureOp.deployedUAVs} UAVs";
                        if (captureOp.deployedHelis > 0) retDetails += $", {captureOp.deployedHelis} Helis";
                        if (captureOp.deployedFrigates > 0) retDetails += $", {captureOp.deployedFrigates} Ships";
                        if (captureOp.deployedSubmarines > 0) retDetails += $", {captureOp.deployedSubmarines} Subs";

                        country.activeOperations.Remove(captureOp);
                        NotificationManager.Instance?.AddNotification($"TERMINATED: {captureOp.opName} decommissioned. {retDetails} returned.", Color.yellow);
                        NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} decommissioned {captureOp.opName}. Deployed forces returned to barracks.");
                        RefreshMilitaryData();
                    };
                    actionRow.Add(terminateBtn);
                    card.Add(actionRow);
                }

                _activeOpsList.Add(card);
            }

            // ── Wartime Quick-Actions (shown only during conflict) ──
            var warStage = gm.WarSystem?.GetWarStage(country) ?? WarStage.Peace;
            if (warStage == WarStage.Conflict)
            {
                // Section header
                var warActHeader = new Label("⚔ WARTIME QUICK ACTIONS") { style = { color = new Color(255/255f, 82/255f, 82/255f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 14, marginBottom = 6 } };
                _activeOpsList.Add(warActHeader);

                // ── Deploy to National Borders ──
                var borderCard = new VisualElement();
                borderCard.AddToClassList("sub-panel");
                borderCard.style.paddingTop = 14;
                borderCard.style.paddingBottom = 14;
                borderCard.style.paddingLeft = 14;
                borderCard.style.paddingRight = 14;
                borderCard.style.marginBottom = 10;
                borderCard.style.borderLeftWidth = 3;
                borderCard.style.borderLeftColor = new Color(0.14f, 0.93f, 0.34f, 0.9f);

                bool alreadyDefending = country.activeOperations.Exists(o => o.type == "DEFEND BORDERS");
                borderCard.Add(new Label("🛡 NATIONAL BORDER DEFENSE") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });
                borderCard.Add(new Label(alreadyDefending
                    ? "Border defense already active. Reinforce it using the REINFORCE button above."
                    : "Deploy troops and armour to hold your borders. Border units reduce enemy breakthrough damage by 50%.")
                    { style = { color = Color.grey, fontSize = 8, whiteSpace = WhiteSpace.Normal, marginBottom = 8 } });

                if (!alreadyDefending)
                {
                    var deployBorderBtn = new Button(() =>
                    {
                        if (country.unitInfantry < 1000)
                        {
                            NotificationManager.Instance?.AddNotification("BORDER DEFENSE FAILED: Need at least 1,000 infantry available.", Color.red);
                            return;
                        }
                        int borderTroops = Mathf.Min(5000, country.unitInfantry / 4);
                        int borderTanks  = Mathf.Min(20,   country.unitTanks);
                        int borderAPCs   = Mathf.Min(30,   country.unitAPCs);
                        country.unitInfantry -= borderTroops;
                        country.unitTanks    -= borderTanks;
                        country.unitAPCs     -= borderAPCs;

                        var borderOp = new MilitaryOperation
                        {
                            opName         = $"Operation IRON WALL",
                            regionName     = country.countryName + " (Borders)",
                            type           = "DEFEND BORDERS",
                            status         = "Active",
                            costPerMonth   = borderTroops * 0.005f,
                            deployedTroops = borderTroops,
                            deployedTanks  = borderTanks,
                            deployedAPCs   = borderAPCs,
                            turnsToDeploy  = 2
                        };
                        country.activeOperations.Add(borderOp);

                        NotificationManager.Instance?.AddNotification(
                            $"BORDER DEFENSE DEPLOYED: {borderTroops:N0} troops, {borderTanks} tanks, {borderAPCs} APCs ordered to National Borders (arrives in 2 months).",
                            new Color(0.14f, 0.93f, 0.34f));
                        NewsSystem.Instance?.PublishNews(
                            $"MILITARY: {country.countryName} fortifies its borders with heavy armoured units.");
                        RefreshMilitaryData();

                    }) { text = "🛡 SEND TROOPS TO NATIONAL BORDERS" };
                    deployBorderBtn.AddToClassList("button-premium");
                    deployBorderBtn.style.height = 28;
                    deployBorderBtn.style.fontSize = 9;
                    deployBorderBtn.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f, 0.3f);
                    borderCard.Add(deployBorderBtn);
                }
                _activeOpsList.Add(borderCard);

                // ── Air Intercept Card ──
                var enemy = gm.GetCountryByName(country.warEnemyName)
                         ?? gm.allCountries?.Find(c => c != country && (gm.WarSystem?.GetWarStage(c) == WarStage.Conflict));
                if (enemy != null)
                {
                    var interceptCard = new VisualElement();
                    interceptCard.AddToClassList("sub-panel");
                    interceptCard.style.paddingTop = 14;
                    interceptCard.style.paddingBottom = 14;
                    interceptCard.style.paddingLeft = 14;
                    interceptCard.style.paddingRight = 14;
                    interceptCard.style.marginBottom = 10;
                    interceptCard.style.borderLeftWidth = 3;
                    interceptCard.style.borderLeftColor = new Color(0.1f, 0.5f, 0.9f, 0.9f);

                    interceptCard.Add(new Label("✈ LAUNCH AIR INTERCEPT") { style = { color = new Color(0.1f, 0.5f, 0.9f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });
                    interceptCard.Add(new Label($"Scramble fighters to intercept {enemy.countryName} air assets and stop bombing raids. Costs 40 fuel.")
                        { style = { color = Color.grey, fontSize = 8, whiteSpace = WhiteSpace.Normal, marginBottom = 8 } });

                    var captureEnemy = enemy;
                    var interceptBtn = new Button(() =>
                    {
                        if (gm.WarSystem is Systems.WarSystem ws)
                        {
                            ws.LaunchAirIntercept(country, captureEnemy);
                            RefreshMilitaryData();
                        }
                    }) { text = "✈ INTERCEPT ENEMY AIRCRAFT" };
                    interceptBtn.AddToClassList("button-premium");
                    interceptBtn.style.height = 28;
                    interceptBtn.style.fontSize = 9;
                    interceptBtn.style.backgroundColor = new Color(0.1f, 0.5f, 0.9f, 0.3f);
                    interceptCard.Add(interceptBtn);
                    _activeOpsList.Add(interceptCard);
                }
            }
        }

        private void ShowReinforceModal(MilitaryOperation op)
        {
            var country = GameManager.Instance.playerCountry;
            
            var overlay = new VisualElement() { style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0, backgroundColor = new Color(0, 0, 0, 0.85f), justifyContent = Justify.Center, alignItems = Align.Center } };
            var modal = new VisualElement() { style = { width = 450, paddingBottom = 20, paddingTop = 20, paddingLeft = 20, paddingRight = 20, backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f), borderTopLeftRadius=8, borderTopRightRadius=8, borderBottomLeftRadius=8, borderBottomRightRadius=8 } };
            SetBorderColor(modal.style, new Color(0, 210/255f, 255/255f, 0.5f));
            SetBorderWidth(modal.style, 1);
            overlay.Add(modal);

            modal.Add(new Label($"REINFORCE: {op.opName}") { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 15 } });
            
            var scroll = new ScrollView { style = { maxHeight = 300, marginBottom = 20 } };
            modal.Add(scroll);

            scroll.Add(new Label("AVAILABLE RESERVES TO DEPLOY") { style = { color = Color.grey, fontSize = 9, marginBottom = 10 } });

            // Only show slider if they have any reserves for it
            SliderInt troopsSlider = null;
            if (country.unitInfantry > 0)
            {
                var troopsLabel = new Label($"INFANTRY (Deploying: 0 / {country.unitInfantry:N0} avail)") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } };
                scroll.Add(troopsLabel);
                troopsSlider = new SliderInt(0, country.unitInfantry) { style = { marginBottom = 10 } };
                troopsSlider.RegisterValueChangedCallback(evt => troopsLabel.text = $"INFANTRY (Deploying: {evt.newValue:N0} / {country.unitInfantry:N0} avail)");
                scroll.Add(troopsSlider);
            }

            SliderInt tanksSlider = null;
            if (country.unitTanks > 0)
            {
                var tanksLabel = new Label($"MAIN BATTLE TANKS (Deploying: 0 / {country.unitTanks:N0} avail)") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } };
                scroll.Add(tanksLabel);
                tanksSlider = new SliderInt(0, country.unitTanks) { style = { marginBottom = 10 } };
                tanksSlider.RegisterValueChangedCallback(evt => tanksLabel.text = $"MAIN BATTLE TANKS (Deploying: {evt.newValue:N0} / {country.unitTanks:N0} avail)");
                scroll.Add(tanksSlider);
            }

            SliderInt apcsSlider = null;
            if (country.unitAPCs > 0)
            {
                var apcsLabel = new Label($"ARMOURED PERSONNEL CARRIERS (Deploying: 0 / {country.unitAPCs:N0} avail)") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } };
                scroll.Add(apcsLabel);
                apcsSlider = new SliderInt(0, country.unitAPCs) { style = { marginBottom = 10 } };
                apcsSlider.RegisterValueChangedCallback(evt => apcsLabel.text = $"ARMOURED PERSONNEL CARRIERS (Deploying: {evt.newValue:N0} / {country.unitAPCs:N0} avail)");
                scroll.Add(apcsSlider);
            }

            SliderInt uavsSlider = null;
            if (country.unitUAVs > 0)
            {
                var uavsLabel = new Label($"TACTICAL UAVs (Deploying: 0 / {country.unitUAVs:N0} avail)") { style = { color = Color.grey, fontSize = 9, marginBottom = 4 } };
                scroll.Add(uavsLabel);
                uavsSlider = new SliderInt(0, country.unitUAVs) { style = { marginBottom = 10 } };
                uavsSlider.RegisterValueChangedCallback(evt => uavsLabel.text = $"TACTICAL UAVs (Deploying: {evt.newValue:N0} / {country.unitUAVs:N0} avail)");
                scroll.Add(uavsSlider);
            }

            var actionRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "CANCEL" };
            cancelBtn.AddToClassList("button-premium");
            cancelBtn.style.width = Length.Percent(48);
            cancelBtn.style.height = 30;
            actionRow.Add(cancelBtn);

            var confirmBtn = new Button() { text = "DEPLOY REINFORCEMENTS" };
            confirmBtn.AddToClassList("button-premium");
            confirmBtn.style.backgroundColor = new Color(0, 210/255f, 255/255f, 0.4f);
            confirmBtn.style.width = Length.Percent(48);
            confirmBtn.style.height = 30;
            confirmBtn.clicked += () =>
            {
                int sendTroops = troopsSlider != null ? troopsSlider.value : 0;
                int sendTanks = tanksSlider != null ? tanksSlider.value : 0;
                int sendApcs = apcsSlider != null ? apcsSlider.value : 0;
                int sendUavs = uavsSlider != null ? uavsSlider.value : 0;

                int totalDeployed = sendTroops + sendTanks + sendApcs + sendUavs;
                if (totalDeployed <= 0)
                {
                    NotificationManager.Instance?.AddNotification("INVALID COMMAND: Select units to reinforce.", Color.red);
                    return;
                }

                // Append to OP
                op.deployedTroops += sendTroops;
                op.deployedTanks += sendTanks;
                op.deployedAPCs += sendApcs;
                op.deployedUAVs += sendUavs;

                // Deduct from Reserve
                country.unitInfantry = Mathf.Max(0, country.unitInfantry - sendTroops);
                country.unitTanks = Mathf.Max(0, country.unitTanks - sendTanks);
                country.unitAPCs = Mathf.Max(0, country.unitAPCs - sendApcs);
                country.unitUAVs = Mathf.Max(0, country.unitUAVs - sendUavs);

                // Add flat cost + recalculate boost
                float addedBoost = (sendTroops / 1000f) * 0.5f + sendTanks * 0.8f + sendApcs * 0.4f + sendUavs * 1.2f;
                op.securityBoostRate += addedBoost;

                overlay.parent?.Remove(overlay);
                NotificationManager.Instance?.AddNotification($"REINFORCEMENTS DEPLOYED: {totalDeployed:N0} structural units added to {op.opName}.", Color.cyan);
                RefreshMilitaryData();
            };
            actionRow.Add(confirmBtn);
            modal.Add(actionRow);

            var dashboard = _root?.Q<VisualElement>("MilitaryDashboard");
            if (dashboard != null) dashboard.Add(overlay);
            else _panelOps.Add(overlay);
        }

        private void DeployOperation(CountryDataSO country, string opName, string regionName, string type, 
            int troops, int tanks, int apcs, int fighters, int helis, int frigates, int submarines,
            int rdf = 0, int bombers = 0, int uavs = 0)
        {
            // Calculate dynamic cost per turn based on the deployed sizes
            float monthlyCost = 0.01f + 
                                (troops / 1000f) * 0.01f + 
                                tanks * 0.005f + 
                                apcs * 0.002f + 
                                fighters * 0.015f + 
                                helis * 0.008f + 
                                frigates * 0.04f + 
                                submarines * 0.06f +
                                (rdf / 1000f) * 0.02f +
                                bombers * 0.025f +
                                uavs * 0.005f;

            float securityBoostRate = 1.0f + 
                                      (troops / 1000f) * 0.5f + 
                                      tanks * 0.8f + 
                                      apcs * 0.4f + 
                                      fighters * 2.0f + 
                                      helis * 1.0f + 
                                      frigates * 1.5f + 
                                      submarines * 2.0f +
                                      (rdf / 1000f) * 3.5f +
                                      bombers * 3.0f +
                                      uavs * 1.2f;

            // Apply weapons shortage penalty!
            if (country.unitRifles < country.armyStats.personnel)
            {
                securityBoostRate *= 0.5f;
            }

            // Deduct from standing unit counts
            country.unitInfantry = Mathf.Max(0, country.unitInfantry - troops);
            country.unitTanks = Mathf.Max(0, country.unitTanks - tanks);
            country.unitAPCs = Mathf.Max(0, country.unitAPCs - apcs);
            country.unitFighters = Mathf.Max(0, country.unitFighters - fighters);
            country.unitAttackHelis = Mathf.Max(0, country.unitAttackHelis - helis);
            country.unitFrigates = Mathf.Max(0, country.unitFrigates - frigates);
            country.unitSubmarines = Mathf.Max(0, country.unitSubmarines - submarines);
            country.rdfStats.personnel = Mathf.Max(0, country.rdfStats.personnel - rdf);
            country.unitBombers = Mathf.Max(0, country.unitBombers - bombers);
            country.unitUAVs = Mathf.Max(0, country.unitUAVs - uavs);

            var op = new MilitaryOperation
            {
                opName = opName,
                regionName = regionName,
                type = type,
                status = "Active",
                costPerMonth = monthlyCost * 1000f, // Cost in millions for persistence
                deployedTroops = troops,
                deployedHelis = helis,
                deployedTanks = tanks,
                deployedAPCs = apcs,
                deployedFighters = fighters,
                deployedFrigates = frigates,
                deployedSubmarines = submarines,
                deployedRDF = rdf,
                deployedBombers = bombers,
                deployedUAVs = uavs,
                securityBoostRate = securityBoostRate,
                turnsToDeploy = 1
            };

            country.activeOperations.Add(op);

            // Deduct initial deployment funding immediately
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.playerBudget -= monthlyCost;
                country.treasury = gm.playerBudget;
            }

            // Boost regional security immediately on deployment!
            var targetReg = country.regions.Find(r => r.regionName == regionName);
            if (targetReg != null)
            {
                // Elite RDF Spec-Ops amplify the immediate first-strike zone securing!
                float rdfBonus = rdf > 0 ? (rdf / 1000f) * 8f : 
                                 (country.rdfStats.personnel > 0 && country.rdfReadiness >= 75f) ? 3.0f : 0f;
                // UAV recon provides immediate ISR (Intelligence, Surveillance, Reconnaissance) boost
                float uavBonus = uavs > 0 ? uavs * 0.15f : 0f;
                // Bomber strike provides immediate suppression
                float airBonus = bombers > 0 ? bombers * 0.5f : 0f;

                targetReg.security = Mathf.Min(100f, targetReg.security + 5f + (troops / 1000f) * 1.5f + rdfBonus + uavBonus + airBonus);

                if (rdfBonus > 0f && rdf > 0)
                    NotificationManager.Instance?.AddNotification($"RDF SPEC-OPS: {rdf} elite operators inserted into {regionName} (+{rdfBonus:F1}% Immediate Security).", Color.cyan);
                if (uavBonus > 0f)
                    NotificationManager.Instance?.AddNotification($"ISR ACTIVE: {uavs} UAVs providing 24/7 surveillance over {regionName} theatre.", new Color(0, 0.8f, 1f));
                if (airBonus > 0f)
                    NotificationManager.Instance?.AddNotification($"AIR STRIKE: {bombers} strategic bombers struck key targets in {regionName} (+{airBonus:F1}% Suppression).", new Color(1f, 0.4f, 0f));
            }

            string deploymentDetails = $"{troops:N0} troops";
            if (rdf > 0) deploymentDetails += $", {rdf} RDF Spec-Ops";
            if (tanks > 0) deploymentDetails += $", {tanks} tanks";
            if (apcs > 0) deploymentDetails += $", {apcs} APCs";
            if (fighters > 0) deploymentDetails += $", {fighters} jets";
            if (bombers > 0) deploymentDetails += $", {bombers} bombers";
            if (uavs > 0) deploymentDetails += $", {uavs} UAVs";
            if (helis > 0) deploymentDetails += $", {helis} helis";
            if (frigates > 0) deploymentDetails += $", {frigates} frigates";
            if (submarines > 0) deploymentDetails += $", {submarines} subs";

            NotificationManager.Instance?.AddNotification($"DEPLOYED: {opName} launched ({deploymentDetails}).", Color.red);
            string opType = type == "AERIAL STRIKE" ? "air assets" : type == "SPECIAL OPS" ? "elite forces" : type == "NAVAL BLOCKADE" ? "naval forces" : "combined forces";
            NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} launched {type} operation in {regionName} theatre deploying {opType} under {opName}.");

            RefreshMilitaryData();
        }

        private void PopulateActiveOpsList(CountryDataSO country)
        {
            if (_opsList == null) return;
            _opsList.Clear();

            // Update the count badge on the header button
            var btnViewAll = _root?.Q<Button>("btn-view-all-ops");
            int opCount = country.activeOperations != null ? country.activeOperations.Count : 0;
            if (btnViewAll != null)
                btnViewAll.text = opCount > 0 ? $"VIEW ALL OPERATIONS ({opCount}) >" : "VIEW ALL OPERATIONS >";

            if (country.activeOperations == null || country.activeOperations.Count == 0)
            {
                // Styled empty state
                var empty = new VisualElement();
                empty.style.flexDirection = FlexDirection.Column;
                empty.style.alignItems = Align.Center;
                empty.style.justifyContent = Justify.Center;
                empty.style.flexGrow = 1;
                empty.style.paddingTop = 20;
                empty.style.paddingBottom = 20;
                var emptyIcon = new Label("—");
                emptyIcon.style.fontSize = 28;
                emptyIcon.style.color = new Color(0.27f, 0.50f, 0.60f);
                emptyIcon.style.marginBottom = 8;
                var emptyLbl = new Label("NO ACTIVE OPERATIONS");
                emptyLbl.style.fontSize = 11;
                emptyLbl.style.color = new StyleColor(new Color(0.42f, 0.50f, 0.60f, 1f));
                emptyLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                emptyLbl.style.letterSpacing = 1;
                emptyLbl.style.marginTop = 10;
                var emptySub = new Label("Launch an operation to begin deployment.");
                emptySub.style.fontSize = 9;
                emptySub.style.color = new StyleColor(new Color(0.3f, 0.38f, 0.46f, 1f));
                emptySub.style.marginTop = 6;
                empty.Add(emptyIcon); empty.Add(emptyLbl); empty.Add(emptySub);
                _opsList.Add(empty);
                return;
            }

            foreach (var op in country.activeOperations)
            {
                OpType typeEnum = OpType.Peacekeeping;
                if (op.type.Contains("TERROR")) typeEnum = OpType.CounterTerror;
                else if (op.type.Contains("BORDER")) typeEnum = OpType.BorderPatrol;
                else if (op.type.Contains("RECON")) typeEnum = OpType.AirRecon;
                else if (op.type.Contains("MARITIME") || op.type.Contains("NAVAL")) typeEnum = OpType.Maritime;
                else if (op.type.Contains("HVT") || op.type.Contains("STRIKE")) typeEnum = OpType.HvtCapture;

                int progress = 100;
                if (op.turnsToDeploy > 0) progress = 10;
                else progress = 50;

                int successChance = 80;

                var card = new VisualElement();
                card.AddToClassList("op-card");
                card.AddToClassList(OpTypeCardClass(typeEnum));

                var iconEl = new VisualElement();
                iconEl.AddToClassList("op-card__icon");
                var iconGlyph = new Label(OpTypeGlyph(typeEnum));
                iconGlyph.AddToClassList("op-card__icon-glyph");
                iconEl.Add(iconGlyph);
                card.Add(iconEl);

                var info = new VisualElement();
                info.AddToClassList("op-card__info");
                var nameLabel = new Label(op.opName);
                nameLabel.AddToClassList("op-card__name");
                var meta = new Label($"Type: {op.type}    Region: {op.regionName}");
                meta.AddToClassList("op-card__meta");
                var obj = new Label($"Objective: Secure {op.regionName} and suppress hostilities.");
                obj.AddToClassList("op-card__objective");
                info.Add(nameLabel); info.Add(meta); info.Add(obj);
                card.Add(info);

                var progStat = new VisualElement();
                progStat.AddToClassList("op-card__stats");
                var progLbl = new Label("PROGRESS");
                progLbl.AddToClassList("op-card__stat-label");
                var progVal = new Label($"{progress}%");
                progVal.AddToClassList("op-card__stat-value");
                var barBg = new VisualElement();
                barBg.AddToClassList("op-card__progress-bar");
                var barFill = new VisualElement();
                barFill.AddToClassList("op-card__progress-fill");
                barFill.AddToClassList(ProgressFillClass(progress));
                barFill.style.width = Length.Percent(progress);
                barBg.Add(barFill);
                progStat.Add(progLbl); progStat.Add(progVal); progStat.Add(barBg);
                card.Add(progStat);

                var successStat = new VisualElement();
                successStat.AddToClassList("op-card__stats");
                var successLbl = new Label("SUCCESS\nCHANCE");
                successLbl.AddToClassList("op-card__stat-label");
                var successVal = new Label($"{successChance}%");
                successVal.AddToClassList("op-card__stat-value");
                successStat.Add(successLbl); successStat.Add(successVal);
                card.Add(successStat);

                var etaEl = new VisualElement();
                etaEl.AddToClassList("op-card__eta");
                var etaLbl = new Label("ETA");
                etaLbl.AddToClassList("op-card__eta-label");
                string etaStr = op.turnsToDeploy > 0 ? $"{op.turnsToDeploy} Mo" : "Deployed";
                var etaVal = new Label(etaStr);
                etaVal.AddToClassList("op-card__eta-value");
                etaEl.Add(etaLbl); etaEl.Add(etaVal);
                card.Add(etaEl);
                
                var viewBtn = new Button(() => { /* End mission logic */ });
                viewBtn.text = "ABORT";
                viewBtn.AddToClassList("op-card__view-btn");
                viewBtn.style.backgroundColor = new Color(244/255f, 67/255f, 54/255f, 0.4f);
                viewBtn.clicked += () => {
                        country.activeOperations.Remove(op);
                        country.unitInfantry += op.deployedTroops;
                        country.rdfStats.personnel += op.deployedRDF;
                        country.unitTanks += op.deployedTanks;
                        country.unitAPCs += op.deployedAPCs;
                        country.unitFighters += op.deployedFighters;
                        country.unitAttackHelis += op.deployedHelis;
                        RefreshMilitaryData();
                };
                card.Add(viewBtn);

                _opsList.Add(card);
            }
        }

        private void PopulateOperationsFeed(CountryDataSO country, GameManager gm)
        {
            if (_feedList == null) return;
            _feedList.Clear();
            var feedData = DashboardLiveContent.BuildMilitaryFeed(country, gm);
            if (feedData == null || feedData.Count == 0) return;

            foreach (var f in feedData)
            {
                var item = new VisualElement();
                item.AddToClassList("feed-item");

                FeedColour col = FeedColour.Green;
                if (f.Message.Contains("Critical") || f.Message.Contains("WAR") || f.Message.Contains("KIA") || f.Message.Contains("SHORTAGE")) col = FeedColour.Red;
                else if (f.Message.Contains("Alert") || f.Message.Contains("Warning")) col = FeedColour.Yellow;

                var dot = new VisualElement();
                dot.AddToClassList("feed-item__dot");
                dot.AddToClassList(FeedDotClass(col));
                item.Add(dot);

                var dateCol = new VisualElement();
                dateCol.AddToClassList("feed-item__date-col");
                var day  = new Label("LIVE");
                day.AddToClassList("feed-item__day");
                var date = new Label($"{gm.TurnSystem.CurrentDate}");
                date.AddToClassList("feed-item__date");
                dateCol.Add(day); dateCol.Add(date);
                item.Add(dateCol);

                var text = new Label($"[{f.Tag}] {f.Message}");
                text.AddToClassList("feed-item__text");
                item.Add(text);

                _feedList.Add(item);
            }
        }

        private void PopulateOpTypesGrid(CountryDataSO country)
        {
            var opTypesScroll = _root?.Q<ScrollView>("op-types-scroll");
            if (opTypesScroll == null) return;
            opTypesScroll.Clear();
            
            var opTypes = new List<OpTypeEntry>()
            {
                new OpTypeEntry("COUNTER TERROR", "Eliminate terrorist cells and safe havens.", "RDF, Air Support", "₦50B", OpType.CounterTerror),
                new OpTypeEntry("BORDER PATROL", "Secure borders and prevent infiltration.", "Infantry, Air Recon", "₦15B", OpType.BorderPatrol),
                new OpTypeEntry("PEACEKEEPING", "Maintain stability in unstable regions.", "Infantry, Logistics", "₦35B", OpType.Peacekeeping),
                new OpTypeEntry("AIR RECON", "Gather intelligence and monitor threats.", "Air Wing", "₦25B", OpType.AirRecon),
                new OpTypeEntry("MARITIME PATROL", "Protect territorial waters and trade.", "Navy, Air Support", "₦20B", OpType.Maritime),
                new OpTypeEntry("HVT CAPTURE", "Capture or neutralise high value targets.", "RDF, Intelligence", "₦40B", OpType.HvtCapture),
            };

            foreach (var t in opTypes)
            {
                var card = new VisualElement();
                card.AddToClassList("op-type-card");
                card.AddToClassList(OpTypeCardClass(t.Type));

                var header = new VisualElement();
                header.AddToClassList("op-type-card__header");
                var iconLabel = new Label(OpTypeGlyph(t.Type));
                iconLabel.AddToClassList("op-type-card__icon");
                iconLabel.style.color = OpTypeAccentColour(t.Type);
                var nameLabel = new Label(t.Name);
                nameLabel.AddToClassList("op-type-card__name");
                nameLabel.style.color = OpTypeAccentColour(t.Type);
                header.Add(iconLabel); header.Add(nameLabel);
                card.Add(header);

                var desc = new Label(t.Description);
                desc.AddToClassList("op-type-card__desc");
                card.Add(desc);

                var reqLabel = new Label("Required Forces");
                reqLabel.AddToClassList("op-type-card__req-label");
                card.Add(reqLabel);
                var reqVal = new Label(t.RequiredForces);
                reqVal.AddToClassList("op-type-card__req-val");
                card.Add(reqVal);

                var costLabel = new Label("Est. Cost");
                costLabel.AddToClassList("op-type-card__cost-label");
                card.Add(costLabel);
                var costVal = new Label(t.EstCost);
                costVal.AddToClassList("op-type-card__cost-val");
                card.Add(costVal);

                var selectBtn = new Button(() => { ShowLaunchOpModal(); });
                selectBtn.text = "SELECT";
                selectBtn.AddToClassList("op-type-card__select-btn");
                card.Add(selectBtn);

                opTypesScroll.Add(card);
            }
        }

        private void ShowInboxModal()
        {
            var country = GameManager.Instance.playerCountry;
            // Clear the routine message badge when player opens inbox
            StateCraft.Systems.InboxSystem.Instance?.ClearRoutineBadge();
            var overlay = new VisualElement() { style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0, backgroundColor = new Color(0, 0, 0, 0.85f), justifyContent = Justify.Center, alignItems = Align.Center } };
            var modal = new VisualElement() { style = { width = 500, paddingBottom = 20, paddingTop = 20, paddingLeft = 20, paddingRight = 20, backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f), borderTopLeftRadius=8, borderTopRightRadius=8, borderBottomLeftRadius=8, borderBottomRightRadius=8 } };
            SetBorderColor(modal.style, new Color(0, 210/255f, 255/255f, 0.5f));
            SetBorderWidth(modal.style, 1);
            overlay.Add(modal);

            var headerRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 10 } };
            headerRow.Add(new Label("COMMAND INBOX") { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold } });
            
            var closeBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "×" };
            closeBtn.style.width = 30; closeBtn.style.height = 30; closeBtn.style.fontSize = 18;
            closeBtn.AddToClassList("button-premium");
            headerRow.Add(closeBtn);
            modal.Add(headerRow);

            var scroll = new ScrollView { style = { maxHeight = 400, marginBottom = 10 } };
            modal.Add(scroll);

            if (country.inboxMessages == null || country.inboxMessages.Count == 0)
            {
                scroll.Add(new Label("Inbox is empty. No new intelligence.") { style = { color = Color.grey, marginTop = 20, unityTextAlign = TextAnchor.MiddleCenter } });
            }
            else
            {
                // Show newest first
                var messages = new List<InboxMessage>(country.inboxMessages);
                messages.Reverse();

                foreach (var msg in messages)
                {
                    var card = new VisualElement();
                    card.AddToClassList("sub-panel");
                    card.style.paddingTop = 10; card.style.paddingBottom = 10;
                    card.style.paddingLeft = 10; card.style.paddingRight = 10;
                    card.style.marginBottom = 5;

                    var topRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                    Color senderColor = msg.urgency == "Critical" ? new Color(255/255f, 82/255f, 82/255f) : (msg.urgency == "Important" ? new Color(0.9f, 0.6f, 0f) : new Color(0.1f, 0.5f, 0.9f));
                    topRow.Add(new Label(msg.sender) { style = { color = senderColor, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } });
                    topRow.Add(new Label($"Turn {msg.turnReceived}") { style = { color = Color.grey, fontSize = 8 } });
                    card.Add(topRow);

                    card.Add(new Label(msg.subject) { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 2 } });
                    
                    var bodyLabel = new Label(msg.body) { style = { color = new Color(200/255f, 200/255f, 200/255f), fontSize = 10, marginTop = 4 } };
                    bodyLabel.style.whiteSpace = WhiteSpace.Normal;
                    card.Add(bodyLabel);

                    scroll.Add(card);
                }
            }

            var clearBtn = new Button() { text = "CLEAR ALL MESSAGES" };
            clearBtn.AddToClassList("button-premium");
            clearBtn.style.height = 30;
            clearBtn.clicked += () =>
            {
                country.inboxMessages?.Clear();
                overlay.parent?.Remove(overlay);
                RefreshMilitaryData();
            };
            modal.Add(clearBtn);

            var dashboard = _root?.Q<VisualElement>("MilitaryDashboard");
            if (dashboard != null) dashboard.Add(overlay);
            else _root?.Add(overlay);
        }

        private void ShowLaunchOpModal()
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            var deterioratedRegions = country.regions.FindAll(r => r.security < 70f);
            if (deterioratedRegions.Count == 0)
            {
                NotificationManager.Instance?.AddNotification("THEATRE CALM: No regions require operations (Security is stable above 70%).", Color.yellow);
                return;
            }

            var overlay = new VisualElement();
            overlay.name = "OpModalOverlay";
            overlay.style.position = Position.Absolute;
            overlay.style.top = 0; overlay.style.bottom = 0; overlay.style.left = 0; overlay.style.right = 0;
            overlay.style.backgroundColor = new Color(8/255f, 12/255f, 22/255f, 0.96f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;

            var modal = new VisualElement();
            modal.AddToClassList("sub-panel");
            modal.style.width = 900;
            modal.style.maxHeight = Length.Percent(90);
            modal.style.paddingTop = 20; modal.style.paddingBottom = 20; modal.style.paddingLeft = 25; modal.style.paddingRight = 25;
            SetBorderWidth(modal.style, 1);
            SetBorderColor(modal.style, new Color(255/255f, 82/255f, 82/255f, 0.4f));
            overlay.Add(modal);

            var title = new Label("NEW MILITARY OPERATION");
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 18;
            title.style.marginBottom = 15;
            title.style.borderBottomWidth = 1;
            title.style.borderBottomColor = new Color(1,1,1,0.1f);
            title.style.paddingBottom = 10;
            modal.Add(title);

            var columns = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            modal.Add(columns);

            var leftCol = new ScrollView(ScrollViewMode.Vertical) { style = { width = Length.Percent(60), paddingRight = 20, borderRightWidth = 1, borderRightColor = new Color(1,1,1,0.1f) } };
            var rightCol = new ScrollView(ScrollViewMode.Vertical) { style = { width = Length.Percent(40), paddingLeft = 20 } };
            columns.Add(leftCol);
            columns.Add(rightCol);

            // -- LEFT COLUMN: CONFIGURATION --
            
            VisualElement MakeFieldLabel(string text) => new Label(text) { style = { color = Color.grey, fontSize = 10, marginTop = 10, marginBottom = 4, unityFontStyleAndWeight = FontStyle.Bold } };

            leftCol.Add(MakeFieldLabel("OPERATION NAME"));
            var nameInput = new TextField() { value = $"OPERATION {deterioratedRegions[0].regionName.ToUpper().Replace(" ", "")} SHIELD" };
            nameInput.style.height = 30;
            leftCol.Add(nameInput);

            leftCol.Add(MakeFieldLabel("OPERATION TYPE"));
            var typeDropdown = new DropdownField();
            var opTypes = System.Enum.GetValues(typeof(OpType));
            var typeChoices = new List<string>();
            foreach(var t in opTypes) typeChoices.Add(t.ToString());
            typeDropdown.choices = typeChoices;
            typeDropdown.value = typeChoices[0];
            typeDropdown.style.height = 30;
            leftCol.Add(typeDropdown);

            leftCol.Add(MakeFieldLabel("TARGET REGION"));
            var regionDropdown = new DropdownField();
            var regionChoices = new List<string>();
            foreach (var r in deterioratedRegions) regionChoices.Add(r.regionName);
            var gmWarSys = GameManager.Instance.WarSystem;
            if (gmWarSys != null && GameManager.Instance.allCountries != null)
            {
                foreach(var c in GameManager.Instance.allCountries)
                {
                    if (c != GameManager.Instance.playerCountry && gmWarSys.GetWarStage(c) == StateCraft.Interfaces.WarStage.Conflict)
                        regionChoices.Add(c.countryName);
                }
            }
            regionDropdown.choices = regionChoices;
            regionDropdown.value = regionChoices[0];
            regionDropdown.style.height = 30;
            leftCol.Add(regionDropdown);

            leftCol.Add(MakeFieldLabel("MISSION OBJECTIVE"));
            var objDropdown = new DropdownField();
            objDropdown.choices = new List<string> { "Destroy Terrorist Cells", "Secure Infrastructure", "Hostage Rescue", "Reconnaissance" };
            objDropdown.value = "Destroy Terrorist Cells";
            objDropdown.style.height = 30;
            leftCol.Add(objDropdown);

            var div1 = new VisualElement() { style = { height = 1, backgroundColor = new Color(1,1,1,0.1f), marginTop = 20, marginBottom = 10 } };
            leftCol.Add(div1);

            leftCol.Add(MakeFieldLabel("ASSIGN PERSONNEL"));
            int deployTroops = 0, deployRdf = 0;
            int deployTanks = 0, deployApcs = 0, deployHelis = 0, deployDrones = 0, deployBoats = 0;

            // -- RIGHT COLUMN: ESTIMATES & READINESS --
            var rightTopTitle = new Label("ESTIMATED RESULTS") { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 } };
            rightCol.Add(rightTopTitle);

            VisualElement MakeEstRow(string labelText, out Label valLabel)
            {
                var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6 } };
                r.Add(new Label(labelText) { style = { color = Color.grey, fontSize = 11 } });
                valLabel = new Label("-") { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } };
                r.Add(valLabel);
                return r;
            }

            rightCol.Add(MakeEstRow("Success Chance", out var lblChance));
            rightCol.Add(MakeEstRow("Daily Cost", out var lblCost));
            rightCol.Add(MakeEstRow("Duration", out var lblDuration));
            rightCol.Add(MakeEstRow("Casualty Risk", out var lblCasualties));
            rightCol.Add(MakeEstRow("Civilian Risk", out var lblCiv));
            rightCol.Add(MakeEstRow("Political Impact", out var lblPol));

            var div2 = new VisualElement() { style = { height = 1, backgroundColor = new Color(1,1,1,0.1f), marginTop = 20, marginBottom = 10 } };
            rightCol.Add(div2);

            var readTitle = new Label("MISSION READINESS") { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 } };
            rightCol.Add(readTitle);

            var readinessPanel = new VisualElement() { style = { marginBottom = 20 } };
            rightCol.Add(readinessPanel);

            var warningsPanel = new VisualElement();
            rightCol.Add(warningsPanel);

            // Re-eval function
            System.Action UpdateEstimates = () =>
            {
                float monthlyCost = 0.01f + (deployTroops / 1000f) * 0.01f + deployTanks * 0.005f + deployApcs * 0.002f + deployHelis * 0.008f + deployDrones * 0.005f + deployBoats * 0.04f + (deployRdf / 1000f) * 0.02f;
                float localCost = monthlyCost * country.exchangeRate;
                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
                
                float securityBoost = 1.0f + (deployTroops / 1000f) * 0.5f + deployTanks * 0.8f + deployApcs * 0.4f + deployHelis * 1.0f + deployBoats * 1.5f + (deployRdf / 1000f) * 3.5f + deployDrones * 1.2f;
                float baseChance = Mathf.Clamp(40f + securityBoost * 1.8f, 10f, 95f);
                int durationDays = Mathf.Max(5, Mathf.RoundToInt(60f - securityBoost * 2f));

                lblChance.text = $"{baseChance:F0}%";
                lblChance.style.color = baseChance >= 75f ? new Color(0, 0.9f, 0.47f) : (baseChance >= 50f ? new Color(1f, 0.76f, 0.03f) : new Color(1f, 0.32f, 0.32f));
                
                lblCost.text = $"{sym}{(localCost / 30f):F2}B";
                lblDuration.text = $"{durationDays} Days";

                lblCasualties.text = deployTroops < 2000 ? "HIGH" : (deployTanks > 5 ? "LOW" : "MEDIUM");
                lblCasualties.style.color = lblCasualties.text == "HIGH" ? Color.red : (lblCasualties.text == "LOW" ? Color.green : Color.yellow);
                
                lblCiv.text = deployHelis > 0 || deployTanks > 10 ? "MEDIUM" : "LOW";
                lblPol.text = baseChance > 70 ? "+3 Approval" : (baseChance < 40 ? "-5 Approval" : "Neutral");

                // Readiness Panel
                readinessPanel.Clear();
                VisualElement MakeReadinessRow(string text, bool ok)
                {
                    var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 } };
                    var dot = new VisualElement() { style = { width = 8, height = 8, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, marginRight = 8, backgroundColor = ok ? Color.green : (text.Contains("Available") ? Color.green : Color.yellow) } };
                    if (!ok) dot.style.backgroundColor = Color.red;
                    r.Add(dot);
                    r.Add(new Label(text) { style = { color = Color.white, fontSize = 10 } });
                    return r;
                }

                bool armyOk = deployTroops <= country.unitInfantry && deployTanks <= country.unitTanks;
                bool rdfOk = deployRdf <= country.rdfStats.personnel;
                bool airOk = deployHelis <= country.unitAttackHelis;
                
                readinessPanel.Add(MakeReadinessRow(armyOk ? "Army Ready" : "Army Understaffed", armyOk));
                if (deployRdf > 0) readinessPanel.Add(MakeReadinessRow(rdfOk ? "RDF Ready" : "RDF Understaffed", rdfOk));
                if (deployHelis > 0 || deployDrones > 0) readinessPanel.Add(MakeReadinessRow(airOk ? "Air Support Ready" : "Air Support Insufficient", airOk));
                readinessPanel.Add(MakeReadinessRow("Supplies Available", true));
                
                // Warnings
                warningsPanel.Clear();
                int totalDeployed = deployTroops + deployRdf + deployTanks + deployApcs + deployHelis + deployDrones + deployBoats;
                
                if (totalDeployed == 0) warningsPanel.Add(new Label("⚠ No forces assigned.") { style = { color = Color.yellow, fontSize = 10, marginTop = 4 } });
                if (!armyOk) warningsPanel.Add(new Label("⚠ Insufficient Army Personnel/Tanks.") { style = { color = Color.red, fontSize = 10, marginTop = 4 } });
                if (deployTroops > 0 && deployHelis == 0 && deployDrones == 0) warningsPanel.Add(new Label("⚠ Lack of Air Support increases casualty risk.") { style = { color = Color.yellow, fontSize = 10, marginTop = 4 } });
                if (baseChance < 50) warningsPanel.Add(new Label("⚠ Mission likely to fail.") { style = { color = Color.red, fontSize = 10, marginTop = 4, unityFontStyleAndWeight = FontStyle.Bold } });
            };

            // Stepper Helper
            void AddStepper(VisualElement parent, string name, int currentVal, int step, int maxAllowed, System.Action<int> setter)
            {
                var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 4 } };
                r.Add(new Label(name) { style = { color = Color.white, fontSize = 11, width = 120 } });
                
                var ctr = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                
                var btnMinus = new Button(() => { 
                    int n = Mathf.Max(0, currentVal - step); 
                    setter(n); UpdateEstimates(); 
                }) { text = "[-]" };
                btnMinus.style.width = 30; btnMinus.style.height = 20; btnMinus.style.backgroundColor = new Color(1,1,1,0.1f);
                btnMinus.style.color = Color.white; btnMinus.style.borderTopWidth = btnMinus.style.borderBottomWidth = btnMinus.style.borderLeftWidth = btnMinus.style.borderRightWidth = 0;
                
                var valLbl = new Label(currentVal.ToString("N0")) { style = { color = Color.white, fontSize = 11, unityTextAlign = TextAnchor.MiddleCenter, width = 50 } };
                
                var btnPlus = new Button(() => { 
                    int n = currentVal + step; 
                    setter(n); UpdateEstimates(); 
                }) { text = "[+]" };
                btnPlus.style.width = 30; btnPlus.style.height = 20; btnPlus.style.backgroundColor = new Color(1,1,1,0.1f);
                btnPlus.style.color = Color.white; btnPlus.style.borderTopWidth = btnPlus.style.borderBottomWidth = btnPlus.style.borderLeftWidth = btnPlus.style.borderRightWidth = 0;

                // Update closure for UI refresh
                var originalSetter = setter;
                setter = (val) => { originalSetter(val); valLbl.text = val.ToString("N0"); };

                ctr.Add(btnMinus); ctr.Add(valLbl); ctr.Add(btnPlus);
                r.Add(ctr);
                parent.Add(r);
            }

            AddStepper(leftCol, "Army Soldiers", deployTroops, 1000, country.unitInfantry, v => deployTroops = v);
            AddStepper(leftCol, "RDF Personnel", deployRdf, 500, country.rdfStats.personnel, v => deployRdf = v);

            var div3 = new VisualElement() { style = { height = 1, backgroundColor = new Color(1,1,1,0.1f), marginTop = 20, marginBottom = 10 } };
            leftCol.Add(div3);
            leftCol.Add(MakeFieldLabel("ASSIGN EQUIPMENT"));

            AddStepper(leftCol, "Tanks", deployTanks, 5, country.unitTanks, v => deployTanks = v);
            AddStepper(leftCol, "APCs", deployApcs, 10, country.unitAPCs, v => deployApcs = v);
            AddStepper(leftCol, "Helicopters", deployHelis, 2, country.unitAttackHelis, v => deployHelis = v);
            AddStepper(leftCol, "Drones", deployDrones, 4, country.unitUAVs, v => deployDrones = v);
            AddStepper(leftCol, "Patrol Boats", deployBoats, 1, country.unitFrigates, v => deployBoats = v);

            UpdateEstimates();

            // -- BOTTOM BAR --
            var actionRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 20, paddingTop = 15, borderTopWidth = 1, borderTopColor = new Color(1,1,1,0.1f) } };
            
            var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "CANCEL" };
            cancelBtn.AddToClassList("button-premium");
            cancelBtn.style.width = Length.Percent(30);
            cancelBtn.style.height = 40;
            cancelBtn.style.backgroundColor = new Color(1,1,1,0.1f);
            actionRow.Add(cancelBtn);

            var confirmBtn = new Button() { text = "LAUNCH OPERATION" };
            confirmBtn.AddToClassList("button-premium");
            confirmBtn.style.width = Length.Percent(65);
            confirmBtn.style.height = 40;
            confirmBtn.style.backgroundColor = new Color(0, 0.5f, 0.2f);
            confirmBtn.clicked += () =>
            {
                int totalDeployed = deployTroops + deployTanks + deployApcs + deployHelis + deployBoats + deployRdf + deployDrones;
                if (totalDeployed <= 0)
                {
                    NotificationManager.Instance?.AddNotification("INVALID DEPLOYMENT: You must deploy at least some troops or equipment!", Color.red);
                    return;
                }
                
                if (deployTroops > country.unitInfantry || deployTanks > country.unitTanks || deployRdf > country.rdfStats.personnel)
                {
                    NotificationManager.Instance?.AddNotification("INVALID DEPLOYMENT: You do not have enough assigned units available!", Color.red);
                    return;
                }

                DeployOperation(country, nameInput.value.ToUpper(), regionDropdown.value, typeDropdown.value, 
                    deployTroops, deployTanks, deployApcs, 0, deployHelis, deployBoats, 0, deployRdf, 0, deployDrones);
                overlay.parent?.Remove(overlay);
            };
            actionRow.Add(confirmBtn);

            modal.Add(actionRow);

            var dashboard = _root?.Q<VisualElement>("MilitaryDashboard");
            if (dashboard != null) dashboard.Add(overlay);
            else _root?.Add(overlay);
        }

        private MilitaryOperation _selectedOp = null;

        private void PopulateOperationsMasterList(CountryDataSO country, GameManager gm)
        {
            var opsMasterList = _root?.Q<ScrollView>("ops-master-list");
            var lblCount = _root?.Q<Label>("lbl-active-ops-count");
            var opsDetailView = _root?.Q<VisualElement>("ops-detail-view");
            var opsNoSelection = _root?.Q<VisualElement>("ops-no-selection");

            if (opsMasterList == null) return;
            opsMasterList.Clear();

            int count = country.activeOperations != null ? country.activeOperations.Count : 0;
            if (lblCount != null) lblCount.text = $"ACTIVE OPERATIONS ({count})";

            if (count == 0)
            {
                _selectedOp = null;
                if (opsDetailView != null) opsDetailView.style.display = DisplayStyle.None;
                if (opsNoSelection != null) opsNoSelection.style.display = DisplayStyle.Flex;
                return;
            }
            foreach (var op in country.activeOperations)
            {
                var card = new VisualElement();
                card.AddToClassList("ops-card");
                card.style.marginBottom = 10;
                card.style.paddingTop = 10; card.style.paddingBottom = 10; card.style.paddingLeft = 10; card.style.paddingRight = 10;

                if (_selectedOp == op)
                {
                    card.AddToClassList("active-op-card");
                }

                var title = new Label(op.opName) { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } };
                var subtitle = new Label($"{op.type} · {op.regionName}") { style = { color = Color.grey, fontSize = 9 } };
                card.Add(title); card.Add(subtitle);

                // Circular gauge mini mock
                var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginTop = 10 } };
                var progBarBg = new VisualElement() { style = { height = 4, backgroundColor = new Color(1,1,1,0.1f), flexGrow = 1, marginRight = 10, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } };
                var progBarFill = new VisualElement() { style = { height = 4, backgroundColor = new Color(0.14f, 0.93f, 0.34f), width = Length.Percent(75), borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } };
                progBarBg.Add(progBarFill);
                var pct = new Label("75%") { style = { color = Color.white, fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold } };
                row.Add(progBarBg); row.Add(pct);
                card.Add(row);

                var capturedOp = op;
                card.RegisterCallback<ClickEvent>(evt => {
                    _selectedOp = capturedOp;
                    PopulateOperationsMasterList(country, gm); // Refresh selection
                });
                
                opsMasterList.Add(card);
            }

            if (_selectedOp != null && country.activeOperations.Contains(_selectedOp))
            {
                if (opsDetailView != null) opsDetailView.style.display = DisplayStyle.Flex;
                if (opsNoSelection != null) opsNoSelection.style.display = DisplayStyle.None;
                SelectOperation(_selectedOp, country, gm);
            }
            else
            {
                if (opsDetailView != null) opsDetailView.style.display = DisplayStyle.None;
                if (opsNoSelection != null) opsNoSelection.style.display = DisplayStyle.Flex;
            }
            
            // Wire up launch button
            var btnLaunch = _root?.Q<Button>("btn-launch-new");
            if (btnLaunch != null)
            {
                btnLaunch.clicked -= ShowLaunchOpModal;
                btnLaunch.clicked += ShowLaunchOpModal;
            }
        }

        private void SelectOperation(MilitaryOperation op, CountryDataSO country, GameManager gm)
        {
            if (_root == null) return;
            
            _root.Q<Label>("det-op-name").text = op.opName.ToUpper();
            _root.Q<Label>("det-op-type").text = $"{op.type} · {op.regionName}";
            _root.Q<Label>("det-op-status").text = op.status.ToUpper();
            _root.Q<Label>("det-start-date").text = "Started: " + (gm?.TurnSystem != null ? gm.TurnSystem.CurrentDate : "Live");

            _root.Q<Label>("stat-region").text = op.regionName;
            _root.Q<Label>("stat-objective").text = "Neutralise Threats";
            _root.Q<Label>("stat-duration").text = op.turnsToDeploy > 0 ? $"Deploying ({op.turnsToDeploy} mo)" : "Active";
            _root.Q<Label>("stat-chance").text = "82%";
            _root.Q<Label>("stat-intel").text = "Good";
            
            var lblThreat = _root.Q<Label>("stat-threat");
            lblThreat.text = "Medium";
            lblThreat.style.color = new Color(1, 0.7f, 0);

            // Cost
            float opCostB = (op.costPerMonth / 1000f) * country.exchangeRate;
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            _root.Q<Label>("stat-cost").text = $"{sym}{opCostB:F2}B / mo";

            // Outlook
            _root.Q<Label>("stat-success-main").text = "82%";
            _root.Q<Label>("out-prob").text = "82%";
            _root.Q<Label>("out-losses").text = "Low";
            _root.Q<Label>("out-civ").text = "Low";
            _root.Q<Label>("out-pol").text = "+3 Approval";
            _root.Q<Label>("out-intel").text = "Good";

            // Forces
            var forcesList = _root.Q<ScrollView>("det-forces-list");
            forcesList.Clear();
            if (op.deployedTroops > 0) AddForceStat(forcesList, "Infantry", op.deployedTroops, country.unitInfantry + op.deployedTroops);
            if (op.deployedRDF > 0) AddForceStat(forcesList, "RDF Spec-Ops", op.deployedRDF, country.rdfStats.personnel + op.deployedRDF);
            if (op.deployedTanks > 0) AddForceStat(forcesList, "Tanks", op.deployedTanks, country.unitTanks + op.deployedTanks);
            if (op.deployedAPCs > 0) AddForceStat(forcesList, "APCs", op.deployedAPCs, country.unitAPCs + op.deployedAPCs);

            // Equipment
            var equipList = _root.Q<ScrollView>("det-equip-list");
            equipList.Clear();
            if (op.deployedFighters > 0) AddForceStat(equipList, "Fighter Jets", op.deployedFighters, country.unitFighters + op.deployedFighters);
            if (op.deployedBombers > 0) AddForceStat(equipList, "Bombers", op.deployedBombers, country.unitBombers + op.deployedBombers);
            if (op.deployedUAVs > 0) AddForceStat(equipList, "UAVs", op.deployedUAVs, country.unitUAVs + op.deployedUAVs);
            if (op.deployedHelis > 0) AddForceStat(equipList, "Attack Helis", op.deployedHelis, country.unitAttackHelis + op.deployedHelis);
            if (op.deployedFrigates > 0) AddForceStat(equipList, "Frigates", op.deployedFrigates, country.unitFrigates + op.deployedFrigates);
            if (op.deployedSubmarines > 0) AddForceStat(equipList, "Submarines", op.deployedSubmarines, country.unitSubmarines + op.deployedSubmarines);

            // Status Bars
            var statusList = _root.Q<ScrollView>("det-status-list");
            statusList.Clear();
            AddStatusBar(statusList, "Mission Progress", 65);
            AddStatusBar(statusList, "Area Controlled", 40);
            AddStatusBar(statusList, "Enemy Strength", 85, true);

            // Resources
            var resList = _root.Q<ScrollView>("det-res-list");
            resList.Clear();
            AddResourceBar(resList, "Fuel Reserves", 8, 10);
            AddResourceBar(resList, "Ammo Reserves", 6, 10);
            AddResourceBar(resList, "Medical Supplies", 9, 10);

            // Actions
            var btnReinforce = _root.Q<Button>("btn-action-reinforce");
            if (btnReinforce != null)
            {
                btnReinforce.clicked -= () => ShowReinforceModal(op);
                btnReinforce.clicked += () => ShowReinforceModal(op);
            }

            var btnAbort = _root.Q<Button>("btn-op-abort");
            if (btnAbort != null)
            {
                btnAbort.clicked -= AbortOperation;
                btnAbort.clicked += AbortOperation;
            }
        }

        private void AbortOperation()
        {
            var op = _selectedOp;
            var country = GameManager.Instance?.playerCountry;
            if (op == null || country == null) return;

            country.unitInfantry += op.deployedTroops;
            country.rdfStats.personnel += op.deployedRDF;
            country.unitAttackHelis += op.deployedHelis;
            country.unitTanks += op.deployedTanks;
            country.unitAPCs += op.deployedAPCs;
            country.unitFighters += op.deployedFighters;
            country.unitBombers += op.deployedBombers;
            country.unitUAVs += op.deployedUAVs;
            country.unitFrigates += op.deployedFrigates;
            country.unitSubmarines += op.deployedSubmarines;

            country.activeOperations.Remove(op);
            _selectedOp = null;
            NotificationManager.Instance?.AddNotification($"TERMINATED: {op.opName} decommissioned. Forces returned.", Color.yellow);
            RefreshMilitaryData();
        }

        private void AddForceStat(VisualElement container, string name, int deployed, int max)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6 } };
            row.Add(new Label(name) { style = { color = Color.white, fontSize = 10 } });
            
            var right = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var barBg = new VisualElement() { style = { width = 50, height = 4, backgroundColor = new Color(1,1,1,0.1f), marginRight = 8, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } };
            float pct = max > 0 ? (float)deployed / max : 0;
            var barFill = new VisualElement() { style = { width = Length.Percent(pct * 100), height = 4, backgroundColor = new Color(0.14f, 0.93f, 0.34f), borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } };
            barBg.Add(barFill);
            
            right.Add(barBg);
            right.Add(new Label($"{deployed:N0}") { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, width = 40, unityTextAlign = TextAnchor.MiddleRight } });
            row.Add(right);
            container.Add(row);
        }

        private void AddStatusBar(VisualElement container, string name, int pct, bool isWarning = false)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
            row.Add(new Label(name) { style = { color = Color.grey, fontSize = 10, width = 100 } });
            
            var barBg = new VisualElement() { style = { flexGrow = 1, height = 4, backgroundColor = new Color(1,1,1,0.1f), marginRight = 8, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } };
            var barFill = new VisualElement() { style = { width = Length.Percent(pct), height = 4, backgroundColor = isWarning ? new Color(1, 0.7f, 0) : new Color(0.14f, 0.93f, 0.34f), borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } };
            barBg.Add(barFill);
            row.Add(barBg);
            row.Add(new Label($"{pct}%") { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, width = 30, unityTextAlign = TextAnchor.MiddleRight } });
            container.Add(row);
        }

        private void AddResourceBar(VisualElement container, string name, int filled, int total)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
            row.Add(new Label(name) { style = { color = Color.grey, fontSize = 10, width = 100 } });
            
            var blocks = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexGrow = 1, marginRight = 8 } };
            for(int i=0; i<total; i++)
            {
                var b = new VisualElement() { style = { width = 8, height = 12, backgroundColor = i < filled ? new Color(0.14f, 0.93f, 0.34f) : new Color(1,1,1,0.1f), marginRight = 2, borderTopLeftRadius = 1, borderTopRightRadius = 1, borderBottomLeftRadius = 1, borderBottomRightRadius = 1 } };
                blocks.Add(b);
            }
            row.Add(blocks);
            container.Add(row);
        }
    }
}
