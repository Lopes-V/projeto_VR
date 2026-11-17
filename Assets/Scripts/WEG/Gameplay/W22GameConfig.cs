using System;
using UnityEngine;

namespace WEG.W22
{
    [Serializable]
    public class W22StepInfo
    {
        public int step;
        public string title;
        [TextArea(2, 4)] public string action;
        [TextArea(2, 6)] public string parts;
        [TextArea(3, 8)] public string explanation;
    }

    /// <summary>
    /// Parâmetros configuráveis do simulador (RNF de manutenibilidade): pontuação, tempo,
    /// tolerâncias de encaixe e textos didáticos de cada etapa.
    /// </summary>
    [CreateAssetMenu(menuName = "WEG/Configuração do simulador W22", fileName = "W22GameConfig")]
    public class W22GameConfig : ScriptableObject
    {
        [Header("Pontuação (seção 7 do documento)")]
        public int initialScore = 1000;
        public int orderErrorPenalty = 20;
        public int locationErrorPenalty = 40;
        [Tooltip("Tempo de referência em segundos (10 min).")]
        public float referenceTime = 600f;
        [Tooltip("Desconta 1 ponto a cada N segundos acima do tempo de referência.")]
        public float timePenaltyInterval = 10f;
        public bool pauseTimerOnPanel = true;

        [Header("Classificação")]
        public int excellentMin = 900;
        public int goodMin = 700;
        public int regularMin = 400;

        [Header("Encaixe (snap)")]
        public float snapRadius = 0.10f;
        public float smallPartSnapRadius = 0.045f;
        [Tooltip("Tolerância de orientação (graus) para a peça encaixar no motor.")]
        public float orientationTolerance = 25f;

        [Header("Aperto dos parafusos")]
        [Tooltip("Segundos segurando o gatilho com a ferramenta no parafuso.")]
        public float tightenTime = 1.2f;

        [Tooltip("Versão dos textos/valores gerados (usada pelo gerador para atualizar).")]
        public int dataVersion = 1;

        [Header("Etapas de montagem")]
        public W22StepInfo[] steps;

        public W22StepInfo GetStep(int step)
        {
            if (steps == null) return null;
            foreach (var s in steps) if (s.step == step) return s;
            return null;
        }

        public string Classify(int score)
        {
            if (score >= excellentMin) return "Excelente";
            if (score >= goodMin) return "Bom";
            if (score >= regularMin) return "Regular";
            return "Precisa praticar";
        }
    }
}
