# StateCraft 🌍

**StateCraft** is a deep geopolitical grand strategy and nation-simulation game built in Unity. Lead a nation through the complex realities of modern statecraft—balancing economic growth, domestic politics, intelligence, military power, diplomacy, and global crises.

---

## 📌 Highlights & Core Features

### 🏛️ Domestic Politics & Governance
- **Approval & Stability**: Govern regional populations with distinct stability, security, and insurgency metrics.
- **Elections & Factions**: Navigate political factions and democratic elections where public sentiment dictates political survival.
- **Corruption & Reforms**: Enact nationwide policies to combat corruption, reform institutions, and improve state capacity.

### 📈 Comprehensive Economic Simulation
- **National Budget & Debt**: Manage tax brackets, government expenditures, interest rates, sovereign debt, and inflation.
- **Commodities & Industries**: Exploit natural resources (oil, minerals, agriculture, gas), construct industries, and research production blueprints.
- **Power Grid Management**: Balance electricity supply and demand across regions with diverse power generation infrastructure.
- **Bilateral Trade Agreements**: Negotiate trade pacts, export commodities, and secure crucial imports.

### ⚔️ Military & Defense
- **Branch Management**: Command the Army, Navy, Air Force, and Rapid Deployment Forces (RDF).
- **Weapons Procurement**: Purchase, research, and manufacture modern defense systems and equipment.
- **Postures & Military Bases**: Establish military bases, set defensive/offensive postures, and mobilize forces.
- **Dynamic War System**: Experience multi-phase warfare with frontline progress, momentum shifts, casualties, territorial changes, and peace negotiations.

### 🕵️ Intelligence & Covert Operations
- **Reconnaissance & Surveillance**: Monitor neighboring threats, satellite intel, and border movements.
- **Covert Missions**: Launch intelligence operations, counter-insurgency campaigns, and asymmetric maneuvers.
- **Threat Assessment**: Evaluate global and regional military capabilities with automated defense analysis.

### 🌐 Diplomacy & Living World
- **Relations Matrix**: Dynamic bilateral relations influenced by historical ties, trade, treaties, and diplomatic actions.
- **World Tension**: Global friction rises with skirmishes and warfare, prompting international reactions.
- **News Broadcast & Dispatch Alerts**: Stay informed with live news tickers, global media events, and classified cabinet dispatches.

---

## 🗺️ Featured Nations & Scenarios

Experience tailor-made historical and geopolitical briefings across diverse regional powers:

- **Nigeria**: Manage oil dependency, border friction, economic diversification, and electoral pressure.
- **Ghana**: Preserve democratic stability, tackle unemployment, and balance regional competition.
- **Ethiopia**: Restore unity in restive regions, counter drought, and assert Horn of Africa leadership.
- **South Africa**: Tackle inequality, modernize the power grid, and project continental influence.
- **Egypt**: Navigate Nile water disputes, stimulate non-oil sectors, and secure regional partnerships.
- **Kenya**: Drive East Africa's economic engine while countering cross-border insurgent threats.
- **Morocco**: Leverage trade with Europe, expand renewable energy, and advance diplomatic recognition.
- **DR Congo**: Pacify eastern provinces, secure vast mineral resources, and establish state presence.
- **Tanzania & Angola**: Develop offshore natural gas and post-conflict oil infrastructure.

---

## 🏗️ Architecture & Technical Stack

- **Engine**: [Unity](https://unity.com/) (Universal Render Pipeline / Built-in)
- **UI Framework**: **Unity UI Toolkit** (UXML / USS) with responsive custom elements, radial indicators, and animated data dashboards.
- **Architecture**:
  - **ScriptableObject Data Layer**: Modular configurations for countries, regions, commodities, weapons, policies, and events.
  - **Event-Driven Subsystems**: Loosely coupled architecture powered by an `EventBus` and `GameManager`.
  - **Turn & Real-Time Simulation**: Flexible temporal simulation with monthly tick resolution and real-time execution.

---

## 📁 Repository Structure

```text
StateCraft/
├── Assets/
│   ├── Editor/             # Custom Unity editor windows, asset builders, and tooling
│   └── Scripts/
│       ├── Core/           # Game context, EventBus, GameManager, scenario briefings
│       ├── Data/           # ScriptableObjects (Economy, Military, Policies, Regions, Countries)
│       ├── Interfaces/     # Core system interfaces and contracts
│       ├── Map/            # Interactive camera, country controllers, and 3D visualizers
│       ├── Systems/        # Simulation backends (Economy, War, Politics, Intelligence, Diplomacy)
│       └── UI/             # UI Toolkit controllers, styling, and dashboard tabs
├── ProjectSettings/        # Unity project configurations
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites
- **Unity 2022.3 LTS** (or newer recommended)
- Git LFS (if tracking large binary assets)

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/EmmyBlazee/StateCraft.git
   ```
2. Open the project folder in **Unity Hub**.
3. Allow Unity to import and compile packages.
4. Open the initial scene in `Assets/Scenes/` (e.g., `Main` or `SplashScene`).
5. Press **Play** in the Unity Editor to start the simulation.

---

## 📄 License

This project is licensed under the terms specified in the repository. See [LICENSE](LICENSE) for details (if applicable).
