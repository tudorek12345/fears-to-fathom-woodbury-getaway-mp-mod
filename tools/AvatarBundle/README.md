# Woodbury Avatar Bundle

Unity 2021.3.x is required because the game runs on Unity 2021.3.33.

## Source Assets

Use CC0 character assets:

- Quaternius Universal Base Characters: https://quaternius.com/packs/universalbasecharacters.html
- Quaternius Universal Animation Library: https://quaternius.com/packs/universalanimationlibrary.html

Create or import four humanoid prefabs at these exact Unity paths:

- `Assets/AvatarBundle/Prefabs/quaternius_regular_male.prefab`
- `Assets/AvatarBundle/Prefabs/quaternius_regular_female.prefab`
- `Assets/AvatarBundle/Prefabs/quaternius_teen_male.prefab`
- `Assets/AvatarBundle/Prefabs/quaternius_teen_female.prefab`

Each prefab should contain the visible skinned mesh and, if animation is wanted, an `Animator` controller with the `ThirdPersonBasic` parameters used by the mod: `Strafe`, `Forward`, `GroundSpeed`, `IsMoving`, `IsRunning`, `IsCrouching`, and `IsJumping`.

The checked-in source setup uses the free Standard Quaternius download. That archive only includes `Superhero_Male_FullBody.fbx` and `Superhero_Female_FullBody.fbx`, so the four manifest ids are generated from those two CC0 bodies; the teen prefabs use a smaller model scale. Replace the source FBXs if you later add the paid Source archive with the true Regular/Teen bodies.

## Build

From the repo root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build-AvatarBundle.ps1
```

The output bundle is written to `output/avatars/woodbury_avatars.bundle`.

Install it into the game with:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build-AvatarBundle.ps1 -InstallToGameDir "C:\Games\Fears to Fathom - Woodbury Getaway"
```
