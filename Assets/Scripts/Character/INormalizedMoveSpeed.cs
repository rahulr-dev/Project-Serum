using System;
using UnityEngine;

namespace Character
{
    public interface INormalizedMoveSpeed
    {
        float NormalizedSpeed { get; }
    }

    public interface IJumpEvents
    {
        event Action OnJumped;
        event Action OnLanded;
    }

    public static class CharacterMotorUtil
    {
        public static INormalizedMoveSpeed FindSpeedSource(
            Component host,
            MonoBehaviour assigned,
            SideScrollerController locomotion)
        {
            if (assigned is INormalizedMoveSpeed fromAssigned)
                return fromAssigned;

            if (locomotion != null)
                return locomotion;

            if (host == null)
                return null;

            MonoBehaviour[] behaviours = host.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is INormalizedMoveSpeed speed)
                    return speed;
            }

            return null;
        }

        public static void BindJumpEvents(Component host, Action jumped, Action landed, ref Action unbind)
        {
            unbind?.Invoke();
            unbind = null;

            if (host == null)
                return;

            SideScrollerController motor = host.GetComponent<SideScrollerController>();
            if (motor == null)
            {
                IJumpEvents jump = host.GetComponent<IJumpEvents>();
                if (jump == null)
                    return;

                jump.OnJumped += jumped;
                jump.OnLanded += landed;
                unbind = () =>
                {
                    jump.OnJumped -= jumped;
                    jump.OnLanded -= landed;
                };
                return;
            }

            motor.OnJumped += jumped;
            motor.OnLanded += landed;
            unbind = () =>
            {
                motor.OnJumped -= jumped;
                motor.OnLanded -= landed;
            };
        }
    }
}
