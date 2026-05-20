# Fears to Fathom: Woodbury Getaway Co-op Mod

Experimental BepInEx 5 LAN co-op mod for **Fears to Fathom: Woodbury Getaway**.


## Status

Work in progress.

Cabin has the most coverage. Pizzeria and RoadTrip have early sync foundations. Full story parity is still being built and tested scene by scene.

## Requirements

- Fears to Fathom: Woodbury Getaway
- BepInEx 5 Mono
- .NET SDK
- Unity/game reference DLLs copied into `lib/`

## Build

```powershell
.\scripts\CopyLibs.ps1 -GameDir "<GameDir>"
dotnet build .\src\WoodburySpectatorSync\WoodburySpectatorSync.csproj -c Release
```

Output:

```text
src\WoodburySpectatorSync\bin\Release\net472\WoodburySpectatorSync.dll
```

## Install

Copy the built DLL into:

```text
<GameDir>\BepInEx\plugins\
```

Optional avatar bundles can be placed in:

```text
<GameDir>\BepInEx\plugins\WoodburySpectatorSync\avatars\
```

## Launch

For a paired test session, use the launcher:

```powershell
.\scripts\Launch-CoopPair.ps1 -GameDir "<GameDir>" -AutoStartHost -AutoConnectClient
```

For separate machines, configure one instance as `CoopHost` and the other as `CoopClient`, then point the client at the host IP.

## Controls

| Key | Action |
|---|---|
| `F6` | Toggle host |
| `F7` | Connect client |
| `F8` | Toggle overlay |
| `F9` | Debug/progress action |

## Project Layout

```text
src/WoodburySpectatorSync/   BepInEx plugin source
scripts/                     build and launch helpers
tools/AvatarBundle/          optional avatar bundle tooling
```

## Notes

This project is unofficial and not affiliated with the game developer or publisher.

See `CHANGELOG.md` for implementation history.
