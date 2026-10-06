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
