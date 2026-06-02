using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WoodburySpectatorSync.Coop
{
    internal static class SceneSyncRegistries
    {
        public static GenericNpcRegistry CreatePizzeriaNpcRegistry(ManualLogSource logger, Action<string> sessionLogWrite, string side)
        {
            return new GenericNpcRegistry(logger, sessionLogWrite, side, "Pizzeria", new[]
            {
                new GenericNpcEntry("Pizzeria/Mike/Main", "MikePizzeria", "MikePizzeria"),
                new GenericNpcEntry("Pizzeria/Hiker/Main", "PizzeriaHiker", "PizzeriaHiker"),
                new GenericNpcEntry("Pizzeria/Hobo/Main", "Hobo", "Hobo"),
                new GenericNpcEntry("Pizzeria/Chef/Main", "Chef", "Chef"),
                new GenericNpcEntry("Pizzeria/NPC/FoldingGuy", "PizzeriaFoldingGuy", "PizzeriaFoldingGuy"),
                new GenericNpcEntry("Pizzeria/NPC/Main", "PizzeriaNPC", "PizzeriaNPC", multiple: true),
                new GenericNpcEntry("Pizzeria/NPC/Other", "PizzeriaNPCOther", "PizzeriaNPCOther", multiple: true)
            });
        }

        public static GenericNpcRegistry CreateRoadTripNpcRegistry(ManualLogSource logger, Action<string> sessionLogWrite, string side)
        {
            return new GenericNpcRegistry(logger, sessionLogWrite, side, "RoadTrip", new[]
            {
                new GenericNpcEntry("RoadTrip/Mike/InCar", "MikeInCar", "MikeInCar"),
                new GenericNpcEntry("RoadTrip/Vehicle/MikeTruckLoop", "MikeTruckInLoopScene", "MikeTruckInLoopScene"),
                new GenericNpcEntry("RoadTrip/Vehicle/MikeTruckNora", "MikeTruckGoingToNora", "MikeTruckGoingToNora"),
                new GenericNpcEntry("RoadTrip/Vehicle/MikeTruckParking", "MikeTruckInParking", "MikeTruckInParking"),
                new GenericNpcEntry("RoadTrip/Deer/Main", "RedDeer", "RedDeer")
            });
        }

        public static GenericNpcRegistry CreateOfficeNpcRegistry(ManualLogSource logger, Action<string> sessionLogWrite, string side)
        {
            return new GenericNpcRegistry(logger, sessionLogWrite, side, "Office", new[]
            {
                new GenericNpcEntry("Office/Janitor/Main", "OfficeJanitor", "OfficeJanitor"),
                new GenericNpcEntry("Office/Worker/Main", "OfficeWorker", "OfficeWorker", multiple: true)
            });
        }

        public static GenericNpcRegistry CreateParkingLotNpcRegistry(ManualLogSource logger, Action<string> sessionLogWrite, string side)
        {
            return new GenericNpcRegistry(logger, sessionLogWrite, side, "Parking", new[]
            {
                new GenericNpcEntry("ParkingLot/Stranger/Elevator", "ElevatorStranger", "ElevatorStranger"),
                new GenericNpcEntry("ParkingLot/Mike/Main", "MikeParkingLot", "MikeParkingLot"),
                new GenericNpcEntry("ParkingLot/Cop/Walking", "WalkingCop", "WalkingCopController")
            });
        }
    }

    internal static class SceneDiscoveryManifest
    {
        public static void LogIfDiscoveryOnlyScene(string side, string sceneName, ManualLogSource logger, Action<string> sessionLogWrite)
        {
            if (!IsDiscoveryOnlyScene(sceneName))
            {
                return;
            }

            var managers = CountLoadedComponentsByName("GameManager", exact: false);
            var npcs = CountLoadedComponentsByName("NPC", exact: false) +
                       CountLoadedComponentsByName("Mike", exact: false) +
                       CountLoadedComponentsByName("Hiker", exact: false);
            var doors = CountLoadedComponentsByName("Door", exact: false);
            var holdables = CountLoadedComponentsByName("Holdable", exact: true);
            var ui = CountLoadedComponentsByName("UI", exact: false);

            Log("SceneManifest " + side +
                " scene=" + (string.IsNullOrEmpty(sceneName) ? "-" : sceneName) +
                " managers=" + managers +
                " npcLike=" + npcs +
                " doors=" + doors +
                " holdables=" + holdables +
                " ui=" + ui +
                " mode=discovery-only", logger, sessionLogWrite);
        }

        private static bool IsDiscoveryOnlyScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            return sceneName.IndexOf("Office", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   sceneName.IndexOf("Parking", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   sceneName.IndexOf("Lot", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int CountLoadedComponentsByName(string text, bool exact)
        {
            var count = 0;
            var behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || behaviour.gameObject == null) continue;
                var scene = behaviour.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded) continue;
                var name = behaviour.GetType().Name;
                if (exact
                        ? string.Equals(name, text, StringComparison.Ordinal)
                        : name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    count++;
                }
            }
            return count;
        }

        private static void Log(string message, ManualLogSource logger, Action<string> sessionLogWrite)
        {
            logger?.LogInfo(message);
            sessionLogWrite?.Invoke(message);
        }
    }
}
