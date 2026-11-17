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
        /// <summary>Colisores da própria peça (sem os de peças presas a ela, como rolamentos no rotor).</summary>
        public Collider[] Colliders { get; private set; }

        W22GameManager manager;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Grab = GetComponent<XRGrabInteractable>();
            Outline = GetComponent<W22Outline>();
            Colliders = GetComponents<Collider>();
            var mc = GetComponent<MeshCollider>();
            var box = GetComponent<BoxCollider>();
            if (mc != null && mc.sharedMesh != null) LocalCenter = mc.sharedMesh.bounds.center;
            else if (box != null) LocalCenter = box.center;
            else LocalCenter = Vector3.zero;
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
            if (Grab != null) Grab.throwOnDetach = true; // solta com a velocidade da mão
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
            // Encaixada: sem arremesso (o XRI tentaria lançar um corpo cinemático).
            if (Grab != null) Grab.throwOnDetach = false;
            if (!Body.isKinematic)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }
            Body.isKinematic = true;
            // Encaixada (e possivelmente filha de outra peça): sem interpolação para não atrasar nem "voltar".
            Body.interpolation = RigidbodyInterpolation.None;
            transform.SetParent(t.transform, false);
            tight = false;
            tightenProgress = 0f;
            if (t.requiresTightening) ApplyScrewPose();
            else
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
            SyncBody();
        }

        /// <summary>Copia a pose do Transform para o Rigidbody (evita a peça "voltar" para a pose antiga).</summary>
        public void SyncBody()
        {
            Body.position = transform.position;
            Body.rotation = transform.rotation;
        }

        /// <summary>Parafuso: sobe/desce ao longo do próprio eixo (Y do encaixe) e gira conforme o aperto.</summary>
        public void ApplyScrewPose()
        {
            float k = Mathf.Clamp01(tightenProgress);
            transform.localPosition = Vector3.up * looseOffset * (1f - k);
            transform.localRotation = Quaternion.Euler(0f, -k * turns * 360f, 0f);
            if (Body != null) SyncBody();
        }

        /// <summary>Solta a peça no mundo, com gravidade.</summary>
        public void Detach(bool physics)
        {
            if (current != null && current.occupant == this) current.occupant = null;
            current = null;
            tight = false;
            tightenProgress = 0f;
            transform.SetParent(null, true);
            SyncBody();
            Body.isKinematic = !physics;
            Body.interpolation = physics ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            if (physics) Body.WakeUp();
        }
    }
}
