using UnityEngine;

namespace Hollow.Combat
{
    public sealed class RawMixamoAnimationDebugOverlay : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerAnimationSystemMode animationSystemMode = PlayerAnimationSystemMode.SimpleFullBodyAnimation;
        [SerializeField] private Rect overlayRect = new(18f, 18f, 380f, 230f);

        private string requestedState = "Idle";

        private static readonly string[] StateOrder =
        {
            "Idle",
            "Walk",
            "Run",
            "Attack",
            "GuardBlock",
            "HitReaction",
            "Death"
        };

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            ResolveReferences();
            for (var index = 0; index < StateOrder.Length; index++)
            {
                if (DebugKeyboardInput.NumberWasPressed(index + 1))
                {
                    PlayState(StateOrder[index]);
                }
            }
        }

        public void Configure(Animator nextAnimator, PlayerAnimationSystemMode nextAnimationSystemMode)
        {
            animator = nextAnimator;
            animationSystemMode = nextAnimationSystemMode;
        }

        private void OnGUI()
        {
            ResolveReferences();
            GUILayout.BeginArea(overlayRect, GUI.skin.box);
            GUILayout.Label("Raw Mixamo Animation Debug");
            GUILayout.Label($"Mode: {animationSystemMode}");
            GUILayout.Label($"Requested: {requestedState}");
            GUILayout.Label($"State: {CurrentStateLabel()}");
            GUILayout.Label($"Clip: {CurrentClipLabel()}");

            GUILayout.BeginHorizontal();
            for (var index = 0; index < StateOrder.Length; index++)
            {
                if (GUILayout.Button($"{index + 1} {StateOrder[index]}", GUILayout.Height(24f)))
                {
                    PlayState(StateOrder[index]);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void PlayState(string stateName)
        {
            ResolveReferences();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            requestedState = stateName;
            animator.Play(stateName, 0, 0f);
            animator.Update(0f);
        }

        private string CurrentStateLabel()
        {
            if (animator == null || animator.runtimeAnimatorController == null || animator.layerCount == 0)
            {
                return "<none>";
            }

            var info = animator.GetCurrentAnimatorStateInfo(0);
            foreach (var state in StateOrder)
            {
                if (info.IsName(state))
                {
                    return state;
                }
            }

            return $"hash:{info.shortNameHash}";
        }

        private string CurrentClipLabel()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return "<none>";
            }

            var clips = animator.GetCurrentAnimatorClipInfo(0);
            return clips.Length > 0 && clips[0].clip != null ? clips[0].clip.name : "<none>";
        }

        private void ResolveReferences()
        {
            animator ??= FindFirstObjectByType<Animator>();
        }
    }
}
