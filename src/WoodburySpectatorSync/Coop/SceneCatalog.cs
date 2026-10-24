using System;

namespace WoodburySpectatorSync.Coop
{
    // Exact shipped scene names. Unknown builds stay visible in diagnostics.
    internal static class SceneCatalog
    {
        public static readonly string[] GameplayScenes = {
            "OfficeLayout", "ParkingLotScene", "RoadTripLoop", "Pizzeria", "CabinScene", "CabinSceneDark"
        };

        public static string AdapterFor(string scene)
        {
            switch (scene)
            {
                case "OfficeLayout": return "OfficeLayout";
                case "Typing Shooter Final scene": return "TypingShooter";
                case "CreditsScene": return "Credits";
                case "ParkingLotScene": return "ParkingLot";
                case "RoadTripLoop": return "RoadTrip";
                case "Pizzeria": return "Pizzeria";
                case "CabinScene":
                case "CabinSceneDark":
                case "CabinDarkScene": return "Cabin";
                default: return string.Empty;
            }
        }

        public static bool Matches(string adapter, string scene)
        {
            return !string.IsNullOrEmpty(adapter) && string.Equals(AdapterFor(scene), adapter, StringComparison.Ordinal);
        }

        public static bool HasOfficeWorld(string scene) => scene == "OfficeLayout" || scene == "Typing Shooter Final scene";
        public static bool HasPlayerAvatar(string scene) => Array.IndexOf(GameplayScenes, scene) >= 0 || HasOfficeWorld(scene);
    }
}
