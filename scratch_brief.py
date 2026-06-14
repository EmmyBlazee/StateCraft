import re

file_path = r"c:\Users\Tobi\StateCraft\Assets\Scripts\Core\ScenarioBriefings.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

archetypes = {
    "Maghreb": {
        "countries": ["Algeria", "Libya", "Tunisia", "Western_Sahara"],
        "title": "The North African Frontier",
        "context": "Positioned between the Sahara and the Mediterranean, your nation is a crucial bridge between continents. Energy exports and border security dominate the political landscape, while youth unemployment and regional rivalries threaten to destabilize your government.",
        "priorities": "• Secure the borders — weapons and insurgents flow freely in the desert\n• Diversify from hydrocarbons — the energy sector cannot employ everyone\n• Manage European and Middle Eastern diplomatic pressure effectively"
    },
    "Sahel": {
        "countries": ["Mali", "Niger", "Chad", "Mauritania", "Burkina_Faso", "Sudan"],
        "title": "The Sahel Conflict Zone",
        "context": "Your nation lies entirely within the Sahel — a harsh, unforgiving landscape gripped by overlapping insurgencies, droughts, and military coups. State control barely extends past the capital. You must rely on foreign aid while protecting your sovereignty from encroaching powers.",
        "priorities": "• Contain the insurgency — if the north falls, the capital is next\n• Secure foreign military assistance without becoming a proxy state\n• Invest in water and agricultural infrastructure before famine strikes"
    },
    "WestCoastal": {
        "countries": ["Senegal", "Cote_dIvoire", "Guinea", "Liberia", "Sierra_Leone", "Togo", "Benin", "Gambia", "Guinea-Bissau"],
        "title": "West Coastal Development",
        "context": "Driven by major ports and agricultural exports like cocoa and timber, your coastal nation holds immense economic potential. But political volatility, historical conflicts, and wealth inequality constantly threaten to disrupt trade and spark civil unrest.",
        "priorities": "• Modernise port infrastructure to dominate regional trade routes\n• Keep the military satisfied — a coup attempt is never entirely off the table\n• Distribute agricultural wealth to prevent rural uprisings"
    },
    "Central": {
        "countries": ["Cameroon", "Central_African_Republic", "Republic_of_the_Congo", "Gabon", "Equatorial_Guinea"],
        "title": "The Equatorial Heartland",
        "context": "Deep in the equatorial rainforest, your nation is blessed with timber, oil, and minerals. Unfortunately, these riches have fueled corruption, patronage networks, and armed rebel groups operating in the deep jungle where the national army struggles to operate.",
        "priorities": "• Crack down on corruption — leakage is destroying the national treasury\n• Build roads into the interior to project state power into rebel-held zones\n• Balance relations with competing superpowers eager for your resources"
    },
    "East": {
        "countries": ["Somalia", "Djibouti", "Eritrea", "South_Sudan", "Rwanda", "Burundi", "Uganda"],
        "title": "East African Crossroads",
        "context": "Your nation sits at the crux of the Great Lakes and the Horn of Africa, an area defined by deep historical ethnic tensions and rapid economic growth. Border skirmishes are common, and refugee flows put immense strain on your economy.",
        "priorities": "• Avoid drawn-out border conflicts that drain public resources\n• Integrate your economy with regional trade blocs to stimulate growth\n• Manage ethnic and political factions to maintain absolute stability"
    },
    "Southern": {
        "countries": ["Zambia", "Zimbabwe", "Botswana", "Namibia", "Mozambique", "Malawi", "Lesotho", "Eswatini"],
        "title": "Southern African Resources",
        "context": "Your nation boasts some of the continent\\'s most impressive mineral wealth and wildlife, but is plagued by systemic debt, inflation, and infrastructure decay. The legacy of colonial boundaries continues to challenge regional integration and domestic unity.",
        "priorities": "• Restructure national debt before hyperinflation claims the economy\n• Leverage mining revenues to build lasting civic infrastructure\n• Keep relations strong with South Africa, the regional hegemon"
    },
    "Islands": {
        "countries": ["Madagascar", "Mauritius", "Seychelles", "Comoros", "Cabo_Verde", "Sao_Tome_and_Principe"],
        "title": "The Island Economies",
        "context": "Isolated from the mainland chaos, your island nation enjoys unique strategic value and biodiversity. While spared from large-scale land wars, you face extreme economic vulnerability to global shipping disruptions, climate change, and foreign debt.",
        "priorities": "• Boost tourism and high-tech sectors to ensure economic independence\n• Establish maritime security to protect exclusive economic zones\n• Secure climate resilience funding from international organizations"
    }
}

new_entries = []
for arch, data in archetypes.items():
    for c in data["countries"]:
        c_key = c.replace("_", " ")
        entry = (
            f'            ["{c_key}"] = new Briefing\n'
            f'            {{\n'
            f'                title = "{c_key}: {data["title"]}",\n'
            f'                context = "{data["context"]}",\n'
            f'                priorities = "{data["priorities"]}"\n'
            f'            }},\n\n'
        )
        new_entries.append(entry)

insert_str = "".join(new_entries)

search_str = r'(\s+\["Angola"\] = new Briefing.*?\}),(\s*)\};'
match = re.search(search_str, content, re.DOTALL)
if match:
    new_dict_end = match.group(1) + ",\n\n" + insert_str + match.group(2) + "};"
    replaced = content[:match.start()] + new_dict_end + content[match.end():]
    
    with open(file_path, "w", encoding="utf-8") as f:
        f.write(replaced)
    print("Success! added 45 entries.")
else:
    print("Could not find injection point.")
