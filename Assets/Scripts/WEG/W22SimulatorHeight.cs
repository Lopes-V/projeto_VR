using Unity.XR.CoreUtils;
using UnityEngine;

namespace WEG.W22
{
    /// <summary>
    /// Só para testes no Editor com o XR Interaction Simulator: o simulador começa a cabeça em y = 0,
    /// então o rig passa para o modo Device com a altura dos olhos de um adulto em pé.
    /// Fica no objeto do simulador (tag EditorOnly), por isso não existe no build do Quest.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class W22SimulatorHeight : MonoBehaviour
    {
        [Tooltip("Altura dos olhos, em metros, usada no simulador.")]
        public float eyeHeight = 1.65f;

        XROrigin origin;

        void Start()
        {
            origin = FindAnyObjectByType<XROrigin>();
            if (origin == null) return;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
            origin.CameraYOffset = eyeHeight;
            Apply();
        }

        // Sem headset não há subsistema de rastreamento, e o XROrigin não aplica o deslocamento sozinho.
        void LateUpdate() => Apply();

        void Apply()
        {
            if (origin == null || origin.CameraFloorOffsetObject == null) return;
            var t = origin.CameraFloorOffsetObject.transform;
            if (Mathf.Abs(t.localPosition.y - eyeHeight) > 0.001f)
                t.localPosition = new Vector3(t.localPosition.x, eyeHeight, t.localPosition.z);
        }
    }
}
