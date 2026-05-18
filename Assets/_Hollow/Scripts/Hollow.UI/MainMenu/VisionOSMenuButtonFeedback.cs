using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hollow.UI.MainMenu
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public sealed class VisionOSMenuButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, ISubmitHandler
    {
        private Button button;
        private Image image;
        private Action action;
        private Action<string> statusSink;
        private Color baseColor;
        private Color pointerColor;
        private Color activatedColor;
        private bool disablesAfterActivation;
        private bool activated;
        private int lastActivationFrame = -1;

        public string StepName { get; private set; }

        public string ButtonLabel { get; private set; }

        public bool HasActivated => activated;

        public bool DisablesAfterActivation => disablesAfterActivation;

        public void Configure(
            string stepName,
            string buttonLabel,
            Color color,
            Action onActivated,
            Action<string> nextStatusSink,
            bool disableAfterActivation)
        {
            button = GetComponent<Button>();
            image = GetComponent<Image>();
            StepName = string.IsNullOrWhiteSpace(stepName) ? "Unknown" : stepName;
            ButtonLabel = string.IsNullOrWhiteSpace(buttonLabel) ? gameObject.name : buttonLabel;
            action = onActivated;
            statusSink = nextStatusSink;
            baseColor = color;
            pointerColor = Color.Lerp(color, Color.white, 0.18f);
            activatedColor = Color.Lerp(color, Color.white, 0.34f);
            disablesAfterActivation = disableAfterActivation;
            activated = false;

            image.color = baseColor;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(ActivateFromButton);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Report("pointer-down");
            Flash(pointerColor);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Report("click");
            Activate("click");
        }

        public void OnSubmit(BaseEventData eventData)
        {
            Report("submit");
            Activate("submit");
        }

        public void ActivateFromButton()
        {
            Activate("button");
        }

        public void ActivateForTests()
        {
            Activate("test");
        }

        private void Activate(string source)
        {
            if (lastActivationFrame == Time.frameCount || (disablesAfterActivation && activated) || button == null || !button.interactable)
            {
                return;
            }

            lastActivationFrame = Time.frameCount;
            activated = true;
            if (disablesAfterActivation)
            {
                button.interactable = false;
            }

            Flash(activatedColor);
            Report($"activate:{source}");
            action?.Invoke();
        }

        private void Flash(Color color)
        {
            if (image != null)
            {
                image.color = color;
            }

            if (Application.isPlaying && !activated)
            {
                CancelInvoke(nameof(RestoreColor));
                Invoke(nameof(RestoreColor), 0.12f);
            }
        }

        private void RestoreColor()
        {
            if (!activated && image != null)
            {
                image.color = baseColor;
            }
        }

        private void Report(string phase)
        {
            var message = $"VisionOS menu tap: {StepName}/{ButtonLabel} {phase}";
            Debug.Log(message);
            statusSink?.Invoke(message);
        }
    }
}
