using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WEG.W22.EditorTools
{
    /// <summary>
    /// Ferramentas pegáveis da oficina (prefabs). Eixo longo em Y local, face plana voltada para -Z
    /// (pendurada no painel), pivô no centro. A chave combinada 13 mm e o torquímetro apertam
    /// os parafusos M8 (cabeça sextavada de 13 mm).
    /// </summary>
    public static class W22ToolFactory
    {
        public const string PrefabDir = "Assets/Prefabs/WEG/Tools";
        const string MeshDir = "Assets/Models/WEG/Tools";

        // Slots de material do WegMeshBuilder reaproveitados para as ferramentas.
        const int Red = 0, Steel = 1, Wood = 2, Black = 3, DarkMetal = 4;

        public class ToolDef
        {
            public string id, file, name;
            public bool canTighten;
            public Vector3 tip;
            public System.Func<WegMeshBuilder> build;
        }

        public static readonly ToolDef[] Tools =
        {
            new ToolDef { id = "T01", file = "T01_Chave_Combinada_13mm", name = "Chave combinada 13 mm", canTighten = true, tip = new Vector3(0, 0.088f, 0), build = Wrench },
            new ToolDef { id = "T02", file = "T02_Chave_de_Fenda", name = "Chave de fenda", tip = new Vector3(0, 0.12f, 0), build = Screwdriver },
            new ToolDef { id = "T03", file = "T03_Martelo", name = "Martelo", tip = new Vector3(0, 0.13f, 0), build = Hammer },
            new ToolDef { id = "T04", file = "T04_Alicate", name = "Alicate universal", tip = new Vector3(0, 0.09f, 0), build = Pliers },
            new ToolDef { id = "T05", file = "T05_Torquimetro", name = "Torquímetro com soquete 13 mm", canTighten = true, tip = new Vector3(0, 0.19f, -0.04f), build = TorqueWrench },
        };

        public static Dictionary<string, GameObject> BuildAll(Material[] slotMats)
        {
            EnsureFolder(PrefabDir);
            EnsureFolder(MeshDir);
            var result = new Dictionary<string, GameObject>();
            foreach (var t in Tools)
            {
                var b = t.build();
                int[] used;
                var mesh = b.ToMesh(t.file, out used);
                mesh = SaveMesh(mesh, $"{MeshDir}/{t.file}.asset");
                var root = new GameObject(t.file);
                var info = root.AddComponent<W22PartInfo>();
                info.partId = t.id;
                info.displayName = t.name;
                var vis = new GameObject("LOD0");
                vis.transform.SetParent(root.transform, false);
                vis.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mats = new Material[used.Length];
                for (int i = 0; i < used.Length; i++) mats[i] = slotMats[used[i]];
                vis.AddComponent<MeshRenderer>().sharedMaterials = mats;
                var tipGo = new GameObject("Ponta");
                tipGo.transform.SetParent(root.transform, false);
                tipGo.transform.localPosition = t.tip;
                var bc = root.AddComponent<BoxCollider>();
                bc.center = mesh.bounds.center;
                bc.size = Vector3.Max(mesh.bounds.size, Vector3.one * 0.02f);
                result[t.id] = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{t.file}.prefab");
                Object.DestroyImmediate(root);
            }
            return result;
        }

        // ---------------------------------------------------------------- geometria
        static void CylY(WegMeshBuilder b, int m, Vector3 c, float r, float h, int seg = 14)
        {
            var c1 = new WegMeshBuilder();
            c1.Cylinder(m, r, -h * 0.5f, h * 0.5f, seg, Mathf.Min(0.003f, r * 0.25f));
            b.Append(c1, Matrix4x4.TRS(c, Quaternion.Euler(-90f, 0, 0), Vector3.one));
        }

        static WegMeshBuilder Wrench()
        {
            var b = new WegMeshBuilder();
            b.ChamferBox(Steel, new Vector3(0, -0.005f, 0), new Vector3(0.017f, 0.13f, 0.006f), 0.002f);
            // Boca aberta 13 mm (lado de cima).
            b.ChamferBox(Steel, new Vector3(0, 0.067f, 0), new Vector3(0.042f, 0.014f, 0.007f), 0.002f);
            b.ChamferBox(Steel, new Vector3(-0.0135f, 0.083f, 0), new Vector3(0.009f, 0.024f, 0.007f), 0.002f);
            b.ChamferBox(Steel, new Vector3(0.0135f, 0.083f, 0), new Vector3(0.009f, 0.024f, 0.007f), 0.002f);
            // Estrela (anel) do lado de baixo.
            var ring = new WegMeshBuilder();
            ring.Tube(Steel, 0.0075f, 0.0165f, -0.0035f, 0.0035f, 18);
            b.Append(ring, Matrix4x4.Translate(new Vector3(0, -0.078f, 0)));
            return b;
        }

        static WegMeshBuilder Screwdriver()
        {
            var b = new WegMeshBuilder();
            CylY(b, Red, new Vector3(0, -0.06f, 0), 0.016f, 0.1f, 12);
            CylY(b, Black, new Vector3(0, -0.006f, 0), 0.011f, 0.012f, 12);
            CylY(b, Steel, new Vector3(0, 0.055f, 0), 0.0035f, 0.11f, 8);
            b.ChamferBox(Steel, new Vector3(0, 0.114f, 0), new Vector3(0.007f, 0.012f, 0.0016f), 0.0004f);
            return b;
        }

        static WegMeshBuilder Hammer()
        {
            var b = new WegMeshBuilder();
            CylY(b, Wood, new Vector3(0, -0.03f, 0), 0.013f, 0.27f, 10);
            CylY(b, Black, new Vector3(0, -0.13f, 0), 0.0155f, 0.08f, 10);
            b.ChamferBox(DarkMetal, new Vector3(0, 0.12f, 0), new Vector3(0.11f, 0.034f, 0.032f), 0.004f);
            b.ChamferBox(DarkMetal, new Vector3(0.06f, 0.12f, 0), new Vector3(0.012f, 0.03f, 0.03f), 0.003f);
            return b;
        }

        static WegMeshBuilder Pliers()
        {
            var b = new WegMeshBuilder();
            b.ChamferBox(Red, new Vector3(-0.014f, -0.04f, 0), new Vector3(0.016f, 0.13f, 0.012f), 0.005f, Quaternion.Euler(0, 0, -6f));
            b.ChamferBox(Red, new Vector3(0.014f, -0.04f, 0), new Vector3(0.016f, 0.13f, 0.012f), 0.005f, Quaternion.Euler(0, 0, 6f));
            b.ChamferBox(Steel, new Vector3(0, 0.055f, 0), new Vector3(0.026f, 0.07f, 0.012f), 0.004f);
            var pin = new WegMeshBuilder();
            pin.Cylinder(Steel, 0.008f, -0.008f, 0.008f, 12);
            b.Append(pin, Matrix4x4.Translate(new Vector3(0, 0.03f, 0)));
            return b;
        }

        static WegMeshBuilder TorqueWrench()
        {
            var b = new WegMeshBuilder();
            b.ChamferBox(Steel, new Vector3(0, 0.0f, 0), new Vector3(0.022f, 0.30f, 0.02f), 0.004f);
            CylY(b, Black, new Vector3(0, -0.19f, 0), 0.017f, 0.1f, 12);
            b.ChamferBox(DarkMetal, new Vector3(0, 0.175f, 0), new Vector3(0.04f, 0.05f, 0.026f), 0.006f);
            // Soquete 13 mm saindo da cabeça (para o lado de fora do painel).
            var sock = new WegMeshBuilder();
            sock.Cylinder(Steel, 0.0115f, -0.032f, 0f, 14, 0.002f);
            b.Append(sock, Matrix4x4.Translate(new Vector3(0, 0.19f, -0.008f)));
            // Escala graduada.
            b.ChamferBox(Black, new Vector3(0, -0.06f, -0.0105f), new Vector3(0.012f, 0.06f, 0.001f), 0f);
            return b;
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
