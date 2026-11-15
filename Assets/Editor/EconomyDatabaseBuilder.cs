#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using StateCraft.Data.Economy;

namespace StateCraft.Editor
{
    /// <summary>
    /// One-shot helper: creates all EconomyDatabase sub-assets and populates them
    /// with the values previously hardcoded in EconomyTabContent.
    ///
    /// Usage: StateCraft menu → Create Economy Database Assets
    /// After running, drag the generated EconomyDatabase asset onto GameManager.EconomyDatabase.
    /// </summary>
    public static class EconomyDatabaseBuilder
    {
        [MenuItem("StateCraft/Create Economy Database Assets")]
        public static void Build()
        {
            // ── Ensure output folder exists ───────────────────────────────────
            const string folder = "Assets/ScriptableObjects/Economy";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Economy");

            // ── Infrastructure ────────────────────────────────────────────────
            CreateInfra(folder, "Roads",     "NATIONAL HIGHWAYS",    InfraCategory.Transport, "TRN", "Links farms to cities; improves tax collection & army deployment speed.", 8f,  2.5f);
            CreateInfra(folder, "Rail",      "RAIL FREIGHT CORRIDORS", InfraCategory.Transport, "TRN", "Bulk minerals & grain move inland; lowers export delays.", 12f, 3f);
            CreateInfra(folder, "Ports",     "SEAPORTS & HARBORS",   InfraCategory.Transport, "TRN", "Drives resource dividends and trade deal value.", 15f, 4f);
            CreateInfra(folder, "Airports",  "AVIATION HUBS",        InfraCategory.Transport, "TRN", "Business travel & air cargo; boosts GDP growth modifier.", 18f, 3.5f);
            CreateInfra(folder, "Power",     "NATIONAL POWER GRID",  InfraCategory.Energy,    "PWR", "Stable power supply for manufacturing and households.", 20f, 0.15f);
            CreateInfra(folder, "Health",    "HEALTHCARE NETWORK",   InfraCategory.Social,    "SOC", "Raises stability & labour productivity.", 6f,  1.5f);
            CreateInfra(folder, "Education", "EDUCATION SYSTEM",     InfraCategory.Social,    "SOC", "Feeds IT/fintech sectors and long-run growth.", 7f,  2f);
            CreateInfra(folder, "Telecom",   "DIGITAL BACKBONE",     InfraCategory.Digital,   "NET", "Enables tech industry & intelligence coverage.", 9f,  2.5f);

            // ── Industries ────────────────────────────────────────────────────
            CreateIndustry(folder, "Oil",    "OIL & GAS",       0.05f, 10f, 3f);
            CreateIndustry(folder, "Mining", "MINING & MINERALS", 0.02f, 8f, 2.5f);
            CreateIndustry(folder, "Agri",   "AGRICULTURE",     0.03f, 5f,  1.5f);
            CreateIndustry(folder, "Mfg",    "MANUFACTURING",   0.025f, 12f, 3f);
            CreateIndustry(folder, "Tech",   "TECH & FINTECH",  0.02f, 14f, 2f);

            // ── Commodities ───────────────────────────────────────────────────
            CreateCommodity(folder, "Electricity",       "ELECTRICITY",        "Energy & Power",    "MW",      "National power grid load");
            CreateCommodity(folder, "CrudeOil",          "CRUDE OIL",          "Energy & Power",    "Barrels", "Export / refining feedstock");
            CreateCommodity(folder, "Rice",              "RICE",               "Food & Agriculture","Tonnes",  "Staple food security");
            CreateCommodity(folder, "Cassava",           "CASSAVA",            "Food & Agriculture","Tonnes",  "Staple food security");
            CreateCommodity(folder, "Cocoa",             "COCOA",              "Food & Agriculture","Tonnes",  "Cash crop export");
            CreateCommodity(folder, "ProcessedMinerals", "PROCESSED MINERALS", "Materials & Goods", "Tonnes",  "Industrial inputs");
            CreateCommodity(folder, "TechGoods",         "TECH GOODS",         "Materials & Goods", "Units",   "High-value exports");

            // ── Blueprints ────────────────────────────────────────────────────
            CreateBlueprint(folder, "Coastal Highway Phase II",    "Infra",  "Roads",        "",  45f,  18, "Paved corridor linking ports to interior.");
            CreateBlueprint(folder, "Trans-National Rail Spine",   "Infra",  "Rail",         "",  80f,  36, "Heavy freight line for minerals & grain.");
            CreateBlueprint(folder, "Deep-Water Port Expansion",   "Infra",  "Ports",        "",  60f,  24, "Container terminals & export berths.");
            CreateBlueprint(folder, "Gas-Fired Power Complex",     "Energy", "PowerGas",     "",  35f,  14, "+500 MW baseload capacity.");
            CreateBlueprint(folder, "Hydroelectric Dam",           "Energy", "PowerHydro",   "",  55f,  30, "Renewable surge for industrial zones.");
            CreateBlueprint(folder, "Nuclear Power Station",       "Energy", "PowerNuclear", "", 120f,  48, "Requires uranium imports or domestic supply.");
            CreateBlueprint(folder, "Solar Belt Programme",        "Energy", "PowerSolar",   "",  22f,  10, "Distributed grids for rural towns.");
            CreateBlueprint(folder, "Rice Belt Mechanization",     "Agri",   "indAgriRice",  "",  18f,  12, "Irrigation & mill upgrades.");
            CreateBlueprint(folder, "Cocoa Processing Park",       "Agri",   "indAgriCocoa", "",  15f,  10, "Bean-to-bar export value chain.");
            CreateBlueprint(folder, "National Data Centre",        "Tech",   "indTechIT",    "",  25f,  16, "Cloud & fintech backbone.");

            // ── Master database ───────────────────────────────────────────────
            string dbPath = $"{folder}/EconomyDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<EconomyDatabaseSO>(dbPath)
                     ?? ScriptableObject.CreateInstance<EconomyDatabaseSO>();

            db.infrastructure.Clear();
            db.industries.Clear();
            db.commodities.Clear();
            db.blueprints.Clear();

            foreach (var id in new[]{"Roads","Rail","Ports","Airports","Power","Health","Education","Telecom"})
                db.infrastructure.Add(AssetDatabase.LoadAssetAtPath<InfraDataSO>($"{folder}/Infra_{id}.asset"));

            foreach (var id in new[]{"Oil","Mining","Agri","Mfg","Tech"})
                db.industries.Add(AssetDatabase.LoadAssetAtPath<IndustryDataSO>($"{folder}/Industry_{id}.asset"));

            foreach (var id in new[]{"Electricity","CrudeOil","Rice","Cassava","Cocoa","ProcessedMinerals","TechGoods"})
                db.commodities.Add(AssetDatabase.LoadAssetAtPath<CommodityDataSO>($"{folder}/Commodity_{id}.asset"));

            foreach (var n in new[]{
                "Coastal Highway Phase II","Trans-National Rail Spine","Deep-Water Port Expansion",
                "Gas-Fired Power Complex","Hydroelectric Dam","Nuclear Power Station","Solar Belt Programme",
                "Rice Belt Mechanization","Cocoa Processing Park","National Data Centre"})
                db.blueprints.Add(AssetDatabase.LoadAssetAtPath<BlueprintDataSO>($"{folder}/Blueprint_{Sanitize(n)}.asset"));

            if (!AssetDatabase.Contains(db))
                AssetDatabase.CreateAsset(db, dbPath);
            else
                EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Economy Database", "All assets created!\n\nDrag 'EconomyDatabase' asset onto GameManager.EconomyDatabase in the Inspector.", "OK");
            Selection.activeObject = db;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        static void CreateInfra(string folder, string id, string title, InfraCategory cat, string icon, string effect, float baseCost, float costPerLevel)
        {
            string path = $"{folder}/Infra_{id}.asset";
            var so = AssetDatabase.LoadAssetAtPath<InfraDataSO>(path) ?? ScriptableObject.CreateInstance<InfraDataSO>();
            so.id = id; so.title = title; so.category = cat; so.iconTag = icon;
            so.effectDescription = effect; so.baseCost = baseCost; so.costPerLevel = costPerLevel;
            SaveOrMark(so, path);
        }

        static void CreateIndustry(string folder, string id, string title, float gdpShare, float baseCost, float costPerLevel)
        {
            string path = $"{folder}/Industry_{id}.asset";
            var so = AssetDatabase.LoadAssetAtPath<IndustryDataSO>(path) ?? ScriptableObject.CreateInstance<IndustryDataSO>();
            so.id = id; so.title = title; so.gdpShareAtMax = gdpShare; so.baseCost = baseCost; so.costPerLevel = costPerLevel;
            SaveOrMark(so, path);
        }

        static void CreateCommodity(string folder, string id, string title, string category, string unit, string role)
        {
            string path = $"{folder}/Commodity_{id}.asset";
            var so = AssetDatabase.LoadAssetAtPath<CommodityDataSO>(path) ?? ScriptableObject.CreateInstance<CommodityDataSO>();
            so.id = id; so.title = title; so.category = category; so.unit = unit; so.role = role;
            SaveOrMark(so, path);
        }

        static void CreateBlueprint(string folder, string name, string type, string targetId, string region, float cost, int months, string desc)
        {
            string path = $"{folder}/Blueprint_{Sanitize(name)}.asset";
            var so = AssetDatabase.LoadAssetAtPath<BlueprintDataSO>(path) ?? ScriptableObject.CreateInstance<BlueprintDataSO>();
            so.blueprintName = name; so.type = type; so.targetId = targetId; so.regionHint = region;
            so.totalCostB = cost; so.months = months; so.description = desc;
            SaveOrMark(so, path);
        }

        static void SaveOrMark(ScriptableObject so, string path)
        {
            if (!AssetDatabase.Contains(so)) AssetDatabase.CreateAsset(so, path);
            else EditorUtility.SetDirty(so);
        }

        static string Sanitize(string s) => s.Replace(" ", "_").Replace("&", "and").Replace("/", "-");
    }
}
#endif
