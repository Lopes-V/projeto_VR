using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace WEG.W22
{
    /// <summary>
    /// Interface do simulador construída em tempo de execução: HUD com cronômetro que segue a cabeça
    /// (RF13, RNF de conforto) e painel flutuante com abas (RF10 a RF12), início e resultado (RF16).
    /// </summary>
    public class W22UI : MonoBehaviour
    {
        [Tooltip("Posição do painel flutuante (acima do motor-base).")]
        public Vector3 panelPosition = new Vector3(0f, 1.48f, 1.12f);
        public float panelTilt = 8f;

        static readonly Color PanelBg = new Color(0.07f, 0.09f, 0.12f, 0.96f);
        static readonly Color Accent = new Color(0.18f, 0.35f, 0.47f, 1f); // azul RAL 5009 clareado
        static readonly Color TabIdle = new Color(0.16f, 0.19f, 0.23f, 1f);
        static readonly Color TextMain = new Color(0.93f, 0.95f, 0.97f, 1f);
        static readonly Color TextSoft = new Color(0.70f, 0.75f, 0.80f, 1f);

        Transform head;
        RectTransform hudRoot;
        TextMeshProUGUI hudTime, hudScore, hudStep;

        GameObject panel;
        TextMeshProUGUI panelTitle, panelBody;
        readonly List<Button> tabButtons = new List<Button>();
        readonly List<string> tabContents = new List<string>();
        Button primaryButton;
        TextMeshProUGUI primaryLabel;
        Action onPrimary;

        public bool PanelOpen => panel != null && panel.activeSelf;

        public void Build(Transform headTransform)
        {
            head = headTransform;
            EnsureEventSystem();
            BuildHud();
            BuildPanel();
            HidePanel();
        }

        // ---------------------------------------------------------------- HUD
        void BuildHud()
        {
            var canvas = NewCanvas("HUD_Cronometro", new Vector2(640, 96), 0.001f);
            hudRoot = canvas.GetComponent<RectTransform>();
            var bg = NewImage("Fundo", hudRoot, new Color(0.05f, 0.06f, 0.08f, 0.8f));
            Stretch(bg.rectTransform, 0, 0, 0, 0);
            hudTime = NewText("Tempo", hudRoot, "00:00", 38, TextMain, TextAlignmentOptions.MidlineLeft);
            hudTime.lineSpacing = -25f;
            Place(hudTime.rectTransform, 0f, 0.24f, 20, 0);
            hudStep = NewText("Etapa", hudRoot, "", 24, TextSoft, TextAlignmentOptions.Center);
            hudStep.enableAutoSizing = true;
            hudStep.fontSizeMin = 16;
            hudStep.fontSizeMax = 24;
            Place(hudStep.rectTransform, 0.24f, 0.78f, 6, 6);
            hudScore = NewText("Pontos", hudRoot, "1000", 38, TextMain, TextAlignmentOptions.MidlineRight);
            hudScore.lineSpacing = -25f;
            Place(hudScore.rectTransform, 0.78f, 1f, 0, 20);
            // Sem colisão com raios: o HUD não é interativo.
            var cg = canvas.gameObject.AddComponent<CanvasGroup>();
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        public void UpdateHud(float seconds, int score, string step)
        {
            int m = Mathf.FloorToInt(seconds / 60f), s = Mathf.FloorToInt(seconds % 60f);
            hudTime.text = $"<size=50%><color=#A9B4C0>TEMPO</color></size>\n{m:00}:{s:00}";
            hudScore.text = $"<size=50%><color=#A9B4C0>PONTOS</color></size>\n{score}";
            hudStep.text = step;
        }

        void LateUpdate()
        {
            if (head == null || hudRoot == null) return;
            // Segue a cabeça com atraso leve (RNF: evita HUD rigidamente preso à tela).
            var fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (fwd.sqrMagnitude < 1e-4f) fwd = head.up;
            fwd.Normalize();
            var target = head.position + fwd * 1.1f + Vector3.up * 0.34f;
            float k = 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime);
            hudRoot.position = Vector3.Lerp(hudRoot.position, target, k);
            var look = Quaternion.LookRotation(hudRoot.position - head.position, Vector3.up);
            hudRoot.rotation = Quaternion.Slerp(hudRoot.rotation, look, k);
        }

        // ---------------------------------------------------------------- painel
        void BuildPanel()
        {
            var canvas = NewCanvas("Painel_Didatico", new Vector2(980, 600), 0.001f);
            panel = canvas.gameObject;
            var rt = canvas.GetComponent<RectTransform>();
            rt.position = panelPosition;
            rt.rotation = Quaternion.Euler(panelTilt, 0, 0);
            var bg = NewImage("Fundo", rt, PanelBg);
            Stretch(bg.rectTransform, 0, 0, 0, 0);
            var bar = NewImage("Faixa", rt, Accent);
            bar.rectTransform.anchorMin = new Vector2(0, 1);
            bar.rectTransform.anchorMax = new Vector2(1, 1);
            bar.rectTransform.offsetMin = new Vector2(0, -10);
            bar.rectTransform.offsetMax = Vector2.zero;

            panelTitle = NewText("Titulo", rt, "", 40, TextMain, TextAlignmentOptions.MidlineLeft);
            panelTitle.fontStyle = FontStyles.Bold;
            panelTitle.enableAutoSizing = true;
            panelTitle.fontSizeMin = 26;
            panelTitle.fontSizeMax = 40;
            panelTitle.overflowMode = TextOverflowModes.Ellipsis;
            Anchor(panelTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -88), new Vector2(-40, -24));

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var b = NewButton($"Aba_{i}", rt, "", 24, TabIdle);
                Anchor((RectTransform)b.transform, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(40 + i * 300, -150), new Vector2(40 + i * 300 + 290, -100));
                b.onClick.AddListener(() => SelectTab(idx));
                tabButtons.Add(b);
            }

            panelBody = NewText("Conteudo", rt, "", 32, TextMain, TextAlignmentOptions.TopLeft);
            panelBody.enableAutoSizing = true;
            panelBody.fontSizeMin = 22;
            panelBody.fontSizeMax = 32;
            Anchor(panelBody.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 110), new Vector2(-40, -170));

            primaryButton = NewButton("Botao_Principal", rt, "Continuar", 32, Accent);
            Anchor((RectTransform)primaryButton.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-340, 28), new Vector2(-40, 92));
            primaryLabel = primaryButton.GetComponentInChildren<TextMeshProUGUI>();
            primaryButton.onClick.AddListener(() =>
            {
                var cb = onPrimary;
                HidePanel();
                cb?.Invoke();
            });
        }

        /// <summary>Mostra o painel. tabs = pares (nome da aba, conteúdo); null ou vazio = sem abas.</summary>
        public void ShowPanel(string title, IList<KeyValuePair<string, string>> tabs, string body, string buttonLabel, Action onButton)
        {
            panelTitle.text = title;
            tabContents.Clear();
            for (int i = 0; i < tabButtons.Count; i++)
            {
                bool has = tabs != null && i < tabs.Count;
                tabButtons[i].gameObject.SetActive(has);
                if (has)
                {
                    tabButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = tabs[i].Key;
                    tabContents.Add(tabs[i].Value);
                }
            }
            var bodyRt = panelBody.rectTransform;
            bodyRt.offsetMax = new Vector2(-40, tabContents.Count > 0 ? -170 : -110);
            if (tabContents.Count > 0) SelectTab(0);
            else panelBody.text = body;
            primaryLabel.text = buttonLabel;
            onPrimary = onButton;
            panel.SetActive(true);
        }

        public void HidePanel()
        {
            if (panel != null) panel.SetActive(false);
        }

        void SelectTab(int idx)
        {
            if (idx < 0 || idx >= tabContents.Count) return;
            panelBody.text = tabContents[idx];
            for (int i = 0; i < tabButtons.Count; i++)
                tabButtons[i].GetComponent<Image>().color = i == idx ? Accent : TabIdle;
        }

        // ---------------------------------------------------------------- utilidades de UI
        static void EnsureEventSystem()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null) es = new GameObject("EventSystem").AddComponent<EventSystem>();
            // Os raios dos controles precisam do XRUIInputModule (ele também aceita mouse no simulador).
            if (es.GetComponent<XRUIInputModule>() == null)
            {
                foreach (var m in es.GetComponents<BaseInputModule>()) m.enabled = false;
                es.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        Canvas NewCanvas(string name, Vector2 size, float scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = head != null ? head.GetComponent<Camera>() : Camera.main;
            canvas.sortingOrder = 10;
            go.AddComponent<TrackedDeviceGraphicRaycaster>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.localScale = Vector3.one * scale;
            return canvas;
        }

        static Image NewImage(string name, Transform parent, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = c;
            return img;
        }

        static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color c, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }

        static Button NewButton(string name, Transform parent, string label, float size, Color c)
        {
            var img = NewImage(name, parent, c);
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            b.colors = colors;
            var t = NewText("Texto", img.transform, label, size, TextMain, TextAlignmentOptions.Center);
            Stretch(t.rectTransform, 8, 4, 8, 4);
            return b;
        }

        static void Stretch(RectTransform rt, float l, float b, float r, float t)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
        }

        static void Place(RectTransform rt, float x0, float x1, float padL, float padR)
        {
            rt.anchorMin = new Vector2(x0, 0);
            rt.anchorMax = new Vector2(x1, 1);
            rt.offsetMin = new Vector2(padL, 0);
            rt.offsetMax = new Vector2(-padR, 0);
        }

        static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
        }
    }
}
