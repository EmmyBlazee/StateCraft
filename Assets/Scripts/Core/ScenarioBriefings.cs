
using System.Collections.Generic;
using StateCraft.Data;

namespace StateCraft.Core
{
    public static class ScenarioBriefings
    {
        private struct Briefing
        {
            public string title;
            public string context;
            public string priorities;
        }

        private static readonly Dictionary<string, Briefing> _briefings = new Dictionary<string, Briefing>
        {
            ["Nigeria"] = new Briefing
            {
                title = "Nigeria: Oil, Power & the Long Game",
                context =
                    "Africa's most populous nation teeters on a knife's edge. " +
                    "Oil revenues fund the federal budget, but global prices are volatile and domestic refining is weak. " +
                    "Tension on the Ghana border is rising — your generals want authorisation to mobilise, " +
                    "but an election is only three years away and the people are watching.",
                priorities =
                    "• Secure the treasury — military mobilisation is expensive and the deficit is widening\n" +
                    "• Decide your posture on Ghana — de-escalate diplomatically or prepare for war\n" +
                    "• Keep approval above 50% — an unpopular president doesn't survive election night"
            },

            ["Ghana"] = new Briefing
            {
                title = "Ghana: Democracy Under Pressure",
                context =
                    "Ghana has long been West Africa's democratic model, but cracks are showing. " +
                    "Cocoa revenues are falling, youth unemployment is rising, and your northern regions " +
                    "are restless. Neighbouring Nigeria watches your every move.",
                priorities =
                    "• Diversify the economy — cocoa dependence is a strategic weakness\n" +
                    "• Stabilise the north — insurgent activity is creeping toward the border\n" +
                    "• Manage the Nigeria relationship — confrontation would be costly"
            },

            ["Ethiopia"] = new Briefing
            {
                title = "Ethiopia: The Horn on the Brink",
                context =
                    "Ethiopia is the Horn of Africa's giant — but internal fractures threaten its unity. " +
                    "The Tigray conflict has left scars, regional factions press for autonomy, " +
                    "and drought is hammering agricultural output. Regional leadership demands strength and restraint in equal measure.",
                priorities =
                    "• Restore stability to restive regions before insurgency spreads\n" +
                    "• Rebuild the economy — agricultural collapse is your biggest internal threat\n" +
                    "• Assert regional influence carefully — neighbours are watching for weakness"
            },

            ["South Africa"] = new Briefing
            {
                title = "South Africa: The Unfinished Promise",
                context =
                    "South Africa carries the weight of its history and the hope of a continent. " +
                    "The most industrialised economy in sub-Saharan Africa is plagued by inequality, " +
                    "rolling blackouts, and political tension. The world expects leadership. " +
                    "Your citizens expect results.",
                priorities =
                    "• Fix the power grid — load-shedding is strangling business and approval ratings\n" +
                    "• Tackle unemployment — it fuels the instability you cannot afford\n" +
                    "• Project continental authority — South Africa should lead, not react"
            },

            ["Egypt"] = new Briefing
            {
                title = "Egypt: Anchor of the North",
                context =
                    "Egypt sits at the crossroads of Africa and the Middle East — a strategic prize " +
                    "and a perpetual battleground of influence. The Nile is life itself, " +
                    "and upstream dams are threatening your water security. " +
                    "Your military is strong but your economy needs oxygen.",
                priorities =
                    "• Resolve the Nile water dispute diplomatically before it becomes a crisis\n" +
                    "• Grow non-oil revenue — tourism and industry need investment\n" +
                    "• Maintain regional security partnerships — instability in Libya bleeds south"
            },

            ["Kenya"] = new Briefing
            {
                title = "Kenya: East Africa's Engine",
                context =
                    "Nairobi is East Africa's financial capital and Kenya its most dynamic economy. " +
                    "But al-Shabaab pressure from Somalia, drought in the north, and urban inequality " +
                    "test your government daily. You have ambition — the question is execution.",
                priorities =
                    "• Secure the Somali border — insurgent incursions are damaging your global image\n" +
                    "• Invest in northern infrastructure — neglect breeds radicalisation\n" +
                    "• Keep foreign investment flowing — your growth story must not stall"
            },

            ["Morocco"] = new Briefing
            {
                title = "Morocco: Gateway Between Worlds",
                context =
                    "Morocco straddles two worlds — Africa and Europe, tradition and modernity. " +
                    "Your phosphate wealth gives you leverage, your proximity to Europe gives you opportunity. " +
                    "But the Western Sahara question shadows every diplomatic move you make.",
                priorities =
                    "• Advance the Western Sahara position — international recognition is within reach\n" +
                    "• Deepen Europe ties — trade and migration agreements define your decade\n" +
                    "• Grow renewable energy — Morocco can be Africa's green power hub"
            },

            ["DR Congo"] = new Briefing
            {
                title = "DR Congo: Vast, Rich, Ungoverned",
                context =
                    "The Democratic Republic of Congo holds more mineral wealth than almost any nation on earth " +
                    "and yet its people remain among the poorest. Armed groups control vast swaths of the east. " +
                    "The state barely reaches the jungle. You have inherited both the treasure and the curse.",
                priorities =
                    "• Pacify the eastern provinces — armed groups are bleeding the nation dry\n" +
                    "• Build extractive revenue that reaches the treasury, not just warlords\n" +
                    "• Establish state presence in at least three ungoverned regions this term"
            },

            ["Tanzania"] = new Briefing
            {
                title = "Tanzania: Steady Hand, Rising Ambition",
                context =
                    "Tanzania is East Africa's quiet achiever — politically stable, geographically vast, " +
                    "and sitting on significant natural gas reserves. The challenge is turning potential into prosperity " +
                    "before regional competitors outpace you.",
                priorities =
                    "• Develop the gas sector — offshore reserves could transform your budget\n" +
                    "• Invest in the southern corridor — infrastructure unlocks growth\n" +
                    "• Maintain the tradition of political stability — it is your greatest asset"
            },

            ["Angola"] = new Briefing
            {
                title = "Angola: Oil After the War",
                context =
                    "Angola's civil war ended two decades ago but the scars remain. " +
                    "Oil wealth rebuilt Luanda but left the interior behind. " +
                    "Corruption hollowed out institutions. Now you must diversify or decline " +
                    "as global oil demand shifts.",
                priorities =
                    "• Reduce oil dependency — a single-commodity economy is a fragile economy\n" +
                    "• Fight corruption at the institutional level — it is killing investment\n" +
                    "• Develop the interior — Luanda cannot be the only story"
            },

            ["Algeria"] = new Briefing
            {
                title = "Algeria: The North African Frontier",
                context = "Positioned between the Sahara and the Mediterranean, your nation is a crucial bridge between continents. Energy exports and border security dominate the political landscape, while youth unemployment and regional rivalries threaten to destabilize your government.",
                priorities = "• Secure the borders — weapons and insurgents flow freely in the desert\n• Diversify from hydrocarbons — the energy sector cannot employ everyone\n• Manage European and Middle Eastern diplomatic pressure effectively"
            },

            ["Libya"] = new Briefing
            {
                title = "Libya: The North African Frontier",
                context = "Positioned between the Sahara and the Mediterranean, your nation is a crucial bridge between continents. Energy exports and border security dominate the political landscape, while youth unemployment and regional rivalries threaten to destabilize your government.",
                priorities = "• Secure the borders — weapons and insurgents flow freely in the desert\n• Diversify from hydrocarbons — the energy sector cannot employ everyone\n• Manage European and Middle Eastern diplomatic pressure effectively"
            },

            ["Tunisia"] = new Briefing
            {
                title = "Tunisia: The North African Frontier",
                context = "Positioned between the Sahara and the Mediterranean, your nation is a crucial bridge between continents. Energy exports and border security dominate the political landscape, while youth unemployment and regional rivalries threaten to destabilize your government.",
                priorities = "• Secure the borders — weapons and insurgents flow freely in the desert\n• Diversify from hydrocarbons — the energy sector cannot employ everyone\n• Manage European and Middle Eastern diplomatic pressure effectively"
            },

            ["Western Sahara"] = new Briefing
            {
                title = "Western Sahara: The North African Frontier",
                context = "Positioned between the Sahara and the Mediterranean, your nation is a crucial bridge between continents. Energy exports and border security dominate the political landscape, while youth unemployment and regional rivalries threaten to destabilize your government.",
                priorities = "• Secure the borders — weapons and insurgents flow freely in the desert\n• Diversify from hydrocarbons — the energy sector cannot employ everyone\n• Manage European and Middle Eastern diplomatic pressure effectively"
            },

            ["Mali"] = new Briefing
            {
                title = "Mali: The Sahel Conflict Zone",
                context = "Your nation lies entirely within the Sahel — a harsh, unforgiving landscape gripped by overlapping insurgencies, droughts, and military coups. State control barely extends past the capital. You must rely on foreign aid while protecting your sovereignty from encroaching powers.",
                priorities = "• Contain the insurgency — if the north falls, the capital is next\n• Secure foreign military assistance without becoming a proxy state\n• Invest in water and agricultural infrastructure before famine strikes"
            },

            ["Niger"] = new Briefing
            {
                title = "Niger: The Sahel Conflict Zone",
                context = "Your nation lies entirely within the Sahel — a harsh, unforgiving landscape gripped by overlapping insurgencies, droughts, and military coups. State control barely extends past the capital. You must rely on foreign aid while protecting your sovereignty from encroaching powers.",
                priorities = "• Contain the insurgency — if the north falls, the capital is next\n• Secure foreign military assistance without becoming a proxy state\n• Invest in water and agricultural infrastructure before famine strikes"
            },

            ["Chad"] = new Briefing
            {
                title = "Chad: The Sahel Conflict Zone",
                context = "Your nation lies entirely within the Sahel — a harsh, unforgiving landscape gripped by overlapping insurgencies, droughts, and military coups. State control barely extends past the capital. You must rely on foreign aid while protecting your sovereignty from encroaching powers.",
                priorities = "• Contain the insurgency — if the north falls, the capital is next\n• Secure foreign military assistance without becoming a proxy state\n• Invest in water and agricultural infrastructure before famine strikes"
            },

            ["Mauritania"] = new Briefing
            {
                title = "Mauritania: The Sahel Conflict Zone",
                context = "Your nation lies entirely within the Sahel — a harsh, unforgiving landscape gripped by overlapping insurgencies, droughts, and military coups. State control barely extends past the capital. You must rely on foreign aid while protecting your sovereignty from encroaching powers.",
                priorities = "• Contain the insurgency — if the north falls, the capital is next\n• Secure foreign military assistance without becoming a proxy state\n• Invest in water and agricultural infrastructure before famine strikes"
            },

            ["Burkina Faso"] = new Briefing
            {
                title = "Burkina Faso: The Sahel Conflict Zone",
                context = "Your nation lies entirely within the Sahel — a harsh, unforgiving landscape gripped by overlapping insurgencies, droughts, and military coups. State control barely extends past the capital. You must rely on foreign aid while protecting your sovereignty from encroaching powers.",
                priorities = "• Contain the insurgency — if the north falls, the capital is next\n• Secure foreign military assistance without becoming a proxy state\n• Invest in water and agricultural infrastructure before famine strikes"
            },

            ["Sudan"] = new Briefing
            {
                title = "Sudan: The Sahel Conflict Zone",
                context = "Your nation lies entirely within the Sahel — a harsh, unforgiving landscape gripped by overlapping insurgencies, droughts, and military coups. State control barely extends past the capital. You must rely on foreign aid while protecting your sovereignty from encroaching powers.",
                priorities = "• Contain the insurgency — if the north falls, the capital is next\n• Secure foreign military assistance without becoming a proxy state\n• Invest in water and agricultural infrastructure before famine strikes"
            },

            ["Senegal"] = new Briefing
            {
                title = "Senegal: West Coastal Development",
                context = "Driven by major ports and agricultural exports like cocoa and timber, your coastal nation holds immense economic potential. But political volatility, historical conflicts, and wealth inequality constantly threaten to disrupt trade and spark civil unrest.",
                priorities = "• Modernise port infrastructure to dominate regional trade routes\n• Keep the military satisfied — a coup attempt is never entirely off the table\n• Distribute agricultural wealth to prevent rural uprisings"
            },

            ["Cote dIvoire"] = new Briefing
            {
                title = "Cote dIvoire: West Coastal Development",
                context = "Driven by major ports and agricultural exports like cocoa and timber, your coastal nation holds immense economic potential. But political volatility, historical conflicts, and wealth inequality constantly threaten to disrupt trade and spark civil unrest.",
                priorities = "• Modernise port infrastructure to dominate regional trade routes\n• Keep the military satisfied — a coup attempt is never entirely off the table\n• Distribute agricultural wealth to prevent rural uprisings"
            },

            ["Guinea"] = new Briefing
            {
                title = "Guinea: West Coastal Development",
                context = "Driven by major ports and agricultural exports like cocoa and timber, your coastal nation holds immense economic potential. But political volatility, historical conflicts, and wealth inequality constantly threaten to disrupt trade and spark civil unrest.",
                priorities = "• Modernise port infrastructure to dominate regional trade routes\n• Keep the military satisfied — a coup attempt is never entirely off the table\n• Distribute agricultural wealth to prevent rural uprisings"
            },

            ["Liberia"] = new Briefing
            {
                title = "Liberia: West Coastal Development",
                context = "Driven by major ports and agricultural exports like cocoa and timber, your coastal nation holds immense economic potential. But political volatility, historical conflicts, and wealth inequality constantly threaten to disrupt trade and spark civil unrest.",
                priorities = "• Modernise port infrastructure to dominate regional trade routes\n• Keep the military satisfied — a coup attempt is never entirely off the table\n• Distribute agricultural wealth to prevent rural uprisings"
            },

            ["Sierra Leone"] = new Briefing
            {
                title = "Sierra Leone: West Coastal Development",
                context = "Driven by major ports and agricultural exports like cocoa and timber, your coastal nation holds immense economic potential. But political volatility, historical conflicts, and wealth inequality constantly threaten to disrupt trade and spark civil unrest.",
                priorities = "• Modernise port infrastructure to dominate regional trade routes\n• Keep the military satisfied — a coup attempt is never entirely off the table\n• Distribute agricultural wealth to prevent rural uprisings"
            },

            ["Togo"] = new Briefing
            {
                title = "Togo: West Coastal Development",
                context = "Driven by major ports and agricultural exports like cocoa and timber, your coastal nation holds immense economic potential. But political volatility, historical conflicts, and wealth inequality constantly threaten to disrupt trade and spark civil unrest.",
                priorities = "• Modernise port infrastructure to dominate regional trade routes\n• Keep the military satisfied — a coup attempt is never entirely off the table\n• Distribute agricultural wealth to prevent rural uprisings"
            },

            ["Benin"] = new Briefing
            {
                title = "Benin: West Coastal Development",
                context = "Driven by major ports and agricultural exports like cocoa and timber, your coastal nation holds immense economic potential. But political volatility, historical conflicts, and wealth inequality constantly threaten to disrupt trade and spark civil unrest.",
                priorities = "• Modernise port infrastructure to dominate regional trade routes\n• Keep the military satisfied — a coup attempt is never entirely off the table\n• Distribute agricultural wealth to prevent rural uprisings"
            },

            ["Gambia"] = new Briefing
            {
                title = "Gambia: West Coastal Development",
                context = "Driven by major ports and agricultural exports like cocoa and timber, your coastal nation holds immense economic potential. But political volatility, historical conflicts, and wealth inequality constantly threaten to disrupt trade and spark civil unrest.",
                priorities = "• Modernise port infrastructure to dominate regional trade routes\n• Keep the military satisfied — a coup attempt is never entirely off the table\n• Distribute agricultural wealth to prevent rural uprisings"
            },

            ["Guinea-Bissau"] = new Briefing
            {
                title = "Guinea-Bissau: West Coastal Development",
                context = "Driven by major ports and agricultural exports like cocoa and timber, your coastal nation holds immense economic potential. But political volatility, historical conflicts, and wealth inequality constantly threaten to disrupt trade and spark civil unrest.",
                priorities = "• Modernise port infrastructure to dominate regional trade routes\n• Keep the military satisfied — a coup attempt is never entirely off the table\n• Distribute agricultural wealth to prevent rural uprisings"
            },

            ["Cameroon"] = new Briefing
            {
                title = "Cameroon: The Equatorial Heartland",
                context = "Deep in the equatorial rainforest, your nation is blessed with timber, oil, and minerals. Unfortunately, these riches have fueled corruption, patronage networks, and armed rebel groups operating in the deep jungle where the national army struggles to operate.",
                priorities = "• Crack down on corruption — leakage is destroying the national treasury\n• Build roads into the interior to project state power into rebel-held zones\n• Balance relations with competing superpowers eager for your resources"
            },

            ["Central African Republic"] = new Briefing
            {
                title = "Central African Republic: The Equatorial Heartland",
                context = "Deep in the equatorial rainforest, your nation is blessed with timber, oil, and minerals. Unfortunately, these riches have fueled corruption, patronage networks, and armed rebel groups operating in the deep jungle where the national army struggles to operate.",
                priorities = "• Crack down on corruption — leakage is destroying the national treasury\n• Build roads into the interior to project state power into rebel-held zones\n• Balance relations with competing superpowers eager for your resources"
            },

            ["Republic of the Congo"] = new Briefing
            {
                title = "Republic of the Congo: The Equatorial Heartland",
                context = "Deep in the equatorial rainforest, your nation is blessed with timber, oil, and minerals. Unfortunately, these riches have fueled corruption, patronage networks, and armed rebel groups operating in the deep jungle where the national army struggles to operate.",
                priorities = "• Crack down on corruption — leakage is destroying the national treasury\n• Build roads into the interior to project state power into rebel-held zones\n• Balance relations with competing superpowers eager for your resources"
            },

            ["Gabon"] = new Briefing
            {
                title = "Gabon: The Equatorial Heartland",
                context = "Deep in the equatorial rainforest, your nation is blessed with timber, oil, and minerals. Unfortunately, these riches have fueled corruption, patronage networks, and armed rebel groups operating in the deep jungle where the national army struggles to operate.",
                priorities = "• Crack down on corruption — leakage is destroying the national treasury\n• Build roads into the interior to project state power into rebel-held zones\n• Balance relations with competing superpowers eager for your resources"
            },

            ["Equatorial Guinea"] = new Briefing
            {
                title = "Equatorial Guinea: The Equatorial Heartland",
                context = "Deep in the equatorial rainforest, your nation is blessed with timber, oil, and minerals. Unfortunately, these riches have fueled corruption, patronage networks, and armed rebel groups operating in the deep jungle where the national army struggles to operate.",
                priorities = "• Crack down on corruption — leakage is destroying the national treasury\n• Build roads into the interior to project state power into rebel-held zones\n• Balance relations with competing superpowers eager for your resources"
            },

            ["Somalia"] = new Briefing
            {
                title = "Somalia: East African Crossroads",
                context = "Your nation sits at the crux of the Great Lakes and the Horn of Africa, an area defined by deep historical ethnic tensions and rapid economic growth. Border skirmishes are common, and refugee flows put immense strain on your economy.",
                priorities = "• Avoid drawn-out border conflicts that drain public resources\n• Integrate your economy with regional trade blocs to stimulate growth\n• Manage ethnic and political factions to maintain absolute stability"
            },

            ["Djibouti"] = new Briefing
            {
                title = "Djibouti: East African Crossroads",
                context = "Your nation sits at the crux of the Great Lakes and the Horn of Africa, an area defined by deep historical ethnic tensions and rapid economic growth. Border skirmishes are common, and refugee flows put immense strain on your economy.",
                priorities = "• Avoid drawn-out border conflicts that drain public resources\n• Integrate your economy with regional trade blocs to stimulate growth\n• Manage ethnic and political factions to maintain absolute stability"
            },

            ["Eritrea"] = new Briefing
            {
                title = "Eritrea: East African Crossroads",
                context = "Your nation sits at the crux of the Great Lakes and the Horn of Africa, an area defined by deep historical ethnic tensions and rapid economic growth. Border skirmishes are common, and refugee flows put immense strain on your economy.",
                priorities = "• Avoid drawn-out border conflicts that drain public resources\n• Integrate your economy with regional trade blocs to stimulate growth\n• Manage ethnic and political factions to maintain absolute stability"
            },

            ["South Sudan"] = new Briefing
            {
                title = "South Sudan: East African Crossroads",
                context = "Your nation sits at the crux of the Great Lakes and the Horn of Africa, an area defined by deep historical ethnic tensions and rapid economic growth. Border skirmishes are common, and refugee flows put immense strain on your economy.",
                priorities = "• Avoid drawn-out border conflicts that drain public resources\n• Integrate your economy with regional trade blocs to stimulate growth\n• Manage ethnic and political factions to maintain absolute stability"
            },

            ["Rwanda"] = new Briefing
            {
                title = "Rwanda: East African Crossroads",
                context = "Your nation sits at the crux of the Great Lakes and the Horn of Africa, an area defined by deep historical ethnic tensions and rapid economic growth. Border skirmishes are common, and refugee flows put immense strain on your economy.",
                priorities = "• Avoid drawn-out border conflicts that drain public resources\n• Integrate your economy with regional trade blocs to stimulate growth\n• Manage ethnic and political factions to maintain absolute stability"
            },

            ["Burundi"] = new Briefing
            {
                title = "Burundi: East African Crossroads",
                context = "Your nation sits at the crux of the Great Lakes and the Horn of Africa, an area defined by deep historical ethnic tensions and rapid economic growth. Border skirmishes are common, and refugee flows put immense strain on your economy.",
                priorities = "• Avoid drawn-out border conflicts that drain public resources\n• Integrate your economy with regional trade blocs to stimulate growth\n• Manage ethnic and political factions to maintain absolute stability"
            },

            ["Uganda"] = new Briefing
            {
                title = "Uganda: East African Crossroads",
                context = "Your nation sits at the crux of the Great Lakes and the Horn of Africa, an area defined by deep historical ethnic tensions and rapid economic growth. Border skirmishes are common, and refugee flows put immense strain on your economy.",
                priorities = "• Avoid drawn-out border conflicts that drain public resources\n• Integrate your economy with regional trade blocs to stimulate growth\n• Manage ethnic and political factions to maintain absolute stability"
            },

            ["Zambia"] = new Briefing
            {
                title = "Zambia: Southern African Resources",
                context = "Your nation boasts some of the continent\'s most impressive mineral wealth and wildlife, but is plagued by systemic debt, inflation, and infrastructure decay. The legacy of colonial boundaries continues to challenge regional integration and domestic unity.",
                priorities = "• Restructure national debt before hyperinflation claims the economy\n• Leverage mining revenues to build lasting civic infrastructure\n• Keep relations strong with South Africa, the regional hegemon"
            },

            ["Zimbabwe"] = new Briefing
            {
                title = "Zimbabwe: Southern African Resources",
                context = "Your nation boasts some of the continent\'s most impressive mineral wealth and wildlife, but is plagued by systemic debt, inflation, and infrastructure decay. The legacy of colonial boundaries continues to challenge regional integration and domestic unity.",
                priorities = "• Restructure national debt before hyperinflation claims the economy\n• Leverage mining revenues to build lasting civic infrastructure\n• Keep relations strong with South Africa, the regional hegemon"
            },

            ["Botswana"] = new Briefing
            {
                title = "Botswana: Southern African Resources",
                context = "Your nation boasts some of the continent\'s most impressive mineral wealth and wildlife, but is plagued by systemic debt, inflation, and infrastructure decay. The legacy of colonial boundaries continues to challenge regional integration and domestic unity.",
                priorities = "• Restructure national debt before hyperinflation claims the economy\n• Leverage mining revenues to build lasting civic infrastructure\n• Keep relations strong with South Africa, the regional hegemon"
            },

            ["Namibia"] = new Briefing
            {
                title = "Namibia: Southern African Resources",
                context = "Your nation boasts some of the continent\'s most impressive mineral wealth and wildlife, but is plagued by systemic debt, inflation, and infrastructure decay. The legacy of colonial boundaries continues to challenge regional integration and domestic unity.",
                priorities = "• Restructure national debt before hyperinflation claims the economy\n• Leverage mining revenues to build lasting civic infrastructure\n• Keep relations strong with South Africa, the regional hegemon"
            },

            ["Mozambique"] = new Briefing
            {
                title = "Mozambique: Southern African Resources",
                context = "Your nation boasts some of the continent\'s most impressive mineral wealth and wildlife, but is plagued by systemic debt, inflation, and infrastructure decay. The legacy of colonial boundaries continues to challenge regional integration and domestic unity.",
                priorities = "• Restructure national debt before hyperinflation claims the economy\n• Leverage mining revenues to build lasting civic infrastructure\n• Keep relations strong with South Africa, the regional hegemon"
            },

            ["Malawi"] = new Briefing
            {
                title = "Malawi: Southern African Resources",
                context = "Your nation boasts some of the continent\'s most impressive mineral wealth and wildlife, but is plagued by systemic debt, inflation, and infrastructure decay. The legacy of colonial boundaries continues to challenge regional integration and domestic unity.",
                priorities = "• Restructure national debt before hyperinflation claims the economy\n• Leverage mining revenues to build lasting civic infrastructure\n• Keep relations strong with South Africa, the regional hegemon"
            },

            ["Lesotho"] = new Briefing
            {
                title = "Lesotho: Southern African Resources",
                context = "Your nation boasts some of the continent\'s most impressive mineral wealth and wildlife, but is plagued by systemic debt, inflation, and infrastructure decay. The legacy of colonial boundaries continues to challenge regional integration and domestic unity.",
                priorities = "• Restructure national debt before hyperinflation claims the economy\n• Leverage mining revenues to build lasting civic infrastructure\n• Keep relations strong with South Africa, the regional hegemon"
            },

            ["Eswatini"] = new Briefing
            {
                title = "Eswatini: Southern African Resources",
                context = "Your nation boasts some of the continent\'s most impressive mineral wealth and wildlife, but is plagued by systemic debt, inflation, and infrastructure decay. The legacy of colonial boundaries continues to challenge regional integration and domestic unity.",
                priorities = "• Restructure national debt before hyperinflation claims the economy\n• Leverage mining revenues to build lasting civic infrastructure\n• Keep relations strong with South Africa, the regional hegemon"
            },

            ["Madagascar"] = new Briefing
            {
                title = "Madagascar: The Island Economies",
                context = "Isolated from the mainland chaos, your island nation enjoys unique strategic value and biodiversity. While spared from large-scale land wars, you face extreme economic vulnerability to global shipping disruptions, climate change, and foreign debt.",
                priorities = "• Boost tourism and high-tech sectors to ensure economic independence\n• Establish maritime security to protect exclusive economic zones\n• Secure climate resilience funding from international organizations"
            },

            ["Mauritius"] = new Briefing
            {
                title = "Mauritius: The Island Economies",
                context = "Isolated from the mainland chaos, your island nation enjoys unique strategic value and biodiversity. While spared from large-scale land wars, you face extreme economic vulnerability to global shipping disruptions, climate change, and foreign debt.",
                priorities = "• Boost tourism and high-tech sectors to ensure economic independence\n• Establish maritime security to protect exclusive economic zones\n• Secure climate resilience funding from international organizations"
            },

            ["Seychelles"] = new Briefing
            {
                title = "Seychelles: The Island Economies",
                context = "Isolated from the mainland chaos, your island nation enjoys unique strategic value and biodiversity. While spared from large-scale land wars, you face extreme economic vulnerability to global shipping disruptions, climate change, and foreign debt.",
                priorities = "• Boost tourism and high-tech sectors to ensure economic independence\n• Establish maritime security to protect exclusive economic zones\n• Secure climate resilience funding from international organizations"
            },

            ["Comoros"] = new Briefing
            {
                title = "Comoros: The Island Economies",
                context = "Isolated from the mainland chaos, your island nation enjoys unique strategic value and biodiversity. While spared from large-scale land wars, you face extreme economic vulnerability to global shipping disruptions, climate change, and foreign debt.",
                priorities = "• Boost tourism and high-tech sectors to ensure economic independence\n• Establish maritime security to protect exclusive economic zones\n• Secure climate resilience funding from international organizations"
            },

            ["Cabo Verde"] = new Briefing
            {
                title = "Cabo Verde: The Island Economies",
                context = "Isolated from the mainland chaos, your island nation enjoys unique strategic value and biodiversity. While spared from large-scale land wars, you face extreme economic vulnerability to global shipping disruptions, climate change, and foreign debt.",
                priorities = "• Boost tourism and high-tech sectors to ensure economic independence\n• Establish maritime security to protect exclusive economic zones\n• Secure climate resilience funding from international organizations"
            },

            ["Sao Tome and Principe"] = new Briefing
            {
                title = "Sao Tome and Principe: The Island Economies",
                context = "Isolated from the mainland chaos, your island nation enjoys unique strategic value and biodiversity. While spared from large-scale land wars, you face extreme economic vulnerability to global shipping disruptions, climate change, and foreign debt.",
                priorities = "• Boost tourism and high-tech sectors to ensure economic independence\n• Establish maritime security to protect exclusive economic zones\n• Secure climate resilience funding from international organizations"
            },

};

        public static void Apply(System.Collections.Generic.List<CountryDataSO> allCountries)
        {
            if (allCountries == null) return;
            foreach (var country in allCountries)
            {
                if (country == null) continue;
                if (_briefings.TryGetValue(country.countryName, out var b))
                {
                    country.scenarioTitle      = b.title;
                    country.scenarioContext    = b.context;
                    country.scenarioPriorities = b.priorities;
                }
            }
        }
    }
}
