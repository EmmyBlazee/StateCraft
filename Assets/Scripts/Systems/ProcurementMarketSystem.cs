using UnityEngine;
using System.Collections.Generic;
using StateCraft.Data;
using StateCraft.Core;

namespace StateCraft.Systems
{
    /// <summary>
    /// Generates dynamic procurement offers for the military market.
    /// Hooks into GameManager to refresh every 3 months.
    /// </summary>
    public static class ProcurementMarketSystem
    {
        private static List<ProcurementOffer> _masterTemplates = new List<ProcurementOffer>();
        private static bool _initialized = false;

        private static void InitTemplates()
        {
            if (_initialized) return;

            // Army Equipment
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_RIFLE_1", title = "10,000 Assault Rifles", subtitle = "Infantry Weapons", tags = new string[]{"5.56mm", "Standard Issue"}, supplier = "FN Herstal", country = "Belgium", quantity = "10k Units", deliveryMonths = 2, flagEmoji = "🇧🇪", produceType = "RIFLE", unitQuantity = 10000, totalCostB = 50.0f });
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_RIFLE_2", title = "20,000 AK-12 Rifles", subtitle = "Infantry Weapons", tags = new string[]{"5.45mm", "Modernized"}, supplier = "Kalashnikov", country = "Russia", quantity = "20k Units", deliveryMonths = 3, flagEmoji = "🇷🇺", produceType = "RIFLE", unitQuantity = 20000, totalCostB = 90.0f });
            
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_APC_1", title = "100 APCs (Rooikat Mk1D)", subtitle = "Armored Personnel Carrier", tags = new string[]{"8x8", "Mine Resistant"}, supplier = "Denel Land Systems", country = "South Africa", quantity = "100 Units", deliveryMonths = 4, flagEmoji = "🇿🇦", produceType = "APC", unitQuantity = 100, totalCostB = 42.0f });
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_APC_2", title = "150 Stryker IFVs", subtitle = "Infantry Fighting Vehicle", tags = new string[]{"Mobile", "Networked"}, supplier = "General Dynamics", country = "USA", quantity = "150 Units", deliveryMonths = 5, flagEmoji = "🇺🇸", produceType = "APC", unitQuantity = 150, totalCostB = 75.0f });
            
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_TNK_1", title = "50 Main Battle Tanks (T-90)", subtitle = "Main Battle Tank", tags = new string[]{"125mm Gun", "ERA Armor"}, supplier = "Uralvagonzavod", country = "Russia", quantity = "50 Units", deliveryMonths = 6, flagEmoji = "🇷🇺", produceType = "TNK", unitQuantity = 50, totalCostB = 75.0f });
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_TNK_2", title = "40 Leopard 2A7", subtitle = "Main Battle Tank", tags = new string[]{"Heavy Armor", "120mm Gun"}, supplier = "KMW", country = "Germany", quantity = "40 Units", deliveryMonths = 8, flagEmoji = "🇩🇪", produceType = "TNK", unitQuantity = 40, totalCostB = 120.0f });
            
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_ART_1", title = "24 Mobile Artillery (M109)", subtitle = "Self-Propelled Howitzer", tags = new string[]{"155mm", "Tracked"}, supplier = "BAE Systems", country = "USA", quantity = "24 Units", deliveryMonths = 5, flagEmoji = "🇺🇸", produceType = "ARTILLERY", unitQuantity = 24, totalCostB = 48.0f });
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_ART_2", title = "30 Caesar Howitzers", subtitle = "Self-Propelled Howitzer", tags = new string[]{"155mm", "Wheeled"}, supplier = "Nexter", country = "France", quantity = "30 Units", deliveryMonths = 6, flagEmoji = "🇫🇷", produceType = "ARTILLERY", unitQuantity = 30, totalCostB = 55.0f });

            // Air Force Equipment
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_FTR_1", title = "12 Fighter Jets (JF-17)", subtitle = "Multi-Role Combat Aircraft", tags = new string[]{"4.5 Gen", "Avionics"}, supplier = "PAC", country = "Pakistan", quantity = "12 Units", deliveryMonths = 8, flagEmoji = "🇵🇰", produceType = "FTR", unitQuantity = 12, totalCostB = 180.0f });
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_FTR_2", title = "10 F-16 Block 70/72", subtitle = "Multi-Role Combat Aircraft", tags = new string[]{"4.5 Gen", "AESA Radar"}, supplier = "Lockheed Martin", country = "USA", quantity = "10 Units", deliveryMonths = 12, flagEmoji = "🇺🇸", produceType = "FTR", unitQuantity = 10, totalCostB = 250.0f });
            
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_BOMBER_1", title = "4 Strategic Bombers (H-6K)", subtitle = "Strategic Bomber", tags = new string[]{"Long Range", "Payload"}, supplier = "Xian Aircraft", country = "China", quantity = "4 Units", deliveryMonths = 12, flagEmoji = "🇨🇳", produceType = "BOMBER", unitQuantity = 4, totalCostB = 120.0f });
            
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_HEL_1", title = "15 Attack Helicopters (AH-64)", subtitle = "Attack Helicopter", tags = new string[]{"Anti-Armor", "Agile"}, supplier = "Boeing", country = "USA", quantity = "15 Units", deliveryMonths = 7, flagEmoji = "🇺🇸", produceType = "HEL", unitQuantity = 15, totalCostB = 180.0f });
            
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_UAV_1", title = "20 Combat Drones (TB2)", subtitle = "Unmanned Combat Aerial Vehicle", tags = new string[]{"Recon", "Precision"}, supplier = "Baykar", country = "Turkey", quantity = "20 Units", deliveryMonths = 3, flagEmoji = "🇹🇷", produceType = "UAV", unitQuantity = 20, totalCostB = 50.0f });

            // Naval Equipment
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_PATROL_1", title = "8 Offshore Patrol Boats", subtitle = "Offshore Patrol Vessel", tags = new string[]{"Coastal Ops", "Fast"}, supplier = "ASFAT", country = "Turkey", quantity = "8 Units", deliveryMonths = 6, flagEmoji = "🇹🇷", produceType = "PATROL_BOAT", unitQuantity = 8, totalCostB = 89.6f });
            
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_FRG_1", title = "2 Multi-Role Frigates", subtitle = "Frigate", tags = new string[]{"ASW", "AAW"}, supplier = "Naval Group", country = "France", quantity = "2 Units", deliveryMonths = 24, flagEmoji = "🇫🇷", produceType = "FRG", unitQuantity = 2, totalCostB = 500.0f });
            
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_SUB_1", title = "1 Attack Submarine", subtitle = "Diesel-Electric Submarine", tags = new string[]{"Stealth", "Torpedoes"}, supplier = "ThyssenKrupp", country = "Germany", quantity = "1 Unit", deliveryMonths = 36, flagEmoji = "🇩🇪", produceType = "SUB", unitQuantity = 1, totalCostB = 350.0f });
            
            _masterTemplates.Add(new ProcurementOffer { id = "OFF_DES_1", title = "1 Guided Missile Destroyer", subtitle = "Destroyer", tags = new string[]{"Aegis", "Heavy Strike"}, supplier = "BAE Systems", country = "UK", quantity = "1 Unit", deliveryMonths = 48, flagEmoji = "🇬🇧", produceType = "DESTROYER", unitQuantity = 1, totalCostB = 600.0f });

            _initialized = true;
        }

        public static void RefreshMarket(CountryDataSO country)
        {
            InitTemplates();

            if (country.availableOffers == null)
                country.availableOffers = new List<ProcurementOffer>();

            country.availableOffers.Clear();

            var gm = GameManager.Instance;
            
            // Randomly shuffle templates
            List<ProcurementOffer> shuffled = new List<ProcurementOffer>(_masterTemplates);
            for (int i = 0; i < shuffled.Count; i++)
            {
                ProcurementOffer temp = shuffled[i];
                int randomIndex = UnityEngine.Random.Range(i, shuffled.Count);
                shuffled[i] = shuffled[randomIndex];
                shuffled[randomIndex] = temp;
            }

            int count = 0;
            foreach(var template in shuffled)
            {
                // Diplomacy check: Don't offer contracts from Hostile/At War nations
                if (gm != null && gm.Relations != null)
                {
                    var tier = gm.Relations.GetTier(country.countryName, template.country);
                    if (tier == RelationTier.Hostile || tier == RelationTier.AtWar)
                        continue;
                }

                ProcurementOffer offer = new ProcurementOffer();
                offer.id = template.id + "_" + UnityEngine.Random.Range(100, 999);
                offer.title = template.title;
                offer.subtitle = template.subtitle;
                offer.tags = template.tags;
                offer.supplier = template.supplier;
                offer.country = template.country;
                offer.produceType = template.produceType;
                offer.unitQuantity = template.unitQuantity;
                offer.flagEmoji = template.flagEmoji;
                offer.quantity = template.quantity;

                // Randomize pricing +/- 15%
                float priceMod = UnityEngine.Random.Range(0.85f, 1.15f);
                offer.totalCostB = template.totalCostB * priceMod;
                float costPerUnitB = offer.totalCostB / offer.unitQuantity;
                offer.unitCost = costPerUnitB < 1.0f ? $"₦{costPerUnitB * 1000f:F1}M" : $"₦{costPerUnitB:F1}B";
                offer.totalCost = $"₦{offer.totalCostB:F1}B";

                // Randomize delivery time +/- 1 month
                offer.deliveryMonths = Mathf.Max(1, template.deliveryMonths + UnityEngine.Random.Range(-1, 2));
                offer.deliveryTime = $"{offer.deliveryMonths} Months";

                country.availableOffers.Add(offer);
                count++;
                
                // Keep the market at 10 items
                if (count >= 10) break;
            }
        }
    }
}
