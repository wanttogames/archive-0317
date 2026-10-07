# CASE 001 physical key visuals

The **Archive 03:17 / Dress CASE 001 Keys** Editor menu applies this additive pass to the existing motel scene. Unity APIs create/save the meshes, materials, connected visual prefabs and scene. Assets are self-contained under `Assets/Prefabs/MotelKeys`; no external asset or package was added.

The front cabinet now has a visible brass key, metal rings and a faded 404 plastic tag. Its actual collider is raycast inspectable and grants the existing `Room404KeyTaken` flag. The shared CaseEnvironmentState hides it after pickup and when restoring acquired progress. The old cabinet interaction remains compatible with existing saves and flow.

The existing TV-triggered Room403KeyEvidence object retains its activation and progress rules. Its simple box renderer is replaced by a physical key/ring/403 plastic tag with wear and a 404 reverse. The model is oriented away from the bed so the metal key is visible. Its interaction remains on the original object, with the prompt `E 키 태그 조사`.

InspectableDocument now optionally supports a texture and caption per page, falling back to the existing single-image API for other documents. The existing image viewer shows a worn key illustration: page 1 front 403, page 2 reverse 404 with tape. Only the existing evidence page (index 1) records `KeyEvidenceFound`. Existing notebook/report text is preserved. Image pages keep the existing document text internally while hiding its visual text viewport.

The expanded CASE 001 actual Play Mode smoke test verifies the spare key is rendered, Raycast acquisition and disappearance, the TV-triggered physical evidence key, distinct front/reverse images in the actual UI, and that front inspection does not record reverse evidence. Native Game View captures are saved in `MotelKeys/`. The full test also checks ArchiveRoom, stairs, numbered doors, rear cues, the 403 interior and case completion, backing up/restoring the user's original progress.

Manual follow-up: verify interaction aim comfort, physical keyboard/mouse feel and brightness on the user's display.

## Actual results — 2026-10-07

Unity 6000.3.25f1 GUI Editor: compile settled successfully, actual Play Mode full regression PASS with 181 passing assertions. Front 403 inspection leaves `KeyEvidenceFound` unset; only reverse 404 records it. Actual key acquisition, removal, physical TV-triggered appearance and both UI textures passed. Game View images `Spare404.png`, `EvidenceWorld.png`, `Front403.png` and `Back404.png` were captured in Play Mode and inspected; the spare label orientation and evidence key placement were refined after the first capture and the complete suite was rerun.

Console at final test completion: 0 errors / 0 runtime exceptions; 7 MCP connection/disposal lifecycle warnings, inspected directly via Editor Console APIs. The motel scene and connected prefabs were saved through Unity Editor APIs. Original saved case progress was restored by the test.
