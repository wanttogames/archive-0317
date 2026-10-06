# Archive 03:17 — playable basement archive

Open `Assets/Scenes/ArchiveRoom.unity` and press Play. Click the Game View if Unity has released the cursor.

## Controls

| Input | Action |
| --- | --- |
| WASD | Walk |
| Mouse | Look, with vertical angle limited to ±80° |
| Left / right Shift | Run |
| E | Inspect CASE 001 within 2.5 metres; close the description |
| TAB | Open collected case records in the motel; compare records after investigating 404 |
| ESC | Toggle cursor lock; close an open description and return to play |
| Left click while unlocked | Return to captured gameplay |

The small centre crosshair and `E 조사` prompt appear during captured gameplay. The raycast respects the first collider, so walls and furniture block inspection. Reading a file stops movement and looking. Losing application focus releases the cursor.

## Project layout

- `Assets/Scenes/ArchiveRoom.unity`: saved playable room, first enabled build scene; SampleScene remains in the project.
- `Assets/Scripts/Player/FirstPersonPlayer.cs`: Input System keyboard/mouse, CharacterController movement, gravity, sprint, cursor state, raycast.
- `Assets/Scripts/Interaction/CaseFile.cs`: editable title and Korean case description.
- `Assets/Scripts/UI/ArchiveHUD.cs`: crosshair, prompt, cursor hint, case description panel.
- `Assets/Prefabs/Player/FirstPersonPlayer.prefab`: controller, camera, AudioListener and HUD together, preserving internal references.
- `Assets/Prefabs/Environment/InvestigationDesk.prefab` and `CaseStorageShelf.prefab`: collidable furniture.
- `Assets/Prefabs/Interaction/Case001.prefab`: collidable folder, label and case content.
- `Assets/Materials/ArchiveRoom`: URP materials and atmosphere VolumeProfile.
- `Assets/Fonts/NotoSansCJKkr-Regular.otf`: bundled Korean font with its SIL OFL license in `NotoSansCJK-LICENSE.txt`. Source: https://github.com/notofonts/noto-cjk/tree/main/Sans/OTF/Korean . The previous local Windows font is not used or included in the commit.
- `Assets/Editor/ArchiveRoomBuilder.cs`: reproducible scene construction menu. It refuses to overwrite an existing scene.
- `Assets/Editor/ArchiveRoomSmokeTest.cs`: Play Mode component checks from `Archive 03:17/Run Play Mode Smoke Test`.
- `Assets/Editor/ArchiveRoomVisualPass.cs`: Editor-only visual pass on the existing scene; it preserves gameplay objects and refuses to duplicate the details root.
- `Assets/Textures/ArchiveRoom`: 128px point-filtered, mipmapped, original procedural textures.
- `Assets/Prefabs/Environment/Institutional`: CRT, telephone, office chair, notice board and wall clock.

Runtime scripts have no UnityEditor dependency. Existing URP assets, packages, input backend and tutorial assets are retained. The archive room uses static cold fluorescent fixtures, concrete walls/floor/ceiling, archive shelves and a desk. Its environment remains unchanged by the CASE 001 field slice.

## CASE 001 — 404호 field slice

Start Play Mode in `ArchiveRoom`, inspect CASE 001 with E, and select `현장 조사 시작`. A brief fade loads `Assets/Scenes/Cases/Case001_Motel.unity`. Both enabled build scenes use the same FirstPersonPlayer prefab, including the Korean document viewer, cursor control and a single EventSystem. Read documents using the mouse buttons; E or ESC closes the viewer and resumes movement. Brief observations stay at the bottom of the screen instead of opening a document.

The fictional case concerns 최민수, registered on 2002-10-11. The official archive record lists room 404. At the motel, inspect the reception ledger and select `세부 기록 확인` to read its room 403 entry. Collect the 404 spare key from the cabinet behind reception. The small office beside the counter contains the initial CCTV entrance record and later a fourth-floor recording. The entrance, telephone, clock, extinguisher, numbered doors and exit can also be inspected.

Walk through the first-floor corridor and climb six physical stair flights, with landings, to the fourth floor. Initially, rooms are numbered 401, 402, 404 and 405. Inspect the 402 and 404 plates after reading the ledger; only a short `…403호는 어디 있지?` observation is displayed. Open 404 with its key and inspect the bed, nightstand, telephone, bathroom door, personal bag and paper on the small desk. After reading the paper, press TAB to revisit the official record and collected field notes, then choose `객실 기록 대조`. This first comparison displays `기록이 일치하지 않는다.` and continues into the middle section. There is no case resolution, return transition, monster or chase. The quiet distant latch on the first upper landing remains once per case progress record.

## CASE 001 middle section — 존재하지 않는 403호

After noticing the missing number and investigating 404's paper, leave 404. One fourth-floor fluorescent is now off, applied only while its tube is out of sight; no change notification appears. The notebook comparison remains available, but pressing that button is not required to continue when the player has already observed the numbered plates and investigated 404. Return to the office and inspect the CRT again. The existing document card displays a 512×288 monochrome RenderTexture of a separate recorded corridor, with 401/402/403/404 plates and a stationary clothed guest in a paused recording. Its single timestamp is `2002-10-11 03:17`, consistent with the existing case date. The frame refreshes at 4Hz and has faint local grain/scan lines. The previous document timestamp was removed to prevent repetition.

On returning to the fourth-floor entrance, two short spatial telephone rings originate near the missing door position. After this cue starts, the plain wall between 402 and 404 may be replaced by a 403 door, only when that wall is outside the player camera view or occluded. The same door style and a slightly crooked plaque avoid a conspicuous special-room design. E displays only `문이 잠겨 있다.`; 1.4 seconds later a faint bell comes from behind it. The leaf and opaque backing remain solid. There is no 403 interior or further story progression. Exploration continues after `Room403DoorInspected`.

`CaseEnvironmentState` contains serialized conditions and small ordered state rules for activation, position, rotation, light state and world labels, optional volumes, visibility gating, a short delay and one-shot audio. It has no motel object names or room numbers in its runtime code. Completed state changes are reapplied on scene load, while audio observations do not replay. `InspectableCCTV` and `InspectableSoundNote` reuse the shared Raycast/HUD. The CCTV camera and its geometry use numeric layer 30 exclusively, excluded from the motel player camera. There is no extra AudioListener. Original procedural facility hum, ventilation/pipe air and CRT electrical room tone are spatial, low-volume loops; no BGM is present. The saved flags include LedgerInspected, RoomNumberMismatchFound, CorridorAltered, CCTVContradictionFound, PhoneLeadPlayed, Room403Revealed and Room403DoorInspected, plus the individual plate observations.

The motel has low ceilings, 128px wallpaper/carpet textures, mixed warm and cool fixtures, matte furnishings, plastic keyholders, a CRT, damp wall strips and exposed cable ducts. The existing archive atmosphere profile and institutional prop prefabs are reused. Motel world labels use a small URP depth-tested font shader, with font-atlas updates handled by `WorldTextDepth`, so room numbers do not appear through walls. The overlay document UI retains its original font material. This is a compact investigation slice intended for approximately 5–10 minutes of exploration and reading; its duration is not enforced and needs a human pacing review.

`CaseDefinition` holds the case ID, scene, official record, comparison prerequisites and flag-driven notebook entries. Additional cases can supply their own collected-note text without editing the HUD. `InspectableDocument`, `InspectableNote` and `InspectableDoor` use the shared Raycast through `Inspectable`. `CaseSceneContext` assigns a case to the existing HUD. `SceneTransitionManager` persists only the fade canvas, while each scene creates its own shared player instance. `CaseProgressStore` writes small JSON records to PlayerPrefs under `Archive0317.Case.<caseId>`; flags and evidence facts survive restarting Play Mode, and `CaseEnvironmentState` reconstructs completed spatial changes. Position, opening animations and the current scene are not saved. For a fresh CASE 001 developer run, delete only `Archive0317.Case.case001` in PlayerPrefs and clear the progress cache. No complex save-slot system is introduced.

Editor menu `Archive 03:17/Run CASE 001 Play Mode Smoke Test` runs from ArchiveRoom Play Mode. It executes the previous archive smoke test, invokes the real start button, waits for the fade/load, exercises collision and all six stair flights, investigates documents through Raycasts, opens 404, waits for physics triggers and compares records. It also checks the absent initial 403, both plate observations, light alteration, recorded 403/timestamp/nonblank monochrome frame, cue gating, visible-wall rejection, reveal after turning away, locked-door collision/delayed sound, and reconstructed state on an actual scene reload. It restores the pre-test case save and writes `Documentation/Verification/Case001SmokeTest.txt`. `Case001MotelBuilder` creates a missing scene without overwriting an existing one; `Case001Room403Builder` is an idempotent additive Editor pass on that base scene. Serialized scenes/prefabs/materials are authored through Editor APIs, not manual YAML edits.

## Visual direction

The room is now approximately 8 × 9 metres with a 2.75 metre ceiling, a support column, partial partition and lowered beam. Damp lower wall paint, aged plaster, worn tile-style linoleum, steel shelves and unevenly arranged binders suggest a late-1990s Korean institutional basement. Boxes, loose paper and a skewed office chair imply ambiguous recent use. The desk has a CRT, keyboard and wired telephone; the walls have a clock, exit sign, notice board, vent grille and cable duct. No windows were added.

CASE 001 keeps its existing placement and collider. A warm ochre cover and concentrated desk light guide the eye; the label is ordinary ink-coloured text, without glow or icons. The fluorescent tubes are visibly lit, with a dimmer far fixture and one unlit tube. All lights are static; no flicker, supernatural timing or horror event was introduced.

Materials use matte low-resolution textures with restrained colour quantisation. Film Grain intensity is 0.055. Pixelation/dithering were considered and omitted to protect file and Korean-text readability. Only the centre fixture casts a shadow; the other fixtures are inexpensive fill lights. No new package, chromatic aberration, VHS noise or camera motion effect was added.

## Verification

`Documentation/Verification/PlayModeSmokeTest.txt` records runtime checks for grounding, walk/run displacement, wall/desk/shelf collision, look limits, raycast targeting, Korean prompt state, case panel content, interaction range and cursor release. The wall check uses the scene's actual boundary after resizing. This invokes the same component methods used by gameplay; physical WASD/mouse/ESC key presses and window focus need a manual Game View pass.

Screenshots in `Documentation/Verification` include the player camera with the HUD and the Scene View. The native Game View request completed after a delay; `GameView.png` was then inspected as well, confirming the small centre crosshair and overall brightness. Camera-rendered images provide higher-resolution checks of CASE 001 targeting and the Korean description panel. The automated checks run in Play Mode and restore the player afterwards. Stop Play Mode before editing/saving the scene. Check final brightness on the target monitor.

The existing `MCP_Test_Cube` was removed from SampleScene and that scene was saved. ArchiveRoom contains no test cube.
