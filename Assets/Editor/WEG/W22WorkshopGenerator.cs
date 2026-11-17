using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace WEG.W22.EditorTools
{
    /// <summary>
    /// Gera a cena da oficina mecânica do simulador (WEG_Oficina.unity): sala, iluminação,
    /// bancada com zonas e silhuetas brancas por etapa (RF03), motor-base à frente (RF08),
    /// caixa de peças à direita com peças e distratoras (RF01) e rig XR.
    /// Layout: usuário na origem olhando para +Z. Peças do motor devem existir (menu WEG > Gerar modelos).
    /// </summary>
    public static class W22WorkshopGenerator
    {
        const string MeshDir = "Assets/Models/WEG/Oficina";
        const string MatDir = "Assets/Materials/WEG/Oficina";
        const string TexDir = "Assets/Textures/WEG";
        const string PartsDir = "Assets/Prefabs/WEG/Parts";
        const string PartMeshDir = "Assets/Models/WEG/Meshes";
        const string ScenePath = "Assets/Scenes/WEG_Oficina.unity";
        const string XrRigPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";

        // Sala (metros)
        const float RoomX0 = -4.5f, RoomX1 = 4.5f, RoomZ0 = -5.8f, RoomZ1 = 2.2f, RoomH = 3.6f;
        // Bancada
        const float BenchTop = 0.90f, BenchX0 = -1.5f, BenchX1 = 1.5f, BenchZ0 = 0.45f, BenchZ1 = 1.20f;

        static int meshCounter;
        static readonly Dictionary<string, Material> M = new Dictionary<string, Material>();

        struct PartRef { public string id, file; public PartRef(string i, string f) { id = i; file = f; } }

        static readonly PartRef[] Parts =
        {
            new PartRef("P01", "P01_Rotor_Eixo"), new PartRef("P02", "P02_Rolamento_Dianteiro_6308ZZ"),
            new PartRef("P03", "P03_Rolamento_Traseiro_6207ZZ"), new PartRef("P04", "P04_Carcaca_Estator"),
            new PartRef("P05", "P05_Tampa_Traseira"), new PartRef("P06", "P06_Tampa_Dianteira"),
            new PartRef("P07", "P07_Ventilador"), new PartRef("P08", "P08_Tampa_Defletora"),
            new PartRef("P09", "P09_Caixa_Ligacao_Base"), new PartRef("P10", "P10_Caixa_Ligacao_Tampa"),
            new PartRef("P11", "P11_Chaveta"), new PartRef("P12", "P12_Parafuso"),
        };

        static readonly string[] Distractors =
        {
            "D01_Engrenagem", "D02_Polia", "D03_Acoplamento", "D04_Correia",
            "D05_Rolamento_6205", "D06_Porca", "D07_Arruela", "D08_Tampa_Errada"
        };

        static string FileOf(string id)
        {
            foreach (var p in Parts) if (p.id == id) return p.file;
            return null;
        }

        // ================================================================ menu
        [MenuItem("WEG/Gerar sala de mecânica (Oficina)")]
        public static void Generate()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{PartsDir}/P04_Carcaca_Estator.prefab") == null)
            {
                Debug.LogError("[WEG] Gere antes os modelos do motor (WEG > Gerar modelos W22 132M).");
                return;
            }
            EnsureFolder(MeshDir);
            EnsureFolder(MatDir);
            EnsureFolder(TexDir);
            meshCounter = 0;
            BuildMaterials();

            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                BuildRoom(new GameObject("Sala").transform);
                BuildLighting(new GameObject("Iluminacao").transform);
                BuildBench(new GameObject("Bancada").transform);
                BuildMotorStand(new GameObject("Suporte_MotorBase").transform);
                BuildPartsCrate(new GameObject("Caixa_Pecas").transform);
                BuildProps(new GameObject("Ambientacao").transform);
                BuildXrRig();

                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[WEG] Oficina gerada em {ScenePath} ({meshCounter} malhas).");
        }

        // ================================================================ materiais e texturas
        static void BuildMaterials()
        {
            M.Clear();
            var floorTex = MakeTexture("Oficina_Piso_Concreto", 512, FloorPixel);
            var pegTex = MakeTexture("Oficina_Painel_Furos", 64, PegboardPixel);

            M["piso"] = Lit("Oficina_Piso", new Color(0.62f, 0.64f, 0.66f), 0f, 0.62f, floorTex);
            M["parede"] = Lit("Oficina_Parede", new Color(0.80f, 0.81f, 0.82f), 0f, 0.15f);
            M["faixa"] = Lit("Oficina_Parede_Faixa", new Color(0.24f, 0.30f, 0.36f), 0f, 0.30f);
            M["teto"] = Lit("Oficina_Teto", new Color(0.30f, 0.31f, 0.33f), 0f, 0.10f);
            M["metal"] = Lit("Oficina_Metal_Escuro", new Color(0.20f, 0.21f, 0.23f), 0.75f, 0.45f);
            M["aco"] = Lit("Oficina_Aco", new Color(0.70f, 0.71f, 0.73f), 0.90f, 0.60f);
            M["manta"] = Lit("Oficina_Manta_Bancada", new Color(0.11f, 0.12f, 0.13f), 0f, 0.25f);
            M["zona"] = Lit("Oficina_Zona_Etapa", new Color(0.19f, 0.21f, 0.23f), 0f, 0.20f);
            M["madeira"] = Lit("Oficina_Madeira", new Color(0.58f, 0.42f, 0.26f), 0f, 0.35f);
            M["vermelho"] = Lit("Oficina_Vermelho", new Color(0.72f, 0.07f, 0.05f), 0.20f, 0.60f);
            M["amarelo"] = Lit("Oficina_Amarelo", new Color(0.96f, 0.74f, 0.05f), 0.10f, 0.55f);
            M["preto"] = Lit("Oficina_Preto", new Color(0.05f, 0.05f, 0.06f), 0f, 0.40f);
            M["papelao"] = Lit("Oficina_Papelao", new Color(0.60f, 0.45f, 0.28f), 0f, 0.15f);
            M["painel"] = Lit("Oficina_Painel_Ferramentas", Color.white, 0f, 0.20f, pegTex);
            M["luz"] = Emissive("Oficina_Luminaria", new Color(1f, 0.98f, 0.94f), 3.0f);
            M["janela"] = Emissive("Oficina_Janela", new Color(0.72f, 0.84f, 0.95f), 1.6f);

            // Silhueta branca da bancada (RF03), já criada pelo gerador do motor.
            var sil = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/WEG/WEG_Silhouette_White.mat");
            if (sil == null)
            {
                sil = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                sil.SetColor("_BaseColor", Color.white);
                AssetDatabase.CreateAsset(sil, "Assets/Materials/WEG/WEG_Silhouette_White.mat");
            }
            M["silhueta"] = sil;
        }

        static Color FloorPixel(int x, int y, int size)
        {
            float u = (float)x / size, v = (float)y / size;
            float n = Mathf.PerlinNoise(u * 6f, v * 6f) * 0.6f + Mathf.PerlinNoise(u * 23f + 7f, v * 23f + 3f) * 0.4f;
            float speck = Mathf.PerlinNoise(u * 140f, v * 140f) > 0.78f ? -0.05f : 0f;
            float seam = (x < 2 || y < 2) ? -0.10f : 0f; // junta de dilatação a cada 2 m
            float g = 0.88f + (n - 0.5f) * 0.14f + speck + seam;
            return new Color(g, g, g * 1.01f, 1f);
        }

        static Color PegboardPixel(int x, int y, int size)
        {
            float cx = size * 0.5f, cy = size * 0.5f;
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (d < size * 0.16f) return new Color(0.08f, 0.07f, 0.06f, 1f);
            if (d < size * 0.19f) return new Color(0.42f, 0.33f, 0.22f, 1f);
            return new Color(0.66f, 0.53f, 0.37f, 1f);
        }

        static Texture2D MakeTexture(string name, int size, System.Func<int, int, int, Color> f)
        {
            string path = $"{TexDir}/{name}.png";
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, f(x, y, size));
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.anisoLevel = 4;
            imp.mipmapEnabled = true;
            imp.maxTextureSize = size;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material GetOrCreateMat(string name, Shader sh)
        {
            string path = $"{MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(sh); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = sh;
            mat.enableInstancing = true;
            return mat;
        }

        static Material Lit(string name, Color c, float metallic, float smooth, Texture2D tex = null)
        {
            var mat = GetOrCreateMat(name, Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", c);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smooth);
            if (tex != null) { mat.SetTexture("_BaseMap", tex); mat.SetTexture("_MainTex", tex); }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material Emissive(string name, Color c, float intensity)
        {
            var mat = Lit(name, c, 0f, 0.5f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", c * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ================================================================ helpers de geometria
        static GameObject Obj(string name, Transform parent, WegMeshBuilder b, Material mat, bool isStatic = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            int[] used;
            var mesh = b.ToMesh(name, out used);
            mesh = SaveMesh(mesh, $"{MeshDir}/{meshCounter:000}_{name}.asset");
            meshCounter++;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (isStatic)
            {
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
                // Colisor estático ajustado à malha: o rig XR (com gravidade) e as peças apoiam no cenário.
                var bc = go.AddComponent<BoxCollider>();
                bc.size = Vector3.Max(bc.size, Vector3.one * 0.01f);
            }
            return go;
        }

        static GameObject Box(Transform parent, string name, string mat, Vector3 center, Vector3 size, float ch = 0.003f, Quaternion? rot = null, bool isStatic = true)
        {
            var b = new WegMeshBuilder();
            b.ChamferBox(0, Vector3.zero, size, ch);
            var go = Obj(name, parent, b, M[mat], isStatic);
            go.transform.localPosition = center;
            go.transform.localRotation = rot ?? Quaternion.identity;
            return go;
        }

        /// <summary>Cilindro com eixo em Y local.</summary>
        static GameObject Cyl(Transform parent, string name, string mat, Vector3 center, float r, float h, int seg = 16, Quaternion? rot = null, bool isStatic = true)
        {
            var c = new WegMeshBuilder();
            c.Cylinder(0, r, -h * 0.5f, h * 0.5f, seg, Mathf.Min(0.004f, r * 0.2f));
            var b = new WegMeshBuilder();
            b.Append(c, Matrix4x4.Rotate(Quaternion.Euler(-90f, 0, 0)));
            var go = Obj(name, parent, b, M[mat], isStatic);
            go.transform.localPosition = center;
            go.transform.localRotation = rot ?? Quaternion.identity;
            return go;
        }

        /// <summary>Plano retangular voltado para +Y local, com UV em metros / tile.</summary>
        static GameObject Plane(Transform parent, string name, string mat, Vector3 center, Vector2 size, Quaternion rot, float tile)
        {
            var b = new WegMeshBuilder();
            float hx = size.x * 0.5f, hz = size.y * 0.5f;
            var n = Vector3.up;
            int a = b.Vert(new Vector3(-hx, 0, -hz), n, new Vector2(-hx, -hz) / tile);
            int c1 = b.Vert(new Vector3(hx, 0, -hz), n, new Vector2(hx, -hz) / tile);
            int c2 = b.Vert(new Vector3(hx, 0, hz), n, new Vector2(hx, hz) / tile);
            int c3 = b.Vert(new Vector3(-hx, 0, hz), n, new Vector2(-hx, hz) / tile);
            b.Quad(0, a, c1, c2, c3);
            var go = Obj(name, parent, b, M[mat]);
            go.transform.localPosition = center;
            go.transform.localRotation = rot;
            return go;
        }

        static TextMeshPro Label(Transform parent, string name, string text, Vector3 pos, Quaternion rot, float fontSize, Color color, Vector2 box)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            var t = go.AddComponent<TextMeshPro>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.rectTransform.sizeDelta = box;
            return t;
        }

        static Mesh SaveMesh(Mesh m, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(m, path); return m; }
            existing.Clear();
            EditorUtility.CopySerialized(m, existing);
            Object.DestroyImmediate(m);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        static readonly Quaternion FaceDown = Quaternion.Euler(180f, 0, 0);
        static readonly Quaternion FaceMinusZ = Quaternion.Euler(-90f, 0, 0);
        static readonly Quaternion FacePlusZ = Quaternion.Euler(90f, 0, 0);
        static readonly Quaternion FacePlusX = Quaternion.Euler(0, 0, -90f);
        static readonly Quaternion FaceMinusX = Quaternion.Euler(0, 0, 90f);

        // ================================================================ sala
        static void BuildRoom(Transform t)
        {
            float w = RoomX1 - RoomX0, d = RoomZ1 - RoomZ0, cx = (RoomX0 + RoomX1) * 0.5f, cz = (RoomZ0 + RoomZ1) * 0.5f;
            float band = 1.2f;

            var floor = Plane(t, "Piso", "piso", new Vector3(cx, 0, cz), new Vector2(w, d), Quaternion.identity, 2f);
            var floorCol = floor.GetComponent<BoxCollider>();
            floorCol.center = new Vector3(0, -0.1f, 0);
            floorCol.size = new Vector3(w, 0.2f, d);
            Plane(t, "Teto", "teto", new Vector3(cx, RoomH, cz), new Vector2(w, d), FaceDown, 2f);

            // Paredes: parte superior clara + faixa inferior escura.
            Plane(t, "Parede_Frente", "parede", new Vector3(cx, (band + RoomH) * 0.5f, RoomZ1), new Vector2(w, RoomH - band), FaceMinusZ, 2f);
            Plane(t, "Parede_Frente_Faixa", "faixa", new Vector3(cx, band * 0.5f, RoomZ1), new Vector2(w, band), FaceMinusZ, 2f);
            Plane(t, "Parede_Fundo", "parede", new Vector3(cx, (band + RoomH) * 0.5f, RoomZ0), new Vector2(w, RoomH - band), FacePlusZ, 2f);
            Plane(t, "Parede_Fundo_Faixa", "faixa", new Vector3(cx, band * 0.5f, RoomZ0), new Vector2(w, band), FacePlusZ, 2f);
            Plane(t, "Parede_Esquerda", "parede", new Vector3(RoomX0, (band + RoomH) * 0.5f, cz), new Vector2(RoomH - band, d), FacePlusX, 2f);
            Plane(t, "Parede_Esquerda_Faixa", "faixa", new Vector3(RoomX0, band * 0.5f, cz), new Vector2(band, d), FacePlusX, 2f);
            Plane(t, "Parede_Direita", "parede", new Vector3(RoomX1, (band + RoomH) * 0.5f, cz), new Vector2(RoomH - band, d), FaceMinusX, 2f);
            Plane(t, "Parede_Direita_Faixa", "faixa", new Vector3(RoomX1, band * 0.5f, cz), new Vector2(band, d), FaceMinusX, 2f);

            // Rodapé metálico e frisos.
            Box(t, "Rodape_Frente", "metal", new Vector3(cx, 0.06f, RoomZ1 - 0.01f), new Vector3(w, 0.12f, 0.02f), 0.002f);
            Box(t, "Rodape_Fundo", "metal", new Vector3(cx, 0.06f, RoomZ0 + 0.01f), new Vector3(w, 0.12f, 0.02f), 0.002f);
            Box(t, "Rodape_Esquerda", "metal", new Vector3(RoomX0 + 0.01f, 0.06f, cz), new Vector3(0.02f, 0.12f, d), 0.002f);
            Box(t, "Rodape_Direita", "metal", new Vector3(RoomX1 - 0.01f, 0.06f, cz), new Vector3(0.02f, 0.12f, d), 0.002f);

            // Pilares de aço (perfil H simplificado) nas paredes laterais.
            for (int i = 0; i < 3; i++)
            {
                float z = RoomZ0 + 1.2f + i * 2.8f;
                Box(t, $"Pilar_E_{i}", "metal", new Vector3(RoomX0 + 0.12f, RoomH * 0.5f, z), new Vector3(0.24f, RoomH, 0.26f), 0.004f);
                Box(t, $"Pilar_D_{i}", "metal", new Vector3(RoomX1 - 0.12f, RoomH * 0.5f, z), new Vector3(0.24f, RoomH, 0.26f), 0.004f);
            }

            // Vigas do teto.
            for (int i = 0; i < 4; i++)
            {
                float z = RoomZ0 + 1.0f + i * 2.2f;
                Box(t, $"Viga_Teto_{i}", "metal", new Vector3(cx, RoomH - 0.15f, z), new Vector3(w, 0.30f, 0.18f), 0.004f);
            }

            // Janelas altas na parede direita (luz natural).
            for (int i = 0; i < 5; i++)
            {
                float z = RoomZ0 + 1.0f + i * 1.4f;
                var win = Plane(t, $"Janela_{i}", "janela", new Vector3(RoomX1 - 0.005f, 2.55f, z), new Vector2(0.9f, 1.1f), FaceMinusX, 1f);
                win.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                Box(t, $"Janela_{i}_Moldura_H", "metal", new Vector3(RoomX1 - 0.02f, 2.55f, z), new Vector3(0.03f, 0.04f, 1.1f), 0.002f);
                Box(t, $"Janela_{i}_Moldura_V", "metal", new Vector3(RoomX1 - 0.02f, 2.55f, z), new Vector3(0.03f, 0.9f, 0.04f), 0.002f);
                Box(t, $"Janela_{i}_Peitoril", "metal", new Vector3(RoomX1 - 0.04f, 1.98f, z), new Vector3(0.08f, 0.04f, 1.2f), 0.003f);
            }

            // Portão de enrolar no fundo.
            float doorW = 3.4f, doorH = 3.0f, doorX = 1.6f;
            for (int i = 0; i < 20; i++)
                Box(t, $"Portao_Lamina_{i:00}", "aco", new Vector3(doorX, 0.075f + i * 0.15f, RoomZ0 + 0.03f), new Vector3(doorW, 0.13f, 0.03f), 0.01f);
            Box(t, "Portao_Caixa", "metal", new Vector3(doorX, doorH + 0.2f, RoomZ0 + 0.12f), new Vector3(doorW + 0.3f, 0.4f, 0.24f), 0.01f);
            Box(t, "Portao_Guia_E", "amarelo", new Vector3(doorX - doorW * 0.5f - 0.08f, doorH * 0.5f, RoomZ0 + 0.06f), new Vector3(0.12f, doorH, 0.1f), 0.004f);
            Box(t, "Portao_Guia_D", "amarelo", new Vector3(doorX + doorW * 0.5f + 0.08f, doorH * 0.5f, RoomZ0 + 0.06f), new Vector3(0.12f, doorH, 0.1f), 0.004f);

            // Faixas de segurança no piso: corredor em volta da área de montagem.
            float y = 0.002f, lw = 0.08f;
            float fx0 = -2.0f, fx1 = 2.6f, fz0 = -1.0f, fz1 = 1.45f;
            Box(t, "Faixa_Piso_Frente", "amarelo", new Vector3((fx0 + fx1) * 0.5f, y, fz1), new Vector3(fx1 - fx0, 0.003f, lw), 0f);
            Box(t, "Faixa_Piso_Tras", "amarelo", new Vector3((fx0 + fx1) * 0.5f, y, fz0), new Vector3(fx1 - fx0, 0.003f, lw), 0f);
            Box(t, "Faixa_Piso_Esq", "amarelo", new Vector3(fx0, y, (fz0 + fz1) * 0.5f), new Vector3(lw, 0.003f, fz1 - fz0), 0f);
            Box(t, "Faixa_Piso_Dir", "amarelo", new Vector3(fx1, y, (fz0 + fz1) * 0.5f), new Vector3(lw, 0.003f, fz1 - fz0), 0f);
            // Zebrado na frente do portão.
            for (int i = 0; i < 12; i++)
                Box(t, $"Zebrado_{i:00}", i % 2 == 0 ? "amarelo" : "preto", new Vector3(doorX - doorW * 0.5f + 0.15f + i * 0.28f, y, RoomZ0 + 0.6f),
                    new Vector3(0.14f, 0.003f, 0.9f), 0f, Quaternion.Euler(0, 35f, 0));
        }

        // ================================================================ iluminação
        static void BuildLighting(Transform t)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.46f, 0.47f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.21f, 0.20f);
            RenderSettings.fog = false;

            // Luz principal: dia entrando pelas janelas (única luz com sombra, leve para o Quest).
            var sun = new GameObject("Luz_Janelas").AddComponent<Light>();
            sun.transform.SetParent(t, false);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.90f);
            sun.intensity = 0.9f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.7f;
            sun.transform.rotation = Quaternion.Euler(48f, -62f, 0f);
            RenderSettings.sun = sun;

            // Luz de tarefa sobre a bancada (sem sombra).
            var task = new GameObject("Luz_Bancada").AddComponent<Light>();
            task.transform.SetParent(t, false);
            task.type = LightType.Spot;
            task.color = new Color(1f, 0.97f, 0.92f);
            task.intensity = 2.2f;
            task.range = 4.5f;
            task.spotAngle = 120f;
            task.innerSpotAngle = 70f;
            task.shadows = LightShadows.None;
            task.transform.position = new Vector3(0f, 2.75f, 0.75f);
            task.transform.rotation = Quaternion.Euler(90f, 0, 0);

            // Luminárias lineares de LED (emissivas) em 3 fileiras.
            for (int r = 0; r < 3; r++)
                for (int i = 0; i < 3; i++)
                {
                    float x = -3.0f + i * 3.0f, z = -4.2f + r * 2.6f;
                    var body = Box(t, $"Luminaria_{r}{i}", "metal", new Vector3(x, RoomH - 0.42f, z), new Vector3(1.5f, 0.06f, 0.2f), 0.01f);
                    var led = Box(body.transform, "LED", "luz", new Vector3(0, -0.032f, 0), new Vector3(1.42f, 0.008f, 0.14f), 0.002f);
                    led.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                    Box(body.transform, "Cabo_E", "preto", new Vector3(-0.6f, 0.21f, 0), new Vector3(0.006f, 0.36f, 0.006f), 0f);
                    Box(body.transform, "Cabo_D", "preto", new Vector3(0.6f, 0.21f, 0), new Vector3(0.006f, 0.36f, 0.006f), 0f);
                }
            // Luminária extra centralizada sobre a bancada.
            var lb = Box(t, "Luminaria_Bancada", "metal", new Vector3(0f, 2.8f, 0.8f), new Vector3(1.8f, 0.06f, 0.22f), 0.01f);
            var lled = Box(lb.transform, "LED", "luz", new Vector3(0, -0.032f, 0), new Vector3(1.72f, 0.008f, 0.16f), 0.002f);
            lled.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Box(lb.transform, "Cabo_E", "preto", new Vector3(-0.7f, 0.4f, 0), new Vector3(0.006f, 0.74f, 0.006f), 0f);
            Box(lb.transform, "Cabo_D", "preto", new Vector3(0.7f, 0.4f, 0), new Vector3(0.006f, 0.74f, 0.006f), 0f);
        }

        // ================================================================ bancada, zonas e silhuetas
        struct SilSpec
        {
            public string id;
            public float x, z;
            public Vector3 rest; // rotação de repouso (Euler)
            public SilSpec(string id, float x, float z, Vector3 rest) { this.id = id; this.x = x; this.z = z; this.rest = rest; }
        }

        static void BuildBench(Transform t)
        {
            float cx = (BenchX0 + BenchX1) * 0.5f, cz = (BenchZ0 + BenchZ1) * 0.5f;
            float w = BenchX1 - BenchX0, d = BenchZ1 - BenchZ0;
            // Tampo de madeira + manta de borracha.
            Box(t, "Tampo", "madeira", new Vector3(cx, BenchTop - 0.024f, cz), new Vector3(w, 0.04f, d), 0.004f);
            Box(t, "Manta", "manta", new Vector3(cx, BenchTop - 0.002f, cz), new Vector3(w - 0.03f, 0.004f, d - 0.03f), 0.001f);
            // Estrutura de aço.
            float legH = BenchTop - 0.044f;
            float[] lx = { BenchX0 + 0.06f, BenchX1 - 0.06f, cx };
            float[] lz = { BenchZ0 + 0.06f, BenchZ1 - 0.06f };
            int k = 0;
            foreach (float x in lx)
                foreach (float z in lz)
                    Box(t, $"Pe_{k++}", "metal", new Vector3(x, legH * 0.5f, z), new Vector3(0.05f, legH, 0.05f), 0.003f);
            Box(t, "Travessa_Frente", "metal", new Vector3(cx, BenchTop - 0.08f, BenchZ0 + 0.06f), new Vector3(w - 0.1f, 0.06f, 0.04f), 0.003f);
            Box(t, "Travessa_Tras", "metal", new Vector3(cx, BenchTop - 0.08f, BenchZ1 - 0.06f), new Vector3(w - 0.1f, 0.06f, 0.04f), 0.003f);
            Box(t, "Prateleira_Inferior", "metal", new Vector3(cx, 0.18f, cz), new Vector3(w - 0.12f, 0.02f, d - 0.12f), 0.003f);
            Box(t, "Rodapes_Prateleira", "madeira", new Vector3(cx, 0.20f, cz), new Vector3(w - 0.16f, 0.015f, d - 0.16f), 0.003f);
            Box(t, "Encosto", "metal", new Vector3(cx, BenchTop + 0.06f, BenchZ1 + 0.01f), new Vector3(w, 0.12f, 0.02f), 0.003f);

            // Zonas por etapa (RF03). Centro (x -0,42 a 0,42) fica livre para o motor-base.
            var zones = new GameObject("Zonas_Etapas").transform;
            zones.SetParent(t, false);
            Zone(zones, 1, -1.48f, -0.88f, BenchZ0 + 0.02f, BenchZ1 - 0.02f);
            Zone(zones, 3, -0.86f, -0.44f, BenchZ0 + 0.02f, 0.82f);
            Zone(zones, 4, -0.86f, -0.44f, 0.83f, BenchZ1 - 0.02f);
            Zone(zones, 5, 0.44f, 0.80f, BenchZ0 + 0.02f, BenchZ1 - 0.02f);
            Zone(zones, 6, 0.82f, 1.20f, BenchZ0 + 0.02f, BenchZ1 - 0.02f);
            Zone(zones, 7, 1.22f, 1.48f, BenchZ0 + 0.02f, BenchZ1 - 0.02f);

            var down = new Vector3(-90f, 0, 0);   // eixo do motor apontando para cima (peças redondas deitadas)
            var alongX = new Vector3(0, 90f, 0);  // eixo do motor ao longo da bancada
            var screw = new Vector3(0, 0, 90f);   // parafuso deitado
            var specs = new List<SilSpec>
            {
                new SilSpec("P01", -1.18f, 0.98f, alongX),
                new SilSpec("P02", -1.34f, 0.66f, down),
                new SilSpec("P03", -1.06f, 0.66f, down),
                new SilSpec("P05", -0.70f, 0.635f, down),
                new SilSpec("P06", -0.70f, 1.015f, down),
                new SilSpec("P07", 0.58f, 0.64f, down),
                new SilSpec("P08", 0.58f, 1.02f, down),
                new SilSpec("P09", 0.92f, 1.03f, Vector3.zero),
                new SilSpec("P10", 0.92f, 0.70f, Vector3.zero),
                new SilSpec("P11", 1.35f, 0.82f, Vector3.zero),
            };
            for (int i = 0; i < 4; i++) specs.Add(new SilSpec("P12", -0.50f, 0.53f + i * 0.075f, screw));   // etapa 3
            for (int i = 0; i < 4; i++) specs.Add(new SilSpec("P12", -0.50f, 0.91f + i * 0.075f, screw));   // etapa 4
            for (int i = 0; i < 3; i++) specs.Add(new SilSpec("P12", 0.75f, 0.56f + i * 0.08f, screw));     // etapa 5
            for (int i = 0; i < 8; i++) specs.Add(new SilSpec("P12", 1.08f + (i % 2) * 0.07f, 0.56f + (i / 2) * 0.09f, screw)); // etapa 6

            var sil = new GameObject("Silhuetas").transform;
            sil.SetParent(t, false);
            int n = 0;
            foreach (var s in specs) Silhouette(sil, s, n++);
        }

        static void Zone(Transform parent, int step, float x0, float x1, float z0, float z1)
        {
            var go = Box(parent, $"Zona_Etapa_{step}", "zona", new Vector3((x0 + x1) * 0.5f, BenchTop + 0.001f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.002f, z1 - z0), 0.001f);
            // Moldura fina branca da zona.
            float y = BenchTop + 0.0025f, lw = 0.004f;
            Box(go.transform, "Borda_F", "silhueta", new Vector3(0, y - go.transform.localPosition.y, -(z1 - z0) * 0.5f + lw), new Vector3(x1 - x0 - 0.01f, 0.001f, lw), 0f);
            Box(go.transform, "Borda_T", "silhueta", new Vector3(0, y - go.transform.localPosition.y, (z1 - z0) * 0.5f - lw), new Vector3(x1 - x0 - 0.01f, 0.001f, lw), 0f);
            Box(go.transform, "Borda_E", "silhueta", new Vector3(-(x1 - x0) * 0.5f + lw, y - go.transform.localPosition.y, 0), new Vector3(lw, 0.001f, z1 - z0 - 0.01f), 0f);
            Box(go.transform, "Borda_D", "silhueta", new Vector3((x1 - x0) * 0.5f - lw, y - go.transform.localPosition.y, 0), new Vector3(lw, 0.001f, z1 - z0 - 0.01f), 0f);
            Label(go.transform, "Rotulo", $"ETAPA {step}", new Vector3(0, 0.003f, -(z1 - z0) * 0.5f + 0.025f), Quaternion.Euler(90f, 0, 0),
                0.32f, new Color(0.85f, 0.87f, 0.9f), new Vector2(x1 - x0, 0.05f));
        }

        static void Silhouette(Transform parent, SilSpec s, int index)
        {
            string file = FileOf(s.id);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{PartMeshDir}/{file}_LOD1.asset");
            if (mesh == null) return;
            var rot = Quaternion.Euler(s.rest);
            // Calcula o deslocamento para a peça apoiar na bancada, centrada no ponto da silhueta.
            var bmin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var bmax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var bb = mesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var c = new Vector3((i & 1) == 0 ? bb.min.x : bb.max.x, (i & 2) == 0 ? bb.min.y : bb.max.y, (i & 4) == 0 ? bb.min.z : bb.max.z);
                var r = rot * c;
                bmin = Vector3.Min(bmin, r);
                bmax = Vector3.Max(bmax, r);
            }
            var pivot = new Vector3(-(bmin.x + bmax.x) * 0.5f, -bmin.y, -(bmin.z + bmax.z) * 0.5f);

            var root = new GameObject($"Silhueta_{s.id}_{index:00}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(s.x, BenchTop, s.z);
            var info = root.AddComponent<W22PartInfo>();
            info.partId = s.id;
            info.displayName = $"Silhueta {s.id}";

            // Onde o pivô da peça fica quando ela é colocada sobre a silhueta (para o snap).
            var fit = new GameObject("Encaixe");
            fit.transform.SetParent(root.transform, false);
            fit.transform.localPosition = pivot;
            fit.transform.localRotation = rot;

            // Contorno branco: a própria malha achatada sobre a bancada (sem textura nem detalhe).
            var flat = new GameObject("Contorno");
            flat.transform.SetParent(root.transform, false);
            flat.transform.localPosition = new Vector3(0, 0.0035f, 0);
            flat.transform.localScale = new Vector3(1f, 0.004f, 1f);
            var vis = new GameObject("Malha");
            vis.transform.SetParent(flat.transform, false);
            vis.transform.localPosition = new Vector3(pivot.x, 0, pivot.z);
            vis.transform.localRotation = rot;
            vis.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = vis.AddComponent<MeshRenderer>();
            var mats = new Material[mesh.subMeshCount];
            for (int i = 0; i < mats.Length; i++) mats[i] = M["silhueta"];
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ================================================================ motor-base
        static void BuildMotorStand(Transform t)
        {
            float x = 0f, z = 0.80f;
            Cyl(t, "Base_Giratoria", "metal", new Vector3(x, BenchTop + 0.012f, z), 0.30f, 0.024f, 48);
            Cyl(t, "Base_Giratoria_Topo", "manta", new Vector3(x, BenchTop + 0.0255f, z), 0.285f, 0.003f, 48);
            Cyl(t, "Base_Giratoria_Anel", "amarelo", new Vector3(x, BenchTop + 0.013f, z), 0.302f, 0.006f, 48);

            // Motor-base: carcaça com estator (P04), eixo ao longo da bancada, apoiada nos pés.
            var anchor = new GameObject("Ponto_MotorBase").transform;
            anchor.SetParent(t, false);
            anchor.localPosition = new Vector3(x + 0.165f, BenchTop + 0.027f + W22Dimensions.H, z);
            anchor.localRotation = Quaternion.Euler(0, 90f, 0);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PartsDir}/P04_Carcaca_Estator.prefab");
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.SetParent(anchor, false);
        }

        // ================================================================ caixa de peças (direita)
        static void BuildPartsCrate(Transform t)
        {
            float cx = 1.15f, cz = -0.35f;
            float bw = 0.90f, bd = 0.60f, floorY = 0.50f, rimY = 0.86f, wall = 0.015f;
            // Suporte.
            Box(t, "Suporte_Tampo", "metal", new Vector3(cx, floorY - 0.02f, cz), new Vector3(bw + 0.06f, 0.03f, bd + 0.06f), 0.004f);
            for (int i = 0; i < 4; i++)
            {
                float lx = cx + ((i & 1) == 0 ? -1 : 1) * (bw * 0.5f - 0.02f);
                float lz = cz + ((i & 2) == 0 ? -1 : 1) * (bd * 0.5f - 0.02f);
                Box(t, $"Suporte_Pe_{i}", "metal", new Vector3(lx, (floorY - 0.035f) * 0.5f, lz), new Vector3(0.04f, floorY - 0.035f, 0.04f), 0.003f);
                Cyl(t, $"Rodizio_{i}", "preto", new Vector3(lx, 0.035f, lz), 0.035f, 0.025f, 12, Quaternion.Euler(0, 0, 90f));
            }
            // Caixa plástica amarela (aberta em cima).
            float h = rimY - floorY;
            Box(t, "Caixa_Fundo", "amarelo", new Vector3(cx, floorY + wall * 0.5f, cz), new Vector3(bw, wall, bd), 0.004f);
            Box(t, "Caixa_Lado_F", "amarelo", new Vector3(cx, floorY + h * 0.5f, cz - bd * 0.5f + wall * 0.5f), new Vector3(bw, h, wall), 0.004f);
            Box(t, "Caixa_Lado_T", "amarelo", new Vector3(cx, floorY + h * 0.5f, cz + bd * 0.5f - wall * 0.5f), new Vector3(bw, h, wall), 0.004f);
            Box(t, "Caixa_Lado_E", "amarelo", new Vector3(cx - bw * 0.5f + wall * 0.5f, floorY + h * 0.5f, cz), new Vector3(wall, h, bd), 0.004f);
            Box(t, "Caixa_Lado_D", "amarelo", new Vector3(cx + bw * 0.5f - wall * 0.5f, floorY + h * 0.5f, cz), new Vector3(wall, h, bd), 0.004f);
            Box(t, "Caixa_Borda", "amarelo", new Vector3(cx, rimY - 0.01f, cz - bd * 0.5f - 0.006f), new Vector3(bw + 0.02f, 0.02f, 0.012f), 0.003f);
            Box(t, "Caixa_Pegador", "preto", new Vector3(cx, rimY - 0.06f, cz - bd * 0.5f - 0.008f), new Vector3(0.18f, 0.03f, 0.01f), 0.004f);
            Label(t, "Caixa_Rotulo", "PEÇAS", new Vector3(cx, floorY + h * 0.45f, cz - bd * 0.5f - 0.004f), Quaternion.identity, 0.6f,
                new Color(0.1f, 0.1f, 0.1f), new Vector2(0.4f, 0.1f));

            // Área de spawn para a lógica (RF01: peças em posição aleatória).
            var spawn = new GameObject("Area_Spawn").transform;
            spawn.SetParent(t, false);
            spawn.localPosition = new Vector3(cx, floorY + 0.15f, cz);
            var col = spawn.gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(bw - 0.08f, 0.25f, bd - 0.08f);

            // Peças e distratoras dentro da caixa (posição inicial de exemplo, semente fixa).
            var rnd = new System.Random(22);
            var items = new List<string>();
            foreach (var p in Parts) if (p.id != "P04" && p.id != "P12") items.Add(p.file);
            items.AddRange(Distractors);
            Shuffle(items, rnd);
            var pieces = new GameObject("Pecas").transform;
            pieces.SetParent(t, false);
            int cols = 4, rows = 3;
            for (int i = 0; i < items.Count; i++)
            {
                int layer = i / (cols * rows), cell = i % (cols * rows);
                float px = cx - bw * 0.5f + 0.13f + (cell % cols) * ((bw - 0.26f) / (cols - 1));
                float pz = cz - bd * 0.5f + 0.13f + (cell / cols) * ((bd - 0.26f) / (rows - 1));
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PartsDir}/{items[i]}.prefab");
                if (prefab == null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.SetParent(pieces, false);
                var rot = Quaternion.Euler((float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 360f);
                inst.transform.localRotation = rot;
                // Apoia a peça no fundo (ou na camada de cima) usando os limites do colisor.
                var bc = inst.GetComponent<BoxCollider>();
                float lift = 0.05f;
                if (bc != null)
                {
                    float minY = float.MaxValue;
                    for (int c = 0; c < 8; c++)
                    {
                        var corner = bc.center + Vector3.Scale(bc.size * 0.5f, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                        minY = Mathf.Min(minY, (rot * corner).y);
                    }
                    lift = -minY;
                }
                inst.transform.localPosition = new Vector3(px + (float)(rnd.NextDouble() - 0.5) * 0.05f, floorY + wall + lift + layer * 0.14f,
                    pz + (float)(rnd.NextDouble() - 0.5) * 0.05f);
            }

            // Bandeja de parafusos ao lado da caixa (19 parafusos P12).
            float tx = cx + bw * 0.5f + 0.22f, tz = cz + 0.05f, ty = 0.86f;
            Box(t, "Bandeja_Suporte", "metal", new Vector3(tx, ty * 0.5f, tz), new Vector3(0.05f, ty, 0.05f), 0.003f);
            Box(t, "Bandeja_Fundo", "preto", new Vector3(tx, ty + 0.005f, tz), new Vector3(0.26f, 0.01f, 0.18f), 0.003f);
            Box(t, "Bandeja_Borda_F", "preto", new Vector3(tx, ty + 0.025f, tz - 0.085f), new Vector3(0.26f, 0.04f, 0.01f), 0.002f);
            Box(t, "Bandeja_Borda_T", "preto", new Vector3(tx, ty + 0.025f, tz + 0.085f), new Vector3(0.26f, 0.04f, 0.01f), 0.002f);
            Box(t, "Bandeja_Borda_E", "preto", new Vector3(tx - 0.125f, ty + 0.025f, tz), new Vector3(0.01f, 0.04f, 0.18f), 0.002f);
            Box(t, "Bandeja_Borda_D", "preto", new Vector3(tx + 0.125f, ty + 0.025f, tz), new Vector3(0.01f, 0.04f, 0.18f), 0.002f);
            var screwPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PartsDir}/P12_Parafuso.prefab");
            for (int i = 0; i < 19; i++)
            {
                var s = (GameObject)PrefabUtility.InstantiatePrefab(screwPrefab);
                s.transform.SetParent(pieces, false);
                s.transform.localRotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 90f);
                s.transform.localPosition = new Vector3(tx - 0.09f + (i % 5) * 0.045f + (float)(rnd.NextDouble() - 0.5) * 0.01f, ty + 0.0165f,
                    tz - 0.06f + (i / 5) * 0.04f);
            }
        }

        static void Shuffle(List<string> l, System.Random rnd)
        {
            for (int i = l.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                var tmp = l[i]; l[i] = l[j]; l[j] = tmp;
            }
        }

        // ================================================================ ambientação
        static void BuildProps(Transform t)
        {
            BuildToolWall(new GameObject("Painel_Ferramentas").transform, t);
            BuildRollCabinet(new GameObject("Armario_Ferramentas").transform, t, new Vector3(-2.35f, 0f, 1.75f));
            BuildShelf(new GameObject("Estante").transform, t);
            BuildCrane(new GameObject("Ponte_Rolante").transform, t);
            BuildSafety(new GameObject("Seguranca").transform, t);
        }

        static void BuildToolWall(Transform t, Transform parent)
        {
            t.SetParent(parent, false);
            float z = RoomZ1 - 0.02f;
            Plane(t, "Painel", "painel", new Vector3(0, 1.62f, z), new Vector2(2.6f, 0.95f), FaceMinusZ, 0.025f);
            Box(t, "Moldura_Sup", "metal", new Vector3(0, 2.105f, z - 0.01f), new Vector3(2.64f, 0.03f, 0.03f), 0.003f);
            Box(t, "Moldura_Inf", "metal", new Vector3(0, 1.135f, z - 0.01f), new Vector3(2.64f, 0.03f, 0.03f), 0.003f);
            Box(t, "Moldura_E", "metal", new Vector3(-1.31f, 1.62f, z - 0.01f), new Vector3(0.03f, 1.0f, 0.03f), 0.003f);
            Box(t, "Moldura_D", "metal", new Vector3(1.31f, 1.62f, z - 0.01f), new Vector3(0.03f, 1.0f, 0.03f), 0.003f);
            Label(t, "Placa_Oficina", "OFICINA DE MONTAGEM  ·  MOTOR TRIFÁSICO W22", new Vector3(0, 2.32f, z - 0.005f), Quaternion.identity, 1.6f,
                new Color(0.18f, 0.22f, 0.28f), new Vector2(5.0f, 0.25f));

            float fz = z - 0.025f;
            // Chaves combinadas (tamanhos crescentes).
            for (int i = 0; i < 7; i++)
            {
                float len = 0.14f + i * 0.02f, x = -1.15f + i * 0.07f, y = 1.92f - len * 0.5f;
                Box(t, $"Chave_{i}_Haste", "aco", new Vector3(x, y, fz), new Vector3(0.018f + i * 0.002f, len, 0.006f), 0.002f);
                Cyl(t, $"Chave_{i}_Boca", "aco", new Vector3(x, y + len * 0.5f, fz), 0.018f + i * 0.002f, 0.008f, 14, Quaternion.Euler(90f, 0, 0));
                Cyl(t, $"Chave_{i}_Anel", "aco", new Vector3(x, y - len * 0.5f, fz), 0.016f + i * 0.002f, 0.008f, 14, Quaternion.Euler(90f, 0, 0));
            }
            // Martelo.
            Cyl(t, "Martelo_Cabo", "madeira", new Vector3(-0.45f, 1.55f, fz), 0.014f, 0.32f, 10);
            Box(t, "Martelo_Cabeca", "metal", new Vector3(-0.45f, 1.73f, fz), new Vector3(0.12f, 0.04f, 0.035f), 0.004f);
            // Chaves de fenda.
            string[] handleMats = { "vermelho", "amarelo", "vermelho", "amarelo", "preto" };
            for (int i = 0; i < 5; i++)
            {
                float x = -0.2f + i * 0.06f;
                Cyl(t, $"Fenda_{i}_Cabo", handleMats[i], new Vector3(x, 1.86f, fz), 0.014f, 0.1f, 10);
                Cyl(t, $"Fenda_{i}_Haste", "aco", new Vector3(x, 1.73f, fz), 0.004f, 0.16f, 8);
            }
            // Alicates (simplificados).
            for (int i = 0; i < 3; i++)
            {
                float x = 0.25f + i * 0.09f;
                Box(t, $"Alicate_{i}_Cabo_E", "vermelho", new Vector3(x - 0.012f, 1.66f, fz), new Vector3(0.016f, 0.14f, 0.01f), 0.004f, Quaternion.Euler(0, 0, 6f));
                Box(t, $"Alicate_{i}_Cabo_D", "vermelho", new Vector3(x + 0.012f, 1.66f, fz), new Vector3(0.016f, 0.14f, 0.01f), 0.004f, Quaternion.Euler(0, 0, -6f));
                Box(t, $"Alicate_{i}_Boca", "metal", new Vector3(x, 1.77f, fz), new Vector3(0.03f, 0.07f, 0.012f), 0.004f);
            }
            // Torquímetro e chave inglesa.
            Box(t, "Torquimetro", "aco", new Vector3(0.75f, 1.62f, fz), new Vector3(0.022f, 0.45f, 0.02f), 0.004f);
            Cyl(t, "Torquimetro_Cabo", "preto", new Vector3(0.75f, 1.44f, fz), 0.016f, 0.1f, 10);
            Box(t, "Chave_Inglesa", "aco", new Vector3(0.92f, 1.62f, fz), new Vector3(0.03f, 0.26f, 0.01f), 0.004f);
            Box(t, "Chave_Inglesa_Boca", "aco", new Vector3(0.92f, 1.77f, fz), new Vector3(0.07f, 0.05f, 0.012f), 0.004f);
            // Prateleira de potes (rolamentos, graxa).
            Box(t, "Prateleira_Painel", "metal", new Vector3(1.05f, 1.30f, fz - 0.06f), new Vector3(0.36f, 0.015f, 0.14f), 0.003f);
            for (int i = 0; i < 3; i++)
                Cyl(t, $"Pote_{i}", i == 1 ? "vermelho" : "metal", new Vector3(0.95f + i * 0.1f, 1.35f, fz - 0.06f), 0.035f, 0.08f, 14);
        }

        static void BuildRollCabinet(Transform t, Transform parent, Vector3 pos)
        {
            t.SetParent(parent, false);
            t.localPosition = pos;
            Box(t, "Corpo", "vermelho", new Vector3(0, 0.52f, 0), new Vector3(0.72f, 0.88f, 0.46f), 0.01f);
            Box(t, "Tampo", "preto", new Vector3(0, 0.975f, 0), new Vector3(0.74f, 0.03f, 0.48f), 0.006f);
            float[] drawerH = { 0.09f, 0.09f, 0.09f, 0.14f, 0.2f };
            float y = 0.92f;
            for (int i = 0; i < drawerH.Length; i++)
            {
                y -= drawerH[i] + 0.012f;
                Box(t, $"Gaveta_{i}", "vermelho", new Vector3(0, y + drawerH[i] * 0.5f, -0.232f), new Vector3(0.66f, drawerH[i], 0.012f), 0.004f);
                Box(t, $"Puxador_{i}", "aco", new Vector3(0, y + drawerH[i] - 0.02f, -0.245f), new Vector3(0.5f, 0.012f, 0.012f), 0.004f);
            }
            for (int i = 0; i < 4; i++)
                Cyl(t, $"Rodizio_{i}", "preto", new Vector3(((i & 1) == 0 ? -0.3f : 0.3f), 0.04f, ((i & 2) == 0 ? -0.18f : 0.18f)), 0.04f, 0.03f, 12, Quaternion.Euler(0, 0, 90f));
        }

        static void BuildShelf(Transform t, Transform parent)
        {
            t.SetParent(parent, false);
            t.localPosition = new Vector3(RoomX0 + 0.35f, 0, -1.6f);
            float len = 2.4f, depth = 0.55f, h = 2.2f;
            for (int i = 0; i < 3; i++)
                for (int s = -1; s <= 1; s += 2)
                    Box(t, $"Montante_{i}_{(s < 0 ? "A" : "B")}", "amarelo", new Vector3(s * depth * 0.5f, h * 0.5f, -len * 0.5f + i * len * 0.5f), new Vector3(0.06f, h, 0.06f), 0.003f);
            float[] levels = { 0.12f, 0.85f, 1.55f, 2.15f };
            for (int i = 0; i < levels.Length; i++)
            {
                Box(t, $"Longarina_{i}", "metal", new Vector3(0, levels[i], 0), new Vector3(depth, 0.04f, len), 0.003f);
                Box(t, $"Assoalho_{i}", "madeira", new Vector3(0, levels[i] + 0.03f, 0), new Vector3(depth - 0.02f, 0.02f, len - 0.04f), 0.002f);
            }
            // Caixas de papelão.
            var rnd = new System.Random(7);
            for (int i = 0; i < 9; i++)
            {
                int lvl = i % 3 == 0 ? 0 : (i % 3 == 1 ? 2 : 3);
                float bz = -len * 0.5f + 0.3f + (i / 3) * 0.75f;
                var size = new Vector3(0.35f + (float)rnd.NextDouble() * 0.12f, 0.22f + (float)rnd.NextDouble() * 0.12f, 0.34f + (float)rnd.NextDouble() * 0.15f);
                Box(t, $"CaixaPapelao_{i}", "papelao", new Vector3(0, levels[lvl] + 0.04f + size.y * 0.5f, bz), size, 0.006f, Quaternion.Euler(0, (float)rnd.NextDouble() * 10f - 5f, 0));
            }
            // Motores prontos na prateleira do meio (prefab montado).
            var motor = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/WEG/WEG_W22_132M_Montado.prefab");
            if (motor != null)
                for (int i = 0; i < 2; i++)
                {
                    var m = (GameObject)PrefabUtility.InstantiatePrefab(motor);
                    m.transform.SetParent(t, false);
                    m.transform.localPosition = new Vector3(0.02f, levels[1] + 0.04f + W22Dimensions.H, -0.55f + i * 0.85f);
                    m.transform.localRotation = Quaternion.Euler(0, i == 0 ? 90f : -90f, 0);
                }
        }

        static void BuildCrane(Transform t, Transform parent)
        {
            t.SetParent(parent, false);
            float y = RoomH - 0.55f, z = -0.2f;
            Box(t, "Trilho_E", "amarelo", new Vector3(RoomX0 + 0.3f, y, -1.8f), new Vector3(0.18f, 0.22f, 7.6f), 0.006f);
            Box(t, "Trilho_D", "amarelo", new Vector3(RoomX1 - 0.3f, y, -1.8f), new Vector3(0.18f, 0.22f, 7.6f), 0.006f);
            Box(t, "Viga_Principal", "amarelo", new Vector3(0, y - 0.05f, z), new Vector3(RoomX1 - RoomX0 - 0.5f, 0.32f, 0.22f), 0.008f);
            Box(t, "Viga_Aba_Inf", "amarelo", new Vector3(0, y - 0.22f, z), new Vector3(RoomX1 - RoomX0 - 0.5f, 0.03f, 0.3f), 0.004f);
            Box(t, "Cabeceira_E", "metal", new Vector3(RoomX0 + 0.3f, y + 0.12f, z), new Vector3(0.3f, 0.12f, 0.6f), 0.006f);
            Box(t, "Cabeceira_D", "metal", new Vector3(RoomX1 - 0.3f, y + 0.12f, z), new Vector3(0.3f, 0.12f, 0.6f), 0.006f);
            float hx = -1.4f;
            Box(t, "Talha_Carro", "metal", new Vector3(hx, y - 0.3f, z), new Vector3(0.3f, 0.12f, 0.36f), 0.008f);
            Box(t, "Talha_Corpo", "amarelo", new Vector3(hx, y - 0.48f, z), new Vector3(0.24f, 0.26f, 0.26f), 0.02f);
            Cyl(t, "Talha_Motor", "metal", new Vector3(hx + 0.18f, y - 0.48f, z), 0.08f, 0.14f, 14, Quaternion.Euler(0, 0, 90f));
            float chainTop = y - 0.62f, chainBot = 1.75f;
            for (int i = 0; i < 2; i++)
                Box(t, $"Corrente_{i}", "metal", new Vector3(hx + (i == 0 ? -0.04f : 0.04f), (chainTop + chainBot) * 0.5f, z), new Vector3(0.012f, chainTop - chainBot, 0.012f), 0f);
            Box(t, "Moitao", "amarelo", new Vector3(hx, chainBot - 0.06f, z), new Vector3(0.14f, 0.12f, 0.06f), 0.01f);
            Cyl(t, "Gancho", "metal", new Vector3(hx, chainBot - 0.17f, z), 0.04f, 0.02f, 14, Quaternion.Euler(90f, 0, 0));
            Box(t, "Botoeira_Cabo", "preto", new Vector3(hx + 0.2f, (chainTop + 1.25f) * 0.5f, z), new Vector3(0.008f, chainTop - 1.25f, 0.008f), 0f);
            Box(t, "Botoeira", "amarelo", new Vector3(hx + 0.2f, 1.18f, z), new Vector3(0.07f, 0.16f, 0.05f), 0.01f);
        }

        static void BuildSafety(Transform t, Transform parent)
        {
            t.SetParent(parent, false);
            float z = RoomZ1 - 0.12f;
            // Extintor na parede da frente, à direita.
            float ex = 2.9f;
            Cyl(t, "Extintor_Corpo", "vermelho", new Vector3(ex, 0.55f, z), 0.08f, 0.5f, 16);
            Cyl(t, "Extintor_Topo", "preto", new Vector3(ex, 0.83f, z), 0.03f, 0.06f, 10);
            Box(t, "Extintor_Gatilho", "preto", new Vector3(ex + 0.03f, 0.88f, z), new Vector3(0.08f, 0.02f, 0.03f), 0.004f);
            Box(t, "Extintor_Placa", "vermelho", new Vector3(ex, 1.35f, RoomZ1 - 0.005f), new Vector3(0.3f, 0.3f, 0.006f), 0.004f);
            Label(t, "Extintor_Texto", "EXTINTOR", new Vector3(ex, 1.35f, RoomZ1 - 0.01f), Quaternion.identity, 0.45f, Color.white, new Vector2(0.3f, 0.08f));
            Box(t, "Extintor_Piso", "vermelho", new Vector3(ex, 0.002f, z - 0.3f), new Vector3(0.7f, 0.003f, 0.7f), 0f);

            // Placas de EPI.
            Box(t, "Placa_EPI", "amarelo", new Vector3(-2.35f, 1.6f, RoomZ1 - 0.005f), new Vector3(0.5f, 0.32f, 0.006f), 0.004f);
            Label(t, "Placa_EPI_Texto", "USE ÓCULOS\nE LUVAS", new Vector3(-2.35f, 1.6f, RoomZ1 - 0.01f), Quaternion.identity, 0.55f,
                new Color(0.08f, 0.08f, 0.08f), new Vector2(0.48f, 0.3f));
            Box(t, "Placa_Atencao", "amarelo", new Vector3(RoomX1 - 0.005f, 1.6f, -0.6f), new Vector3(0.006f, 0.32f, 0.5f), 0.004f);
            Label(t, "Placa_Atencao_Texto", "ATENÇÃO\nCARGA SUSPENSA", new Vector3(RoomX1 - 0.01f, 1.6f, -0.6f), Quaternion.Euler(0, 90f, 0), 0.45f,
                new Color(0.08f, 0.08f, 0.08f), new Vector2(0.48f, 0.3f));
        }

        // ================================================================ rig XR
        static void BuildXrRig()
        {
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(XrRigPath);
            if (rig == null)
            {
                Debug.LogWarning($"[WEG] Rig XR não encontrado em {XrRigPath}. Adicione um XR Origin manualmente.");
                return;
            }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(rig);
            inst.transform.position = Vector3.zero;
            inst.transform.rotation = Quaternion.identity;

            // Simulador XR para testar sem óculos (mouse e teclado). Tag EditorOnly: não entra no build do Quest.
            var sim = AssetDatabase.LoadAssetAtPath<GameObject>(XrSimulatorPath);
            if (sim != null)
            {
                var s = (GameObject)PrefabUtility.InstantiatePrefab(sim);
                s.tag = "EditorOnly";
                s.AddComponent<W22SimulatorHeight>();
            }
            else
                Debug.LogWarning("[WEG] XR Interaction Simulator não importado (Package Manager > XR Interaction Toolkit > Samples).");
        }

        const string XrSimulatorPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/XR Interaction Simulator/XR Interaction Simulator.prefab";

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
