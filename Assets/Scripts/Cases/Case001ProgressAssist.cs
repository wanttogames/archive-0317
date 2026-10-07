using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archive0317
{
    /// <summary>
    /// CASE 001 pacing assistance:
    /// - gives progressively more specific hints only after no investigation progress for a while;
    /// - repairs proven prerequisite flags from later milestones for old/partial saves;
    /// - restores Room 403 re-entry if a resume happens after the room sealed but before completion.
    /// No hint or repair is required to advance a normal playthrough.
    /// </summary>
    public sealed class Case001ProgressAssist : MonoBehaviour
    {
        private const float FirstHintDelay = 75f;
        private const float FollowupHintDelay = 95f;
        private const float RepeatHintDelay = 120f;

        private CaseDefinition definition;
        private FirstPersonPlayer player;
        private InspectableCaseDoor room403Entrance;
        private string progressSignature;
        private float nextHintAt;
        private float nextEvaluate;
        private int hintLevel;
        private bool resumeRepairShown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Attach()
        {
            if (SceneManager.GetActiveScene().name != "Case001_Motel") return;
            if (Object.FindFirstObjectByType<Case001ProgressAssist>() != null) return;
            new GameObject("Case001ProgressAssist").AddComponent<Case001ProgressAssist>();
        }

        private void Start()
        {
            var context = Object.FindFirstObjectByType<CaseSceneContext>();
            definition = context != null ? context.Definition : null;
            player = Object.FindFirstObjectByType<FirstPersonPlayer>();
            if (definition == null || definition.Id != "case001" || player == null)
            {
                enabled = false;
                return;
            }

            ResolveRoom403Entrance();
            RepairDerivedProgress();
            progressSignature = Signature(CaseProgressStore.Get(definition));
            ResetHintTimer();
        }

        private void Update()
        {
            if (definition == null || player == null || Time.unscaledTime < nextEvaluate) return;
            nextEvaluate = Time.unscaledTime + .35f;

            var progress = CaseProgressStore.Get(definition);
            RepairDerivedProgress();
            RestoreRoom403Reentry(progress);

            string signature = Signature(progress);
            if (signature != progressSignature)
            {
                progressSignature = signature;
                hintLevel = 0;
                ResetHintTimer();
                return;
            }

            if (ShouldSuppressHints(progress) || Time.unscaledTime < nextHintAt) return;

            string hint = BuildHint(progress, hintLevel);
            if (!string.IsNullOrEmpty(hint))
            {
                player.HUD.ShowToast(hint, hintLevel == 0 ? 5f : 6.5f);
                hintLevel = Mathf.Min(2, hintLevel + 1);
            }

            nextHintAt = Time.unscaledTime + (hintLevel >= 2 ? RepeatHintDelay : FollowupHintDelay);
        }

        private bool ShouldSuppressHints(CaseProgress progress)
        {
            if (progress.Has("Room403Completed")) return true;
            if (player.HUD.IsCaseOpen || PauseMenuController.IsOpen || MainMenuController.IsMenuOpen || SceneTransitionManager.IsTransitioning) return true;
            return false;
        }

        private void ResetHintTimer()
        {
            nextHintAt = Time.unscaledTime + FirstHintDelay;
        }

        private string BuildHint(CaseProgress p, int level)
        {
            bool direct = level > 0;

            if (!p.Has("LedgerInspected"))
                return direct
                    ? "프런트 책상 위 숙박 장부에서 최민수의 객실 기록을 확인해 보자."
                    : "현장 기록은 프런트에서 시작하는 편이 좋겠다.";

            if (!p.Has("Room404KeyTaken"))
                return direct
                    ? "프런트의 예비 열쇠 보관함을 확인하면 404호를 열 수 있다."
                    : "장부의 객실을 확인했다. 그 방에 들어갈 방법이 프런트에 있을 것이다.";

            if (!p.Has("Room404Entered"))
                return direct
                    ? "예비 열쇠로 4층 404호 문을 확인해 보자."
                    : "열쇠와 장부가 가리키는 객실을 직접 확인해야 한다.";

            if (!p.Has("Room404PaperInspected"))
                return direct
                    ? "404호 안에 남겨진 종이 기록을 찾아 읽어 보자."
                    : "객실 안에 투숙객이 남긴 기록이 있을지도 모른다.";

            if (!p.Has("MissingRoomNoticed"))
            {
                if (!p.Has("Room402PlateSeen") || !p.Has("Room404PlateSeen"))
                    return direct
                        ? "4층 복도의 402호와 404호 번호판을 차례로 확인해 보자."
                        : "복도의 객실 번호 배치가 기록과 맞는지 살펴볼 필요가 있다.";
                return "장부에는 403호가 있었다. 지금 복도에서 그 번호가 보이는지 다시 확인해 보자.";
            }

            if (!p.Has("CorridorAltered"))
                return direct
                    ? "404호에서 나와 복도와 천장 조명을 다시 바라보자."
                    : "404호 조사 전과 지금의 복도가 같은지 확인해 보자.";

            if (!p.Has("CCTVContradictionFound"))
                return direct
                    ? "1층 프런트의 CCTV 모니터에서 4층 복도 녹화를 확인해 보자."
                    : "현재 복도와 과거 기록을 비교할 방법이 프런트에 있다.";

            if (!p.Has("Room403Revealed"))
                return direct
                    ? "4층 복도로 돌아가 사라졌던 403호 자리와 주변을 다시 확인해 보자."
                    : "녹화 확인 이후 4층에 변화가 생겼을 가능성이 있다.";

            if (!p.Has("Room403Opened"))
            {
                if (!p.Has("Room403DoorInspected"))
                    return direct
                        ? "나타난 403호 문을 직접 조사해 보자."
                        : "새로 나타난 문에서 뭔가 들리는 것 같다.";
                return direct
                    ? "403호 문을 한 번 더 확인해 보자. 잠금 상태가 달라졌을 수 있다."
                    : "조금 전과 같은 문인데, 손잡이의 느낌이 달라진 것 같다.";
            }

            if (!p.Has("Room403Entered"))
                return "열린 403호 문 안으로 들어가 내부를 확인해 보자.";

            if (!p.Has("BagInspected") || !p.Has("ReceiptInspected") || !p.Has("PersonalNoteInspected") || !p.Has("PhoneInspected"))
                return direct
                    ? "403호의 가방, 영수증, 수첩, 전화기를 모두 확인해 보자."
                    : "방 안의 개인 물품과 전화기에 아직 확인할 것이 남아 있다.";

            if (p.Has("PhoneEventTriggered") && !p.Has("PhoneEventAnswered"))
                return direct
                    ? "울리는 전화기의 수화기를 들어 보자."
                    : "403호에서 울리는 소리를 그냥 지나칠 수는 없다.";

            if (!p.Has("PhoneEventAnswered"))
                return "조사한 물건들 중 전화기에 다시 변화가 없는지 확인해 보자.";

            if (!p.Has("BathroomVisited"))
                return direct
                    ? "403호 안쪽 욕실을 직접 확인해 보자."
                    : "방 안에서 아직 들어가 보지 않은 공간이 있다.";

            if (!p.Has("BathroomRevisited"))
                return direct
                    ? "욕실을 나온 뒤 다시 한 번 들어가 보자."
                    : "욕실 쪽에서 방금 전과 다른 기척이 느껴진다.";

            if (!p.Has("RoomAltered"))
                return "욕실에서 나와 방 안을 다시 천천히 둘러보자.";

            if (!p.Has("TelevisionInspected"))
            {
                if (!p.Has("TelevisionPowerOn"))
                    return "욕실 밖으로 나와 방 안의 전자기기에 변화가 없는지 확인해 보자.";
                return direct
                    ? "혼자 켜진 TV 화면을 가까이서 조사해 보자."
                    : "켜진 화면이 보여 주는 장소를 확인해야 한다.";
            }

            if (!p.Has("KeyEvidenceFound"))
                return direct
                    ? "TV를 확인한 뒤 방 중앙 바닥 근처에 새로 나타난 물건을 찾아보자."
                    : "화면을 본 뒤 방 안에 없던 물건이 생긴 것 같다.";

            if (!p.Has("ReceiptChangedSeen"))
                return direct
                    ? "처음 확인했던 숙박 영수증을 다시 읽어 보자."
                    : "키 태그의 두 번호가 기존 기록 하나를 다시 확인하게 만든다.";

            if (!p.Has("Room403ExitOpened"))
                return direct
                    ? "키 태그을 확보했다. 403호 출구 손잡이를 다시 확인해 보자."
                    : "방을 나갈 방법이 이제 달라졌을지도 모른다.";

            if (!p.Has("Room403Completed"))
                return "403호를 나가 복도에 무엇이 남아 있는지 확인해 보자.";

            return string.Empty;
        }

        private void RepairDerivedProgress()
        {
            var p = CaseProgressStore.Get(definition);

            // Only infer prerequisites when a later flag proves that the earlier step must have happened.
            if (p.Has("Room404Entered")) CaseProgressStore.Mark(definition, "Room404KeyTaken");
            if (p.Has("Room404PaperInspected"))
            {
                CaseProgressStore.Mark(definition, "Room404Entered");
                CaseProgressStore.Mark(definition, "Room404KeyTaken");
            }
            if (p.Has("CorridorAltered")) CaseProgressStore.Mark(definition, "MissingRoomNoticed");
            if (p.Has("CCTVContradictionFound"))
            {
                CaseProgressStore.Mark(definition, "CorridorAltered");
                CaseProgressStore.Mark(definition, "MissingRoomNoticed");
            }
            if (p.Has("Room403Revealed")) CaseProgressStore.Mark(definition, "CCTVContradictionFound");
            if (p.Has("Room403Opened")) CaseProgressStore.Mark(definition, "Room403Revealed");
            if (p.Has("Room403Entered"))
            {
                CaseProgressStore.Mark(definition, "Room403Opened");
                CaseProgressStore.Mark(definition, "Room403Revealed");
            }
            if (p.Has("TelevisionInspected"))
            {
                CaseProgressStore.Mark(definition, "TelevisionPowerOn");
                CaseProgressStore.Mark(definition, "RoomAltered");
            }
            if (p.Has("KeyEvidenceFound"))
            {
                CaseProgressStore.Mark(definition, "TelevisionInspected");
                CaseProgressStore.Mark(definition, "KeyTagVisible");
            }
            if (p.Has("ReceiptChangedSeen")) CaseProgressStore.Mark(definition, "KeyEvidenceFound");
            if (p.Has("Room403Completed"))
            {
                CaseProgressStore.Mark(definition, "KeyEvidenceFound");
                CaseProgressStore.Mark(definition, "Room403Entered");
            }
        }

        private void RestoreRoom403Reentry(CaseProgress p)
        {
            // A resumed save spawns back in the motel scene. If the horror sequence had already
            // closed the exterior 403 door before the key was obtained, the normal door guard
            // would reject re-entry forever. Re-open only the corridor-side door in that resume state.
            if (!p.Has("Room403Entered") || p.Has("Room403Completed") || p.Has("KeyEvidenceFound") || !p.Has("RoomAltered")) return;
            if (IsInsideRoom403()) return;

            if (room403Entrance == null) ResolveRoom403Entrance();
            if (room403Entrance == null || room403Entrance.IsOpen) return;

            room403Entrance.SetOpen(true);
            CaseProgressStore.Mark(definition, "Room403ResumeAccessRestored");
            if (!resumeRepairShown)
            {
                resumeRepairShown = true;
                player.HUD.ShowToast("403호 조사 경로를 복구했다. 내부 조사를 계속할 수 있다.", 4.5f);
            }
        }

        private void ResolveRoom403Entrance()
        {
            var transform = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == "RoomDoor403" && t.position.x > -3f);
            room403Entrance = transform != null ? transform.GetComponent<InspectableCaseDoor>() : null;
        }

        private bool IsInsideRoom403()
        {
            Vector3 p = player.transform.position;
            return p.x < -17.65f && p.x > -22.6f && p.y > 8.35f && p.z > 1.9f && p.z < 7.1f;
        }

        private static string Signature(CaseProgress p)
        {
            int facts = p.facts != null ? p.facts.Count : 0;
            int evidence = p.evidenceIds != null ? p.evidenceIds.Count : 0;
            int flags = p.flags != null ? p.flags.Count : 0;
            return flags + ":" + facts + ":" + evidence + ":" + (p.verdict ?? "");
        }
    }
}
