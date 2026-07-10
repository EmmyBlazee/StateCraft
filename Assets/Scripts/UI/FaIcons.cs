namespace StateCraft.UI
{
    /// <summary>
    /// Font Awesome 6 Free (Solid) codepoints for use in UI Toolkit labels
    /// that carry the .fa-icon USS class.
    ///
    /// Usage:
    ///   var lbl = new Label(FaIcons.Shield);
    ///   lbl.AddToClassList("fa-icon");
    ///
    /// The .fa-icon class in Global.uss sets -unity-font to fa-solid-900.ttf.
    /// </summary>
    public static class FaIcons
    {
        // ── Economy ──────────────────────────────────────────────────────────
        public const string ChartLine        = "\uf201"; // fa-chart-line
        public const string ChartBar         = "\uf080"; // fa-chart-bar
        public const string ChartPie         = "\uf200"; // fa-chart-pie
        public const string TrendUp          = "\uf062"; // fa-arrow-up (growth)
        public const string TrendDown        = "\uf063"; // fa-arrow-down (decline)
        public const string Coins            = "\uf51e"; // fa-coins
        public const string DollarSign       = "\uf155"; // fa-dollar-sign
        public const string HandHoldingUsd   = "\uf4c0"; // fa-hand-holding-dollar
        public const string CreditCard       = "\uf09d"; // fa-credit-card
        public const string Receipt          = "\uf543"; // fa-receipt
        public const string Landmark         = "\uf66f"; // fa-landmark (budget/treasury)
        public const string Briefcase        = "\uf0b1"; // fa-briefcase

        // ── Sectors ───────────────────────────────────────────────────────
        public const string OilCan           = "\uf613"; // fa-oil-can
        public const string Gem              = "\uf3a5"; // fa-gem  (mining)
        public const string Wheat            = "\uf72d"; // fa-wheat-awn → use Seedling instead
        public const string Seedling         = "\uf4d8"; // fa-seedling (agriculture)
        public const string IndustryAlt      = "\uf275"; // fa-industry
        public const string Microchip        = "\uf2db"; // fa-microchip (tech)

        // ── Budget ministries ─────────────────────────────────────────────
        public const string Shield           = "\uf132"; // fa-shield (military)
        public const string UserSecret       = "\uf21b"; // fa-user-secret (intel)
        public const string GraduationCap    = "\uf19d"; // fa-graduation-cap
        public const string HeartPulse       = "\uf21e"; // fa-heartbeat / health
        public const string Road             = "\uf018"; // fa-road (infrastructure)
        public const string Tractor          = "\uf722"; // fa-tractor (agriculture)
        public const string Bolt             = "\uf0e7"; // fa-bolt (energy)
        public const string HandsHelping     = "\uf4c4"; // fa-hand-holding-heart (welfare)

        // ── Projects / Construction ───────────────────────────────────────
        public const string HardHat          = "\uf807"; // fa-hard-hat
        public const string Building         = "\uf1ad"; // fa-building
        public const string Hammer           = "\uf6e3"; // fa-hammer
        public const string Wrench           = "\uf0ad"; // fa-wrench
        public const string Clock            = "\uf017"; // fa-clock
        public const string CalendarCheck    = "\uf274"; // fa-calendar-check

        // ── Alerts / Status ───────────────────────────────────────────────
        public const string TriangleExclaim  = "\uf071"; // fa-triangle-exclamation
        public const string CircleCheck      = "\uf058"; // fa-circle-check
        public const string CircleInfo       = "\uf05a"; // fa-circle-info
        public const string CircleXMark      = "\uf057"; // fa-circle-xmark
        public const string BullsEye         = "\uf140"; // fa-bullseye

        // ── Report row icons ──────────────────────────────────────────────
        public const string ArrowTrendUp     = "\ue098"; // fa-arrow-trend-up
        public const string ArrowTrendDown   = "\ue097"; // fa-arrow-trend-down
        public const string Globe            = "\uf0ac"; // fa-globe (market)
        public const string Scroll           = "\uf70e"; // fa-scroll (policy)
        public const string Fire             = "\uf06d"; // fa-fire (alert)

        // ── Military / Bases ──────────────────────────────────────────────
        public const string Plane            = "\uf072"; // fa-plane
        public const string Anchor           = "\uf13d"; // fa-anchor
        public const string Tower            = "\uf519"; // fa-broadcast-tower
        public const string Tint             = "\uf043"; // fa-tint

        // ── Helper: icon for each budget ministry, in order ───────────────
        public static readonly string[] BudgetMinistryIcons =
        {
            Shield,         // Military
            UserSecret,     // Intelligence
            GraduationCap,  // Education
            HeartPulse,     // Health
            Road,           // Infrastructure
            Seedling,       // Agriculture
            Bolt,           // Energy
            HandsHelping    // Welfare
        };

        // ── Helper: icon per sector (matches EconomyTabContent.Industries order) ───
        public static readonly string[] SectorIcons =
        {
            OilCan,      // OIL & GAS
            Gem,         // MINING
            Seedling,    // AGRICULTURE
            IndustryAlt, // MANUFACTURING
            Microchip    // TECH & FINTECH
        };
    }
}
