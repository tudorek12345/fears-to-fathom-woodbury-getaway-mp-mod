# 0.4.49 character and menu validation

Plugin **0.4.49**, game protocol **7**. DLL SHA256: `5e31a5e6a711923f5fb5059aaf461395f03c175f184ee389dfdc8836d6b8dd47`. Both players must update together. The separate Steam helper IPC remains version 6 and receives the game protocol during initialization.

## Changes

- The game's own Multiplayer text entry opens modern Co-op and Character pages. Connection actions retain LAN/IP, Spacewar, Host/Join, lobby copying, Disconnect and native Start story. An independent Canvas fixes inherited oversized fonts and clipping.
- Character offers a rotating 3D preview, face/body framing, four face shapes, six skin tones, five hairstyles, five hair colors, three outerwear styles, eight jacket colors, six trouser colors, four boot colors and four accessory choices. Apply persists the profile; Discard, tab changes and closing cancel unapplied edits.
- Auto avatars use an original stylized winter-traveler mesh with 17 bones and continuous clothing surfaces. It works in the six gameplay scenes and the retained Office world during the typing minigame. Existing body, head, arm, wrist, crouch, seated and directional locomotion drivers animate it. Explicit prefab/GameModel/AssetBundle/Capsule overrides retain their own visuals. This styles the remote co-op avatar; it does not replace the native first-person hand mesh or add facial-expression/finger-bone animation.
- Nine bounded catalog indices travel over reliable messages with session and revision checks. Changed profiles send immediately; unchanged profiles replay every three seconds. Scene transitions retain the remote profile, while disconnects clear it. Older wire versions reject rather than silently misreading appearance data.
- Saved-profile parsing and renderer lookup are cached. Meshes change only when the selected profile changes; preview rendering occurs only when dirty. Replaced meshes, per-character materials and preview textures are released. The preview waits for Unity's skinning pass before manually rendering a changed mesh.

## Automated evidence

The final package regression gate passes **873 checks** in `build/package-049-final.log`: 795 managed pose/protocol/transport/character checks, 18 snapshot assertions, 33 log fixtures, seven Unity-error classifications, six native menu/animation log fixtures, six story-parity/lookup fixtures and eight appearance log fixtures. Release compilation has zero warnings/errors.

The tests execute production serialization, session/revision handling, draft behavior and real client/server TCP appearance exchange in both directions. They reject invalid catalog values, truncated/trailing payloads, duplicates and old-session/revision replays. They also retain the existing snapshot, transport, story-scalar, scene-cache, animation and helper-failure regressions. Log fixtures reject missing additive-avatar coverage and stopped skin rendering.

Both native suites passed: `SpacewarCoreTests` and `SpacewarOutputIsolation`.

The complete local package is `site/downloads/woodbury-coop-latest.7z` (8,296,852 bytes), SHA256 `2b9c22f5ee18f469363e5d9a1bcd2bafdee742ee121490da6eecd065877e4d37`. Independent extraction verified all 30 listed file checksums, both runtime report entries, the actual native helper and agreement between built, installed and packaged DLL hashes. No game reference assemblies are included. PlayerPrefs compare byte-for-byte with the pre-sweep backup. Evidence: `build/package-049-verification.json`.

| Route | Result | Local report |
| --- | --- | --- |
| TCP only, all ten build scenes | PASS | `build/release-049-tcp/20261024-015022-538/summary.json` |
| TCP + UDP, all ten build scenes | PASS | `build/release-049-udp/20261024-015953-214/summary.json` |
| Graphics: Office and menu return | PASS | `build/release-049-visual/20261024-014536-055/summary.json` |

Each full route starts through native Host/Join, then visits Office, additive typing, Office again, ParkingLot, RoadTrip, Pizzeria, Cabin, CabinDark, credits and menu. Disclaimer is boot-only coverage. Probes check scene/snapshot wiring, both skeletons, distinct initial looks and changed remote looks, typing presentation, seated RoadTrip poses and actual native Cabin story-field digests. This is scene-entry and synchronization coverage, not a completed story playthrough.

The TCP-only and TCP+UDP runs apply 7,861 and 7,916 host state messages, respectively, with zero mod runtime errors or retry-backlog warnings. Character and story-parity checks pass. CabinScene matches 15 available fields; CabinSceneDark matches all 30. The custom avatar is checked in the six gameplay scenes plus the additive typing world. The UDP run records 8,403 received UDP host transforms, alongside TCP traffic. Client scene lookup totals are 9,022 requests / 687 searches for TCP and 8,928 / 684 for UDP; these measure reused searches, not FPS.

## Graphics review

The final graphics report uses the same DLL hash above and has zero mod runtime errors. Native menu actions, character Apply/Discard/reopen/cancel, both rig probes and live appearance changes pass. Images are under `build/release-049-visual/20261024-014536-055/images`:

- `23512-menu.png` shows native Multiplayer placement above Episodes without overlap.
- `23512-multiplayer.png` shows the modern connection controls.
- Both peers' character, puffer and face captures contain visible portraits; `portrait-pixels.json` records six passing non-empty portrait checks. The preview keeps the user's chosen face/body framing across menu visits.
- `OfficeLayout-23512-7.png` shows the client's changed white jacket, khaki trousers and burgundy boots on the host.
- `OfficeLayout-31220-7.png` shows the host's changed ochre hoodie, shaved hair and beard on the client.
- The numbered pose frames exercise visible skeleton deformation. Diagnostic cameras and lighting differ from the normal game view; this is not all-scene visual calibration.

Earlier captures exposed blank portraits after outfit changes: Unity reported a skinning buffer-size mismatch. The failing captures/logs remain under `build/menu-049-20261024-013814` and `build/menu-049-20261024-014021`. The corrected render timing is verified in `build/menu-049-20261024-014346` and the final paired graphics report. The error classifier now fails on this renderer error, even when Unity does not throw a managed exception.

## Spacewar and logs

`build/spacewar-native-049-probe.log` records a real signed-in App ID 480 initialization, lobby creation and relay readiness using 0.4.49 metadata. The first Steam SDR configuration fetch returned HTTP 504; Steam retried and reached readiness. The helper closed after the probe. This used one account and does not prove a two-account join.

The previously requested completed release, 0.4.48, was launched in `build/play-release-048` while this update was in development. Its DLL hash was verified against the completed package. That play session has since exited; its logs are preserved separately in `build/diagnostics-play-048/20261024-014104-755.zip`.

Latest final 0.4.49 diagnostics: `build/diagnostics-049-final/20261024-020928-745.zip`. It contains the recent final game logs and installed-file hashes. The two full scene archives also retain their original raw logs and pre-test PlayerPrefs backups.

The tester's one-second Spacewar disconnect has not been reproduced with both PCs. The earlier stream-ownership/readiness/error-retention repairs remain in place, but that report still requires a real retest and both-PC logs. The included `Collect-CoopDiagnostics.ps1` archives local logs and installed-file hashes without uploading anything.

## Limits

The installed game's missing `SteamAPI_ManualDispatch_Init` entry point and its credits achievement failure remain separate environment errors. The game's Steam library was not replaced. The earlier full graphics sweep's out-of-memory failure remains documented in the 0.4.48 report; current full routes are headless, with a focused graphics run.

Full story and ending branches, save/resume parity, gameplay reconnect recovery, all-scene visual calibration and two-account Spacewar gameplay remain unverified. No FPS improvement or full-story completion percentage is claimed. No commit, push or public deployment is performed by this validation workflow.
