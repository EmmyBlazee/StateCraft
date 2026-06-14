import os
import re

# 1. Write the USS file
uss_content = """/* ═══════════════════════════════════════════════════════════════
   MILITARY OPERATIONS TAB  —  Unity UI Toolkit Stylesheet
   Matches the National Defence Command screenshot exactly
   ═══════════════════════════════════════════════════════════════ */

/* ── Colour tokens ──────────────────────────────────────────── */
/* bg-deep    : #0a0e14   (outermost background)                  */
/* bg-panel   : #0d1117   (panel background)                      */
/* bg-card    : #111820   (card / row backgrounds)                */
/* border     : #1e2d3d   (subtle borders)                        */
/* accent-blue: #00a8ff   (links, highlights)                     */
/* accent-cyan: #00e5ff   (treasury / good values)                */
/* green      : #00e676   (low threat / readiness bars)           */
/* yellow     : #ffeb3b   (medium threat / progress)              */
/* red        : #f44336   (high threat / casualties)              */
/* text-main  : #e0e8f0   (primary text)                          */
/* text-dim   : #6b8099   (secondary / label text)                */

/* ══════════════════════════════════════════════════════════════ */
/*  BODY (3-column)                                              */
/* ══════════════════════════════════════════════════════════════ */
.ops-body {
    flex-direction: row;
    flex-grow: 1;
    padding: 10px;
    column-gap: 10px;
    background-color: #0a0e14;
    color: #e0e8f0;
    font-size: 11px;
}

.ops-col {
    flex-direction: column;
    row-gap: 10px;
}

.ops-col--left   { width: 260px; flex-shrink: 0; }
.ops-col--center { flex-grow: 1; }
.ops-col--right  { width: 300px; flex-shrink: 0; }

/* ══════════════════════════════════════════════════════════════ */
/*  SHARED PANEL                                                  */
/* ══════════════════════════════════════════════════════════════ */
.ops-panel {
    background-color: #0d1117;
    border-width: 1px;
    border-color: #1e2d3d;
    border-radius: 4px;
    padding: 12px;
    flex-direction: column;
}

.ops-panel--stretch { flex-grow: 1; }
.ops-panel--feed    { height: 260px; }
.ops-panel--intel   { flex-grow: 1; }

.ops-panel__title {
    font-size: 11px;
    -unity-font-style: bold;
    color: #e0e8f0;
    letter-spacing: 1px;
    margin-bottom: 10px;
}

.ops-panel-header-row {
    flex-direction: row;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 10px;
}

.ops-panel-header-row .ops-panel__title { margin-bottom: 0; }

/* ══════════════════════════════════════════════════════════════ */
/*  LINK BUTTON                                                   */
/* ══════════════════════════════════════════════════════════════ */
.ops-btn-link {
    background-color: transparent;
    border-width: 0;
    color: #00a8ff;
    font-size: 10px;
    padding: 4px 0;
    cursor: link;
    -unity-font-style: bold;
    align-self: flex-start;
    margin-top: 4px;
}

.ops-btn-link:hover { color: #00e5ff; }

/* ══════════════════════════════════════════════════════════════ */
/*  THREAT STATUS LIST                                            */
/* ══════════════════════════════════════════════════════════════ */
.threat-list {
    flex-grow: 1;
}

.threat-row {
    flex-direction: row;
    align-items: center;
    padding: 8px 6px;
    border-bottom-width: 1px;
    border-bottom-color: #1a2535;
}

.threat-row__icon {
    width: 28px;
    height: 28px;
    border-radius: 14px;
    border-width: 2px;
    align-items: center;
    justify-content: center;
    margin-right: 10px;
    flex-shrink: 0;
}

.threat-row__icon--high   { border-color: #f44336; background-color: rgba(244,67,54,0.12); }
.threat-row__icon--medium { border-color: #ffeb3b; background-color: rgba(255,235,59,0.12); }
.threat-row__icon--low    { border-color: #00e676; background-color: rgba(0,230,118,0.12); }

.threat-row__icon-glyph {
    font-size: 12px;
    -unity-font-style: bold;
}

.threat-row__info { flex-grow: 1; }

.threat-row__name {
    font-size: 11px;
    -unity-font-style: bold;
    color: #e0e8f0;
    margin-bottom: 2px;
}

.threat-row__sub {
    font-size: 9px;
    color: #6b8099;
}

.threat-row__badge-col {
    flex-direction: column;
    align-items: flex-end;
}

.threat-row__level-label {
    font-size: 8px;
    color: #6b8099;
    margin-bottom: 2px;
}

.threat-row__level {
    font-size: 10px;
    -unity-font-style: bold;
}

.threat-row__level--high   { color: #f44336; }
.threat-row__level--medium { color: #ffeb3b; }
.threat-row__level--low    { color: #00e676; }

/* ══════════════════════════════════════════════════════════════ */
/*  INTELLIGENCE ASSESSMENT                                       */
/* ══════════════════════════════════════════════════════════════ */
.intel-header {
    flex-direction: row;
    align-items: center;
    margin-bottom: 10px;
}

.intel-icon {
    width: 36px;
    height: 36px;
    border-radius: 18px;
    align-items: center;
    justify-content: center;
    margin-right: 10px;
    flex-shrink: 0;
}

.intel-icon--danger {
    background-color: rgba(244,67,54,0.15);
    border-width: 2px;
    border-color: #f44336;
}

.intel-icon__glyph {
    font-size: 18px;
    -unity-font-style: bold;
    color: #f44336;
}

.intel-region-name {
    font-size: 13px;
    -unity-font-style: bold;
    color: #f44336;
    letter-spacing: 1px;
}

.intel-grid {
    display: flex;
    flex-direction: column;
}

.intel-row {
    flex-direction: row;
    justify-content: space-between;
    margin-bottom: 5px;
}

.intel-key {
    font-size: 10px;
    color: #6b8099;
    flex-grow: 1;
}

.intel-val {
    font-size: 10px;
    color: #e0e8f0;
    -unity-font-style: bold;
}

.intel-val--danger { color: #f44336; }

/* ══════════════════════════════════════════════════════════════ */
/*  ACTIVE OPERATIONS — CARDS                                     */
/* ══════════════════════════════════════════════════════════════ */
.ops-list {
    flex-grow: 1;
}

.op-card {
    flex-direction: row;
    background-color: #111820;
    border-width: 1px;
    border-left-width: 4px;
    border-color: #1e2d3d;
    border-radius: 3px;
    padding: 10px;
    align-items: center;
    margin-bottom: 8px;
}

.op-card--counter-terror { border-left-color: #f44336; }
.op-card--peacekeeping   { border-left-color: #ffeb3b; }
.op-card--border-patrol  { border-left-color: #00e676; }
.op-card--air-recon      { border-left-color: #00a8ff; }
.op-card--maritime       { border-left-color: #00bcd4; }
.op-card--hvt            { border-left-color: #ab47bc; }

.op-card__icon {
    width: 44px;
    height: 44px;
    border-radius: 22px;
    border-width: 2px;
    border-color: #1e2d3d;
    background-color: #0d1117;
    align-items: center;
    justify-content: center;
    margin-right: 12px;
    flex-shrink: 0;
}

.op-card__icon-glyph {
    font-size: 18px;
    color: #e0e8f0;
}

.op-card__info {
    flex-grow: 1;
}

.op-card__name {
    font-size: 12px;
    -unity-font-style: bold;
    color: #e0e8f0;
    margin-bottom: 2px;
    letter-spacing: 0.5px;
}

.op-card__meta {
    font-size: 10px;
    color: #6b8099;
    margin-bottom: 6px;
}

.op-card__objective {
    font-size: 10px;
    color: #8faabb;
    white-space: normal;
}

.op-card__stats {
    flex-direction: column;
    align-items: center;
    margin-left: 12px;
    flex-shrink: 0;
    width: 64px;
}

.op-card__stat-label {
    font-size: 8px;
    color: #6b8099;
    margin-bottom: 2px;
    letter-spacing: 0.5px;
}

.op-card__stat-value {
    font-size: 18px;
    -unity-font-style: bold;
    color: #e0e8f0;
}

.op-card__progress-bar {
    width: 64px;
    height: 6px;
    background-color: #1e2d3d;
    border-radius: 3px;
    margin-top: 4px;
}

.op-card__progress-fill {
    height: 6px;
    border-radius: 3px;
}

.op-card__progress-fill--good   { background-color: #00e676; }
.op-card__progress-fill--medium { background-color: #ffeb3b; }
.op-card__progress-fill--low    { background-color: #f44336; }

.op-card__eta {
    flex-direction: column;
    align-items: center;
    margin-left: 12px;
    flex-shrink: 0;
    width: 70px;
}

.op-card__eta-label {
    font-size: 8px;
    color: #6b8099;
    margin-bottom: 2px;
}

.op-card__eta-value {
    font-size: 11px;
    -unity-font-style: bold;
    color: #ffeb3b;
    -unity-text-align: middle-center;
}

.op-card__view-btn {
    margin-left: 12px;
    background-color: #1e2d3d;
    color: #e0e8f0;
    border-width: 1px;
    border-color: #2a3f58;
    border-radius: 3px;
    padding: 6px 12px;
    font-size: 10px;
    -unity-font-style: bold;
    cursor: link;
    flex-shrink: 0;
}

.op-card__view-btn:hover { background-color: #2a3f58; }

.ops-btn-launch-new {
    border-width: 1px;
    border-color: #2a3f58;
    border-radius: 4px;
    background-color: transparent;
    padding: 14px;
    align-items: center;
    justify-content: center;
    margin-top: 8px;
    cursor: link;
}

.ops-btn-launch-new:hover { background-color: rgba(0,168,255,0.06); border-color: #00a8ff; }

.ops-btn-launch-new__label {
    font-size: 12px;
    color: #00a8ff;
    -unity-font-style: bold;
    letter-spacing: 1px;
}

/* ══════════════════════════════════════════════════════════════ */
/*  OPERATION FEED                                                */
/* ══════════════════════════════════════════════════════════════ */
.feed-list {
    flex-grow: 1;
}

.feed-item {
    flex-direction: row;
    align-items: flex-start;
    padding: 6px 0;
    border-bottom-width: 1px;
    border-bottom-color: #141e29;
}

.feed-item__dot {
    width: 8px;
    height: 8px;
    border-radius: 4px;
    margin-top: 3px;
    margin-right: 10px;
    flex-shrink: 0;
}

.feed-item__dot--green  { background-color: #00e676; }
.feed-item__dot--yellow { background-color: #ffeb3b; }
.feed-item__dot--red    { background-color: #f44336; }

.feed-item__date-col {
    flex-direction: column;
    width: 68px;
    flex-shrink: 0;
    margin-right: 8px;
}

.feed-item__day {
    font-size: 9px;
    -unity-font-style: bold;
    color: #e0e8f0;
}

.feed-item__date {
    font-size: 9px;
    color: #6b8099;
}

.feed-item__text {
    font-size: 10px;
    color: #8faabb;
    flex-grow: 1;
    white-space: normal;
}

/* ══════════════════════════════════════════════════════════════ */
/*  LAUNCH NEW OPERATION — OP TYPE CARDS                          */
/* ══════════════════════════════════════════════════════════════ */
.op-types-grid {
    flex-direction: row;
    flex-wrap: wrap;
    align-content: flex-start;
}

.op-type-card {
    width: 130px;
    background-color: #111820;
    border-width: 1px;
    border-color: #1e2d3d;
    border-radius: 4px;
    padding: 10px;
    flex-direction: column;
    margin-right: 8px;
    margin-bottom: 8px;
}

.op-type-card__header {
    flex-direction: row;
    align-items: center;
    margin-bottom: 6px;
}

.op-type-card__icon {
    width: 18px;
    height: 18px;
    margin-right: 6px;
    flex-shrink: 0;
    font-size: 14px;
}

.op-type-card__name {
    font-size: 10px;
    -unity-font-style: bold;
    color: #e0e8f0;
    letter-spacing: 0.5px;
}

.op-type-card__desc {
    font-size: 9px;
    color: #6b8099;
    white-space: normal;
    flex-grow: 1;
    margin-bottom: 6px;
}

.op-type-card__req-label {
    font-size: 8px;
    color: #6b8099;
    margin-bottom: 1px;
}

.op-type-card__req-val {
    font-size: 9px;
    color: #8faabb;
    margin-bottom: 4px;
}

.op-type-card__cost-label {
    font-size: 8px;
    color: #6b8099;
    margin-bottom: 1px;
}

.op-type-card__cost-val {
    font-size: 9px;
    color: #e0e8f0;
    margin-bottom: 8px;
}

.op-type-card__select-btn {
    border-width: 1px;
    border-radius: 3px;
    padding: 4px;
    font-size: 9px;
    -unity-font-style: bold;
    -unity-text-align: middle-center;
    cursor: link;
    background-color: transparent;
}

/* per-type accent colours */
.op-type-card--counter-terror .op-type-card__select-btn {
    border-color: #f44336; color: #f44336;
}
.op-type-card--border-patrol .op-type-card__select-btn {
    border-color: #00e676; color: #00e676;
}
.op-type-card--peacekeeping .op-type-card__select-btn {
    border-color: #00a8ff; color: #00a8ff;
}
.op-type-card--air-recon .op-type-card__select-btn {
    border-color: #00a8ff; color: #00a8ff;
}
.op-type-card--maritime .op-type-card__select-btn {
    border-color: #00bcd4; color: #00bcd4;
}
.op-type-card--hvt .op-type-card__select-btn {
    border-color: #ab47bc; color: #ab47bc;
}

.op-type-card__select-btn:hover {
    background-color: rgba(255,255,255,0.06);
}

/* ══════════════════════════════════════════════════════════════ */
/*  FORCE READINESS                                               */
/* ══════════════════════════════════════════════════════════════ */
.readiness-list {
    flex-direction: column;
}

.readiness-row {
    flex-direction: row;
    align-items: center;
    margin-bottom: 6px;
}

.readiness-row__icon {
    font-size: 14px;
    width: 20px;
    color: #6b8099;
    margin-right: 8px;
    flex-shrink: 0;
}

.readiness-row__name {
    width: 72px;
    font-size: 10px;
    -unity-font-style: bold;
    color: #e0e8f0;
    flex-shrink: 0;
}

.readiness-row__bar-bg {
    flex-grow: 1;
    height: 6px;
    background-color: #1e2d3d;
    border-radius: 3px;
    margin: 0 8px;
}

.readiness-row__bar-fill {
    height: 6px;
    border-radius: 3px;
    background-color: #00e676;
}

.readiness-row__pct {
    font-size: 10px;
    -unity-font-style: bold;
    color: #00e676;
    width: 32px;
    -unity-text-align: middle-right;
    flex-shrink: 0;
}
"""

with open(r"C:\Users\Tobi\StateCraft\Assets\UI\Styles\OperationsTab.uss", "w", encoding="utf-8") as f:
    f.write(uss_content)


# 2. Update MilitaryDashboard.uxml
uxml_path = r"C:\Users\Tobi\StateCraft\Assets\UI\Layouts\MilitaryDashboard.uxml"
with open(uxml_path, "r", encoding="utf-8") as f:
    uxml = f.read()

# Add the new stylesheet right after the Global.uss
if "OperationsTab.uss" not in uxml:
    uxml = uxml.replace('<Style src="project://database/Assets/UI/Styles/Global.uss" />', '<Style src="project://database/Assets/UI/Styles/Global.uss" />\n    <Style src="project://database/Assets/UI/Styles/OperationsTab.uss" />')

new_panel_operations = """            <ui:VisualElement name="PanelOperations" style="display: none; height: 100%;">
                <ui:VisualElement name="body" class="ops-body">

                    <!-- ─── LEFT COLUMN ─────────────── -->
                    <ui:VisualElement name="col-left" class="ops-col ops-col--left">

                        <!-- Threat Status -->
                        <ui:VisualElement name="panel-threat" class="ops-panel">
                            <ui:Label text="THREAT STATUS" class="ops-panel__title"/>
                            <ui:ScrollView name="threat-list" class="threat-list">
                                <!-- Items injected by C# -->
                            </ui:ScrollView>
                            <ui:Button name="btn-view-regions" text="VIEW ALL REGIONS ›" class="ops-btn-link"/>
                        </ui:VisualElement>

                        <!-- Intelligence Assessment -->
                        <ui:VisualElement name="panel-intel" class="ops-panel ops-panel--intel">
                            <ui:Label text="INTELLIGENCE ASSESSMENT" class="ops-panel__title"/>
                            <ui:VisualElement class="intel-header">
                                <ui:VisualElement class="intel-icon intel-icon--danger">
                                    <ui:Label text="!" class="intel-icon__glyph"/>
                                </ui:VisualElement>
                                <ui:Label name="lbl-intel-region" text="AWAITING INTEL" class="intel-region-name"/>
                            </ui:VisualElement>
                            <ui:VisualElement class="intel-grid">
                                <ui:VisualElement class="intel-row"><ui:Label text="Threat Level:" class="intel-key"/><ui:Label name="lbl-threat-level" text="-" class="intel-val intel-val--danger"/></ui:VisualElement>
                                <ui:VisualElement class="intel-row"><ui:Label text="Enemy Strength:" class="intel-key"/><ui:Label name="lbl-enemy-strength" text="-" class="intel-val"/></ui:VisualElement>
                                <ui:VisualElement class="intel-row"><ui:Label text="Confidence:" class="intel-key"/><ui:Label name="lbl-confidence" text="-" class="intel-val"/></ui:VisualElement>
                                <ui:VisualElement class="intel-row"><ui:Label text="Intel Source:" class="intel-key"/><ui:Label name="lbl-intel-source" text="-" class="intel-val"/></ui:VisualElement>
                                <ui:VisualElement class="intel-row"><ui:Label text="Last Updated:" class="intel-key"/><ui:Label name="lbl-last-updated" text="-" class="intel-val"/></ui:VisualElement>
                            </ui:VisualElement>
                            <ui:Button name="btn-view-intel" text="VIEW INTEL REPORT ›" class="ops-btn-link"/>
                        </ui:VisualElement>

                    </ui:VisualElement>

                    <!-- ─── CENTER COLUMN ────────────── -->
                    <ui:VisualElement name="col-center" class="ops-col ops-col--center">

                        <!-- Active Operations -->
                        <ui:VisualElement name="panel-active-ops" class="ops-panel ops-panel--stretch">
                            <ui:VisualElement class="ops-panel-header-row">
                                <ui:Label text="ACTIVE OPERATIONS" class="ops-panel__title"/>
                                <ui:Button name="btn-view-all-ops" text="VIEW ALL OPERATIONS ›" class="ops-btn-link"/>
                            </ui:VisualElement>

                            <ui:ScrollView name="ops-list" class="ops-list">
                                <!-- Operation cards injected by C# -->
                            </ui:ScrollView>

                            <ui:Button name="btn-launch-new" class="ops-btn-launch-new">
                                <ui:Label text="＋  LAUNCH NEW OPERATION" class="ops-btn-launch-new__label"/>
                            </ui:Button>
                        </ui:VisualElement>

                        <!-- Operation Feed -->
                        <ui:VisualElement name="panel-feed" class="ops-panel ops-panel--feed">
                            <ui:VisualElement class="ops-panel-header-row">
                                <ui:Label text="OPERATION FEED" class="ops-panel__title"/>
                                <ui:Button name="btn-view-feed" text="VIEW FULL FEED ›" class="ops-btn-link"/>
                            </ui:VisualElement>
                            <ui:ScrollView name="feed-list" class="feed-list">
                                <!-- Feed items injected by C# -->
                            </ui:ScrollView>
                        </ui:VisualElement>

                    </ui:VisualElement>

                    <!-- ─── RIGHT COLUMN ─────────────── -->
                    <ui:VisualElement name="col-right" class="ops-col ops-col--right">

                        <!-- Launch New Operation -->
                        <ui:VisualElement name="panel-launch" class="ops-panel">
                            <ui:Label text="LAUNCH NEW OPERATION" class="ops-panel__title"/>
                            <ui:VisualElement name="op-types-grid" class="op-types-grid">
                                <!-- Op type cards injected by C# -->
                            </ui:VisualElement>
                            <ui:Button name="btn-view-op-types" text="VIEW ALL OPERATION TYPES ›" class="ops-btn-link"/>
                        </ui:VisualElement>

                        <!-- Force Readiness -->
                        <ui:VisualElement name="panel-readiness" class="ops-panel">
                            <ui:Label text="FORCE READINESS" class="ops-panel__title"/>
                            <ui:VisualElement name="readiness-list" class="readiness-list">
                                <!-- Readiness rows injected by C# -->
                            </ui:VisualElement>
                            <ui:Button name="btn-view-force" text="VIEW FORCE DETAILS ›" class="ops-btn-link"/>
                        </ui:VisualElement>

                    </ui:VisualElement>

                </ui:VisualElement>
            </ui:VisualElement>"""

# Replace in uxml using regex
uxml = re.sub(r'<ui:VisualElement name="PanelOperations".*?</ui:VisualElement>\s*<!-- 4. PROJECTS TAB -->', new_panel_operations + '\n            <!-- 4. PROJECTS TAB -->', uxml, flags=re.DOTALL)

with open(uxml_path, "w", encoding="utf-8") as f:
    f.write(uxml)
