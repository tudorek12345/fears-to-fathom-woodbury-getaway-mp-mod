using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal static class OfficeSceneStateSync
    {
        public const string KeyPrefix = "SceneState.";

        private const string GamePrefix = KeyPrefix + "Game.";
        private const string YellowIntroPrefix = KeyPrefix + "YellowIntro.";
        private const string PlayerPrefix = KeyPrefix + "Player.";
        private const string TablePrefix = KeyPrefix + "Table.";
        private const string ComputerPrefix = KeyPrefix + "Computer.";
        private const string CoffeePrefix = KeyPrefix + "Coffee.";
        private const string PhonePrefix = KeyPrefix + "Phone.";
        private const string TypeMasterPrefix = KeyPrefix + "TypeMaster.";
        private const string TypingShooterPrefix = KeyPrefix + "TypingShooter.";
        private const string RadioPrefix = KeyPrefix + "Radio.";
        private const string FridgePrefix = KeyPrefix + "Fridge.";
        private const string MicrowavePrefix = KeyPrefix + "Microwave.";
        private const string ToiletPrefix = KeyPrefix + "Toilet.";
        private const string JanitorPrefix = KeyPrefix + "Janitor.";
        private const string WorkerPrefix = KeyPrefix + "Worker.";
        private const string WorkerMonitorPrefix = KeyPrefix + "WorkerMonitor.";
        private const string UiPrefix = KeyPrefix + "UI.";
        private const string VendingPrefix = KeyPrefix + "Vending.";
        private const string BoundsSubPrefix = KeyPrefix + "BoundsSub.";
        private const string TriggerSubPrefix = KeyPrefix + "TriggerSub.";
        private const string DisplaySubPrefix = KeyPrefix + "DisplaySub.";
        private const string GenericTriggerPrefix = KeyPrefix + "GenericTrigger.";
        private const string EventTriggerPrefix = KeyPrefix + "EventTrigger.";
        private const BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        private static long _nextSuppressLogMs;

        public static int EmitHostFlags(string fullPrefix, Action<string, int> emit)
        {
            if (emit == null) return 0;

            var hash = 41;
            EmitGame(fullPrefix + GamePrefix, UnityEngine.Object.FindObjectOfType<OfficeLayoutGameManager>(), emit, ref hash);
            EmitYellowIntro(fullPrefix + YellowIntroPrefix, UnityEngine.Object.FindObjectOfType<YellowIntroManager>(), emit, ref hash);
            EmitPlayer(fullPrefix + PlayerPrefix, UnityEngine.Object.FindObjectOfType<OfficeLayoutPlayerController>(), emit, ref hash);
            EmitTable(fullPrefix + TablePrefix, UnityEngine.Object.FindObjectOfType<TableManager>(), emit, ref hash);
            EmitComputer(fullPrefix + ComputerPrefix, UnityEngine.Object.FindObjectOfType<ComputerManager>(), emit, ref hash);
            EmitCoffee(fullPrefix + CoffeePrefix, UnityEngine.Object.FindObjectOfType<CoffeeMachineManager>(), emit, ref hash);
            EmitPhone(fullPrefix + PhonePrefix, UnityEngine.Object.FindObjectOfType<OfficePhone>(), emit, ref hash);
            EmitTypeMaster(fullPrefix + TypeMasterPrefix, UnityEngine.Object.FindObjectOfType<TypeMasterManager>(), emit, ref hash);
            EmitTypingShooter(fullPrefix + TypingShooterPrefix, UnityEngine.Object.FindObjectOfType<TypingShooterComputerManager>(), emit, ref hash);
            EmitRadios(fullPrefix + RadioPrefix, UnityEngine.Object.FindObjectsOfType<OfficeRadio>(), emit, ref hash);
            EmitFridges(fullPrefix + FridgePrefix, UnityEngine.Object.FindObjectsOfType<OfficeFridge>(), emit, ref hash);
            EmitMicrowaves(fullPrefix + MicrowavePrefix, UnityEngine.Object.FindObjectsOfType<OfficeMicrowave>(), emit, ref hash);
            EmitToilets(fullPrefix + ToiletPrefix, UnityEngine.Object.FindObjectsOfType<OfficeToiletManager>(), emit, ref hash);
            EmitJanitor(fullPrefix + JanitorPrefix, UnityEngine.Object.FindObjectOfType<OfficeJanitor>(), emit, ref hash);
            EmitWorkers(fullPrefix + WorkerPrefix, UnityEngine.Object.FindObjectsOfType<OfficeWorker>(), emit, ref hash);
            EmitWorkerMonitors(fullPrefix + WorkerMonitorPrefix, UnityEngine.Object.FindObjectsOfType<OfficeWorkerMonitor>(), emit, ref hash);
            EmitUi(fullPrefix + UiPrefix, UnityEngine.Object.FindObjectOfType<OfficeLayoutUIManager>(), emit, ref hash);
            hash = hash * 31 + VendingMachineSceneSync.EmitHostFlags(fullPrefix + VendingPrefix, emit);
            EmitBoundsSubs(fullPrefix + BoundsSubPrefix, UnityEngine.Object.FindObjectsOfType<OnTriggerOfficeBoundsSub>(), emit, ref hash);
            EmitTriggerSubs(fullPrefix + TriggerSubPrefix, UnityEngine.Object.FindObjectsOfType<OnTriggerSub>(), emit, ref hash);
            EmitDisplaySubs(fullPrefix + DisplaySubPrefix, UnityEngine.Object.FindObjectsOfType<OnTriggerDisplaySub>(), emit, ref hash);
            EmitGenericTriggers(fullPrefix + GenericTriggerPrefix, UnityEngine.Object.FindObjectsOfType<OnTrigger>(), emit, ref hash);
            EmitEventTriggers(fullPrefix + EventTriggerPrefix, UnityEngine.Object.FindObjectsOfType<TriggerEventInvoker>(), emit, ref hash);
            return hash;
        }

        public static void CollectSyncedTransforms(List<Transform> transforms)
        {
            if (transforms == null) return;

            var fridges = UnityEngine.Object.FindObjectsOfType<OfficeFridge>();
            SortByPath(fridges);
            for (var i = 0; i < fridges.Length; i++)
            {
                if (fridges[i] != null) transforms.Add(fridges[i].transform);
            }

            var microwaves = UnityEngine.Object.FindObjectsOfType<OfficeMicrowave>();
            SortByPath(microwaves);
            for (var i = 0; i < microwaves.Length; i++)
            {
                var door = GetFieldValue<Transform>(microwaves[i], "doorTransform");
                if (door != null) transforms.Add(door);
            }

            var toiletLids = UnityEngine.Object.FindObjectsOfType<OfficeToiletLid>();
            SortByPath(toiletLids);
            for (var i = 0; i < toiletLids.Length; i++)
            {
                if (toiletLids[i] != null) transforms.Add(toiletLids[i].transform);
            }

            var toiletSeats = UnityEngine.Object.FindObjectsOfType<OfficeToiletSeat>();
            SortByPath(toiletSeats);
            for (var i = 0; i < toiletSeats.Length; i++)
            {
                if (toiletSeats[i] != null) transforms.Add(toiletSeats[i].transform);
            }

            var janitor = UnityEngine.Object.FindObjectOfType<OfficeJanitor>();
            if (janitor != null) transforms.Add(janitor.transform);

            var workers = UnityEngine.Object.FindObjectsOfType<OfficeWorker>();
            SortByPath(workers);
            for (var i = 0; i < workers.Length; i++)
            {
                if (workers[i] != null) transforms.Add(workers[i].transform);
            }

            var monitors = UnityEngine.Object.FindObjectsOfType<OfficeWorkerMonitor>();
            SortByPath(monitors);
            for (var i = 0; i < monitors.Length; i++)
            {
                if (monitors[i] != null) transforms.Add(monitors[i].transform);
            }

            VendingMachineSceneSync.CollectSyncedTransforms(transforms);
        }

        public static bool TryApplyFlag(string fieldName, int value, ManualLogSource logger)
        {
            if (string.IsNullOrEmpty(fieldName) ||
                !fieldName.StartsWith(KeyPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (fieldName.StartsWith(GamePrefix, StringComparison.Ordinal))
            {
                return TryApplyGame(fieldName.Substring(GamePrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(YellowIntroPrefix, StringComparison.Ordinal))
            {
                return TryApplyYellowIntro(fieldName.Substring(YellowIntroPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(PlayerPrefix, StringComparison.Ordinal))
            {
                return TryApplyPlayer(fieldName.Substring(PlayerPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(TablePrefix, StringComparison.Ordinal))
            {
                return TryApplyTable(fieldName.Substring(TablePrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(ComputerPrefix, StringComparison.Ordinal))
            {
                return TryApplyComputer(fieldName.Substring(ComputerPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(CoffeePrefix, StringComparison.Ordinal))
            {
                return TryApplyCoffee(fieldName.Substring(CoffeePrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(PhonePrefix, StringComparison.Ordinal))
            {
                return TryApplyPhone(fieldName.Substring(PhonePrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(TypeMasterPrefix, StringComparison.Ordinal))
            {
                return TryApplyTypeMaster(fieldName.Substring(TypeMasterPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(TypingShooterPrefix, StringComparison.Ordinal))
            {
                return TryApplyTypingShooter(fieldName.Substring(TypingShooterPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(RadioPrefix, StringComparison.Ordinal))
            {
                return TryApplyRadio(fieldName.Substring(RadioPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(FridgePrefix, StringComparison.Ordinal))
            {
                return TryApplyFridge(fieldName.Substring(FridgePrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(MicrowavePrefix, StringComparison.Ordinal))
            {
                return TryApplyMicrowave(fieldName.Substring(MicrowavePrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(ToiletPrefix, StringComparison.Ordinal))
            {
                return TryApplyToilet(fieldName.Substring(ToiletPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(JanitorPrefix, StringComparison.Ordinal))
            {
                return TryApplyJanitor(fieldName.Substring(JanitorPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(WorkerPrefix, StringComparison.Ordinal))
            {
                return TryApplyWorker(fieldName.Substring(WorkerPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(WorkerMonitorPrefix, StringComparison.Ordinal))
            {
                return TryApplyWorkerMonitor(fieldName.Substring(WorkerMonitorPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(UiPrefix, StringComparison.Ordinal))
            {
                return TryApplyUi(fieldName.Substring(UiPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(VendingPrefix, StringComparison.Ordinal))
            {
                return VendingMachineSceneSync.TryApplyFlag(fieldName.Substring(VendingPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(BoundsSubPrefix, StringComparison.Ordinal))
            {
                return TryApplyBoundsSub(fieldName.Substring(BoundsSubPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(TriggerSubPrefix, StringComparison.Ordinal))
            {
                return TryApplyTriggerSub(fieldName.Substring(TriggerSubPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(DisplaySubPrefix, StringComparison.Ordinal))
            {
                return TryApplyDisplaySub(fieldName.Substring(DisplaySubPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(GenericTriggerPrefix, StringComparison.Ordinal))
            {
                return TryApplyGenericTrigger(fieldName.Substring(GenericTriggerPrefix.Length), value, logger);
            }

            if (fieldName.StartsWith(EventTriggerPrefix, StringComparison.Ordinal))
            {
                return TryApplyEventTrigger(fieldName.Substring(EventTriggerPrefix.Length), value, logger);
            }

            return true;
        }

        private static void EmitGame(string prefix, OfficeLayoutGameManager manager, Action<string, int> emit, ref int hash)
        {
            if (manager == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var player = GetFieldValue<OfficeLayoutPlayerController>(manager, "officeLayoutPlayerController");
            var firstPerson = GetFieldValue<FirstPersonController>(manager, "firstPersonController");
            var yellowIntro = GetFieldObject(manager, "yellowIntroManager");
            var yellowAudio = GetFieldValue<AudioSource>(manager, "yellowTextsAmbience");
            var mikeTruck = GetFieldObject(manager, "mikeTruckInLoopScene");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", manager.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "CurrentState", (int)manager.currentState, emit, ref hash);
            Emit(prefix + "CurrentPlayerState", (int)manager.currentPlayerState, emit, ref hash);
            Emit(prefix + "PlayerControllerEnabled", player != null && player.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "FirstPersonEnabled", firstPerson != null && firstPerson.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "FirstPersonActive", firstPerson != null && firstPerson.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "YellowIntroActive", IsObjectActive(yellowIntro) ? 1 : 0, emit, ref hash);
            Emit(prefix + "YellowAudio", yellowAudio != null && yellowAudio.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "MikeTruckLoopActive", IsObjectActive(mikeTruck) ? 1 : 0, emit, ref hash);
        }

        private static void EmitYellowIntro(string prefix, YellowIntroManager yellowIntro, Action<string, int> emit, ref int hash)
        {
            if (yellowIntro == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var texts = GetFieldValue<YellowTextInstance[]>(yellowIntro, "introTexts");
            var enabledMask = 0;
            if (texts != null)
            {
                for (var i = 0; i < texts.Length && i < 30; i++)
                {
                    if (IsYellowTextEnabled(texts[i])) enabledMask |= 1 << i;
                }
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", yellowIntro.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", yellowIntro.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "TimerMs", Mathf.RoundToInt(GetFieldValue<float>(yellowIntro, "timer") * 1000f), emit, ref hash);
            Emit(prefix + "CurrentTextIndex", GetFieldValue<int>(yellowIntro, "currentTextIndex"), emit, ref hash);
            Emit(prefix + "LastDisabledIndex", GetFieldValue<int>(yellowIntro, "lastDisabledIndex"), emit, ref hash);
            Emit(prefix + "TextCount", texts != null ? texts.Length : 0, emit, ref hash);
            Emit(prefix + "TextEnabledMask", enabledMask, emit, ref hash);
        }

        private static void EmitPlayer(string prefix, OfficeLayoutPlayerController player, Action<string, int> emit, ref int hash)
        {
            if (player == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var mainCamera = GetFieldValue<Camera>(player, "mainCamera");
            var firstPerson = GetFieldValue<FirstPersonController>(player, "firstPersonController");
            var lookHere = GetFieldValue<Transform>(player, "lookHere");
            var hallwaySub = GetFieldObject(player, "hallwayRestroomSubTrigger");
            var currentHolding = GetFieldValue<Holdable>(player, "currentHoldingObject");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", player.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", player.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "FirstPersonActive", firstPerson != null && firstPerson.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "FirstPersonEnabled", firstPerson != null && firstPerson.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "FirstPersonCanZoom", firstPerson != null && firstPerson.canZoomIn ? 1 : 0, emit, ref hash);
            Emit(prefix + "CanThrowItem", player.canThrowItem ? 1 : 0, emit, ref hash);
            Emit(prefix + "RecentlyThrown", player.playerhasRecentlyThrown ? 1 : 0, emit, ref hash);
            Emit(prefix + "MadeCoffee", GetFieldValue<bool>(player, "madeCoffee") ? 1 : 0, emit, ref hash);
            Emit(prefix + "CurrentSip", player.currentSip, emit, ref hash);
            Emit(prefix + "Sipping", player.sipping ? 1 : 0, emit, ref hash);
            Emit(prefix + "DelayTimerMs", Mathf.RoundToInt(player.delayTimer * 1000f), emit, ref hash);
            Emit(prefix + "CupRotX", Quantize(player.cupRotX), emit, ref hash);
            Emit(prefix + "SipAnim", player.handAnimator != null && player.handAnimator.GetBool(player.sipParam) ? 1 : 0, emit, ref hash);
            Emit(prefix + "SipAudio", player.sipAS != null && player.sipAS.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "HallwayRestroomSub", IsObjectActive(hallwaySub) ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhoneTrigger", IsObjectActive(player.phoneTrigger) ? 1 : 0, emit, ref hash);
            Emit(prefix + "LookTargetHash", lookHere != null ? StableStringHash(NetPath.GetPath(lookHere)) : 0, emit, ref hash);
            Emit(prefix + "HoldingHash", currentHolding != null ? StableStringHash(NetPath.GetPath(currentHolding.transform)) : 0, emit, ref hash);
            Emit(prefix + "CameraFov10", mainCamera != null ? Mathf.RoundToInt(mainCamera.fieldOfView * 10f) : 0, emit, ref hash);
        }

        private static void EmitTable(string prefix, TableManager table, Action<string, int> emit, ref int hash)
        {
            if (table == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", table.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "CoffeeDone", table.coffeeDone ? 1 : 0, emit, ref hash);
            Emit(prefix + "UsedRestroom", table.usedRestroom ? 1 : 0, emit, ref hash);
            Emit(prefix + "IsSitting", table.isSitting ? 1 : 0, emit, ref hash);
            Emit(prefix + "UsingComputer", table.usingComputer ? 1 : 0, emit, ref hash);
            Emit(prefix + "UsingTelephone", table.usingTelephone ? 1 : 0, emit, ref hash);
            Emit(prefix + "MikeCalled", table.mikeCalled ? 1 : 0, emit, ref hash);
            Emit(prefix + "RemindedCabin", table.remindedPlayerAboutBookingACabin ? 1 : 0, emit, ref hash);
            Emit(prefix + "TableCamera", IsObjectActive(GetFieldObject(table, "tableCameraHolder")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "DialogueCam", IsObjectActive(table.DialogueCam) ? 1 : 0, emit, ref hash);
            Emit(prefix + "MonitorTrigger", IsObjectActive(table.monitorTrigger) ? 1 : 0, emit, ref hash);
            Emit(prefix + "TelephoneTrigger", IsObjectActive(table.telephoneTrigger) ? 1 : 0, emit, ref hash);
        }

        private static void EmitUi(string prefix, OfficeLayoutUIManager ui, Action<string, int> emit, ref int hash)
        {
            if (ui == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", ui.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "InConversation", ui.inCoversation ? 1 : 0, emit, ref hash);
            Emit(prefix + "FadeCanvas", IsObjectActive(GetFieldObject(ui, "fadeCanvas")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "IntroCanvas", IsObjectActive(GetFieldObject(ui, "introCanvas")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "DialogCam", IsObjectActive(GetFieldObject(ui, "dialogCam")) ? 1 : 0, emit, ref hash);
            Emit(prefix + "PeeingParent", ui.peeingParent != null && ui.peeingParent.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "PeeGreenBar", Quantize01(GetFloatMember(GetFieldObject(ui, "greenBar"), "fillAmount")), emit, ref hash);
            Emit(prefix + "PhoneAllowed", ui.phoneUI != null && ui.phoneUI.allowPhone ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhonePaused", ui.phoneUI != null && ui.phoneUI.isPaused ? 1 : 0, emit, ref hash);
            Emit(prefix + "PhoneCanvas", ui.phoneUI != null && IsObjectActive(ui.phoneUI.phoneCanvas) ? 1 : 0, emit, ref hash);
        }

        private static void EmitComputer(string prefix, ComputerManager computer, Action<string, int> emit, ref int hash)
        {
            if (computer == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", computer.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "DesktopOn", computer.isDesktopOn ? 1 : 0, emit, ref hash);
            Emit(prefix + "OpenSheetsOnStart", computer.openSheetsOnStart ? 1 : 0, emit, ref hash);
            Emit(prefix + "ClickSound", computer.clickSound ? 1 : 0, emit, ref hash);
            Emit(prefix + "CoffeeDone", computer.coffeeDone ? 1 : 0, emit, ref hash);
            Emit(prefix + "CallDone", computer.callDone ? 1 : 0, emit, ref hash);
            Emit(prefix + "CanExit", computer.canExit ? 1 : 0, emit, ref hash);
            Emit(prefix + "TypeMasterOpen", computer.istypeMasterWindowOpen ? 1 : 0, emit, ref hash);
            Emit(prefix + "JustExited", computer.justExitedComputer ? 1 : 0, emit, ref hash);
            Emit(prefix + "BrowserOpen", GetFieldValue<bool>(computer, "isBrowserWindowOpen") ? 1 : 0, emit, ref hash);
            Emit(prefix + "BrowserTransition", GetFieldValue<bool>(computer, "isBrowserInTransition") ? 1 : 0, emit, ref hash);
            Emit(prefix + "SearchDone", GetFieldValue<bool>(computer, "searchTextDone") ? 1 : 0, emit, ref hash);
            Emit(prefix + "SearchPageOn", GetFieldValue<bool>(computer, "searchPageOn") ? 1 : 0, emit, ref hash);
            Emit(prefix + "SearchCabinDone", GetFieldValue<bool>(computer, "searchTextDoneCabin") ? 1 : 0, emit, ref hash);
            Emit(prefix + "SearchInputLen", GetTextLength(GetFieldObject(computer, "inputField")), emit, ref hash);
            Emit(prefix + "SearchCabinInputLen", GetTextLength(GetFieldObject(computer, "inputFieldCabin")), emit, ref hash);
            Emit(prefix + "SheetsOpen", GetFieldValue<bool>(computer, "isSheetsWindowOpen") ? 1 : 0, emit, ref hash);
            Emit(prefix + "SheetsTransition", GetFieldValue<bool>(computer, "isSheetsInTransition") ? 1 : 0, emit, ref hash);
            Emit(prefix + "TypeMasterTransition", GetFieldValue<bool>(computer, "istypeMasterWindowInTransition") ? 1 : 0, emit, ref hash);
            Emit(prefix + "IconsCount", GetFieldValue<int>(computer, "iconsCount"), emit, ref hash);
            Emit(prefix + "ParentObject", IsObjectActive(computer.parentObject) ? 1 : 0, emit, ref hash);
            Emit(prefix + "BrowserWindow", IsObjectActive(computer.browserWindow) ? 1 : 0, emit, ref hash);
            Emit(prefix + "SheetWindow", IsObjectActive(computer.sheetWindow) ? 1 : 0, emit, ref hash);
            Emit(prefix + "TypeMasterWindow", IsObjectActive(computer.typeMasterWindow) ? 1 : 0, emit, ref hash);
            Emit(prefix + "BrowserSibling", GetSiblingIndex(computer.browserWindow), emit, ref hash);
            Emit(prefix + "SheetSibling", GetSiblingIndex(computer.sheetWindow), emit, ref hash);
            Emit(prefix + "TypeMasterSibling", GetSiblingIndex(computer.typeMasterWindow), emit, ref hash);
            EmitRect(prefix + "BrowserRect.", computer.browserWindowRect, emit, ref hash);
            EmitRect(prefix + "SheetRect.", computer.sheetWindowRect, emit, ref hash);
            EmitRect(prefix + "TypeMasterRect.", computer.typeMasterWindowRect, emit, ref hash);
            Emit(prefix + "SearchPage", IsObjectActive(computer.searchPage) ? 1 : 0, emit, ref hash);
            Emit(prefix + "AirbnbPage", IsObjectActive(computer.AirbnbPage) ? 1 : 0, emit, ref hash);
            Emit(prefix + "HomePage", IsObjectActive(computer.homePage) ? 1 : 0, emit, ref hash);
            Emit(prefix + "CabinListPage", IsObjectActive(computer.cabinListPage) ? 1 : 0, emit, ref hash);
            Emit(prefix + "MainCabinPage", IsObjectActive(computer.mainCabinPage) ? 1 : 0, emit, ref hash);
            Emit(prefix + "ConfirmPopup", IsObjectActive(computer.confirmationPopup) ? 1 : 0, emit, ref hash);
            Emit(prefix + "LoadingIcon", IsObjectActive(computer.loadingIcon) ? 1 : 0, emit, ref hash);
            EmitIconMasks(prefix, computer, emit, ref hash);
        }

        private static void EmitIconMasks(string prefix, ComputerManager computer, Action<string, int> emit, ref int hash)
        {
            var icons = GetFieldValue<List<ComputerIcon>>(computer, "icons");
            var shadowMask = 0;
            var outlineMask = 0;
            var rootMask = 0;
            if (icons != null)
            {
                for (var i = 0; i < icons.Count && i < 30; i++)
                {
                    var icon = icons[i];
                    if (icon == null) continue;
                    if (icon.gameObject != null && icon.gameObject.activeSelf) rootMask |= 1 << i;
                    if (icon.whiteShadowGO != null && icon.whiteShadowGO.activeSelf) shadowMask |= 1 << i;
                    if (icon.whiteOutlineGO != null && icon.whiteOutlineGO.activeSelf) outlineMask |= 1 << i;
                }
            }

            Emit(prefix + "IconCount", icons != null ? icons.Count : 0, emit, ref hash);
            Emit(prefix + "IconRootMask", rootMask, emit, ref hash);
            Emit(prefix + "IconShadowMask", shadowMask, emit, ref hash);
            Emit(prefix + "IconOutlineMask", outlineMask, emit, ref hash);
        }

        private static void EmitCoffee(string prefix, CoffeeMachineManager coffee, Action<string, int> emit, ref int hash)
        {
            if (coffee == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", coffee.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", coffee.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "State", (int)coffee.GetCoffeeMachineState(), emit, ref hash);
            Emit(prefix + "WaterAdded", coffee.isWaterAdded ? 1 : 0, emit, ref hash);
            Emit(prefix + "PodAdded", coffee.isPodAdded ? 1 : 0, emit, ref hash);
            Emit(prefix + "CupPlaced", coffee.isCupPlaced ? 1 : 0, emit, ref hash);
            Emit(prefix + "Brewing", GetFieldValue<bool>(coffee, "brewingCoffee") ? 1 : 0, emit, ref hash);
            Emit(prefix + "CanInput", GetFieldValue<bool>(coffee, "canInput") ? 1 : 0, emit, ref hash);
            Emit(prefix + "PourParticle", IsObjectActive(GetFieldObject(coffee, "coffeePourParticle")) ? 1 : 0, emit, ref hash);
        }

        private static void EmitPhone(string prefix, OfficePhone phone, Action<string, int> emit, ref int hash)
        {
            if (phone == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", phone.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Ringing", phone.isPhoneRinging ? 1 : 0, emit, ref hash);
            Emit(prefix + "RingingAudio", phone.ringingAS != null && phone.ringingAS.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "PickupAudio", phone.phonePickupAS != null && phone.phonePickupAS.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "HangupAudio", phone.phoneHangupAS != null && phone.phoneHangupAS.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "BeepAudio", phone.phoneBeepAS != null && phone.phoneBeepAS.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "PointLight", IsObjectActive(phone.pointLight) ? 1 : 0, emit, ref hash);
            Emit(prefix + "HaloLight", phone.HaloLight != null && phone.HaloLight.gameObject.activeSelf ? 1 : 0, emit, ref hash);
        }

        private static void EmitTypeMaster(string prefix, TypeMasterManager typeMaster, Action<string, int> emit, ref int hash)
        {
            if (typeMaster == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", typeMaster.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", typeMaster.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "AllowGameoverByLength", typeMaster.allowGameoverByLength ? 1 : 0, emit, ref hash);
            Emit(prefix + "CanPlay", typeMaster.canPlay ? 1 : 0, emit, ref hash);
            Emit(prefix + "GameStarted", typeMaster.gameStarted ? 1 : 0, emit, ref hash);
            Emit(prefix + "KeyHeld", typeMaster.keyHeld ? 1 : 0, emit, ref hash);
            Emit(prefix + "KeyTimeOut", typeMaster.keyTimeOut ? 1 : 0, emit, ref hash);
            Emit(prefix + "State", ReadEnumInt(typeMaster, "state"), emit, ref hash);
            Emit(prefix + "TimerMs", Mathf.RoundToInt(GetFieldValue<float>(typeMaster, "timer") * 1000f), emit, ref hash);
            Emit(prefix + "Correct", typeMaster.correctCharCount, emit, ref hash);
            Emit(prefix + "Incorrect", typeMaster.incorrectCharCount, emit, ref hash);
            Emit(prefix + "CurrentChar", typeMaster.currentCharIndex, emit, ref hash);
            Emit(prefix + "GeneratedLength", typeMaster.generatedTextLength, emit, ref hash);
            Emit(prefix + "GeneratedWords", typeMaster.generatedWordsCount, emit, ref hash);
            Emit(prefix + "PlayerInputTextLength", typeMaster.playerInputTextLength, emit, ref hash);
            Emit(prefix + "PlayerInputFieldLength", typeMaster.playerInputFieldTextLength, emit, ref hash);
            Emit(prefix + "RoundEndedTimer", typeMaster.roundEndedBecauseOfTimer ? 1 : 0, emit, ref hash);
            Emit(prefix + "RoundEndedChars", typeMaster.roundEndedBecauseOfCharacters ? 1 : 0, emit, ref hash);
            Emit(prefix + "InTransition", typeMaster.inTransition ? 1 : 0, emit, ref hash);
        }

        private static void EmitTypingShooter(string prefix, TypingShooterComputerManager shooter, Action<string, int> emit, ref int hash)
        {
            if (shooter == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", shooter.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "Enabled", shooter.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "InsideMiniGame", shooter.insideMiniGame ? 1 : 0, emit, ref hash);
            Emit(prefix + "GameFocused", shooter.gameFocused ? 1 : 0, emit, ref hash);
            Emit(prefix + "CanGetup", shooter.canGetup ? 1 : 0, emit, ref hash);
            Emit(prefix + "InTransition", GetFieldValue<bool>(shooter, "inTransition") ? 1 : 0, emit, ref hash);
            Emit(prefix + "BlackScreen", IsObjectActive(shooter.blackScreenImage) ? 1 : 0, emit, ref hash);
            Emit(prefix + "RenderTexture", IsObjectActive(shooter.renderTexture) ? 1 : 0, emit, ref hash);
            Emit(prefix + "RestartScreen", IsObjectActive(shooter.blackScreenDuringRestart) ? 1 : 0, emit, ref hash);
            Emit(prefix + "ComputerCamera", IsObjectActive(shooter.computerCamera) ? 1 : 0, emit, ref hash);
        }

        private static void EmitRadios(string prefix, OfficeRadio[] radios, Action<string, int> emit, ref int hash)
        {
            SortByPath(radios);
            Emit(prefix + "Count", radios != null ? radios.Length : 0, emit, ref hash);
            if (radios == null) return;

            for (var i = 0; i < radios.Length; i++)
            {
                var radio = radios[i];
                if (radio == null) continue;
                var itemPrefix = prefix + i + ".";
                var radioAudio = GetFieldValue<AudioSource>(radio, "radioAudioSource");
                var buttonAudio = GetFieldValue<AudioSource>(radio, "radioButtonAudioSource");
                var playButton = GetFieldValue<Transform>(radio, "playButton");
                Emit(itemPrefix + "RootActive", radio.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", radio.radioEnabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Track", GetAudioTrackIndex(radio, radioAudio != null ? radioAudio.clip : null), emit, ref hash);
                Emit(itemPrefix + "TrackPlaying", radioAudio != null && radioAudio.isPlaying ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "TrackTimeMs", radioAudio != null ? Mathf.RoundToInt(radioAudio.time * 1000f) : 0, emit, ref hash);
                Emit(itemPrefix + "ButtonAudio", buttonAudio != null && buttonAudio.isPlaying ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "ButtonLocalX", playButton != null ? Quantize(playButton.localPosition.x) : 0, emit, ref hash);
                Emit(itemPrefix + "ButtonLocalY", playButton != null ? Quantize(playButton.localPosition.y) : 0, emit, ref hash);
                Emit(itemPrefix + "ButtonLocalZ", playButton != null ? Quantize(playButton.localPosition.z) : 0, emit, ref hash);
            }
        }

        private static void EmitFridges(string prefix, OfficeFridge[] fridges, Action<string, int> emit, ref int hash)
        {
            SortByPath(fridges);
            Emit(prefix + "Count", fridges != null ? fridges.Length : 0, emit, ref hash);
            if (fridges == null) return;

            for (var i = 0; i < fridges.Length; i++)
            {
                var fridge = fridges[i];
                if (fridge == null) continue;
                var itemPrefix = prefix + i + ".";
                var pointLight = GetFieldValue<Light>(fridge, "pointLight");
                var doorAudio = GetFieldValue<AudioSource>(fridge, "doorAudioSource");
                Emit(itemPrefix + "RootActive", fridge.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Interactable", GetFieldValue<bool>(fridge, "isInteractable") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Open", fridge.IsOpen ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "KeepLightOn", GetFieldValue<bool>(fridge, "keepLightOn") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Light", pointLight != null && pointLight.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "RunningAudio", fridge.fridgeRunningAS != null && fridge.fridgeRunningAS.isPlaying ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "RunningVolume", fridge.fridgeRunningAS != null ? Quantize01(fridge.fridgeRunningAS.volume) : 0, emit, ref hash);
                Emit(itemPrefix + "DoorAudio", doorAudio != null && doorAudio.isPlaying ? 1 : 0, emit, ref hash);
            }
        }

        private static void EmitMicrowaves(string prefix, OfficeMicrowave[] microwaves, Action<string, int> emit, ref int hash)
        {
            SortByPath(microwaves);
            Emit(prefix + "Count", microwaves != null ? microwaves.Length : 0, emit, ref hash);
            if (microwaves == null) return;

            for (var i = 0; i < microwaves.Length; i++)
            {
                var microwave = microwaves[i];
                if (microwave == null) continue;
                var itemPrefix = prefix + i + ".";
                var door = GetFieldValue<Transform>(microwave, "doorTransform");
                var light = GetFieldObject(microwave, "microwaveLight");
                var audio = GetFieldValue<AudioSource>(microwave, "microwaveAudioSource");
                Emit(itemPrefix + "RootActive", microwave.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Open", GetFieldValue<bool>(microwave, "isOpen") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Transitioning", GetFieldValue<bool>(microwave, "transitioning") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Light", IsObjectActive(light) ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "DoorRotX", door != null ? Quantize(door.localEulerAngles.x) : 0, emit, ref hash);
                Emit(itemPrefix + "DoorRotY", door != null ? Quantize(door.localEulerAngles.y) : 0, emit, ref hash);
                Emit(itemPrefix + "DoorRotZ", door != null ? Quantize(door.localEulerAngles.z) : 0, emit, ref hash);
                Emit(itemPrefix + "Audio", audio != null && audio.isPlaying ? 1 : 0, emit, ref hash);
            }
        }

        private static void EmitToilets(string prefix, OfficeToiletManager[] toilets, Action<string, int> emit, ref int hash)
        {
            SortByPath(toilets);
            var all = UnityEngine.Object.FindObjectOfType<AllOfficeToiletsManager>();
            Emit(prefix + "Count", toilets != null ? toilets.Length : 0, emit, ref hash);
            Emit(prefix + "UsedIndex", all != null ? all.UsedToiletIndex : -1, emit, ref hash);
            if (toilets == null) return;

            for (var i = 0; i < toilets.Length; i++)
            {
                var toilet = toilets[i];
                if (toilet == null) continue;
                var itemPrefix = prefix + i + ".";
                var lid = GetFieldValue<OfficeToiletLid>(toilet, "toiletLid");
                var seat = GetFieldValue<OfficeToiletSeat>(toilet, "toiletSeat");
                var sittingParent = GetFieldObject(toilet, "sittingCameraParent");
                var peeAudio = GetFieldValue<AudioSource>(toilet, "peeAS");
                var toiletDoor = GetFieldValue<OneSidedDoor>(toilet, "toiletDoor");
                Emit(itemPrefix + "RootActive", toilet.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "CanPee", toilet.canPee ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "PeeingDone", toilet.peeingDone ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "ClickCollider", toilet.toiletClickSphereCollider != null && toilet.toiletClickSphereCollider.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "SittingCamera", IsObjectActive(sittingParent) ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "LidOpen", lid != null && lid.isOpen ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "SeatOpen", seat != null && seat.isOpen ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "PeeAudio", peeAudio != null && peeAudio.isPlaying ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "DoorOpen", toiletDoor != null && toiletDoor.IsOpen ? 1 : 0, emit, ref hash);
            }
        }

        private static void EmitJanitor(string prefix, OfficeJanitor janitor, Action<string, int> emit, ref int hash)
        {
            if (janitor == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            var agent = janitor.GetComponent<UnityEngine.AI.NavMeshAgent>();
            var dialogueCamera = GetFieldValue<Camera>(janitor, "dialogueCamera");
            var jumpscareLight = GetFieldValue<Light>(janitor, "jumpscareLight");
            var humming = GetFieldValue<AudioSource>(janitor, "hummingAudioSource");
            var footstepSystem = GetFieldObject(janitor, "footstepSystem");
            var jumpscareTrigger = GetFieldObject(janitor, "jumpscareTrigger");
            var capsule = GetFieldObject(janitor, "antiPlayerPushCapsule");
            var skinned = GetFieldValue<SkinnedMeshRenderer>(janitor, "skinnedMeshRenderer");
            var broom = GetFieldValue<MeshRenderer>(janitor, "broomMeshRenderer");
            var headRig = GetFieldObject(janitor, "headRig");
            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "RootActive", janitor.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "State", (int)janitor.currentState, emit, ref hash);
            Emit(prefix + "Moving", janitor.moving ? 1 : 0, emit, ref hash);
            Emit(prefix + "Go", GetFieldValue<bool>(janitor, "go") ? 1 : 0, emit, ref hash);
            Emit(prefix + "PlayerStall", GetFieldValue<int>(janitor, "playerStallIndex"), emit, ref hash);
            Emit(prefix + "SequenceStarted", GetFieldValue<bool>(janitor, "sequenceStarted") ? 1 : 0, emit, ref hash);
            Emit(prefix + "CanTalk", janitor.canTalk ? 1 : 0, emit, ref hash);
            Emit(prefix + "DisablePhone", janitor.disablePhoneAccess ? 1 : 0, emit, ref hash);
            Emit(prefix + "LockedJumpscare", GetFieldValue<bool>(janitor, "lockedJumpscare") ? 1 : 0, emit, ref hash);
            Emit(prefix + "TalkingPostJumpscare", GetFieldValue<bool>(janitor, "talkingPostJumpscare") ? 1 : 0, emit, ref hash);
            Emit(prefix + "StopToTalk", GetFieldValue<bool>(janitor, "stopToTalk") ? 1 : 0, emit, ref hash);
            Emit(prefix + "NavEnabled", agent != null && agent.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "NavStopped", agent != null && agent.enabled && agent.isStopped ? 1 : 0, emit, ref hash);
            Emit(prefix + "AnimatorState", janitor.animator != null ? janitor.animator.GetInteger("State") : 0, emit, ref hash);
            Emit(prefix + "HeadRigWeight", Quantize01(GetFloatMember(headRig, "weight")), emit, ref hash);
            Emit(prefix + "DialogueCamera", dialogueCamera != null && dialogueCamera.gameObject.activeSelf ? 1 : 0, emit, ref hash);
            Emit(prefix + "JumpscareLight", jumpscareLight != null && jumpscareLight.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "JumpscareLightIntensity", jumpscareLight != null ? Quantize01(jumpscareLight.intensity) : 0, emit, ref hash);
            Emit(prefix + "HummingAudio", humming != null && humming.isPlaying ? 1 : 0, emit, ref hash);
            Emit(prefix + "Footsteps", IsObjectActive(footstepSystem) ? 1 : 0, emit, ref hash);
            Emit(prefix + "JumpscareTrigger", IsObjectActive(jumpscareTrigger) ? 1 : 0, emit, ref hash);
            Emit(prefix + "AntiPushCapsule", IsObjectActive(capsule) ? 1 : 0, emit, ref hash);
            Emit(prefix + "SkinnedVisible", skinned != null && skinned.enabled ? 1 : 0, emit, ref hash);
            Emit(prefix + "BroomVisible", broom != null && broom.enabled ? 1 : 0, emit, ref hash);
        }

        private static void EmitWorkers(string prefix, OfficeWorker[] workers, Action<string, int> emit, ref int hash)
        {
            SortByPath(workers);
            Emit(prefix + "Count", workers != null ? workers.Length : 0, emit, ref hash);
            if (workers == null) return;

            for (var i = 0; i < workers.Length; i++)
            {
                var worker = workers[i];
                if (worker == null) continue;
                var itemPrefix = prefix + i + ".";
                var dialogueCamera = GetFieldValue<Camera>(worker, "dialogueCamera");
                var light = GetFieldValue<Light>(worker, "conversationLight");
                var typingAudio = GetFieldValue<AudioSource>(worker, "typingAudioSource");
                var animator = worker.GetComponent<Animator>();
                var headRig = GetFieldObject(worker, "headRig");
                var idleMovement = GetFieldValue<IdleMovement>(worker, "idleMovement");
                Emit(itemPrefix + "RootActive", worker.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "CanTalk", worker.canTalk ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "FirstConversationCompleted", GetFieldValue<bool>(worker, "firstConversationCompleted") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "CanStartHitConvo", GetFieldValue<bool>(worker, "canStartHitConvo") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "DialogueCamera", dialogueCamera != null && dialogueCamera.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "ConversationLight", light != null && light.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "ConversationLightIntensity", light != null ? Quantize01(light.intensity) : 0, emit, ref hash);
                Emit(itemPrefix + "TypingAudio", typingAudio != null && typingAudio.isPlaying ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "AnimatorSpeed", animator != null ? Quantize01(animator.speed) : 1000, emit, ref hash);
                Emit(itemPrefix + "HeadRigWeight", Quantize01(GetFloatMember(headRig, "weight")), emit, ref hash);
                Emit(itemPrefix + "IdleHeadMove", idleMovement != null && idleMovement.applyHeadMovement ? 1 : 0, emit, ref hash);
            }
        }

        private static void EmitWorkerMonitors(string prefix, OfficeWorkerMonitor[] monitors, Action<string, int> emit, ref int hash)
        {
            SortByPath(monitors);
            Emit(prefix + "Count", monitors != null ? monitors.Length : 0, emit, ref hash);
            if (monitors == null) return;

            for (var i = 0; i < monitors.Length; i++)
            {
                var monitor = monitors[i];
                if (monitor == null) continue;
                var itemPrefix = prefix + i + ".";
                var videoPlayer = GetFieldObject(monitor, "videoPlayer");
                var meshRenderer = GetFieldValue<MeshRenderer>(monitor, "meshRenderer");
                var clip = GetMemberValue(videoPlayer, "clip");
                Emit(itemPrefix + "RootActive", monitor.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", monitor.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "CurrentClipIndex", GetFieldValue<int>(monitor, "currentClipIndex"), emit, ref hash);
                Emit(itemPrefix + "VideoClipHash", StableStringHash(GetObjectName(clip)), emit, ref hash);
                Emit(itemPrefix + "VideoPlaying", GetBoolMember(videoPlayer, "isPlaying") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "VideoTimeMs", GetVideoTimeMs(videoPlayer), emit, ref hash);
                Emit(itemPrefix + "RendererEnabled", meshRenderer != null && meshRenderer.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "MaterialOn", meshRenderer != null && meshRenderer.sharedMaterial != null ? 1 : 0, emit, ref hash);
            }
        }

        private static void EmitBoundsSubs(string prefix, OnTriggerOfficeBoundsSub[] triggers, Action<string, int> emit, ref int hash)
        {
            SortByPath(triggers);
            Emit(prefix + "Count", triggers != null ? triggers.Length : 0, emit, ref hash);
            if (triggers == null) return;

            for (var i = 0; i < triggers.Length; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;
                var itemPrefix = prefix + i + ".";
                Emit(itemPrefix + "RootActive", trigger.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", trigger.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "OnlyOnce", trigger.onlyOnce ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Entered", GetFieldValue<bool>(trigger, "entered") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "EnterObject", IsObjectActive(trigger.enterGameObject) ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "SpecificKeyHash", StableStringHash(GetFieldValue<string>(trigger, "specificDialogueKey")), emit, ref hash);
            }
        }

        private static void EmitTriggerSubs(string prefix, OnTriggerSub[] triggers, Action<string, int> emit, ref int hash)
        {
            SortByPath(triggers);
            Emit(prefix + "Count", triggers != null ? triggers.Length : 0, emit, ref hash);
            if (triggers == null) return;

            for (var i = 0; i < triggers.Length; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;
                var itemPrefix = prefix + i + ".";
                Emit(itemPrefix + "RootActive", trigger.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", trigger.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "OnlyOnce", trigger.onlyOnce ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Entered", GetFieldValue<bool>(trigger, "entered") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "EnterObject", IsObjectActive(trigger.enterGameObject) ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "SubKeyHash", StableStringHash(trigger.subKey), emit, ref hash);
                Emit(itemPrefix + "SubTimeMs", Mathf.RoundToInt(trigger.subTime * 1000f), emit, ref hash);
            }
        }

        private static void EmitDisplaySubs(string prefix, OnTriggerDisplaySub[] triggers, Action<string, int> emit, ref int hash)
        {
            SortByPath(triggers);
            Emit(prefix + "Count", triggers != null ? triggers.Length : 0, emit, ref hash);
            if (triggers == null) return;

            for (var i = 0; i < triggers.Length; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;
                var itemPrefix = prefix + i + ".";
                Emit(itemPrefix + "RootActive", trigger.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", trigger.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "OnlyOnce", trigger.onlyOnce ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Entered", GetFieldValue<bool>(trigger, "entered") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "EnterObject", IsObjectActive(trigger.enterGameObject) ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "SubKeyHash", StableStringHash(GetFieldValue<string>(trigger, "subKey")), emit, ref hash);
            }
        }

        private static void EmitGenericTriggers(string prefix, OnTrigger[] triggers, Action<string, int> emit, ref int hash)
        {
            SortByPath(triggers);
            Emit(prefix + "Count", triggers != null ? triggers.Length : 0, emit, ref hash);
            if (triggers == null) return;

            for (var i = 0; i < triggers.Length; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;
                var itemPrefix = prefix + i + ".";
                Emit(itemPrefix + "RootActive", trigger.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", trigger.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "OnlyOnce", trigger.onlyOnce ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Entered", GetFieldValue<bool>(trigger, "entered") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "EnterObject", IsObjectActive(trigger.enterGameObject) ? 1 : 0, emit, ref hash);
            }
        }

        private static void EmitEventTriggers(string prefix, TriggerEventInvoker[] triggers, Action<string, int> emit, ref int hash)
        {
            SortByPath(triggers);
            Emit(prefix + "Count", triggers != null ? triggers.Length : 0, emit, ref hash);
            if (triggers == null) return;

            for (var i = 0; i < triggers.Length; i++)
            {
                var trigger = triggers[i];
                if (trigger == null) continue;
                var itemPrefix = prefix + i + ".";
                Emit(itemPrefix + "RootActive", trigger.gameObject.activeSelf ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Enabled", trigger.enabled ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "OnlyOnce", GetFieldValue<bool>(trigger, "onlyOnce") ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "Triggered", trigger.isTriggered ? 1 : 0, emit, ref hash);
                Emit(itemPrefix + "EnterObject", IsObjectActive(trigger.enterGameObject) ? 1 : 0, emit, ref hash);
            }
        }

        private static bool TryApplyGame(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var manager = UnityEngine.Object.FindObjectOfType<OfficeLayoutGameManager>();
            if (manager == null) return false;

            var player = GetFieldValue<OfficeLayoutPlayerController>(manager, "officeLayoutPlayerController");
            var firstPerson = GetFieldValue<FirstPersonController>(manager, "firstPersonController");
            var yellowIntro = GetFieldObject(manager, "yellowIntroManager");
            var yellowAudio = GetFieldValue<AudioSource>(manager, "yellowTextsAmbience");
            var mikeTruck = GetFieldObject(manager, "mikeTruckInLoopScene");
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { manager.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "CurrentState", StringComparison.Ordinal)) { manager.currentState = (OfficeLayoutGameManager.GameState)value; return true; }
            if (string.Equals(name, "CurrentPlayerState", StringComparison.Ordinal)) { manager.currentPlayerState = (OfficeLayoutGameManager.PlayerState)value; return true; }
            if (string.Equals(name, "PlayerControllerEnabled", StringComparison.Ordinal)) { if (player != null) player.enabled = value != 0; return true; }
            if (string.Equals(name, "FirstPersonEnabled", StringComparison.Ordinal)) { if (firstPerson != null) firstPerson.enabled = value != 0; return true; }
            if (string.Equals(name, "FirstPersonActive", StringComparison.Ordinal)) { if (firstPerson != null) firstPerson.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "YellowIntroActive", StringComparison.Ordinal)) { SetObjectActive(yellowIntro, value != 0); return true; }
            if (string.Equals(name, "YellowAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(yellowAudio, value != 0); return true; }
            if (string.Equals(name, "MikeTruckLoopActive", StringComparison.Ordinal)) { SetObjectActive(mikeTruck, value != 0); return true; }
            MaybeLogUnknown(logger, "Office game", name);
            return true;
        }

        private static bool TryApplyYellowIntro(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var yellowIntro = UnityEngine.Object.FindObjectOfType<YellowIntroManager>();
            if (yellowIntro == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { yellowIntro.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { yellowIntro.enabled = false; SuppressYellowIntroLog(logger); return true; }
            if (string.Equals(name, "TimerMs", StringComparison.Ordinal)) { SetFieldValue(yellowIntro, "timer", value / 1000f); return true; }
            if (string.Equals(name, "CurrentTextIndex", StringComparison.Ordinal)) { SetFieldValue(yellowIntro, "currentTextIndex", value); return true; }
            if (string.Equals(name, "LastDisabledIndex", StringComparison.Ordinal)) { SetFieldValue(yellowIntro, "lastDisabledIndex", value); return true; }
            if (string.Equals(name, "TextCount", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "TextEnabledMask", StringComparison.Ordinal)) { ApplyYellowIntroTextMask(yellowIntro, value); return true; }
            MaybeLogUnknown(logger, "Office yellow-intro", name);
            return true;
        }

        private static bool TryApplyPlayer(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var player = UnityEngine.Object.FindObjectOfType<OfficeLayoutPlayerController>();
            if (player == null) return false;

            var mainCamera = GetFieldValue<Camera>(player, "mainCamera");
            var firstPerson = GetFieldValue<FirstPersonController>(player, "firstPersonController");
            var hallwaySub = GetFieldObject(player, "hallwayRestroomSubTrigger");
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { player.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { player.enabled = value != 0; return true; }
            if (string.Equals(name, "FirstPersonActive", StringComparison.Ordinal)) { if (firstPerson != null) firstPerson.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "FirstPersonEnabled", StringComparison.Ordinal)) { if (firstPerson != null) firstPerson.enabled = value != 0; return true; }
            if (string.Equals(name, "FirstPersonCanZoom", StringComparison.Ordinal)) { if (firstPerson != null) firstPerson.canZoomIn = value != 0; return true; }
            if (string.Equals(name, "CanThrowItem", StringComparison.Ordinal)) { player.canThrowItem = value != 0; return true; }
            if (string.Equals(name, "RecentlyThrown", StringComparison.Ordinal)) { player.playerhasRecentlyThrown = value != 0; return true; }
            if (string.Equals(name, "MadeCoffee", StringComparison.Ordinal)) { SetFieldValue(player, "madeCoffee", value != 0); return true; }
            if (string.Equals(name, "CurrentSip", StringComparison.Ordinal)) { player.currentSip = value; return true; }
            if (string.Equals(name, "Sipping", StringComparison.Ordinal)) { player.sipping = value != 0; return true; }
            if (string.Equals(name, "DelayTimerMs", StringComparison.Ordinal)) { player.delayTimer = value / 1000f; return true; }
            if (string.Equals(name, "CupRotX", StringComparison.Ordinal)) { player.cupRotX = Dequantize(value); return true; }
            if (string.Equals(name, "SipAnim", StringComparison.Ordinal)) { if (player.handAnimator != null) player.handAnimator.SetBool(player.sipParam, value != 0); return true; }
            if (string.Equals(name, "SipAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(player.sipAS, value != 0); return true; }
            if (string.Equals(name, "HallwayRestroomSub", StringComparison.Ordinal)) { SetObjectActive(hallwaySub, value != 0); return true; }
            if (string.Equals(name, "PhoneTrigger", StringComparison.Ordinal)) { SetObjectActive(player.phoneTrigger, value != 0); return true; }
            if (string.Equals(name, "LookTargetHash", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "HoldingHash", StringComparison.Ordinal)) return true;
            if (string.Equals(name, "CameraFov10", StringComparison.Ordinal)) { if (mainCamera != null) mainCamera.fieldOfView = value / 10f; return true; }
            MaybeLogUnknown(logger, "Office player", name);
            return true;
        }

        private static bool TryApplyTable(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var table = UnityEngine.Object.FindObjectOfType<TableManager>();
            if (table == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { table.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "CoffeeDone", StringComparison.Ordinal)) { table.coffeeDone = value != 0; return true; }
            if (string.Equals(name, "UsedRestroom", StringComparison.Ordinal)) { table.usedRestroom = value != 0; return true; }
            if (string.Equals(name, "IsSitting", StringComparison.Ordinal)) { table.isSitting = value != 0; return true; }
            if (string.Equals(name, "UsingComputer", StringComparison.Ordinal)) { table.usingComputer = value != 0; return true; }
            if (string.Equals(name, "UsingTelephone", StringComparison.Ordinal)) { table.usingTelephone = value != 0; return true; }
            if (string.Equals(name, "MikeCalled", StringComparison.Ordinal)) { table.mikeCalled = value != 0; return true; }
            if (string.Equals(name, "RemindedCabin", StringComparison.Ordinal)) { table.remindedPlayerAboutBookingACabin = value != 0; return true; }
            if (string.Equals(name, "TableCamera", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(table, "tableCameraHolder"), value != 0); return true; }
            if (string.Equals(name, "DialogueCam", StringComparison.Ordinal)) { SetObjectActive(table.DialogueCam, value != 0); return true; }
            if (string.Equals(name, "MonitorTrigger", StringComparison.Ordinal)) { SetObjectActive(table.monitorTrigger, value != 0); return true; }
            if (string.Equals(name, "TelephoneTrigger", StringComparison.Ordinal)) { SetObjectActive(table.telephoneTrigger, value != 0); return true; }
            MaybeLogUnknown(logger, "Office table", name);
            return true;
        }

        private static bool TryApplyComputer(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var computer = UnityEngine.Object.FindObjectOfType<ComputerManager>();
            if (computer == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { computer.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "DesktopOn", StringComparison.Ordinal)) { computer.isDesktopOn = value != 0; return true; }
            if (string.Equals(name, "OpenSheetsOnStart", StringComparison.Ordinal)) { computer.openSheetsOnStart = value != 0; return true; }
            if (string.Equals(name, "ClickSound", StringComparison.Ordinal)) { computer.clickSound = value != 0; return true; }
            if (string.Equals(name, "CoffeeDone", StringComparison.Ordinal)) { computer.coffeeDone = value != 0; return true; }
            if (string.Equals(name, "CallDone", StringComparison.Ordinal)) { computer.callDone = value != 0; return true; }
            if (string.Equals(name, "CanExit", StringComparison.Ordinal)) { computer.canExit = value != 0; return true; }
            if (string.Equals(name, "TypeMasterOpen", StringComparison.Ordinal)) { computer.istypeMasterWindowOpen = value != 0; return true; }
            if (string.Equals(name, "JustExited", StringComparison.Ordinal)) { computer.justExitedComputer = value != 0; return true; }
            if (string.Equals(name, "BrowserOpen", StringComparison.Ordinal)) { SetFieldValue(computer, "isBrowserWindowOpen", value != 0); return true; }
            if (string.Equals(name, "BrowserTransition", StringComparison.Ordinal)) { SetFieldValue(computer, "isBrowserInTransition", value != 0); return true; }
            if (string.Equals(name, "SearchDone", StringComparison.Ordinal)) { SetFieldValue(computer, "searchTextDone", value != 0); return true; }
            if (string.Equals(name, "SearchPageOn", StringComparison.Ordinal)) { SetFieldValue(computer, "searchPageOn", value != 0); return true; }
            if (string.Equals(name, "SearchCabinDone", StringComparison.Ordinal)) { SetFieldValue(computer, "searchTextDoneCabin", value != 0); return true; }
            if (string.Equals(name, "SearchInputLen", StringComparison.Ordinal)) { ApplyKnownTextPrefix(GetFieldObject(computer, "inputField"), GetFieldObject(computer, "searchBarText"), GetFieldValue<string>(computer, "searchString"), value, GetFieldValue<bool>(computer, "searchTextDone")); return true; }
            if (string.Equals(name, "SearchCabinInputLen", StringComparison.Ordinal)) { ApplyKnownTextPrefix(GetFieldObject(computer, "inputFieldCabin"), GetFieldObject(computer, "searchBarTextCabin"), GetFieldValue<string>(computer, "searchStringCabin"), value, GetFieldValue<bool>(computer, "searchTextDoneCabin")); return true; }
            if (string.Equals(name, "SheetsOpen", StringComparison.Ordinal)) { SetFieldValue(computer, "isSheetsWindowOpen", value != 0); return true; }
            if (string.Equals(name, "SheetsTransition", StringComparison.Ordinal)) { SetFieldValue(computer, "isSheetsInTransition", value != 0); return true; }
            if (string.Equals(name, "TypeMasterTransition", StringComparison.Ordinal)) { SetFieldValue(computer, "istypeMasterWindowInTransition", value != 0); return true; }
            if (string.Equals(name, "IconsCount", StringComparison.Ordinal)) { SetFieldValue(computer, "iconsCount", value); return true; }
            if (string.Equals(name, "ParentObject", StringComparison.Ordinal)) { SetObjectActive(computer.parentObject, value != 0); return true; }
            if (string.Equals(name, "BrowserWindow", StringComparison.Ordinal)) { SetObjectActive(computer.browserWindow, value != 0); return true; }
            if (string.Equals(name, "SheetWindow", StringComparison.Ordinal)) { SetObjectActive(computer.sheetWindow, value != 0); return true; }
            if (string.Equals(name, "TypeMasterWindow", StringComparison.Ordinal)) { SetObjectActive(computer.typeMasterWindow, value != 0); return true; }
            if (string.Equals(name, "BrowserSibling", StringComparison.Ordinal)) { SetSiblingIndex(computer.browserWindow, value); return true; }
            if (string.Equals(name, "SheetSibling", StringComparison.Ordinal)) { SetSiblingIndex(computer.sheetWindow, value); return true; }
            if (string.Equals(name, "TypeMasterSibling", StringComparison.Ordinal)) { SetSiblingIndex(computer.typeMasterWindow, value); return true; }
            if (TryApplyRect(name, "BrowserRect.", computer.browserWindowRect, value)) return true;
            if (TryApplyRect(name, "SheetRect.", computer.sheetWindowRect, value)) return true;
            if (TryApplyRect(name, "TypeMasterRect.", computer.typeMasterWindowRect, value)) return true;
            if (string.Equals(name, "SearchPage", StringComparison.Ordinal)) { SetObjectActive(computer.searchPage, value != 0); return true; }
            if (string.Equals(name, "AirbnbPage", StringComparison.Ordinal)) { SetObjectActive(computer.AirbnbPage, value != 0); return true; }
            if (string.Equals(name, "HomePage", StringComparison.Ordinal)) { SetObjectActive(computer.homePage, value != 0); return true; }
            if (string.Equals(name, "CabinListPage", StringComparison.Ordinal)) { SetObjectActive(computer.cabinListPage, value != 0); return true; }
            if (string.Equals(name, "MainCabinPage", StringComparison.Ordinal)) { SetObjectActive(computer.mainCabinPage, value != 0); return true; }
            if (string.Equals(name, "ConfirmPopup", StringComparison.Ordinal)) { SetObjectActive(computer.confirmationPopup, value != 0); return true; }
            if (string.Equals(name, "LoadingIcon", StringComparison.Ordinal)) { SetObjectActive(computer.loadingIcon, value != 0); return true; }
            if (TryApplyIconMask(name, computer, value)) return true;
            MaybeLogUnknown(logger, "Office computer", name);
            return true;
        }

        private static bool TryApplyCoffee(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var coffee = UnityEngine.Object.FindObjectOfType<CoffeeMachineManager>();
            if (coffee == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { coffee.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { coffee.enabled = false; SuppressLog(logger); return true; }
            if (string.Equals(name, "State", StringComparison.Ordinal)) { SetFieldValue(coffee, "coffeeMachineState", value); ApplyCoffeeAnimator(coffee); return true; }
            if (string.Equals(name, "WaterAdded", StringComparison.Ordinal)) { coffee.isWaterAdded = value != 0; ApplyCoffeeAnimator(coffee); return true; }
            if (string.Equals(name, "PodAdded", StringComparison.Ordinal)) { coffee.isPodAdded = value != 0; ApplyCoffeeAnimator(coffee); return true; }
            if (string.Equals(name, "CupPlaced", StringComparison.Ordinal)) { coffee.isCupPlaced = value != 0; return true; }
            if (string.Equals(name, "Brewing", StringComparison.Ordinal)) { SetFieldValue(coffee, "brewingCoffee", value != 0); return true; }
            if (string.Equals(name, "CanInput", StringComparison.Ordinal)) { SetFieldValue(coffee, "canInput", value != 0); return true; }
            if (string.Equals(name, "PourParticle", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(coffee, "coffeePourParticle"), value != 0); return true; }
            MaybeLogUnknown(logger, "Office coffee", name);
            return true;
        }

        private static bool TryApplyPhone(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var phone = UnityEngine.Object.FindObjectOfType<OfficePhone>();
            if (phone == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { phone.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Ringing", StringComparison.Ordinal)) { phone.isPhoneRinging = value != 0; return true; }
            if (string.Equals(name, "RingingAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(phone.ringingAS, value != 0); return true; }
            if (string.Equals(name, "PickupAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(phone.phonePickupAS, value != 0); return true; }
            if (string.Equals(name, "HangupAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(phone.phoneHangupAS, value != 0); return true; }
            if (string.Equals(name, "BeepAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(phone.phoneBeepAS, value != 0); return true; }
            if (string.Equals(name, "PointLight", StringComparison.Ordinal)) { SetObjectActive(phone.pointLight, value != 0); return true; }
            if (string.Equals(name, "HaloLight", StringComparison.Ordinal)) { if (phone.HaloLight != null) SetObjectActive(phone.HaloLight.gameObject, value != 0); return true; }
            MaybeLogUnknown(logger, "Office phone", name);
            return true;
        }

        private static bool TryApplyTypeMaster(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var typeMaster = UnityEngine.Object.FindObjectOfType<TypeMasterManager>();
            if (typeMaster == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { typeMaster.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { typeMaster.enabled = false; return true; }
            if (string.Equals(name, "AllowGameoverByLength", StringComparison.Ordinal)) { typeMaster.allowGameoverByLength = value != 0; return true; }
            if (string.Equals(name, "CanPlay", StringComparison.Ordinal)) { typeMaster.canPlay = false; return true; }
            if (string.Equals(name, "GameStarted", StringComparison.Ordinal)) { typeMaster.gameStarted = value != 0; return true; }
            if (string.Equals(name, "KeyHeld", StringComparison.Ordinal)) { typeMaster.keyHeld = value != 0; return true; }
            if (string.Equals(name, "KeyTimeOut", StringComparison.Ordinal)) { typeMaster.keyTimeOut = value != 0; return true; }
            if (string.Equals(name, "State", StringComparison.Ordinal)) { SetFieldValue(typeMaster, "state", value); return true; }
            if (string.Equals(name, "TimerMs", StringComparison.Ordinal)) { SetFieldValue(typeMaster, "timer", value / 1000f); return true; }
            if (string.Equals(name, "Correct", StringComparison.Ordinal)) { typeMaster.correctCharCount = value; return true; }
            if (string.Equals(name, "Incorrect", StringComparison.Ordinal)) { typeMaster.incorrectCharCount = value; return true; }
            if (string.Equals(name, "CurrentChar", StringComparison.Ordinal)) { typeMaster.currentCharIndex = value; return true; }
            if (string.Equals(name, "GeneratedLength", StringComparison.Ordinal)) { typeMaster.generatedTextLength = value; return true; }
            if (string.Equals(name, "GeneratedWords", StringComparison.Ordinal)) { typeMaster.generatedWordsCount = value; return true; }
            if (string.Equals(name, "PlayerInputTextLength", StringComparison.Ordinal)) { typeMaster.playerInputTextLength = value; return true; }
            if (string.Equals(name, "PlayerInputFieldLength", StringComparison.Ordinal)) { typeMaster.playerInputFieldTextLength = value; return true; }
            if (string.Equals(name, "RoundEndedTimer", StringComparison.Ordinal)) { typeMaster.roundEndedBecauseOfTimer = value != 0; return true; }
            if (string.Equals(name, "RoundEndedChars", StringComparison.Ordinal)) { typeMaster.roundEndedBecauseOfCharacters = value != 0; return true; }
            if (string.Equals(name, "InTransition", StringComparison.Ordinal)) { typeMaster.inTransition = value != 0; return true; }
            MaybeLogUnknown(logger, "Office TypeMaster", name);
            return true;
        }

        private static bool TryApplyTypingShooter(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var shooter = UnityEngine.Object.FindObjectOfType<TypingShooterComputerManager>();
            if (shooter == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { shooter.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "Enabled", StringComparison.Ordinal)) { shooter.enabled = false; return true; }
            if (string.Equals(name, "InsideMiniGame", StringComparison.Ordinal)) { shooter.insideMiniGame = value != 0; return true; }
            if (string.Equals(name, "GameFocused", StringComparison.Ordinal)) { shooter.gameFocused = value != 0; return true; }
            if (string.Equals(name, "CanGetup", StringComparison.Ordinal)) { shooter.canGetup = value != 0; return true; }
            if (string.Equals(name, "InTransition", StringComparison.Ordinal)) { SetFieldValue(shooter, "inTransition", value != 0); return true; }
            if (string.Equals(name, "BlackScreen", StringComparison.Ordinal)) { SetObjectActive(shooter.blackScreenImage, value != 0); return true; }
            if (string.Equals(name, "RenderTexture", StringComparison.Ordinal)) { SetObjectActive(shooter.renderTexture, value != 0); return true; }
            if (string.Equals(name, "RestartScreen", StringComparison.Ordinal)) { SetObjectActive(shooter.blackScreenDuringRestart, value != 0); return true; }
            if (string.Equals(name, "ComputerCamera", StringComparison.Ordinal)) { SetObjectActive(shooter.computerCamera, value != 0); return true; }
            MaybeLogUnknown(logger, "Office TypingShooter", name);
            return true;
        }

        private static bool TryApplyRadio(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var radios = UnityEngine.Object.FindObjectsOfType<OfficeRadio>();
            SortByPath(radios);
            if (index >= radios.Length) return false;
            var radio = radios[index];
            if (radio == null) return false;

            var radioAudio = GetFieldValue<AudioSource>(radio, "radioAudioSource");
            var buttonAudio = GetFieldValue<AudioSource>(radio, "radioButtonAudioSource");
            var playButton = GetFieldValue<Transform>(radio, "playButton");
            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { radio.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Enabled", StringComparison.Ordinal)) { radio.radioEnabled = value != 0; return true; }
            if (string.Equals(fieldName, "Track", StringComparison.Ordinal)) { ApplyAudioTrack(radio, radioAudio, value); return true; }
            if (string.Equals(fieldName, "TrackPlaying", StringComparison.Ordinal)) { ApplyAudioPlayback(radioAudio, value != 0); return true; }
            if (string.Equals(fieldName, "TrackTimeMs", StringComparison.Ordinal)) { ApplyAudioTime(radioAudio, value); return true; }
            if (string.Equals(fieldName, "ButtonAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(buttonAudio, value != 0); return true; }
            if (playButton != null && string.Equals(fieldName, "ButtonLocalX", StringComparison.Ordinal)) { var p = playButton.localPosition; p.x = Dequantize(value); playButton.localPosition = p; return true; }
            if (playButton != null && string.Equals(fieldName, "ButtonLocalY", StringComparison.Ordinal)) { var p = playButton.localPosition; p.y = Dequantize(value); playButton.localPosition = p; return true; }
            if (playButton != null && string.Equals(fieldName, "ButtonLocalZ", StringComparison.Ordinal)) { var p = playButton.localPosition; p.z = Dequantize(value); playButton.localPosition = p; return true; }
            MaybeLogUnknown(logger, "Office radio", name);
            return true;
        }

        private static bool TryApplyFridge(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var fridges = UnityEngine.Object.FindObjectsOfType<OfficeFridge>();
            SortByPath(fridges);
            if (index >= fridges.Length) return false;
            var fridge = fridges[index];
            if (fridge == null) return false;

            var pointLight = GetFieldValue<Light>(fridge, "pointLight");
            var doorAudio = GetFieldValue<AudioSource>(fridge, "doorAudioSource");
            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { fridge.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Interactable", StringComparison.Ordinal)) { SetFieldValue(fridge, "isInteractable", value != 0); fridge.gameObject.layer = value != 0 ? LayerMask.NameToLayer("Default") : LayerMask.NameToLayer("Ignore Raycast"); return true; }
            if (string.Equals(fieldName, "Open", StringComparison.Ordinal)) { SetFieldValue(fridge, "<IsOpen>k__BackingField", value != 0); return true; }
            if (string.Equals(fieldName, "KeepLightOn", StringComparison.Ordinal)) { SetFieldValue(fridge, "keepLightOn", value != 0); return true; }
            if (string.Equals(fieldName, "Light", StringComparison.Ordinal)) { if (pointLight != null) pointLight.enabled = value != 0; return true; }
            if (string.Equals(fieldName, "RunningAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(fridge.fridgeRunningAS, value != 0); return true; }
            if (string.Equals(fieldName, "RunningVolume", StringComparison.Ordinal)) { if (fridge.fridgeRunningAS != null) fridge.fridgeRunningAS.volume = Dequantize01(value); return true; }
            if (string.Equals(fieldName, "DoorAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(doorAudio, value != 0); return true; }
            MaybeLogUnknown(logger, "Office fridge", name);
            return true;
        }

        private static bool TryApplyMicrowave(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var microwaves = UnityEngine.Object.FindObjectsOfType<OfficeMicrowave>();
            SortByPath(microwaves);
            if (index >= microwaves.Length) return false;
            var microwave = microwaves[index];
            if (microwave == null) return false;

            var door = GetFieldValue<Transform>(microwave, "doorTransform");
            var light = GetFieldObject(microwave, "microwaveLight");
            var audio = GetFieldValue<AudioSource>(microwave, "microwaveAudioSource");
            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { microwave.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Open", StringComparison.Ordinal)) { SetFieldValue(microwave, "isOpen", value != 0); return true; }
            if (string.Equals(fieldName, "Transitioning", StringComparison.Ordinal)) { SetFieldValue(microwave, "transitioning", false); return true; }
            if (string.Equals(fieldName, "Light", StringComparison.Ordinal)) { SetObjectActive(light, value != 0); return true; }
            if (door != null && string.Equals(fieldName, "DoorRotX", StringComparison.Ordinal)) { var r = door.localEulerAngles; r.x = Dequantize(value); door.localEulerAngles = r; return true; }
            if (door != null && string.Equals(fieldName, "DoorRotY", StringComparison.Ordinal)) { var r = door.localEulerAngles; r.y = Dequantize(value); door.localEulerAngles = r; return true; }
            if (door != null && string.Equals(fieldName, "DoorRotZ", StringComparison.Ordinal)) { var r = door.localEulerAngles; r.z = Dequantize(value); door.localEulerAngles = r; return true; }
            if (string.Equals(fieldName, "Audio", StringComparison.Ordinal)) { ApplyAudioPlayback(audio, value != 0); return true; }
            MaybeLogUnknown(logger, "Office microwave", name);
            return true;
        }

        private static bool TryApplyToilet(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal) ||
                string.Equals(name, "UsedIndex", StringComparison.Ordinal))
            {
                return true;
            }

            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var toilets = UnityEngine.Object.FindObjectsOfType<OfficeToiletManager>();
            SortByPath(toilets);
            if (index >= toilets.Length) return false;
            var toilet = toilets[index];
            if (toilet == null) return false;

            var lid = GetFieldValue<OfficeToiletLid>(toilet, "toiletLid");
            var seat = GetFieldValue<OfficeToiletSeat>(toilet, "toiletSeat");
            var sittingParent = GetFieldObject(toilet, "sittingCameraParent");
            var peeAudio = GetFieldValue<AudioSource>(toilet, "peeAS");
            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { toilet.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "CanPee", StringComparison.Ordinal)) { toilet.canPee = value != 0; return true; }
            if (string.Equals(fieldName, "PeeingDone", StringComparison.Ordinal)) { toilet.peeingDone = value != 0; return true; }
            if (string.Equals(fieldName, "ClickCollider", StringComparison.Ordinal)) { if (toilet.toiletClickSphereCollider != null) toilet.toiletClickSphereCollider.enabled = value != 0; return true; }
            if (string.Equals(fieldName, "SittingCamera", StringComparison.Ordinal)) { SetObjectActive(sittingParent, value != 0); return true; }
            if (string.Equals(fieldName, "LidOpen", StringComparison.Ordinal)) { if (lid != null) lid.isOpen = value != 0; return true; }
            if (string.Equals(fieldName, "SeatOpen", StringComparison.Ordinal)) { if (seat != null) seat.isOpen = value != 0; return true; }
            if (string.Equals(fieldName, "PeeAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(peeAudio, value != 0); return true; }
            if (string.Equals(fieldName, "DoorOpen", StringComparison.Ordinal)) { return true; }
            MaybeLogUnknown(logger, "Office toilet", name);
            return true;
        }

        private static bool TryApplyJanitor(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var janitor = UnityEngine.Object.FindObjectOfType<OfficeJanitor>();
            if (janitor == null) return false;

            var agent = janitor.GetComponent<UnityEngine.AI.NavMeshAgent>();
            var dialogueCamera = GetFieldValue<Camera>(janitor, "dialogueCamera");
            var jumpscareLight = GetFieldValue<Light>(janitor, "jumpscareLight");
            var humming = GetFieldValue<AudioSource>(janitor, "hummingAudioSource");
            var footstepSystem = GetFieldObject(janitor, "footstepSystem");
            var jumpscareTrigger = GetFieldObject(janitor, "jumpscareTrigger");
            var capsule = GetFieldObject(janitor, "antiPlayerPushCapsule");
            var skinned = GetFieldValue<SkinnedMeshRenderer>(janitor, "skinnedMeshRenderer");
            var broom = GetFieldValue<MeshRenderer>(janitor, "broomMeshRenderer");
            var headRig = GetFieldObject(janitor, "headRig");
            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { janitor.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "State", StringComparison.Ordinal)) { SetFieldValue(janitor, "currentState", value); if (janitor.animator != null) janitor.animator.SetInteger("State", value); return true; }
            if (string.Equals(name, "Moving", StringComparison.Ordinal)) { janitor.moving = value != 0; return true; }
            if (string.Equals(name, "Go", StringComparison.Ordinal)) { SetFieldValue(janitor, "go", value != 0); return true; }
            if (string.Equals(name, "PlayerStall", StringComparison.Ordinal)) { SetFieldValue(janitor, "playerStallIndex", value); return true; }
            if (string.Equals(name, "SequenceStarted", StringComparison.Ordinal)) { SetFieldValue(janitor, "sequenceStarted", value != 0); return true; }
            if (string.Equals(name, "CanTalk", StringComparison.Ordinal)) { janitor.canTalk = value != 0; return true; }
            if (string.Equals(name, "DisablePhone", StringComparison.Ordinal)) { janitor.disablePhoneAccess = value != 0; return true; }
            if (string.Equals(name, "LockedJumpscare", StringComparison.Ordinal)) { SetFieldValue(janitor, "lockedJumpscare", value != 0); return true; }
            if (string.Equals(name, "TalkingPostJumpscare", StringComparison.Ordinal)) { SetFieldValue(janitor, "talkingPostJumpscare", value != 0); return true; }
            if (string.Equals(name, "StopToTalk", StringComparison.Ordinal)) { SetFieldValue(janitor, "stopToTalk", value != 0); return true; }
            if (string.Equals(name, "NavEnabled", StringComparison.Ordinal)) { if (agent != null) agent.enabled = false; return true; }
            if (string.Equals(name, "NavStopped", StringComparison.Ordinal)) { if (agent != null && agent.enabled) agent.isStopped = true; return true; }
            if (string.Equals(name, "AnimatorState", StringComparison.Ordinal)) { if (janitor.animator != null) janitor.animator.SetInteger("State", value); return true; }
            if (string.Equals(name, "HeadRigWeight", StringComparison.Ordinal)) { SetFloatMember(headRig, "weight", Dequantize01(value)); return true; }
            if (string.Equals(name, "DialogueCamera", StringComparison.Ordinal)) { if (dialogueCamera != null) dialogueCamera.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "JumpscareLight", StringComparison.Ordinal)) { if (jumpscareLight != null) jumpscareLight.enabled = value != 0; return true; }
            if (string.Equals(name, "JumpscareLightIntensity", StringComparison.Ordinal)) { if (jumpscareLight != null) jumpscareLight.intensity = Dequantize01(value); return true; }
            if (string.Equals(name, "HummingAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(humming, value != 0); return true; }
            if (string.Equals(name, "Footsteps", StringComparison.Ordinal)) { SetObjectActive(footstepSystem, value != 0); return true; }
            if (string.Equals(name, "JumpscareTrigger", StringComparison.Ordinal)) { SetObjectActive(jumpscareTrigger, value != 0); return true; }
            if (string.Equals(name, "AntiPushCapsule", StringComparison.Ordinal)) { SetObjectActive(capsule, value != 0); return true; }
            if (string.Equals(name, "SkinnedVisible", StringComparison.Ordinal)) { if (skinned != null) skinned.enabled = value != 0; return true; }
            if (string.Equals(name, "BroomVisible", StringComparison.Ordinal)) { if (broom != null) broom.enabled = value != 0; return true; }
            MaybeLogUnknown(logger, "Office janitor", name);
            return true;
        }

        private static bool TryApplyWorker(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var workers = UnityEngine.Object.FindObjectsOfType<OfficeWorker>();
            SortByPath(workers);
            if (index >= workers.Length) return false;
            var worker = workers[index];
            if (worker == null) return false;

            var dialogueCamera = GetFieldValue<Camera>(worker, "dialogueCamera");
            var light = GetFieldValue<Light>(worker, "conversationLight");
            var typingAudio = GetFieldValue<AudioSource>(worker, "typingAudioSource");
            var animator = worker.GetComponent<Animator>();
            var headRig = GetFieldObject(worker, "headRig");
            var idleMovement = GetFieldValue<IdleMovement>(worker, "idleMovement");
            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { worker.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "CanTalk", StringComparison.Ordinal)) { worker.canTalk = value != 0; return true; }
            if (string.Equals(fieldName, "FirstConversationCompleted", StringComparison.Ordinal)) { SetFieldValue(worker, "firstConversationCompleted", value != 0); return true; }
            if (string.Equals(fieldName, "CanStartHitConvo", StringComparison.Ordinal)) { SetFieldValue(worker, "canStartHitConvo", value != 0); return true; }
            if (string.Equals(fieldName, "DialogueCamera", StringComparison.Ordinal)) { if (dialogueCamera != null) dialogueCamera.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "ConversationLight", StringComparison.Ordinal)) { if (light != null) light.enabled = value != 0; return true; }
            if (string.Equals(fieldName, "ConversationLightIntensity", StringComparison.Ordinal)) { if (light != null) light.intensity = Dequantize01(value); return true; }
            if (string.Equals(fieldName, "TypingAudio", StringComparison.Ordinal)) { ApplyAudioPlayback(typingAudio, value != 0); return true; }
            if (string.Equals(fieldName, "AnimatorSpeed", StringComparison.Ordinal)) { if (animator != null) animator.speed = Dequantize01(value); return true; }
            if (string.Equals(fieldName, "HeadRigWeight", StringComparison.Ordinal)) { SetFloatMember(headRig, "weight", Dequantize01(value)); return true; }
            if (string.Equals(fieldName, "IdleHeadMove", StringComparison.Ordinal)) { if (idleMovement != null) idleMovement.applyHeadMovement = value != 0; return true; }
            MaybeLogUnknown(logger, "Office worker", name);
            return true;
        }

        private static bool TryApplyWorkerMonitor(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var monitors = UnityEngine.Object.FindObjectsOfType<OfficeWorkerMonitor>();
            SortByPath(monitors);
            if (index >= monitors.Length) return false;
            var monitor = monitors[index];
            if (monitor == null) return false;

            var videoPlayer = GetFieldObject(monitor, "videoPlayer");
            var meshRenderer = GetFieldValue<MeshRenderer>(monitor, "meshRenderer");
            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { monitor.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Enabled", StringComparison.Ordinal)) { monitor.enabled = false; SuppressWorkerMonitorLog(logger, monitors.Length); return true; }
            if (string.Equals(fieldName, "CurrentClipIndex", StringComparison.Ordinal)) { SetFieldValue(monitor, "currentClipIndex", value); return true; }
            if (string.Equals(fieldName, "VideoClipHash", StringComparison.Ordinal)) { ApplyWorkerMonitorClip(monitor, videoPlayer, value); return true; }
            if (string.Equals(fieldName, "VideoPlaying", StringComparison.Ordinal)) { InvokeNoArg(videoPlayer, value != 0 ? "Play" : "Pause"); return true; }
            if (string.Equals(fieldName, "VideoTimeMs", StringComparison.Ordinal)) { ApplyVideoTime(videoPlayer, value); return true; }
            if (string.Equals(fieldName, "RendererEnabled", StringComparison.Ordinal)) { if (meshRenderer != null) meshRenderer.enabled = value != 0; return true; }
            if (string.Equals(fieldName, "MaterialOn", StringComparison.Ordinal)) { ApplyWorkerMonitorMaterial(monitor, meshRenderer, value != 0); return true; }
            MaybeLogUnknown(logger, "Office worker-monitor", name);
            return true;
        }

        private static bool TryApplyUi(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Exists", StringComparison.Ordinal)) return true;
            var ui = UnityEngine.Object.FindObjectOfType<OfficeLayoutUIManager>();
            if (ui == null) return false;

            if (string.Equals(name, "RootActive", StringComparison.Ordinal)) { ui.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(name, "InConversation", StringComparison.Ordinal)) { ui.inCoversation = value != 0; return true; }
            if (string.Equals(name, "FadeCanvas", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(ui, "fadeCanvas"), value != 0); return true; }
            if (string.Equals(name, "IntroCanvas", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(ui, "introCanvas"), value != 0); return true; }
            if (string.Equals(name, "DialogCam", StringComparison.Ordinal)) { SetObjectActive(GetFieldObject(ui, "dialogCam"), value != 0); return true; }
            if (string.Equals(name, "PeeingParent", StringComparison.Ordinal)) { SetObjectActive(ui.peeingParent, value != 0); return true; }
            if (string.Equals(name, "PeeGreenBar", StringComparison.Ordinal)) { SetFloatMember(GetFieldObject(ui, "greenBar"), "fillAmount", Dequantize01(value)); return true; }
            if (string.Equals(name, "PhoneAllowed", StringComparison.Ordinal)) { if (ui.phoneUI != null) ui.phoneUI.allowPhone = value != 0; return true; }
            if (string.Equals(name, "PhonePaused", StringComparison.Ordinal)) { if (ui.phoneUI != null) ui.phoneUI.isPaused = value != 0; return true; }
            if (string.Equals(name, "PhoneCanvas", StringComparison.Ordinal)) { if (ui.phoneUI != null) SetObjectActive(ui.phoneUI.phoneCanvas, value != 0); return true; }
            MaybeLogUnknown(logger, "Office UI", name);
            return true;
        }

        private static bool TryApplyBoundsSub(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var triggers = UnityEngine.Object.FindObjectsOfType<OnTriggerOfficeBoundsSub>();
            SortByPath(triggers);
            if (index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; SuppressBoundsSubLog(logger, triggers.Length); return true; }
            if (string.Equals(fieldName, "OnlyOnce", StringComparison.Ordinal)) { trigger.onlyOnce = value != 0; return true; }
            if (string.Equals(fieldName, "Entered", StringComparison.Ordinal)) { SetFieldValue(trigger, "entered", value != 0); return true; }
            if (string.Equals(fieldName, "EnterObject", StringComparison.Ordinal)) { SetObjectActive(trigger.enterGameObject, value != 0); return true; }
            if (string.Equals(fieldName, "SpecificKeyHash", StringComparison.Ordinal)) return true;
            MaybeLogUnknown(logger, "Office bounds-sub", name);
            return true;
        }

        private static bool TryApplyTriggerSub(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var triggers = UnityEngine.Object.FindObjectsOfType<OnTriggerSub>();
            SortByPath(triggers);
            if (index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; SuppressTriggerLog(logger, "OnTriggerSub", triggers.Length); return true; }
            if (string.Equals(fieldName, "OnlyOnce", StringComparison.Ordinal)) { trigger.onlyOnce = value != 0; return true; }
            if (string.Equals(fieldName, "Entered", StringComparison.Ordinal)) { SetFieldValue(trigger, "entered", value != 0); return true; }
            if (string.Equals(fieldName, "EnterObject", StringComparison.Ordinal)) { SetObjectActive(trigger.enterGameObject, value != 0); return true; }
            if (string.Equals(fieldName, "SubKeyHash", StringComparison.Ordinal)) return true;
            if (string.Equals(fieldName, "SubTimeMs", StringComparison.Ordinal)) { trigger.subTime = value / 1000f; return true; }
            MaybeLogUnknown(logger, "Office trigger-sub", name);
            return true;
        }

        private static bool TryApplyDisplaySub(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var triggers = UnityEngine.Object.FindObjectsOfType<OnTriggerDisplaySub>();
            SortByPath(triggers);
            if (index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; SuppressTriggerLog(logger, "OnTriggerDisplaySub", triggers.Length); return true; }
            if (string.Equals(fieldName, "OnlyOnce", StringComparison.Ordinal)) { trigger.onlyOnce = value != 0; return true; }
            if (string.Equals(fieldName, "Entered", StringComparison.Ordinal)) { SetFieldValue(trigger, "entered", value != 0); return true; }
            if (string.Equals(fieldName, "EnterObject", StringComparison.Ordinal)) { SetObjectActive(trigger.enterGameObject, value != 0); return true; }
            if (string.Equals(fieldName, "SubKeyHash", StringComparison.Ordinal)) return true;
            MaybeLogUnknown(logger, "Office display-sub", name);
            return true;
        }

        private static bool TryApplyGenericTrigger(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var triggers = UnityEngine.Object.FindObjectsOfType<OnTrigger>();
            SortByPath(triggers);
            if (index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; SuppressTriggerLog(logger, "OnTrigger", triggers.Length); return true; }
            if (string.Equals(fieldName, "OnlyOnce", StringComparison.Ordinal)) { trigger.onlyOnce = value != 0; return true; }
            if (string.Equals(fieldName, "Entered", StringComparison.Ordinal)) { SetFieldValue(trigger, "entered", value != 0); return true; }
            if (string.Equals(fieldName, "EnterObject", StringComparison.Ordinal)) { SetObjectActive(trigger.enterGameObject, value != 0); return true; }
            MaybeLogUnknown(logger, "Office generic-trigger", name);
            return true;
        }

        private static bool TryApplyEventTrigger(string name, int value, ManualLogSource logger)
        {
            if (string.Equals(name, "Count", StringComparison.Ordinal)) return true;
            var index = ParseIndexedName(name, out var fieldName);
            if (index < 0) return true;
            var triggers = UnityEngine.Object.FindObjectsOfType<TriggerEventInvoker>();
            SortByPath(triggers);
            if (index >= triggers.Length) return false;
            var trigger = triggers[index];
            if (trigger == null) return false;

            if (string.Equals(fieldName, "RootActive", StringComparison.Ordinal)) { trigger.gameObject.SetActive(value != 0); return true; }
            if (string.Equals(fieldName, "Enabled", StringComparison.Ordinal)) { trigger.enabled = false; SuppressTriggerLog(logger, "TriggerEventInvoker", triggers.Length); return true; }
            if (string.Equals(fieldName, "OnlyOnce", StringComparison.Ordinal)) { SetFieldValue(trigger, "onlyOnce", value != 0); return true; }
            if (string.Equals(fieldName, "Triggered", StringComparison.Ordinal)) { trigger.isTriggered = value != 0; return true; }
            if (string.Equals(fieldName, "EnterObject", StringComparison.Ordinal)) { SetObjectActive(trigger.enterGameObject, value != 0); return true; }
            MaybeLogUnknown(logger, "Office event-trigger", name);
            return true;
        }

        private static void ApplyCoffeeAnimator(CoffeeMachineManager coffee)
        {
            var animator = GetFieldValue<Animator>(coffee, "coffeeAnimator");
            if (animator == null) return;

            var state = coffee.GetCoffeeMachineState();
            animator.SetBool("OpenPod", state == CoffeeMachineManager.CoffeeMachineState.OpenPod);
            animator.SetBool("AddWater",
                state == CoffeeMachineManager.CoffeeMachineState.AddWater ||
                state == CoffeeMachineManager.CoffeeMachineState.WaterAdded ||
                state == CoffeeMachineManager.CoffeeMachineState.PouringCoffee ||
                coffee.isWaterAdded);
            animator.SetBool("WaterAdded", coffee.isWaterAdded);
            animator.SetBool("PodAdded", coffee.isPodAdded);
        }

        private static int ReadEnumInt(object target, string name)
        {
            var field = GetField(target, name);
            if (field == null) return 0;
            try
            {
                return Convert.ToInt32(field.GetValue(target));
            }
            catch
            {
                return 0;
            }
        }

        private static void ApplyAudioPlayback(AudioSource audio, bool playing)
        {
            if (audio == null) return;
            if (playing)
            {
                if (!audio.isPlaying) audio.Play();
            }
            else if (audio.isPlaying)
            {
                audio.Stop();
            }
        }

        private static int GetAudioTrackIndex(object owner, AudioClip clip)
        {
            if (owner == null || clip == null) return -1;
            var tracks = GetFieldValue<Array>(owner, "tracks");
            if (tracks == null) return -1;

            for (var i = 0; i < tracks.Length; i++)
            {
                var item = tracks.GetValue(i);
                var trackClip = GetFieldValue<AudioClip>(item, "audioClip");
                if (trackClip == clip) return i;
            }

            return -1;
        }

        private static void ApplyAudioTrack(object owner, AudioSource audio, int trackIndex)
        {
            if (owner == null || audio == null || trackIndex < 0) return;
            var tracks = GetFieldValue<Array>(owner, "tracks");
            if (tracks == null || trackIndex >= tracks.Length) return;
            var item = tracks.GetValue(trackIndex);
            var clip = GetFieldValue<AudioClip>(item, "audioClip");
            if (clip == null) return;
            var volume = GetFieldValue<float>(item, "volume");
            audio.clip = clip;
            audio.volume = volume;
        }

        private static void ApplyAudioTime(AudioSource audio, int timeMs)
        {
            if (audio == null || audio.clip == null || timeMs < 0) return;
            var time = Mathf.Clamp(timeMs / 1000f, 0f, Mathf.Max(0f, audio.clip.length - 0.02f));
            if (Mathf.Abs(audio.time - time) > 0.35f)
            {
                audio.time = time;
            }
        }

        private static int GetVideoTimeMs(object videoPlayer)
        {
            if (!TryGetDoubleMember(videoPlayer, "time", out var time) || time < 0d) return 0;
            return Mathf.Clamp(Mathf.RoundToInt((float)(time * 1000d)), 0, int.MaxValue);
        }

        private static void ApplyVideoTime(object videoPlayer, int timeMs)
        {
            if (videoPlayer == null || timeMs < 0) return;
            var target = timeMs / 1000d;
            if (TryGetDoubleMember(videoPlayer, "time", out var current) &&
                Math.Abs(current - target) < 0.35d)
            {
                return;
            }

            SetDoubleMember(videoPlayer, "time", target);
        }

        private static void ApplyWorkerMonitorClip(OfficeWorkerMonitor monitor, object videoPlayer, int clipHash)
        {
            if (monitor == null || videoPlayer == null || clipHash == 0) return;
            var clips = GetFieldObject(monitor, "videoClips") as IList;
            if (clips == null) return;

            for (var i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                if (StableStringHash(GetObjectName(clip)) != clipHash) continue;
                SetMemberValue(videoPlayer, "clip", clip);
                return;
            }
        }

        private static void ApplyWorkerMonitorMaterial(OfficeWorkerMonitor monitor, MeshRenderer meshRenderer, bool materialOn)
        {
            if (meshRenderer == null) return;
            if (!materialOn)
            {
                meshRenderer.material = null;
                return;
            }

            var material = GetFieldValue<Material>(monitor, "tvOnMaterial");
            if (material != null) meshRenderer.material = material;
        }

        private static void ApplyYellowIntroTextMask(YellowIntroManager yellowIntro, int mask)
        {
            var texts = GetFieldValue<YellowTextInstance[]>(yellowIntro, "introTexts");
            if (texts == null) return;
            for (var i = 0; i < texts.Length && i < 30; i++)
            {
                if (texts[i] == null) continue;
                texts[i].Enabled((mask & (1 << i)) != 0);
            }
        }

        private static bool IsYellowTextEnabled(YellowTextInstance text)
        {
            if (text == null) return false;
            var behaviours = text.GetComponents<Behaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null) continue;
                var typeName = behaviour.GetType().FullName ?? string.Empty;
                if (string.Equals(typeName, "TMPro.TMP_Text", StringComparison.Ordinal) ||
                    typeName.EndsWith(".TMP_Text", StringComparison.Ordinal) ||
                    typeName.EndsWith(".TextMeshProUGUI", StringComparison.Ordinal))
                {
                    return behaviour.enabled;
                }
            }

            return false;
        }

        private static float GetFloatMember(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name)) return 0f;
            var field = GetField(target, name);
            if (field != null)
            {
                try
                {
                    return Convert.ToSingle(field.GetValue(target));
                }
                catch
                {
                    return 0f;
                }
            }

            var property = target.GetType().GetProperty(name, FieldFlags);
            if (property == null || !property.CanRead) return 0f;
            try
            {
                return Convert.ToSingle(property.GetValue(target, null));
            }
            catch
            {
                return 0f;
            }
        }

        private static void SetFloatMember(object target, string name, float value)
        {
            if (target == null || string.IsNullOrEmpty(name)) return;
            var field = GetField(target, name);
            if (field != null)
            {
                try
                {
                    field.SetValue(target, value);
                }
                catch
                {
                }
                return;
            }

            var property = target.GetType().GetProperty(name, FieldFlags);
            if (property == null || !property.CanWrite) return;
            try
            {
                property.SetValue(target, value, null);
            }
            catch
            {
            }
        }

        private static int ParseIndexedName(string name, out string fieldName)
        {
            fieldName = string.Empty;
            if (string.IsNullOrEmpty(name)) return -1;
            var dot = name.IndexOf('.');
            if (dot <= 0 || dot >= name.Length - 1) return -1;
            if (!int.TryParse(name.Substring(0, dot), out var index)) return -1;
            fieldName = name.Substring(dot + 1);
            return index;
        }

        private static int Quantize(float value)
        {
            return Mathf.RoundToInt(value * 1000f);
        }

        private static float Dequantize(int value)
        {
            return value / 1000f;
        }

        private static int Quantize01(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 1000f);
        }

        private static float Dequantize01(int value)
        {
            return Mathf.Clamp01(value / 1000f);
        }

        private static void EmitRect(string prefix, RectTransform rect, Action<string, int> emit, ref int hash)
        {
            if (rect == null)
            {
                Emit(prefix + "Exists", 0, emit, ref hash);
                return;
            }

            Emit(prefix + "Exists", 1, emit, ref hash);
            Emit(prefix + "LocalX", Quantize(rect.localPosition.x), emit, ref hash);
            Emit(prefix + "LocalY", Quantize(rect.localPosition.y), emit, ref hash);
            Emit(prefix + "LocalZ", Quantize(rect.localPosition.z), emit, ref hash);
            Emit(prefix + "ScaleX", Quantize(rect.localScale.x), emit, ref hash);
            Emit(prefix + "ScaleY", Quantize(rect.localScale.y), emit, ref hash);
            Emit(prefix + "ScaleZ", Quantize(rect.localScale.z), emit, ref hash);
        }

        private static bool TryApplyRect(string name, string prefix, RectTransform rect, int value)
        {
            if (string.IsNullOrEmpty(name) ||
                string.IsNullOrEmpty(prefix) ||
                !name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (rect == null) return true;
            var field = name.Substring(prefix.Length);
            if (string.Equals(field, "Exists", StringComparison.Ordinal)) return true;

            var localPosition = rect.localPosition;
            var localScale = rect.localScale;
            if (string.Equals(field, "LocalX", StringComparison.Ordinal)) localPosition.x = Dequantize(value);
            else if (string.Equals(field, "LocalY", StringComparison.Ordinal)) localPosition.y = Dequantize(value);
            else if (string.Equals(field, "LocalZ", StringComparison.Ordinal)) localPosition.z = Dequantize(value);
            else if (string.Equals(field, "ScaleX", StringComparison.Ordinal)) localScale.x = Dequantize(value);
            else if (string.Equals(field, "ScaleY", StringComparison.Ordinal)) localScale.y = Dequantize(value);
            else if (string.Equals(field, "ScaleZ", StringComparison.Ordinal)) localScale.z = Dequantize(value);
            else return true;

            rect.localPosition = localPosition;
            rect.localScale = localScale;
            return true;
        }

        private static int GetSiblingIndex(object target)
        {
            var go = GetGameObject(target);
            if (go == null || go.transform == null) return -1;
            return go.transform.GetSiblingIndex();
        }

        private static void SetSiblingIndex(object target, int index)
        {
            var go = GetGameObject(target);
            if (go == null || go.transform == null || go.transform.parent == null) return;
            var max = Mathf.Max(0, go.transform.parent.childCount - 1);
            go.transform.SetSiblingIndex(Mathf.Clamp(index, 0, max));
        }

        private static int GetTextLength(object textOwner)
        {
            var value = GetMemberValue(textOwner, "text");
            return value is string text ? text.Length : 0;
        }

        private static void ApplyKnownTextPrefix(object inputField, object textField, string fullText, int length, bool completed)
        {
            if (string.IsNullOrEmpty(fullText)) fullText = string.Empty;
            var safeLength = Mathf.Clamp(length, 0, fullText.Length);
            var value = fullText.Substring(0, safeLength);
            SetMemberValue(inputField, "text", value);
            SetMemberValue(textField, "text", value);
            if (completed || safeLength >= fullText.Length)
            {
                SetMemberValue(inputField, "interactable", false);
            }
        }

        private static bool TryApplyIconMask(string name, ComputerManager computer, int value)
        {
            if (string.Equals(name, "IconCount", StringComparison.Ordinal)) return true;
            if (!string.Equals(name, "IconRootMask", StringComparison.Ordinal) &&
                !string.Equals(name, "IconShadowMask", StringComparison.Ordinal) &&
                !string.Equals(name, "IconOutlineMask", StringComparison.Ordinal))
            {
                return false;
            }

            var icons = GetFieldValue<List<ComputerIcon>>(computer, "icons");
            if (icons == null) return true;
            for (var i = 0; i < icons.Count && i < 30; i++)
            {
                var icon = icons[i];
                if (icon == null) continue;
                var active = (value & (1 << i)) != 0;
                if (string.Equals(name, "IconRootMask", StringComparison.Ordinal)) SetObjectActive(icon.gameObject, active);
                else if (string.Equals(name, "IconShadowMask", StringComparison.Ordinal)) SetObjectActive(icon.whiteShadowGO, active);
                else if (string.Equals(name, "IconOutlineMask", StringComparison.Ordinal)) SetObjectActive(icon.whiteOutlineGO, active);
            }

            return true;
        }

        private static void SortByPath<T>(T[] items) where T : Component
        {
            if (items == null || items.Length <= 1) return;
            Array.Sort(items, (a, b) =>
            {
                var left = a != null ? NetPath.GetPath(a.transform) : string.Empty;
                var right = b != null ? NetPath.GetPath(b.transform) : string.Empty;
                return string.CompareOrdinal(left, right);
            });
        }

        private static void SuppressLog(ManualLogSource logger)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextSuppressLogMs)
            {
                _nextSuppressLogMs = nowMs + 10000;
                logger.LogInfo("Office scene-state client brain suppressed coffee machine");
            }
        }

        private static void SuppressBoundsSubLog(ManualLogSource logger, int count)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextSuppressLogMs)
            {
                _nextSuppressLogMs = nowMs + 10000;
                logger.LogInfo("Office scene-state client brain suppressed bounds-sub triggers=" + count);
            }
        }

        private static void SuppressTriggerLog(ManualLogSource logger, string typeName, int count)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextSuppressLogMs)
            {
                _nextSuppressLogMs = nowMs + 10000;
                logger.LogInfo("Office scene-state client brain suppressed " + typeName + " triggers=" + count);
            }
        }

        private static void SuppressWorkerMonitorLog(ManualLogSource logger, int count)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextSuppressLogMs)
            {
                _nextSuppressLogMs = nowMs + 10000;
                logger.LogInfo("Office scene-state client brain suppressed worker monitors=" + count);
            }
        }

        private static void SuppressYellowIntroLog(ManualLogSource logger)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (logger != null && nowMs >= _nextSuppressLogMs)
            {
                _nextSuppressLogMs = nowMs + 10000;
                logger.LogInfo("Office scene-state client brain suppressed yellow intro manager");
            }
        }

        private static void MaybeLogUnknown(ManualLogSource logger, string area, string name)
        {
            if (logger == null) return;
            logger.LogDebug(area + " sync ignored key=" + name);
        }

        private static bool IsObjectActive(object target)
        {
            var go = GetGameObject(target);
            return go != null && go.activeSelf;
        }

        private static void SetObjectActive(object target, bool active)
        {
            var go = GetGameObject(target);
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }

        private static GameObject GetGameObject(object target)
        {
            if (target is GameObject go) return go;
            if (target is Component component) return component.gameObject;
            return null;
        }

        private static object GetFieldObject(object target, string name)
        {
            return GetFieldValue<object>(target, name);
        }

        private static object GetMemberValue(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name)) return null;
            var field = GetField(target, name);
            if (field != null)
            {
                try { return field.GetValue(target); }
                catch { return null; }
            }

            var property = target.GetType().GetProperty(name, FieldFlags);
            if (property == null || !property.CanRead) return null;
            try { return property.GetValue(target, null); }
            catch { return null; }
        }

        private static void SetMemberValue(object target, string name, object value)
        {
            if (target == null || string.IsNullOrEmpty(name)) return;
            var field = GetField(target, name);
            if (field != null)
            {
                try { field.SetValue(target, value); }
                catch { }
                return;
            }

            var property = target.GetType().GetProperty(name, FieldFlags);
            if (property == null || !property.CanWrite) return;
            try { property.SetValue(target, value, null); }
            catch { }
        }

        private static bool GetBoolMember(object target, string name)
        {
            var value = GetMemberValue(target, name);
            return value is bool boolValue && boolValue;
        }

        private static bool TryGetDoubleMember(object target, string name, out double value)
        {
            value = 0d;
            var memberValue = GetMemberValue(target, name);
            if (memberValue == null) return false;
            try
            {
                value = Convert.ToDouble(memberValue);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SetDoubleMember(object target, string name, double value)
        {
            SetMemberValue(target, name, value);
        }

        private static void InvokeNoArg(object target, string methodName)
        {
            if (target == null || string.IsNullOrEmpty(methodName)) return;
            try
            {
                var method = target.GetType().GetMethod(methodName, FieldFlags);
                if (method != null) method.Invoke(target, null);
            }
            catch
            {
            }
        }

        private static string GetObjectName(object target)
        {
            if (target is UnityEngine.Object unityObject) return unityObject.name ?? string.Empty;
            return target != null ? target.ToString() : string.Empty;
        }

        private static T GetFieldValue<T>(object target, string name)
        {
            var field = GetField(target, name);
            if (field == null) return default(T);
            var value = field.GetValue(target);
            if (value is T typed) return typed;
            return default(T);
        }

        private static void SetFieldValue(object target, string name, object value)
        {
            var field = GetField(target, name);
            if (field == null) return;

            try
            {
                if (field.FieldType.IsEnum && value is int intValue)
                {
                    field.SetValue(target, Enum.ToObject(field.FieldType, intValue));
                    return;
                }

                field.SetValue(target, value);
            }
            catch
            {
            }
        }

        private static FieldInfo GetField(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name)) return null;
            var key = target.GetType().FullName + "." + name;
            if (FieldCache.TryGetValue(key, out var cached)) return cached;
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(name, FieldFlags | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    FieldCache[key] = field;
                    return field;
                }

                type = type.BaseType;
            }

            FieldCache[key] = null;
            return null;
        }

        private static int StableStringHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                if (!string.IsNullOrEmpty(value))
                {
                    for (var i = 0; i < value.Length; i++)
                    {
                        hash ^= char.ToUpperInvariant(value[i]);
                        hash *= 16777619u;
                    }
                }

                return (int)hash;
            }
        }

        private static void Emit(string key, int value, Action<string, int> emit, ref int hash)
        {
            emit(key, value);
            unchecked
            {
                hash = hash * 31 + (key != null ? key.GetHashCode() : 0);
                hash = hash * 31 + value;
            }
        }
    }
}
