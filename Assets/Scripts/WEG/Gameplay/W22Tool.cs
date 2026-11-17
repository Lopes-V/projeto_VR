using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace WEG.W22
{
    /// <summary>
    /// Ferramenta na mão: com o gatilho apertado e a ponta encostada num parafuso encaixado,
    /// o parafuso gira e afunda até ficar apertado (só a chave 13 mm e o torquímetro servem).
    /// </summary>
    [RequireComponent(typeof(W22Part))]
    public class W22Tool : MonoBehaviour
    {
        public bool canTighten;
        public Transform tip;
        [Tooltip("Distância máxima (m) entre a ponta da ferramenta e a cabeça do parafuso.")]
        public float reach = 0.045f;

        XRGrabInteractable grab;
        W22GameManager manager;
        bool active;
        [System.NonSerialized] public bool warned;

        public Vector3 TipWorld => tip != null ? tip.position : transform.position;

        public XRBaseInputInteractor Holder =>
            grab != null && grab.isSelected ? grab.firstInteractorSelecting as XRBaseInputInteractor : null;

        void Awake() => grab = GetComponent<XRGrabInteractable>();

        void OnEnable()
        {
            if (grab == null) return;
            grab.activated.AddListener(OnActivated);
            grab.deactivated.AddListener(OnDeactivated);
            grab.selectExited.AddListener(OnReleased);
        }

        void OnDisable()
        {
            if (grab == null) return;
            grab.activated.RemoveListener(OnActivated);
            grab.deactivated.RemoveListener(OnDeactivated);
            grab.selectExited.RemoveListener(OnReleased);
        }

        public void Bind(W22GameManager m) => manager = m;

        void OnActivated(ActivateEventArgs args) { active = true; warned = false; }
        void OnDeactivated(DeactivateEventArgs args) { active = false; }
        void OnReleased(SelectExitEventArgs args) { active = false; }

        void Update()
        {
            if (active && manager != null && grab.isSelected) manager.TickTool(this, Time.deltaTime);
        }
    }
}
