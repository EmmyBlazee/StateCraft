using UnityEngine;

namespace StateCraft.Core
{
    /// <summary>
    /// Holds cross-scene data configured in the Splash/Main Menu scene.
    /// This allows SampleScene to initialize correctly without prompting the player again.
    /// </summary>
    public static class GameContext
    {
        public static string PlayerName { get; set; } = "Leader";
        public static string SelectedCountryName { get; set; } = "";
        
        // Flag indicating if we are returning to the main menu. If false, we might want to play the intro video.
        public static bool ReturningToMainMenu { get; set; } = false;
        
        // Flag indicating if the user clicked "Load Game" from the main menu
        public static bool ShouldLoadSave { get; set; } = false;
        
        public static void Reset()
        {
            PlayerName = "Leader";
            SelectedCountryName = "";
            ShouldLoadSave = false;
        }
    }
}
