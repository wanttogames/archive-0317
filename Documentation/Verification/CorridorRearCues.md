# Sparse rear corridor cues

`CorridorRearCue` is authored in the motel scene by the **Archive 03:17 / Add Corridor Rear Cues** editor menu. Existing investigation, numbered doors and gameplay systems are preserved.

Only walking inside the fourth-floor corridor can trigger the cue. The player must travel at least 5 metres; the first cue has an 18-second delay, and later cues have 38–55-second cooldowns. A distant doorway must lie more than 4 metres behind the current view. Quiet spatial mono audio alternates a latch, short slipper scrape and muffled door shut. Procedural clips are generated once at scene start; no new package or background music is used.

There are at most three cues per scene visit. A single low-poly, faceless coat silhouette appears on either the second or third cue, chosen at startup, initially behind the player near a distant doorway. First cues never show anyone. Once observed, a camera rotation exceeding 7 degrees hides the silhouette. It also expires after 9 seconds or when leaving the corridor, opening documents/menu, or unlocking the cursor. It has no collider, AI, attack, chase or camera control. It does not mark story progress or alter the 403 revelation conditions.

The integrated CASE 001 Play Mode smoke test exercises the actual component using a temporary copy: walking/cooldown gating, empty first cue, selected second appearance, looking back, disappearance on camera movement, empty third cue and three-cue cap. Player pose and original component are restored afterward; the encompassing test restores the user's saved case progress. Audio balance and the subjective effect on a human turning around still require a headphones playthrough.

## Actual verification — 2026-10-07

Unity 6000.3.25f1 GUI Editor compiled the scripts successfully. Final actual Play Mode run passed all 176 CASE 001 smoke assertions, including the rear cue regression and existing stair collision, open 404 numberplate, 403 investigation and case completion checks. Console at completion had 0 errors / 0 runtime exceptions and 7 MCP connection/disposal warnings. A screenshot capture helper initially selected a pending-destruction test copy; that helper was repaired and the complete suite was rerun successfully.

`CorridorRearFigure.png` is an inspected native Game View screenshot captured after the actual second-cue condition fired on a temporary component copy. The camera was held briefly for the screenshot. The randomized third-cue choice uses the same path; the deterministic regression selects the second cue. Original saved case progress was restored, and the motel scene was saved through Unity MCP / Editor APIs.
