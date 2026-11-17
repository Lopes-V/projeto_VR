using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace WEG.W22
{
    /// <summary>Peça que o usuário pega, solta e encaixa (RF02, RF04).</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class W22Part : MonoBehaviour
    {
        public string partId;
        public bool isDistractor;

        [System.NonSerialized] public W22SnapTarget current;
        [System.NonSerialized] public W22PartState state;

        [Header("Parafuso")]
        [Tooltip("Quanto o parafuso fica para fora (m) antes de apertar.")]
        public float looseOffset = 0.012f;
        [Tooltip("Voltas visuais até apertar.")]
        public float turns = 3f;
        [System.NonSerialized] public bool tight;
        [System.NonSerialized] public float tightenProgress;

        public Rigidbody Body { get; private set; }
        public XRGrabInteractable Grab { get; private set; }
        public W22Outline Outline { get; private set; }
        public Vector3 LocalCenter { get; private set; }
        public Vector3 WorldCenter => transform.TransformPoint(LocalCenter);

        W22GameManager manager;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Grab = GetComponent<XRGrabInteractable>();
            Outline = GetComponent<W22Outline>();
            var box = GetComponent<BoxCollider>();
            LocalCenter = box != null ? box.center : Vector3.zero;
        }

        void OnEnable()
        {
            if (Grab == null) return;
            Grab.selectEntered.AddListener(OnGrabbed);
            Grab.selectExited.AddListener(OnReleased);
        }

        void OnDisable()
        {
            if (Grab == null) return;
            Grab.selectEntered.RemoveListener(OnGrabbed);
            Grab.selectExited.RemoveListener(OnReleased);
        }

        public void Bind(W22GameManager m) => manager = m;

        void OnGrabbed(SelectEnterEventArgs args)
        {
            if (manager != null) manager.OnPartGrabbed(this);
        }

        void OnReleased(SelectExitEventArgs args)
        {
            if (args.isCanceled) return;
            if (manager != null) manager.OnPartReleased(this, args.interactorObject);
        }

        public void SetState(W22PartState s)
        {
            state = s;
            if (Outline != null) Outline.Show(s);
        }

        /// <summary>Prende a peça no ponto de encaixe.</summary>
        public void AttachTo(W22SnapTarget t)
        {
            current = t;
            t.occupant = this;
            Body.isKinematic = true;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            transform.SetParent(t.transform, false);
            tight = false;
            tightenProgress = 0f;
            if (t.requiresTightening) ApplyScrewPose();
            else
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>Parafuso: sobe/desce ao longo do próprio eixo (Y do encaixe) e gira conforme o aperto.</summary>
        public void ApplyScrewPose()
        {
            float k = Mathf.Clamp01(tightenProgress);
            transform.localPosition = Vector3.up * looseOffset * (1f - k);
            transform.localRotation = Quaternion.Euler(0f, -k * turns * 360f, 0f);
        }

        /// <summary>Solta a peça no mundo, com gravidade.</summary>
        public void Detach(bool physics)
        {
            if (current != null && current.occupant == this) current.occupant = null;
            current = null;
            tight = false;
            tightenProgress = 0f;
            transform.SetParent(null, true);
            Body.isKinematic = !physics;
        }
    }
}
