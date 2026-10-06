# Archive 03:17 — playable basement archive

Open `Assets/Scenes/ArchiveRoom.unity` and press Play. Click the Game View if Unity has released the cursor.

## Controls

| Input | Action |
| --- | --- |
| WASD | Walk |
| Mouse | Look, with vertical angle limited to ±80° |
| Left / right Shift | Run |
| E | Inspect CASE 001 within 2.5 metres; close the description |
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

Runtime scripts have no UnityEditor dependency. Existing URP assets, packages, input backend and tutorial assets are retained. The room uses static cold fluorescent fixtures, concrete walls/floor/ceiling, archive shelves and a desk. There are no scripted horror events or motel map.

## Visual direction

The room is now approximately 8 × 9 metres with a 2.75 metre ceiling, a support column, partial partition and lowered beam. Damp lower wall paint, aged plaster, worn tile-style linoleum, steel shelves and unevenly arranged binders suggest a late-1990s Korean institutional basement. Boxes, loose paper and a skewed office chair imply ambiguous recent use. The desk has a CRT, keyboard and wired telephone; the walls have a clock, exit sign, notice board, vent grille and cable duct. No windows were added.

CASE 001 keeps its existing placement and collider. A warm ochre cover and concentrated desk light guide the eye; the label is ordinary ink-coloured text, without glow or icons. The fluorescent tubes are visibly lit, with a dimmer far fixture and one unlit tube. All lights are static; no flicker, supernatural timing or horror event was introduced.

Materials use matte low-resolution textures with restrained colour quantisation. Film Grain intensity is 0.055. Pixelation/dithering were considered and omitted to protect file and Korean-text readability. Only the centre fixture casts a shadow; the other fixtures are inexpensive fill lights. No new package, chromatic aberration, VHS noise or camera motion effect was added.

## Verification

`Documentation/Verification/PlayModeSmokeTest.txt` records runtime checks for grounding, walk/run displacement, wall/desk/shelf collision, look limits, raycast targeting, Korean prompt state, case panel content, interaction range and cursor release. The wall check uses the scene's actual boundary after resizing. This invokes the same component methods used by gameplay; physical WASD/mouse/ESC key presses and window focus need a manual Game View pass.

Screenshots in `Documentation/Verification` are captured from the player camera with the HUD. Native Game View screenshot requests did not write a file in the connected Editor, so camera rendering was used as the verification fallback. The automated checks run in Play Mode and restore the player afterwards. Stop Play Mode before editing/saving the scene. Check final brightness on the target monitor.

The existing `MCP_Test_Cube` was removed from SampleScene and that scene was saved. ArchiveRoom contains no test cube.
