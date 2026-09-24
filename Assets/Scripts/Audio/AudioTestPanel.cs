using UnityEngine;

namespace Serum.Audio
{
    public class AudioTestPanel : MonoBehaviour
    {
        public Animator character;
        public FootstepController footsteps;
        public AudioSource ambience;
        float speed = 1f;

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 340, 260), GUI.skin.box);
            GUILayout.Label("SERUM AUDIO TEST");
            GUILayout.Label("Running in place: footsteps come from clip events.");
            GUILayout.Label("Animation speed: " + speed.ToString("0.00"));
            speed = GUILayout.HorizontalSlider(speed, 0f, 2f);
            if (character != null) character.speed = speed;
            if (GUILayout.Button("Left foot")) footsteps.FootstepLeft();
            if (GUILayout.Button("Right foot")) footsteps.FootstepRight();
            if (GUILayout.Button("Landing")) footsteps.Landing();
            if (GUILayout.Button("Toggle forest ambience")) ambience.mute = !ambience.mute;
            GUILayout.Label("Set speed to zero to test individual sounds.");
            GUILayout.EndArea();
        }
    }
}
