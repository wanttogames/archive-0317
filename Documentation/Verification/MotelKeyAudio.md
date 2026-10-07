# CASE 001 key acquisition audio

InteractionSoundscape now synthesizes distinct mono metal keyring clips: a light three-contact ring for the spare 404 key and a lower, longer ring for reverse-side evidence. Playback uses the existing spatial one-shot helper with 100% 3D blend, logarithmic rolloff and source volumes 0.10 / 0.13. These are short tactile metal sounds rather than UI clicks or stingers.

InspectableNote optionally uses dedicated key pickup audio and suppresses its generic inspect tap. It checks the existing progress flag before marking it, so only a first acquisition plays the ring. Both the physical spare key and legacy cabinet interaction enable this option, sharing Room404KeyTaken.

InspectableDocument optionally identifies a key evidence document. ArchiveHUD suppresses its generic opening/inspection/page sounds and starts the heavy ring only when Viewed reports newly recorded evidence. KeyEvidenceFound is still recorded on page index 1. The UI source is stopped and other UI sound starts are temporarily suppressed for 0.9 seconds, covering the acquisition sequence without changing page or button behavior.

After 0.4 seconds of unscaled time, a quiet, lower-pitched latch plays at a point 2.1m behind the player's view at acquisition time (clamped inside intervening walls). The position is fixed, allowing the player to turn toward the original location. A scene change cancels the delayed cue. Source volume is 0.06. This does not spawn an enemy, modify story flags, or trigger the separate sparse corridor silhouette system.

The expanded actual CASE 001 Play Mode smoke test verifies dedicated spare AudioSource playback and volume, acquisition deduplication via the cabinet, no heavy/rear cue on the front page, immediate heavy playback with the reverse evidence flag, quiet UI source, approximately 0.4-second rear delay and rear position, page cycling and reopening without repeat acquisition audio. It preserves the original saved progress.

Manual follow-up: listen with headphones for metal timbre, perceived loudness and rear localization on the user's audio hardware. Programmatic AudioSource checks do not establish subjective sound quality.

## Actual results — 2026-10-07

Unity 6000.3.25f1 GUI Editor: script compilation settled successfully; actual Play Mode full CASE 001 smoke test PASS, 192 assertions. Actual playing spare/heavy AudioSources had spatialBlend=1 and the specified volume limits; front page produced no heavy/rear cue; UI source was silent during evidence acquisition. Playback counters after the full test were spare=1, evidence=1, rear=1, including cabinet reread, page cycling and reopening tests. Measured rear delay was 0.4072647 seconds and its recorded position was behind the acquisition view.

Console at completion: 0 errors / 0 runtime exceptions, with 4 MCP connection/disposal warnings inspected directly from the Editor Console. The original saved case progress was restored by the smoke test. Motel audio options were saved via Unity MCP / Editor APIs; other documents, world visuals, movement and case completion passed their existing checks.
