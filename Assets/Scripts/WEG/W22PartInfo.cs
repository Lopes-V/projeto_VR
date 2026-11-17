using UnityEngine;

namespace WEG.W22
{
    /// <summary>
    /// Identifica uma peça do simulador (motor W22 ou distratora).
    /// O pivô do objeto é o ponto de encaixe da peça; assembledPosition é onde
    /// esse pivô fica no motor montado (coordenadas do motor, em metros).
    /// </summary>
    public class W22PartInfo : MonoBehaviour
    {
        public string partId;
        public string displayName;
        [Tooltip("Etapa de montagem (1 a 7). 0 para distratoras.")]
        public int step;
        public bool isDistractor;
        public Vector3 assembledPosition;
    }
}
