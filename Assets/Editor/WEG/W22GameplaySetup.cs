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

            // 1. Peças e ferramentas pegáveis (caixa, bandeja e ferramentas sobre o armário).
            var roots = new List<Transform>();
            var piecesRoot = GameObject.Find("Caixa_Pecas/Pecas");
            if (piecesRoot != null) roots.Add(piecesRoot.transform);
            var toolsRoot = GameObject.Find("Ambientacao/Ferramentas_Soltas");
            if (toolsRoot != null) roots.Add(toolsRoot.transform);
            W22Part rotor = null;
            foreach (var groupRoot in roots)
            {
                foreach (Transform child in groupRoot)
                {
                    var info = child.GetComponent<W22PartInfo>();
                    if (info == null) continue;
                    // Colisor convexo com o formato da peça (rolam e assentam direito), no lugar da caixa.
                    ConvexCollider(child.gameObject, PhysMatFor(info.partId));
                    var rb = GetOrAdd<Rigidbody>(child.gameObject);
                    rb.isKinematic = false;
                    rb.useGravity = true;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    float mass = MassOf(info.partId);
                    rb.mass = mass;
                    rb.linearDamping = 0.05f;
                    rb.angularDamping = 0.35f;
                    // Peças pequenas e leves: detecção contínua para não atravessar a bancada.
                    rb.collisionDetectionMode = mass <= 0.1f ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.ContinuousSpeculative;
                    var grab = GetOrAdd<XRGrabInteractable>(child.gameObject);
                    // Mão física: a peça segurada colide com bancada e caixa e pode ser arremessada.
                    grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
                    grab.throwOnDetach = true;
                    grab.useDynamicAttach = true;
                    grab.retainTransformParent = true;
                    grab.attachEaseInTime = 0.08f;
                    var ol = GetOrAdd<W22Outline>(child.gameObject);
                    ol.outlineMaterial = outline;
                    var part = GetOrAdd<W22Part>(child.gameObject);
                    part.partId = info.partId;
                    part.isDistractor = info.isDistractor;
                    if (info.partId == "P01") rotor = part;
                    foreach (var def in W22ToolFactory.Tools)
                    {
                        if (def.id != info.partId) continue;
                        grab.useDynamicAttach = false; // segura pelo cabo, ponta para a frente
                        var tool = GetOrAdd<W22Tool>(child.gameObject);
                        tool.canTighten = def.canTighten;
                        tool.tip = child.Find("Ponta");
                    }
                }
            }

            // 2. Silhuetas da bancada e do painel de ferramentas (organização).
            var silRoots = new List<Transform>();
            var sil = GameObject.Find("Bancada/Silhuetas");
            if (sil != null) silRoots.Add(sil.transform);
            var toolSil = GameObject.Find("Ambientacao/Painel_Ferramentas/Silhuetas_Ferramentas");
            if (toolSil != null) silRoots.Add(toolSil.transform);
            foreach (var silRoot in silRoots)
                foreach (Transform s in silRoot)
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
            if (anchor != null)
            {
                // Carcaça fixa com colisor côncavo (o furo do estator existe para a física).
                foreach (Transform c in anchor.transform)
                {
                    var lod = c.Find("LOD1");
                    if (lod == null) continue;
                    foreach (var box in c.GetComponents<BoxCollider>()) Object.DestroyImmediate(box);
                    var mc = GetOrAdd<MeshCollider>(c.gameObject);
                    mc.sharedMesh = lod.GetComponent<MeshFilter>().sharedMesh;
                    mc.convex = false;
                    mc.sharedMaterial = PhysMat("WEG_Fisica_Metal", 0.35f, 0.45f, 0.05f);
                }
            }
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

        // ---------------------------------------------------------------- física
        static float MassOf(string id)
        {
            switch (id)
            {
                case "P01": return 8f;    case "P02": return 0.9f; case "P03": return 0.6f;
                case "P05": return 2.5f;  case "P06": return 3f;   case "P07": return 0.3f;
                case "P08": return 1.5f;  case "P09": return 1.2f; case "P10": return 0.6f;
                case "P11": return 0.05f; case "P12": return 0.03f;
                case "D01": return 1.2f;  case "D02": return 1.5f; case "D03": return 0.6f;
                case "D04": return 0.2f;  case "D05": return 0.25f; case "D06": return 0.05f;
                case "D07": return 0.01f; case "D08": return 2f;
                case "T01": return 0.2f;  case "T02": return 0.15f; case "T03": return 0.6f;
                case "T04": return 0.3f;  case "T05": return 0.9f;
                default: return 1f;
            }
        }

        static PhysicsMaterial PhysMatFor(string id)
        {
            if (id == "P07") return PhysMat("WEG_Fisica_Plastico", 0.45f, 0.55f, 0.12f);
            if (id == "D04") return PhysMat("WEG_Fisica_Borracha", 0.9f, 1.0f, 0.2f);
            if (id == "T02" || id == "T04") return PhysMat("WEG_Fisica_Ferramenta", 0.5f, 0.6f, 0.08f);
            return PhysMat("WEG_Fisica_Metal", 0.35f, 0.45f, 0.05f);
        }

        static readonly Dictionary<string, PhysicsMaterial> physMats = new Dictionary<string, PhysicsMaterial>();

        static PhysicsMaterial PhysMat(string name, float dyn, float stat, float bounce)
        {
            PhysicsMaterial m;
            if (physMats.TryGetValue(name, out m) && m != null) return m;
            EnsureFolder("Assets/Data/WEG/Fisica");
            string path = $"Assets/Data/WEG/Fisica/{name}.asset";
            m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (m == null) { m = new PhysicsMaterial(name); AssetDatabase.CreateAsset(m, path); }
            m.dynamicFriction = dyn;
            m.staticFriction = stat;
            m.bounciness = bounce;
            m.frictionCombine = PhysicsMaterialCombine.Average;
            m.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(m);
            physMats[name] = m;
            return m;
        }

        /// <summary>Troca a caixa de colisão pelo casco convexo da malha (LOD1, ou LOD0 nas ferramentas).</summary>
        static void ConvexCollider(GameObject go, PhysicsMaterial mat)
        {
            var src = go.transform.Find("LOD1") ?? go.transform.Find("LOD0");
            var mf = src != null ? src.GetComponent<MeshFilter>() : null;
            if (mf == null || mf.sharedMesh == null) return;
            foreach (var box in go.GetComponents<BoxCollider>()) Object.DestroyImmediate(box);
            var mc = GetOrAdd<MeshCollider>(go);
            mc.sharedMesh = mf.sharedMesh;
            mc.convex = true;
            mc.sharedMaterial = mat;
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
            // Posição correta: orientação conforme a simetria de cada peça.
            switch (id)
            {
                case "P01": case "P05": case "P06": case "P07": case "P08":
                    t.orientation = W22OrientationRule.Axis; t.axis = Vector3.forward; break;     // eixo do motor, sentido importa
                case "P02": case "P03": case "P11":
                    t.orientation = W22OrientationRule.AxisFlip; t.axis = Vector3.forward; break; // simétricas de frente e verso
                case "P09":
                    t.orientation = W22OrientationRule.Full; break;                               // entradas de cabo de um lado
                case "P10":
                    t.orientation = W22OrientationRule.FullFlipY; break;                          // tampa simétrica
                case "P12":
                    t.orientation = W22OrientationRule.Axis; t.axis = Vector3.up;                 // cabeça para fora
                    t.requiresTightening = true; break;
            }
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
            const int CurrentDataVersion = 2;
            var cfg = AssetDatabase.LoadAssetAtPath<W22GameConfig>(ConfigPath);
            if (cfg != null && cfg.dataVersion >= CurrentDataVersion) return cfg; // preserva ajustes feitos pela equipe
            bool create = cfg == null;
            if (create)
            {
                EnsureFolder("Assets/Data/WEG");
                cfg = ScriptableObject.CreateInstance<W22GameConfig>();
            }
            cfg.dataVersion = CurrentDataVersion;
            cfg.orientationTolerance = 25f;
            cfg.tightenTime = 1.2f;
            const string Tighten = " Aperte cada parafuso com a chave combinada 13 mm ou o torquímetro (ponta no parafuso + gatilho).";
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
            // Etapas com parafusos: o aperto com ferramenta faz parte da ação.
            foreach (var s in cfg.steps)
                if (s.step >= 3 && s.step <= 6)
                {
                    s.action += Tighten;
                    s.parts += "\n• Ferramenta: chave combinada 13 mm ou torquímetro";
                }
            if (create) AssetDatabase.CreateAsset(cfg, ConfigPath);
            else EditorUtility.SetDirty(cfg);
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
