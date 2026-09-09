using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace InteractionSystem
{
    public enum InteractionActionType
    {
        AnimationPlay,
        AnimatorSetTrigger,
        AnimatorPlayState,
        ParticlePlay,
        ParticleStop,
        AudioPlay,
        AudioStop,
        DoorOpen,
        TimelinePlay,
        QTEStart,
        DialogueStart,
        InvokeEventPlay,
        CustomUnityEvent
    }

    [Serializable]
    public class InteractionAction
    {
        [SerializeField]
        private UnityEngine.Object targetObject;

        [SerializeField]
        private InteractionActionType actionType = InteractionActionType.CustomUnityEvent;

        [SerializeField]
        private string stringParam = "";

        [SerializeField]
        private UnityEvent customEvent = new UnityEvent();

        public UnityEngine.Object TargetObject
        {
            get => targetObject;
            set => targetObject = value;
        }

        public InteractionActionType ActionType
        {
            get => actionType;
            set => actionType = value;
        }

        public string StringParam
        {
            get => stringParam;
            set => stringParam = value;
        }

        public UnityEvent CustomEvent => customEvent;

        public InteractionAction()
        {
        }

        public InteractionAction(UnityEngine.Object target, InteractionActionType type)
        {
            targetObject = target;
            actionType = type;
        }

        public void Execute()
        {
            if (actionType == InteractionActionType.CustomUnityEvent)
            {
                customEvent?.Invoke();
                return;
            }

            if (targetObject == null)
            {
                Debug.LogWarning("[InteractionAction] Cannot execute action because targetObject is null.");
                return;
            }

            GameObject targetGo = targetObject as GameObject;
            Component targetComp = targetObject as Component;
            if (targetGo == null && targetComp != null)
            {
                targetGo = targetComp.gameObject;
            }

            switch (actionType)
            {
                case InteractionActionType.AnimationPlay:
                case InteractionActionType.AnimatorPlayState:
                    {
                        Animator anim = GetOrGetComponent<Animator>(targetObject);
                        if (anim != null)
                        {
                            if (!string.IsNullOrEmpty(stringParam))
                                anim.Play(stringParam);
                            else
                                anim.Play(0);
                        }
                        else
                        {
                            Debug.LogWarning($"[InteractionAction] No Animator found on target {targetObject.name}");
                        }
                    }
                    break;

                case InteractionActionType.AnimatorSetTrigger:
                    {
                        Animator anim = GetOrGetComponent<Animator>(targetObject);
                        if (anim != null)
                        {
                            anim.SetTrigger(stringParam);
                        }
                        else
                        {
                            Debug.LogWarning($"[InteractionAction] No Animator found on target {targetObject.name}");
                        }
                    }
                    break;

                case InteractionActionType.ParticlePlay:
                    {
                        ParticleSystem ps = GetOrGetComponent<ParticleSystem>(targetObject);
                        if (ps != null)
                        {
                            ps.Play();
                        }
                        else
                        {
                            Debug.LogWarning($"[InteractionAction] No ParticleSystem found on target {targetObject.name}");
                        }
                    }
                    break;

                case InteractionActionType.ParticleStop:
                    {
                        ParticleSystem ps = GetOrGetComponent<ParticleSystem>(targetObject);
                        if (ps != null)
                        {
                            ps.Stop();
                        }
                    }
                    break;

                case InteractionActionType.AudioPlay:
                    {
                        AudioSource audio = GetOrGetComponent<AudioSource>(targetObject);
                        if (audio != null)
                        {
                            audio.Play();
                        }
                        else
                        {
                            Debug.LogWarning($"[InteractionAction] No AudioSource found on target {targetObject.name}");
                        }
                    }
                    break;

                case InteractionActionType.AudioStop:
                    {
                        AudioSource audio = GetOrGetComponent<AudioSource>(targetObject);
                        if (audio != null)
                        {
                            audio.Stop();
                        }
                    }
                    break;

                case InteractionActionType.DoorOpen:
                    {
                        Door door = GetOrGetComponent<Door>(targetObject);
                        if (door != null)
                        {
                            door.Open();
                        }
                        else
                        {
                            Debug.LogWarning($"[InteractionAction] No Door component found on target {targetObject.name}");
                        }
                    }
                    break;

                case InteractionActionType.TimelinePlay:
                    {
                        PlayableDirector director = GetOrGetComponent<PlayableDirector>(targetObject);
                        if (director != null)
                        {
                            director.Play();
                        }
                        else
                        {
                            Debug.LogWarning($"[InteractionAction] No PlayableDirector found on target {targetObject.name}");
                        }
                    }
                    break;

                case InteractionActionType.InvokeEventPlay:
                    {
                        InvokeEvent evt = GetOrGetComponent<InvokeEvent>(targetObject);
                        if (evt != null)
                        {
                            evt.Play();
                        }
                    }
                    break;

                case InteractionActionType.QTEStart:
                case InteractionActionType.DialogueStart:
                    {
                        // Dynamic method call via reflection or public method invocation
                        var method = targetObject.GetType().GetMethod("StartQTE") ?? 
                                     targetObject.GetType().GetMethod("StartDialogue") ??
                                     targetObject.GetType().GetMethod("Play") ??
                                     targetObject.GetType().GetMethod("Interact");
                        if (method != null)
                        {
                            method.Invoke(targetObject, null);
                        }
                        else
                        {
                            Debug.LogWarning($"[InteractionAction] Target {targetObject.name} does not have a Play/StartQTE/StartDialogue method.");
                        }
                    }
                    break;

                default:
                    customEvent?.Invoke();
                    break;
            }
        }

        private T GetOrGetComponent<T>(UnityEngine.Object obj) where T : Component
        {
            if (obj is T directMatch) return directMatch;
            if (obj is GameObject go) return go.GetComponent<T>();
            if (obj is Component comp) return comp.GetComponent<T>();
            return null;
        }

        public string GetDisplayLabel()
        {
            string targetName = targetObject != null ? targetObject.name : "None";
            switch (actionType)
            {
                case InteractionActionType.AnimationPlay:
                    return $"{targetName} → Play()";
                case InteractionActionType.AnimatorSetTrigger:
                    return $"{targetName} → SetTrigger(\"{stringParam}\")";
                case InteractionActionType.AnimatorPlayState:
                    return $"{targetName} → Play(\"{stringParam}\")";
                case InteractionActionType.ParticlePlay:
                    return $"{targetName} → Play()";
                case InteractionActionType.ParticleStop:
                    return $"{targetName} → Stop()";
                case InteractionActionType.AudioPlay:
                    return $"{targetName} → Play()";
                case InteractionActionType.AudioStop:
                    return $"{targetName} → Stop()";
                case InteractionActionType.DoorOpen:
                    return $"{targetName} → Open()";
                case InteractionActionType.TimelinePlay:
                    return $"{targetName} → Play()";
                case InteractionActionType.QTEStart:
                    return $"{targetName} → StartQTE()";
                case InteractionActionType.DialogueStart:
                    return $"{targetName} → StartDialogue()";
                case InteractionActionType.InvokeEventPlay:
                    return $"{targetName} → Play()";
                case InteractionActionType.CustomUnityEvent:
                    return $"{targetName} → Custom Event";
                default:
                    return $"{targetName} → {actionType}";
            }
        }
    }
}
