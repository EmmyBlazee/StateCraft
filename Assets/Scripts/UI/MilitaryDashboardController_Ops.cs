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
        private void PopulateThreatList(CountryDataSO country)
        {
            if (_threatList == null) return;
            _threatList.Clear();

            if (country.activeRecommendations == null || country.activeRecommendations.Count == 0)
            {
                var noRecs = new Label("No active recommendations. Regions are stable.") 
                { 
                    style = { color = Color.grey, fontSize = 10, unityFontStyleAndWeight = FontStyle.Italic, marginTop = 10, unityTextAlign = TextAnchor.MiddleCenter } 
                };
                _threatList.Add(noRecs);
                return;
            }

            foreach (var rec in country.activeRecommendations)
            {
                var card = new VisualElement();
                Color borderCol = rec.urgency == "High" ? new Color(1f, 0.22f, 0.22f, 0.8f) 
                                : rec.urgency == "Medium" ? new Color(1f, 0.6f, 0f, 0.8f)    
                                : new Color(0.14f, 0.93f, 0.34f, 0.8f);                      
                
                card.style.backgroundColor = new Color(0.1f, 0.15f, 0.2f, 0.6f);
                card.style.borderLeftWidth = 4;
                card.style.borderLeftColor = borderCol;
                card.style.borderTopWidth = card.style.borderBottomWidth = card.style.borderRightWidth = 1;
                card.style.borderTopColor = card.style.borderBottomColor = card.style.borderRightColor = new Color(1,1,1,0.08f);
                card.style.borderTopLeftRadius = card.style.borderTopRightRadius = card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 4;
                card.style.paddingTop = card.style.paddingBottom = card.style.paddingLeft = card.style.paddingRight = 10;
                card.style.marginBottom = 10;

                var headerRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 } };
                var regionLbl = new Label(rec.regionName.ToUpper()) { style = { color = Color.white, fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold } };
                var urgencyBadge = new Label(rec.urgency.ToUpper()) { style = { color = borderCol, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } };
                headerRow.Add(regionLbl);
                headerRow.Add(urgencyBadge);
                card.Add(headerRow);

                var threatLbl = new Label($"{rec.threatType} Threat") { style = { color = borderCol, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 } };
                card.Add(threatLbl);

                var descLbl = new Label(rec.justification) { style = { color = new Color(0.75f, 0.8f, 0.85f), fontSize = 12, whiteSpace = WhiteSpace.Normal, marginBottom = 8 } };
                card.Add(descLbl);

                string recommendedMissionId = "counter_insurgency";
                string recommendedMissionName = "Counter-Insurgency Sweep";
                if (rec.threatType == "Piracy")
                {
                    recommendedMissionId = "naval_patrol";
                    recommendedMissionName = "Naval Patrol";
                }
                else if (rec.threatType == "Terrorist Presence")
                {
                    recommendedMissionId = "ground_assault";
                    recommendedMissionName = "Ground Assault";
                }

                string constraintWarning = "";
                bool resourcesAvailable = true;
                if (rec.threatType == "Piracy")
                {
                    int availableFrigates = country.unitFrigates;
                    if (availableFrigates <= 0)
                    {
                        constraintWarning = "⚠ 0 Frigates available in reserve";
                        resourcesAvailable = false;
                    }
                    else
                    {
                        constraintWarning = $"✓ {availableFrigates} Frigate(s) available";
                    }
                }
                else if (rec.threatType == "Terrorist Presence")
                {
                    int availableArmy = country.unitInfantry;
                    if (availableArmy < 500)
                    {
                        constraintWarning = $"⚠ only {availableArmy} Army troops available (500 req)";
                        resourcesAvailable = false;
                    }
                    else
                    {
                        constraintWarning = $"✓ {availableArmy} Army troops available";
                    }
                }
                else
                {
                    int availableArmy = country.unitInfantry;
                    if (availableArmy < 200)
                    {
                        constraintWarning = $"⚠ only {availableArmy} Army troops available (200 req)";
                        resourcesAvailable = false;
                    }
                    else
                    {
                        constraintWarning = $"✓ {availableArmy} Army troops available";
                    }
                }

                var resourceLbl = new Label(constraintWarning) { style = { color = resourcesAvailable ? new Color(0.14f, 0.93f, 0.34f) : new Color(1f, 0.22f, 0.22f), fontSize = 11, marginBottom = 8 } };
                card.Add(resourceLbl);

                var btnPlan = new Button(() => {
                    ShowLaunchOpModal(rec.regionName, recommendedMissionId);
                }) { text = rec.isAddressed ? "OPERATION ACTIVE" : $"PLAN {recommendedMissionName.ToUpper()}" };
                btnPlan.style.height = 28;
                btnPlan.style.fontSize = 11;
                btnPlan.style.unityFontStyleAndWeight = FontStyle.Bold;
                if (rec.isAddressed)
                {
                    btnPlan.SetEnabled(false);
                    btnPlan.style.backgroundColor = new Color(0.15f, 0.2f, 0.25f, 0.8f);
                    btnPlan.style.color = Color.grey;
                }
                else
                {
                    btnPlan.style.backgroundColor = resourcesAvailable ? new Color(0.08f, 0.45f, 0.22f, 0.9f) : new Color(0.35f, 0.15f, 0.15f, 0.9f);
                    btnPlan.style.color = Color.white;
                }
                card.Add(btnPlan);

                _threatList.Add(card);
            }
        }

        private void ShowBlockadeModal()
        {
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

            var btnFullMatrix = new Button(() => ShowRegionalStabilityModal(country, gm)) { text = "🌐 VIEW FULL STABILITY MATRIX" };
            btnFullMatrix.style.backgroundColor = new Color(0.1f, 0.6f, 1f, 0.2f);
            btnFullMatrix.style.color = new Color(0f, 0.85f, 1f);
            btnFullMatrix.style.fontSize = 12;
            btnFullMatrix.style.unityFontStyleAndWeight = FontStyle.Bold;
            btnFullMatrix.style.marginBottom = 10;
            btnFullMatrix.style.height = 30;
            _theatreStatusList.Add(btnFullMatrix);

            foreach (var region in country.regions)
            {
                string status = region.security >= 70f ? "SECURE" : (region.security >= 45f ? "CONTESTED" : "CRITICAL");
                Color statusCol = region.security >= 70f ? new Color(0.14f, 0.93f, 0.34f)
                    : (region.security >= 45f ? new Color(0.9f, 0.6f, 0f) : new Color(1f, 0.3f, 0.3f));

                var card = new VisualElement() { style = { backgroundColor = new Color(0.08f, 0.11f, 0.16f), marginBottom = 8, borderLeftWidth = 3, borderLeftColor = statusCol } };
                SetBorderRadius(card.style, 4);
                card.style.paddingTop = card.style.paddingBottom = card.style.paddingLeft = card.style.paddingRight = 10;
                
                var topRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 } };
                topRow.Add(new Label(region.regionName.ToUpper()) { style = { color = Color.white, fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold } });
                topRow.Add(new Label(status) { style = { color = statusCol, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                card.Add(topRow);

                var statsRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                statsRow.Add(new Label($"Stab: {region.stability:F0}%") { style = { color = new Color(0.8f, 0.88f, 0.95f), fontSize = 12 } });
                statsRow.Add(new Label($"Sec: {region.security:F0}%") { style = { color = new Color(0.8f, 0.88f, 0.95f), fontSize = 12 } });
                if (region.insurgencyLevel > 0)
                {
                    statsRow.Add(new Label($"Threat: {region.insurgencyLevel:F0}%") { style = { color = new Color(1f, 0.35f, 0.35f), fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                }
                card.Add(statsRow);

                _theatreStatusList.Add(card);
            }
        }

        private void ShowRegionalStabilityModal(CountryDataSO country, GameManager gm)
        {
            var root = _dashboardRoot ?? _panelOps?.panel?.visualTree;
            if (root == null) return;

            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.top = overlay.style.bottom = overlay.style.left = overlay.style.right = 0;
            overlay.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;
            root.Add(overlay);

            var modal = new VisualElement();
            modal.style.width = 900;
            modal.style.maxHeight = Length.Percent(85);
            modal.style.backgroundColor = new Color(10/255f, 16/255f, 24/255f);
            modal.style.borderLeftWidth = modal.style.borderRightWidth = modal.style.borderTopWidth = modal.style.borderBottomWidth = 1;
            modal.style.borderLeftColor = modal.style.borderRightColor = modal.style.borderTopColor = modal.style.borderBottomColor = new Color(0f, 0.8f, 1f, 0.4f);
            SetBorderRadius(modal.style, 8);
            modal.style.paddingLeft = modal.style.paddingRight = modal.style.paddingTop = modal.style.paddingBottom = 22;
            overlay.Add(modal);

            var headerRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 16, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.1f), paddingBottom = 12 } };
            headerRow.Add(new Label($"🌐 REGIONAL STABILITY & SECURITY MATRIX — {country.countryName.ToUpper()}") { style = { color = new Color(0f, 0.85f, 1f), fontSize = 18, unityFontStyleAndWeight = FontStyle.Bold } });
            var closeBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "✕" };
            closeBtn.style.backgroundColor = Color.clear; closeBtn.style.color = Color.grey; closeBtn.style.fontSize = 18;
            SetBorderWidth(closeBtn.style, 0);
            headerRow.Add(closeBtn);
            modal.Add(headerRow);

            var scroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1, height = 480 } };
            modal.Add(scroll);

            var tHead = new VisualElement() { style = { flexDirection = FlexDirection.Row, backgroundColor = new Color(0.06f, 0.09f, 0.14f), marginBottom = 10 } };
            SetBorderRadius(tHead.style, 4);
            tHead.style.paddingTop = tHead.style.paddingBottom = tHead.style.paddingLeft = tHead.style.paddingRight = 10;
            tHead.Add(new Label("REGION") { style = { color = Color.grey, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, width = 150 } });
            tHead.Add(new Label("STABILITY") { style = { color = Color.grey, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, width = 150 } });
            tHead.Add(new Label("SECURITY") { style = { color = Color.grey, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, width = 100 } });
            tHead.Add(new Label("INSURGENCY") { style = { color = Color.grey, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, width = 120 } });
            tHead.Add(new Label("INTEL") { style = { color = Color.grey, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, width = 90 } });
            tHead.Add(new Label("ACTION") { style = { color = Color.grey, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 } });
            scroll.Add(tHead);

            var sortedRegions = new List<RegionDataSO>(country.regions);
            sortedRegions.Sort((a, b) => a.stability.CompareTo(b.stability));

            foreach (var r in sortedRegions)
            {
                var rRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, backgroundColor = new Color(0.08f, 0.11f, 0.16f), marginBottom = 8 } };
                SetBorderRadius(rRow.style, 4);
                rRow.style.paddingTop = rRow.style.paddingBottom = rRow.style.paddingLeft = rRow.style.paddingRight = 10;
                
                rRow.Add(new Label(r.regionName) { style = { color = Color.white, fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, width = 150 } });

                var stabBox = new VisualElement() { style = { width = 150, flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                Color stabCol = r.stability >= 70f ? new Color(0.14f, 0.93f, 0.34f) : r.stability >= 45f ? new Color(1f, 0.7f, 0.2f) : new Color(1f, 0.35f, 0.35f);
                stabBox.Add(new Label($"{r.stability:F0}%") { style = { color = stabCol, fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, width = 40 } });
                var barBg = new VisualElement() { style = { height = 8, flexGrow = 1, backgroundColor = new Color(1,1,1,0.1f) } };
                SetBorderRadius(barBg.style, 4);
                var barFill = new VisualElement() { style = { height = 8, width = Length.Percent(r.stability), backgroundColor = stabCol } };
                SetBorderRadius(barFill.style, 4);
                barBg.Add(barFill);
                stabBox.Add(barBg);
                rRow.Add(stabBox);

                rRow.Add(new Label($"{r.security:F0}%") { style = { color = Color.white, fontSize = 13, width = 100 } });

                string insTxt = r.insurgencyLevel > 0 ? $"{r.insurgencyLevel:F0}% [{r.GetEntrenchment()}]" : "CLEAR";
                Color insCol = r.insurgencyLevel > 50f ? new Color(1f, 0.3f, 0.3f) : r.insurgencyLevel > 0 ? new Color(1f, 0.7f, 0.2f) : Color.grey;
                rRow.Add(new Label(insTxt) { style = { color = insCol, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, width = 120 } });

                rRow.Add(new Label($"{r.intelCoverage:F0}%") { style = { color = new Color(0f, 0.85f, 1f), fontSize = 13, width = 90 } });

                var btnLaunchHere = new Button(() => {
                    overlay.parent?.Remove(overlay);
                    ShowLaunchOpModal(r.regionName);
                }) { text = "LAUNCH OP" };
                btnLaunchHere.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f);
                btnLaunchHere.style.color = Color.black;
                btnLaunchHere.style.fontSize = 11;
                btnLaunchHere.style.unityFontStyleAndWeight = FontStyle.Bold;
                btnLaunchHere.style.height = 28;
                rRow.Add(btnLaunchHere);

                scroll.Add(rRow);
            }
        }

        private void PopulateOperations(CountryDataSO country, GameManager gm)
        {
            if (_activeOpsList == null) return;
            _activeOpsList.Clear();

            if (gm?.WarSystem != null && gm.allCountries != null)
            {
                bool atWar = false;
                foreach (var other in gm.allCountries)
                {
                    if (other == country) continue;
                    if (gm.WarSystem.GetWarStage(other) != StateCraft.Interfaces.WarStage.Conflict) continue;
                    atWar = true;

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

                    var enemyRow = new VisualElement();
                    enemyRow.style.flexDirection  = FlexDirection.Row;
                    enemyRow.style.justifyContent = Justify.SpaceBetween;
                    enemyRow.style.marginBottom   = 8;
                    enemyRow.Add(new Label($"Enemy exhaustion: {other.warExhaustion:F0}%  |  Enemy will: {other.warWillToFight:F0}%  |  Enemy KIA: {other.totalKIA:N0}") {
                        style = { color = new Color(0.55f, 0.55f, 0.65f), fontSize = 8 }
                    });
                    frontCard.Add(enemyRow);

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
                
                float opCostB = (op.costPerMonth / 1000f) * country.exchangeRate;
                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
                footer.Add(new Label($"{sym}{opCostB:F2}B/mo") { style = { color = Color.grey, fontSize = 8 } });
                footer.Add(new Label($"{op.deployedTroops:N0} INF") { style = { color = Color.grey, fontSize = 8 } });
                card.Add(footer);

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
                        var contrList = captureOp.GetContributionsMigrated();
                        foreach (var c in contrList)
                        {
                            MilitaryOperation.ReturnToReserves(country, c.branch, c.GetUnitCountsDict(), c.personnelAssigned);
                        }

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
                        GameManager.Instance?.SaveGame();
                    };
                    actionRow.Add(terminateBtn);
                    card.Add(actionRow);
                }

                _activeOpsList.Add(card);
            }

            var warStage = gm.WarSystem?.GetWarStage(country) ?? WarStage.Peace;
            if (warStage == WarStage.Conflict)
            {
                var warActHeader = new Label("⚔ WARTIME QUICK ACTIONS") { style = { color = new Color(255/255f, 82/255f, 82/255f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 14, marginBottom = 6 } };
                _activeOpsList.Add(warActHeader);

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
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(country);
                        UnityEditor.AssetDatabase.SaveAssets();
#endif

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
            float currentSuccess = CalculateOpSuccess(op, country);
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;

            var overlay = new VisualElement { style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0, backgroundColor = new Color(0,0,0,0.88f), justifyContent = Justify.Center, alignItems = Align.Center } };
            var modal = new VisualElement { style = { width = 520, paddingBottom = 24, paddingTop = 24, paddingLeft = 24, paddingRight = 24, backgroundColor = new Color(0.07f,0.1f,0.15f,1f), borderTopLeftRadius=8, borderTopRightRadius=8, borderBottomLeftRadius=8, borderBottomRightRadius=8 } };
            SetBorderColor(modal.style, new Color(0.14f, 0.93f, 0.34f, 0.4f));
            SetBorderWidth(modal.style, 1);
            overlay.Add(modal);

            modal.Add(new Label($"REINFORCE: {op.opName}") { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });
            modal.Add(new Label($"{op.type}  ·  {op.regionName}") { style = { color = new Color(0.5f,0.55f,0.65f), fontSize = 10, marginBottom = 16 } });

            var summaryRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 16, backgroundColor = new Color(0.04f,0.06f,0.1f,1f), borderTopLeftRadius=6, borderTopRightRadius=6, borderBottomLeftRadius=6, borderBottomRightRadius=6, paddingTop=10, paddingBottom=10, paddingLeft=12, paddingRight=12 } };

            void AddSummaryCol(VisualElement parent, string heading, string value, Color col)
            {
                var col_ = new VisualElement { style = { flexGrow = 1, alignItems = Align.Center } };
                col_.Add(new Label(heading) { style = { color = new Color(0.4f,0.45f,0.5f), fontSize = 8, marginBottom = 2 } });
                col_.Add(new Label(value) { style = { color = col, fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold } });
                parent.Add(col_);
            }

            AddSummaryCol(summaryRow, "CURRENT SUCCESS", $"{currentSuccess:F0}%", Color.white);
            var arrowLbl = new Label("→") { style = { color = new Color(0.3f,0.35f,0.4f), fontSize = 18, alignSelf = Align.Center, marginLeft = 6, marginRight = 6 } };
            summaryRow.Add(arrowLbl);
            Label newSuccessLbl;
            AddSummaryCol(summaryRow, "NEW SUCCESS", "—", new Color(0.14f,0.93f,0.34f));
            newSuccessLbl = summaryRow.Q<Label>() is Label ll ? summaryRow[summaryRow.childCount - 1].Q<Label>() : null;
            var lastCol = summaryRow[summaryRow.childCount - 1];
            var newSuccLabel = lastCol.childCount > 1 ? lastCol[1] as Label : null;
            modal.Add(summaryRow);

            Label addlUpkeepLbl;
            var upkeepRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 } };
            upkeepRow.Add(new Label("Additional Upkeep / mo:") { style = { color = new Color(0.5f,0.55f,0.65f), fontSize = 10 } });
            addlUpkeepLbl = new Label("—") { style = { color = new Color(1f,0.7f,0.2f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } };
            upkeepRow.Add(addlUpkeepLbl);
            modal.Add(upkeepRow);

            Label estCasLbl;
            var casRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 16 } };
            casRow.Add(new Label("Estimated Casualties:") { style = { color = new Color(0.5f,0.55f,0.65f), fontSize = 10 } });
            estCasLbl = new Label("—") { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } };
            casRow.Add(estCasLbl);
            modal.Add(casRow);

            var scroll = new ScrollView { style = { maxHeight = 220, marginBottom = 16 } };
            modal.Add(scroll);
            scroll.Add(new Label("ASSIGN ADDITIONAL FORCES") { style = { color = new Color(0.4f,0.45f,0.5f), fontSize = 8, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 8 } });

            int addTroops = 0, addTanks = 0, addApcs = 0, addUavs = 0;

            void RefreshSummary()
            {
                var tempOp = new StateCraft.Data.MilitaryOperation {
                    deployedTroops = op.deployedTroops + addTroops,
                    deployedTanks  = op.deployedTanks  + addTanks,
                    deployedAPCs   = op.deployedAPCs   + addApcs,
                    deployedUAVs   = op.deployedUAVs   + addUavs,
                    deployedRDF    = op.deployedRDF, deployedFighters = op.deployedFighters,
                    deployedBombers = op.deployedBombers, deployedHelis = op.deployedHelis,
                    deployedFrigates = op.deployedFrigates, deployedSubmarines = op.deployedSubmarines,
                    morale = op.morale, enemyStrengthEst = op.enemyStrengthEst
                };
                float newSuccess = CalculateOpSuccess(tempOp, country);
                if (newSuccLabel != null) { newSuccLabel.text = $"{newSuccess:F0}%"; newSuccLabel.style.color = newSuccess > currentSuccess ? new Color(0.14f,0.93f,0.34f) : new Color(1f,0.5f,0.1f); }

                float addedCost = ((addTroops / 1000f) * 0.01f + addTanks * 0.005f + addApcs * 0.002f + addUavs * 0.005f) * country.exchangeRate;
                addlUpkeepLbl.text = addedCost < 0.001f ? "—" : $"{sym}{addedCost:F2}B";
                int totalAdded = addTroops + addTanks + addApcs + addUavs;
                estCasLbl.text = totalAdded == 0 ? "—" : totalAdded < 2000 ? "Low–Medium" : "Low";
            }

            void AddReinfSlider(string label, int avail, System.Action<int> setter)
            {
                if (avail <= 0) return;
                var lbl = new Label($"{label}: 0 / {avail:N0}") { style = { color = Color.grey, fontSize = 9, marginBottom = 3 } };
                scroll.Add(lbl);
                var sl = new SliderInt(0, avail) { style = { marginBottom = 10 } };
                sl.RegisterValueChangedCallback(evt => { setter(evt.newValue); lbl.text = $"{label}: {evt.newValue:N0} / {avail:N0}"; RefreshSummary(); });
                scroll.Add(sl);
            }

            AddReinfSlider("Infantry",  country.unitInfantry, v => addTroops = v);
            AddReinfSlider("Tanks",     country.unitTanks,    v => addTanks  = v);
            AddReinfSlider("APCs",      country.unitAPCs,     v => addApcs   = v);
            AddReinfSlider("Tactical UAVs", country.unitUAVs, v => addUavs   = v);

            RefreshSummary();

            var actionRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };

            var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "CANCEL" };
            cancelBtn.AddToClassList("button-premium"); cancelBtn.style.width = Length.Percent(30); cancelBtn.style.height = 32;
            actionRow.Add(cancelBtn);

            var confirmBtn = new Button { text = "DEPLOY REINFORCEMENTS" };
            confirmBtn.AddToClassList("button-premium");
            confirmBtn.style.backgroundColor = new Color(0.05f, 0.4f, 0.2f, 0.9f);
            confirmBtn.style.width = Length.Percent(65); confirmBtn.style.height = 32;
            confirmBtn.clicked += () =>
            {
                int total = addTroops + addTanks + addApcs + addUavs;
                if (total <= 0) { NotificationManager.Instance?.AddNotification("Select units to reinforce.", Color.red); return; }

                op.deployedTroops += addTroops; op.deployedTanks += addTanks;
                op.deployedAPCs   += addApcs;   op.deployedUAVs  += addUavs;
                country.unitInfantry = Mathf.Max(0, country.unitInfantry - addTroops);
                country.unitTanks    = Mathf.Max(0, country.unitTanks    - addTanks);
                country.unitAPCs     = Mathf.Max(0, country.unitAPCs     - addApcs);
                country.unitUAVs     = Mathf.Max(0, country.unitUAVs     - addUavs);
                op.securityBoostRate += (addTroops / 1000f) * 0.5f + addTanks * 0.8f + addApcs * 0.4f + addUavs * 1.2f;

                overlay.parent?.Remove(overlay);
                NotificationManager.Instance?.AddNotification($"REINFORCEMENTS DEPLOYED: {total:N0} units added to {op.opName}.", Color.cyan);
                
                string reinfMsg = $"REINFORCED: Added {total:N0} units to Operation '{op.opName}' in {op.regionName} (Infantry: {addTroops}, Tanks: {addTanks}, APCs: {addApcs}, UAVs: {addUavs}).";
                LogOperationalEvent(country, reinfMsg);
                
                RefreshMilitaryData();
            };
            actionRow.Add(confirmBtn);
            modal.Add(actionRow);

            if (_dashboardRoot != null) _dashboardRoot.Add(overlay);
        }

        private void ShowCompletedOpsModal()
        {
            var country = Core.GameManager.Instance?.playerCountry;
            if (country == null) return;

            var overlay = new VisualElement { style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0, backgroundColor = new Color(0,0,0,0.88f), justifyContent = Justify.Center, alignItems = Align.Center } };
            var modal = new VisualElement { style = { width = 600, maxHeight = Length.Percent(88), paddingBottom = 24, paddingTop = 24, paddingLeft = 24, paddingRight = 24, backgroundColor = new Color(0.07f,0.1f,0.15f,1f), borderTopLeftRadius=8, borderTopRightRadius=8, borderBottomLeftRadius=8, borderBottomRightRadius=8 } };
            SetBorderColor(modal.style, new Color(0.2f,0.25f,0.35f,0.6f));
            SetBorderWidth(modal.style, 1);
            overlay.Add(modal);

            var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 16 } };
            headerRow.Add(new Label("COMPLETED OPERATIONS") { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold } });
            var closeBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "×" };
            closeBtn.style.width = 30; closeBtn.style.height = 30; closeBtn.style.fontSize = 18;
            closeBtn.AddToClassList("button-premium");
            headerRow.Add(closeBtn);
            modal.Add(headerRow);

            var scroll = new ScrollView { style = { flexGrow = 1 } };
            modal.Add(scroll);

            if (country.completedOperations == null || country.completedOperations.Count == 0)
            {
                scroll.Add(new Label("No completed operations on record.") { style = { color = Color.grey, marginTop = 20, unityTextAlign = TextAnchor.MiddleCenter } });
            }
            else
            {
                var list = new System.Collections.Generic.List<CompletedMilitaryOperation>(country.completedOperations);
                list.Reverse();

                foreach (var cop in list)
                {
                    Color outcomeCol = cop.outcome == "Victory" ? new Color(0.14f,0.93f,0.34f)
                                    : cop.outcome == "Defeat"  ? new Color(1f,0.3f,0.3f)
                                    : new Color(0.9f,0.6f,0.1f);

                    var card = new VisualElement { style = { backgroundColor = new Color(0.05f,0.07f,0.12f,1f), borderTopLeftRadius=6, borderTopRightRadius=6, borderBottomLeftRadius=6, borderBottomRightRadius=6, paddingTop=12, paddingBottom=12, paddingLeft=12, paddingRight=12, marginBottom=10 } };
                    SetBorderWidth(card.style, 1);
                    SetBorderColor(card.style, new Color(outcomeCol.r, outcomeCol.g, outcomeCol.b, 0.3f));

                    var topRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 } };
                    topRow.Add(new Label(cop.opName) { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                    var outcomePill = new Label(cop.outcome ?? "Unknown");
                    outcomePill.style.color = outcomeCol;
                    outcomePill.style.fontSize = 9;
                    outcomePill.style.unityFontStyleAndWeight = FontStyle.Bold;
                    outcomePill.style.backgroundColor = new Color(outcomeCol.r, outcomeCol.g, outcomeCol.b, 0.15f);
                    outcomePill.style.paddingLeft = 8; outcomePill.style.paddingRight = 8;
                    outcomePill.style.paddingTop = 2; outcomePill.style.paddingBottom = 2;
                    outcomePill.style.borderTopLeftRadius = outcomePill.style.borderTopRightRadius = outcomePill.style.borderBottomLeftRadius = outcomePill.style.borderBottomRightRadius = 3;
                    topRow.Add(outcomePill);
                    card.Add(topRow);

                    card.Add(new Label($"{cop.type}  ·  {cop.regionName}  ·  {cop.durationMonths} months  ·  {cop.dateCompleted}") { style = { color = new Color(0.5f,0.55f,0.65f), fontSize = 9, marginBottom = 8 } });

                    var statsGrid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
                    void AddStat(string k, string v)
                    {
                        var s = new VisualElement { style = { marginRight = 20, marginBottom = 4 } };
                        s.Add(new Label(k) { style = { color = new Color(0.4f,0.45f,0.5f), fontSize = 8 } });
                        s.Add(new Label(v) { style = { color = Color.white, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                        statsGrid.Add(s);
                    }
                    AddStat("Casualties",    $"{cop.casualtiesSustained:N0}");
                    AddStat("Equipment Lost", $"{cop.equipmentLost:N0}");
                    AddStat("Territory",     $"{cop.territoryCaptured:F0}%");
                    AddStat("Pol. Impact",   cop.politicalImpact >= 0 ? $"+{cop.politicalImpact} Approval" : $"{cop.politicalImpact} Approval");
                    if (!string.IsNullOrEmpty(cop.rewards)) AddStat("Rewards", cop.rewards);
                    card.Add(statsGrid);
                    scroll.Add(card);
                }
            }

            if (_dashboardRoot != null) _dashboardRoot.Add(overlay);
        }

        private void DeployOperation(CountryDataSO country, string opName, string regionName, string missionId, List<BranchContribution> contributions)
        {
            float monthlyCost = MilitaryOperation.CalculateUpkeep(contributions);
            float securityBoostRate = MilitaryOperation.CalculateSecurityBoost(country, missionId, contributions);

            foreach (var c in contributions)
            {
                MilitaryOperation.DeductFromReserves(country, c.branch, c.GetUnitCountsDict(), c.personnelAssigned);
            }

            var op = new MilitaryOperation
            {
                operationId = System.Guid.NewGuid().ToString(),
                missionId = missionId,
                opName = opName,
                regionName = regionName,
                type = missionId,
                status = "Active",
                costPerMonth = monthlyCost * 1000f,
                securityBoostRate = securityBoostRate,
                turnsToDeploy = 1,
                effectiveTroopStrength = 100f,
                morale = 80f,
                contributions = contributions
            };

            op.objective = missionId == "ground_assault" ? "Conduct offensive land operations" :
                           missionId == "amphibious_landing" ? "Secure shoreline and establish beachhead" :
                           missionId == "aerial_recon" ? "Gather theater-wide intel" :
                           $"Establish security presence in {regionName}";

            op.SyncFlatFields();
            country.activeOperations.Add(op);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(country);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
            UnityEngine.Debug.Log($"DeployOperation: After Add, Count = {country.activeOperations.Count}");

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.playerBudget -= monthlyCost;
                country.treasury = gm.playerBudget;
            }

            var targetReg = country.regions.Find(r => r.regionName == regionName);
            if (targetReg != null)
            {
                targetReg.security = Mathf.Min(100f, targetReg.security + 5f + securityBoostRate * 2f);
            }

            int totalTroops = 0;
            foreach (var c in contributions) totalTroops += c.personnelAssigned;
            string deploymentDetails = $"{totalTroops:N0} personnel deployed across {contributions.Count} branches";

            NotificationManager.Instance?.AddNotification($"DEPLOYED: {opName} launched ({deploymentDetails}).", Color.red);
            NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} launched {missionId} operation in {regionName} theatre under {opName}.");

            LogOperationalEvent(country, $"LAUNCHED: Operation '{opName}' ({missionId}) in {regionName}. Deployed {totalTroops:N0} personnel across {contributions.Count} branches.");
            RefreshMilitaryData();
            GameManager.Instance?.SaveGame();
        }

        private void DeployOperation(CountryDataSO country, string opName, string regionName, string type, 
            int troops, int tanks, int apcs, int fighters, int helis, int frigates, int submarines,
            int rdf = 0, int bombers = 0, int uavs = 0, int airForce = 0, int navy = 0, int patrolBoats = 0)
        {
            var contributions = new List<BranchContribution>();
            
            MilitaryBranch branch = MilitaryBranch.Army;
            if (frigates > 0 || submarines > 0 || patrolBoats > 0 || navy > 0)
            {
                branch = MilitaryBranch.Navy;
            }
            else if (fighters > 0 || bombers > 0 || helis > 0 || uavs > 0 || airForce > 0)
            {
                branch = MilitaryBranch.AirForce;
            }
            else if (rdf > 0)
            {
                branch = MilitaryBranch.RDF;
            }

            var contr = new BranchContribution();
            contr.branch = branch;
            contr.personnelAssigned = branch == MilitaryBranch.RDF ? rdf : (branch == MilitaryBranch.Navy ? navy : (branch == MilitaryBranch.AirForce ? airForce : troops));
            
            if (tanks > 0) contr.SetUnitCount("Tanks", tanks);
            if (apcs > 0) contr.SetUnitCount("APCs", apcs);
            if (fighters > 0) contr.SetUnitCount("Fighters", fighters);
            if (bombers > 0) contr.SetUnitCount("Bombers", bombers);
            if (helis > 0) contr.SetUnitCount("Helicopters", helis);
            if (uavs > 0) contr.SetUnitCount("Drones", uavs);
            if (frigates > 0) contr.SetUnitCount("Frigates", frigates);
            if (submarines > 0) contr.SetUnitCount("Submarines", submarines);
            if (patrolBoats > 0) contr.SetUnitCount("Patrol Boats", patrolBoats);
            
            contributions.Add(contr);
            
            DeployOperation(country, opName, regionName, type, contributions);
        }

        private void PopulateActiveOpsList(CountryDataSO country)
        {
            if (_opsList == null) return;
            _opsList.Clear();

            var btnViewAll = _panelOps?.Q<Button>("btn-view-all-ops");
            int opCount = country.activeOperations != null ? country.activeOperations.Count : 0;
            if (btnViewAll != null)
                btnViewAll.text = opCount > 0 ? $"VIEW ALL OPERATIONS ({opCount}) >" : "VIEW ALL OPERATIONS >";

            if (country.activeOperations == null || country.activeOperations.Count == 0)
            {
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

                int progress = Mathf.RoundToInt(op.objectiveProgress);
                if (op.turnsToDeploy > 0) progress = 0;

                int strength = Mathf.RoundToInt(op.effectiveTroopStrength);
                Color strengthColor = strength >= 70 ? new Color(0.14f, 0.93f, 0.34f)
                                    : strength >= 40 ? new Color(1f, 0.75f, 0.1f)
                                    : new Color(1f, 0.25f, 0.25f);

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

                var nameRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                var nameLabel = new Label(op.opName);
                nameLabel.AddToClassList("op-card__name");
                nameRow.Add(nameLabel);
                if (op.needsReinforcement)
                {
                    var badge = new Label("⚠ REINFORCE") { style = { color = new Color(1f, 0.65f, 0.1f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold, backgroundColor = new Color(1f, 0.65f, 0.1f, 0.15f), borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, paddingLeft = 5, paddingRight = 5, paddingTop = 2, paddingBottom = 2, marginLeft = 8 } };
                    nameRow.Add(badge);
                }
                if (op.status == "Stalled")
                {
                    var stalledBadge = new Label("✖ STALLED") { style = { color = new Color(1f, 0.25f, 0.25f), fontSize = 9, unityFontStyleAndWeight = FontStyle.Bold, backgroundColor = new Color(1f, 0.25f, 0.25f, 0.15f), borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, paddingLeft = 5, paddingRight = 5, paddingTop = 2, paddingBottom = 2, marginLeft = 8 } };
                    nameRow.Add(stalledBadge);
                }
                info.Add(nameRow);

                var meta = new Label($"Type: {op.type}    Region: {op.regionName}    Month {op.turnsActive}");
                meta.AddToClassList("op-card__meta");
                var obj = new Label($"Objective: {op.objective}    Casualties: {op.casualtiesSustained:N0}    Equip Lost: {op.attritionEquipment}");
                obj.AddToClassList("op-card__objective");
                info.Add(meta); info.Add(obj);

                var contrList = op.GetContributionsMigrated();
                if (contrList.Count > 0)
                {
                    string branchBreakdown = "";
                    foreach (var c in contrList)
                    {
                        string unitsStr = "";
                        var dict = c.GetUnitCountsDict();
                        foreach (var kv in dict)
                        {
                            if (kv.Value > 0) unitsStr += $"{kv.Value} {kv.Key}, ";
                        }
                        unitsStr = unitsStr.TrimEnd(',', ' ');
                        string detail = $"{c.branch}: {c.personnelAssigned} personnel" + (string.IsNullOrEmpty(unitsStr) ? "" : $" ({unitsStr})");
                        branchBreakdown += detail + "  •  ";
                    }
                    if (branchBreakdown.EndsWith("  •  ")) branchBreakdown = branchBreakdown.Substring(0, branchBreakdown.Length - 5);
                    var breakdownLabel = new Label(branchBreakdown) { style = { color = new Color(0, 0.8f, 1f), fontSize = 9, marginTop = 4 } };
                    info.Add(breakdownLabel);
                }
                card.Add(info);

                var progStat = new VisualElement();
                progStat.AddToClassList("op-card__stats");
                var progLbl = new Label("OBJECTIVE");
                progLbl.AddToClassList("op-card__stat-label");
                var progVal = new Label(op.turnsToDeploy > 0 ? "DEPLOYING" : $"{progress}%");
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

                var strengthStat = new VisualElement();
                strengthStat.AddToClassList("op-card__stats");
                var strengthLbl = new Label("STRENGTH");
                strengthLbl.AddToClassList("op-card__stat-label");
                var strengthVal = new Label($"{strength}%") { style = { color = strengthColor } };
                strengthVal.AddToClassList("op-card__stat-value");
                strengthStat.Add(strengthLbl); strengthStat.Add(strengthVal);
                card.Add(strengthStat);

                var etaEl = new VisualElement();
                etaEl.AddToClassList("op-card__eta");
                var etaLbl = new Label("ETA");
                etaLbl.AddToClassList("op-card__eta-label");
                string etaStr = op.turnsToDeploy > 0 ? $"{op.turnsToDeploy} Mo" : "Deployed";
                var etaVal = new Label(etaStr);
                etaVal.AddToClassList("op-card__eta-value");
                etaEl.Add(etaLbl); etaEl.Add(etaVal);
                card.Add(etaEl);
                
                var viewBtn = new Button(() => {  });
                viewBtn.text = "ABORT";
                viewBtn.AddToClassList("op-card__view-btn");
                viewBtn.style.backgroundColor = new Color(244/255f, 67/255f, 54/255f, 0.4f);
                viewBtn.clicked += () => {
                        country.activeOperations.Remove(op);
                        var contrList = op.GetContributionsMigrated();
                        foreach (var c in contrList)
                        {
                            MilitaryOperation.ReturnToReserves(country, c.branch, c.GetUnitCountsDict(), c.personnelAssigned);
                        }

                        if (_selectedOp == op) _selectedOp = null;
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
            var opTypesScroll = _panelOps?.Q<ScrollView>("op-types-scroll");
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
            if (country.inboxMessages != null)
            {
                foreach (var m in country.inboxMessages) m.isRead = true;
            }
            StateCraft.Systems.InboxSystem.Instance?.ClearRoutineBadge();

            var btnInbox = _panelOps?.Q<Button>("btn-inbox");
            var badge    = _panelOps?.Q<Label>("inbox-unread-badge");
            var icon     = _panelOps?.Q<Label>("inbox-icon");
            if (btnInbox != null)
            {
                btnInbox.style.backgroundColor = Color.clear;
                btnInbox.style.borderTopColor = btnInbox.style.borderBottomColor =
                    btnInbox.style.borderLeftColor = btnInbox.style.borderRightColor =
                    new Color(1f, 1f, 1f, 0.15f);
            }
            if (icon  != null) { icon.text = FaIcons.Envelope; icon.style.color = new Color(0f, 210/255f, 255/255f); }
            if (badge != null) badge.style.display = DisplayStyle.None;
            var overlay = new VisualElement() { style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0, backgroundColor = new Color(0, 0, 0, 0.85f), justifyContent = Justify.Center, alignItems = Align.Center } };
            var modal = new VisualElement() { style = { width = 750, paddingBottom = 22, paddingTop = 22, paddingLeft = 24, paddingRight = 24, backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f) } };
            SetBorderRadius(modal.style, 8);
            SetBorderColor(modal.style, new Color(0, 210/255f, 255/255f, 0.5f));
            SetBorderWidth(modal.style, 1);
            overlay.Add(modal);

            var headerRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 14 } };
            var headerLeft = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var headerIcon = new Label(FaIcons.Envelope);
            headerIcon.AddToClassList("fa-icon");
            headerIcon.style.color = new Color(0f, 210/255f, 255/255f);
            headerIcon.style.fontSize = 22;
            headerIcon.style.marginRight = 10;
            headerLeft.Add(headerIcon);
            headerLeft.Add(new Label("COMMAND INBOX") { style = { color = Color.white, fontSize = 20, unityFontStyleAndWeight = FontStyle.Bold } });
            headerRow.Add(headerLeft);

            var closeBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "×" };
            closeBtn.style.width = 34; closeBtn.style.height = 34; closeBtn.style.fontSize = 22;
            closeBtn.AddToClassList("button-premium");
            headerRow.Add(closeBtn);
            modal.Add(headerRow);

            var scroll = new ScrollView { style = { maxHeight = 480, marginBottom = 14 } };
            modal.Add(scroll);

            if (country.inboxMessages == null || country.inboxMessages.Count == 0)
            {
                scroll.Add(new Label("Inbox is empty. No new intelligence.") { style = { color = Color.grey, marginTop = 20, fontSize = 14, unityTextAlign = TextAnchor.MiddleCenter } });
            }
            else
            {
                var messages = new List<InboxMessage>(country.inboxMessages);
                messages.Reverse();

                foreach (var msg in messages)
                {
                    var card = new VisualElement();
                    card.AddToClassList("sub-panel");
                    card.style.paddingTop = 12; card.style.paddingBottom = 12;
                    card.style.paddingLeft = 14; card.style.paddingRight = 14;
                    card.style.marginBottom = 8;

                    var topRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                    Color senderColor = msg.urgency == "Critical" ? new Color(255/255f, 82/255f, 82/255f) : (msg.urgency == "Important" ? new Color(0.9f, 0.6f, 0f) : new Color(0.1f, 0.5f, 0.9f));
                    topRow.Add(new Label(msg.sender) { style = { color = senderColor, fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold } });
                    topRow.Add(new Label($"Turn {msg.turnReceived}") { style = { color = Color.grey, fontSize = 12 } });
                    card.Add(topRow);

                    card.Add(new Label(msg.subject) { style = { color = Color.white, fontSize = 15, unityFontStyleAndWeight = FontStyle.Bold, marginTop = 4 } });
                    
                    var bodyLabel = new Label(msg.body) { style = { color = new Color(220/255f, 225/255f, 230/255f), fontSize = 13, marginTop = 6 } };
                    bodyLabel.style.whiteSpace = WhiteSpace.Normal;
                    card.Add(bodyLabel);

                    scroll.Add(card);
                }
            }

            var clearBtn = new Button() { text = "CLEAR ALL MESSAGES" };
            clearBtn.AddToClassList("button-premium");
            clearBtn.style.height = 36;
            clearBtn.style.fontSize = 13;
            clearBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            clearBtn.clicked += () =>
            {
                country.inboxMessages?.Clear();
                overlay.parent?.Remove(overlay);
                RefreshMilitaryData();
            };
            modal.Add(clearBtn);

            if (_dashboardRoot != null) _dashboardRoot.Add(overlay);
        }

        private enum CrisisType { None, Insurgency, ActiveWar, CampDetected, NationalInstability, BorderThreat }
        private struct CrisisReport
        {
            public CrisisType type;
            public string title;
            public string description;
            public string severity;
        }

        private List<CrisisReport> EvaluateCrises(CountryDataSO country)
        {
            var crises = new List<CrisisReport>();

            foreach (var r in country.regions)
            {
                if (r.insurgencyLevel > 20f)
                {
                    crises.Add(new CrisisReport {
                        type = CrisisType.Insurgency,
                        title = $"INSURGENCY — {r.regionName.ToUpper()}",
                        description = $"Insurgency at {r.insurgencyLevel:F0}% strength. {r.insurgencyDuration} month(s) unaddressed. Region security: {r.security:F0}%.",
                        severity = r.insurgencyLevel > 60 ? "CRITICAL" : "HIGH"
                    });
                }
                else if (r.security < 65f || r.stability < 65f)
                {
                    crises.Add(new CrisisReport {
                        type = CrisisType.Insurgency,
                        title = $"CIVIL UNREST & SECURITY DEPLOYMENT — {r.regionName.ToUpper()}",
                        description = $"Security is at {r.security:F0}% and stability is at {r.stability:F0}% in {r.regionName}. Military deployment recommended to restore order.",
                        severity = (r.security < 35f || r.stability < 35f) ? "CRITICAL" : "MODERATE"
                    });
                }
                if (r.insurgentCampDetected)
                {
                    crises.Add(new CrisisReport {
                        type = CrisisType.CampDetected,
                        title = $"INSURGENT CAMP DETECTED — {r.regionName.ToUpper()}",
                        description = $"Intel assets have pinpointed an insurgent camp in {r.regionName} at {r.campStrength:F0}% strength. Strike window is open.",
                        severity = "HIGH"
                    });
                }
                if (r.specialization == RegionSpecialization.BorderConflict && r.insurgencyLevel > 20f)
                {
                    crises.Add(new CrisisReport {
                        type = CrisisType.BorderThreat,
                        title = $"BORDER THREAT — {r.regionName.ToUpper()}",
                        description = $"{r.regionName} is a border zone with active hostile activity at {r.insurgencyLevel:F0}% intensity.",
                        severity = "MODERATE"
                    });
                }
            }

            var warSys = GameManager.Instance?.WarSystem;
            if (warSys != null && GameManager.Instance.allCountries != null)
            {
                foreach (var c in GameManager.Instance.allCountries)
                {
                    if (c == country) continue;
                    if (warSys.GetWarStage(c) == StateCraft.Interfaces.WarStage.Conflict)
                    {
                        crises.Add(new CrisisReport {
                            type = CrisisType.ActiveWar,
                            title = $"ACTIVE WAR — {c.countryName.ToUpper()}",
                            description = $"Armed conflict is ongoing with {c.countryName}. Military operations in the theatre are authorized.",
                            severity = "CRITICAL"
                        });
                    }
                }
            }

            if (country.stability < 35f)
            {
                crises.Add(new CrisisReport {
                    type = CrisisType.NationalInstability,
                    title = "NATIONAL INSTABILITY",
                    description = $"National stability has fallen to {country.stability:F0}%. The government has authorized military deployment to restore order.",
                    severity = country.stability < 20f ? "CRITICAL" : "HIGH"
                });
            }
            if (country.publicApproval < 25f)
            {
                crises.Add(new CrisisReport {
                    type = CrisisType.NationalInstability,
                    title = "CIVIL UNREST",
                    description = $"Public approval has collapsed to {country.publicApproval:F0}%. Civil unrest may require a peacekeeping deployment.",
                    severity = "HIGH"
                });
            }

            return crises;
        }

        private void ShowNoCrisisPanel()
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.top = 0; overlay.style.bottom = 0; overlay.style.left = 0; overlay.style.right = 0;
            overlay.style.backgroundColor = new Color(8/255f, 12/255f, 22/255f, 0.92f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;

            var box = new VisualElement();
            box.style.backgroundColor = new Color(15/255f, 22/255f, 30/255f);
            box.style.borderTopLeftRadius = 8; box.style.borderTopRightRadius = 8;
            box.style.borderBottomLeftRadius = 8; box.style.borderBottomRightRadius = 8;
            box.style.paddingTop = 30; box.style.paddingBottom = 30;
            box.style.paddingLeft = 40; box.style.paddingRight = 40;
            box.style.alignItems = Align.Center;
            box.style.width = 480;
            SetBorderWidth(box.style, 1);
            SetBorderColor(box.style, new Color(0.14f, 0.93f, 0.34f, 0.3f));

            var iconLbl = new Label("\u2714") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 40, marginBottom = 16 } };
            box.Add(iconLbl);

            box.Add(new Label("THEATRE IS STABLE") { style = { color = Color.white, fontSize = 18, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 10 } });
            box.Add(new Label("No active crises, insurgencies, or conflicts\nrequire military intervention at this time.") { style = { color = new Color(150/255f, 160/255f, 170/255f), fontSize = 13, marginBottom = 6, unityTextAlign = TextAnchor.MiddleCenter } });
            box.Add(new Label("Monitor regional security and stability.\nOperations can only be authorized during a crisis.") { style = { color = new Color(100/255f, 110/255f, 120/255f), fontSize = 11, marginBottom = 25, unityTextAlign = TextAnchor.MiddleCenter } });

            var closeBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "UNDERSTOOD" };
            closeBtn.AddToClassList("button-premium");
            closeBtn.style.width = 200; closeBtn.style.height = 38;
            closeBtn.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f);
            closeBtn.style.color = Color.black;
            closeBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            box.Add(closeBtn);

            overlay.Add(box);
            overlay.RegisterCallback<ClickEvent>(evt => { if (evt.target == overlay) overlay.parent?.Remove(overlay); });
            if (_dashboardRoot != null) _dashboardRoot.Add(overlay);
        }

        private void ShowLaunchOpModalNoArgs()
        {
            ShowLaunchOpModal();
        }

        private void ShowLaunchOpModal(string prefillRegionName = null, string prefillMissionId = null)
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            var activeCrises = EvaluateCrises(country);
            var deterioratedRegions = country.regions.FindAll(r => r.security < 70f || r.insurgencyLevel > 10f || r.stability < 70f);
            if (deterioratedRegions.Count == 0) deterioratedRegions = country.regions;

            var overlay = new VisualElement();
            overlay.name = "OpModalOverlay";
            overlay.style.position = Position.Absolute;
            overlay.style.top = 0; overlay.style.bottom = 0; overlay.style.left = 0; overlay.style.right = 0;
            overlay.style.backgroundColor = new Color(8/255f, 12/255f, 22/255f, 0.96f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;

            var modal = new VisualElement();
            modal.AddToClassList("sub-panel");
            modal.style.width = 1100;
            modal.style.maxHeight = Length.Percent(90);
            modal.style.paddingTop = 20; modal.style.paddingBottom = 20; modal.style.paddingLeft = 25; modal.style.paddingRight = 25;
            SetBorderWidth(modal.style, 1);
            SetBorderColor(modal.style, new Color(0.1f, 0.6f, 0.9f, 0.5f));
            overlay.Add(modal);

            var titleRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 12, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.1f), paddingBottom = 10 } };
            var title = new Label("NEW MILITARY OPERATION");
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 18;
            titleRow.Add(title);

            bool isJointMode = false;
            var modeRow = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            var btnModeSingle = new Button() { text = "SINGLE BRANCH" };
            var btnModeJoint = new Button() { text = "JOINT OPERATION" };
            
            void UpdateModeStyles()
            {
                btnModeSingle.style.backgroundColor = !isJointMode ? new Color(0.14f, 0.93f, 0.34f) : new Color(1,1,1,0.1f);
                btnModeSingle.style.color = !isJointMode ? Color.black : Color.white;
                btnModeJoint.style.backgroundColor = isJointMode ? new Color(0.1f, 0.6f, 1f) : new Color(1,1,1,0.1f);
                btnModeJoint.style.color = isJointMode ? Color.black : Color.white;
            }
            btnModeSingle.style.unityFontStyleAndWeight = btnModeJoint.style.unityFontStyleAndWeight = FontStyle.Bold;
            btnModeSingle.style.fontSize = btnModeJoint.style.fontSize = 10;
            btnModeSingle.style.height = btnModeJoint.style.height = 26;
            btnModeSingle.style.marginRight = 5;
            modeRow.Add(btnModeSingle);
            modeRow.Add(btnModeJoint);
            titleRow.Add(modeRow);
            modal.Add(titleRow);

            var crisisStrip = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 14 } };
            foreach (var crisis in activeCrises)
            {
                Color sevCol = crisis.severity == "CRITICAL" ? new Color(1f, 0.25f, 0.25f) : crisis.severity == "HIGH" ? new Color(1f, 0.65f, 0.1f) : new Color(0.1f, 0.65f, 1f);
                var tag = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, backgroundColor = new Color(sevCol.r, sevCol.g, sevCol.b, 0.12f), borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, paddingLeft = 8, paddingRight = 10, paddingTop = 5, paddingBottom = 5, marginRight = 8, marginBottom = 6 } };
                tag.style.borderTopWidth = tag.style.borderBottomWidth = tag.style.borderLeftWidth = tag.style.borderRightWidth = 1;
                tag.style.borderTopColor = tag.style.borderBottomColor = tag.style.borderLeftColor = tag.style.borderRightColor = new Color(sevCol.r, sevCol.g, sevCol.b, 0.5f);
                var dot = new VisualElement { style = { width = 7, height = 7, backgroundColor = sevCol, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, marginRight = 6 } };
                tag.Add(dot);
                tag.Add(new Label(crisis.title) { style = { color = sevCol, fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                tag.tooltip = crisis.description;
                crisisStrip.Add(tag);
            }
            modal.Add(crisisStrip);

            var columns = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            modal.Add(columns);

            var leftCol = new VisualElement() { style = { width = Length.Percent(60), paddingRight = 20, borderRightWidth = 1, borderRightColor = new Color(1,1,1,0.1f) } };
            var rightCol = new VisualElement() { style = { width = Length.Percent(40), paddingLeft = 20 } };
            columns.Add(leftCol);
            columns.Add(rightCol);

            string ParseHtmlIcon(string str)
            {
                if (string.IsNullOrEmpty(str)) return "";
                if (str.StartsWith("&#x") && str.EndsWith(";"))
                {
                    string hex = str.Substring(3, str.Length - 4);
                    if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int code))
                    {
                        return char.ConvertFromUtf32(code);
                    }
                }
                return str;
            }

            VisualElement MakeFieldLabel(string text, string iconStr) 
            {
                var container = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8, paddingLeft = 6, borderLeftWidth = 2, borderLeftColor = new Color(0.14f, 0.93f, 0.34f) } };
                string cleanIcon = ParseHtmlIcon(iconStr);
                if (!string.IsNullOrEmpty(cleanIcon))
                {
                    var icon = new Label(cleanIcon) { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 12, marginRight = 6 } };
                    icon.AddToClassList("fa-icon");
                    container.Add(icon);
                }
                var lbl = new Label(text) { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } };
                container.Add(lbl);
                return container;
            }

            VisualElement MakeCardSection()
            {
                return new VisualElement() { style = { backgroundColor = new Color(0.10f, 0.12f, 0.18f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = new Color(1,1,1,0.05f), borderBottomColor = new Color(1,1,1,0.05f), borderLeftColor = new Color(1,1,1,0.05f), borderRightColor = new Color(1,1,1,0.05f), borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, paddingTop = 15, paddingBottom = 15, paddingLeft = 15, paddingRight = 15, marginBottom = 15 } };
            }

            void ApplyInputStyle(VisualElement el)
            {
                el.AddToClassList("sc-input");
                el.RegisterCallback<FocusInEvent>(e => el.style.borderBottomColor = new Color(0.14f, 0.93f, 0.34f));
                el.RegisterCallback<FocusOutEvent>(e => el.style.borderBottomColor = new Color(1,1,1,0.2f));
            }

            var cardConfig = MakeCardSection();
            leftCol.Add(cardConfig);

            cardConfig.Add(MakeFieldLabel("BRANCH SELECTION", "&#xf0e8;"));
            var branchContainer = new VisualElement() { style = { marginBottom = 10 } };
            cardConfig.Add(branchContainer);

            cardConfig.Add(MakeFieldLabel("OPERATION NAME", "&#xf024;"));
            var nameInput = new TextField() { value = $"OPERATION {deterioratedRegions[0].regionName.ToUpper().Replace(" ", "")} SHIELD" };
            nameInput.style.height = 30;
            ApplyInputStyle(nameInput);
            cardConfig.Add(nameInput);

            cardConfig.Add(MakeFieldLabel("OPERATION TYPE", "&#xf0eb;"));
            var typeDropdown = new DropdownField();
            typeDropdown.style.height = 30;
            ApplyInputStyle(typeDropdown);
            cardConfig.Add(typeDropdown);

            cardConfig.Add(MakeFieldLabel("TARGET REGION", "&#xf041;"));
            var regionDropdown = new DropdownField();
            var regionChoices = new List<string>();
            foreach (var r in deterioratedRegions)
            {
                string suffix = r.insurgencyDuration > 0 ? $" [{r.GetEntrenchment().ToString()}]" : "";
                regionChoices.Add(r.regionName + suffix);
            }
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
            ApplyInputStyle(regionDropdown);
            regionDropdown.RegisterValueChangedCallback(evt => {
                if (!string.IsNullOrEmpty(evt.newValue))
                {
                    string cleanReg = evt.newValue.Split('[')[0].Trim().Replace(" ", "");
                    nameInput.value = $"OPERATION {cleanReg.ToUpper()} SHIELD";
                }
            });
            cardConfig.Add(regionDropdown);

            cardConfig.Add(MakeFieldLabel("MISSION OBJECTIVE", "&#xf140;"));
            var objDropdown = new DropdownField();
            objDropdown.style.height = 30;
            ApplyInputStyle(objDropdown);
            cardConfig.Add(objDropdown);

            var assignScroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1, height = 300, marginTop = 10 } };
            leftCol.Add(assignScroll);

            int deployTroops = 0, deployRdf = 0, deployAirForce = 0, deployNavy = 0;
            int deployTanks = 0, deployApcs = 0, deployHelis = 0, deployDrones = 0, deployBoats = 0;
            int deployFighters = 0, deployBombers = 0, deployFrigates = 0, deploySubs = 0;
            int deployArtillery = 0, deployDestroyers = 0;

            var cardEstimates = MakeCardSection();
            rightCol.Add(cardEstimates);
            cardEstimates.Add(MakeFieldLabel("ESTIMATED RESULTS", "&#xf201;"));

            VisualElement MakeEstRow(string labelText, out Label valLabel)
            {
                var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6, alignItems = Align.Center } };
                r.Add(new Label(labelText) { style = { color = Color.grey, fontSize = 11 } });
                valLabel = new Label("-") { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } };
                r.Add(valLabel);
                return r;
            }

            var chanceContainer = new VisualElement() { style = { marginBottom = 15 } };
            chanceContainer.Add(new Label("SUCCESS CHANCE") { style = { color = Color.grey, fontSize = 11, marginBottom = 4 } });
            var chanceValueLabel = new Label("42% LOW") { style = { color = Color.white, fontSize = 24, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } };
            chanceContainer.Add(chanceValueLabel);
            var chanceBarLabel = new Label("██████░░░░") { style = { fontSize = 14, letterSpacing = 2 } };
            chanceContainer.Add(chanceBarLabel);
            cardEstimates.Add(chanceContainer);

            cardEstimates.Add(MakeEstRow("Enemy Forces", out var lblEnemy));
            cardEstimates.Add(MakeEstRow("Daily Cost", out var lblCost));
            cardEstimates.Add(MakeEstRow("Duration", out var lblDuration));
            cardEstimates.Add(MakeEstRow("Casualty Risk", out var lblCasualties));
            cardEstimates.Add(MakeEstRow("Civilian Risk", out var lblCiv));
            cardEstimates.Add(MakeEstRow("Political Impact", out var lblPol));

            var cardReadiness = MakeCardSection();
            rightCol.Add(cardReadiness);
            cardReadiness.Add(MakeFieldLabel("MISSION READINESS", "&#xf0ae;"));
            var readinessPanel = new VisualElement() { style = { marginBottom = 10 } };
            cardReadiness.Add(readinessPanel);
            var warningsPanel = new VisualElement();
            cardReadiness.Add(warningsPanel);

            var cardResources = MakeCardSection();
            rightCol.Add(cardResources);
            cardResources.Add(MakeFieldLabel("RESOURCE CONSUMPTION", "&#xf0e4;"));
            cardResources.Add(MakeEstRow("Fuel", out var lblFuel));
            cardResources.Add(MakeEstRow("Ammo", out var lblAmmo));
            cardResources.Add(MakeEstRow("Medical", out var lblMed));
            cardResources.Add(MakeEstRow("Supply", out var lblSup));

            MilitaryBranch selectedSingleBranch = MilitaryBranch.Army;
            List<MilitaryBranch> selectedJointBranches = new List<MilitaryBranch>() { MilitaryBranch.Army, MilitaryBranch.AirForce };
            List<MissionDefinitionSO> filteredMissions = new List<MissionDefinitionSO>();

            System.Action UpdateEstimates = () =>
            {
                string selectedRegionName = regionDropdown.value.Split('[')[0].Trim();
                var targetReg = deterioratedRegions.Find(x => x.regionName == selectedRegionName);
                int entrenchmentPenalty = targetReg != null && targetReg.insurgencyDuration > 0 ? (int)targetReg.GetEntrenchment() * 10 : 0;

                var tempContributions = new List<BranchContribution>();
                var activeBranches = isJointMode ? selectedJointBranches : new List<MilitaryBranch>() { selectedSingleBranch };
                foreach (var branch in activeBranches)
                {
                    var c = new BranchContribution() { branch = branch };
                    if (branch == MilitaryBranch.Army)
                    {
                        c.personnelAssigned = deployTroops;
                        c.SetUnitCount("Tanks", deployTanks);
                        c.SetUnitCount("APCs", deployApcs);
                        c.SetUnitCount("Artillery", deployArtillery);
                    }
                    else if (branch == MilitaryBranch.AirForce)
                    {
                        c.personnelAssigned = deployAirForce;
                        c.SetUnitCount("Fighters", deployFighters);
                        c.SetUnitCount("Bombers", deployBombers);
                        c.SetUnitCount("Helicopters", deployHelis);
                        c.SetUnitCount("Drones", deployDrones);
                    }
                    else if (branch == MilitaryBranch.Navy)
                    {
                        c.personnelAssigned = deployNavy;
                        c.SetUnitCount("Frigates", deployFrigates);
                        c.SetUnitCount("Submarines", deploySubs);
                        c.SetUnitCount("Patrol Boats", deployBoats);
                        c.SetUnitCount("Destroyers", deployDestroyers);
                    }
                    else if (branch == MilitaryBranch.RDF)
                    {
                        c.personnelAssigned = deployRdf;
                    }
                    tempContributions.Add(c);
                }

                float monthlyCost = MilitaryOperation.CalculateUpkeep(tempContributions);
                if (entrenchmentPenalty > 0) monthlyCost *= 1.0f + (entrenchmentPenalty / 50f);
                float localCost = monthlyCost * country.exchangeRate;
                string sym = string.IsNullOrEmpty(country.currencySymbol) || country.currencySymbol == "₦" ? "N" : country.currencySymbol;

                if (targetReg != null && targetReg.insurgencyLevel > 0)
                {
                    int actualFighters = Mathf.RoundToInt(targetReg.insurgencyLevel * 125f);
                    if (targetReg.intelCoverage < 15f)
                    {
                        lblEnemy.text = "UNKNOWN";
                        lblEnemy.style.color = new Color(1f, 0.32f, 0.32f);
                    }
                    else if (targetReg.intelCoverage < 50f)
                    {
                        int minEst = Mathf.Max(50, actualFighters - 3500);
                        int maxEst = actualFighters + 4500;
                        lblEnemy.text = $"{minEst:N0} - {maxEst:N0}";
                        lblEnemy.style.color = new Color(1f, 0.76f, 0.03f);
                    }
                    else
                    {
                        int minEst = Mathf.Max(50, actualFighters - 500);
                        int maxEst = actualFighters + 500;
                        lblEnemy.text = $"~{actualFighters:N0} ({minEst}-{maxEst})";
                        lblEnemy.style.color = new Color(0, 0.9f, 0.47f);
                    }
                }
                else
                {
                    lblEnemy.text = "MINIMAL";
                    lblEnemy.style.color = Color.grey;
                }

                float baseChance = MilitaryOperation.CalculateSecurityBoost(country, typeDropdown.value, tempContributions);
                float intelBonus = targetReg != null ? (targetReg.intelCoverage * 0.2f) : 0f;
                baseChance = Mathf.Clamp(baseChance + intelBonus - entrenchmentPenalty, 5f, 95f);

                int durationDays = Mathf.Max(3, Mathf.RoundToInt(60f - (baseChance / 2f) + entrenchmentPenalty));

                string chanceRating = baseChance >= 75f ? "HIGH" : (baseChance >= 50f ? "MEDIUM" : "LOW");
                chanceValueLabel.text = $"{baseChance:F0}% {chanceRating}";
                
                Color cCol = baseChance >= 75f ? new Color(0, 0.9f, 0.47f) : (baseChance >= 50f ? new Color(1f, 0.76f, 0.03f) : new Color(1f, 0.32f, 0.32f));
                chanceValueLabel.style.color = cCol;
                chanceBarLabel.style.color = cCol;

                int filledBlocks = Mathf.RoundToInt(baseChance / 10f);
                string bar = new string('█', filledBlocks) + new string('░', 10 - filledBlocks);
                chanceBarLabel.text = bar;
                
                lblCost.text = $"{sym}{(localCost / 30f):F2}B";
                lblDuration.text = $"{durationDays} Days";

                lblCasualties.text = (deployTroops > 0 && deployTanks == 0 && deployApcs == 0) ? "HIGH" : (deployTanks > 5 ? "LOW" : "MEDIUM");
                lblCasualties.style.color = Color.white;
                lblCasualties.style.backgroundColor = lblCasualties.text == "HIGH" ? new Color(0.8f, 0.2f, 0.2f) : (lblCasualties.text == "LOW" ? new Color(0.1f, 0.6f, 0.2f) : new Color(0.8f, 0.6f, 0.1f));
                lblCasualties.style.paddingLeft = 6; lblCasualties.style.paddingRight = 6; lblCasualties.style.paddingTop = 2; lblCasualties.style.paddingBottom = 2;
                lblCasualties.style.borderTopLeftRadius = 4; lblCasualties.style.borderTopRightRadius = 4; lblCasualties.style.borderBottomLeftRadius = 4; lblCasualties.style.borderBottomRightRadius = 4;
                
                lblCiv.text = (deployHelis > 0 || deployTanks > 10) ? "MEDIUM" : "LOW";
                
                string polText = baseChance > 70 ? "+3 Approval" : (baseChance < 40 ? "-5 Approval" : "Neutral");
                lblPol.text = polText;
                lblPol.style.color = baseChance > 70 ? new Color(0.14f, 0.93f, 0.34f) : (baseChance < 40 ? Color.red : Color.grey);

                readinessPanel.Clear();
                warningsPanel.Clear();

                VisualElement MakeReadinessRow(string text, Color statusColor)
                {
                    var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 } };
                    var dot = new VisualElement() { style = { width = 8, height = 8, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, marginRight = 8, backgroundColor = statusColor } };
                    r.Add(dot);
                    r.Add(new Label(text) { style = { color = Color.white, fontSize = 11 } });
                    return r;
                }

                bool armyPersonnelOk = deployTroops <= country.unitInfantry;
                bool armyVehiclesOk = deployTanks <= country.unitTanks && deployApcs <= country.unitAPCs && deployArtillery <= country.unitArtillery;
                bool armyCrewOk = deployTroops >= (deployTanks * 4 + deployApcs * 2 + deployArtillery * 3) || (deployTanks == 0 && deployApcs == 0 && deployArtillery == 0);
                
                bool airPersonnelOk = deployAirForce <= country.airForceStats.personnel;
                bool airVehiclesOk = deployFighters <= country.unitFighters && deployBombers <= country.unitBombers && deployHelis <= country.unitAttackHelis && deployDrones <= country.unitUAVs;
                bool airCrewOk = deployAirForce >= (deployFighters * 1 + deployBombers * 2 + deployHelis * 2 + deployDrones * 1) || (deployFighters == 0 && deployBombers == 0 && deployHelis == 0 && deployDrones == 0);

                bool navyPersonnelOk = deployNavy <= country.navyStats.personnel;
                bool navyVehiclesOk = deployFrigates <= country.unitFrigates && deploySubs <= country.unitSubmarines && deployBoats <= country.unitPatrolBoats && deployDestroyers <= country.unitDestroyers;
                bool navyCrewOk = deployNavy >= (deployFrigates * 15 + deploySubs * 20 + deployBoats * 5 + deployDestroyers * 30) || (deployFrigates == 0 && deploySubs == 0 && deployBoats == 0 && deployDestroyers == 0);

                bool rdfPersonnelOk = deployRdf <= country.rdfStats.personnel;

                if (activeBranches.Contains(MilitaryBranch.Army))
                {
                    readinessPanel.Add(MakeReadinessRow(armyPersonnelOk && armyVehiclesOk ? "Army Assets Available" : "Army Depleted", armyPersonnelOk && armyVehiclesOk ? Color.green : Color.red));
                }
                if (activeBranches.Contains(MilitaryBranch.AirForce))
                {
                    readinessPanel.Add(MakeReadinessRow(airPersonnelOk && airVehiclesOk ? "Air Force Assets Available" : "Air Force Depleted", airPersonnelOk && airVehiclesOk ? Color.green : Color.red));
                }
                if (activeBranches.Contains(MilitaryBranch.Navy))
                {
                    readinessPanel.Add(MakeReadinessRow(navyPersonnelOk && navyVehiclesOk ? "Navy Assets Available" : "Navy Depleted", navyPersonnelOk && navyVehiclesOk ? Color.green : Color.red));
                }
                if (activeBranches.Contains(MilitaryBranch.RDF))
                {
                    readinessPanel.Add(MakeReadinessRow(rdfPersonnelOk ? "RDF Assets Available" : "RDF Depleted", rdfPersonnelOk ? Color.green : Color.red));
                }

                int totalUnitsAssigned = deployTroops + deployRdf + deployAirForce + deployNavy + deployTanks + deployApcs + deployArtillery + deployHelis + deployDrones + deployBoats + deployFighters + deployBombers + deployFrigates + deploySubs + deployDestroyers;
                if (totalUnitsAssigned == 0) warningsPanel.Add(MakeReadinessRow("No Combat Units Assigned", Color.red));
                if (!armyPersonnelOk || !armyVehiclesOk) warningsPanel.Add(MakeReadinessRow("Insufficient Army Reserves", Color.red));
                if (!armyCrewOk && (deployTanks > 0 || deployApcs > 0 || deployArtillery > 0)) warningsPanel.Add(MakeReadinessRow("Insufficient Crew for Army Vehicles", Color.red));
                if (!airPersonnelOk || !airVehiclesOk) warningsPanel.Add(MakeReadinessRow("Insufficient Air Force Reserves", Color.red));
                if (!airCrewOk && (deployFighters > 0 || deployBombers > 0 || deployHelis > 0 || deployDrones > 0)) warningsPanel.Add(MakeReadinessRow("Insufficient Crew for Aircraft", Color.red));
                if (!navyPersonnelOk || !navyVehiclesOk) warningsPanel.Add(MakeReadinessRow("Insufficient Navy Reserves", Color.red));
                if (!navyCrewOk && (deployFrigates > 0 || deploySubs > 0 || deployBoats > 0 || deployDestroyers > 0)) warningsPanel.Add(MakeReadinessRow("Insufficient Crew for Navy Vessels", Color.red));
                if (!rdfPersonnelOk) warningsPanel.Add(MakeReadinessRow("Insufficient RDF Reserves", Color.red));
                if (baseChance < 50) warningsPanel.Add(MakeReadinessRow("Success Chance Below 50%", Color.red));

                int fuelCost = deployTanks * 10 + deployApcs * 5 + deployArtillery * 3 + deployHelis * 15 + deployBoats * 20 + deployFighters * 25 + deployBombers * 40 + deployFrigates * 50 + deployDestroyers * 75;
                int ammoCost = deployTroops / 100 + deployTanks * 5 + deployArtillery * 10 + deployHelis * 5 + deployFighters * 8 + deployBombers * 15 + deployFrigates * 15 + deployDestroyers * 25;
                int medCost = deployTroops / 500 + deployRdf / 200;
                int supCost = deployTroops / 200 + deployTanks * 2 + deployArtillery * 1 + deployHelis * 2;
                
                lblFuel.text = fuelCost.ToString("N0") + " Units/Day";
                lblAmmo.text = ammoCost.ToString("N0") + " Units/Day";
                lblMed.text = medCost.ToString("N0") + " Units/Day";
                lblSup.text = supCost.ToString("N0") + " Units/Day";
            };

            void AddStepper(VisualElement parent, string name, int currentVal, int step, int maxAllowed, System.Action<int> setter)
            {
                var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 8 } };
                r.Add(new Label(name) { style = { color = Color.white, fontSize = 11, width = 100, flexShrink = 0 } });
                
                var valLbl = new Label($"{currentVal:N0} / {maxAllowed:N0}") { style = { color = Color.white, fontSize = 11, unityTextAlign = TextAnchor.MiddleRight, width = 75, flexShrink = 0 } };
                
                var btnMinus = new Button(() => {
                    int val = Mathf.Max(0, currentVal - step);
                    currentVal = val;
                    setter(val);
                    valLbl.text = $"{val:N0} / {maxAllowed:N0}";
                    UpdateEstimates();
                }) { text = "-" };
                btnMinus.AddToClassList("sc-stepper-btn");
                btnMinus.style.width = 20; btnMinus.style.height = 20; btnMinus.style.flexShrink = 0;

                var slider = new SliderInt(0, maxAllowed, SliderDirection.Horizontal) { value = currentVal };
                slider.AddToClassList("sc-slider");
                slider.style.marginLeft = 5; slider.style.marginRight = 5; slider.style.flexShrink = 1;
                
                var btnPlus = new Button(() => {
                    int val = Mathf.Min(maxAllowed, currentVal + step);
                    currentVal = val;
                    setter(val);
                    valLbl.text = $"{val:N0} / {maxAllowed:N0}";
                    UpdateEstimates();
                }) { text = "+" };
                btnPlus.AddToClassList("sc-stepper-btn");
                btnPlus.style.width = 20; btnPlus.style.height = 20; btnPlus.style.flexShrink = 0;

                slider.RegisterValueChangedCallback(evt => {
                    int val = evt.newValue;
                    if (step > 1 && val < maxAllowed) {
                        val = Mathf.RoundToInt(val / (float)step) * step;
                        if (val != evt.newValue) slider.SetValueWithoutNotify(val);
                    }
                    currentVal = val;
                    setter(val);
                    valLbl.text = $"{val:N0} / {maxAllowed:N0}";
                    UpdateEstimates();
                });
                
                var controlRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexGrow = 1, flexShrink = 1, alignItems = Align.Center } };
                controlRow.Add(btnMinus);
                controlRow.Add(slider);
                controlRow.Add(btnPlus);

                r.Add(controlRow);
                r.Add(valLbl);
                parent.Add(r);
            }

            void BuildSteppers()
            {
                assignScroll.Clear();

                var activeBranches = isJointMode ? selectedJointBranches : new List<MilitaryBranch>() { selectedSingleBranch };

                foreach (var branch in activeBranches)
                {
                    var branchSection = MakeCardSection();
                    branchSection.style.marginBottom = 10;
                    
                    if (branch == MilitaryBranch.Army)
                    {
                        branchSection.Add(MakeFieldLabel("ARMY ALLOCATION", "&#xf0c0;"));
                        AddStepper(branchSection, "Army Soldiers", deployTroops, 1000, country.unitInfantry, v => deployTroops = v);
                        AddStepper(branchSection, "Tanks", deployTanks, 5, country.unitTanks, v => deployTanks = v);
                        AddStepper(branchSection, "APCs", deployApcs, 10, country.unitAPCs, v => deployApcs = v);
                        AddStepper(branchSection, "Artillery", deployArtillery, 5, country.unitArtillery, v => deployArtillery = v);
                    }
                    else if (branch == MilitaryBranch.AirForce)
                    {
                        branchSection.Add(MakeFieldLabel("AIR FORCE ALLOCATION", "&#xf0fb;"));
                        AddStepper(branchSection, "Air Force", deployAirForce, 100, country.airForceStats.personnel, v => deployAirForce = v);
                        AddStepper(branchSection, "Fighters", deployFighters, 2, country.unitFighters, v => deployFighters = v);
                        AddStepper(branchSection, "Bombers", deployBombers, 1, country.unitBombers, v => deployBombers = v);
                        AddStepper(branchSection, "Helicopters", deployHelis, 2, country.unitAttackHelis, v => deployHelis = v);
                        AddStepper(branchSection, "Drones", deployDrones, 4, country.unitUAVs, v => deployDrones = v);
                    }
                    else if (branch == MilitaryBranch.Navy)
                    {
                        branchSection.Add(MakeFieldLabel("NAVY ALLOCATION", "&#xf21a;"));
                        AddStepper(branchSection, "Navy Crew", deployNavy, 100, country.navyStats.personnel, v => deployNavy = v);
                        AddStepper(branchSection, "Patrol Boats", deployBoats, 1, country.unitPatrolBoats, v => deployBoats = v);
                        AddStepper(branchSection, "Frigates", deployFrigates, 1, country.unitFrigates, v => deployFrigates = v);
                        AddStepper(branchSection, "Destroyers", deployDestroyers, 1, country.unitDestroyers, v => deployDestroyers = v);
                        AddStepper(branchSection, "Submarines", deploySubs, 1, country.unitSubmarines, v => deploySubs = v);
                    }
                    else if (branch == MilitaryBranch.RDF)
                    {
                        branchSection.Add(MakeFieldLabel("RDF ALLOCATION", "&#xf135;"));
                        AddStepper(branchSection, "RDF Operators", deployRdf, 500, country.rdfStats.personnel, v => deployRdf = v);
                    }

                    assignScroll.Add(branchSection);
                }

                UpdateEstimates();
            }

            void UpdateTypeChoices()
            {
                filteredMissions.Clear();
                var catalog = MissionCatalog.GetAll();

                string selectedRegionName = regionDropdown.value.Split('[')[0].Trim();
                var targetReg = deterioratedRegions.Find(x => x.regionName == selectedRegionName);
                bool isCoastal = targetReg == null || targetReg.isCoastal;

                if (!isJointMode)
                {
                    filteredMissions = catalog.FindAll(m => m.leadBranch == selectedSingleBranch && !m.isJoint);
                }
                else
                {
                    filteredMissions = catalog.FindAll(m => m.isJoint);
                }

                var dropdownChoices = new List<string>();
                foreach (var m in filteredMissions)
                {
                    bool reqsMet = true;
                    if (m.requiredBranches != null)
                    {
                        foreach (var req in m.requiredBranches)
                        {
                            if (!selectedJointBranches.Contains(req)) { reqsMet = false; break; }
                        }
                    }

                    if (m.leadBranch == MilitaryBranch.Navy && !isCoastal)
                    {
                        continue;
                    }

                    if (reqsMet)
                    {
                        dropdownChoices.Add(m.displayName);
                    }
                    else
                    {
                        string missing = "";
                        foreach (var req in m.requiredBranches)
                        {
                            if (!selectedJointBranches.Contains(req)) missing += req.ToString() + ", ";
                        }
                        missing = missing.TrimEnd(',', ' ');
                        dropdownChoices.Add($"[LOCKED] {m.displayName} (needs {missing})");
                    }
                }

                if (dropdownChoices.Count == 0) dropdownChoices.Add("No missions available");
                typeDropdown.choices = dropdownChoices;
                typeDropdown.value = dropdownChoices[0];
            }

            void DrawBranchSelectors()
            {
                branchContainer.Clear();
                if (!isJointMode)
                {
                    var singleRow = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                    foreach (MilitaryBranch b in System.Enum.GetValues(typeof(MilitaryBranch)))
                    {
                        var btn = new Button() { text = b.ToString().ToUpper() };
                        btn.style.flexGrow = 1;
                        btn.style.height = 30;
                        btn.style.unityFontStyleAndWeight = FontStyle.Bold;
                        btn.style.fontSize = 11;
                        btn.style.backgroundColor = selectedSingleBranch == b ? new Color(0.14f, 0.93f, 0.34f) : new Color(1,1,1,0.1f);
                        btn.style.color = selectedSingleBranch == b ? Color.black : Color.white;
                        btn.clicked += () =>
                        {
                            selectedSingleBranch = b;
                            DrawBranchSelectors();
                            UpdateTypeChoices();
                            BuildSteppers();
                        };
                        singleRow.Add(btn);
                    }
                    branchContainer.Add(singleRow);
                }
                else
                {
                    var jointGrid = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, paddingLeft = 10, paddingRight = 10, paddingTop = 8, paddingBottom = 8, backgroundColor = new Color(1,1,1,0.05f), borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } };
                    foreach (MilitaryBranch b in System.Enum.GetValues(typeof(MilitaryBranch)))
                    {
                        var tgl = new Toggle(b.ToString()) { value = selectedJointBranches.Contains(b) };
                        tgl.style.color = Color.white;
                        tgl.RegisterValueChangedCallback(evt =>
                        {
                            if (evt.newValue)
                            {
                                if (!selectedJointBranches.Contains(b)) selectedJointBranches.Add(b);
                            }
                            else
                            {
                                if (selectedJointBranches.Contains(b)) selectedJointBranches.Remove(b);
                            }
                            UpdateTypeChoices();
                            BuildSteppers();
                        });
                        jointGrid.Add(tgl);
                        jointGrid.Add(new VisualElement() { style = { width = 15 } });
                    }
                    branchContainer.Add(jointGrid);
                }
            }

            btnModeSingle.clicked += () => { if (isJointMode) { isJointMode = false; deployTroops = deployRdf = deployAirForce = deployNavy = deployTanks = deployApcs = deployHelis = deployDrones = deployBoats = deployFighters = deployBombers = deployFrigates = deploySubs = 0; UpdateModeStyles(); DrawBranchSelectors(); UpdateTypeChoices(); BuildSteppers(); } };
            btnModeJoint.clicked += () => { if (!isJointMode) { isJointMode = true; deployTroops = deployRdf = deployAirForce = deployNavy = deployTanks = deployApcs = deployHelis = deployDrones = deployBoats = deployFighters = deployBombers = deployFrigates = deploySubs = 0; UpdateModeStyles(); DrawBranchSelectors(); UpdateTypeChoices(); BuildSteppers(); } };

            UpdateModeStyles();
            DrawBranchSelectors();
            UpdateTypeChoices();
            BuildSteppers();

            typeDropdown.RegisterValueChangedCallback(evt =>
            {
                string choice = evt.newValue;
                if (choice == null) return;

                if (choice.Contains("Ground") || choice.Contains("Insurgency"))
                    objDropdown.choices = new List<string> { "Secure Infrastructure", "Destroy Insurgent Cells", "Establish Patrol Routes" };
                else if (choice.Contains("Recon") || choice.Contains("Reconnaissance"))
                    objDropdown.choices = new List<string> { "Area Reconnaissance", "Aerial Target Acquisition" };
                else if (choice.Contains("Strike") || choice.Contains("Cas"))
                    objDropdown.choices = new List<string> { "Precision Air Strike", "CAS Ground Support" };
                else if (choice.Contains("Amphibious") || choice.Contains("Blockade"))
                    objDropdown.choices = new List<string> { "Secure Beachhead", "Maritime Blockade Patrol" };
                else
                    objDropdown.choices = new List<string> { "Restore Area Security", "Objective Patrol" };

                objDropdown.value = objDropdown.choices[0];
                BuildSteppers();
            });

            regionDropdown.RegisterValueChangedCallback(evt => { UpdateTypeChoices(); BuildSteppers(); });

            var actionRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginTop = 20, paddingTop = 15, borderTopWidth = 1, borderTopColor = new Color(1,1,1,0.1f) } };
            var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "CANCEL" };
            cancelBtn.AddToClassList("button-premium");
            cancelBtn.style.width = Length.Percent(30);
            cancelBtn.style.height = 40;
            cancelBtn.style.backgroundColor = new Color(1,1,1,0.1f);
            actionRow.Add(cancelBtn);

            var confirmBtn = new Button();
            confirmBtn.AddToClassList("button-premium");
            confirmBtn.style.width = Length.Percent(65);
            confirmBtn.style.height = 40;
            confirmBtn.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f);
            confirmBtn.style.color = Color.black;
            confirmBtn.style.unityFontStyleAndWeight = FontStyle.Bold;

            confirmBtn.clicked += () =>
            {
                int totalUnitsAssigned = deployTroops + deployRdf + deployAirForce + deployNavy + deployTanks + deployApcs + deployHelis + deployDrones + deployBoats + deployFighters + deployBombers + deployFrigates + deploySubs;
                if (totalUnitsAssigned <= 0)
                {
                    NotificationManager.Instance?.AddNotification("INVALID DEPLOYMENT: You must deploy at least some troops or equipment!", Color.red);
                    return;
                }

                if (typeDropdown.value.StartsWith("[LOCKED]"))
                {
                    NotificationManager.Instance?.AddNotification("INVALID DEPLOYMENT: Selected mission requires branches that are not configured!", Color.red);
                    return;
                }

                var finalContributions = new List<BranchContribution>();
                var activeBranches = isJointMode ? selectedJointBranches : new List<MilitaryBranch>() { selectedSingleBranch };
                foreach (var branch in activeBranches)
                {
                    var c = new BranchContribution() { branch = branch };
                    if (branch == MilitaryBranch.Army)
                    {
                        c.personnelAssigned = deployTroops;
                        c.SetUnitCount("Tanks", deployTanks);
                        c.SetUnitCount("APCs", deployApcs);
                    }
                    else if (branch == MilitaryBranch.AirForce)
                    {
                        c.personnelAssigned = deployAirForce;
                        c.SetUnitCount("Fighters", deployFighters);
                        c.SetUnitCount("Bombers", deployBombers);
                        c.SetUnitCount("Helicopters", deployHelis);
                        c.SetUnitCount("Drones", deployDrones);
                    }
                    else if (branch == MilitaryBranch.Navy)
                    {
                        c.personnelAssigned = deployNavy;
                        c.SetUnitCount("Frigates", deployFrigates);
                        c.SetUnitCount("Submarines", deploySubs);
                        c.SetUnitCount("Patrol Boats", deployBoats);
                    }
                    else if (branch == MilitaryBranch.RDF)
                    {
                        c.personnelAssigned = deployRdf;
                    }
                    finalContributions.Add(c);
                }

                string selectedName = typeDropdown.value;
                var selectedMission = filteredMissions.Find(m => m.displayName == selectedName);
                string missionId = selectedMission != null ? selectedMission.missionId : "generic_mission";

                DeployOperation(country, nameInput.value.ToUpper(), regionDropdown.value.Split('[')[0].Trim(), missionId, finalContributions);

                string selectedRegionName = regionDropdown.value.Split('[')[0].Trim();
                var targetReg = deterioratedRegions.Find(x => x.regionName == selectedRegionName);
                if (targetReg != null && missionId == "counter_insurgency")
                {
                    targetReg.insurgencyDuration = 0;
                }

                overlay.parent?.Remove(overlay);
            };

            confirmBtn.style.flexDirection = FlexDirection.Row;
            confirmBtn.style.alignItems = Align.Center;
            confirmBtn.style.justifyContent = Justify.Center;
            confirmBtn.text = "";
            confirmBtn.Clear();

            var iconLbl = new Label(ParseHtmlIcon("&#xf135;")) { style = { color = Color.white, fontSize = 16, marginRight = 8 } };
            iconLbl.AddToClassList("fa-icon");
            confirmBtn.Add(iconLbl);
            confirmBtn.Add(new Label("LAUNCH OPERATION") { style = { color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14 } });
            
            actionRow.Add(confirmBtn);
            modal.Add(actionRow);

            if (!string.IsNullOrEmpty(prefillRegionName))
            {
                var matchedChoice = regionChoices.Find(c => c.StartsWith(prefillRegionName, System.StringComparison.OrdinalIgnoreCase));
                if (matchedChoice != null)
                {
                    regionDropdown.value = matchedChoice;
                }
            }

            if (!string.IsNullOrEmpty(prefillMissionId))
            {
                var prefillMission = MissionCatalog.GetAll().Find(m => m.missionId == prefillMissionId || m.displayName.ToLower().Contains(prefillMissionId.ToLower()));
                if (prefillMission != null)
                {
                    if (prefillMission.isJoint)
                    {
                        isJointMode = true;
                        if (prefillMission.requiredBranches != null)
                            selectedJointBranches = new List<MilitaryBranch>(prefillMission.requiredBranches);
                    }
                    else
                    {
                        isJointMode = false;
                        selectedSingleBranch = prefillMission.leadBranch;
                    }
                    UpdateModeStyles();
                    DrawBranchSelectors();
                    UpdateTypeChoices();
                    var matchedType = typeDropdown.choices.Find(c => c.Contains(prefillMission.displayName));
                    if (matchedType != null)
                    {
                        typeDropdown.value = matchedType;
                    }
                }
            }
            
            if (_dashboardRoot != null) _dashboardRoot.Add(overlay);
        }

        private MilitaryOperation _selectedOp = null;
        private bool _hasAutoSelectedFirstOp = false;
        private VisualElement _opsDetailView = null;
        private VisualElement _opsDetailContainer = null;

        private void PopulateBranchReadiness(CountryDataSO country)
        {
            if (_panelOps == null) return;
            
            var armyReady = _panelOps.Q<Label>("army-readiness");
            var armyEquip = _panelOps.Q<Label>("army-equip");
            if (armyReady != null) armyReady.text = $"{country.militaryReadiness:F0}% ready";
            if (armyEquip != null) armyEquip.text = $"{country.unitRifles:N0} rifles · {country.unitAPCs:N0} APCs · {country.unitTanks:N0} tanks";

            var afReady = _panelOps.Q<Label>("air-readiness");
            var afEquip = _panelOps.Q<Label>("air-equip");
            if (afReady != null) afReady.text = $"{country.militaryReadiness:F0}% ready";
            if (afEquip != null) afEquip.text = $"{country.unitFighters:N0} fighters · {country.unitBombers:N0} bombers · {country.unitUAVs:N0} drones";

            var navyReady = _panelOps.Q<Label>("navy-readiness");
            var navyEquip = _panelOps.Q<Label>("navy-equip");
            if (navyReady != null)
            {
                bool navyCritical = country.unitFrigates == 0 && country.unitPatrolBoats == 0 && country.unitSubmarines == 0;
                navyReady.text = navyCritical ? "critical" : $"{country.militaryReadiness:F0}% ready";
                navyReady.style.color = navyCritical ? new Color(240/255f, 80/255f, 80/255f) : Color.white;
            }
            if (navyEquip != null) navyEquip.text = $"{country.unitFrigates:N0} frigates · {country.unitPatrolBoats:N0} boats · {country.unitSubmarines:N0} subs";

            var rdfReady = _panelOps.Q<Label>("rdf-readiness");
            var rdfEquip = _panelOps.Q<Label>("rdf-equip");
            if (rdfReady != null) rdfReady.text = $"{country.rdfStats.readiness:F0}% ready";
            if (rdfEquip != null) rdfEquip.text = $"{country.rdfStats.personnel:N0} operators";
        }

        private void CheckOperationAlerts(CountryDataSO country)
        {
            var alertsContainer = _panelOps?.Q<VisualElement>("ops-alerts-container");
            var alertTitle = alertsContainer?.Q<Label>("alert-title");
            var alertDesc = alertsContainer?.Q<Label>("alert-desc");
            if (alertsContainer == null || alertTitle == null || alertDesc == null) return;

            bool navyCritical = country.unitFrigates == 0 && country.unitPatrolBoats == 0 && country.unitSubmarines == 0;
            
            if (navyCritical)
            {
                alertsContainer.style.display = DisplayStyle.Flex;
                alertTitle.text = "navy can't sustain a maritime operation";
                alertDesc.text = "0 frigates, no patrol boats or submarines available. procure or repair naval assets before assigning a coastal operation.";
            }
            else
            {
                alertsContainer.style.display = DisplayStyle.None;
            }
        }

        private void PopulateOperationsMasterList(CountryDataSO country, GameManager gm)
        {
            var opsMasterList = _panelOps?.Q<ScrollView>("ops-master-list");
            var lblCount      = _panelOps?.Q<Label>("lbl-active-ops-count");

            if (_opsDetailView == null)
            {
                _opsDetailView = _panelOps?.Q<VisualElement>("ops-detail-view") ?? _dashboardRoot?.Q<VisualElement>("ops-detail-view");
            }
            if (_opsDetailContainer == null)
            {
                _opsDetailContainer = _panelOps?.Q<VisualElement>("ops-detail-container") ?? _dashboardRoot?.Q<VisualElement>("ops-detail-container");
            }

            if (_opsDetailView != null)
            {
                _opsDetailView.RemoveFromHierarchy();
                if (_opsDetailContainer != null)
                {
                    _opsDetailContainer.Add(_opsDetailView);
                }
                _opsDetailView.style.display = DisplayStyle.None;
            }

            if (opsMasterList == null) return;
            opsMasterList.Clear();

            int count = country.activeOperations != null ? country.activeOperations.Count : 0;
            if (lblCount != null) lblCount.text = $"active operations ({count})";

            if (count == 0)
            {
                _selectedOp = null;
                _hasAutoSelectedFirstOp = false;
                if (_opsDetailView != null) _opsDetailView.style.display = DisplayStyle.None;

                var placeholder = new VisualElement();
                placeholder.style.backgroundColor = new Color(15/255f, 22/255f, 30/255f);
                placeholder.style.borderTopRightRadius = 6;
                placeholder.style.borderBottomRightRadius = 6;
                placeholder.style.borderBottomLeftRadius = 6;
                placeholder.style.borderTopLeftRadius = 6;
                placeholder.style.paddingTop = 25; placeholder.style.paddingBottom = 25;
                placeholder.style.alignItems = Align.Center;
                
                placeholder.Add(new Label("NO ACTIVE OPERATIONS") { style = { color = new Color(1,1,1,0.3f), fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, letterSpacing = 2 } });
                placeholder.Add(new Label("Click '+ new operation' to plan and launch a military campaign.") { style = { color = new Color(150/255f, 160/255f, 170/255f, 0.5f), fontSize = 12, marginTop = 10 } });
                opsMasterList.Add(placeholder);
                return;
            }

            if (count > 0 && _selectedOp == null && !_hasAutoSelectedFirstOp)
            {
                _selectedOp = country.activeOperations[0];
                _hasAutoSelectedFirstOp = true;
            }

            foreach (var op in country.activeOperations)
            {
                var card = new VisualElement();
                card.AddToClassList("ops-branch-card");
                card.style.backgroundColor = new Color(15/255f, 22/255f, 30/255f);
                card.style.borderLeftWidth = 4;
                card.style.marginBottom = 15;
                card.style.paddingTop = 15; card.style.paddingBottom = 15;
                card.style.paddingLeft = 20; card.style.paddingRight = 20;
                card.style.borderTopRightRadius = 6;
                card.style.borderBottomRightRadius = 6;
                card.style.flexShrink = 0;
                
                Color threatCol = op.enemyStrengthEst > 70f ? new Color(1f, 0.3f, 0.3f)
                                : op.enemyStrengthEst > 40f ? new Color(1f, 0.7f, 0.1f)
                                : new Color(0.14f, 0.93f, 0.34f);
                card.style.borderLeftColor = threatCol;

                bool isSelected = _selectedOp != null && _selectedOp.operationId == op.operationId;

                if (isSelected)
                {
                    card.style.backgroundColor = new Color(20/255f, 30/255f, 40/255f);
                }

                var rowContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center } };
                
                var leftDetails = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                string opNameLower = op.opName.ToLower();
                string displayName = opNameLower.StartsWith("operation") ? opNameLower : $"operation {opNameLower}";
                leftDetails.Add(new Label(displayName) { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } });
                int daysRunning = op.turnsActive * 30;
                string durationStr = op.turnsToDeploy > 0 ? $"deploying ({op.turnsToDeploy} mo)" : (daysRunning == 0 ? "day 1" : $"day {daysRunning}");
                
                string expandText = isSelected ? " · click to collapse details ▴" : " · click to view details ▾";
                leftDetails.Add(new Label(($"{op.type} · {op.regionName} · {durationStr}" + expandText).ToLower()) { style = { color = new Color(150/255f, 160/255f, 170/255f), fontSize = 12 } });
                
                rowContainer.Add(leftDetails);

                var rightProgress = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                
                var statusText = new Label(op.phase == "Engaged" ? "engaged" : "on track") { style = { color = new Color(150/255f, 160/255f, 170/255f), fontSize = 12, marginRight = 15 } };
                rightProgress.Add(statusText);

                int progressPct = op.phase == "Preparing" ? 5 : op.phase == "Advancing" ? 25
                                : op.phase == "Engaged"   ? 55 : op.phase == "Securing" ? 80 : 100;
                Color phaseCol = op.phase == "Engaged"  ? new Color(1f, 0.35f, 0.35f)
                               : op.phase == "Securing" ? new Color(0.14f, 0.93f, 0.34f)
                               : op.phase == "Advancing" ? new Color(0.1f, 0.6f, 1f)
                               : new Color(0.9f, 0.6f, 0.1f);

                var barBg = new VisualElement { style = { height = 6, width = 120, backgroundColor = new Color(1,1,1,0.1f), borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3 } };
                var barFill = new VisualElement { style = { height = 6, width = Length.Percent(progressPct), backgroundColor = phaseCol, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3 } };
                barBg.Add(barFill);
                rightProgress.Add(barBg);

                rowContainer.Add(rightProgress);
                card.Add(rowContainer);

                var capturedOp = op;
                card.RegisterCallback<ClickEvent>(evt => {
                    if (_opsDetailView != null && _opsDetailView.parent == card && _opsDetailView.Contains(evt.target as VisualElement))
                    {
                        return;
                    }
                    if (_selectedOp != null && _selectedOp.operationId == capturedOp.operationId) 
                        _selectedOp = null;
                    else 
                        _selectedOp = capturedOp; 
                    PopulateOperationsMasterList(country, gm); 
                });
                
                if (isSelected)
                {
                    if (_opsDetailView != null)
                    {
                        _opsDetailView.RemoveFromHierarchy();
                        card.Add(_opsDetailView);
                        _opsDetailView.style.display = DisplayStyle.Flex;
                        SelectOperation(op, country, gm);
                    }
                }

                opsMasterList.Add(card);
            }

            if (_selectedOp == null && _opsDetailView != null)
            {
                _opsDetailView.style.display = DisplayStyle.None;
            }
            
            var btnLaunch = _panelOps?.Q<Button>("btn-launch-new");
            if (btnLaunch != null) { btnLaunch.clicked -= ShowLaunchOpModalNoArgs; btnLaunch.clicked += ShowLaunchOpModalNoArgs; }

            var btnInbox = _panelOps?.Q<Button>("btn-inbox");
            if (btnInbox != null)
            {
                btnInbox.clicked -= ShowInboxModal;
                btnInbox.clicked += ShowInboxModal;

                var badge = _panelOps?.Q<Label>("inbox-unread-badge");
                var icon  = _panelOps?.Q<Label>("inbox-icon");

                int unreadCount = country.inboxMessages?.FindAll(m => !m.isRead).Count ?? 0;
                int totalCount  = country.inboxMessages?.Count ?? 0;

                if (unreadCount > 0)
                {
                    btnInbox.style.backgroundColor = new Color(220/255f, 40/255f, 40/255f, 0.35f);
                    btnInbox.style.borderTopColor = btnInbox.style.borderBottomColor =
                        btnInbox.style.borderLeftColor = btnInbox.style.borderRightColor =
                        new Color(255/255f, 80/255f, 80/255f);

                    if (icon != null)
                    {
                        icon.text = FaIcons.EnvelopeOpen;
                        icon.style.color = new Color(255/255f, 80/255f, 80/255f);
                    }

                    if (badge != null)
                    {
                        badge.text = unreadCount > 99 ? "99+" : unreadCount.ToString();
                        badge.style.display = DisplayStyle.Flex;
                    }
                }
                else
                {
                    btnInbox.style.backgroundColor = Color.clear;
                    btnInbox.style.borderTopColor = btnInbox.style.borderBottomColor =
                        btnInbox.style.borderLeftColor = btnInbox.style.borderRightColor =
                        new Color(255/255f, 255/255f, 255/255f, 0.15f);

                    if (icon != null)
                    {
                        icon.text = FaIcons.Envelope;
                        icon.style.color = totalCount > 0
                            ? new Color(0f, 210/255f, 255/255f)
                            : new Color(0.5f, 0.6f, 0.7f);
                    }

                    if (badge != null) badge.style.display = DisplayStyle.None;
                }
            }
            RefreshConsoleLogUI(country);
        }

        private void LogOperationalEvent(CountryDataSO country, string message)
        {
            if (country.operationalLogs == null) country.operationalLogs = new List<string>();
            string timestamp = GameManager.Instance?.TurnSystem?.CurrentDate ?? System.DateTime.Now.ToString("dd MMM yyyy");
            string entry = $"[{timestamp}] {message}";
            country.operationalLogs.Add(entry);
            while (country.operationalLogs.Count > 50) country.operationalLogs.RemoveAt(0);
            RefreshConsoleLogUI(country);
        }

        private void RefreshConsoleLogUI(CountryDataSO country)
        {
            var consoleScroll = _panelOps?.Q<ScrollView>("ops-console-scroll");
            if (consoleScroll == null) return;
            consoleScroll.contentContainer?.Clear();
            if (country.operationalLogs == null) country.operationalLogs = new List<string>();
            foreach (var log in country.operationalLogs)
            {
                var lbl = new Label(log) { style = { color = new Color(0f, 0.7f, 1f), fontSize = 10, marginBottom = 2 } };
                consoleScroll.Add(lbl);
            }
            consoleScroll.schedule.Execute(() => {
                if (consoleScroll.verticalScroller != null) consoleScroll.scrollOffset = new Vector2(0, consoleScroll.verticalScroller.highValue);
            }).StartingIn(50);
        }

        private void SelectOperation(MilitaryOperation op, CountryDataSO country, GameManager gm)
        {
            var view = _opsDetailView ?? _panelOps?.Q<VisualElement>("ops-detail-view") ?? _dashboardRoot?.Q<VisualElement>("ops-detail-view");
            if (view == null) return;

            void SetText(string name, string txt)
            {
                var lbl = view.Q<Label>(name);
                if (lbl != null) lbl.text = txt;
            }

            SetText("det-op-name", op.opName.ToUpper());
            SetText("det-op-type", $"{op.type}  ·  {op.regionName}");
            SetText("det-op-status", op.phase.ToUpper());
            SetText("det-start-date", "Commander: " + (string.IsNullOrEmpty(op.commander) ? "Unassigned" : op.commander));

            SetText("stat-region", op.regionName);
            SetText("stat-objective", string.IsNullOrEmpty(op.objective) ? op.type : op.objective);
            int daysActive = op.turnsActive * 30;
            SetText("stat-duration", op.turnsToDeploy > 0 ? $"Deploying ({op.turnsToDeploy} mo)" : (daysActive == 0 ? "Day 1" : $"{daysActive} Days Active"));

            float successChance = CalculateOpSuccess(op, country);
            SetText("stat-chance", $"{successChance:F0}%");
            SetText("stat-intel", op.cooldownIntel == 0 ? "Good" : "Stale");

            var lblThreat = view.Q<Label>("stat-threat");
            if (lblThreat != null)
            {
                string threatTxt = op.enemyStrengthEst > 70f ? "High" : op.enemyStrengthEst > 40f ? "Medium" : "Low";
                lblThreat.text = threatTxt;
                lblThreat.style.color = op.enemyStrengthEst > 70f ? new Color(1f,0.3f,0.3f)
                                      : op.enemyStrengthEst > 40f ? new Color(1f, 0.7f, 0f)
                                      : new Color(0.14f, 0.93f, 0.34f);
            }

            float opCostB = (op.costPerMonth / 1000f) * country.exchangeRate;
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            SetText("stat-cost", $"{sym}{opCostB:F2}B / mo");

            SetText("stat-success-main", $"{successChance:F0}%");
            SetText("out-prob", $"{successChance:F0}%");
            SetText("out-losses", op.casualtiesSustained > 500 ? "High" : op.casualtiesSustained > 100 ? "Medium" : "Low");
            SetText("out-civ", (op.deployedHelis > 0 || op.deployedTanks > 10) ? "Medium" : "Low");
            SetText("out-pol", successChance > 70 ? "+3 Approval" : successChance < 40 ? "-5 Approval" : "Neutral");
            SetText("out-intel", op.cooldownIntel == 0 ? "Good" : "Stale");

            var forcesList = view.Q<ScrollView>("det-forces-list");
            if (forcesList != null)
            {
                forcesList.contentContainer?.Clear();
                if (op.deployedTroops   > 0) AddForceStat(forcesList, "Infantry",       op.deployedTroops,    country.unitInfantry + op.deployedTroops);
                if (op.deployedRDF      > 0) AddForceStat(forcesList, "RDF Spec-Ops",   op.deployedRDF,       country.rdfStats.personnel + op.deployedRDF);
                if (op.deployedAirForce > 0) AddForceStat(forcesList, "Air Force Crew", op.deployedAirForce,  country.airForceStats.personnel + op.deployedAirForce);
                if (op.deployedNavy     > 0) AddForceStat(forcesList, "Navy Crew",      op.deployedNavy,      country.navyStats.personnel + op.deployedNavy);
                if (forcesList.contentContainer == null || forcesList.contentContainer.childCount == 0) 
                    forcesList.Add(new Label("No ground forces assigned.") { style = { color = Color.grey, fontSize = 9 } });
            }

            var equipList = view.Q<ScrollView>("det-equip-list");
            if (equipList != null)
            {
                equipList.contentContainer?.Clear();
                if (op.deployedTanks      > 0) AddForceStat(equipList, "Tanks",          op.deployedTanks,     country.unitTanks + op.deployedTanks);
                if (op.deployedAPCs       > 0) AddForceStat(equipList, "APCs",           op.deployedAPCs,      country.unitAPCs + op.deployedAPCs);
                if (op.deployedFighters   > 0) AddForceStat(equipList, "Fighter Jets",   op.deployedFighters,  country.unitFighters + op.deployedFighters);
                if (op.deployedBombers    > 0) AddForceStat(equipList, "Bombers",        op.deployedBombers,   country.unitBombers  + op.deployedBombers);
                if (op.deployedUAVs       > 0) AddForceStat(equipList, "UAVs",           op.deployedUAVs,      country.unitUAVs     + op.deployedUAVs);
                if (op.deployedHelis      > 0) AddForceStat(equipList, "Attack Helis",   op.deployedHelis,     country.unitAttackHelis + op.deployedHelis);
                if (op.deployedArtillery  > 0) AddForceStat(equipList, "Artillery",     op.deployedArtillery, country.unitArtillery + op.deployedArtillery);
                if (op.deployedFrigates   > 0) AddForceStat(equipList, "Frigates",       op.deployedFrigates,  country.unitFrigates + op.deployedFrigates);
                if (op.deployedDestroyers > 0) AddForceStat(equipList, "Destroyers",     op.deployedDestroyers, country.unitDestroyers + op.deployedDestroyers);
                if (op.deployedSubmarines > 0) AddForceStat(equipList, "Submarines",     op.deployedSubmarines, country.unitSubmarines + op.deployedSubmarines);
                if (op.deployedPatrolBoats > 0) AddForceStat(equipList, "Patrol Boats",   op.deployedPatrolBoats, country.unitPatrolBoats + op.deployedPatrolBoats);
                if (equipList.contentContainer == null || equipList.contentContainer.childCount == 0) 
                    equipList.Add(new Label("No equipment assigned.") { style = { color = Color.grey, fontSize = 9 } });
            }

            var statusList = view.Q<ScrollView>("det-status-list");
            if (statusList != null)
            {
                statusList.contentContainer?.Clear();

                void AddIntelRow(string label, string value, Color col)
                {
                    var r = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6 } };
                    r.Add(new Label(label) { style = { color = new Color(0.55f,0.65f,0.75f), fontSize = 11 } });
                    r.Add(new Label(value) { style = { color = col, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                    statusList.Add(r);
                }

                Color phaseColStat = op.phase == "Engaged"   ? new Color(1f, 0.35f, 0.35f)
                                   : op.phase == "Securing"  ? new Color(0.14f, 0.93f, 0.34f)
                                   : op.phase == "Advancing" ? new Color(0.1f, 0.6f, 1f)
                                   : new Color(0.9f, 0.6f, 0.1f);

                statusList.Add(new Label("MISSION INTEL") { style = { color = new Color(0.4f,0.45f,0.5f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6, marginTop = 2 } });

                AddIntelRow("Current Phase",    op.phase,                                  phaseColStat);
                AddIntelRow("Objective Progress", $"{op.objectiveProgress:F0}%",           new Color(0.14f, 0.93f, 0.34f));
                AddIntelRow("Days Active",       daysActive == 0 ? "< 1 mo" : $"{daysActive} days", Color.white);
                AddIntelRow("Commander",         string.IsNullOrEmpty(op.commander) ? "TBA" : op.commander, Color.white);
                AddIntelRow("Enemy Est.",        $"{op.enemyStrengthEst:F0}% strength",    op.enemyStrengthEst > 60f ? new Color(1f,0.35f,0.35f) : Color.white);
                AddIntelRow("Territory Ctrl.",  $"{op.territoryCaptured:F0}%",             new Color(0.14f, 0.93f, 0.34f));
                AddIntelRow("Troop Morale",     $"{op.morale:F0}%",                         op.morale < 40f ? new Color(1f,0.35f,0.35f) : op.morale < 65f ? new Color(1f,0.7f,0.1f) : Color.white);
                AddIntelRow("Friendly Casualties", $"{op.casualtiesSustained:N0}",           op.casualtiesSustained > 500 ? new Color(1f,0.35f,0.35f) : Color.white);
                AddIntelRow("Enemy Casualties",   $"{op.enemyCasualtiesInflicted:N0}",        new Color(0.14f, 0.93f, 0.34f));

                var contrList = op.GetContributionsMigrated();
                foreach (var c in contrList)
                {
                    if (c.casualtiesSuffered > 0 || c.equipmentLost > 0)
                    {
                        AddIntelRow($"{c.branch.ToString().ToUpper()} Losses", $"{c.casualtiesSuffered:N0} KIA | {c.equipmentLost:N0} Equip", new Color(1f, 0.5f, 0.5f));
                    }
                }

                if (op.deployedAirForce > 0)
                {
                    AddIntelRow("Air Recon Target", "Command Bunker / Ammo Depot (Ready)", new Color(0.1f, 0.7f, 1f));
                }

                var dividerMove = new VisualElement { style = { height = 1, backgroundColor = new Color(1,1,1,0.07f), marginTop = 4, marginBottom = 8 } };
                statusList.Add(dividerMove);

                if (op.turnsToDeploy > 0)
                {
                    statusList.Add(new Label("TROOP & EQUIPMENT MOVEMENT (TRANSIT)") { style = { color = new Color(1f, 0.7f, 0.2f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 } });
                    AddIntelRow("Deployment Status", $"In Transit ({op.turnsToDeploy} mo remaining)", new Color(1f, 0.7f, 0.2f));
                    if (op.deployedTroops > 0) AddIntelRow("Infantry Convoys", $"{op.deployedTroops:N0} Soldiers in transit to {op.regionName}", Color.white);
                    if (op.deployedTanks > 0 || op.deployedAPCs > 0) AddIntelRow("Armored Transports", $"{op.deployedTanks:N0} Tanks, {op.deployedAPCs:N0} APCs on heavy transports", Color.white);
                    if (op.deployedFighters > 0 || op.deployedHelis > 0) AddIntelRow("Air Relocation", $"{op.deployedFighters:N0} Jets, {op.deployedHelis:N0} Helis staging at airbase", Color.white);
                    if (op.deployedFrigates > 0 || op.deployedDestroyers > 0) AddIntelRow("Naval Movement", $"{op.deployedFrigates:N0} Frigates, {op.deployedDestroyers:N0} Destroyers en route", Color.white);
                }
                else
                {
                    statusList.Add(new Label("TROOP & EQUIPMENT MOVEMENT (ACTIVE PATROLS)") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 } });
                    AddIntelRow("Deployment Status", "Deployed & Positioned in Theatre", new Color(0.14f, 0.93f, 0.34f));
                    if (op.deployedTroops > 0) AddIntelRow("Infantry Positions", $"{op.deployedTroops:N0} Soldiers on active grid patrols & FOBs", Color.white);
                    if (op.deployedTanks > 0 || op.deployedAPCs > 0) AddIntelRow("Armor Movements", $"{op.deployedTanks:N0} Tanks, {op.deployedAPCs:N0} APCs on offensive patrols", Color.white);
                    if (op.deployedFighters > 0 || op.deployedHelis > 0) AddIntelRow("Air Operations", $"{op.deployedFighters:N0} Jets, {op.deployedHelis:N0} Helis on Combat Air Patrol", Color.white);
                    if (op.deployedFrigates > 0 || op.deployedDestroyers > 0) AddIntelRow("Naval Blockade", $"{op.deployedFrigates:N0} Frigates, {op.deployedDestroyers:N0} Destroyers patrolling coast", Color.white);
                }

                var divider = new VisualElement { style = { height = 1, backgroundColor = new Color(1,1,1,0.07f), marginTop = 4, marginBottom = 8 } };
                statusList.Add(divider);
                statusList.Add(new Label("SUPPLY STATUS") { style = { color = new Color(0.4f,0.45f,0.5f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 6 } });

                int fuelFilled  = Mathf.RoundToInt(Mathf.Clamp(country.reserveFuel  / 10f, 0, 10));
                int ammoFilled  = Mathf.RoundToInt(Mathf.Clamp(country.reserveAmmo  / 10f, 0, 10));

                AddStatusBar(statusList, "Fuel",  fuelFilled * 10, country.reserveFuel < 30f);
                AddStatusBar(statusList, "Ammo",  ammoFilled * 10, country.reserveAmmo < 30f);
                AddStatusBar(statusList, "Morale", Mathf.RoundToInt(op.morale), op.morale < 40f);
            }

            var resList = view.Q<ScrollView>("det-res-list");
            if (resList != null)
            {
                resList.contentContainer?.Clear();
                int fuelFilled  = Mathf.RoundToInt(Mathf.Clamp(country.reserveFuel  / 10f, 0, 10));
                int ammoFilled  = Mathf.RoundToInt(Mathf.Clamp(country.reserveAmmo  / 10f, 0, 10));
                AddResourceBar(resList, "Fuel Reserves",    fuelFilled, 10);
                AddResourceBar(resList, "Ammo Reserves",    ammoFilled, 10);
                AddResourceBar(resList, "Medical Supplies", Mathf.RoundToInt(Mathf.Clamp(country.reserveMedical / 10f, 0, 10)), 10);
            }

            SetupCommandDecisions(op, country, gm);

            var btnAbort = view.Q<Button>("btn-op-abort");
            if (btnAbort != null) { btnAbort.clicked -= AbortOperation; btnAbort.clicked += AbortOperation; }

            var btnSave = view.Q<Button>("btn-op-save");
            if (btnSave != null)
            {
                btnSave.clicked -= OnSaveClicked;
                btnSave.clicked += OnSaveClicked;
            }
        }

        private float CalculateOpSuccess(MilitaryOperation op, CountryDataSO country)
        {
            float strength = 1.0f + (op.deployedTroops / 1000f) * 0.5f + op.deployedTanks * 0.8f
                           + op.deployedAPCs * 0.4f + op.deployedFighters * 2.0f + op.deployedHelis * 1.0f
                           + op.deployedFrigates * 1.5f + op.deployedSubmarines * 2.0f
                           + (op.deployedRDF / 1000f) * 3.5f + op.deployedBombers * 3.0f + op.deployedUAVs * 1.2f;
            float base_ = Mathf.Clamp(40f + strength * 1.8f, 10f, 95f);
            base_ += (op.morale - 70f) * 0.1f;
            base_ -= (op.enemyStrengthEst - 50f) * 0.15f;
            if (country.unitRifles < country.armyStats.personnel) base_ *= 0.85f;
            return Mathf.Clamp(base_, 5f, 95f);
        }

        private void SetupCommandDecisions(MilitaryOperation op, CountryDataSO country, GameManager gm)
        {
            var view = _opsDetailView ?? _panelOps?.Q<VisualElement>("ops-detail-view") ?? _dashboardRoot?.Q<VisualElement>("ops-detail-view");
            var btnAirstrike = view?.Q<Button>("btn-action-airstrike");
            var cmdScroll = view?.Q<ScrollView>("det-cmd-decisions-scroll") 
                         ?? (btnAirstrike?.parent as ScrollView) 
                         ?? (btnAirstrike?.parent?.parent as ScrollView);

            if (cmdScroll == null) return;

            cmdScroll?.contentContainer?.Clear();

            bool hasAirForce = op.deployedAirForce > 0 || op.deployedFighters > 0 || op.deployedBombers > 0 || op.deployedHelis > 0 || op.deployedUAVs > 0;
            bool hasNavy     = op.deployedNavy > 0 || op.deployedFrigates > 0 || op.deployedDestroyers > 0 || op.deployedSubmarines > 0 || op.deployedPatrolBoats > 0;
            bool hasArmy     = op.deployedTroops > 0 || op.deployedTanks > 0 || op.deployedAPCs > 0 || op.deployedArtillery > 0;
            bool hasRDF      = op.deployedRDF > 0;

            void AddActionCard(string title, string desc, string costLine, string effectLine, string cooldownLabel, bool onCooldown, System.Action onConfirm)
            {
                var card = new VisualElement();
                card.style.backgroundColor = new Color(0.08f, 0.11f, 0.16f, 1f);
                card.style.borderTopWidth = card.style.borderBottomWidth = card.style.borderLeftWidth = card.style.borderRightWidth = 1;
                card.style.borderTopColor = card.style.borderBottomColor = card.style.borderLeftColor = card.style.borderRightColor = new Color(0.2f, 0.25f, 0.35f, 0.5f);
                card.style.borderTopLeftRadius = card.style.borderTopRightRadius = card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 6;
                card.style.paddingTop = 10; card.style.paddingBottom = 10; card.style.paddingLeft = 10; card.style.paddingRight = 10;
                card.style.marginBottom = 8;

                card.Add(new Label(title) { style = { color = Color.white, fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });
                card.Add(new Label(desc)  { style = { color = new Color(0.65f,0.72f,0.8f), fontSize = 11, whiteSpace = WhiteSpace.Normal, marginBottom = 6 } });

                if (!string.IsNullOrEmpty(costLine))
                    card.Add(new Label($"Cost: {costLine}") { style = { color = new Color(1f,0.7f,0.2f), fontSize = 11 } });
                if (!string.IsNullOrEmpty(effectLine))
                    card.Add(new Label($"Effect: {effectLine}") { style = { color = new Color(0.14f,0.93f,0.34f), fontSize = 11 } });

                card.Add(new Label($"Status: {cooldownLabel}") { style = { color = new Color(0.55f,0.6f,0.7f), fontSize = 10, marginTop = 4 } });

                var btn = new Button();
                btn.text = onCooldown ? cooldownLabel : $"EXECUTE: {title.ToUpper()}";
                btn.style.marginTop = 8; btn.style.height = 28; btn.style.fontSize = 11; btn.style.unityFontStyleAndWeight = FontStyle.Bold;
                btn.style.backgroundColor = onCooldown ? new Color(0.15f,0.15f,0.15f,0.8f) : new Color(0.08f,0.45f,0.22f,0.9f);
                btn.style.color = onCooldown ? new Color(0.5f,0.5f,0.5f) : Color.white;
                btn.SetEnabled(!onCooldown);
                btn.clicked += onConfirm;
                card.Add(btn);
                cmdScroll.Add(card);
            }

            if (hasArmy)
            {
                AddActionCard(
                    "Artillery Barrage",
                    "Orders heavy artillery and tank bombardment on enemy defensive lines.",
                    "15 Ammo",
                    "-12% Enemy Strength  +8% Success Chance",
                    op.cooldownAirstrike > 0 ? $"On Cooldown ({op.cooldownAirstrike} turns)" : "Ready",
                    op.cooldownAirstrike > 0,
                    () => {
                        if (country.reserveAmmo < 15f) { NotificationManager.Instance?.AddNotification("INSUFFICIENT AMMO for artillery barrage.", Color.red); return; }
                        country.reserveAmmo -= 15f;
                        op.enemyStrengthEst = Mathf.Max(10f, op.enemyStrengthEst - 12f);
                        op.cooldownAirstrike = 2;
                        LogOperationalEvent(country, $"ARTILLERY BARRAGE EXECUTED in {op.regionName}. Enemy defensive line suppressed. (-15 Ammo)");
                        NotificationManager.Instance?.AddNotification($"BARRAGE EXECUTED in {op.regionName}. Enemy suppressed.", new Color(1f,0.5f,0.1f));
                        SelectOperation(op, country, gm);
                        PopulateOperationsMasterList(country, gm);
                    }
                );
            }

            if (hasAirForce)
            {
                AddActionCard(
                    "Call Air Strike",
                    "Precision strike on enemy positions. Suppresses enemy strength and eliminates hostiles.",
                    "20 Fuel",
                    "-15% Enemy Strength  ·  Inflicts Enemy KIA",
                    op.cooldownAirstrike > 0 ? $"On Cooldown ({op.cooldownAirstrike} turns)" : "Ready",
                    op.cooldownAirstrike > 0,
                    () => ExecuteAirStrike(op, country, gm, "Enemy Defensive Positions")
                );

                AddActionCard(
                    "Deploy Recon Drone",
                    "Launches surveillance drone to scan target region, revealing exact enemy counts and HVT targets.",
                    "15 Fuel",
                    "+8% Success Chance  ·  Full Recon Report & HVT Targets",
                    op.cooldownRecon > 0 ? $"On Cooldown ({op.cooldownRecon} turns)" : "Ready",
                    op.cooldownRecon > 0,
                    () => {
                        if (country.reserveFuel < 15f) { NotificationManager.Instance?.AddNotification("INSUFFICIENT FUEL for recon drone.", Color.red); return; }
                        country.reserveFuel -= 15f;
                        op.cooldownRecon = 2;
                        op.cooldownIntel = 0;

                        var targetRegion = country.regions?.Find(r => r.regionName == op.regionName);
                        if (targetRegion != null) targetRegion.intelCoverage = 100f;

                        int enemyPersonnel = Mathf.RoundToInt((op.enemyStrengthEst / 100f) * 2800 + UnityEngine.Random.Range(150, 450));
                        int technicals = Mathf.RoundToInt(enemyPersonnel / 140f);
                        int aaGuns = Mathf.RoundToInt(enemyPersonnel / 320f);
                        string primaryHVT = "Insurgent Ammunition Depot & Supply Hub";
                        string secondaryHVT = "Regional Command Bunker & Comms Array";

                        LogOperationalEvent(country, $"AIR RECON REPORT: {op.regionName} — Confirmed {enemyPersonnel:N0} insurgents, {technicals} technicals, {aaGuns} AA emplacements. HVT: Supply Depot. (-15 Fuel)");
                        NotificationManager.Instance?.AddNotification($"RECON ACTIVE: Surveillance feed online for {op.regionName}.", Color.cyan);
                        StateCraft.Systems.InboxSystem.Instance?.SendMessage(country, "AIR RECON COMMAND",
                            $"RECON DISPATCH: {op.regionName.ToUpper()} SECTOR REPORT",
                            $"HIGH-RESOLUTION AERIAL SURVEILLANCE REPORT FOR {op.opName.ToUpper()}\n\n" +
                            $"• Sector Target: {op.regionName}\n" +
                            $"• Confirmed Insurgent Personnel: {enemyPersonnel:N0} combatants\n" +
                            $"• Armored Technicals & Vehicles: {technicals:N0} vehicles\n" +
                            $"• Anti-Aircraft Emplacements: {aaGuns:N0} positions\n" +
                            $"• Primary High-Value Target (HVT): {primaryHVT}\n" +
                            $"• Secondary HVT Target: {secondaryHVT}\n\n" +
                            $"Surveillance drone telemetry synchronized with Command Inbox.",
                            "Critical");

                        SelectOperation(op, country, gm);
                        PopulateOperationsMasterList(country, gm);

                        ShowReconReportModal(op, country, gm, enemyPersonnel, technicals, aaGuns, primaryHVT, secondaryHVT);
                    }
                );
            }
            else
            {
                AddActionCard(
                    "Call Air Strike",
                    "Requires Air Force aircraft (Fighters/Bombers/Helis) assigned to this operation.",
                    "None",
                    "LOCKED — Air Force assets required",
                    "REQUIRES AIR FORCE BRANCH",
                    true,
                    null
                );
            }

            if (hasNavy)
            {
                AddActionCard(
                    "Naval Shore Bombardment",
                    "Orders naval frigates and destroyers to bombard enemy coastal strongholds.",
                    "25 Fuel  ·  20 Ammo",
                    "-20% Enemy Strength  +15% Success Chance",
                    op.cooldownAirstrike > 0 ? $"On Cooldown ({op.cooldownAirstrike} turns)" : "Ready",
                    op.cooldownAirstrike > 0,
                    () => {
                        if (country.reserveFuel < 25f || country.reserveAmmo < 20f) { NotificationManager.Instance?.AddNotification("INSUFFICIENT SUPPLIES for naval bombardment.", Color.red); return; }
                        country.reserveFuel -= 25f;
                        country.reserveAmmo -= 20f;
                        op.enemyStrengthEst = Mathf.Max(10f, op.enemyStrengthEst - 20f);
                        op.cooldownAirstrike = 3;
                        LogOperationalEvent(country, $"NAVAL BOMBARDMENT EXECUTED off {op.regionName} coast. Enemy coastal bunkers destroyed.");
                        NotificationManager.Instance?.AddNotification($"NAVAL BOMBARDMENT EXECUTED in {op.regionName}.", Color.cyan);
                        SelectOperation(op, country, gm);
                        PopulateOperationsMasterList(country, gm);
                    }
                );
            }

            AddActionCard(
                "Reinforce Operation",
                "Send additional troops, tanks, air support, or naval vessels to strengthen the operation.",
                "Varies by units sent",
                "+Success Chance  +Friendly Strength",
                "Ready",
                false,
                () => ShowReinforceModal(op)
            );

            AddActionCard(
                "Request Intelligence",
                "Tasks intelligence assets to update threat assessments for this operation.",
                "5 Intel Budget",
                "+10% Success Chance  ·  Refreshes enemy estimate",
                op.cooldownIntel > 0 ? $"On Cooldown ({op.cooldownIntel} turns)" : "Ready",
                op.cooldownIntel > 0,
                () => {
                    op.enemyStrengthEst = Mathf.Clamp(op.enemyStrengthEst + UnityEngine.Random.Range(-15f, 3f), 10f, 100f);
                    op.cooldownIntel = 2;
                    LogOperationalEvent(country, $"INTEL REPORT RECEIVED for {op.opName}. Threat matrix updated.");
                    NotificationManager.Instance?.AddNotification($"INTEL UPDATE: Intelligence report received for {op.opName}.", new Color(0.4f,0.7f,1f));
                    StateCraft.Systems.InboxSystem.Instance?.SendMessage(country, "MILITARY INTELLIGENCE",
                        $"INTEL DISPATCH: {op.opName.ToUpper()} ({op.regionName.ToUpper()})",
                        $"OFFICIAL DISPATCH FOR OPERATION {op.opName.ToUpper()}\n\n" +
                        $"• Region: {op.regionName}\n" +
                        $"• Intel Confidence: 100%\n" +
                        $"• Updated Hostile Strength Est: {op.enemyStrengthEst:F0}%\n" +
                        $"• Territory Controlled: {op.territoryCaptured:F0}%\n" +
                        $"• Morale Level: {op.morale:F0}%\n" +
                        $"• Casualties Sustained: {op.casualtiesSustained:N0} KIA | Hostile KIA: {op.enemyCasualtiesInflicted:N0}\n\n" +
                        $"Field operatives confirm sector threat matrix successfully updated.",
                        "Important");
                    SelectOperation(op, country, gm);
                    PopulateOperationsMasterList(country, gm);
                }
            );
        }

        private void ShowReconReportModal(MilitaryOperation op, CountryDataSO country, GameManager gm, int enemyPersonnel, int technicals, int aaGuns, string primaryHVT, string secondaryHVT)
        {
            var root = _dashboardRoot ?? _panelOps?.panel?.visualTree;
            if (root == null) return;

            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.top = overlay.style.bottom = overlay.style.left = overlay.style.right = 0;
            overlay.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;
            root.Add(overlay);

            var modal = new VisualElement();
            modal.style.width = 650;
            modal.style.backgroundColor = new Color(10/255f, 16/255f, 24/255f);
            modal.style.borderLeftWidth = modal.style.borderRightWidth = modal.style.borderTopWidth = modal.style.borderBottomWidth = 1;
            modal.style.borderLeftColor = modal.style.borderRightColor = modal.style.borderTopColor = modal.style.borderBottomColor = new Color(0f, 0.8f, 1f, 0.4f);
            SetBorderRadius(modal.style, 8);
            modal.style.paddingLeft = modal.style.paddingRight = modal.style.paddingTop = modal.style.paddingBottom = 20;
            overlay.Add(modal);

            var headerRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 15, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.1f), paddingBottom = 10 } };
            var title = new Label($"AIR RECON SURVEILLANCE REPORT — {op.regionName.ToUpper()}") { style = { color = new Color(0f, 0.85f, 1f), fontSize = 15, unityFontStyleAndWeight = FontStyle.Bold } };
            headerRow.Add(title);
            var closeBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "✕" };
            closeBtn.style.backgroundColor = Color.clear; closeBtn.style.color = Color.grey; closeBtn.style.fontSize = 16;
            SetBorderWidth(closeBtn.style, 0);
            headerRow.Add(closeBtn);
            modal.Add(headerRow);

            var statusBox = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, backgroundColor = new Color(0f, 0.85f, 1f, 0.12f), marginBottom = 15 } };
            SetBorderRadius(statusBox.style, 4);
            statusBox.style.paddingLeft = statusBox.style.paddingRight = 10;
            statusBox.style.paddingTop = statusBox.style.paddingBottom = 6;
            statusBox.Add(new Label("• HIGH RESOLUTION DRONE FEED ACTIVE — 100% INTEL CONFIDENCE") { style = { color = new Color(0f, 0.85f, 1f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
            modal.Add(statusBox);

            var forceCard = new VisualElement() { style = { backgroundColor = new Color(0.06f, 0.09f, 0.14f), marginBottom = 12 } };
            SetBorderRadius(forceCard.style, 6);
            forceCard.style.paddingTop = forceCard.style.paddingBottom = forceCard.style.paddingLeft = forceCard.style.paddingRight = 12;
            forceCard.Add(new Label("ENEMY FORCE COMPOSITION & STRENGTH") { style = { color = new Color(0.7f, 0.75f, 0.8f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 8 } });

            void AddStatLine(string key, string val, Color valCol)
            {
                var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 5 } };
                r.Add(new Label(key) { style = { color = new Color(0.6f, 0.65f, 0.7f), fontSize = 12 } });
                r.Add(new Label(val) { style = { color = valCol, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
                forceCard.Add(r);
            }

            AddStatLine("Insurgent Ground Combatants:", $"{enemyPersonnel:N0} Personnel", new Color(1f, 0.35f, 0.35f));
            AddStatLine("Armed Technicals / Light Armor:", $"{technicals:N0} Vehicles", new Color(1f, 0.7f, 0.2f));
            AddStatLine("Anti-Air & Mortar Emplacements:", $"{aaGuns:N0} Fortified Positions", new Color(1f, 0.7f, 0.2f));
            AddStatLine("Estimated Overall Threat Level:", op.enemyStrengthEst > 60f ? "HIGH THREAT" : "MEDIUM THREAT", op.enemyStrengthEst > 60f ? new Color(1f,0.3f,0.3f) : new Color(1f,0.7f,0.2f));
            modal.Add(forceCard);

            var hvtCard = new VisualElement() { style = { backgroundColor = new Color(0.06f, 0.09f, 0.14f), marginBottom = 15 } };
            SetBorderRadius(hvtCard.style, 6);
            hvtCard.style.paddingTop = hvtCard.style.paddingBottom = hvtCard.style.paddingLeft = hvtCard.style.paddingRight = 12;
            hvtCard.Add(new Label("BOMBABLE HIGH-VALUE TARGETS (HVT IDENTIFIED)") { style = { color = new Color(1f, 0.7f, 0.2f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 8 } });

            var hvt1 = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6 } };
            hvt1.Add(new Label($"1. {primaryHVT}") { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } });
            hvt1.Add(new Label("[BOMB TARGET CONFIRMED]") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
            hvtCard.Add(hvt1);

            var hvt2 = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            hvt2.Add(new Label($"2. {secondaryHVT}") { style = { color = Color.white, fontSize = 12 } });
            hvt2.Add(new Label("[HIGH PRIORITY]") { style = { color = new Color(1f, 0.7f, 0.2f), fontSize = 11 } });
            hvtCard.Add(hvt2);
            modal.Add(hvtCard);

            var actionRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginTop = 5 } };
            actionRow.Add(new Label("Tactical Recommendation: Launch precision Air Strike on HVT Supply Depot.") { style = { color = new Color(0.6f, 0.7f, 0.8f), fontSize = 11, flexGrow = 1, whiteSpace = WhiteSpace.Normal, marginRight = 10 } });

            var btnAirStrikeNow = new Button() { text = "CALL AIR STRIKE NOW" };
            btnAirStrikeNow.style.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
            btnAirStrikeNow.style.color = Color.white;
            btnAirStrikeNow.style.fontSize = 11;
            btnAirStrikeNow.style.unityFontStyleAndWeight = FontStyle.Bold;
            btnAirStrikeNow.style.height = 32;
            btnAirStrikeNow.style.paddingLeft = 14; btnAirStrikeNow.style.paddingRight = 14;
            btnAirStrikeNow.clicked += () => {
                overlay.parent?.Remove(overlay);
                ExecuteAirStrike(op, country, gm, primaryHVT);
            };
            actionRow.Add(btnAirStrikeNow);
            modal.Add(actionRow);
        }

        private void ExecuteAirStrike(MilitaryOperation op, CountryDataSO country, GameManager gm, string targetName = "Enemy Positions")
        {
            if (country.reserveFuel < 20f)
            {
                NotificationManager.Instance?.AddNotification("INSUFFICIENT FUEL for air strike.", Color.red);
                return;
            }

            country.reserveFuel -= 20f;
            int killedEnemies = UnityEngine.Random.Range(120, 280);
            int destroyedVehicles = UnityEngine.Random.Range(2, 6);

            op.enemyCasualtiesInflicted += killedEnemies;
            op.enemyStrengthEst = Mathf.Max(10f, op.enemyStrengthEst - 15f);
            op.cooldownAirstrike = 3;

            LogOperationalEvent(country, $"AIR STRIKE EXECUTED on {targetName} in {op.regionName}. BDA: {killedEnemies:N0} insurgents killed (KIA), {destroyedVehicles} technicals destroyed. (-20 Fuel)");
            NotificationManager.Instance?.AddNotification($"AIR STRIKE BDA: {killedEnemies:N0} enemy combatants killed in {op.regionName}!", new Color(1f, 0.4f, 0.1f));
            StateCraft.Systems.NewsSystem.Instance?.PublishNews($"BREAKING NEWS: Air Strike executed on {targetName} in {op.regionName}! Bomb Damage Assessment confirms {killedEnemies:N0} insurgent combatants killed (KIA) and {destroyedVehicles} technicals destroyed.", true);
            StateCraft.Systems.InboxSystem.Instance?.SendMessage(country, "AIR COMBAT COMMAND",
                $"BDA DISPATCH: {targetName.ToUpper()} STRIKE",
                $"BOMB DAMAGE ASSESSMENT (BDA) DISPATCH\n\n" +
                $"• Operation Target Sector: {op.regionName}\n" +
                $"• Primary Target Neutralized: {targetName}\n" +
                $"• Confirmed Hostile Losses: {killedEnemies:N0} Insurgents Killed (KIA)\n" +
                $"• Armored Technicals Destroyed: {destroyedVehicles:N0} Vehicles\n" +
                $"• Aviation Supplies Consumed: -20 Barrels Fuel\n\n" +
                $"Air Force strike squadron executed precision run and returned safely to forward airbase.",
                "Critical");

            SelectOperation(op, country, gm);
            PopulateOperationsMasterList(country, gm);

            ShowAirStrikeBDAModal(op, targetName, killedEnemies, destroyedVehicles);
        }

        private void ShowAirStrikeBDAModal(MilitaryOperation op, string targetName, int killedEnemies, int destroyedVehicles)
        {
            var root = _dashboardRoot ?? _panelOps?.panel?.visualTree;
            if (root == null) return;

            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.top = overlay.style.bottom = overlay.style.left = overlay.style.right = 0;
            overlay.style.backgroundColor = new Color(0, 0, 0, 0.85f);
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;
            root.Add(overlay);

            var modal = new VisualElement();
            modal.style.width = 550;
            modal.style.backgroundColor = new Color(22/255f, 14/255f, 14/255f);
            modal.style.borderLeftWidth = modal.style.borderRightWidth = modal.style.borderTopWidth = modal.style.borderBottomWidth = 1;
            modal.style.borderLeftColor = modal.style.borderRightColor = modal.style.borderTopColor = modal.style.borderBottomColor = new Color(1f, 0.35f, 0.1f, 0.6f);
            SetBorderRadius(modal.style, 8);
            modal.style.paddingLeft = modal.style.paddingRight = modal.style.paddingTop = modal.style.paddingBottom = 20;
            overlay.Add(modal);

            var headerRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 12, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.1f), paddingBottom = 8 } };
            headerRow.Add(new Label($"AIR STRIKE BOMB DAMAGE ASSESSMENT — {op.regionName.ToUpper()}") { style = { color = new Color(1f, 0.45f, 0.1f), fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } });
            var closeBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "✕" };
            closeBtn.style.backgroundColor = Color.clear; closeBtn.style.color = Color.grey; closeBtn.style.fontSize = 16;
            SetBorderWidth(closeBtn.style, 0);
            headerRow.Add(closeBtn);
            modal.Add(headerRow);

            var targetBox = new VisualElement() { style = { backgroundColor = new Color(1f, 0.35f, 0.1f, 0.12f), marginBottom = 12 } };
            SetBorderRadius(targetBox.style, 4);
            targetBox.style.paddingTop = targetBox.style.paddingBottom = targetBox.style.paddingLeft = targetBox.style.paddingRight = 8;
            targetBox.Add(new Label($"TARGET NEUTRALIZED: {targetName.ToUpper()}") { style = { color = new Color(1f, 0.45f, 0.1f), fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
            modal.Add(targetBox);

            var statsCard = new VisualElement() { style = { backgroundColor = new Color(0.12f, 0.08f, 0.08f), marginBottom = 15 } };
            SetBorderRadius(statsCard.style, 6);
            statsCard.style.paddingTop = statsCard.style.paddingBottom = statsCard.style.paddingLeft = statsCard.style.paddingRight = 12;
            
            void AddBdaRow(string label, string val, Color col)
            {
                var r = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6 } };
                r.Add(new Label(label) { style = { color = new Color(0.7f, 0.65f, 0.65f), fontSize = 12 } });
                r.Add(new Label(val) { style = { color = col, fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold } });
                statsCard.Add(r);
            }

            AddBdaRow("Hostile Combatants Killed (KIA):", $"{killedEnemies:N0} Insurgents", new Color(1f, 0.3f, 0.3f));
            AddBdaRow("Technicals & Armor Destroyed:", $"{destroyedVehicles:N0} Vehicles", new Color(1f, 0.7f, 0.2f));
            AddBdaRow("Enemy Strength Suppression:", "-15%", new Color(0.14f, 0.93f, 0.34f));
            AddBdaRow("Operation Total Enemy KIA:", $"{op.enemyCasualtiesInflicted:N0} Killed", new Color(0.14f, 0.93f, 0.34f));
            modal.Add(statsCard);

            var btnOk = new Button(() => overlay.parent?.Remove(overlay)) { text = "CONFIRM ASSESSMENT" };
            btnOk.style.backgroundColor = new Color(0.8f, 0.25f, 0.1f);
            btnOk.style.color = Color.white;
            btnOk.style.fontSize = 11;
            btnOk.style.unityFontStyleAndWeight = FontStyle.Bold;
            btnOk.style.height = 34;
            modal.Add(btnOk);
        }

        private void AbortOperation()
        {
            var op = _selectedOp;
            var country = GameManager.Instance?.playerCountry;
            if (op == null || country == null) return;

            var contrList = op.GetContributionsMigrated();
            foreach (var c in contrList)
            {
                MilitaryOperation.ReturnToReserves(country, c.branch, c.GetUnitCountsDict(), c.personnelAssigned);
            }

            var completedOp = new StateCraft.Data.CompletedMilitaryOperation
            {
                opName = op.opName,
                regionName = op.regionName,
                type = op.type,
                outcome = "Aborted",
                durationMonths = op.turnsActive,
                casualtiesSustained = op.casualtiesSustained,
                equipmentLost = 0,
                territoryCaptured = op.territoryCaptured,
                politicalImpact = -3,
                dateCompleted = GameManager.Instance?.TurnSystem?.CurrentDate ?? "Unknown",
                rewards = "None"
            };

            country.completedOperations.Add(completedOp);
            while (country.completedOperations.Count > 20)
            {
                country.completedOperations.RemoveAt(0);
            }

            country.activeOperations.Remove(op);
            var rec = country.activeRecommendations.Find(r => r.regionName == op.regionName);
            if (rec != null) rec.isAddressed = false;
            _selectedOp = null;
            NotificationManager.Instance?.AddNotification($"TERMINATED: {op.opName} aborted. Forces returning.", Color.yellow);
            
            int totalTroops = 0;
            foreach (var c in contrList) totalTroops += c.personnelAssigned;
            LogOperationalEvent(country, $"ABORTED: Operation '{op.opName}' terminated in {op.regionName}. Returned {totalTroops:N0} personnel to reserves.");
            
            RefreshMilitaryData();
            GameManager.Instance?.SaveGame();
        }

        private void OnSaveClicked()
        {
            GameManager.Instance?.SaveGame();
        }

        private void AddForceStat(VisualElement container, string name, int deployed, int max)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 6 } };
            row.Add(new Label(name) { style = { color = Color.white, fontSize = 12 } });
            
            var right = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var barBg = new VisualElement() { style = { width = 50, height = 6, backgroundColor = new Color(1,1,1,0.1f), marginRight = 8, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3 } };
            float pct = max > 0 ? (float)deployed / max : 0;
            var barFill = new VisualElement() { style = { width = Length.Percent(pct * 100), height = 6, backgroundColor = new Color(0.14f, 0.93f, 0.34f), borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3 } };
            barBg.Add(barFill);
            
            right.Add(barBg);
            right.Add(new Label($"{deployed:N0}") { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, width = 50, unityTextAlign = TextAnchor.MiddleRight } });
            row.Add(right);
            container.Add(row);
        }

        private void AddStatusBar(VisualElement container, string name, int pct, bool isWarning = false)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
            row.Add(new Label(name) { style = { color = new Color(0.65f,0.72f,0.8f), fontSize = 12, width = 110 } });
            
            var barBg = new VisualElement() { style = { flexGrow = 1, height = 6, backgroundColor = new Color(1,1,1,0.1f), marginRight = 8, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3 } };
            var barFill = new VisualElement() { style = { width = Length.Percent(pct), height = 6, backgroundColor = isWarning ? new Color(1, 0.7f, 0) : new Color(0.14f, 0.93f, 0.34f), borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3 } };
            barBg.Add(barFill);
            row.Add(barBg);
            row.Add(new Label($"{pct}%") { style = { color = Color.white, fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, width = 40, unityTextAlign = TextAnchor.MiddleRight } });
            container.Add(row);
        }

        private void AddResourceBar(VisualElement container, string name, int filled, int total)
        {
            var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
            row.Add(new Label(name) { style = { color = new Color(0.65f,0.72f,0.8f), fontSize = 12, width = 120 } });
            
            var blocks = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexGrow = 1, marginRight = 8 } };
            for(int i=0; i<total; i++)
            {
                var b = new VisualElement() { style = { width = 9, height = 14, backgroundColor = i < filled ? new Color(0.14f, 0.93f, 0.34f) : new Color(1,1,1,0.1f), marginRight = 2, borderTopLeftRadius = 2, borderTopRightRadius = 2, borderBottomLeftRadius = 2, borderBottomRightRadius = 2 } };
                blocks.Add(b);
            }
            row.Add(blocks);
            container.Add(row);
        }
    }
}
