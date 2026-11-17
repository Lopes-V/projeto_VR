namespace WEG.W22
{
    /// <summary>
    /// Todas as medidas do motor WEG W22 132M 4 polos usadas pelo simulador, em metros.
    /// Origem de cada valor (ver Assets/Reference/WEG/dimensoes.md e montagem-pecas.md):
    ///   [WEG]   catálogo oficial lido
    ///   [DERIV] conta feita sobre valores [WEG] ou [SP]
    ///   [SP]    linha W22 Severe Process 132M: provisório até ler o catálogo do W22 padrão
    ///   [ISO]   dimensão normalizada de rolamento (ISO 15), não vem do catálogo WEG
    ///   [PROJ]  simplificação didática do simulador, não é dado do motor real
    /// Sistema de coordenadas: Z = eixo do rotor (+Z para a ponta de eixo), Y para cima,
    /// origem no ombro do eixo (face externa da tampa dianteira).
    /// Para trocar um valor provisório, altere SÓ aqui e gere os modelos de novo (menu WEG).
    /// </summary>
    public static class W22Dimensions
    {
        // ---------- Montagem IEC 132M [WEG] ----------
        public const float H = 0.132f;      // altura do eixo
        public const float A = 0.216f;      // entre furos dos pés, largura
        public const float B = 0.178f;      // entre furos dos pés, comprimento
        public const float C = 0.089f;      // ombro do eixo ao furo dianteiro
        public const float K = 0.012f;      // diâmetro do furo do pé (leitura provável)
        public const float BB = 0.225f;     // comprimento total do pé
        public const float D = 0.038f;      // diâmetro da ponta de eixo
        public const float E = 0.080f;      // comprimento da ponta de eixo
        public const float KeyF = 0.010f;   // largura da chaveta
        public const float KeyGD = 0.008f;  // altura da chaveta
        public const float KeyES = 0.063f;  // comprimento útil da chaveta
        public const float G = 0.033f;      // fundo do rasgo à face oposta do eixo

        // ---------- Cotas da linha Severe Process 132M [SP] (provisório) ----------
        public const float AA = 0.051f;     // largura da aba do pé
        public const float AB = 0.248f;     // largura sobre os pés
        public const float HA = 0.020f;     // espessura do pé
        public const float HC = 0.266f;     // base ao topo da carcaça
        public const float HD = 0.354f;     // base ao topo da caixa de ligação
        public const float L = 0.490f;      // comprimento total
        public const float LL = 0.140f;     // caixa de ligação (comprimento, mapeamento provável)
        public const float LM = 0.133f;     // caixa de ligação (largura, mapeamento provável)
        public const float CableEntryD = 0.032f; // entrada M32x1,5

        // ---------- Rolamentos [WEG tipo] + [ISO medidas] ----------
        public const float Brg6308Bore = 0.040f, Brg6308OD = 0.090f, Brg6308W = 0.023f; // dianteiro (lado acionado)
        public const float Brg6207Bore = 0.035f, Brg6207OD = 0.072f, Brg6207W = 0.017f; // traseiro
        public const float Brg6205Bore = 0.025f, Brg6205OD = 0.052f, Brg6205W = 0.015f; // distratora

        // ---------- Derivados [DERIV] ----------
        public const float FootHoleFrontZ = -C;
        public const float FootHoleRearZ = -(C + B);
        public const float FootCenterZ = -(C + B * 0.5f);
        public const float KeyBottomY = G - D * 0.5f;         // fundo do rasgo: 0,014
        public const float KeyCenterZ = E * 0.5f;             // [PROJ] chaveta centralizada na ponta
        public const float BodyFinTipR = HC - H;              // 0,134 [DERIV de SP]
        public const float CoverRearZ = -(L - E);             // -0,410 [DERIV de SP]
        public const float TBTopY = HD - H;                   // 0,222 [DERIV de SP]

        // ---------- Carcaça e estator [PROJ] ----------
        public const float BodyFrontZ = -0.040f;
        public const float BodyRearZ = -0.320f;
        public const float BodyCoreR = 0.116f;
        public const float BodyInnerR = 0.110f;
        public const float FinPitchDeg = 10f;
        public const float FinRootThickness = 0.006f;
        public const float FinTipThickness = 0.003f;
        public const float FinSkipTopDeg = 40f;      // sem aletas sob a caixa de ligação
        public const float TBSeatBottomY = 0.070f;   // assento fundido da caixa sobre a carcaça (preenche a folga sob a flange)
        public const float ShaftCenterHoleR = 0.0051f; // furo roscado M12 (d1) na ponta do eixo [WEG: M12; PROJ: raio de broca]
        public const float FinSkipBottomDeg = 30f;   // sem aletas entre os pés
        public const float FootPillarHalfLen = 0.075f;
        public const float StatorBoreR = 0.081f;
        public const float StatorFrontZ = -0.075f;
        public const float StatorRearZ = -0.285f;
        public const float WindingInnerR = 0.085f;
        public const float WindingOuterR = 0.106f;
        public const float WindingLength = 0.030f;

        // ---------- Rotor e eixo [PROJ], exceto ponta de eixo [WEG] ----------
        public const float RotorR = 0.0795f;
        public const float ShaftShoulderR = 0.022f;
        public const float ShaftBodyR = 0.024f;
        public const float ShaftNdeR = 0.021f;
        public const float ShaftFanR = 0.015f;
        public const float ShaftRearEndZ = -0.375f;
        public const float DeBearingZ = -0.0235f;    // centro do 6308 (z -0,012 a -0,035)
        public const float NdeBearingZ = -0.3305f;   // centro do 6207 (z -0,322 a -0,339)

        // ---------- Tampas [PROJ] ----------
        public const float ShieldRimR = 0.124f;
        public const float ShieldRimT = 0.013f;
        public const float ShieldBoltR = 0.116f;
        public const float FrontShieldT = 0.040f;
        public const float RearShieldT = 0.025f;
        public const float FrontHubR = 0.056f;
        public const float RearHubR = 0.048f;
        public const int ShieldRibs = 8;

        // ---------- Ventilador e tampa defletora [PROJ] ----------
        public const float FanZ = -0.361f;
        public const int FanBlades = 10;
        public const float FanTipR = 0.106f;
        public const float CoverFrontZ = -0.326f;
        public const float CoverOuterR = 0.128f;
        public const float CoverWall = 0.003f;

        // ---------- Caixa de ligação [PROJ] ----------
        public const float TBZ = -0.140f;
        public const float TBBottomY = 0.104f;
        public const float TBSplitY = 0.190f;

        // ---------- Parafuso único reutilizado, M8 [PROJ] ----------
        public const float ScrewHeadAF = 0.013f;
        public const float ScrewHeadH = 0.0055f;
        public const float ScrewShankR = 0.004f;
        public const float ScrewShankL = 0.020f;
    }
}
