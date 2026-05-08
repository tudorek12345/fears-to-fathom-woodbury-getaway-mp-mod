using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace WoodburySpectatorSync.Coop
{
    internal static class CabinBoardGameSync
    {
        public const string KeyPrefix = "BoardGame.";

        private const string JengaPrefix = KeyPrefix + "Jenga.";
        private const string JengaActivePrefix = JengaPrefix + "Active.";
        private const string JengaPiecePrefix = JengaPrefix + "Piece.";
        private const string JengaMiniPrefix = KeyPrefix + "JengaMini.";
        private const string JengaMiniActivePrefix = JengaMiniPrefix + "Active.";
        private const string OuijaPrefix = KeyPrefix + "Ouija.";
        private const string OuijaActivePrefix = OuijaPrefix + "Active.";
        private const string MikeOuijaPrefix = KeyPrefix + "MikeOuija.";
        private const string MikeOuijaActivePrefix = MikeOuijaPrefix + "Active.";
        private const int MaskChunkBits = 27;
        private const int TransformScale = 1000;
        private const int LogIntervalMs = 5000;
        private static readonly BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Dictionary<string, long> LastApplyLogMsByKey = new Dictionary<string, long>();
        private static readonly Dictionary<string, long> LastSuppressLogMsByKey = new Dictionary<string, long>();

        public static int EmitHostFlags(CabinGameManager manager, string fullPrefix, Action<string, int> emit)
        {
            if (manager == null || emit == null)
            {
                return 0;
            }

            var hash = 17;
            var player = ResolveCabinPlayer(manager);
            var jenga = ResolveJengaController(manager);
            var mini = ResolveJengaMiniGameController(manager, jenga);
            var ouija = ResolveOuijaController(manager, player);
            var mike = ResolveMikeController(manager);

            EmitObjectActive(fullPrefix, JengaActivePrefix + "sittingPlayerBoardGame", player, "sittingPlayerBoardGame", emit, ref hash);
            EmitObjectActive(fullPrefix, JengaActivePrefix + "sittingCamJengaBoardGame", player, "sittingCamJengaBoardGame", emit, ref hash);
            EmitObjectActive(fullPrefix, JengaActivePrefix + "fovZoomJengaBoardGame", player, "fovZoomJengaBoardGame", emit, ref hash);
            EmitObjectActive(fullPrefix, JengaActivePrefix + "jengaPieces", player, "jengaPieces", emit, ref hash);
            EmitObjectActive(fullPrefix, JengaActivePrefix + "triggerSubGetJenga", player, "triggerSubGetJenga", emit, ref hash);
            EmitObjectActive(fullPrefix, JengaActivePrefix + "diningTableCollider", player, "diningTableCollider", emit, ref hash);
            EmitInt(fullPrefix, JengaPrefix + "diningTableCollidersMask", BuildColliderMask(GetFieldObject(player, "diningTableColliders")), emit, ref hash);
            EmitBool(fullPrefix, JengaPrefix + "playerIsSeatedWithJenga", GetBool(player, "playerIsSeatedWithJenga"), emit, ref hash);
            EmitBool(fullPrefix, JengaPrefix + "hasWonJenga", GetBool(player, "hasWonJenga"), emit, ref hash);
            EmitBool(fullPrefix, JengaPrefix + "canPlaceJenga", GetBool(player, "CanPlaceJenga"), emit, ref hash);
            EmitInt(fullPrefix, JengaPrefix + "playerBoardGameType", GetEnumOrInt(player, "currentHoldingBoardGameType"), emit, ref hash);
            EmitObjectActive(fullPrefix, OuijaActivePrefix + "sittingPlayerOuijaBoardGame", player, "sittingPlayerOuijaBoardGame", emit, ref hash);
            EmitObjectActive(fullPrefix, OuijaActivePrefix + "sittingCamOuijaBoardGame", player, "sittingCamOuijaBoardGame", emit, ref hash);
            EmitObjectActive(fullPrefix, OuijaActivePrefix + "fovZoomOuijaBoardGame", player, "fovZoomOuijaBoardGame", emit, ref hash);
            EmitObjectActive(fullPrefix, OuijaActivePrefix + "ouijaBoardGame", player, "ouijaBoardGame", emit, ref hash);
            EmitObjectActive(fullPrefix, OuijaActivePrefix + "basementTable", player, "basementTable", emit, ref hash);
            EmitObjectActive(fullPrefix, OuijaActivePrefix + "basementTableLight", player, "basementTableLight", emit, ref hash);
            EmitObjectActive(fullPrefix, OuijaActivePrefix + "triggerSubGetOuija", player, "triggerSubGetOuija", emit, ref hash);
            EmitObjectActive(fullPrefix, OuijaActivePrefix + "triggerSubTurnOffLights", player, "triggerSubTurnOffLights", emit, ref hash);
            EmitObjectActive(fullPrefix, MikeOuijaActivePrefix + "ouijaTable", mike, "ouijaTable", emit, ref hash);
            EmitInt(fullPrefix, MikeOuijaPrefix + "ouijaTableParent", ResolveMikeOuijaTableParentMode(mike), emit, ref hash);
            EmitInt(fullPrefix, MikeOuijaPrefix + "tableHoldRigWeight", Quantize(GetWeight(GetFieldObject(mike, "tableHoldRig"))), emit, ref hash);

            if (jenga != null)
            {
                EmitInt(fullPrefix, JengaPrefix + "rootActive", IsActive(jenga.gameObject), emit, ref hash);
                EmitInt(fullPrefix, JengaPrefix + "enabled", jenga.enabled ? 1 : 0, emit, ref hash);
                EmitInt(fullPrefix, JengaPrefix + "currentState", GetEnumOrInt(jenga, "currentState"), emit, ref hash);
                EmitBool(fullPrefix, JengaPrefix + "isInteractable", GetBool(jenga, "isInteractable"), emit, ref hash);
                EmitBool(fullPrefix, JengaPrefix + "hasSelectedBlock", GetBool(jenga, "hasSelectedBlock"), emit, ref hash);
                EmitBool(fullPrefix, JengaPrefix + "isMiniGameDirectionRight", GetBool(jenga, "isMiniGameDirectionRight"), emit, ref hash);
                EmitInt(fullPrefix, JengaPrefix + "selectedPieceIndex", GetSelectedPieceIndex(jenga), emit, ref hash);
                EmitObjectActive(fullPrefix, JengaActivePrefix + "diningTableCollider", jenga, "diningTableCollider", emit, ref hash);
                EmitJengaPieceMasks(fullPrefix, jenga, emit, ref hash);
            }

            if (mini != null)
            {
                EmitInt(fullPrefix, JengaMiniPrefix + "rootActive", IsActive(mini.gameObject), emit, ref hash);
                EmitInt(fullPrefix, JengaMiniPrefix + "enabled", mini.enabled ? 1 : 0, emit, ref hash);
                EmitInt(fullPrefix, JengaMiniPrefix + "currentLevelIndex", GetMiniCurrentLevelIndex(mini), emit, ref hash);
                EmitBool(fullPrefix, JengaMiniPrefix + "isRightOriented", GetBool(mini, "isRightOriented"), emit, ref hash);
                EmitBool(fullPrefix, JengaMiniPrefix + "hasMiniGameStarted", GetBool(mini, "hasMiniGameStarted"), emit, ref hash);
                EmitBool(fullPrefix, JengaMiniPrefix + "gameLost", GetBool(mini, "gameLost"), emit, ref hash);
                EmitBool(fullPrefix, JengaMiniPrefix + "hasWon", GetBool(mini, "hasWon"), emit, ref hash);
                EmitInt(fullPrefix, JengaMiniPrefix + "collectiblesRemaining", GetInt(mini, "collectiblesRemaining"), emit, ref hash);
                EmitObjectActive(fullPrefix, JengaMiniActivePrefix + "fillSprite", mini, "fillSprite", emit, ref hash);
                EmitObjectActive(fullPrefix, JengaMiniActivePrefix + "dotted", GetFieldObject(mini, "currentLevel"), "dotted", emit, ref hash);
                EmitObjectActive(fullPrefix, JengaMiniActivePrefix + "completeFill", GetFieldObject(mini, "currentLevel"), "completeFill", emit, ref hash);
                EmitObjectActive(fullPrefix, JengaMiniActivePrefix + "cursor", mini, "cursor", emit, ref hash);
                EmitBool(fullPrefix, JengaMiniPrefix + "cursorDragging", GetBool(GetFieldObject(mini, "cursor"), "IsDragging"), emit, ref hash);
                EmitMiniCollectibleMasks(fullPrefix, mini, emit, ref hash);
            }

            if (ouija != null)
            {
                var initialPosition = GetVector3(ouija, "initialPosition");
                EmitInt(fullPrefix, OuijaPrefix + "initialPositionX", Quantize(initialPosition.x), emit, ref hash);
                EmitInt(fullPrefix, OuijaPrefix + "initialPositionY", Quantize(initialPosition.y), emit, ref hash);
                EmitInt(fullPrefix, OuijaPrefix + "initialPositionZ", Quantize(initialPosition.z), emit, ref hash);
                EmitBool(fullPrefix, OuijaPrefix + "dragAudioMuted", GetAudioMuted(GetFieldObject(ouija, "dragSFXAudioSource")), emit, ref hash);
                EmitInt(fullPrefix, OuijaPrefix + "dragAudioVolume", Quantize(GetAudioVolume(GetFieldObject(ouija, "dragSFXAudioSource"))), emit, ref hash);
                EmitBool(fullPrefix, OuijaPrefix + "canTakeMouseInput", GetBool(ouija, "canTakeMouseInput"), emit, ref hash);
                EmitInt(fullPrefix, OuijaPrefix + "randomPositionMoveTimer", Quantize(GetFloat(ouija, "randomPositionMoveTimer")), emit, ref hash);
                EmitInt(fullPrefix, OuijaPrefix + "moveToYesPointTimer", Quantize(GetFloat(ouija, "moveToYesPointTimer")), emit, ref hash);
                EmitInt(fullPrefix, OuijaPrefix + "roundTimer", Quantize(GetFloat(ouija, "roundTimer")), emit, ref hash);
                EmitBool(fullPrefix, OuijaPrefix + "startRound", GetBool(ouija, "startRound"), emit, ref hash);
                EmitInt(fullPrefix, OuijaPrefix + "roundIndex", GetInt(ouija, "roundIndex"), emit, ref hash);
                EmitBool(fullPrefix, OuijaPrefix + "movingToYesPoint", GetBool(ouija, "movingToYesPoint"), emit, ref hash);
                EmitBool(fullPrefix, OuijaPrefix + "hasStartedMovingToYes", GetBool(ouija, "hasStartedMovingToYes"), emit, ref hash);
                EmitBool(fullPrefix, OuijaPrefix + "hasReachedYesPoint", GetBool(ouija, "hasReachedYesPoint"), emit, ref hash);
                EmitBool(fullPrefix, OuijaPrefix + "startMoveYesTimer", GetBool(ouija, "startMoveYesTimer"), emit, ref hash);
                EmitBool(fullPrefix, OuijaPrefix + "canMoveToNextTarget", GetBool(ouija, "canMoveToNextTarget"), emit, ref hash);
                EmitBool(fullPrefix, OuijaPrefix + "canRandomlyMove", GetBool(ouija, "canRandomlyMove"), emit, ref hash);
                EmitBool(fullPrefix, OuijaPrefix + "hasReachedRandomPoint", GetBool(ouija, "hasReachedRandomPoint"), emit, ref hash);
                EmitInt(fullPrefix, OuijaPrefix + "mouseX", Quantize(GetFloat(ouija, "mouseX")), emit, ref hash);
                EmitInt(fullPrefix, OuijaPrefix + "mouseY", Quantize(GetFloat(ouija, "mouseY")), emit, ref hash);
                EmitBool(fullPrefix, OuijaPrefix + "parentIsMoving", GetBool(ouija, "parentIsMoving"), emit, ref hash);
                EmitInt(fullPrefix, OuijaPrefix + "targetCode", BuildOuijaTargetCode(ouija), emit, ref hash);
            }

            return hash;
        }

        public static void CollectSyncedTransforms(CabinGameManager manager, List<Transform> transforms)
        {
            if (manager == null || transforms == null)
            {
                return;
            }

            var seq = manager.CurrentSequence;
            if (seq == SequenceType.PlayingJenga)
            {
                var player = ResolveCabinPlayer(manager);
                AddTransform(GetFieldObject(player, "jengaPieces"), transforms);
                var jenga = ResolveJengaController(manager);
                var pieces = GetJengaPieces(jenga);
                for (var i = 0; i < pieces.Count; i++)
                {
                    var piece = pieces[i];
                    if (piece != null && (piece.gameObject.activeSelf || piece.HasMovedOut || !GetRigidbodyKinematic(piece)))
                    {
                        AddTransform(piece.transform, transforms);
                    }
                }

                var mini = ResolveJengaMiniGameController(manager, jenga);
                AddTransform(GetFieldObject(mini, "currentLevel"), transforms);
                AddTransform(GetFieldObject(mini, "cursor"), transforms);
            }

            if (seq == SequenceType.GoingToPlayOuija || seq == SequenceType.PlayingOuija)
            {
                var player = ResolveCabinPlayer(manager);
                var ouija = ResolveOuijaController(manager, player);
                var mike = ResolveMikeController(manager);
                AddTransform(GetFieldObject(player, "ouijaBoardGame"), transforms);
                AddTransform(GetFieldObject(player, "basementTable"), transforms);
                AddTransform(GetFieldObject(player, "basementTableLight"), transforms);
                AddTransform(GetFieldObject(mike, "ouijaTable"), transforms);
                AddTransform(GetFieldObject(ouija, "parentTransform"), transforms);
                AddTransform(GetFieldObject(ouija, "planchetteTransform"), transforms);
            }
        }

        public static bool TryApplyFlag(CabinGameManager manager, string fieldName, int value, ManualLogSource logger)
        {
            if (manager == null || string.IsNullOrEmpty(fieldName) || !fieldName.StartsWith(KeyPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            var player = ResolveCabinPlayer(manager);
            var jenga = ResolveJengaController(manager);
            var mini = ResolveJengaMiniGameController(manager, jenga);
            var ouija = ResolveOuijaController(manager, player);
            var applied = false;

            if (fieldName.StartsWith(JengaActivePrefix, StringComparison.Ordinal))
            {
                applied = ApplyJengaActiveFlag(player, jenga, fieldName.Substring(JengaActivePrefix.Length), value);
            }
            else if (fieldName.StartsWith(JengaPiecePrefix, StringComparison.Ordinal))
            {
                applied = ApplyJengaPieceFlag(jenga, fieldName.Substring(JengaPiecePrefix.Length), value, logger);
            }
            else if (fieldName.StartsWith(JengaMiniActivePrefix, StringComparison.Ordinal))
            {
                applied = ApplyJengaMiniActiveFlag(mini, fieldName.Substring(JengaMiniActivePrefix.Length), value);
            }
            else if (fieldName.StartsWith(JengaMiniPrefix, StringComparison.Ordinal))
            {
                applied = ApplyJengaMiniFlag(mini, fieldName.Substring(JengaMiniPrefix.Length), value, logger);
            }
            else if (fieldName.StartsWith(JengaPrefix, StringComparison.Ordinal))
            {
                applied = ApplyJengaFlag(player, jenga, fieldName.Substring(JengaPrefix.Length), value, logger);
            }
            else if (fieldName.StartsWith(OuijaActivePrefix, StringComparison.Ordinal))
            {
                applied = ApplyOuijaActiveFlag(player, fieldName.Substring(OuijaActivePrefix.Length), value);
            }
            else if (fieldName.StartsWith(MikeOuijaActivePrefix, StringComparison.Ordinal))
            {
                applied = ApplyMikeOuijaActiveFlag(ResolveMikeController(manager), fieldName.Substring(MikeOuijaActivePrefix.Length), value);
            }
            else if (fieldName.StartsWith(MikeOuijaPrefix, StringComparison.Ordinal))
            {
                applied = ApplyMikeOuijaFlag(ResolveMikeController(manager), fieldName.Substring(MikeOuijaPrefix.Length), value);
            }
            else if (fieldName.StartsWith(OuijaPrefix, StringComparison.Ordinal))
            {
                applied = ApplyOuijaFlag(ouija, fieldName.Substring(OuijaPrefix.Length), value);
            }

            if (applied)
            {
                SuppressLocalBoardGameBrains(manager, logger);
                MaybeLog(logger, LastApplyLogMsByKey, fieldName, "Cabin board game client apply key=" + fieldName + " value=" + value);
            }

            return applied;
        }

        public static void ApplyClientPhaseVisuals(CabinGameManager manager, ManualLogSource logger)
        {
            if (manager == null)
            {
                return;
            }

            SuppressLocalBoardGameBrains(manager, logger);

            var seq = manager.CurrentSequence;
            if (seq == SequenceType.GoingToPlayOuija || seq == SequenceType.PlayingOuija)
            {
                SetFieldValue(manager, "mikeHasPlacedBasementTable", true);
                SetFieldValue(manager, "isPlayingOuija", seq == SequenceType.PlayingOuija);
                var player = ResolveCabinPlayer(manager);
                SetActive(GetFieldObject(player, "ouijaBoardGame"), true);
                SetActive(GetFieldObject(player, "basementTable"), true);
                SetActive(GetFieldObject(player, "basementTableLight"), true);
                SetActive(GetFieldObject(player, "triggerSubGetOuija"), false);
                SetActive(GetFieldObject(player, "triggerSubTurnOffLights"), false);
                SetEnabledByReflection(GetFieldObject(player, "basementTableCollider"), false);
                ApplyOuijaSpectatorCamera(player, seq == SequenceType.PlayingOuija);
                return;
            }

            SetFieldValue(manager, "isPlayingOuija", false);
            ApplyOuijaSpectatorCamera(ResolveCabinPlayer(manager), false);
        }

        public static bool IsHighFrequencyStoryFlag(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            return fieldName.StartsWith(JengaMiniPrefix + "cursor", StringComparison.Ordinal)
                || fieldName.StartsWith(JengaMiniPrefix + "collectiblesRemaining", StringComparison.Ordinal)
                || fieldName.StartsWith(OuijaPrefix + "dragAudioVolume", StringComparison.Ordinal)
                || fieldName.StartsWith(OuijaPrefix + "randomPositionMoveTimer", StringComparison.Ordinal)
                || fieldName.StartsWith(OuijaPrefix + "moveToYesPointTimer", StringComparison.Ordinal)
                || fieldName.StartsWith(OuijaPrefix + "roundTimer", StringComparison.Ordinal)
                || fieldName.StartsWith(OuijaPrefix + "mouseX", StringComparison.Ordinal)
                || fieldName.StartsWith(OuijaPrefix + "mouseY", StringComparison.Ordinal);
        }

        public static string BuildOverlaySummary(CabinGameManager manager)
        {
            if (manager == null)
            {
                return "MiniGame: -";
            }

            var jenga = ResolveJengaController(manager);
            var mini = ResolveJengaMiniGameController(manager, jenga);
            var player = ResolveCabinPlayer(manager);
            var ouija = ResolveOuijaController(manager, player);

            var jengaState = jenga != null ? GetJengaStateName(GetEnumOrInt(jenga, "currentState")) : "-";
            var selectedIndex = jenga != null ? GetSelectedPieceIndex(jenga) : -1;
            var miniState = BuildMiniStatus(mini);
            var round = ouija != null ? GetInt(ouija, "roundIndex") : -1;
            var target = ouija != null ? BuildOuijaTargetCode(ouija).ToString() : "-";
            var moving = ouija != null && (GetBool(ouija, "parentIsMoving") || GetBool(ouija, "canRandomlyMove") || GetBool(ouija, "movingToYesPoint"));

            return "MiniGame: Jenga=" + jengaState + " sel=" + selectedIndex + " mini=" + miniState
                + " Ouija=round:" + round + " target:" + target + " moving:" + (moving ? "yes" : "no");
        }

        public static void SuppressLocalBoardGameBrains(CabinGameManager manager, ManualLogSource logger)
        {
            if (manager == null)
            {
                return;
            }

            var player = ResolveCabinPlayer(manager);
            var jenga = ResolveJengaController(manager);
            var mini = ResolveJengaMiniGameController(manager, jenga);
            var ouija = ResolveOuijaController(manager, player);

            if (jenga != null)
            {
                jenga.enabled = false;
                SetFieldValue(jenga, "isInteractable", false);
                var pieces = GetJengaPieces(jenga);
                for (var i = 0; i < pieces.Count; i++)
                {
                    var piece = pieces[i];
                    if (piece == null)
                    {
                        continue;
                    }

                    SetFieldValue(piece, "isInteractable", false);
                    SetRigidbodyKinematic(piece, true);
                }

                MaybeLog(logger, LastSuppressLogMsByKey, "jenga", "Cabin board game client suppress local Jenga brain");
            }

            if (mini != null)
            {
                mini.enabled = false;
                SetInputEnabled(GetFieldObject(mini, "scratchCard"), false);
                var cursor = GetFieldObject(mini, "cursor");
                SetEnabledByReflection(cursor, false);
                SetBoolProperty(cursor, "IsDragging", false);
                SetEnabledOnComponentsNamed(cursor, "Collider", false);
                MuteAudio(GetFieldObject(cursor, "audioSource"));
                MaybeLog(logger, LastSuppressLogMsByKey, "jenga-mini", "Cabin board game client suppress local Jenga mini-game brain");
            }

            if (ouija != null)
            {
                ouija.enabled = false;
                MuteAudio(GetFieldObject(ouija, "dragSFXAudioSource"));
                MaybeLog(logger, LastSuppressLogMsByKey, "ouija", "Cabin board game client suppress local Ouija brain");
            }
        }

        private static bool ApplyJengaActiveFlag(CabinPlayerController player, JengaController jenga, string key, int value)
        {
            if (key == "diningTableCollider")
            {
                return TrySetFieldObjectActive(jenga, key, value != 0) || TrySetFieldObjectActive(player, key, value != 0);
            }

            return TrySetFieldObjectActive(player, key, value != 0);
        }

        private static bool ApplyJengaFlag(CabinPlayerController player, JengaController jenga, string key, int value, ManualLogSource logger)
        {
            if (key == "pieceCount")
            {
                var localCount = jenga != null ? GetJengaPieces(jenga).Count : 0;
                if (localCount != value)
                {
                    MaybeLog(logger, LastApplyLogMsByKey, "jenga-piece-count",
                        "Cabin board game Jenga piece count differs host=" + value + " local=" + localCount);
                }

                return true;
            }

            if (key == "rootActive")
            {
                if (jenga == null)
                {
                    return value == 0;
                }

                jenga.gameObject.SetActive(value != 0);
                return true;
            }

            if (key == "enabled")
            {
                if (jenga == null)
                {
                    return value == 0;
                }

                jenga.enabled = false;
                return true;
            }

            if (jenga != null)
            {
                if (key == "currentState")
                {
                    SetFieldValue(jenga, "currentState", value);
                    return true;
                }

                if (key == "isInteractable" || key == "hasSelectedBlock" || key == "isMiniGameDirectionRight")
                {
                    SetFieldValue(jenga, key, value != 0);
                    return true;
                }

                if (key == "selectedPieceIndex")
                {
                    ApplySelectedPiece(jenga, value);
                    return true;
                }
            }

            if (player != null)
            {
                if (key == "playerIsSeatedWithJenga" || key == "hasWonJenga" || key == "canPlaceJenga")
                {
                    SetFieldValue(player, key == "canPlaceJenga" ? "CanPlaceJenga" : key, value != 0);
                    return true;
                }

                if (key == "playerBoardGameType")
                {
                    SetFieldValue(player, "currentHoldingBoardGameType", value);
                    return true;
                }

                if (key == "diningTableCollidersMask")
                {
                    ApplyColliderMask(GetFieldObject(player, "diningTableColliders"), value);
                    return true;
                }
            }

            return false;
        }

        private static bool ApplyJengaPieceFlag(JengaController jenga, string key, int value, ManualLogSource logger)
        {
            if (jenga == null)
            {
                return false;
            }

            var chunk = key.EndsWith("1", StringComparison.Ordinal) ? 1 : 0;
            var baseKey = key.Substring(0, key.Length - 1);
            var pieces = GetJengaPieces(jenga);
            var applied = false;

            for (var bit = 0; bit < MaskChunkBits; bit++)
            {
                var index = chunk * MaskChunkBits + bit;
                if (index >= pieces.Count)
                {
                    break;
                }

                var piece = pieces[index];
                if (piece == null)
                {
                    continue;
                }

                var on = (value & (1 << bit)) != 0;
                if (baseKey == "Active")
                {
                    piece.gameObject.SetActive(on);
                    applied = true;
                }
                else if (baseKey == "MovedOut")
                {
                    piece.HasMovedOut = on;
                    applied = true;
                }
                else if (baseKey == "Selected")
                {
                    SetFieldValue(piece, "isSelected", on);
                    applied = true;
                }
                else if (baseKey == "Kinematic")
                {
                    SetRigidbodyKinematic(piece, on);
                    applied = true;
                }
            }

            if (applied)
            {
                MaybeLog(logger, LastApplyLogMsByKey, "jenga-piece-" + baseKey, "Cabin board game Jenga mask apply mask=" + baseKey + " chunk=" + chunk + " value=" + value);
            }

            return applied;
        }

        private static bool ApplyJengaMiniActiveFlag(JengaDragMiniGameController mini, string key, int value)
        {
            if (mini == null)
            {
                return value == 0;
            }

            if (key == "dotted" || key == "completeFill")
            {
                var currentLevel = GetFieldObject(mini, "currentLevel");
                if (currentLevel == null)
                {
                    return value == 0;
                }

                return TrySetFieldObjectActive(currentLevel, key, value != 0);
            }

            return TrySetFieldObjectActive(mini, key, value != 0);
        }

        private static bool ApplyJengaMiniFlag(JengaDragMiniGameController mini, string key, int value, ManualLogSource logger)
        {
            if (mini == null)
            {
                return false;
            }

            if (key == "rootActive")
            {
                mini.gameObject.SetActive(value != 0);
                return true;
            }

            if (key == "enabled")
            {
                mini.enabled = false;
                return true;
            }

            if (key == "currentLevelIndex")
            {
                ApplyMiniCurrentLevel(mini, value);
                return true;
            }

            if (key == "isRightOriented" || key == "hasMiniGameStarted" || key == "gameLost" || key == "hasWon")
            {
                SetFieldValue(mini, key, value != 0);
                return true;
            }

            if (key == "collectiblesRemaining")
            {
                SetFieldValue(mini, key, value);
                return true;
            }

            if (key == "cursorDragging")
            {
                SetBoolProperty(GetFieldObject(mini, "cursor"), "IsDragging", value != 0);
                return true;
            }

            if (key == "CollectibleActive0" || key == "CollectibleActive1")
            {
                ApplyMiniCollectibleMask(mini, key.EndsWith("1", StringComparison.Ordinal) ? 1 : 0, value);
                MaybeLog(logger, LastApplyLogMsByKey, "jenga-mini-collectibles", "Cabin board game Jenga mini collectible mask apply chunk=" + (key.EndsWith("1", StringComparison.Ordinal) ? 1 : 0) + " value=" + value);
                return true;
            }

            return false;
        }

        private static bool ApplyOuijaActiveFlag(CabinPlayerController player, string key, int value)
        {
            if (player == null)
            {
                return value == 0;
            }

            return TrySetFieldObjectActive(player, key, value != 0);
        }

        private static bool ApplyMikeOuijaActiveFlag(MikeCabinCookController mike, string key, int value)
        {
            if (mike == null)
            {
                return value == 0;
            }

            return TrySetFieldObjectActive(mike, key, value != 0);
        }

        private static bool ApplyMikeOuijaFlag(MikeCabinCookController mike, string key, int value)
        {
            if (mike == null)
            {
                return false;
            }

            if (key == "ouijaTableParent")
            {
                ApplyMikeOuijaTableParent(mike, value);
                return true;
            }

            if (key == "tableHoldRigWeight")
            {
                SetWeight(GetFieldObject(mike, "tableHoldRig"), Dequantize(value));
                return true;
            }

            return false;
        }

        private static bool ApplyOuijaFlag(OuijaController ouija, string key, int value)
        {
            if (ouija == null)
            {
                return false;
            }

            if (key == "initialPositionX" || key == "initialPositionY" || key == "initialPositionZ")
            {
                var initial = GetVector3(ouija, "initialPosition");
                if (key.EndsWith("X", StringComparison.Ordinal))
                {
                    initial.x = Dequantize(value);
                }
                else if (key.EndsWith("Y", StringComparison.Ordinal))
                {
                    initial.y = Dequantize(value);
                }
                else
                {
                    initial.z = Dequantize(value);
                }

                SetFieldValue(ouija, "initialPosition", initial);
                return true;
            }

            if (key == "dragAudioMuted")
            {
                SetAudioMuted(GetFieldObject(ouija, "dragSFXAudioSource"), value != 0);
                return true;
            }

            if (key == "dragAudioVolume")
            {
                SetAudioVolume(GetFieldObject(ouija, "dragSFXAudioSource"), Dequantize(value));
                return true;
            }

            if (key == "targetCode")
            {
                SetFieldValue(ouija, "currentTarget", ResolveOuijaTargetByCode(ouija, value));
                return true;
            }

            if (key == "roundIndex" ||
                key == "canTakeMouseInput" ||
                key == "startRound" ||
                key == "movingToYesPoint" ||
                key == "hasStartedMovingToYes" ||
                key == "hasReachedYesPoint" ||
                key == "startMoveYesTimer" ||
                key == "canMoveToNextTarget" ||
                key == "canRandomlyMove" ||
                key == "hasReachedRandomPoint" ||
                key == "parentIsMoving")
            {
                if (key == "roundIndex")
                {
                    SetFieldValue(ouija, key, value);
                }
                else
                {
                    SetFieldValue(ouija, key, value != 0);
                }
                return true;
            }

            if (key == "randomPositionMoveTimer" ||
                key == "moveToYesPointTimer" ||
                key == "roundTimer" ||
                key == "mouseX" ||
                key == "mouseY")
            {
                SetFieldValue(ouija, key, Dequantize(value));
                return true;
            }

            return false;
        }

        private static void EmitJengaPieceMasks(string fullPrefix, JengaController jenga, Action<string, int> emit, ref int hash)
        {
            var pieces = GetJengaPieces(jenga);
            EmitInt(fullPrefix, JengaPrefix + "pieceCount", pieces.Count, emit, ref hash);
            for (var chunk = 0; chunk < 2; chunk++)
            {
                var active = 0;
                var movedOut = 0;
                var selected = 0;
                var kinematic = 0;
                for (var bit = 0; bit < MaskChunkBits; bit++)
                {
                    var index = chunk * MaskChunkBits + bit;
                    if (index >= pieces.Count)
                    {
                        break;
                    }

                    var piece = pieces[index];
                    if (piece == null)
                    {
                        continue;
                    }

                    var maskBit = 1 << bit;
                    if (piece.gameObject.activeSelf)
                    {
                        active |= maskBit;
                    }

                    if (piece.HasMovedOut)
                    {
                        movedOut |= maskBit;
                    }

                    if (GetBool(piece, "isSelected"))
                    {
                        selected |= maskBit;
                    }

                    if (GetRigidbodyKinematic(piece))
                    {
                        kinematic |= maskBit;
                    }
                }

                EmitInt(fullPrefix, JengaPiecePrefix + "Active" + chunk, active, emit, ref hash);
                EmitInt(fullPrefix, JengaPiecePrefix + "MovedOut" + chunk, movedOut, emit, ref hash);
                EmitInt(fullPrefix, JengaPiecePrefix + "Selected" + chunk, selected, emit, ref hash);
                EmitInt(fullPrefix, JengaPiecePrefix + "Kinematic" + chunk, kinematic, emit, ref hash);
            }
        }

        private static void EmitMiniCollectibleMasks(string fullPrefix, JengaDragMiniGameController mini, Action<string, int> emit, ref int hash)
        {
            var collectibles = GetMiniCollectibles(mini);
            for (var chunk = 0; chunk < 2; chunk++)
            {
                var active = 0;
                for (var bit = 0; bit < MaskChunkBits; bit++)
                {
                    var index = chunk * MaskChunkBits + bit;
                    if (index >= collectibles.Count)
                    {
                        break;
                    }

                    var collectible = collectibles[index];
                    if (collectible != null && collectible.activeSelf)
                    {
                        active |= 1 << bit;
                    }
                }

                EmitInt(fullPrefix, JengaMiniPrefix + "CollectibleActive" + chunk, active, emit, ref hash);
            }
        }

        private static int BuildColliderMask(object collidersObject)
        {
            var colliders = collidersObject as IEnumerable;
            if (colliders == null)
            {
                return 0;
            }

            var mask = 0;
            var index = 0;
            foreach (var item in colliders)
            {
                if (index >= MaskChunkBits)
                {
                    break;
                }

                var collider = item as Collider;
                if (collider != null && collider.enabled)
                {
                    mask |= 1 << index;
                }

                index++;
            }

            return mask;
        }

        private static void ApplyColliderMask(object collidersObject, int mask)
        {
            var colliders = collidersObject as IEnumerable;
            if (colliders == null)
            {
                return;
            }

            var index = 0;
            foreach (var item in colliders)
            {
                if (index >= MaskChunkBits)
                {
                    break;
                }

                var collider = item as Collider;
                if (collider != null)
                {
                    collider.enabled = (mask & (1 << index)) != 0;
                }

                index++;
            }
        }

        private static void ApplyMiniCollectibleMask(JengaDragMiniGameController mini, int chunk, int mask)
        {
            var collectibles = GetMiniCollectibles(mini);
            for (var bit = 0; bit < MaskChunkBits; bit++)
            {
                var index = chunk * MaskChunkBits + bit;
                if (index >= collectibles.Count)
                {
                    break;
                }

                var collectible = collectibles[index];
                if (collectible != null)
                {
                    collectible.SetActive((mask & (1 << bit)) != 0);
                }
            }
        }

        private static void ApplyMiniCurrentLevel(JengaDragMiniGameController mini, int levelIndex)
        {
            var levels = GetFieldObject(mini, "levels") as Array;
            if (levels == null || levelIndex < 0 || levelIndex >= levels.Length)
            {
                return;
            }

            for (var i = 0; i < levels.Length; i++)
            {
                SetActive(levels.GetValue(i), i == levelIndex);
            }

            var currentLevel = levels.GetValue(levelIndex);
            SetFieldValue(mini, "currentLevel", currentLevel);
            var collectibles = GetFieldObject(currentLevel, "collectibles");
            SetFieldValue(mini, "levelCollectibles", collectibles);
            SetFieldValue(mini, "cursorStartPoint", GetFieldObject(currentLevel, "cursorStartPoint"));
        }

        private static void ApplyOuijaSpectatorCamera(CabinPlayerController player, bool active)
        {
            if (player == null)
            {
                return;
            }

            var sittingPlayer = ExtractGameObject(GetFieldObject(player, "sittingPlayerOuijaBoardGame"));
            var sittingCam = GetFieldObject(player, "sittingCamOuijaBoardGame") as Behaviour;
            var fovZoom = GetFieldObject(player, "fovZoomOuijaBoardGame") as Behaviour;
            var firstPerson = GetFieldObject(player, "firstPersonController") as FirstPersonController;
            var camera = sittingCam != null ? sittingCam.GetComponent<Camera>() : null;

            if (active)
            {
                SetFieldValue(player, "currentHoldingBoardGameType", 1);
                if (sittingPlayer != null)
                {
                    sittingPlayer.SetActive(true);
                }

                if (firstPerson != null)
                {
                    firstPerson.gameObject.SetActive(false);
                }

                if (sittingCam != null)
                {
                    sittingCam.gameObject.SetActive(true);
                    sittingCam.enabled = true;
                }

                if (fovZoom != null)
                {
                    fovZoom.gameObject.SetActive(true);
                    fovZoom.enabled = true;
                }

                if (camera != null)
                {
                    SetFieldValue(player, "currentCamera", camera);
                }
                return;
            }

            if (sittingCam != null)
            {
                sittingCam.enabled = false;
            }

            if (fovZoom != null)
            {
                fovZoom.enabled = false;
            }

            if (sittingPlayer != null)
            {
                sittingPlayer.SetActive(false);
            }

            if (firstPerson != null && !firstPerson.gameObject.activeSelf)
            {
                firstPerson.gameObject.SetActive(true);
            }
        }

        private static int GetMiniCurrentLevelIndex(JengaDragMiniGameController mini)
        {
            if (mini == null)
            {
                return -1;
            }

            var currentLevel = GetFieldObject(mini, "currentLevel");
            var levels = GetFieldObject(mini, "levels") as Array;
            if (currentLevel == null || levels == null)
            {
                return -1;
            }

            for (var i = 0; i < levels.Length; i++)
            {
                if (ReferenceEquals(levels.GetValue(i), currentLevel))
                {
                    return i;
                }
            }

            return -1;
        }

        private static List<GameObject> GetMiniCollectibles(JengaDragMiniGameController mini)
        {
            var result = new List<GameObject>();
            if (mini == null)
            {
                return result;
            }

            var list = GetFieldObject(mini, "levelCollectibles") as IEnumerable;
            if (list == null)
            {
                list = GetFieldObject(GetFieldObject(mini, "currentLevel"), "collectibles") as IEnumerable;
            }

            if (list == null)
            {
                return result;
            }

            foreach (var item in list)
            {
                var gameObject = ExtractGameObject(item);
                if (gameObject != null)
                {
                    result.Add(gameObject);
                }
            }

            return result;
        }

        private static void ApplySelectedPiece(JengaController jenga, int index)
        {
            var pieces = GetJengaPieces(jenga);
            for (var i = 0; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                if (piece != null)
                {
                    SetFieldValue(piece, "isSelected", i == index);
                }
            }

            SetFieldValue(jenga, "selectedPiece", index >= 0 && index < pieces.Count ? pieces[index] : null);
        }

        private static int GetSelectedPieceIndex(JengaController jenga)
        {
            if (jenga == null)
            {
                return -1;
            }

            var selected = GetFieldObject(jenga, "selectedPiece") as JengaPiece;
            var pieces = GetJengaPieces(jenga);
            for (var i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] == selected)
                {
                    return i;
                }
            }

            return -1;
        }

        private static List<JengaPiece> GetJengaPieces(JengaController jenga)
        {
            var result = new List<JengaPiece>();
            if (jenga == null)
            {
                return result;
            }

            var pieces = GetFieldObject(jenga, "pieces") as IEnumerable;
            if (pieces == null)
            {
                return result;
            }

            foreach (var piece in pieces)
            {
                var jengaPiece = piece as JengaPiece;
                if (jengaPiece != null)
                {
                    result.Add(jengaPiece);
                }
            }

            return result;
        }

        private static CabinPlayerController ResolveCabinPlayer(CabinGameManager manager)
        {
            return GetFieldObject(manager, "playerController") as CabinPlayerController
                ?? UnityEngine.Object.FindObjectOfType<CabinPlayerController>();
        }

        private static JengaController ResolveJengaController(CabinGameManager manager)
        {
            var direct = GetFieldObject(manager, "jengaController") as JengaController;
            if (direct != null)
            {
                return direct;
            }

            return UnityEngine.Object.FindObjectOfType<JengaController>();
        }

        private static JengaDragMiniGameController ResolveJengaMiniGameController(CabinGameManager manager, JengaController jenga)
        {
            var direct = GetFieldObject(manager, "jengaMiniGameController") as JengaDragMiniGameController
                ?? GetFieldObject(jenga, "miniGameController") as JengaDragMiniGameController;
            if (direct != null)
            {
                return direct;
            }

            return UnityEngine.Object.FindObjectOfType<JengaDragMiniGameController>();
        }

        private static OuijaController ResolveOuijaController(CabinGameManager manager, CabinPlayerController player)
        {
            var direct = GetFieldObject(manager, "ouijaController") as OuijaController;
            if (direct != null)
            {
                return direct;
            }

            var board = GetFieldObject(player, "ouijaBoardGame") as GameObject;
            if (board != null)
            {
                var nested = board.GetComponentInChildren<OuijaController>(true);
                if (nested != null)
                {
                    return nested;
                }
            }

            return UnityEngine.Object.FindObjectOfType<OuijaController>();
        }

        private static MikeCabinCookController ResolveMikeController(CabinGameManager manager)
        {
            return GetFieldObject(manager, "mikeController") as MikeCabinCookController
                ?? UnityEngine.Object.FindObjectOfType<MikeCabinCookController>();
        }

        private static int ResolveMikeOuijaTableParentMode(MikeCabinCookController mike)
        {
            var table = GetFieldObject(mike, "ouijaTable") as Transform;
            if (table == null)
            {
                return 0;
            }

            var hand = GetFieldObject(mike, "ouijaTableHandHoldPoint") as Transform;
            if (hand != null && table.parent == hand)
            {
                return 1;
            }

            var place = GetFieldObject(mike, "basementTablePlacePoint") as Transform;
            if (place != null && table.parent == place)
            {
                return 2;
            }

            return 0;
        }

        private static void ApplyMikeOuijaTableParent(MikeCabinCookController mike, int mode)
        {
            var table = GetFieldObject(mike, "ouijaTable") as Transform;
            if (table == null)
            {
                return;
            }

            Transform parent = null;
            if (mode == 1)
            {
                parent = GetFieldObject(mike, "ouijaTableHandHoldPoint") as Transform;
            }
            else if (mode == 2)
            {
                parent = GetFieldObject(mike, "basementTablePlacePoint") as Transform;
            }

            if (parent == null || table.parent == parent)
            {
                return;
            }

            table.SetParent(parent, false);
            table.localPosition = Vector3.zero;
            table.localRotation = Quaternion.identity;
        }

        private static int BuildOuijaTargetCode(OuijaController ouija)
        {
            var target = GetFieldObject(ouija, "currentTarget") as Transform;
            if (target == null)
            {
                return -1;
            }

            if (target == GetFieldObject(ouija, "yesTransformPoint") as Transform)
            {
                return -2;
            }

            if (target == GetFieldObject(ouija, "noTransformPoint") as Transform)
            {
                return -3;
            }

            var randomPoints = GetFieldObject(ouija, "randomPointsOnBoardToMoveTowards") as Transform[];
            if (randomPoints != null)
            {
                for (var i = 0; i < randomPoints.Length; i++)
                {
                    if (randomPoints[i] == target)
                    {
                        return i;
                    }
                }
            }

            return -4;
        }

        private static Transform ResolveOuijaTargetByCode(OuijaController ouija, int code)
        {
            if (ouija == null || code == -1)
            {
                return null;
            }

            if (code == -2)
            {
                return GetFieldObject(ouija, "yesTransformPoint") as Transform;
            }

            if (code == -3)
            {
                return GetFieldObject(ouija, "noTransformPoint") as Transform;
            }

            var randomPoints = GetFieldObject(ouija, "randomPointsOnBoardToMoveTowards") as Transform[];
            if (randomPoints != null && code >= 0 && code < randomPoints.Length)
            {
                return randomPoints[code];
            }

            return null;
        }

        private static string BuildMiniStatus(JengaDragMiniGameController mini)
        {
            if (mini == null || !mini.gameObject.activeInHierarchy)
            {
                return "-";
            }

            if (GetBool(mini, "hasWon"))
            {
                return "won";
            }

            if (GetBool(mini, "gameLost"))
            {
                return "lost";
            }

            return GetBool(mini, "hasMiniGameStarted") ? "active" : "idle";
        }

        private static string GetJengaStateName(int value)
        {
            if (value == 0)
            {
                return "NotInGame";
            }

            if (value == 1)
            {
                return "SelectBlock";
            }

            if (value == 2)
            {
                return "DragBlock";
            }

            if (value == 3)
            {
                return "MikesTurn";
            }

            return value.ToString();
        }

        private static void EmitObjectActive(string fullPrefix, string key, object owner, string fieldName, Action<string, int> emit, ref int hash)
        {
            EmitInt(fullPrefix, key, GetObjectActive(GetFieldObject(owner, fieldName)), emit, ref hash);
        }

        private static void EmitBool(string fullPrefix, string key, bool value, Action<string, int> emit, ref int hash)
        {
            EmitInt(fullPrefix, key, value ? 1 : 0, emit, ref hash);
        }

        private static void EmitInt(string fullPrefix, string key, int value, Action<string, int> emit, ref int hash)
        {
            emit(fullPrefix + key, value);
            unchecked
            {
                hash = (hash * 31) + StableHash(key);
                hash = (hash * 31) + value;
            }
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 23;
                if (value == null)
                {
                    return hash;
                }

                for (var i = 0; i < value.Length; i++)
                {
                    hash = (hash * 31) + value[i];
                }

                return hash;
            }
        }

        private static int Quantize(float value)
        {
            return Mathf.RoundToInt(value * TransformScale);
        }

        private static float Dequantize(int value)
        {
            return value / (float)TransformScale;
        }

        private static object GetFieldObject(object owner, string fieldName)
        {
            if (owner == null || string.IsNullOrEmpty(fieldName))
            {
                return null;
            }

            var field = FindInstanceField(owner.GetType(), fieldName);
            if (field != null)
            {
                return field.GetValue(owner);
            }

            var property = owner.GetType().GetProperty(fieldName, FieldFlags);
            return property != null && property.CanRead ? property.GetValue(owner, null) : null;
        }

        private static bool SetFieldValue(object owner, string fieldName, object value)
        {
            if (owner == null || string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            var field = FindInstanceField(owner.GetType(), fieldName);
            if (field == null)
            {
                return false;
            }

            try
            {
                if (field.FieldType.IsEnum && value is int)
                {
                    field.SetValue(owner, Enum.ToObject(field.FieldType, (int)value));
                }
                else
                {
                    field.SetValue(owner, value);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static FieldInfo FindInstanceField(Type type, string fieldName)
        {
            while (type != null)
            {
                var field = type.GetField(fieldName, FieldFlags);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static bool GetBool(object owner, string fieldName)
        {
            var value = GetFieldObject(owner, fieldName);
            if (value is bool)
            {
                return (bool)value;
            }

            return value is int && (int)value != 0;
        }

        private static int GetInt(object owner, string fieldName)
        {
            var value = GetFieldObject(owner, fieldName);
            if (value is int)
            {
                return (int)value;
            }

            if (value is bool)
            {
                return (bool)value ? 1 : 0;
            }

            return 0;
        }

        private static float GetFloat(object owner, string fieldName)
        {
            var value = GetFieldObject(owner, fieldName);
            if (value is float)
            {
                return (float)value;
            }

            if (value is double)
            {
                return (float)(double)value;
            }

            if (value is int)
            {
                return (int)value;
            }

            return 0f;
        }

        private static float GetWeight(object owner)
        {
            if (owner == null)
            {
                return 0f;
            }

            var property = owner.GetType().GetProperty("weight", FieldFlags) ??
                           owner.GetType().GetProperty("Weight", FieldFlags);
            if (property != null && property.CanRead)
            {
                var value = property.GetValue(owner, null);
                if (value is float)
                {
                    return (float)value;
                }
            }

            var field = FindInstanceField(owner.GetType(), "weight") ??
                        FindInstanceField(owner.GetType(), "Weight");
            if (field != null)
            {
                var value = field.GetValue(owner);
                if (value is float)
                {
                    return (float)value;
                }
            }

            return 0f;
        }

        private static void SetWeight(object owner, float value)
        {
            if (owner == null)
            {
                return;
            }

            value = Mathf.Clamp01(value);
            var property = owner.GetType().GetProperty("weight", FieldFlags) ??
                           owner.GetType().GetProperty("Weight", FieldFlags);
            if (property != null && property.CanWrite)
            {
                property.SetValue(owner, value, null);
                return;
            }

            var field = FindInstanceField(owner.GetType(), "weight") ??
                        FindInstanceField(owner.GetType(), "Weight");
            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(owner, value);
            }
        }

        private static int GetEnumOrInt(object owner, string fieldName)
        {
            var value = GetFieldObject(owner, fieldName);
            if (value == null)
            {
                return 0;
            }

            if (value is int)
            {
                return (int)value;
            }

            if (value.GetType().IsEnum)
            {
                return Convert.ToInt32(value);
            }

            return 0;
        }

        private static Vector3 GetVector3(object owner, string fieldName)
        {
            var value = GetFieldObject(owner, fieldName);
            return value is Vector3 ? (Vector3)value : Vector3.zero;
        }

        private static bool TrySetFieldObjectActive(object owner, string fieldName, bool active)
        {
            return SetActive(GetFieldObject(owner, fieldName), active);
        }

        private static bool SetActive(object target, bool active)
        {
            var gameObject = ExtractGameObject(target);
            if (gameObject == null)
            {
                return false;
            }

            gameObject.SetActive(active);
            return true;
        }

        private static bool SetEnabledByReflection(object target, bool enabled)
        {
            if (target == null)
            {
                return false;
            }

            if (target is Behaviour)
            {
                ((Behaviour)target).enabled = enabled;
                return true;
            }

            var property = target.GetType().GetProperty("enabled", FieldFlags);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, enabled, null);
                return true;
            }

            return false;
        }

        private static void SetEnabledOnComponentsNamed(object target, string typeNamePart, bool enabled)
        {
            var gameObject = ExtractGameObject(target);
            if (gameObject == null)
            {
                return;
            }

            var components = gameObject.GetComponents<Component>();
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component != null && component.GetType().Name.IndexOf(typeNamePart, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    SetEnabledByReflection(component, enabled);
                }
            }
        }

        private static GameObject ExtractGameObject(object target)
        {
            if (target == null)
            {
                return null;
            }

            if (target is GameObject)
            {
                return (GameObject)target;
            }

            if (target is Component)
            {
                return ((Component)target).gameObject;
            }

            if (target is Transform)
            {
                return ((Transform)target).gameObject;
            }

            return null;
        }

        private static int GetObjectActive(object target)
        {
            var gameObject = ExtractGameObject(target);
            return gameObject != null && gameObject.activeSelf ? 1 : 0;
        }

        private static int IsActive(GameObject gameObject)
        {
            return gameObject != null && gameObject.activeSelf ? 1 : 0;
        }

        private static void AddTransform(object target, List<Transform> transforms)
        {
            if (target == null || transforms == null)
            {
                return;
            }

            var transform = target as Transform;
            if (transform == null)
            {
                var gameObject = ExtractGameObject(target);
                transform = gameObject != null ? gameObject.transform : null;
            }

            if (transform != null && !transforms.Contains(transform))
            {
                transforms.Add(transform);
            }
        }

        private static bool GetRigidbodyKinematic(JengaPiece piece)
        {
            if (piece == null)
            {
                return true;
            }

            var rb = GetFieldObject(piece, "rb") as Rigidbody ?? piece.GetComponent<Rigidbody>();
            return rb == null || rb.isKinematic;
        }

        private static void SetRigidbodyKinematic(JengaPiece piece, bool kinematic)
        {
            if (piece == null)
            {
                return;
            }

            var rb = GetFieldObject(piece, "rb") as Rigidbody ?? piece.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = kinematic;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private static void SetBoolProperty(object owner, string propertyName, bool value)
        {
            if (owner == null)
            {
                return;
            }

            var property = owner.GetType().GetProperty(propertyName, FieldFlags);
            if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
            {
                property.SetValue(owner, value, null);
            }
        }

        private static void SetInputEnabled(object target, bool enabled)
        {
            if (target == null)
            {
                return;
            }

            var property = target.GetType().GetProperty("InputEnabled", FieldFlags);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, enabled, null);
            }
        }

        private static void MuteAudio(object target)
        {
            var audio = target as AudioSource;
            if (audio != null)
            {
                audio.mute = true;
                audio.volume = 0f;
                audio.Stop();
            }
        }

        private static bool GetAudioMuted(object target)
        {
            var audio = target as AudioSource;
            return audio != null && audio.mute;
        }

        private static float GetAudioVolume(object target)
        {
            var audio = target as AudioSource;
            return audio != null ? audio.volume : 0f;
        }

        private static void SetAudioMuted(object target, bool muted)
        {
            var audio = target as AudioSource;
            if (audio != null)
            {
                audio.mute = muted;
                if (muted)
                {
                    audio.Stop();
                }
            }
        }

        private static void SetAudioVolume(object target, float volume)
        {
            var audio = target as AudioSource;
            if (audio != null)
            {
                audio.volume = Mathf.Clamp01(volume);
            }
        }

        private static void MaybeLog(ManualLogSource logger, Dictionary<string, long> bucket, string key, string message)
        {
            if (logger == null || bucket == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long last;
            if (bucket.TryGetValue(key, out last) && now - last < LogIntervalMs)
            {
                return;
            }

            bucket[key] = now;
            logger.LogInfo(message);
        }
    }
}
