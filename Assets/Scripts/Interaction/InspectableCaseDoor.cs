using System.Collections;
using UnityEngine;

namespace Archive0317
{
    public sealed class InspectableCaseDoor : Inspectable
    {
        private enum DoorState { Closed, Opening, Open }

        [SerializeField] private CaseDefinition definition;
        [SerializeField] private Transform leaf;
        [SerializeField] private string[] requirements;
        [SerializeField] private string firstFlag,openFlag,enteredFlag,exitFlag;
        [SerializeField] private AudioSource receiver,latch;

        private Quaternion closed;
        private DoorState state = DoorState.Closed;
        private Coroutine transitionRoutine;
        private Coroutine receiverRoutine;
        private float nextLockedFeedbackAt;

        public bool IsOpen => state == DoorState.Open;
        public bool CuePlayed { get; private set; }
        public override bool IsInteractionAvailable => state == DoorState.Closed && receiverRoutine == null && isActiveAndEnabled;
        public override float InteractionCooldown => .3f;
        public override bool PlayInspectSound => false;
        public override string Prompt => state == DoorState.Open ? "열림" :
            state == DoorState.Opening ? "여는 중…" :
            receiverRoutine != null ? "듣는 중…" : "E 열기";

        public void Configure(CaseDefinition data,Transform panel,string[] conditions,string first,string opened,string entered,string exit,AudioSource sound,AudioSource click)
        {
            definition=data;
            leaf=panel;
            requirements=conditions;
            firstFlag=first;
            openFlag=opened;
            enteredFlag=entered;
            exitFlag=exit;
            receiver=sound;
            latch=click;
        }

        private void Awake()
        {
            if (leaf != null) closed = leaf.localRotation;
        }

        private void Start()
        {
            if (definition == null || leaf == null) return;
            var progress = CaseProgressStore.Get(definition);
            if (progress.Has(openFlag))
                SetOpen(!progress.Has("RoomAltered") || progress.Has(exitFlag));
        }

        public override void Inspect(FirstPersonPlayer player)
        {
            if (!IsInteractionAvailable || player == null || definition == null) return;

            var progress=CaseProgressStore.Get(definition);

            if (!progress.Has(firstFlag))
            {
                LockedFeedback(player, "문이 잠겨 있다.");
                CaseProgressStore.Mark(definition,firstFlag);
                QueueReceiverCue();
                return;
            }

            foreach(var flag in requirements ?? System.Array.Empty<string>())
            {
                if(!progress.Has(flag))
                {
                    LockedFeedback(player, "문이 잠겨 있다.");
                    return;
                }
            }

            if(progress.Has(enteredFlag) && !progress.Has(exitFlag))
            {
                LockedFeedback(player, "손잡이가 움직이지 않는다.");
                return;
            }

            transitionRoutine = StartCoroutine(OpenRoutine());
        }

        private void LockedFeedback(FirstPersonPlayer player, string message)
        {
            if (Time.unscaledTime < nextLockedFeedbackAt) return;
            nextLockedFeedbackAt = Time.unscaledTime + .7f;
            InteractionSoundscape.PlayLockedDoor(transform.position);
            player.HUD.ShowToast(message);
        }

        private void QueueReceiverCue()
        {
            if (receiverRoutine != null || CuePlayed || receiver == null) return;
            receiverRoutine = StartCoroutine(ReceiverRoutine());
        }

        private IEnumerator ReceiverRoutine()
        {
            yield return new WaitForSeconds(1.4f);
            if (receiver != null) receiver.Play();
            CuePlayed = true;
            receiverRoutine = null;
        }

        private IEnumerator OpenRoutine()
        {
            state=DoorState.Opening;

            yield return new WaitForSeconds(1f);
            if (state != DoorState.Opening) yield break;

            if(latch!=null) latch.Play();
            else InteractionSoundscape.PlayDoorLatch(leaf.position);

            foreach(var c in leaf.GetComponentsInChildren<Collider>())
                c.enabled=false;

            var start=leaf.localRotation;
            var end=closed*Quaternion.Euler(0,-95,0);
            InteractionSoundscape.PlayDoorCreak(leaf.position);

            const float duration=2.4f;
            for(float t=0;t<duration;t+=Time.deltaTime)
            {
                if (state != DoorState.Opening) yield break;
                leaf.localRotation=Quaternion.Slerp(start,end,t/duration);
                yield return null;
            }

            leaf.localRotation=end;
            state=DoorState.Open;
            CaseProgressStore.Mark(definition,openFlag);
            transitionRoutine=null;
        }

        public void SetOpen(bool open)
        {
            if(leaf==null)return;

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            leaf.localRotation=closed*Quaternion.Euler(0,open?-95:0,0);
            foreach(var c in leaf.GetComponentsInChildren<Collider>())
                c.enabled=!open;
            state=open?DoorState.Open:DoorState.Closed;
        }

        private void OnDisable()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }
            if (receiverRoutine != null)
            {
                StopCoroutine(receiverRoutine);
                receiverRoutine = null;
            }
            if (state == DoorState.Opening)
                SetOpen(false);
        }
    }
}
