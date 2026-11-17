using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WEG.W22.EditorTools
{
    /// <summary>Índices de material (submeshes) usados por todas as peças.</summary>
    public static class Mat
    {
        public const int Paint = 0, Steel = 1, Copper = 2, Dark = 3, Aluminum = 4, Count = 5;
    }

    /// <summary>
    /// Construtor de malha procedural low poly: revolução (lathe), extrusão de contornos,
    /// caixas chanfradas. A orientação dos triângulos é corrigida automaticamente
    /// a partir das normais pedidas, então a ordem dos vértices não importa.
    /// </summary>
    public class WegMeshBuilder
    {
        public readonly List<Vector3> V = new List<Vector3>();
        public readonly List<Vector3> N = new List<Vector3>();
        public readonly List<Vector2> UV = new List<Vector2>();
        public readonly List<int>[] Tris = new List<int>[Mat.Count];

        public WegMeshBuilder()
        {
            for (int i = 0; i < Mat.Count; i++) Tris[i] = new List<int>();
        }

        public int TriangleCount
        {
            get { int c = 0; foreach (var t in Tris) c += t.Count; return c / 3; }
        }

        public int Vert(Vector3 p, Vector3 n, Vector2 uv)
        {
            V.Add(p);
            N.Add(n.sqrMagnitude > 1e-12f ? n.normalized : Vector3.up);
            UV.Add(uv);
            return V.Count - 1;
        }

        public void Tri(int m, int a, int b, int c)
        {
            Vector3 face = Vector3.Cross(V[b] - V[a], V[c] - V[a]);
            if (face.sqrMagnitude < 1e-18f) return;
            Vector3 want = N[a] + N[b] + N[c];
            var t = Tris[m];
            if (Vector3.Dot(face, want) >= 0f) { t.Add(a); t.Add(b); t.Add(c); }
            else { t.Add(a); t.Add(c); t.Add(b); }
        }

        public void Quad(int m, int a, int b, int c, int d)
        {
            Tri(m, a, b, c);
            Tri(m, a, c, d);
        }

        static Vector2 PlanarUV(Vector3 p) => new Vector2(p.x + p.z, p.y + p.z) * 4f;

        // ---------------------------------------------------------------- revolução
        /// <summary>
        /// Revolve um perfil (x = raio, y = z) em torno do eixo Z. O perfil deve ser percorrido
        /// no sentido anti-horário no plano (raio, z): a normal externa de cada segmento é (dz, -dr).
        /// </summary>
        public void Lathe(int m, IList<Vector2> prof, int seg, float smoothDeg = 30f)
        {
            int n = prof.Count;
            if (n < 2) return;
            var segN = new Vector2[n - 1];
            for (int i = 0; i < n - 1; i++)
            {
                Vector2 d = prof[i + 1] - prof[i];
                segN[i] = d.sqrMagnitude < 1e-14f ? Vector2.zero : new Vector2(d.y, -d.x).normalized;
            }
            float cosLim = Mathf.Cos(smoothDeg * Mathf.Deg2Rad);
            for (int i = 0; i < n - 1; i++)
            {
                if (segN[i] == Vector2.zero) continue;
                Vector2 nA = segN[i], nB = segN[i];
                if (i > 0 && segN[i - 1] != Vector2.zero && Vector2.Dot(segN[i - 1], segN[i]) > cosLim)
                    nA = (segN[i - 1] + segN[i]).normalized;
                if (i < n - 2 && segN[i + 1] != Vector2.zero && Vector2.Dot(segN[i + 1], segN[i]) > cosLim)
                    nB = (segN[i + 1] + segN[i]).normalized;
                Vector2 pA = prof[i], pB = prof[i + 1];
                int baseIdx = V.Count;
                for (int s = 0; s <= seg; s++)
                {
                    float a = s * Mathf.PI * 2f / seg;
                    float c = Mathf.Cos(a), sn = Mathf.Sin(a);
                    Vert(new Vector3(pA.x * c, pA.x * sn, pA.y), new Vector3(nA.x * c, nA.x * sn, nA.y),
                        new Vector2((float)s / seg, pA.y * 4f));
                    Vert(new Vector3(pB.x * c, pB.x * sn, pB.y), new Vector3(nB.x * c, nB.x * sn, nB.y),
                        new Vector2((float)s / seg, pB.y * 4f));
                }
                for (int s = 0; s < seg; s++)
                {
                    int a0 = baseIdx + s * 2, b0 = a0 + 1, a1 = a0 + 2, b1 = a0 + 3;
                    if (pA.x < 1e-6f) Tri(m, a0, b1, b0);
                    else if (pB.x < 1e-6f) Tri(m, a0, a1, b0);
                    else Quad(m, a0, a1, b1, b0);
                }
            }
        }

        /// <summary>Espelha um perfil de revolução no plano z = zMirror, mantendo o sentido anti-horário.</summary>
        public static List<Vector2> MirrorZ(IList<Vector2> prof, float zMirror)
        {
            var r = new List<Vector2>(prof.Count);
            for (int i = prof.Count - 1; i >= 0; i--) r.Add(new Vector2(prof[i].x, 2f * zMirror - prof[i].y));
            return r;
        }

        /// <summary>Converte um perfil em coordenada local w para z = z0 + sign * w (mantém o sentido anti-horário).</summary>
        public static List<Vector2> MapW(IList<Vector2> prof, float z0, float sign)
        {
            var r = new List<Vector2>(prof.Count);
            if (sign > 0) { foreach (var p in prof) r.Add(new Vector2(p.x, z0 + p.y)); }
            else { for (int i = prof.Count - 1; i >= 0; i--) r.Add(new Vector2(prof[i].x, z0 - prof[i].y)); }
            return r;
        }

        /// <summary>Cilindro fechado ao longo de Z.</summary>
        public void Cylinder(int m, float r, float z0, float z1, int seg, float chamfer = 0f)
        {
            if (chamfer <= 0f)
            {
                Lathe(m, new[] { new Vector2(0, z0), new Vector2(r, z0), new Vector2(r, z1), new Vector2(0, z1) }, seg);
                return;
            }
            Lathe(m, new[]
            {
                new Vector2(0, z0), new Vector2(r - chamfer, z0), new Vector2(r, z0 + chamfer),
                new Vector2(r, z1 - chamfer), new Vector2(r - chamfer, z1), new Vector2(0, z1)
            }, seg);
        }

        /// <summary>Anel (tubo) fechado ao longo de Z.</summary>
        public void Tube(int m, float rIn, float rOut, float z0, float z1, int seg)
        {
            Lathe(m, new[]
            {
                new Vector2(rIn, z0), new Vector2(rOut, z0), new Vector2(rOut, z1),
                new Vector2(rIn, z1), new Vector2(rIn, z0)
            }, seg);
        }

        /// <summary>Disco plano em z, voltado para +Z (facingPlus) ou -Z.</summary>
        public void Disc(int m, float rIn, float rOut, float z, int seg, bool facingPlus)
        {
            if (facingPlus) Lathe(m, new[] { new Vector2(rOut, z), new Vector2(rIn, z) }, seg);
            else Lathe(m, new[] { new Vector2(rIn, z), new Vector2(rOut, z) }, seg);
        }

        // ---------------------------------------------------------------- extrusão
        public static Vector2 Polar(float r, float a) => new Vector2(r * Mathf.Cos(a), r * Mathf.Sin(a));

        public static List<Vector2> Circle(float r, int seg, float startRad)
        {
            var l = new List<Vector2>(seg);
            for (int i = 0; i < seg; i++) l.Add(Polar(r, startRad + i * Mathf.PI * 2f / seg));
            return l;
        }

        /// <summary>
        /// Extruda ao longo de Z um contorno estrelado (ordenado por ângulo, anti-horário) com furo
        /// circular opcional. Usado na carcaça aletada, engrenagem e porca.
        /// </summary>
        public void ExtrudeRing(int m, List<Vector2> outer, List<Vector2> inner, float z0, float z1, float smoothDeg)
        {
            Wall(m, outer, z0, z1, smoothDeg, true);
            if (inner != null) Wall(m, inner, z0, z1, smoothDeg, false);
            RingCap(m, outer, inner, z0, -1f);
            RingCap(m, outer, inner, z1, 1f);
        }

        /// <summary>Extruda ao longo de Z um polígono simples qualquer (côncavo permitido).</summary>
        public void ExtrudePolygon(int m, List<Vector2> poly, float z0, float z1, float smoothDeg)
        {
            var p = new List<Vector2>(poly);
            if (SignedArea(p) < 0f) p.Reverse();
            Wall(m, p, z0, z1, smoothDeg, true);
            var tris = EarClip(p);
            for (int s = 0; s < 2; s++)
            {
                float z = s == 0 ? z0 : z1;
                var nrm = new Vector3(0, 0, s == 0 ? -1f : 1f);
                int b = V.Count;
                foreach (var q in p) Vert(new Vector3(q.x, q.y, z), nrm, q * 4f);
                for (int t = 0; t + 2 < tris.Count; t += 3) Tri(m, b + tris[t], b + tris[t + 1], b + tris[t + 2]);
            }
        }

        void Wall(int m, List<Vector2> loop, float z0, float z1, float smoothDeg, bool outward)
        {
            int n = loop.Count;
            var en = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 d = loop[(i + 1) % n] - loop[i];
                Vector2 r = new Vector2(d.y, -d.x);
                if (!outward) r = -r;
                en[i] = r.sqrMagnitude < 1e-14f ? Vector2.zero : r.normalized;
            }
            float cosLim = Mathf.Cos(smoothDeg * Mathf.Deg2Rad);
            float perim = 0f;
            for (int i = 0; i < n; i++)
            {
                if (en[i] == Vector2.zero) continue;
                int ip = (i - 1 + n) % n, inx = (i + 1) % n;
                Vector2 nA = en[i], nB = en[i];
                if (en[ip] != Vector2.zero && Vector2.Dot(en[ip], en[i]) > cosLim) nA = (en[ip] + en[i]).normalized;
                if (en[inx] != Vector2.zero && Vector2.Dot(en[inx], en[i]) > cosLim) nB = (en[inx] + en[i]).normalized;
                Vector2 a = loop[i], b = loop[inx];
                float len = (b - a).magnitude;
                Vector3 na = new Vector3(nA.x, nA.y, 0), nb = new Vector3(nB.x, nB.y, 0);
                int i0 = Vert(new Vector3(a.x, a.y, z0), na, new Vector2(perim * 4f, z0 * 4f));
                int i1 = Vert(new Vector3(b.x, b.y, z0), nb, new Vector2((perim + len) * 4f, z0 * 4f));
                int i2 = Vert(new Vector3(b.x, b.y, z1), nb, new Vector2((perim + len) * 4f, z1 * 4f));
                int i3 = Vert(new Vector3(a.x, a.y, z1), na, new Vector2(perim * 4f, z1 * 4f));
                Quad(m, i0, i1, i2, i3);
                perim += len;
            }
        }

        void RingCap(int m, List<Vector2> outer, List<Vector2> inner, float z, float sign)
        {
            var nrm = new Vector3(0, 0, sign);
            int ob = V.Count;
            foreach (var p in outer) Vert(new Vector3(p.x, p.y, z), nrm, p * 4f);
            int no = outer.Count;
            if (inner == null)
            {
                int c = Vert(new Vector3(0, 0, z), nrm, Vector2.zero);
                for (int i = 0; i < no; i++) Tri(m, c, ob + i, ob + (i + 1) % no);
                return;
            }
            int ib = V.Count;
            foreach (var p in inner) Vert(new Vector3(p.x, p.y, z), nrm, p * 4f);
            int ni = inner.Count;
            float[] ao = Unwrap(outer), ai = Unwrap(inner);
            int io = 0, ii = 0, guard = 0;
            while ((io < no || ii < ni) && guard++ < 100000)
            {
                float nextO = io < no ? ao[io + 1] : float.MaxValue;
                float nextI = ii < ni ? ai[ii + 1] : float.MaxValue;
                if (nextO <= nextI) { Tri(m, ob + io % no, ib + ii % ni, ob + (io + 1) % no); io++; }
                else { Tri(m, ob + io % no, ib + ii % ni, ib + (ii + 1) % ni); ii++; }
            }
        }

        static float[] Unwrap(List<Vector2> loop)
        {
            int n = loop.Count;
            var a = new float[n + 1];
            float prev = Mathf.Atan2(loop[0].y, loop[0].x);
            a[0] = prev;
            for (int k = 1; k <= n; k++)
            {
                var p = loop[k % n];
                float ang = Mathf.Atan2(p.y, p.x);
                while (ang <= prev) ang += Mathf.PI * 2f;
                a[k] = ang;
                prev = ang;
            }
            return a;
        }

        static float SignedArea(List<Vector2> p)
        {
            float s = 0f;
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 a = p[i], b = p[(i + 1) % p.Count];
                s += a.x * b.y - b.x * a.y;
            }
            return s * 0.5f;
        }

        static float Cross2(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        static bool PointInTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross2(b - a, p - a), d2 = Cross2(c - b, p - b), d3 = Cross2(a - c, p - c);
            return d1 > 0f && d2 > 0f && d3 > 0f;
        }

        static List<int> EarClip(List<Vector2> p)
        {
            var res = new List<int>();
            var idx = new List<int>();
            for (int i = 0; i < p.Count; i++) idx.Add(i);
            int guard = 0;
            while (idx.Count > 3 && guard++ < 10000)
            {
                bool clipped = false;
                for (int k = 0; k < idx.Count; k++)
                {
                    int i0 = idx[(k - 1 + idx.Count) % idx.Count], i1 = idx[k], i2 = idx[(k + 1) % idx.Count];
                    Vector2 a = p[i0], b = p[i1], c = p[i2];
                    if (Cross2(b - a, c - b) <= 1e-12f) continue;
                    bool inside = false;
                    for (int q = 0; q < idx.Count; q++)
                    {
                        int iq = idx[q];
                        if (iq == i0 || iq == i1 || iq == i2) continue;
                        if (PointInTri(p[iq], a, b, c)) { inside = true; break; }
                    }
                    if (inside) continue;
                    res.Add(i0); res.Add(i1); res.Add(i2);
                    idx.RemoveAt(k);
                    clipped = true;
                    break;
                }
                if (!clipped) break;
            }
            if (idx.Count == 3) { res.Add(idx[0]); res.Add(idx[1]); res.Add(idx[2]); }
            return res;
        }

        // ---------------------------------------------------------------- caixa chanfrada
        static Vector3[] Oct(float ax, float az, float k, float y)
        {
            return new[]
            {
                new Vector3(ax - k, y, az), new Vector3(-(ax - k), y, az), new Vector3(-ax, y, az - k), new Vector3(-ax, y, -(az - k)),
                new Vector3(-(ax - k), y, -az), new Vector3(ax - k, y, -az), new Vector3(ax, y, -(az - k)), new Vector3(ax, y, az - k)
            };
        }

        void AddFlatQuadOutward(int m, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 n = Vector3.Cross(b - a, d - a);
            if (n.sqrMagnitude < 1e-18f) n = Vector3.Cross(c - b, a - b);
            Vector3 ctr = (a + b + c + d) * 0.25f;
            if (Vector3.Dot(n, ctr) < 0f) n = -n;
            int i0 = Vert(a, n, PlanarUV(a)), i1 = Vert(b, n, PlanarUV(b)), i2 = Vert(c, n, PlanarUV(c)), i3 = Vert(d, n, PlanarUV(d));
            Quad(m, i0, i1, i2, i3);
        }

        /// <summary>Caixa com todas as arestas chanfradas, centrada na origem local.</summary>
        public static WegMeshBuilder ChamferBoxLocal(int m, Vector3 size, float ch)
        {
            var b = new WegMeshBuilder();
            float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;
            float c = Mathf.Min(ch, 0.45f * Mathf.Min(hx, Mathf.Min(hy, hz)));
            float k = c * 0.6f;
            var loops = new List<Vector3[]>();
            if (c > 1e-5f)
            {
                loops.Add(Oct(hx - c, hz - c, k, -hy));
                loops.Add(Oct(hx, hz, c, -hy + c));
                loops.Add(Oct(hx, hz, c, hy - c));
                loops.Add(Oct(hx - c, hz - c, k, hy));
            }
            else
            {
                loops.Add(Oct(hx, hz, 0f, -hy));
                loops.Add(Oct(hx, hz, 0f, hy));
            }
            for (int L = 0; L < loops.Count - 1; L++)
            {
                var la = loops[L];
                var lb = loops[L + 1];
                for (int i = 0; i < 8; i++)
                {
                    int j = (i + 1) % 8;
                    if ((la[i] - la[j]).sqrMagnitude < 1e-14f && (lb[i] - lb[j]).sqrMagnitude < 1e-14f) continue;
                    b.AddFlatQuadOutward(m, la[i], la[j], lb[j], lb[i]);
                }
            }
            for (int s = 0; s < 2; s++)
            {
                var loop = s == 0 ? loops[0] : loops[loops.Count - 1];
                var n = new Vector3(0, s == 0 ? -1f : 1f, 0);
                int ctr = b.Vert(new Vector3(0, loop[0].y, 0), n, Vector2.zero);
                int first = b.V.Count;
                foreach (var p in loop) b.Vert(p, n, PlanarUV(p));
                for (int i = 0; i < 8; i++) b.Tri(m, ctr, first + i, first + (i + 1) % 8);
            }
            return b;
        }

        public void ChamferBox(int m, Vector3 center, Vector3 size, float ch, Quaternion rot)
        {
            Append(ChamferBoxLocal(m, size, ch), Matrix4x4.TRS(center, rot, Vector3.one));
        }

        public void ChamferBox(int m, Vector3 center, Vector3 size, float ch)
        {
            ChamferBox(m, center, size, ch, Quaternion.identity);
        }

        // ---------------------------------------------------------------- utilidades
        /// <summary>Anexa outra malha com transformação rígida (sem escala negativa).</summary>
        public void Append(WegMeshBuilder o, Matrix4x4 mtx)
        {
            int b = V.Count;
            for (int i = 0; i < o.V.Count; i++)
            {
                V.Add(mtx.MultiplyPoint3x4(o.V[i]));
                N.Add(mtx.MultiplyVector(o.N[i]).normalized);
                UV.Add(o.UV[i]);
            }
            for (int m = 0; m < Mat.Count; m++)
                foreach (int t in o.Tris[m]) Tris[m].Add(b + t);
        }

        public void Translate(Vector3 off)
        {
            for (int i = 0; i < V.Count; i++) V[i] += off;
        }

        public Mesh ToMesh(string name, out int[] usedMats)
        {
            var mesh = new Mesh { name = name };
            if (V.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(V);
            mesh.SetNormals(N);
            mesh.SetUVs(0, UV);
            var used = new List<int>();
            for (int m = 0; m < Mat.Count; m++) if (Tris[m].Count > 0) used.Add(m);
            mesh.subMeshCount = used.Count;
            for (int s = 0; s < used.Count; s++) mesh.SetTriangles(Tris[used[s]], s, true);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            usedMats = used.ToArray();
            return mesh;
        }
    }
}
