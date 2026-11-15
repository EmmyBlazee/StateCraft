using UnityEngine;

namespace StateCraft.Core
{
    public static class GameContext
    {
        public static string PlayerName { get; set; } = "Leader";
        public static string SelectedCountryName { get; set; } = "";
        
        public static bool ReturningToMainMenu { get; set; } = false;
        
        public static bool ShouldLoadSave { get; set; } = false;
        
        public static void Reset()
        {
            PlayerName = "Leader";
            SelectedCountryName = "";
            ShouldLoadSave = false;
        }
    }
}
