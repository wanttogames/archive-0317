# CASE 001 field slice verification

Verified in Unity 6000.3.25f1 / URP through Unity MCP on 2026-10-06.

- Both scenes were authored/saved through Unity Editor APIs. ArchiveRoom's environment asset data remains unchanged; its shared player and CASE prefab provide the new UI connection.
- Existing ArchiveRoom component smoke test: PASS. Walk/run, grounding, wall/shelf/desk collision, look limits, interaction range, prompt, description and cursor checks remain valid.
- CASE 001 asynchronous Play Mode smoke test: PASS. The real button listener starts the fade and scene load; one shared player, EventSystem and AudioListener are created in the motel. All six physical stair flights reach the fourth-floor elevation without teleporting between flights. Door key gating, document Raycasts, ledger detail page (403), CCTV hint, room entry trigger, paper, notebook comparison and JSON cache reload pass. Test progress is restored after execution.
- Gameplay run after clearing earlier Editor logs: 0 errors/exceptions, 0 warnings. Stopping Play Mode produced 2 Unity MCP lifecycle warnings: `BufferedFileLogStorage: Flush called but already disposed` and `McpManagerClientHub: Connection not available and auto-reconnect disabled`. These are plugin reconnect/log-storage warnings; there are no new game script exceptions or compilation errors. The plugin was not modified.
- An additional archive UI screenshot cycle produced 6 warnings from the same MCP lifecycle sources. After the final full smoke-test rerun, the Editor Console contains 0 errors/exceptions and 2 MCP lifecycle warnings. ArchiveRoom is open, saved and outside Play Mode.
- Native Game View capture `Case001_GameView.png` and higher-resolution camera/HUD captures were visually inspected. Ledger text, correct-facing room numbers, corridor, reception and room 404 were checked. A depth-tested world-text shader prevents numbers showing through walls; its runtime font atlas is assigned and shader is supported. The archive's overlay font material is retained.
- Scene dependency audit: all new dependencies are included with their `.meta` files. Package versions and unrelated project settings are excluded from this change.

Still requires a human Game View playthrough: physical WASD/mouse/Shift/E/TAB/ESC and button pointer clicks, target-monitor brightness, the quiet latch's audible level, route/signposting and approximately 5–10 minute pacing. Automated tests invoke gameplay component methods, real UI listeners and physics; they do not represent a timed human playthrough or a standalone build test.

Reports: `PlayModeSmokeTest.txt`, `Case001SmokeTest.txt`. No other Unity test assembly was present in Assets.

## Stair lighting correction

All three half-landing fixtures previously sat only 1.26m above the landing surface. Housing, tube and spotlight were moved together to the underside of the next landing slab, or the stairwell roof for the top fixture. Housing world Y positions are now 4.23m, 7.23m and 11.33m. Minimum clearance below the tubes is 2.673m, 2.673m and 3.773m respectively, with a 0.035m gap between each housing and the overhead surface. The builder uses the same placement. Light colour, intensity and range are retained.

The existing ArchiveRoom and CASE 001 Play Mode smoke tests were rerun and passed, including all six stair flights and the investigation endpoint. Gameplay Console: 0 errors/exceptions and 0 warnings. The corrected fixture was visually inspected from the player camera and Game View. Final target-monitor brightness remains a manual check.

## Middle section: the absent room 403

The extended Play Mode smoke test passes the complete archive-to-motel flow and both investigation stages. It checks initial 401/402/404/405 labels and an inactive 403 behind a solid wall; plate Raycasts; original ledger/404/key/door/paper UI; optional notebook comparison; exit-only, out-of-view fluorescent alteration; early CCTV rejection; recorded 403 and a single timestamp; a nonblank grayscale RenderTexture; telephone gating at the fourth-floor return; rejection while the wall is watched; reveal after looking away; the locked door's short response, delayed sound and solid collision; and a real scene reload restoring changes without repeating the lead sound. Original saved case progress is restored by the test. Passing the alternate route without the notebook comparison confirms that observing the plates and investigating 404 can advance the case naturally.

The recorded corridor is an isolated, static reconstruction with four visible door numbers and an ordinary clothed guest. It is not a gameplay space or a complete animated surveillance video. Frames refresh at 4Hz, monochrome noise is restricted to the recording, and `2002-10-11 03:17` appears only on its viewer. Camera/HUD and native Game View screenshots were inspected, including initial versus revealed 403 from the same viewpoint. The 403 interior remains unimplemented and inaccessible.

Gameplay verification Console: 0 Errors, 0 Exceptions, 0 Warnings. Editor compilation and Play Mode lifecycle may emit the previously documented MCP logging/reconnection warnings; these are separate from game runtime diagnostics. No package or plugin was changed. New assets and metadata are included in the dependency audit.

Manual checks remain: actual keyboard/mouse and pointer use, naturally noticing the missing room, finding the return route to CCTV, target-monitor darkness, spatial telephone direction/volume, and the restraint and pacing of the encounter. No timed human playthrough or standalone build has been claimed.
