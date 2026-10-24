# Woodbury Spacewar helper

Adapted from tudorek12345's local MWCMP `tools/SpacewarHelper` and `Net/SpacewarWire.cs`. Woodbury uses its own lobby identity (`tudorek12345.woodbury.spacewar.v1`), a two-member limit and its own executable. MWCMP lobbies cannot join this mod.

Build on Windows x64 with CMake, MSVC C++ build tools and an official Steamworks SDK:

```powershell
.\tools\SpacewarHelper\Build.ps1 -SteamworksSdk "<sdk-directory>"
```

This builds the helper, runs native core and stdout-isolation tests, and stages `build/spacewar-package/Spacewar`. Deploy/package scripts copy that directory to `BepInEx/plugins/Spacewar`. SDK headers and game assemblies are not shipped. Keep the helper's `steam_api64.dll` in its own directory.

F11: choose Spacewar on both PCs, start the host, copy its lobby ID, then enter that ID on the client and connect. Steam must be running under separate accounts. The lobby validates the exact plugin/protocol metadata and original owner. There is no host migration. A disconnected host session currently creates a new lobby; share the new ID when starting again. Steam invitations are not exposed in the mod UI.

The helper uses App ID 480 only in its process and directory. The mod sends its existing frames through bounded, reliable, ordered Steam messages, split into 64 KiB chunks. Snapshot reliability takes priority; unreliable pose/voice channels are future work. [Valve documents the ordering contract here](https://partner.steamgames.com/doc/api/ISteamNetworkingMessages#SendMessageToUser).

`scripts/Test-PlayerPoseRegression.ps1` tests the real managed stream against a fake helper, including fragmentation, unauthorized senders, excessive lobby membership and peer departure. These tests do not establish Internet/relay performance or two-account interoperability. To probe native initialization separately after building the regression executable:

```powershell
.\tests\SyncRegression\bin\Release\net472\SyncRegression.exe --probe "<absolute-path-to-built-Spacewar-directory>"
```

Startup input accepts one UTF-8 BOM emitted by .NET Framework on UTF-8 Windows. Later frames remain strictly binary; repeated/midstream preambles and invalid lengths are rejected. The native core suite covers this without a Steamworks SDK.
