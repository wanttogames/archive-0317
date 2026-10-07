# Open 404 door / corridor number investigation

The bug was reproduced in the running motel Play Mode: the 404 door was open, but its NumberPlate BoxCollider was disabled. InspectableDoor disabled every child collider while opening, including the plate's InspectableNote collider. Since player investigation ignores disabled colliders, players who opened the door before inspecting the number could not complete the corridor-number objective.

After opening finishes, InspectableDoor now restores child colliders with their own Inspectable component. The solid door leaf remains disabled for passage. This preserves the existing one-way opening behavior and keeps the actual numberplate raycastable from the corridor without marking clues automatically.

The CASE 001 smoke test now removes the pre-open 404 plate observation, investigates the open door's actual plate through the player Raycast, and verifies that MissingRoomNoticed can be obtained. The existing entry, exit, document and case completion checks remain in place. Test execution backs up and restores the original saved case progress.

Actual validation on 2026-10-07: Unity 6000.3.25f1 GUI Editor, script compile settled successfully, actual Play Mode regression PASS with 175 passing assertions. Both new open-door plate and objective checks passed. Console at test completion: 0 errors / 0 runtime exceptions, with 6 MCP plugin connection/disposal warnings. Physical keyboard/mouse feel remains a manual check.
