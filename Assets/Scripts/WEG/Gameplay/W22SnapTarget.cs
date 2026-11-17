using UnityEngine;

namespace WEG.W22
{
    /// <summary>
    /// Ponto de encaixe. A posição e rotação deste transform são onde o pivô da peça fica encaixada.
    /// Na bancada (organização) só vale o ID; no motor (montagem) valem também etapa e ordem.
    /// </summary>
    public class W22SnapTarget : MonoBehaviour
    {
        public string partId;
        [Tooltip("true: ponto do motor (montagem). false: silhueta da bancada (organização).")]
        public bool assembly;
        public int step;
        [Tooltip("Ordem dentro da etapa (1 = primeiro). Itens com a mesma ordem podem ir em qualquer sequência.")]
        public int order = 1;
        public float radius = 0.10f;

        [System.NonSerialized] public W22Part occupant;

        public Vector3 ExpectedCenter(W22Part part) => transform.position + transform.rotation * part.LocalCenter;

        public bool IsCorrectlyOccupied => occupant != null && occupant.partId == partId;
    }
}
