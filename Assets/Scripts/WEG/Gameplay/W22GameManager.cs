using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace WEG.W22
{
    /// <summary>
    /// Máquina de estados do simulador (seção 10): Início, Organização, Montagem, Painel, Resultado.
    /// Valida peça, local e ordem a cada encaixe (RF09), aplica bordas (seção 6), pontuação e tempo (seção 7).
    /// </summary>
    public class W22GameManager : MonoBehaviour
    {
        public enum Phase { Start, Organization, Assembly, Result }

        public W22GameConfig config;
        public W22UI ui;
        [Tooltip("Desliga andar e girar pelo joystick (RNF de conforto: sem locomoção artificial).")]
        public bool disableJoystickLocomotion = true;
        [Tooltip("Embaralha a posição das peças na caixa a cada partida (RF01).")]
        public bool shuffleCrate = true;

        public Phase CurrentPhase { get; private set; } = Phase.Start;
        public int CurrentStep { get; private set; }

        readonly List<W22Part> parts = new List<W22Part>();
        readonly List<W22SnapTarget> orgTargets = new List<W22SnapTarget>();
        readonly List<W22SnapTarget> asmTargets = new List<W22SnapTarget>();

        float elapsed;
        bool timerRunning;
        int orderErrors, placeErrors;
        AudioSource audioSource;
        AudioClip okClip, errClip, stepClip;

        static readonly string[] LocomotionTypes =
        {
            "ContinuousMoveProvider", "DynamicMoveProvider", "ContinuousTurnProvider", "SnapTurnProvider",
            "TeleportationProvider", "GrabMoveProvider", "TwoHandedGrabMoveProvider", "ClimbProvider"
        };

        void Start()
        {
            parts.AddRange(FindObjectsByType<W22Part>(FindObjectsInactive.Exclude));
            foreach (var t in FindObjectsByType<W22SnapTarget>(FindObjectsInactive.Include))
                (t.assembly ? asmTargets : orgTargets).Add(t);
            // Física no ritmo do Quest (72 Hz): peças seguradas e arremessos mais suaves.
            Time.fixedDeltaTime = 1f / 72f;
            var baseAnchor = GameObject.Find("Suporte_MotorBase/Ponto_MotorBase");
            if (baseAnchor != null) assemblyColliders.AddRange(baseAnchor.GetComponentsInChildren<Collider>());
            foreach (var p in parts)
            {
                p.Bind(this);
                p.Body.isKinematic = false; // peças com física desde o início (reagem ao serem tocadas)
                p.SetState(W22PartState.None);
                if (p.Grab != null) p.Grab.enabled = false;
                var tool = p.GetComponent<W22Tool>();
                if (tool != null) tool.Bind(this);
            }
            if (shuffleCrate) DropIntoCrate();
            if (disableJoystickLocomotion) DisableLocomotion();
            BuildAudio();

            var cam = Camera.main;
            ui.Build(cam != null ? cam.transform : null);
            ui.ShowPanel("Montagem do Motor WEG W22", null,
                "<b>1. Organize a oficina:</b> pendure cada ferramenta (sobre o armário vermelho) na sua silhueta do painel e coloque cada peça do motor (caixa amarela e bandeja de parafusos) na sua silhueta da bancada. Cuidado: há peças que não pertencem ao motor.\n\n" +
                "<b>2. Monte o motor:</b> encaixe cada peça na posição e na orientação corretas e aperte os parafusos com a <b>chave combinada 13 mm</b> ou o <b>torquímetro</b>: encoste a ponta no parafuso e segure o gatilho.\n" +
                "<color=#3BE65A>verde</color> correto · <color=#FFD21A>amarela</color> fora de ordem · <color=#F2261A>vermelha</color> local ou posição errada · <color=#33BFFF>azul</color> falta apertar\n\n" +
                "Grip: pegar e soltar. Gatilho: usar a ferramenta. O cronômetro começa no primeiro toque.",
                "Iniciar", BeginOrganization);
            RefreshHud();
        }

        void Update()
        {
            bool paused = config.pauseTimerOnPanel && ui.PanelOpen;
            if (timerRunning && !paused) elapsed += Time.deltaTime;
            RefreshHud();
        }

        // ================================================================ fases
        void BeginOrganization()
        {
            CurrentPhase = Phase.Organization;
            foreach (var p in parts) if (p.Grab != null) p.Grab.enabled = true;
            RefreshHud();
        }

        void BeginAssembly()
        {
            CurrentPhase = Phase.Assembly;
            CurrentStep = 1;
            foreach (var p in parts) if (p.state == W22PartState.Correct) p.SetState(W22PartState.None);
            var info = config.GetStep(1);
            ui.ShowPanel("Organização concluída!", null,
                "Ferramentas no painel e peças na bancada.\n\nAgora monte o motor no suporte à sua frente, etapa por etapa. Pegue a chave 13 mm ou o torquímetro no painel quando for apertar os parafusos.\n\n" +
                $"<b>Etapa 1 · {info?.title}</b>\n{info?.action}",
                "Começar montagem", null);
            Play(stepClip);
            RefreshHud();
        }

        void CompleteStep()
        {
            var info = config.GetStep(CurrentStep);
            Play(stepClip);
            var tabs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Resumo da etapa", info != null ? $"<b>O que foi feito</b>\n{info.action}" : ""),
                new KeyValuePair<string, string>("Peças envolvidas", info != null ? info.parts : ""),
                new KeyValuePair<string, string>("Explicação", info != null ? info.explanation : ""),
            };
            bool last = CurrentStep >= LastStep();
            ui.ShowPanel($"Etapa {CurrentStep} concluída · {info?.title}", tabs, null, last ? "Ver resultado" : "Próxima etapa", () =>
            {
                foreach (var t in asmTargets)
                    if (t.step == CurrentStep && t.occupant != null) t.occupant.SetState(W22PartState.None);
                if (last) Finish();
                else
                {
                    CurrentStep++;
                    Reevaluate();
                }
            });
        }

        void Finish()
        {
            CurrentPhase = Phase.Result;
            timerRunning = false;
            foreach (var p in parts) if (p.Grab != null) p.Grab.enabled = false;
            int score = Score();
            int m = Mathf.FloorToInt(elapsed / 60f), s = Mathf.FloorToInt(elapsed % 60f);
            ui.ShowPanel("Motor montado!", null,
                $"<size=120%><b>{config.Classify(score)}</b></size>\n\n" +
                $"Tempo total: <b>{m:00}:{s:00}</b>\n" +
                $"Erros de ordem (amarelo): <b>{orderErrors}</b>\n" +
                $"Erros de local (vermelho): <b>{placeErrors}</b>\n" +
                $"Pontuação: <b>{score}</b> de {config.initialScore}",
                "Reiniciar", Restart);
            Play(stepClip);
        }

        void Restart()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }

        int LastStep()
        {
            int max = 0;
            foreach (var t in asmTargets) max = Mathf.Max(max, t.step);
            return max;
        }

        // ================================================================ eventos das peças
        public void OnPartGrabbed(W22Part p)
        {
            if (!timerRunning && CurrentPhase != Phase.Result) timerRunning = true; // começa no primeiro contato
            if (p.current != null && p.current.assembly) RemoveFromAssembly(p);
            SetAssemblyIgnore(p, true);
            p.Body.interpolation = RigidbodyInterpolation.Interpolate; // movimento suave na mão
            if (p.current != null) p.current.occupant = null;
            p.current = null;
            p.tight = false;
            p.tightenProgress = 0f;
            p.SetState(W22PartState.None);
            Reevaluate();
        }

        public void OnPartReleased(W22Part p, UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor)
        {
            var list = CurrentPhase == Phase.Assembly ? asmTargets : CurrentPhase == Phase.Organization ? orgTargets : null;
            var target = list != null ? FindTarget(p, list) : null;
            if (target == null && CurrentPhase == Phase.Assembly)
            {
                // Na montagem, a peça pode voltar à própria silhueta da bancada (sem cor e sem penalidade).
                var park = FindTarget(p, orgTargets);
                if (park != null && park.partId == p.partId)
                {
                    SetAssemblyIgnore(p, false);
                    p.AttachTo(park);
                    p.SetState(W22PartState.None);
                    Reevaluate();
                    return;
                }
            }
            if (target == null)
            {
                SetAssemblyIgnore(p, false);
                p.Detach(true);
                p.SetState(W22PartState.None);
                Reevaluate();
                return;
            }

            // Posição correta inclui a orientação: peça virada errado não encaixa (borda vermelha).
            if (target.assembly && target.partId == p.partId && !target.OrientationOk(p.transform.rotation, config.orientationTolerance))
            {
                SetAssemblyIgnore(p, false);
                p.Detach(true);
                placeErrors++;
                Feedback(W22PartState.WrongPlace, interactor);
                StartCoroutine(FlashWrong(p));
                Reevaluate();
                return;
            }

            p.AttachTo(target);
            if (target.assembly) AddToAssembly(p);
            else SetAssemblyIgnore(p, false);
            var state = Evaluate(target);
            // Penalidade uma vez por tentativa de encaixe (seção 7).
            if (state == W22PartState.WrongPlace) placeErrors++;
            else if (state == W22PartState.WrongOrder) orderErrors++;
            Feedback(state, interactor);
            Reevaluate();

            if (CurrentPhase == Phase.Organization && OrganizationDone()) BeginAssembly();
            else if (CurrentPhase == Phase.Assembly && StepDone(CurrentStep)) CompleteStep();
        }

        W22SnapTarget FindTarget(W22Part p, List<W22SnapTarget> list)
        {
            W22SnapTarget bestMatch = null, bestAny = null;
            float dMatch = float.MaxValue, dAny = float.MaxValue;
            var center = p.WorldCenter;
            foreach (var t in list)
            {
                if (!t.isActiveAndEnabled) continue;
                if (t.occupant != null && t.occupant != p) continue;
                if (t.transform.IsChildOf(p.transform)) continue; // ex.: alvos dos rolamentos no próprio rotor
                float d = Vector3.Distance(center, t.ExpectedCenter(p));
                if (d > t.radius) continue;
                if (t.partId == p.partId && d < dMatch) { dMatch = d; bestMatch = t; }
                if (d < dAny) { dAny = d; bestAny = t; }
            }
            return bestMatch ?? bestAny;
        }

        // ================================================================ validação
        W22PartState Evaluate(W22SnapTarget t)
        {
            var p = t.occupant;
            if (p == null) return W22PartState.None;
            if (p.partId != t.partId) return W22PartState.WrongPlace;
            if (!t.assembly) return W22PartState.Correct;
            if (t.step > CurrentStep) return W22PartState.WrongOrder;
            if (t.step < CurrentStep) return W22PartState.Correct;
            foreach (var o in asmTargets)
                if (o.step == t.step && o.order < t.order && !o.IsCorrectlyOccupied) return W22PartState.WrongOrder;
            return W22PartState.Correct;
        }

        void Reevaluate()
        {
            var list = CurrentPhase == Phase.Assembly ? asmTargets : orgTargets;
            foreach (var t in list)
            {
                if (t.occupant == null) continue;
                var s = Evaluate(t);
                // Verde de etapas anteriores já foi apagado; mantém só o que é da etapa atual ou erro.
                if (s == W22PartState.Correct && t.assembly && t.step < CurrentStep) s = W22PartState.None;
                // Parafuso no lugar certo, na ordem certa, mas ainda solto.
                if (s == W22PartState.Correct && t.requiresTightening && !t.occupant.tight) s = W22PartState.Loose;
                t.occupant.SetState(s);
            }
        }

        System.Collections.IEnumerator FlashWrong(W22Part p)
        {
            p.SetState(W22PartState.WrongPlace);
            yield return new WaitForSeconds(1.5f);
            if (p.current == null && p.state == W22PartState.WrongPlace) p.SetState(W22PartState.None);
        }

        // ================================================================ aperto com ferramenta
        float hapticTimer;

        /// <summary>Chamado a cada quadro enquanto a ferramenta está na mão com o gatilho apertado.</summary>
        public void TickTool(W22Tool tool, float dt)
        {
            if (CurrentPhase != Phase.Assembly || ui.PanelOpen) return;
            W22Part screw = null;
            float best = tool.reach;
            var tip = tool.TipWorld;
            foreach (var p in parts)
            {
                if (p.current == null || !p.current.assembly || !p.current.requiresTightening || p.tight) continue;
                float d = Vector3.Distance(tip, p.transform.position + p.transform.up * 0.003f);
                if (d < best) { best = d; screw = p; }
            }
            if (screw == null) return;

            var holder = tool.Holder;
            if (!tool.canTighten)
            {
                if (!tool.warned) { tool.warned = true; Play(errClip); if (holder != null) holder.SendHapticImpulse(0.6f, 0.15f); }
                return;
            }
            if (screw.partId != screw.current.partId || Evaluate(screw.current) != W22PartState.Correct)
            {
                // Parafuso em lugar errado ou antes da peça que ele fixa.
                if (!tool.warned) { tool.warned = true; Play(errClip); if (holder != null) holder.SendHapticImpulse(0.6f, 0.15f); }
                return;
            }

            screw.tightenProgress += dt / Mathf.Max(0.1f, config.tightenTime);
            screw.ApplyScrewPose();
            hapticTimer -= dt;
            if (hapticTimer <= 0f && holder != null) { holder.SendHapticImpulse(0.25f, 0.04f); hapticTimer = 0.1f; }
            if (screw.tightenProgress >= 1f)
            {
                screw.tight = true;
                screw.tightenProgress = 1f;
                screw.ApplyScrewPose();
                Play(okClip);
                if (holder != null) holder.SendHapticImpulse(0.7f, 0.12f);
                Reevaluate();
                if (StepDone(CurrentStep)) CompleteStep();
            }
        }

        bool OrganizationDone()
        {
            foreach (var t in orgTargets) if (!t.IsCorrectlyOccupied) return false;
            return orgTargets.Count > 0;
        }

        bool StepDone(int step)
        {
            bool any = false;
            foreach (var t in asmTargets)
            {
                if (t.step != step) continue;
                any = true;
                if (!t.IsCorrectlyOccupied || Evaluate(t) != W22PartState.Correct) return false;
            }
            // Não conclui com peça errada encaixada em ponto desta etapa.
            return any;
        }

        // ================================================================ pontuação e HUD
        int Score()
        {
            int s = config.initialScore - orderErrors * config.orderErrorPenalty - placeErrors * config.locationErrorPenalty;
            if (elapsed > config.referenceTime) s -= Mathf.FloorToInt((elapsed - config.referenceTime) / config.timePenaltyInterval);
            return Mathf.Max(0, s);
        }

        void RefreshHud()
        {
            string step;
            switch (CurrentPhase)
            {
                case Phase.Start: step = "Leia as instruções"; break;
                case Phase.Organization:
                    int ok = 0;
                    foreach (var t in orgTargets) if (t.IsCorrectlyOccupied) ok++;
                    step = $"Organização · {ok}/{orgTargets.Count} itens no lugar"; break;
                case Phase.Assembly:
                    var info = config.GetStep(CurrentStep);
                    step = $"Etapa {CurrentStep}/{LastStep()} · {info?.title}"; break;
                default: step = "Concluído"; break;
            }
            ui.UpdateHud(elapsed, Score(), step);
        }

        // ================================================================ feedback (seção 6: som e vibração)
        void Feedback(W22PartState s, UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor)
        {
            var input = interactor as XRBaseInputInteractor;
            switch (s)
            {
                case W22PartState.Correct:
                    Play(okClip);
                    if (input != null) input.SendHapticImpulse(0.3f, 0.08f);
                    break;
                case W22PartState.WrongOrder:
                    Play(errClip);
                    if (input != null) input.SendHapticImpulse(0.5f, 0.18f);
                    break;
                case W22PartState.WrongPlace:
                    Play(errClip);
                    if (input != null) input.SendHapticImpulse(0.9f, 0.3f);
                    break;
            }
        }

        void BuildAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.6f;
            okClip = Tone("ok", new[] { 880f, 1320f }, 0.09f);
            errClip = Tone("erro", new[] { 220f, 180f }, 0.14f);
            stepClip = Tone("etapa", new[] { 660f, 880f, 1100f }, 0.11f);
        }

        void Play(AudioClip c)
        {
            if (audioSource != null && c != null) audioSource.PlayOneShot(c);
        }

        static AudioClip Tone(string name, float[] freqs, float noteLen)
        {
            int rate = 44100, perNote = Mathf.RoundToInt(rate * noteLen);
            var data = new float[perNote * freqs.Length];
            for (int n = 0; n < freqs.Length; n++)
                for (int i = 0; i < perNote; i++)
                {
                    float env = Mathf.Min(1f, i / (rate * 0.005f)) * (1f - (float)i / perNote);
                    data[n * perNote + i] = Mathf.Sin(2f * Mathf.PI * freqs[n] * i / rate) * env * 0.5f;
                }
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ================================================================ utilidades
        void DisableLocomotion()
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                string n = mb.GetType().Name;
                foreach (var l in LocomotionTypes)
                    if (n == l) { mb.enabled = false; break; }
            }
        }

        /// <summary>
        /// RF01: a cada partida as peças caem na caixa em ordem e rotação aleatórias e se acomodam
        /// pela física (enquanto o aluno lê as instruções), formando uma pilha natural.
        /// </summary>
        void DropIntoCrate()
        {
            var crate = new List<W22Part>();
            foreach (var p in parts) if (p.partId != "P12" && p.transform.parent != null && p.transform.parent.name == "Pecas") crate.Add(p);
            var area = GameObject.Find("Caixa_Pecas/Area_Spawn");
            if (area == null || crate.Count == 0) return;
            var box = area.GetComponent<BoxCollider>();
            var center = area.transform.TransformPoint(box.center);
            var size = box.size;
            for (int i = crate.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = crate[i]; crate[i] = crate[j]; crate[j] = tmp;
            }
            const int cols = 3, rows = 2;
            for (int i = 0; i < crate.Count; i++)
            {
                int layer = i / (cols * rows), cell = i % (cols * rows);
                float x = center.x + ((cell % cols) - (cols - 1) * 0.5f) * (size.x / cols);
                float z = center.z + ((cell / cols) - (rows - 1) * 0.5f) * (size.z / rows);
                float y = center.y + size.y * 0.5f + 0.12f + layer * 0.3f;
                var p = crate[i];
                p.transform.SetPositionAndRotation(new Vector3(x, y, z), Random.rotation);
                p.SyncBody();
                p.Body.linearVelocity = Vector3.zero;
                p.Body.angularVelocity = Vector3.zero;
            }
        }

        // ================================================================ colisões durante a montagem
        // O motor e as peças já montadas ignoram a peça na mão: o rotor entra na carcaça e o rolamento
        // no eixo sem "brigar" com a física; a peça solta volta a colidir com o motor.
        readonly List<Collider> assemblyColliders = new List<Collider>();

        void SetAssemblyIgnore(W22Part p, bool ignore)
        {
            foreach (var pc in p.Colliders)
                foreach (var ac in assemblyColliders)
                {
                    if (ac == null || pc == null || ac == pc) continue;
                    // Nunca volta a colidir com peças presas a ela (rolamentos e chaveta no rotor).
                    if (!ignore && (ac.transform.IsChildOf(p.transform) || p.transform.IsChildOf(ac.transform))) continue;
                    Physics.IgnoreCollision(pc, ac, ignore);
                }
        }

        void AddToAssembly(W22Part p)
        {
            foreach (var c in p.Colliders)
                if (!assemblyColliders.Contains(c)) assemblyColliders.Add(c);
            // Peças seguradas na outra mão também passam a ignorá-la.
            foreach (var other in parts)
                if (other != p && other.Grab != null && other.Grab.isSelected)
                    foreach (var oc in other.Colliders)
                        foreach (var c in p.Colliders) Physics.IgnoreCollision(oc, c, true);
        }

        void RemoveFromAssembly(W22Part p)
        {
            foreach (var c in p.Colliders) assemblyColliders.Remove(c);
        }
    }
}
