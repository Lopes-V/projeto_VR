using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using D = WEG.W22.W22Dimensions;

namespace WEG.W22.EditorTools
{
    /// <summary>
    /// Gera as peças 3D do motor WEG W22 132M (P01 a P12) e as distratoras (D01 a D08)
    /// como malhas procedurais low poly, materiais URP, prefabs com pivô no ponto de encaixe,
    /// o motor montado e uma cena de pré-visualização.
    /// Medidas: somente W22Dimensions. Especificação: Assets/Reference/WEG/montagem-pecas.md.
    /// </summary>
    public static class W22ModelGenerator
    {
        const string MeshDir = "Assets/Models/WEG/Meshes";
        const string MatDir = "Assets/Materials/WEG";
        const string PrefabDir = "Assets/Prefabs/WEG";
        const string PartsDir = "Assets/Prefabs/WEG/Parts";
        const string ScenePath = "Assets/Scenes/WEG_W22_Preview.unity";
        const string ReportPath = "Assets/Reference/WEG/relatorio-modelos.md";

        delegate WegMeshBuilder BuildFn(int lod);

        class PartDef
        {
            public string id, file, name;
            public int step;
            public bool distractor;
            public bool local;        // true: malha já construída em torno do pivô
            public Vector3 fit;       // posição do pivô no motor montado
            public BuildFn build;
        }

        struct ScrewSlot
        {
            public string group;
            public Vector3 pos;
            public Vector3 dir;
        }

        static int Seg(int lod) => lod == 0 ? 64 : 28;
        static int SegS(int lod) => lod == 0 ? 24 : 12;
        static float Rad(float deg) => deg * Mathf.Deg2Rad;

        // ================================================================ menu
        [MenuItem("WEG/Gerar modelos W22 132M")]
        public static void GenerateAll()
        {
            EnsureFolder(MeshDir);
            EnsureFolder(MatDir);
            EnsureFolder(PartsDir);
            EnsureFolder("Assets/Scenes");

            var mats = EnsureMaterials();
            var defs = PartDefs();
            var prefabs = new Dictionary<string, GameObject>();
            var report = new StringBuilder();
            report.AppendLine("# Relatório dos modelos gerados (W22 132M)");
            report.AppendLine();
            report.AppendLine("Gerado por `WEG/Gerar modelos W22 132M`. Medidas em `Assets/Scripts/WEG/W22Dimensions.cs`.");
            report.AppendLine();
            report.AppendLine("| ID | Peça | Etapa | Triângulos LOD0 | Triângulos LOD1 | Materiais | Tamanho (m) |");
            report.AppendLine("|---|---|---|---|---|---|---|");

            int total = 0, totalMotor = 0;
            foreach (var def in defs)
            {
                int tri0, tri1;
                string matNames;
                Vector3 size;
                prefabs[def.id] = BuildPartPrefab(def, mats, out tri0, out tri1, out matNames, out size);
                total += tri0;
                if (!def.distractor) totalMotor += tri0;
                report.AppendLine($"| {def.id} | {def.name} | {(def.distractor ? "-" : def.step.ToString())} | {tri0} | {tri1} | {matNames} | {size.x:0.000} x {size.y:0.000} x {size.z:0.000} |");
            }

            var slots = ScrewSlots();
            int screwTris = 0;
            {
                var b = Screw(0);
                screwTris = b.TriangleCount * slots.Count;
            }
            totalMotor += screwTris - Screw(0).TriangleCount; // P12 já somado uma vez
            report.AppendLine();
            report.AppendLine($"- Motor montado (P01 a P11 + {slots.Count} parafusos P12), LOD0: **{totalMotor} triângulos** (orçamento do guia: até 40 000).");
            report.AppendLine($"- Todas as peças (motor + distratoras, parafuso contado uma vez), LOD0: {total} triângulos.");
            report.AppendLine("- Valores provisórios: ver legenda [SP] e [PROJ] em W22Dimensions.cs. Cor do azul WEG ainda [VALIDAR].");

            var assembled = BuildAssembled(defs, prefabs, slots);
            BuildPreviewScene(defs, prefabs, assembled, slots);

            File.WriteAllText(ReportPath, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[WEG] Modelos W22 gerados: {defs.Count} prefabs de peça, motor montado com {totalMotor} triângulos. Relatório: {ReportPath}");
        }

        // ================================================================ definições
        static List<PartDef> PartDefs()
        {
            return new List<PartDef>
            {
                new PartDef { id = "P01", file = "P01_Rotor_Eixo", name = "Rotor com eixo", step = 1, fit = Vector3.zero, build = Rotor },
                new PartDef { id = "P02", file = "P02_Rolamento_Dianteiro_6308ZZ", name = "Rolamento dianteiro 6308-ZZ", step = 1,
                    fit = new Vector3(0, 0, D.DeBearingZ), build = lod => Bearing(lod, D.Brg6308Bore, D.Brg6308OD, D.Brg6308W, D.DeBearingZ) },
                new PartDef { id = "P03", file = "P03_Rolamento_Traseiro_6207ZZ", name = "Rolamento traseiro 6207-ZZ", step = 1,
                    fit = new Vector3(0, 0, D.NdeBearingZ), build = lod => Bearing(lod, D.Brg6207Bore, D.Brg6207OD, D.Brg6207W, D.NdeBearingZ) },
                new PartDef { id = "P04", file = "P04_Carcaca_Estator", name = "Carcaça com estator bobinado", step = 2, fit = Vector3.zero, build = Housing },
                new PartDef { id = "P05", file = "P05_Tampa_Traseira", name = "Tampa traseira", step = 3, fit = new Vector3(0, 0, D.BodyRearZ), build = RearShield },
                new PartDef { id = "P06", file = "P06_Tampa_Dianteira", name = "Tampa dianteira", step = 4, fit = new Vector3(0, 0, D.BodyFrontZ), build = FrontShield },
                new PartDef { id = "P07", file = "P07_Ventilador", name = "Ventilador", step = 5, fit = new Vector3(0, 0, D.FanZ), build = Fan },
                new PartDef { id = "P08", file = "P08_Tampa_Defletora", name = "Tampa defletora", step = 5, fit = new Vector3(0, 0, D.CoverFrontZ), build = FanCover },
                new PartDef { id = "P09", file = "P09_Caixa_Ligacao_Base", name = "Base da caixa de ligação", step = 6, fit = new Vector3(0, D.TBBottomY, D.TBZ), build = TerminalBase },
                new PartDef { id = "P10", file = "P10_Caixa_Ligacao_Tampa", name = "Tampa da caixa de ligação", step = 6, fit = new Vector3(0, D.TBSplitY, D.TBZ), build = TerminalLid },
                new PartDef { id = "P11", file = "P11_Chaveta", name = "Chaveta 10 x 8 x 63", step = 7, fit = new Vector3(0, D.KeyBottomY, D.KeyCenterZ), build = Key },
                new PartDef { id = "P12", file = "P12_Parafuso", name = "Parafuso (modelo único)", step = 3, local = true, fit = Vector3.zero, build = Screw },

                new PartDef { id = "D01", file = "D01_Engrenagem", name = "Engrenagem", distractor = true, local = true, build = Gear },
                new PartDef { id = "D02", file = "D02_Polia", name = "Polia", distractor = true, local = true, build = Pulley },
                new PartDef { id = "D03", file = "D03_Acoplamento", name = "Acoplamento", distractor = true, local = true, build = Coupling },
                new PartDef { id = "D04", file = "D04_Correia", name = "Correia", distractor = true, local = true, build = Belt },
                new PartDef { id = "D05", file = "D05_Rolamento_6205", name = "Rolamento de tamanho incorreto (6205)", distractor = true, local = true,
                    build = lod => Bearing(lod, D.Brg6205Bore, D.Brg6205OD, D.Brg6205W, 0f) },
                new PartDef { id = "D06", file = "D06_Porca", name = "Porca avulsa", distractor = true, local = true, build = Nut },
                new PartDef { id = "D07", file = "D07_Arruela", name = "Arruela avulsa", distractor = true, local = true, build = Washer },
                new PartDef { id = "D08", file = "D08_Tampa_Errada", name = "Tampa de formato diferente", distractor = true, local = true, build = WrongCover },
            };
        }

        static List<ScrewSlot> ScrewSlots()
        {
            var l = new List<ScrewSlot>();
            float frontFace = D.BodyFrontZ + D.ShieldRimT;
            float rearFace = D.BodyRearZ - D.ShieldRimT;
            for (int i = 0; i < 4; i++)
            {
                float a = Rad(45f + 90f * i);
                Vector2 p = WegMeshBuilder.Polar(D.ShieldBoltR, a);
                l.Add(new ScrewSlot { group = "TampaDianteira", pos = new Vector3(p.x, p.y, frontFace), dir = Vector3.forward });
                l.Add(new ScrewSlot { group = "TampaTraseira", pos = new Vector3(p.x, p.y, rearFace), dir = Vector3.back });
            }
            float[] coverAngles = { 30f, 150f, 270f };
            foreach (float deg in coverAngles)
            {
                Vector2 p = WegMeshBuilder.Polar(D.CoverOuterR, Rad(deg));
                l.Add(new ScrewSlot { group = "TampaDefletora", pos = new Vector3(p.x, p.y, D.BodyRearZ - 0.011f), dir = new Vector3(p.x, p.y, 0).normalized });
            }
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    l.Add(new ScrewSlot { group = "CaixaBase", pos = new Vector3(sx * 0.075f, D.TBBottomY + 0.008f, D.TBZ + sz * 0.078f), dir = Vector3.up });
                    l.Add(new ScrewSlot { group = "CaixaTampa", pos = new Vector3(sx * 0.055f, D.TBTopY - 0.004f, D.TBZ + sz * 0.058f), dir = Vector3.up });
                }
            return l;
        }

        // ================================================================ peças do motor
        static WegMeshBuilder Housing(int lod)
        {
            var b = new WegMeshBuilder();
            int seg = Seg(lod);

            // Carcaça aletada: contorno estrelado extrudado ao longo do eixo.
            var outer = new List<Vector2>();
            float aRoot = (D.FinRootThickness * 0.5f) / D.BodyCoreR;
            float aTip = (D.FinTipThickness * 0.5f) / D.BodyFinTipR;
            int slots = Mathf.RoundToInt(360f / D.FinPitchDeg);
            float half = Rad(D.FinPitchDeg * 0.5f);
            for (int k = 0; k < slots; k++)
            {
                float deg = k * D.FinPitchDeg;
                float c = Rad(deg);
                outer.Add(WegMeshBuilder.Polar(D.BodyCoreR, c - half));
                if (HasFin(deg))
                {
                    outer.Add(WegMeshBuilder.Polar(D.BodyCoreR, c - aRoot));
                    outer.Add(WegMeshBuilder.Polar(D.BodyFinTipR, c - aTip));
                    outer.Add(WegMeshBuilder.Polar(D.BodyFinTipR, c + aTip));
                    outer.Add(WegMeshBuilder.Polar(D.BodyCoreR, c + aRoot));
                }
                else
                {
                    int sub = lod == 0 ? 3 : 1;
                    for (int s = 1; s <= sub; s++) outer.Add(WegMeshBuilder.Polar(D.BodyCoreR, c - half + s * (2f * half) / (sub + 1)));
                }
            }
            var inner = WegMeshBuilder.Circle(D.BodyInnerR, seg, -half);
            b.ExtrudeRing(Mat.Paint, outer, inner, D.BodyRearZ, D.BodyFrontZ, 25f);

            // Pés maciços: sapata (AA x HA x BB) + coluna até a carcaça, dos dois lados.
            float xIn = D.AB * 0.5f - D.AA, xOut = D.AB * 0.5f;
            float yb = -D.H, yt = -D.H + D.HA;
            float pillarZ0 = D.FootCenterZ - D.FootPillarHalfLen, pillarZ1 = D.FootCenterZ + D.FootPillarHalfLen;
            for (int side = -1; side <= 1; side += 2)
            {
                b.ChamferBox(Mat.Paint, new Vector3(side * (xIn + xOut) * 0.5f, (yb + yt) * 0.5f, D.FootCenterZ), new Vector3(D.AA, D.HA, D.BB), 0.003f);

                var poly = new List<Vector2>
                {
                    new Vector2(xIn + 0.004f, yt - 0.002f),
                    new Vector2(xOut - 0.004f, yt - 0.002f),
                    new Vector2(xOut - 0.004f, -0.045f),
                };
                float rArc = D.BodyInnerR + 0.003f;
                float th0 = Mathf.Atan2(-0.045f, xOut - 0.004f);
                float th1 = -Mathf.Acos((xIn + 0.004f) / rArc);
                int arcN = lod == 0 ? 6 : 3;
                for (int i = 0; i <= arcN; i++) poly.Add(WegMeshBuilder.Polar(rArc, Mathf.Lerp(th0, th1, (float)i / arcN)));
                if (side < 0) for (int i = 0; i < poly.Count; i++) poly[i] = new Vector2(-poly[i].x, poly[i].y);
                b.ExtrudePolygon(Mat.Paint, poly, pillarZ0, pillarZ1, 20f);

                // Furos dos pés (K): marcação escura sobre a sapata.
                float[] holeZ = { D.FootHoleFrontZ, D.FootHoleRearZ };
                foreach (float hz in holeZ)
                {
                    var hole = new WegMeshBuilder();
                    hole.Disc(Mat.Dark, 0f, D.K * 0.5f, 0f, SegS(lod), true);
                    b.Append(hole, Matrix4x4.TRS(new Vector3(side * D.A * 0.5f, yt + 0.0004f, hz), Quaternion.Euler(-90f, 0, 0), Vector3.one));
                }
            }

            // Estator: núcleo de chapas e cabeças de bobina (cobre) nas duas pontas.
            b.Lathe(Mat.Steel, new[]
            {
                new Vector2(D.BodyInnerR, D.StatorFrontZ), new Vector2(D.StatorBoreR, D.StatorFrontZ),
                new Vector2(D.StatorBoreR, D.StatorRearZ), new Vector2(D.BodyInnerR, D.StatorRearZ)
            }, seg);
            var winding = WindingProfile(D.StatorFrontZ, D.StatorFrontZ + D.WindingLength);
            b.Lathe(Mat.Copper, winding, seg, 50f);
            b.Lathe(Mat.Copper, WegMeshBuilder.MirrorZ(winding, (D.StatorFrontZ + D.StatorRearZ) * 0.5f), seg, 50f);
            return b;
        }

        static bool HasFin(float deg)
        {
            deg = Mathf.Repeat(deg, 360f);
            if (Mathf.Abs(deg - 90f) < D.FinSkipTopDeg) return false;
            if (Mathf.Abs(deg - 270f) < D.FinSkipBottomDeg) return false;
            return true;
        }

        static List<Vector2> WindingProfile(float zBase, float zEnd)
        {
            float ri = D.WindingInnerR, ro = D.WindingOuterR, t = (ro - ri) * 0.3f;
            return new List<Vector2>
            {
                new Vector2(ro, zBase), new Vector2(ro, zEnd - t), new Vector2(ro - t, zEnd),
                new Vector2(ri + t, zEnd), new Vector2(ri, zEnd - t), new Vector2(ri, zBase), new Vector2(ro, zBase)
            };
        }

        static WegMeshBuilder Rotor(int lod)
        {
            var b = new WegMeshBuilder();
            int seg = lod == 0 ? 40 : 20;
            float rearSeatZ0 = D.NdeBearingZ - D.Brg6207W * 0.5f;   // -0,339
            float rearSeatZ1 = D.NdeBearingZ + D.Brg6207W * 0.5f;   // -0,322
            float deSeatZ0 = D.DeBearingZ - D.Brg6308W * 0.5f;      // -0,035
            float deSeatZ1 = D.DeBearingZ + D.Brg6308W * 0.5f;      // -0,012
            var shaft = new List<Vector2>
            {
                new Vector2(0, D.ShaftRearEndZ),
                new Vector2(D.ShaftFanR - 0.0015f, D.ShaftRearEndZ),
                new Vector2(D.ShaftFanR, D.ShaftRearEndZ + 0.0015f),
                new Vector2(D.ShaftFanR, rearSeatZ0),
                new Vector2(D.Brg6207Bore * 0.5f, rearSeatZ0),
                new Vector2(D.Brg6207Bore * 0.5f, rearSeatZ1),
                new Vector2(D.ShaftNdeR, rearSeatZ1),
                new Vector2(D.ShaftNdeR, D.StatorRearZ - 0.005f),
                new Vector2(D.ShaftBodyR, D.StatorRearZ - 0.005f),
                new Vector2(D.ShaftBodyR, deSeatZ0),
                new Vector2(D.Brg6308Bore * 0.5f, deSeatZ0),
                new Vector2(D.Brg6308Bore * 0.5f, deSeatZ1),
                new Vector2(D.ShaftShoulderR, deSeatZ1),
                new Vector2(D.ShaftShoulderR, 0f),
                new Vector2(D.D * 0.5f, 0f),
                new Vector2(D.D * 0.5f, D.E - 0.0015f),
                new Vector2(D.D * 0.5f - 0.0015f, D.E),
                new Vector2(0, D.E),
            };
            b.Lathe(Mat.Steel, shaft, seg, 30f);

            // Rasgo da chaveta (marcação escura no topo da ponta de eixo).
            b.ChamferBox(Mat.Dark, new Vector3(0, D.D * 0.5f - 0.0002f, D.KeyCenterZ), new Vector3(D.KeyF, 0.0008f, D.KeyES), 0.0003f);

            // Pacote de chapas do rotor e anéis de curto-circuito (alumínio) com aletas.
            int segR = Seg(lod);
            b.Lathe(Mat.Steel, new[]
            {
                new Vector2(D.ShaftBodyR, D.StatorRearZ), new Vector2(D.RotorR, D.StatorRearZ),
                new Vector2(D.RotorR, D.StatorFrontZ), new Vector2(D.ShaftBodyR, D.StatorFrontZ)
            }, segR);
            var ring = new List<Vector2>
            {
                new Vector2(D.RotorR - 0.0015f, D.StatorFrontZ), new Vector2(D.RotorR - 0.0015f, D.StatorFrontZ + 0.010f),
                new Vector2(D.RotorR - 0.0045f, D.StatorFrontZ + 0.013f), new Vector2(0.055f, D.StatorFrontZ + 0.013f),
                new Vector2(0.052f, D.StatorFrontZ + 0.010f), new Vector2(0.052f, D.StatorFrontZ), new Vector2(D.RotorR - 0.0015f, D.StatorFrontZ)
            };
            float mid = (D.StatorFrontZ + D.StatorRearZ) * 0.5f;
            b.Lathe(Mat.Aluminum, ring, segR, 40f);
            b.Lathe(Mat.Aluminum, WegMeshBuilder.MirrorZ(ring, mid), segR, 40f);
            int vanes = 6;
            for (int i = 0; i < vanes; i++)
            {
                float a = 360f * i / vanes;
                var rot = Quaternion.Euler(0, 0, a);
                b.ChamferBox(Mat.Aluminum, rot * new Vector3(0.064f, 0, D.StatorFrontZ + 0.018f), new Vector3(0.018f, 0.003f, 0.010f), 0.0008f, rot);
                b.ChamferBox(Mat.Aluminum, rot * new Vector3(0.064f, 0, 2f * mid - (D.StatorFrontZ + 0.018f)), new Vector3(0.018f, 0.003f, 0.010f), 0.0008f, rot);
            }
            return b;
        }

        static WegMeshBuilder Bearing(int lod, float bore, float od, float w, float zc)
        {
            var b = new WegMeshBuilder();
            int seg = lod == 0 ? 40 : 18;
            float ri = bore * 0.5f, ro = od * 0.5f, t = (ro - ri) * 0.28f, c = 0.0008f, hw = w * 0.5f;
            b.Lathe(Mat.Steel, new[]
            {
                new Vector2(ro - t, zc - hw), new Vector2(ro - c, zc - hw), new Vector2(ro, zc - hw + c),
                new Vector2(ro, zc + hw - c), new Vector2(ro - c, zc + hw), new Vector2(ro - t, zc + hw), new Vector2(ro - t, zc - hw)
            }, seg, 50f);
            b.Lathe(Mat.Steel, new[]
            {
                new Vector2(ri + c, zc - hw), new Vector2(ri + t, zc - hw), new Vector2(ri + t, zc + hw), new Vector2(ri + c, zc + hw),
                new Vector2(ri, zc + hw - c), new Vector2(ri, zc - hw + c), new Vector2(ri + c, zc - hw)
            }, seg, 50f);
            // Blindagens ZZ (metálicas) dos dois lados.
            b.Disc(Mat.Aluminum, ri + t, ro - t, zc + hw - 0.0012f, seg, true);
            b.Disc(Mat.Aluminum, ri + t, ro - t, zc - hw + 0.0012f, seg, false);
            return b;
        }

        /// <summary>Perfil da tampa em coordenada w (0 = face de encosto na carcaça, T = face externa do cubo).</summary>
        static List<Vector2> ShieldProfile(float T, float hubR, float sealR, float brgR, float brgW, float brgOff)
        {
            float rimR = D.ShieldRimR, rimT = D.ShieldRimT, backR = brgR + 0.005f;
            float coneR0 = D.ShieldBoltR - 0.010f;
            float sealDepth = Mathf.Min(0.010f, brgOff - 0.002f);
            return new List<Vector2>
            {
                new Vector2(backR, 0f), new Vector2(rimR, 0f), new Vector2(rimR, rimT - 0.002f), new Vector2(rimR - 0.002f, rimT),
                new Vector2(coneR0, rimT), new Vector2(hubR + 0.004f, T - 0.010f), new Vector2(hubR, T),
                new Vector2(sealR, T), new Vector2(sealR, T - sealDepth), new Vector2(brgR, T - brgOff),
                new Vector2(brgR, T - brgOff - brgW), new Vector2(backR, 0f)
            };
        }

        static WegMeshBuilder FrontShield(int lod)
        {
            var b = new WegMeshBuilder();
            int seg = Seg(lod);
            float T = D.FrontShieldT;
            float brgOff = T - (D.DeBearingZ + D.Brg6308W * 0.5f - D.BodyFrontZ); // distância da face externa ao rolamento
            var prof = ShieldProfile(T, D.FrontHubR, D.ShaftShoulderR + 0.002f, D.Brg6308OD * 0.5f, D.Brg6308W, brgOff);
            b.Lathe(Mat.Paint, WegMeshBuilder.MapW(prof, D.BodyFrontZ, 1f), seg, 20f);
            AddShieldRibs(b, prof, D.BodyFrontZ, 1f, D.FrontHubR);

            // Bujão de dreno automático na parte de baixo da tampa dianteira [WEG].
            var drain = new WegMeshBuilder();
            drain.Cylinder(Mat.Steel, 0.005f, 0f, 0.006f, SegS(lod), 0.0008f);
            b.Append(drain, Matrix4x4.TRS(new Vector3(0, -D.ShieldRimR + 0.001f, D.BodyFrontZ + D.ShieldRimT * 0.5f), Quaternion.Euler(90f, 0, 0), Vector3.one));
            return b;
        }

        static WegMeshBuilder RearShield(int lod)
        {
            var b = new WegMeshBuilder();
            int seg = Seg(lod);
            float T = D.RearShieldT;
            float brgOff = T - (D.BodyRearZ - (D.NdeBearingZ - D.Brg6207W * 0.5f)); // face externa ao lado de fora do 6207
            var prof = ShieldProfile(T, D.RearHubR, D.Brg6207Bore * 0.5f + 0.002f, D.Brg6207OD * 0.5f, D.Brg6207W, brgOff);
            b.Lathe(Mat.Paint, WegMeshBuilder.MapW(prof, D.BodyRearZ, -1f), seg, 20f);
            AddShieldRibs(b, prof, D.BodyRearZ, -1f, D.RearHubR);
            return b;
        }

        static void AddShieldRibs(WegMeshBuilder b, List<Vector2> wProf, float z0, float sign, float hubR)
        {
            // Nervuras radiais sobre o trecho cônico da tampa [PROJ: quantidade].
            Vector2 c0 = wProf[4], c1 = wProf[5];
            Vector2 d = c1 - c0;
            float len = d.magnitude - 0.006f;
            if (len <= 0.01f) return;
            Vector2 mid = (c0 + c1) * 0.5f;
            Vector2 nrm = new Vector2(d.y, -d.x).normalized;     // normal externa (r, w)
            var rib = WegMeshBuilder.ChamferBoxLocal(Mat.Paint, new Vector3(len, 0.005f, 0.010f), 0.0015f);
            for (int i = 0; i < D.ShieldRibs; i++)
            {
                float a = 22.5f + 360f * i / D.ShieldRibs;
                Vector2 pos = mid + nrm * 0.004f;
                float z = z0 + sign * pos.y;
                // Orienta o eixo X da nervura ao longo do cone e o Z local ao longo da normal.
                Vector3 along = new Vector3(d.x, 0, sign * d.y).normalized;
                Vector3 normal3 = new Vector3(nrm.x, 0, sign * nrm.y).normalized;
                var rotLocal = Quaternion.LookRotation(normal3, Vector3.Cross(normal3, along));
                var rotZ = Quaternion.Euler(0, 0, a);
                b.Append(rib, Matrix4x4.TRS(rotZ * new Vector3(pos.x, 0, z), rotZ * rotLocal, Vector3.one));
            }
        }

        static WegMeshBuilder Fan(int lod)
        {
            var b = new WegMeshBuilder();
            int seg = SegS(lod) + 8;
            float z0 = D.FanZ - 0.011f, z1 = D.FanZ + 0.011f;
            b.Lathe(Mat.Dark, new[]
            {
                new Vector2(D.ShaftFanR, z0), new Vector2(0.028f, z0), new Vector2(0.028f, z1),
                new Vector2(D.ShaftFanR, z1), new Vector2(D.ShaftFanR, z0)
            }, seg);
            float r0 = 0.026f, r1 = D.FanTipR;
            for (int i = 0; i < D.FanBlades; i++)
            {
                var rot = Quaternion.Euler(0, 0, 360f * i / D.FanBlades);
                b.ChamferBox(Mat.Dark, rot * new Vector3((r0 + r1) * 0.5f, 0, D.FanZ), new Vector3(r1 - r0, 0.004f, 0.018f), 0.0012f, rot);
            }
            // Disco de apoio das pás, lado da tampa traseira [PROJ].
            b.Tube(Mat.Dark, 0.026f, 0.070f, D.FanZ + 0.009f, D.FanZ + 0.011f, seg);
            return b;
        }

        static WegMeshBuilder FanCover(int lod)
        {
            var b = new WegMeshBuilder();
            int seg = Seg(lod);
            float zr = D.CoverRearZ, zf = D.CoverFrontZ, ro = D.CoverOuterR, ri = D.CoverOuterR - D.CoverWall;
            b.Lathe(Mat.Paint, new[]
            {
                new Vector2(0.110f, zr), new Vector2(0.120f, zr + 0.002f), new Vector2(0.126f, zr + 0.007f), new Vector2(ro, zr + 0.014f),
                new Vector2(ro, zf), new Vector2(ri, zf), new Vector2(ri, zr + 0.014f), new Vector2(ri - 0.002f, zr + 0.009f),
                new Vector2(0.118f, zr + 0.005f), new Vector2(0.110f, zr + 0.003f), new Vector2(0.110f, zr)
            }, seg, 40f);

            // Grade de ventilação: anéis concêntricos, raios e tampa central [PROJ: geometria].
            float[,] rings = { { 0.090f, 0.097f }, { 0.073f, 0.080f }, { 0.056f, 0.063f }, { 0.039f, 0.046f } };
            for (int i = 0; i < rings.GetLength(0); i++)
                b.Tube(Mat.Paint, rings[i, 0], rings[i, 1], zr, zr + 0.003f, seg);
            b.Cylinder(Mat.Paint, 0.025f, zr, zr + 0.003f, seg);
            for (int i = 0; i < 6; i++)
            {
                var rot = Quaternion.Euler(0, 0, 360f * i / 6f + 15f);
                b.ChamferBox(Mat.Paint, rot * new Vector3(0.0675f, 0, zr + 0.0015f), new Vector3(0.087f, 0.005f, 0.003f), 0.0008f, rot);
            }
            return b;
        }

        static WegMeshBuilder TerminalBase(int lod)
        {
            var b = new WegMeshBuilder();
            float h = D.TBSplitY - D.TBBottomY;
            b.ChamferBox(Mat.Paint, new Vector3(0, D.TBBottomY + h * 0.5f, D.TBZ), new Vector3(D.LM, h, D.LL), 0.004f);
            b.ChamferBox(Mat.Paint, new Vector3(0, D.TBBottomY + 0.004f, D.TBZ), new Vector3(D.LM + 0.030f, 0.008f, D.LL + 0.030f), 0.002f);

            // Duas entradas de cabo M32x1,5 na lateral direita [WEG: tamanho; PROJ: lado].
            int seg = SegS(lod);
            var gland = new WegMeshBuilder();
            float rHole = D.CableEntryD * 0.5f - 0.005f;
            gland.Tube(Mat.Paint, rHole, D.CableEntryD * 0.5f + 0.001f, 0f, 0.012f, seg);
            gland.Disc(Mat.Dark, 0f, rHole, 0.004f, seg, true);
            for (int i = -1; i <= 1; i += 2)
                b.Append(gland, Matrix4x4.TRS(new Vector3(D.LM * 0.5f - 0.001f, D.TBBottomY + h * 0.52f, D.TBZ + i * 0.032f), Quaternion.Euler(0, 90f, 0), Vector3.one));

            // Placa de identificação na lateral esquerda [WEG: existe; PROJ: posição e tamanho].
            float px = -D.LM * 0.5f - 0.0007f, py = D.TBBottomY + h * 0.5f;
            b.ChamferBox(Mat.Steel, new Vector3(px, py, D.TBZ), new Vector3(0.0015f, 0.045f, 0.075f), 0.0005f);
            for (int i = 0; i < 5; i++)
                b.ChamferBox(Mat.Dark, new Vector3(px - 0.0008f, py + 0.016f - i * 0.008f, D.TBZ + (i == 0 ? -0.012f : 0f)), new Vector3(0.0003f, 0.002f, i == 0 ? 0.035f : 0.060f), 0f);
            return b;
        }

        static WegMeshBuilder TerminalLid(int lod)
        {
            var b = new WegMeshBuilder();
            float top = D.TBTopY - 0.004f;
            float h = top - D.TBSplitY;
            b.ChamferBox(Mat.Paint, new Vector3(0, D.TBSplitY + h * 0.5f, D.TBZ), new Vector3(D.LM + 0.004f, h, D.LL + 0.004f), 0.006f);
            b.ChamferBox(Mat.Paint, new Vector3(0, top + 0.002f, D.TBZ), new Vector3(D.LM - 0.040f, 0.006f, D.LL - 0.040f), 0.002f);
            return b;
        }

        static WegMeshBuilder Key(int lod)
        {
            var b = new WegMeshBuilder();
            b.ChamferBox(Mat.Steel, new Vector3(0, D.KeyBottomY + D.KeyGD * 0.5f, D.KeyCenterZ), new Vector3(D.KeyF, D.KeyGD, D.KeyES), 0.0008f);
            return b;
        }

        /// <summary>Parafuso M8 de cabeça sextavada. Pivô no assento da cabeça; +Y local aponta para fora da superfície.</summary>
        static WegMeshBuilder Screw(int lod)
        {
            var local = new WegMeshBuilder();
            float rc = D.ScrewHeadAF / Mathf.Sqrt(3f);
            var hex = new List<Vector2>();
            for (int i = 0; i < 6; i++) hex.Add(WegMeshBuilder.Polar(rc, Rad(30f + 60f * i)));
            local.ExtrudeRing(Mat.Steel, hex, null, 0f, D.ScrewHeadH, 5f);
            local.Lathe(Mat.Steel, new[]
            {
                new Vector2(0, -D.ScrewShankL), new Vector2(D.ScrewShankR - 0.0006f, -D.ScrewShankL),
                new Vector2(D.ScrewShankR, -D.ScrewShankL + 0.0006f), new Vector2(D.ScrewShankR, 0.0005f)
            }, lod == 0 ? 12 : 8);
            var b = new WegMeshBuilder();
            b.Append(local, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90f, 0, 0), Vector3.one));
            return b;
        }

        // ================================================================ distratoras
        static WegMeshBuilder Gear(int lod)
        {
            var b = new WegMeshBuilder();
            int teeth = 24;
            float rRoot = 0.050f, rTip = 0.059f, pitch = Mathf.PI * 2f / teeth;
            var outer = new List<Vector2>();
            for (int k = 0; k < teeth; k++)
            {
                float c = k * pitch;
                outer.Add(WegMeshBuilder.Polar(rRoot, c - pitch * 0.5f));
                outer.Add(WegMeshBuilder.Polar(rRoot, c - pitch * 0.26f));
                outer.Add(WegMeshBuilder.Polar(rTip, c - pitch * 0.14f));
                outer.Add(WegMeshBuilder.Polar(rTip, c + pitch * 0.14f));
                outer.Add(WegMeshBuilder.Polar(rRoot, c + pitch * 0.26f));
            }
            b.ExtrudeRing(Mat.Steel, outer, WegMeshBuilder.Circle(0.022f, Seg(lod) / 2, -pitch * 0.5f), -0.0125f, 0.0125f, 15f);
            b.Tube(Mat.Steel, 0.012f, 0.022f, -0.020f, 0.020f, SegS(lod) + 8);
            return b;
        }

        static WegMeshBuilder Pulley(int lod)
        {
            var b = new WegMeshBuilder();
            b.Lathe(Mat.Steel, new[]
            {
                new Vector2(0.012f, -0.020f), new Vector2(0.075f, -0.020f), new Vector2(0.075f, -0.016f), new Vector2(0.062f, -0.010f),
                new Vector2(0.075f, -0.004f), new Vector2(0.075f, 0.004f), new Vector2(0.062f, 0.010f), new Vector2(0.075f, 0.016f),
                new Vector2(0.075f, 0.020f), new Vector2(0.012f, 0.020f), new Vector2(0.012f, -0.020f)
            }, Seg(lod), 10f);
            return b;
        }

        static WegMeshBuilder Coupling(int lod)
        {
            var b = new WegMeshBuilder();
            b.Lathe(Mat.Aluminum, new[]
            {
                new Vector2(0.012f, -0.030f), new Vector2(0.030f, -0.030f), new Vector2(0.030f, 0f), new Vector2(0.045f, 0f),
                new Vector2(0.045f, 0.012f), new Vector2(0.012f, 0.012f), new Vector2(0.012f, -0.030f)
            }, Seg(lod) / 2 + 8, 10f);
            for (int i = 0; i < 3; i++)
            {
                var rot = Quaternion.Euler(0, 0, 120f * i);
                b.ChamferBox(Mat.Aluminum, rot * new Vector3(0.031f, 0, 0.021f), new Vector3(0.022f, 0.020f, 0.018f), 0.002f, rot);
            }
            return b;
        }

        static WegMeshBuilder Belt(int lod)
        {
            var ring = new WegMeshBuilder();
            ring.Lathe(Mat.Dark, new[]
            {
                new Vector2(0.100f, -0.0045f), new Vector2(0.110f, -0.0065f), new Vector2(0.110f, 0.0065f),
                new Vector2(0.100f, 0.0045f), new Vector2(0.100f, -0.0045f)
            }, Seg(lod), 10f);
            var b = new WegMeshBuilder();
            b.Append(ring, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-90f, 0, 0), Vector3.one));
            return b;
        }

        static WegMeshBuilder Nut(int lod)
        {
            var b = new WegMeshBuilder();
            float rc = 0.024f / Mathf.Sqrt(3f);
            var hex = new List<Vector2>();
            for (int i = 0; i < 6; i++) hex.Add(WegMeshBuilder.Polar(rc, Rad(60f * i)));
            b.ExtrudeRing(Mat.Steel, hex, WegMeshBuilder.Circle(0.008f, SegS(lod), 0f), -0.0065f, 0.0065f, 5f);
            return b;
        }

        static WegMeshBuilder Washer(int lod)
        {
            var b = new WegMeshBuilder();
            b.Tube(Mat.Steel, 0.0085f, 0.015f, -0.0015f, 0.0015f, SegS(lod) + 8);
            return b;
        }

        static WegMeshBuilder WrongCover(int lod)
        {
            var b = new WegMeshBuilder();
            b.ChamferBox(Mat.Paint, Vector3.zero, new Vector3(0.200f, 0.200f, 0.014f), 0.004f);
            b.Tube(Mat.Paint, 0.022f, 0.040f, 0.007f, 0.030f, SegS(lod) + 8);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    var hole = new WegMeshBuilder();
                    hole.Disc(Mat.Dark, 0f, 0.006f, 0.0075f, SegS(lod), true);
                    b.Append(hole, Matrix4x4.Translate(new Vector3(sx * 0.080f, sy * 0.080f, 0)));
                }
            return b;
        }

        // ================================================================ assets
        static Material[] EnsureMaterials()
        {
            var defs = new (string name, Color color, float metallic, float smooth)[]
            {
                // Azul WEG: tom provisório [VALIDAR]. Acabamento semibrilho [WEG: plano 203A].
                ("WEG_Paint_Blue", new Color(0.10f, 0.27f, 0.55f), 0.10f, 0.55f),
                ("WEG_Steel", new Color(0.70f, 0.71f, 0.73f), 0.90f, 0.60f),
                ("WEG_Copper", new Color(0.78f, 0.42f, 0.22f), 0.90f, 0.55f),
                ("WEG_Dark", new Color(0.07f, 0.07f, 0.08f), 0.00f, 0.35f),
                ("WEG_Aluminum", new Color(0.84f, 0.85f, 0.87f), 0.80f, 0.45f),
            };
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");
            var mats = new Material[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                string path = $"{MatDir}/{defs[i].name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(lit);
                    AssetDatabase.CreateAsset(mat, path);
                }
                mat.shader = lit;
                mat.SetColor("_BaseColor", defs[i].color);
                mat.SetColor("_Color", defs[i].color);
                mat.SetFloat("_Metallic", defs[i].metallic);
                mat.SetFloat("_Smoothness", defs[i].smooth);
                mat.SetFloat("_Glossiness", defs[i].smooth);
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                mats[i] = mat;
            }

            // Material das silhuetas brancas da bancada (RF03).
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit != null)
            {
                string sPath = $"{MatDir}/WEG_Silhouette_White.mat";
                var s = AssetDatabase.LoadAssetAtPath<Material>(sPath);
                if (s == null) { s = new Material(unlit); AssetDatabase.CreateAsset(s, sPath); }
                s.shader = unlit;
                s.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(s);
            }
            return mats;
        }

        static Mesh SaveMesh(Mesh m, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(m, path);
                return m;
            }
            existing.Clear();
            EditorUtility.CopySerialized(m, existing);
            Object.DestroyImmediate(m);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        static readonly string[] MatShortNames = { "Pintura", "Aço", "Cobre", "Escuro", "Alumínio" };

        static GameObject BuildPartPrefab(PartDef def, Material[] mats, out int tri0, out int tri1, out string matNames, out Vector3 size)
        {
            tri0 = tri1 = 0;
            matNames = "";
            size = Vector3.zero;
            var root = new GameObject(def.file);
            var info = root.AddComponent<W22PartInfo>();
            info.partId = def.id;
            info.displayName = def.name;
            info.step = def.distractor ? 0 : def.step;
            info.isDistractor = def.distractor;
            info.assembledPosition = def.fit;

            var lods = new LOD[2];
            Bounds b0 = new Bounds();
            for (int lod = 0; lod < 2; lod++)
            {
                var bld = def.build(lod);
                if (!def.local) bld.Translate(-def.fit);
                int[] used;
                var mesh = SaveMesh(bld.ToMesh($"{def.file}_LOD{lod}", out used), $"{MeshDir}/{def.file}_LOD{lod}.asset");
                var child = new GameObject($"LOD{lod}");
                child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = child.AddComponent<MeshRenderer>();
                var arr = new Material[used.Length];
                for (int i = 0; i < used.Length; i++) arr[i] = mats[used[i]];
                mr.sharedMaterials = arr;
                lods[lod] = new LOD(lod == 0 ? 0.12f : 0.004f, new Renderer[] { mr });
                if (lod == 0)
                {
                    tri0 = bld.TriangleCount;
                    b0 = mesh.bounds;
                    var names = new List<string>();
                    foreach (int u in used) names.Add(MatShortNames[u]);
                    matNames = string.Join(", ", names);
                }
                else tri1 = bld.TriangleCount;
            }
            var group = root.AddComponent<LODGroup>();
            group.SetLODs(lods);
            group.RecalculateBounds();

            var bc = root.AddComponent<BoxCollider>();
            bc.center = b0.center;
            bc.size = Vector3.Max(b0.size, Vector3.one * 0.004f);
            size = b0.size;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PartsDir}/{def.file}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject BuildAssembled(List<PartDef> defs, Dictionary<string, GameObject> prefabs, List<ScrewSlot> slots)
        {
            var root = new GameObject("WEG_W22_132M_Montado");
            var fits = new GameObject("Encaixes");
            fits.transform.SetParent(root.transform, false);
            foreach (var def in defs)
            {
                if (def.distractor || def.id == "P12") continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[def.id]);
                inst.transform.SetParent(root.transform, false);
                inst.transform.localPosition = def.fit;
                var marker = new GameObject($"Encaixe_{def.id}");
                marker.transform.SetParent(fits.transform, false);
                marker.transform.localPosition = def.fit;
            }
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                var rot = Quaternion.FromToRotation(Vector3.up, s.dir);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabs["P12"]);
                inst.name = $"P12_Parafuso_{s.group}_{i:00}";
                inst.transform.SetParent(root.transform, false);
                inst.transform.localPosition = s.pos;
                inst.transform.localRotation = rot;
                var marker = new GameObject($"Encaixe_P12_{s.group}_{i:00}");
                marker.transform.SetParent(fits.transform, false);
                marker.transform.localPosition = s.pos;
                marker.transform.localRotation = rot;
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/WEG_W22_132M_Montado.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void BuildPreviewScene(List<PartDef> defs, Dictionary<string, GameObject> prefabs, GameObject assembled, List<ScrewSlot> slots)
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.3f;
                light.shadows = LightShadows.Soft;
                lightGo.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.nearClipPlane = 0.01f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.82f, 0.84f, 0.87f);
                camGo.transform.position = new Vector3(0.55f, 1.45f, 1.55f);
                camGo.transform.LookAt(new Vector3(0.55f, 1.0f, 0f));

                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Piso";
                floor.transform.position = new Vector3(0.5f, 1.0f - D.H, 0f);
                floor.transform.localScale = new Vector3(0.4f, 1f, 0.3f);

                // Motor montado.
                var asm = (GameObject)PrefabUtility.InstantiatePrefab(assembled);
                asm.transform.position = new Vector3(0f, 1.0f, 0f);
                asm.transform.rotation = Quaternion.Euler(0, 35f, 0);

                // Vista explodida ao longo do eixo.
                var exploded = new GameObject("W22_Explodido");
                exploded.transform.position = new Vector3(1.0f, 1.0f, 0f);
                exploded.transform.rotation = Quaternion.Euler(0, 90f, 0);
                var offsets = new Dictionary<string, Vector3>
                {
                    { "P04", Vector3.zero },
                    { "P06", new Vector3(0, 0, 0.16f) },
                    { "P01", new Vector3(0, 0, 0.70f) }, { "P02", new Vector3(0, 0, 0.80f) }, { "P03", new Vector3(0, 0, 0.60f) },
                    { "P11", new Vector3(0, 0.06f, 0.70f) },
                    { "P05", new Vector3(0, 0, -0.12f) }, { "P07", new Vector3(0, 0, -0.22f) }, { "P08", new Vector3(0, 0, -0.32f) },
                    { "P09", new Vector3(0, 0.10f, 0) }, { "P10", new Vector3(0, 0.20f, 0) },
                };
                foreach (var def in defs)
                {
                    Vector3 off;
                    if (!offsets.TryGetValue(def.id, out off)) continue;
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[def.id]);
                    inst.transform.SetParent(exploded.transform, false);
                    inst.transform.localPosition = def.fit + off;
                }

                // Distratoras em fila, à frente.
                var row = new GameObject("Distratoras");
                int k = 0;
                foreach (var def in defs)
                {
                    if (!def.distractor && def.id != "P12") continue;
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[def.id]);
                    inst.transform.SetParent(row.transform, false);
                    inst.transform.position = new Vector3(-0.15f + k * 0.17f, 1.0f - D.H + 0.08f, 0.45f);
                    k++;
                }

                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
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
