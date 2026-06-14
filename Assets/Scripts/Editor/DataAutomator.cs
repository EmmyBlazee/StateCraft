using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using StateCraft.Data;

namespace StateCraft.Editor
{
    public class DataAutomator : EditorWindow
    {
        [MenuItem("StateCraft/1. Update All Capitals, GDP and Currencies")]
        public static void UpdateCountryData()
        {
            Dictionary<string, float> realGDP = new Dictionary<string, float>()
            {
                {"South Africa", 410.5f}, {"Egypt", 347.6f}, {"Nigeria", 252.8f},
                {"Algeria", 269.1f}, {"Morocco", 166.4f}, {"Ethiopia", 109.2f},
                {"Kenya", 131.5f}, {"Angola", 115.3f}, {"Ghana", 88.4f},
                {"Cote d'Ivoire", 94.2f}, {"Tanzania", 85.0f}, {"DR Congo", 75.0f},
                {"Uganda", 55.0f}, {"Tunisia", 52.0f}, {"Cameroon", 50.0f},
                {"Libya", 45.0f}, {"Senegal", 35.0f}, {"Zambia", 32.0f},
                {"Sudan", 28.0f}, {"Mozambique", 25.0f}
            };

            Dictionary<string, string> capitals = new Dictionary<string, string>()
            {
                {"Nigeria", "Abuja"}, {"South Africa", "Pretoria"}, {"Egypt", "Cairo"},
                {"Algeria", "Algiers"}, {"Morocco", "Rabat"}, {"Ethiopia", "Addis Ababa"},
                {"Kenya", "Nairobi"}, {"Angola", "Luanda"}, {"Ghana", "Accra"},
                {"Cote d'Ivoire", "Yamoussoukro"}, {"Tanzania", "Dodoma"}, {"DR Congo", "Kinshasa"},
                {"Uganda", "Kampala"}, {"Tunisia", "Tunis"}, {"Cameroon", "Yaoundé"},
                {"Libya", "Tripoli"}, {"Senegal", "Dakar"}, {"Zambia", "Lusaka"},
                {"Sudan", "Khartoum"}, {"Mozambique", "Maputo"}, {"Somalia", "Mogadishu"},
                {"Rwanda", "Kigali"}, {"Burundi", "Gitega"}, {"Malawi", "Lilongwe"},
                {"Madagascar", "Antananarivo"}, {"Mauritius", "Port Louis"}, {"Seychelles", "Victoria"},
                {"Djibouti", "Djibouti"}, {"Eritrea", "Asmara"}, {"South Sudan", "Juba"},
                {"Central African Republic", "Bangui"}, {"Chad", "N'Djamena"}, {"Niger", "Niamey"},
                {"Mali", "Bamako"}, {"Mauritania", "Nouakchott"}, {"Western Sahara", "Laayoune"},
                {"Cape Verde", "Praia"}, {"Gambia", "Banjul"}, {"Guinea", "Conakry"},
                {"Guinea-Bissau", "Bissau"}, {"Sierra Leone", "Freetown"}, {"Liberia", "Monrovia"},
                {"Burkina Faso", "Ouagadougou"}, {"Togo", "Lomé"}, {"Benin", "Porto-Novo"},
                {"Equatorial Guinea", "Malabo"}, {"Gabon", "Libreville"}, {"Republic of the Congo", "Brazzaville"},
                {"Namibia", "Windhoek"}, {"Botswana", "Gaborone"}, {"Zimbabwe", "Harare"},
                {"Lesotho", "Maseru"}, {"Eswatini", "Mbabane"}, {"Comoros", "Moroni"},
                {"Sao Tome and Principe", "São Tomé"}
            };

            Dictionary<string, (string name, string symbol)> currencies = new Dictionary<string, (string name, string symbol)>()
            {
                {"Nigeria", ("Naira", "₦")}, {"South Africa", ("Rand", "R")}, {"Egypt", ("Pound", "E£")},
                {"Algeria", ("Dinar", "DA")}, {"Morocco", ("Dirham", "DH")}, {"Ethiopia", ("Birr", "Br")},
                {"Kenya", ("Shilling", "KSh")}, {"Angola", ("Kwanza", "Kz")}, {"Ghana", ("Cedi", "GH₵")},
                {"Tanzania", ("Shilling", "TSh")}, {"DR Congo", ("Franc", "FC")}, {"Uganda", ("Shilling", "USh")},
                {"Sudan", ("Pound", "SDG")}, {"Libya", ("Dinar", "LD")}, {"Tunisia", ("Dinar", "DT")},
                {"Zambia", ("Kwacha", "ZK")}, {"Mozambique", ("Metical", "MT")}, {"Botswana", ("Pula", "P")},
                {"Namibia", ("Dollar", "N$")}, {"Rwanda", ("Franc", "RF")}, {"Madagascar", ("Ariary", "Ar")},
                {"Mauritius", ("Rupee", "Rs")}, {"Senegal", ("CFA Franc", "CFA")}, {"Mali", ("CFA Franc", "CFA")},
                {"Ivory Coast", ("CFA Franc", "CFA")}, {"Cameroon", ("CFA Franc", "CFA")}, {"Gabon", ("CFA Franc", "CFA")},
                {"Benin", ("CFA Franc", "CFA")}, {"Togo", ("CFA Franc", "CFA")}, {"Burkina Faso", ("CFA Franc", "CFA")},
                {"Niger", ("CFA Franc", "CFA")}, {"Chad", ("CFA Franc", "CFA")}, {"Central African Republic", ("CFA Franc", "CFA")},
                {"Republic of the Congo", ("CFA Franc", "CFA")}, {"Equatorial Guinea", ("CFA Franc", "CFA")}, {"Guinea-Bissau", ("CFA Franc", "CFA")},
                {"Malawi", ("Kwacha", "MK")}, {"Zimbabwe", ("ZiG", "ZiG")}, {"Lesotho", ("Loti", "L")},
                {"Eswatini", ("Lilangeni", "E")}, {"Eritrea", ("Nakfa", "Nfk")}, {"Djibouti", ("Franc", "Fdj")},
                {"South Sudan", ("Pound", "SSP")}, {"Burundi", ("Franc", "FBu")}, {"Comoros", ("Franc", "KMF")},
                {"Cape Verde", ("Escudo", "Esc")}, {"Gambia", ("Dalasi", "D")}, {"Guinea", ("Franc", "FG")},
                {"Liberia", ("Dollar", "L$")}, {"Mauritania", ("Ouguiya", "UM")}, {"Sierra Leone", ("Leone", "Le")},
                {"Seychelles", ("Rupee", "SR")}, {"Sao Tome and Principe", ("Dobra", "Db")}
            };

            Dictionary<string, float> startExchangeRates = new Dictionary<string, float>()
            {
                {"Nigeria", 1550f}, {"South Africa", 18.5f}, {"Egypt", 48f},
                {"Algeria", 134f}, {"Morocco", 10f}, {"Ethiopia", 57f},
                {"Kenya", 130f}, {"Angola", 830f}, {"Ghana", 15f},
                {"Tanzania", 2600f}, {"DR Congo", 2800f}, {"Uganda", 3700f},
                {"Libya", 4.8f}, {"Tunisia", 3.1f}, {"Zambia", 25f},
                {"Cote d'Ivoire", 600f}, {"Senegal", 600f}, {"Cameroon", 600f},
                {"Benin", 600f}, {"Mali", 600f}, {"Burkina Faso", 600f},
                {"Niger", 600f}, {"Chad", 600f}, {"Togo", 600f},
                {"Gabon", 600f}, {"Central African Republic", 600f},
                {"Sudan", 600f}, {"South Sudan", 130f}, {"Rwanda", 1300f},
                {"Burundi", 2800f}, {"Malawi", 1750f}, {"Mozambique", 63f},
                {"Madagascar", 4500f}, {"Mauritius", 46f}, {"Seychelles", 13.5f},
                {"Gambia", 68f}, {"Sierra Leone", 22f}, {"Liberia", 190f},
                {"Guinea", 8600f}, {"Cape Verde", 100f}
            };

            Dictionary<string, List<ResourceDeposit>> resourceMap = new Dictionary<string, List<ResourceDeposit>>()
            {
                {"Nigeria", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Oil, abundance=85}, new ResourceDeposit{type=MineralType.NaturalGas, abundance=70}, new ResourceDeposit{type=MineralType.Gold, abundance=45}, new ResourceDeposit{type=MineralType.IronOre, abundance=30}, new ResourceDeposit{type=MineralType.Coal, abundance=40} }},
                {"South Africa", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Gold, abundance=90}, new ResourceDeposit{type=MineralType.Platinum, abundance=95}, new ResourceDeposit{type=MineralType.IronOre, abundance=60}, new ResourceDeposit{type=MineralType.Coal, abundance=85}, new ResourceDeposit{type=MineralType.Diamonds, abundance=50} }},
                {"DR Congo", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Cobalt, abundance=98}, new ResourceDeposit{type=MineralType.Copper, abundance=92}, new ResourceDeposit{type=MineralType.Diamonds, abundance=75}, new ResourceDeposit{type=MineralType.Gold, abundance=45}, new ResourceDeposit{type=MineralType.Lithium, abundance=60} }},
                {"Angola", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Oil, abundance=80}, new ResourceDeposit{type=MineralType.Diamonds, abundance=65}, new ResourceDeposit{type=MineralType.IronOre, abundance=30} }},
                {"Ghana", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Gold, abundance=88}, new ResourceDeposit{type=MineralType.Oil, abundance=45}, new ResourceDeposit{type=MineralType.Diamonds, abundance=20} }},
                {"Zambia", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Copper, abundance=95}, new ResourceDeposit{type=MineralType.Cobalt, abundance=50} }},
                {"Botswana", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Diamonds, abundance=98}, new ResourceDeposit{type=MineralType.Coal, abundance=40} }},
                {"Namibia", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Uranium, abundance=85}, new ResourceDeposit{type=MineralType.Diamonds, abundance=80}, new ResourceDeposit{type=MineralType.Lithium, abundance=30} }},
                {"Niger", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Uranium, abundance=90}, new ResourceDeposit{type=MineralType.Oil, abundance=30} }},
                {"Zimbabwe", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Gold, abundance=70}, new ResourceDeposit{type=MineralType.Lithium, abundance=80}, new ResourceDeposit{type=MineralType.Platinum, abundance=60}, new ResourceDeposit{type=MineralType.Coal, abundance=50} }},
                {"Libya", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Oil, abundance=95}, new ResourceDeposit{type=MineralType.NaturalGas, abundance=60} }},
                {"Algeria", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.NaturalGas, abundance=95}, new ResourceDeposit{type=MineralType.Oil, abundance=75}, new ResourceDeposit{type=MineralType.IronOre, abundance=50} }},
                {"Egypt", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.NaturalGas, abundance=90}, new ResourceDeposit{type=MineralType.Oil, abundance=55}, new ResourceDeposit{type=MineralType.Gold, abundance=40} }},
                {"Morocco", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.NaturalGas, abundance=30}, new ResourceDeposit{type=MineralType.IronOre, abundance=40}, new ResourceDeposit{type=MineralType.Gold, abundance=20} }},
                {"Sudan", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Gold, abundance=85}, new ResourceDeposit{type=MineralType.Oil, abundance=50} }},
                {"South Sudan", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Oil, abundance=85} }},
                {"Ethiopia", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Gold, abundance=50}, new ResourceDeposit{type=MineralType.NaturalGas, abundance=40} }},
                {"Kenya", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Oil, abundance=30}, new ResourceDeposit{type=MineralType.Gold, abundance=20}, new ResourceDeposit{type=MineralType.IronOre, abundance=20} }},
                {"Uganda", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Gold, abundance=60}, new ResourceDeposit{type=MineralType.Oil, abundance=50}, new ResourceDeposit{type=MineralType.Copper, abundance=30} }},
                {"Tanzania", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Gold, abundance=75}, new ResourceDeposit{type=MineralType.Diamonds, abundance=40}, new ResourceDeposit{type=MineralType.NaturalGas, abundance=50} }},
                {"Mozambique", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.NaturalGas, abundance=90}, new ResourceDeposit{type=MineralType.Coal, abundance=75} }},
                {"Madagascar", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Diamonds, abundance=40}, new ResourceDeposit{type=MineralType.Gold, abundance=30}, new ResourceDeposit{type=MineralType.Lithium, abundance=50} }},
                {"Gabon", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Oil, abundance=80}, new ResourceDeposit{type=MineralType.Gold, abundance=30} }},
                {"Equatorial Guinea", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Oil, abundance=90}, new ResourceDeposit{type=MineralType.NaturalGas, abundance=80} }},
                {"Congo Republic", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Oil, abundance=85} }},
                {"Cameroon", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Oil, abundance=40}, new ResourceDeposit{type=MineralType.NaturalGas, abundance=30}, new ResourceDeposit{type=MineralType.Gold, abundance=20} }},
                {"Ivory Coast", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Oil, abundance=30}, new ResourceDeposit{type=MineralType.Gold, abundance=50} }},
                {"Guinea", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.IronOre, abundance=95}, new ResourceDeposit{type=MineralType.Gold, abundance=70}, new ResourceDeposit{type=MineralType.Diamonds, abundance=30} }},
                {"Sierra Leone", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Diamonds, abundance=90}, new ResourceDeposit{type=MineralType.Gold, abundance=40} }},
                {"Liberia", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.IronOre, abundance=85}, new ResourceDeposit{type=MineralType.Diamonds, abundance=30} }},
                {"Mali", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Gold, abundance=85}, new ResourceDeposit{type=MineralType.Lithium, abundance=50} }},
                {"Mauritania", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.IronOre, abundance=90}, new ResourceDeposit{type=MineralType.Gold, abundance=40} }},
                {"Burkina Faso", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Gold, abundance=80} }},
                {"Senegal", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Gold, abundance=40}, new ResourceDeposit{type=MineralType.Oil, abundance=50}, new ResourceDeposit{type=MineralType.NaturalGas, abundance=40} }},
                {"Chad", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Oil, abundance=80}, new ResourceDeposit{type=MineralType.Gold, abundance=20} }},
                {"Central African Republic", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Diamonds, abundance=70}, new ResourceDeposit{type=MineralType.Gold, abundance=50}, new ResourceDeposit{type=MineralType.Uranium, abundance=40} }},
                {"Malawi", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Uranium, abundance=60}, new ResourceDeposit{type=MineralType.Coal, abundance=30} }},
                {"Lesotho", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Diamonds, abundance=80} }},
                {"Eswatini", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Coal, abundance=60}, new ResourceDeposit{type=MineralType.Diamonds, abundance=20} }},
                {"Tunisia", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Oil, abundance=30}, new ResourceDeposit{type=MineralType.NaturalGas, abundance=40} }},
                {"Rwanda", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Gold, abundance=20}, new ResourceDeposit{type=MineralType.Lithium, abundance=30} }},
                {"Burundi", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Gold, abundance=30}, new ResourceDeposit{type=MineralType.IronOre, abundance=40} }},
                {"Eritrea", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Gold, abundance=50}, new ResourceDeposit{type=MineralType.Copper, abundance=40} }},
                {"Somalia", new List<ResourceDeposit>{ new ResourceDeposit{type=MineralType.Oil, abundance=40}, new ResourceDeposit{type=MineralType.NaturalGas, abundance=30} }}
            };

            string[] guids = AssetDatabase.FindAssets("t:CountryDataSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CountryDataSO country = AssetDatabase.LoadAssetAtPath<CountryDataSO>(path);

                if (country != null)
                {
                    if (realGDP.ContainsKey(country.countryName)) country.gdp = realGDP[country.countryName] * 1e9f;
                    if (capitals.ContainsKey(country.countryName)) country.capitalCity = capitals[country.countryName];
                    
                    if (currencies.ContainsKey(country.countryName))
                    {
                        country.currencyName = currencies[country.countryName].name;
                        country.currencySymbol = currencies[country.countryName].symbol;
                    }

                    if (startExchangeRates.ContainsKey(country.countryName))
                    {
                        country.exchangeRate = startExchangeRates[country.countryName];
                        country.baseExchangeRate = startExchangeRates[country.countryName];
                    }

                    if (resourceMap.ContainsKey(country.countryName))
                    {
                        country.resources = new List<ResourceDeposit>(resourceMap[country.countryName]);
                    }

                    // --- AUTOMATIC FLAG LINKING ---
                    // Looks for a sprite named after the country in a specific folder
                    string flagPath = $"Assets/Textures/Flags/{country.countryName}.png";
                    Sprite flagSprite = AssetDatabase.LoadAssetAtPath<Sprite>(flagPath);
                    if (flagSprite != null)
                    {
                        country.countryFlag = flagSprite;
                    }
                    else
                    {
                        // Fallback: Check for .jpg or other common names
                        flagPath = $"Assets/Textures/Flags/{country.countryName}.jpg";
                        flagSprite = AssetDatabase.LoadAssetAtPath<Sprite>(flagPath);
                        if (flagSprite != null) country.countryFlag = flagSprite;
                    }

                    country.stability = Random.Range(60f, 85f);
                    country.security = Random.Range(40f, 70f);
                    
                    // Split starting debt between Domestic and Foreign
                    float totalStartingDebt = country.gdp * Random.Range(0.2f, 0.5f);
                    country.domesticDebt = totalStartingDebt * 0.6f; // 60% Domestic
                    country.foreignDebt = totalStartingDebt * 0.4f;  // 40% Foreign
                    
                    // Clear runtime lists to reset asset files
                    country.activeOperations = new List<MilitaryOperation>();
                    country.trainingPipelines = new List<RecruitmentPipeline>();
                    country.activeCovertOps = new List<CovertOperation>();
                    country.detectedThreats = new List<IntelligenceThreat>();
                    
                    EditorUtility.SetDirty(country);
                }
            }

            AssetDatabase.SaveAssets();
}

        [MenuItem("StateCraft/2. Generate Major Regions")]
        public static void GenerateMajorRegions()
        {
            Dictionary<string, string[]> majorRegions = new Dictionary<string, string[]>()
            {
                {"Nigeria", new string[] {"Lagos", "Kano", "Rivers", "Borno", "Abuja (FCT)", "Enugu"}},
                {"South Africa", new string[] {"Gauteng", "Western Cape", "KwaZulu-Natal", "Eastern Cape", "Limpopo"}},
                {"Egypt", new string[] {"Cairo", "Alexandria", "Giza", "Suez", "Luxor"}},
                {"Kenya", new string[] {"Nairobi", "Mombasa", "Kisumu", "Nakuru", "Turkana"}},
                {"Ethiopia", new string[] {"Addis Ababa", "Oromia", "Amhara", "Tigray", "Somali"}},
                {"Algeria", new string[] {"Algiers", "Oran", "Constantine", "Annaba"}},
                {"Morocco", new string[] {"Casablanca", "Rabat", "Marrakesh", "Fes", "Tangier"}},
                {"Ghana", new string[] {"Greater Accra", "Ashanti", "Western", "Northern"}},
                {"DR Congo", new string[] {"Kinshasa", "Katanga", "Kivu", "Kasai"}},
                {"Angola", new string[] {"Luanda", "Benguela", "Huambo", "Cabinda"}}
            };

            string[] guids = AssetDatabase.FindAssets("t:CountryDataSO");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CountryDataSO country = AssetDatabase.LoadAssetAtPath<CountryDataSO>(path);

                if (country != null && majorRegions.ContainsKey(country.countryName))
                {
                    string folderPath = $"Assets/Data/Regions/{country.countryName}";
                    if (!System.IO.Directory.Exists(folderPath))
                    {
                        System.IO.Directory.CreateDirectory(folderPath);
                    }

                    country.regions.Clear();

                    foreach (string rName in majorRegions[country.countryName])
                    {
                        string rPath = $"{folderPath}/{rName}.asset";
                        RegionDataSO region = AssetDatabase.LoadAssetAtPath<RegionDataSO>(rPath);

                        if (region == null)
                        {
                            region = ScriptableObject.CreateInstance<RegionDataSO>();
                            region.regionName = rName;
                            region.stability = 75f;
                            region.security = 50f;
                            AssetDatabase.CreateAsset(region, rPath);
                        }

                        country.regions.Add(region);
                    }
                    EditorUtility.SetDirty(country);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
}
    }
}
