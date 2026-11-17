using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using D = WEG.W22.W22Dimensions;

namespace WEG.W22.EditorTools
{
    /// <summary>
    /// Liga a jogabilidade na cena da oficina (chamado pelo gerador da oficina antes de salvar):
    /// peças pegáveis, alvos de encaixe (bancada, motor e rotor), configuração com textos das etapas,
    /// material da borda, gerenciador e interface.
    /// </summary>
    public static class W22GameplaySetup
    {
        const string ConfigPath = "Assets/Data/WEG/W22GameConfig.asset";
        const string OutlineMatPath = "Assets/Materials/WEG/WEG_Outline.mat";
        const string AssembledPath = "Assets/Prefabs/WEG/WEG_W22_132M_Montado.prefab";

        struct AsmRule
        {
            public int step, order;
            public AsmRule(int s, int o) { step = s; order = o; }
        }

        // Sequência didática da seção 8 do documento de requisitos.
        static readonly Dictionary<string, AsmRule> Rules = new Dictionary<string, AsmRule>
        {
            { "P02", new AsmRule(1, 1) }, { "P03", new AsmRule(1, 1) },
            { "P01", new AsmRule(2, 1) },
            { "P05", new AsmRule(3, 1) }, { "TampaTraseira", new AsmRule(3, 2) },
            { "P06", new AsmRule(4, 1) }, { "TampaDianteira", new AsmRule(4, 2) },
            { "P07", new AsmRule(5, 1) }, { "P08", new AsmRule(5, 2) }, { "TampaDefletora", new AsmRule(5, 3) },
            { "P09", new AsmRule(6, 1) }, { "CaixaBase", new AsmRule(6, 2) }, { "P10", new AsmRule(6, 3) }, { "CaixaTampa", new AsmRule(6, 4) },
            { "P11", new AsmRule(7, 1) },
        };

        public static void Apply()
        {
            var config = EnsureConfig();
            var outline = EnsureOutlineMaterial();

            // 1. Peças pegáveis (caixa e bandeja).
            var piecesRoot = GameObject.Find("Caixa_Pecas/Pecas");
            W22Part rotor = null;
            if (piecesRoot != null)
            {
                foreach (Transform child in piecesRoot.transform)
                {
                    var info = child.GetComponent<W22PartInfo>();
                    if (info == null) continue;
                    var rb = GetOrAdd<Rigidbody>(child.gameObject);
                    rb.isKinematic = true;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    rb.mass = info.partId == "P12" ? 0.05f : 1f;
                    var grab = GetOrAdd<XRGrabInteractable>(child.gameObject);
                    grab.movementType = XRBaseInteractable.MovementType.Kinematic;
                    grab.throwOnDetach = false;
                    grab.useDynamicAttach = true;
                    grab.retainTransformParent = true;
                    var ol = GetOrAdd<W22Outline>(child.gameObject);
                    ol.outlineMaterial = outline;
                    var part = GetOrAdd<W22Part>(child.gameObject);
                    part.partId = info.partId;
                    part.isDistractor = info.isDistractor;
                    if (info.partId == "P01") rotor = part;
                }
            }

            // 2. Silhuetas da bancada (organização).
            var sil = GameObject.Find("Bancada/Silhuetas");
            if (sil != null)
                foreach (Transform s in sil.transform)
                {
                    var info = s.GetComponent<W22PartInfo>();
                    var fit = s.Find("Encaixe");
                    if (info == null || fit == null) continue;
                    var t = GetOrAdd<W22SnapTarget>(fit.gameObject);
                    t.partId = info.partId;
                    t.assembly = false;
                    t.radius = info.partId == "P12" || info.partId == "P11" ? config.smallPartSnapRadius : config.snapRadius;
                }

            // 3. Pontos de montagem no motor-base (a partir dos marcadores do prefab montado).
            var anchor = GameObject.Find("Suporte_MotorBase/Ponto_MotorBase");
            var assembled = AssetDatabase.LoadAssetAtPath<GameObject>(AssembledPath);
            if (anchor != null && assembled != null)
            {
                var targetsRoot = new GameObject("Alvos_Montagem").transform;
                targetsRoot.SetParent(anchor.transform, false);
                var markers = assembled.transform.Find("Encaixes");
                foreach (Transform m in markers)
                {
                    // Encaixe_P05 ou Encaixe_P12_TampaTraseira_03
                    string[] tok = m.name.Split('_');
                    if (tok.Length < 2) continue;
                    string id = tok[1];
                    string key = id == "P12" && tok.Length >= 3 ? tok[2] : id;
                    if (id == "P02" || id == "P03" || id == "P11" || id == "P04") continue; // no rotor ou base
                    AsmRule rule;
                    if (!Rules.TryGetValue(key, out rule)) continue;
                    var go = new GameObject($"Alvo_{m.name.Substring("Encaixe_".Length)}");
                    go.transform.SetParent(targetsRoot, false);
                    go.transform.localPosition = m.localPosition;
                    go.transform.localRotation = m.localRotation;
                    AddTarget(go, id, rule, id == "P12" ? config.smallPartSnapRadius : config.snapRadius);
                }
            }

            // 4. Alvos presos ao rotor: rolamentos (etapa 1) e chaveta (etapa 7) acompanham o rotor.
            if (rotor != null)
            {
                AddRotorTarget(rotor.transform, "P02", new Vector3(0, 0, D.DeBearingZ), config.snapRadius);
                AddRotorTarget(rotor.transform, "P03", new Vector3(0, 0, D.NdeBearingZ), config.snapRadius);
                AddRotorTarget(rotor.transform, "P11", new Vector3(0, D.KeyBottomY, D.KeyCenterZ), config.smallPartSnapRadius);
            }

            // 5. Gerenciador e interface.
            var mgrGo = new GameObject("Simulador_W22");
            var ui = mgrGo.AddComponent<W22UI>();
            var mgr = mgrGo.AddComponent<W22GameManager>();
            mgr.config = config;
            mgr.ui = ui;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        static void AddRotorTarget(Transform rotor, string id, Vector3 local, float radius)
        {
            var go = new GameObject($"Alvo_{id}");
            go.transform.SetParent(rotor, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.identity;
            AddTarget(go, id, Rules[id], radius);
        }

        static void AddTarget(GameObject go, string id, AsmRule rule, float radius)
        {
            var t = go.AddComponent<W22SnapTarget>();
            t.partId = id;
            t.assembly = true;
            t.step = rule.step;
            t.order = rule.order;
            t.radius = radius;
        }

        static Material EnsureOutlineMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMatPath);
            var sh = Shader.Find("WEG/Outline");
            if (mat == null)
            {
                mat = new Material(sh);
                AssetDatabase.CreateAsset(mat, OutlineMatPath);
            }
            mat.shader = sh;
            mat.SetFloat("_Width", 0.004f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static W22GameConfig EnsureConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<W22GameConfig>(ConfigPath);
            if (cfg != null) return cfg; // preserva ajustes feitos pela equipe
            EnsureFolder("Assets/Data/WEG");
            cfg = ScriptableObject.CreateInstance<W22GameConfig>();
            cfg.steps = new[]
            {
                Step(1, "Preparação do rotor", "Encaixar os dois rolamentos nas posições do eixo.",
                    "• Rotor com eixo\n• Rolamento dianteiro 6308-ZZ (lado da ponta de eixo)\n• Rolamento traseiro 6207-ZZ",
                    "O rotor é a parte girante do motor e transmite o movimento ao exterior por meio do eixo. Os rolamentos apoiam o eixo, reduzem o atrito e permitem que ele gire com estabilidade. Na montagem real, são instalados com cuidado, sem impactos, para não danificar as pistas."),
                Step(2, "Instalação do rotor na carcaça", "Inserir o conjunto do rotor no interior do estator.",
                    "• Carcaça com estator bobinado (motor-base)\n• Conjunto do rotor com rolamentos",
                    "O estator, com seu enrolamento, produz o campo magnético que faz o rotor girar. O rotor deve ser inserido centralizado e sem tocar no enrolamento, para evitar danos."),
                Step(3, "Tampa traseira", "Posicionar a tampa traseira e fixá-la com os parafusos.",
                    "• Tampa traseira\n• 4 parafusos",
                    "As tampas fecham a carcaça, alojam os rolamentos e mantêm o rotor centralizado. A tampa traseira fica do lado oposto à ponta de eixo."),
                Step(4, "Tampa dianteira", "Posicionar a tampa dianteira (lado da ponta de eixo) e fixá-la.",
                    "• Tampa dianteira\n• 4 parafusos",
                    "A tampa dianteira completa o fechamento do conjunto e sustenta o eixo do lado que aciona a carga, mantendo o alinhamento entre rotor e estator."),
                Step(5, "Ventilador e tampa defletora", "Encaixar o ventilador no eixo, no lado traseiro, e depois fixar a tampa defletora.",
                    "• Ventilador\n• Tampa defletora\n• 3 parafusos",
                    "O ventilador gira com o eixo e empurra ar sobre a carcaça para resfriar o motor. A tampa defletora protege o ventilador e direciona o fluxo de ar pelas aletas da carcaça."),
                Step(6, "Caixa de ligação", "Fixar a base da caixa de ligação na carcaça e, em seguida, a tampa.",
                    "• Base da caixa de ligação + 4 parafusos\n• Tampa da caixa de ligação + 4 parafusos",
                    "A caixa de ligação abriga os terminais de conexão elétrica do motor e protege essas conexões. É por ela que o motor é ligado à rede elétrica."),
                Step(7, "Chaveta", "Inserir a chaveta no rasgo da ponta de eixo.",
                    "• Chaveta 10 x 8 x 63 mm",
                    "A chaveta transmite o torque do eixo a polias ou acoplamentos, evitando que a peça acoplada deslize sobre o eixo."),
            };
            AssetDatabase.CreateAsset(cfg, ConfigPath);
            return cfg;
        }

        static W22StepInfo Step(int n, string title, string action, string parts, string explanation)
        {
            return new W22StepInfo { step = n, title = title, action = action, parts = parts, explanation = explanation };
        }

        /// <summary>Coloca a cena da oficina como primeira cena do build (necessário para reiniciar).</summary>
        public static void EnsureInBuildSettings(string scenePath)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == scenePath);
            list.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{cur}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
