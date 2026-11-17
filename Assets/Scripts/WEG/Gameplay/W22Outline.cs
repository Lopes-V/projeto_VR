using UnityEngine;

namespace WEG.W22
{
    public enum W22PartState { None, Correct, WrongOrder, WrongPlace }

    /// <summary>Borda colorida da peça (seção 6): verde, amarela ou vermelha.</summary>
    public class W22Outline : MonoBehaviour
    {
        public Material outlineMaterial;

        static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        public static readonly Color Green = new Color(0.15f, 0.9f, 0.3f, 1f);
        public static readonly Color Yellow = new Color(1f, 0.82f, 0.1f, 1f);
        public static readonly Color Red = new Color(0.95f, 0.15f, 0.1f, 1f);

        MeshRenderer outlineRenderer;
        MaterialPropertyBlock block;

        void Awake()
        {
            if (outlineMaterial == null) return;
            // Usa a malha do LOD0 da peça.
            MeshFilter source = null;
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
                if (mf.gameObject.name == "LOD0") { source = mf; break; }
            if (source == null) return;
            var go = new GameObject("Borda");
            go.transform.SetParent(source.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            outlineRenderer = go.AddComponent<MeshRenderer>();
            var mats = new Material[source.sharedMesh.subMeshCount];
            for (int i = 0; i < mats.Length; i++) mats[i] = outlineMaterial;
            outlineRenderer.sharedMaterials = mats;
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            block = new MaterialPropertyBlock();
            outlineRenderer.enabled = false;
        }

        public void Show(W22PartState state)
        {
            if (outlineRenderer == null) return;
            if (state == W22PartState.None) { outlineRenderer.enabled = false; return; }
            var c = state == W22PartState.Correct ? Green : state == W22PartState.WrongOrder ? Yellow : Red;
            block.SetColor(ColorId, c);
            outlineRenderer.SetPropertyBlock(block);
            outlineRenderer.enabled = true;
        }
    }
}
