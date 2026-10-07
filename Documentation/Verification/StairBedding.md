# Motel stair enclosure and bedding

The second- and third-floor intermediate landings previously had low guards without a wall above them. Full-height front walls now close those gaps, using the existing stairwell material and BoxColliders. Ground-floor access and the fourth-floor corridor opening remain open. The original motel builder also preserves these two walls when rebuilt.

Both room 404 and room 403 now use connected bedding prefabs under `Assets/Prefabs/MotelBedding`. They add a mattress edge, draped quilt, stitching, folded top edge, pillow volume and a 128px point-filtered woven texture. Room 404 has uneven folds; room 403 is more neatly tucked. Existing bed colliders and investigation objects are preserved; decorative bedding adds no colliders. Existing retro rendering and lighting remain in use.

To reapply bedding after rebuilding the scene with older builders, use **Archive 03:17 / Repair Stairs and Dress Motel Beds** in Edit Mode. This menu regenerates the assets and saves the motel scene through Unity APIs.

Validation uses the running Unity 6000.3.25f1 GUI Editor and Unity MCP. The expanded CASE 001 Play Mode smoke test checks nine wall rays per intermediate floor, two CharacterController boundary checks, all six stair flights, the fourth-floor corridor, existing investigation and completion paths. It also captures both beds from the actual Game View and restores the original player save after the test. Results are recorded in `Case001SmokeTest.txt`; additional wall collision results and Game View images are in `StairBedding/`.

Physical keyboard/mouse feel, monitor brightness and subjective appearance still require a human playthrough. Automated movement uses the existing player component's runtime methods.

## Actual results — 2026-10-07

- Unity GUI Editor: 6000.3.25f1; script compilation settled successfully.
- Actual Play Mode: PASS. Expanded CASE 001 smoke test: 173 passing assertions, including 20 new stair enclosure checks. ArchiveRoom regression checks and existing retro visual checks also ran.
- Console at test completion: 0 errors, 0 runtime exceptions; 7 warnings from the MCP plugin's connection/disposal lifecycle during domain reload. The Console was inspected directly using Unity Editor APIs.
- `Bed404Final.png` and `Bed403Final.png`: actual Game View screenshots inspected for quilt folds, mattress edges, pillow volume and preserved retro rendering. `Stair23.png` and `Stair34.png`: actual Game View stair wall inspection.
- Scene, meshes, materials and connected prefabs were saved through Unity Editor APIs. Original case progress was restored by the smoke test.
