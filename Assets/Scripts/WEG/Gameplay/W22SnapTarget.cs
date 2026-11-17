using UnityEngine;

namespace WEG.W22
{
    /// <summary>Como a orientação da peça é conferida no encaixe.</summary>
    public enum W22OrientationRule
    {
        Any,        // qualquer orientação (o encaixe alinha sozinho)
        Axis,       // o eixo indicado precisa estar alinhado, no mesmo sentido (giro em torno dele é livre)
        AxisFlip,   // o eixo precisa estar alinhado, em qualquer sentido (peça simétrica)
        Full,       // rotação completa
        FullFlipY   // rotação completa, aceitando meia-volta em torno de Y (peça simétrica)
    }

    /// <summary>
    /// Ponto de encaixe. A posição e rotação deste transform são onde o pivô da peça fica encaixada.
    /// Na bancada e no painel (organização) só vale o ID; no motor (montagem) valem também etapa,
    /// ordem, orientação e, para parafusos, o aperto com a ferramenta.
    /// </summary>
    public class W22SnapTarget : MonoBehaviour
    {
        public string partId;
        [Tooltip("true: ponto do motor (montagem). false: silhueta da bancada ou do painel (organização).")]
        public bool assembly;
        public int step;
        [Tooltip("Ordem dentro da etapa (1 = primeiro). Itens com a mesma ordem podem ir em qualquer sequência.")]
        public int order = 1;
        public float radius = 0.10f;

        [Header("Posição correta")]
        public W22OrientationRule orientation = W22OrientationRule.Any;
        public Vector3 axis = Vector3.forward;

        [Header("Parafuso")]
        [Tooltip("O item só conta como montado depois de apertado com a ferramenta.")]
        public bool requiresTightening;

        [System.NonSerialized] public W22Part occupant;

        public Vector3 ExpectedCenter(W22Part part) => transform.position + transform.rotation * part.LocalCenter;

        public bool IsCorrectlyOccupied =>
            occupant != null && occupant.partId == partId && (!requiresTightening || occupant.tight);

        public bool OrientationOk(Quaternion partRotation, float toleranceDeg)
        {
            var r = transform.rotation;
            switch (orientation)
            {
                case W22OrientationRule.Axis:
                    return Vector3.Angle(partRotation * axis, r * axis) <= toleranceDeg;
                case W22OrientationRule.AxisFlip:
                {
                    float a = Vector3.Angle(partRotation * axis, r * axis);
                    return a <= toleranceDeg || a >= 180f - toleranceDeg;
                }
                case W22OrientationRule.Full:
                    return Quaternion.Angle(partRotation, r) <= toleranceDeg;
                case W22OrientationRule.FullFlipY:
                    return Mathf.Min(Quaternion.Angle(partRotation, r), Quaternion.Angle(partRotation, r * Quaternion.Euler(0, 180f, 0))) <= toleranceDeg;
                default:
                    return true;
            }
        }
    }
}
