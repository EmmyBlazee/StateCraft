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
        private void RecruitGeneral()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.playerCountry == null) return;
            var country = gm.playerCountry;
            if (country.treasury >= 250f)
            {
                country.treasury -= 250f;
                // Add a random 1-2 star general
                string[] firstNames = { "John", "Ahmed", "David", "Sani", "Ibrahim", "James", "Victor" };
                string[] lastNames = { "Okafor", "Adeyemi", "Bello", "Ojo", "Musa", "Dogo", "Umar" };
                string name = $"{firstNames[Random.Range(0, firstNames.Length)]} {lastNames[Random.Range(0, lastNames.Length)]}";
                GeneralSpecialty spec = (GeneralSpecialty)Random.Range(0, 4);
                
                if (gm.WarSystem is Systems.WarSystem warSys)
                    warSys.PromoteGeneral(country, name, spec, Random.Range(1, 3));
                
                RefreshMilitaryData();
            }
            else
            {
                NotificationManager.Instance?.AddNotification("Insufficient Funds: Need ₦250M to promote General.", Color.red);
            }
        }

        private void InitializeRecruitTab(VisualElement root)
        {
            // Army Buttons
            var btnArmy100 = root.Q<Button>("BtnArmyRecruit100");
            if (btnArmy100 != null) btnArmy100.clicked += () => ShowPurchaseModal("Infantry Battalion", "INF", 0.005f, 100, true, false, 0);
            var btnArmy500 = root.Q<Button>("BtnArmyRecruit500");
            if (btnArmy500 != null) btnArmy500.clicked += () => ShowPurchaseModal("Infantry Battalion", "INF", 0.025f, 500, true, false, 0);
            var btnArmy1000 = root.Q<Button>("BtnArmyRecruit1000");
            if (btnArmy1000 != null) btnArmy1000.clicked += () => ShowPurchaseModal("Infantry Battalion", "INF", 0.05f, 1000, true, false, 0);

            // Air Force Buttons
            var btnAF25 = root.Q<Button>("BtnAFRecruit25");
            if (btnAF25 != null) btnAF25.clicked += () => ShowPurchaseModal("Airforce Flight Academy", "AIR_PERS", 0.01f, 25, true, false, 0);
            var btnAF50 = root.Q<Button>("BtnAFRecruit50");
            if (btnAF50 != null) btnAF50.clicked += () => ShowPurchaseModal("Airforce Flight Academy", "AIR_PERS", 0.02f, 50, true, false, 0);
            var btnAF150 = root.Q<Button>("BtnAFRecruit150");
            if (btnAF150 != null) btnAF150.clicked += () => ShowPurchaseModal("Airforce Flight Academy", "AIR_PERS", 0.06f, 150, true, false, 0);

            // Navy Buttons
            var btnNavy50 = root.Q<Button>("BtnNavyRecruit50");
            if (btnNavy50 != null) btnNavy50.clicked += () => ShowPurchaseModal("Naval Crew Training", "NAVY_PERS", 0.008f, 50, true, false, 0);
            var btnNavy100 = root.Q<Button>("BtnNavyRecruit100");
            if (btnNavy100 != null) btnNavy100.clicked += () => ShowPurchaseModal("Naval Crew Training", "NAVY_PERS", 0.016f, 100, true, false, 0);
            var btnNavy250 = root.Q<Button>("BtnNavyRecruit250");
            if (btnNavy250 != null) btnNavy250.clicked += () => ShowPurchaseModal("Naval Crew Training", "NAVY_PERS", 0.04f, 250, true, false, 0);

            // RDF Buttons (formerly Gendarmerie)
            var btnRDF250 = root.Q<Button>("BtnRDFRecruit250");
            if (btnRDF250 != null) btnRDF250.clicked += () => ShowPurchaseModal("Elite Spec-Ops RDF", "RDF_PERS", 0.20f, 250, true, false, 0);
            var btnRDF500 = root.Q<Button>("BtnRDFRecruit500");
            if (btnRDF500 != null) btnRDF500.clicked += () => ShowPurchaseModal("Elite Spec-Ops RDF", "RDF_PERS", 0.40f, 500, true, false, 0);
            var btnRDF1000 = root.Q<Button>("BtnRDFRecruit1000");
            if (btnRDF1000 != null) btnRDF1000.clicked += () => ShowPurchaseModal("Elite Spec-Ops RDF", "RDF_PERS", 0.80f, 1000, true, false, 0);
        }

        private void PopulateRecruit(CountryDataSO country)
        {
            if (_root == null) return;
            if (_panelRecruit == null) return;

            // Overview Stats
            long totalPop = country.population;
            int availableManpower = UnityEngine.Mathf.FloorToInt(totalPop * 0.15f); 
            int militaryEligible = UnityEngine.Mathf.FloorToInt(availableManpower * 0.6f);
            
            var lblManpower = _panelRecruit.Q<Label>("RctManpower");
            if (lblManpower != null) lblManpower.text = availableManpower.ToString("N0");

            int totalInTraining = GetTotalInTraining(country, "INF") + GetTotalInTraining(country, "NAVY_PERS") + GetTotalInTraining(country, "AIR_PERS") + GetTotalInTraining(country, "RDF_PERS");
            var lblInTraining = _panelRecruit.Q<Label>("RctInTraining");
            if (lblInTraining != null) lblInTraining.text = totalInTraining.ToString("N0");
            
            int monthlyRecruits = totalInTraining > 0 ? totalInTraining / 6 : 0;
            var lblMonthlyRecruits = _panelRecruit.Q<Label>("RctMonthlyRecruits");
            if (lblMonthlyRecruits != null) lblMonthlyRecruits.text = $"+{monthlyRecruits:N0}";
            
            float monthlyCostB = (totalInTraining * 0.000005f); // Rough estimate of $5k per recruit training cost
            var lblCost = _panelRecruit.Q<Label>("RctMonthlyCost");
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            if (lblCost != null) lblCost.text = $"{sym}{monthlyCostB * country.exchangeRate:F1}B";
            
            float efficiency = (country.armyStats.readiness + country.armyStats.morale) / 2f;
            var lblEfficiency = _panelRecruit.Q<Label>("RctEfficiency");
            if (lblEfficiency != null) lblEfficiency.text = $"{UnityEngine.Mathf.Clamp(efficiency, 0, 100):F0}%";
            
            // RESERVES & MANPOWER
            int activePersonnel = country.armyStats.personnel + country.navyStats.personnel + country.airForceStats.personnel + country.rdfStats.personnel + country.nationalPoliceSize;
            int reservePersonnel = UnityEngine.Mathf.FloorToInt(activePersonnel * 1.5f);
            int veterans = UnityEngine.Mathf.FloorToInt(activePersonnel * 0.8f);
            
            var lblResvPop = _panelRecruit.Q<Label>("ResvAvailPop");
            if (lblResvPop != null) lblResvPop.text = availableManpower.ToString("N0");
            
            var lblResvEligible = _panelRecruit.Q<Label>("ResvMilEligible");
            if (lblResvEligible != null) lblResvEligible.text = militaryEligible.ToString("N0");
            
            var lblResvActive = _panelRecruit.Q<Label>("ResvActivePersonnel");
            if (lblResvActive != null) lblResvActive.text = activePersonnel.ToString("N0");
            
            var lblResvReserve = _panelRecruit.Q<Label>("ResvReservePersonnel");
            if (lblResvReserve != null) lblResvReserve.text = reservePersonnel.ToString("N0");
            
            var lblResvVet = _panelRecruit.Q<Label>("ResvVeterans");
            if (lblResvVet != null) lblResvVet.text = veterans.ToString("N0");

            // ARMY ACADEMY
            int infantryCapacity = 5000 + (country.baseArmyCount * 15000);
            int inTrainingInfantry = GetTotalInTraining(country, "INF");
            UpdateAcademyCard(_panelRecruit, "Army", infantryCapacity, inTrainingInfantry, country.armyStats.readiness);

            // AIR FORCE ACADEMY
            int airCapacity = 500 + (country.baseAirForceCount * 2000);
            int inTrainingAir = GetTotalInTraining(country, "AIR_PERS");
            UpdateAcademyCard(_panelRecruit, "AF", airCapacity, inTrainingAir, country.airForceStats.readiness);

            // NAVY ACADEMY
            int navyCapacity = 1000 + (country.baseNavyCount * 3000);
            int inTrainingNavy = GetTotalInTraining(country, "NAVY_PERS");
            UpdateAcademyCard(_panelRecruit, "Navy", navyCapacity, inTrainingNavy, country.navyStats.readiness);

            // RDF ACADEMY
            int rdfCapacity = 200 + (country.baseRDFCount * 1000);
            int inTrainingRDF = GetTotalInTraining(country, "RDF_PERS");
            UpdateAcademyCard(_panelRecruit, "RDF", rdfCapacity, inTrainingRDF, country.rdfStats.readiness);

            // ACTIVE TRAINING QUEUE
            UpdateQueueUI(country, "INF", "QueueArmy");
            UpdateQueueUI(country, "AIR_PERS", "QueueAF");
            UpdateQueueUI(country, "NAVY_PERS", "QueueNavy");
            UpdateQueueUI(country, "RDF_PERS", "QueueRDF");

            UpdateRecruitSummary(country);

            PopulateWeapons(country);
            PopulateBases(country);
        }

        private void UpdateAcademyCard(VisualElement root, string prefix, int capacity, int inTraining, float readiness)
        {
            var lblCap = root.Q<Label>($"{prefix}AcadCapacity");
            if (lblCap != null) lblCap.text = capacity.ToString("N0");

            var lblTrainees = root.Q<Label>($"{prefix}AcadTrainees");
            if (lblTrainees != null) lblTrainees.text = inTraining.ToString("N0");

            var barReadiness = root.Q<ProgressBar>($"{prefix}AcadReadinessBar");
            if (barReadiness != null) barReadiness.value = readiness;
            var lblReadiness = root.Q<Label>($"{prefix}AcadReadinessPct");
            if (lblReadiness != null) lblReadiness.text = $"{readiness:F0}%";

            // Graduation rate can be based on readiness
            float gradRate = Mathf.Clamp(readiness + 5f, 50f, 98f);
            var barGrad = root.Q<ProgressBar>($"{prefix}AcadGradBar");
            if (barGrad != null) barGrad.value = gradRate;
            var lblGrad = root.Q<Label>($"{prefix}AcadGradPct");
            if (lblGrad != null) lblGrad.text = $"{gradRate:F0}%";
        }

        private void UpdateQueueUI(CountryDataSO country, string id, string prefix)
        {
            if (country.trainingPipelines == null) return;
            if (_panelRecruit == null) return;
            
            int totalCount = 0;
            int minMonths = 6;
            bool isActive = false;

            foreach (var p in country.trainingPipelines)
            {
                if (p.id == id && p.isPersonnel)
                {
                    totalCount += p.amount;
                    if (p.monthsRemaining < minMonths) minMonths = p.monthsRemaining;
                    isActive = true;
                }
            }

            var countLbl = _panelRecruit.Q<Label>($"{prefix}Count");
            var bar = _panelRecruit.Q<ProgressBar>($"{prefix}Bar");
            var pctLbl = _panelRecruit.Q<Label>($"{prefix}Pct");
            var etaLbl = _panelRecruit.Q<Label>($"{prefix}ETA");
            
            if (isActive)
            {
                if (countLbl != null)
                {
                    countLbl.text = totalCount.ToString("N0");
                    if (countLbl.parent != null && countLbl.parent.parent != null)
                    {
                        countLbl.parent.parent.style.opacity = 1f;
                    }
                }
                
                int currentDay = 1;
                if (StateCraft.Core.GameManager.Instance != null && StateCraft.Core.GameManager.Instance.TurnSystem != null)
                {
                    currentDay = ((StateCraft.Core.TurnSystem)StateCraft.Core.GameManager.Instance.TurnSystem).currentDay;
                }
                
                float totalDays = 6f * 30f;
                float daysLeft = ((minMonths - 1) * 30f) + (30f - currentDay);
                if (daysLeft < 0) daysLeft = 0;
                
                float pct = ((totalDays - daysLeft) / totalDays) * 100f;

                if (bar != null) bar.value = UnityEngine.Mathf.Clamp(pct, 2f, 100f);
                if (pctLbl != null) pctLbl.text = $"{pct:F0}%";
                
                if (etaLbl != null) 
                {
                    if (minMonths == 1)
                    {
                        int daysRemaining = 30 - currentDay;
                        etaLbl.text = $"ETA: {(daysRemaining > 0 ? daysRemaining : 1)} Days";
                    }
                    else
                    {
                        etaLbl.text = $"ETA: {minMonths} Mo";
                    }
                }
            }
            else
            {
                if (countLbl != null)
                {
                    countLbl.text = "0";
                    if (countLbl.parent != null && countLbl.parent.parent != null)
                    {
                        countLbl.parent.parent.style.opacity = 0.35f;
                    }
                }
                if (bar != null) bar.value = 0f;
                if (pctLbl != null) pctLbl.text = "0%";
                if (etaLbl != null) etaLbl.text = "Idle";
            }
        }

        private void UpdateRecruitSummary(CountryDataSO country)
        {
            if (_panelRecruit == null || country == null) return;
            
            var lblTotal = _panelRecruit.Q<Label>("SumTotalTrainees");
            var lblAvgTime = _panelRecruit.Q<Label>("SumAvgTime");
            var lblGradRate = _panelRecruit.Q<Label>("SumGradRate");
            var lblRetention = _panelRecruit.Q<Label>("SumRetentionRate");
            
            int totalTrainees = 0;
            int totalMonths = 0;
            int count = 0;
            
            if (country.trainingPipelines != null)
            {
                foreach (var p in country.trainingPipelines)
                {
                    if (p.isPersonnel)
                    {
                        totalTrainees += p.amount;
                        totalMonths += p.monthsRemaining;
                        count++;
                    }
                }
            }
            
            if (lblTotal != null) lblTotal.text = totalTrainees.ToString("N0");
            
            if (lblAvgTime != null)
            {
                if (count > 0)
                {
                    float avgMonths = (float)totalMonths / count;
                    int currentDay = StateCraft.Core.GameManager.Instance?.TurnSystem != null 
                        ? ((StateCraft.Core.TurnSystem)StateCraft.Core.GameManager.Instance.TurnSystem).currentDay 
                        : 1;
                    float avgDays = ((avgMonths - 1f) * 30f) + (30f - currentDay);
                    lblAvgTime.text = avgDays > 0 ? avgDays.ToString("F0") : "0";
                }
                else
                {
                    lblAvgTime.text = "0";
                }
            }
            
            if (lblGradRate != null)
            {
                float avgReadiness = (country.armyStats.readiness + country.navyStats.readiness + country.airForceStats.readiness + country.rdfStats.readiness) / 4f;
                lblGradRate.text = $"{UnityEngine.Mathf.Clamp(avgReadiness + 5f, 50f, 98f):F0}%";
            }
            
            if (lblRetention != null)
            {
                float avgMorale = (country.armyStats.morale + country.navyStats.morale + country.airForceStats.morale + country.rdfStats.morale) / 4f;
                lblRetention.text = $"{UnityEngine.Mathf.Clamp(avgMorale + 10f, 60f, 95f):F0}%";
            }
        }

        private void RecruitUnit(string id, float cost, int amount, bool isPersonnel, bool isProduce, float mineralsCost)
        {
            var country = GameManager.Instance?.playerCountry;
            var gm = GameManager.Instance;
            if (country == null || gm == null) return;
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;

            if (isPersonnel)
            {
                // Enforce base capacity limit!
                int capacity = 0;
                int currentTotal = 0;
                int inTraining = GetTotalInTraining(country, id);
                string baseTypeNeeded = "";

                switch (id)
                {
                    case "INF":
                        capacity = 5000 + (country.baseArmyCount * 15000);
                        currentTotal = country.armyStats.personnel + inTraining;
                        baseTypeNeeded = "Military Base (Army)";
                        break;
                    case "POLICE":
                        capacity = 5000 + (country.baseArmyCount * 5000);
                        currentTotal = country.nationalPoliceSize + inTraining;
                        baseTypeNeeded = "Military Base (Army)";
                        break;
                    case "NAVY_PERS":
                        capacity = 1000 + (country.baseNavyCount * 3000);
                        currentTotal = country.navyStats.personnel + inTraining;
                        baseTypeNeeded = "Naval Base (Navy)";
                        break;
                    case "AIR_PERS":
                        capacity = 500 + (country.baseAirForceCount * 2000);
                        currentTotal = country.airForceStats.personnel + inTraining;
                        baseTypeNeeded = "Strategic Airbase";
                        break;
                    case "RDF_PERS":
                        capacity = 200 + (country.baseRDFCount * 1000);
                        currentTotal = country.rdfStats.personnel + inTraining;
                        baseTypeNeeded = "RDF Command Center";
                        break;
                }

                if (currentTotal + amount > capacity)
                {
                    NotificationManager.Instance?.AddNotification($"CAPACITY FULL: Build more {baseTypeNeeded}s to house these recruits! (Cap: {capacity:N0})", Color.red);
                    return;
                }
            }

            if (isProduce && country.invMineralsTonnes < mineralsCost)
            {
                NotificationManager.Instance?.AddNotification($"LACK OF RAW MATERIALS: Need {mineralsCost:N0} Tonnes of Minerals to produce locally!", Color.red);
                return;
            }

            if (gm.playerBudget >= cost)
            {
                if (isProduce)
                {
                    country.invMineralsTonnes -= mineralsCost;
                }
                
                gm.playerBudget -= cost;
                country.treasury = gm.playerBudget;
                
                string tag = isPersonnel ? "TRAINING" : (isProduce ? "MANUFACTURE" : "IMPORT");
                string unitLabel = "";

                // Determine label
                switch (id)
                {
                    case "INF": unitLabel = "Ground Troops"; break;
                    case "POLICE": unitLabel = "Police Officers"; break;
                    case "NAVY_PERS": unitLabel = "Naval Crews"; break;
                    case "AIR_PERS": unitLabel = "Air Crews"; break;
                    case "RDF_PERS": unitLabel = "Spec-Ops Forces"; break;
                    
                    case "RIFLE": unitLabel = "Service Rifles"; break;
                    case "ARTILLERY": unitLabel = "Artillery Batteries"; break;
                    case "TNK": unitLabel = "Main Battle Tanks"; break;
                    case "APC": unitLabel = "APCs"; break;
                    case "UAV": unitLabel = "Tactical UAVs"; break;
                    case "FTR": unitLabel = "Fighter Jets"; break;
                    case "BOMBER": unitLabel = "Strategic Bombers"; break;
                    case "HEL": unitLabel = "Attack Helis"; break;
                    case "PATROL_BOAT": unitLabel = "Patrol Boats"; break;
                    case "FRG": unitLabel = "Coastal Frigates"; break;
                    case "SUB": unitLabel = "Attack Submarines"; break;
                    case "DESTROYER": unitLabel = "Destroyers"; break;
                    case "BASE_ARMY": unitLabel = "Military Bases"; break;
                    case "BASE_NAVY": unitLabel = "Naval Bases"; break;
                    case "BASE_AIR": unitLabel = "Strategic Airbases"; break;
                    case "BASE_RDF": unitLabel = "RDF Command Centers"; break;
                    case "BASE_DEF_FAC": unitLabel = "Defense Factories"; break;
                }

                if (isPersonnel)
                {
                    // Queue for 6 months of training!
                    if (country.trainingPipelines == null)
                    {
                        UnityEngine.Debug.LogError($"[RecruitUnit] trainingPipelines is NULL! Aborting recruitment.");
                        return;
                    }
                    country.trainingPipelines.Add(new RecruitmentPipeline(id, unitLabel, amount, true));
                    UnityEngine.Debug.Log($"[RecruitUnit] Added {amount} {unitLabel} to trainingPipelines. Total pipeline count: {country.trainingPipelines.Count}");
                    
                    country.militaryReadiness = Mathf.Min(100f, country.militaryReadiness + 0.5f);
                    
                    NotificationManager.Instance?.AddNotification($"TRAINING STARTED: {amount:N0} {unitLabel} entered the 6-month training academy ({sym}{cost:N2}B allocated).", Color.yellow);
                    NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} entered {amount:N0} {unitLabel} recruits into the 6-month instruction program.");
                }
                else
                {
                    // Direct Hardware procurement / Base Construction
                    switch (id)
                    {
                        case "RIFLE": 
                            country.unitRifles += amount; 
                            country.armyStats.equipment += amount;
                            country.armyStrength = Mathf.Min(100f, country.armyStrength + 0.3f);
                            break;
                        case "ARTILLERY": 
                            country.unitArtillery += amount; 
                            country.armyStats.equipment += amount;
                            country.armyStrength = Mathf.Min(100f, country.armyStrength + 1.0f);
                            break;
                        case "TNK": 
                            country.unitTanks += amount; 
                            country.armyStats.equipment += amount;
                            country.armyStrength = Mathf.Min(100f, country.armyStrength + 1.2f);
                            break;
                        case "APC": 
                            country.unitAPCs += amount; 
                            country.armyStats.equipment += amount;
                            country.armyStrength = Mathf.Min(100f, country.armyStrength + 0.8f);
                            break;
                        case "UAV": 
                            country.unitUAVs += amount; 
                            country.airForceStats.equipment += amount;
                            country.airForceStrength = Mathf.Min(100f, country.airForceStrength + 0.6f);
                            break;
                        case "FTR": 
                            country.unitFighters += amount; 
                            country.airForceStats.equipment += amount;
                            country.airForceStrength = Mathf.Min(100f, country.airForceStrength + 2.0f);
                            break;
                        case "BOMBER": 
                            country.unitBombers += amount; 
                            country.airForceStats.equipment += amount;
                            country.airForceStrength = Mathf.Min(100f, country.airForceStrength + 3.0f);
                            break;
                        case "HEL": 
                            country.unitAttackHelis += amount; 
                            country.airForceStats.equipment += amount;
                            country.airForceStrength = Mathf.Min(100f, country.airForceStrength + 1.0f);
                            break;
                        case "PATROL_BOAT": 
                            country.unitPatrolBoats += amount; 
                            country.navyStats.equipment += amount;
                            country.navyStrength = Mathf.Min(100f, country.navyStrength + 0.5f);
                            break;
                        case "FRG": 
                            country.unitFrigates += amount; 
                            country.navyStats.equipment += amount;
                            country.navyStrength = Mathf.Min(100f, country.navyStrength + 3.0f);
                            break;
                        case "SUB": 
                            country.unitSubmarines += amount; 
                            country.navyStats.equipment += amount;
                            country.navyStrength = Mathf.Min(100f, country.navyStrength + 4.5f);
                            break;
                        case "DESTROYER": 
                            country.unitDestroyers += amount; 
                            country.navyStats.equipment += amount;
                            country.navyStrength = Mathf.Min(100f, country.navyStrength + 5.5f);
                            break;
                        case "BASE_ARMY":
                            country.baseArmyCount += amount;
                            country.armyStats.readiness = Mathf.Min(100f, country.armyStats.readiness + 5.0f);
                            country.armyStrength = Mathf.Min(100f, country.armyStrength + 1.5f);
                            break;
                        case "BASE_NAVY":
                            country.baseNavyCount += amount;
                            country.navyStats.readiness = Mathf.Min(100f, country.navyStats.readiness + 8.0f);
                            country.navyStrength = Mathf.Min(100f, country.navyStrength + 2.5f);
                            break;
                        case "BASE_AIR":
                            country.baseAirForceCount += amount;
                            country.airForceStats.readiness = Mathf.Min(100f, country.airForceStats.readiness + 6.0f);
                            country.airForceStrength = Mathf.Min(100f, country.airForceStrength + 2.0f);
                            break;
                        case "BASE_RDF":
                            country.baseRDFCount += amount;
                            country.rdfStats.readiness = Mathf.Min(100f, country.rdfStats.readiness + 10.0f);
                            country.rdfReadiness = Mathf.Min(100f, country.rdfReadiness + 10.0f);
                            break;
                        case "BASE_DEF_FAC":
                            country.baseDefenseFactoryCount += amount;
                            break;
                    }

                    // Update combined strength
                    country.militaryStrength = (country.armyStrength * 0.5f) + (country.navyStrength * 0.2f) + (country.airForceStrength * 0.3f);
                    country.militaryReadiness = Mathf.Min(100f, country.militaryReadiness + 1.0f);

                    NotificationManager.Instance?.AddNotification($"{tag}: {amount:N0} {unitLabel} added (Cost: {UiFormat.Money(cost, sym)}).", Color.cyan);
                    NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} completed {tag.ToLower()} of {amount:N0} {unitLabel}.");
                }

                RefreshMilitaryData();
            }
            else
            {
                NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need {UiFormat.Money(cost, sym)} to execute this transaction.", Color.red);
            }
        }

        private void DecommissionUnit(string id, int amountToRemove, bool isPersonnel)
        {
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;
            
            int amountRemoved = 0;
            string unitLabel = "";

            switch (id)
            {
                case "INF": 
                    amountRemoved = Mathf.Min(country.armyStats.personnel, amountToRemove);
                    country.armyStats.personnel -= amountRemoved;
                    country.unitInfantry -= amountRemoved;
                    unitLabel = "Ground Troops";
                    break;
                case "POLICE": 
                    amountRemoved = Mathf.Min(country.nationalPoliceSize, amountToRemove);
                    country.nationalPoliceSize -= amountRemoved;
                    unitLabel = "Police Officers";
                    break;
                case "NAVY_PERS": 
                    amountRemoved = Mathf.Min(country.navyStats.personnel, amountToRemove);
                    country.navyStats.personnel -= amountRemoved;
                    unitLabel = "Naval Crews";
                    break;
                case "AIR_PERS": 
                    amountRemoved = Mathf.Min(country.airForceStats.personnel, amountToRemove);
                    country.airForceStats.personnel -= amountRemoved;
                    unitLabel = "Air Crews";
                    break;
                case "RDF_PERS": 
                    amountRemoved = Mathf.Min(country.rdfStats.personnel, amountToRemove);
                    country.rdfStats.personnel -= amountRemoved;
                    unitLabel = "Spec-Ops Forces";
                    break;
                
                case "RIFLE": 
                    amountRemoved = Mathf.Min(country.unitRifles, amountToRemove);
                    country.unitRifles -= amountRemoved; 
                    country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amountRemoved);
                    unitLabel = "Service Rifles"; 
                    break;
                case "ARTILLERY": 
                    amountRemoved = Mathf.Min(country.unitArtillery, amountToRemove);
                    country.unitArtillery -= amountRemoved; 
                    country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amountRemoved);
                    unitLabel = "Artillery Batteries"; 
                    break;
                case "TNK": 
                    amountRemoved = Mathf.Min(country.unitTanks, amountToRemove);
                    country.unitTanks -= amountRemoved; 
                    country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amountRemoved);
                    unitLabel = "Main Battle Tanks"; 
                    break;
                case "APC": 
                    amountRemoved = Mathf.Min(country.unitAPCs, amountToRemove);
                    country.unitAPCs -= amountRemoved; 
                    country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amountRemoved);
                    unitLabel = "APCs"; 
                    break;
                case "UAV": 
                    amountRemoved = Mathf.Min(country.unitUAVs, amountToRemove);
                    country.unitUAVs -= amountRemoved; 
                    country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amountRemoved);
                    unitLabel = "Tactical UAVs"; 
                    break;
                case "FTR": 
                    amountRemoved = Mathf.Min(country.unitFighters, amountToRemove);
                    country.unitFighters -= amountRemoved; 
                    country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amountRemoved);
                    unitLabel = "Fighter Jets"; 
                    break;
                case "BOMBER": 
                    amountRemoved = Mathf.Min(country.unitBombers, amountToRemove);
                    country.unitBombers -= amountRemoved; 
                    country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amountRemoved);
                    unitLabel = "Strategic Bombers"; 
                    break;
                case "HEL": 
                    amountRemoved = Mathf.Min(country.unitAttackHelis, amountToRemove);
                    country.unitAttackHelis -= amountRemoved; 
                    country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amountRemoved);
                    unitLabel = "Attack Helis"; 
                    break;
                case "PATROL_BOAT": 
                    amountRemoved = Mathf.Min(country.unitPatrolBoats, amountToRemove);
                    country.unitPatrolBoats -= amountRemoved; 
                    country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amountRemoved);
                    unitLabel = "Patrol Boats"; 
                    break;
                case "FRG": 
                    amountRemoved = Mathf.Min(country.unitFrigates, amountToRemove);
                    country.unitFrigates -= amountRemoved; 
                    country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amountRemoved);
                    unitLabel = "Coastal Frigates"; 
                    break;
                case "SUB": 
                    amountRemoved = Mathf.Min(country.unitSubmarines, amountToRemove);
                    country.unitSubmarines -= amountRemoved; 
                    country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amountRemoved);
                    unitLabel = "Attack Submarines"; 
                    break;
                case "DESTROYER": 
                    amountRemoved = Mathf.Min(country.unitDestroyers, amountToRemove);
                    country.unitDestroyers -= amountRemoved; 
                    country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amountRemoved);
                    unitLabel = "Destroyers"; 
                    break;
                case "BASE_ARMY":
                    amountRemoved = Mathf.Min(country.baseArmyCount, amountToRemove);
                    country.baseArmyCount -= amountRemoved;
                    unitLabel = "Military Bases";
                    break;
                case "BASE_NAVY":
                    amountRemoved = Mathf.Min(country.baseNavyCount, amountToRemove);
                    country.baseNavyCount -= amountRemoved;
                    unitLabel = "Naval Bases";
                    break;
                case "BASE_AIR":
                    amountRemoved = Mathf.Min(country.baseAirForceCount, amountToRemove);
                    country.baseAirForceCount -= amountRemoved;
                    unitLabel = "Strategic Airbases";
                    break;
                case "BASE_RDF":
                    amountRemoved = Mathf.Min(country.baseRDFCount, amountToRemove);
                    country.baseRDFCount -= amountRemoved;
                    unitLabel = "RDF Command Centers";
                    break;
                case "BASE_DEF_FAC":
                    amountRemoved = Mathf.Min(country.baseDefenseFactoryCount, amountToRemove);
                    country.baseDefenseFactoryCount -= amountRemoved;
                    unitLabel = "Defense Factories";
                    break;
            }

            if (amountRemoved > 0)
            {
                // Simple recalculation based on actual hardware
                country.militaryStrength = (country.armyStrength * 0.5f) + (country.navyStrength * 0.2f) + (country.airForceStrength * 0.3f);
                
                NotificationManager.Instance?.AddNotification($"DECOMMISSIONED: {amountRemoved:N0} {unitLabel} removed from active service.", new Color(255/255f, 82/255f, 82/255f));
                NewsSystem.Instance?.PublishNews($"MILITARY: {country.countryName} decommissioned {amountRemoved:N0} {unitLabel} to reduce military expenditures.");
                RefreshMilitaryData();
            }
            else
            {
                NotificationManager.Instance?.AddNotification($"NO UNITS: Cannot decommission {unitLabel} - none in active service.", Color.red);
            }
        }

        private void DecommissionUnit(string id, float cost, int amount, bool isPersonnel)
        {
            var country = GameManager.Instance?.playerCountry;
            var gm = GameManager.Instance;
            if (country == null || gm == null) return;

            float refund = cost * 0.40f; // Reclaim 40% of the training/construction budget!

            if (isPersonnel)
            {
                int standing = 0;
                switch (id)
                {
                    case "INF":
                        standing = country.armyStats.personnel;
                        if (standing < amount) { NotificationManager.Instance?.AddNotification("Disband size exceeds active infantry!", Color.red); return; }
                        country.armyStats.personnel -= amount;
                        country.unitInfantry = Mathf.Max(0, country.unitInfantry - amount);
                        break;
                    case "POLICE":
                        standing = country.nationalPoliceSize;
                        if (standing < amount) { NotificationManager.Instance?.AddNotification("Disband size exceeds active police forces!", Color.red); return; }
                        country.nationalPoliceSize -= amount;
                        break;
                    case "NAVY_PERS":
                        standing = country.navyStats.personnel;
                        if (standing < amount) { NotificationManager.Instance?.AddNotification("Disband size exceeds active naval personnel!", Color.red); return; }
                        country.navyStats.personnel -= amount;
                        break;
                    case "AIR_PERS":
                        standing = country.airForceStats.personnel;
                        if (standing < amount) { NotificationManager.Instance?.AddNotification("Disband size exceeds active air crew!", Color.red); return; }
                        country.airForceStats.personnel -= amount;
                        break;
                    case "RDF_PERS":
                        standing = country.rdfStats.personnel;
                        if (standing < amount) { NotificationManager.Instance?.AddNotification("Disband size exceeds active spec-ops soldiers!", Color.red); return; }
                        country.rdfStats.personnel -= amount;
                        break;
                }

                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
                gm.playerBudget += refund;
                country.treasury = gm.playerBudget;
                NotificationManager.Instance?.AddNotification($"DISBANDED: Dismissed {amount:N0} forces. Reclaimed {sym}{refund:N2}B for Treasury.", Color.cyan);
            }
            else
            {
                switch (id)
                {
                    case "RIFLE":
                        if (country.unitRifles < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds rifles in stock!", Color.red); return; }
                        country.unitRifles -= amount;
                        country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amount);
                        break;
                    case "ARTILLERY":
                        if (country.unitArtillery < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds artillery in stock!", Color.red); return; }
                        country.unitArtillery -= amount;
                        country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amount);
                        break;
                    case "TNK":
                        if (country.unitTanks < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds tanks in stock!", Color.red); return; }
                        country.unitTanks -= amount;
                        country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amount);
                        break;
                    case "APC":
                        if (country.unitAPCs < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds APCs in stock!", Color.red); return; }
                        country.unitAPCs -= amount;
                        country.armyStats.equipment = Mathf.Max(0, country.armyStats.equipment - amount);
                        break;
                    case "UAV":
                        if (country.unitUAVs < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds UAVs in stock!", Color.red); return; }
                        country.unitUAVs -= amount;
                        country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amount);
                        break;
                    case "FTR":
                        if (country.unitFighters < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds fighter jets in stock!", Color.red); return; }
                        country.unitFighters -= amount;
                        country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amount);
                        break;
                    case "BOMBER":
                        if (country.unitBombers < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds bombers in stock!", Color.red); return; }
                        country.unitBombers -= amount;
                        country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amount);
                        break;
                    case "HEL":
                        if (country.unitAttackHelis < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds attack helicopters in stock!", Color.red); return; }
                        country.unitAttackHelis -= amount;
                        country.airForceStats.equipment = Mathf.Max(0, country.airForceStats.equipment - amount);
                        break;
                    case "PATROL_BOAT":
                        if (country.unitPatrolBoats < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds patrol boats in stock!", Color.red); return; }
                        country.unitPatrolBoats -= amount;
                        country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amount);
                        break;
                    case "FRG":
                        if (country.unitFrigates < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds frigates in stock!", Color.red); return; }
                        country.unitFrigates -= amount;
                        country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amount);
                        break;
                    case "SUB":
                        if (country.unitSubmarines < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds submarines in stock!", Color.red); return; }
                        country.unitSubmarines -= amount;
                        country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amount);
                        break;
                    case "DESTROYER":
                        if (country.unitDestroyers < amount) { NotificationManager.Instance?.AddNotification("Scrap amount exceeds destroyers in stock!", Color.red); return; }
                        country.unitDestroyers -= amount;
                        country.navyStats.equipment = Mathf.Max(0, country.navyStats.equipment - amount);
                        break;
                }

                string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
                gm.playerBudget += refund;
                country.treasury = gm.playerBudget;
                NotificationManager.Instance?.AddNotification($"DECOMMISSIONED: Scrapped equipment. Reclaimed {sym}{refund:N2}B for Treasury.", Color.cyan);
            }

            RefreshMilitaryData();
        }

        private int GetTotalInTraining(CountryDataSO country, string id)
        {
            if (country.trainingPipelines == null) return 0;
            int total = 0;
            foreach (var pipe in country.trainingPipelines)
            {
                if (pipe.id == id) total += pipe.amount;
            }
            return total;
        }


    }
}
