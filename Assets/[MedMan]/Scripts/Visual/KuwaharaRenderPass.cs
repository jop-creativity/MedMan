using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace MedMan.Visual
{
    /// <summary>
    /// 4-pass Anisotropic Kuwahara filter implementation for Unity 6.3 LTS URP.
    /// Pass 0: Structure Tensor (Sobel gradients)
    /// Pass 1: Gaussian Blur Horizontal (smooth tensor)
    /// Pass 2: Gaussian Blur Vertical + TFM (edge orientation map)
    /// Pass 3: Anisotropic Kuwahara (oil-paint effect using TFM)
    /// </summary>
    public class KuwaharaRenderPass : ScriptableRenderPass
    {
        // ─────────────────────────────────────────
        // PassData
        // ─────────────────────────────────────────

        private class PassData
        {
            public TextureHandle source;
            public TextureHandle tfm;
            public Material      material;
            public int           pass;
            public float         kernelSize;
            public float         sharpness;
            public float         hardness;
            public float         zeroCrossing;
            public float         zeta;
            public float         intensity;
        }

        // ─────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────

        private Material _material;
        private float    _kernelSize;
        private float    _sharpness;
        private float    _hardness;
        private float    _zeroCrossing;
        private float    _zeta;
        private float    _intensity;

        private static readonly int TFMTexID        = Shader.PropertyToID("_TFM");
        private static readonly int KernelSizeID    = Shader.PropertyToID("_KernelSize");
        private static readonly int SharpnessID     = Shader.PropertyToID("_Sharpness");
        private static readonly int HardnessID      = Shader.PropertyToID("_Hardness");
        private static readonly int ZeroCrossingID  = Shader.PropertyToID("_ZeroCrossing");
        private static readonly int ZetaID          = Shader.PropertyToID("_Zeta");
        private static readonly int IntensityID     = Shader.PropertyToID("_Intensity");

        // ─────────────────────────────────────────
        // Setup
        // ─────────────────────────────────────────

        /// <summary>
        /// Called every frame by KuwaharaRendererFeature.
        /// Sets material properties directly — reliable for RenderGraph.
        /// </summary>
        public void Setup(Material material, float kernelSize, float sharpness,
                          float hardness, float zeroCrossing, float zeta, float intensity)
        {
            _material     = material;
            _kernelSize   = kernelSize;
            _sharpness    = sharpness;
            _hardness     = hardness;
            _zeroCrossing = zeroCrossing;
            _zeta         = zeta;
            _intensity    = intensity;

            material.SetFloat(KernelSizeID,   kernelSize);
            material.SetFloat(SharpnessID,    sharpness);
            material.SetFloat(HardnessID,     hardness);
            material.SetFloat(ZeroCrossingID, zeroCrossing);
            material.SetFloat(ZetaID,         zeta);
            material.SetFloat(IntensityID,    intensity);
        }

        // ─────────────────────────────────────────
        // Render Graph
        // ─────────────────────────────────────────

        /// <summary>
        /// Records all 4 Kuwahara passes into the Render Graph.
        /// Intermediate textures are created per-frame and managed by RenderGraph.
        /// </summary>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;
            if (_intensity <= 0f)  return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle cameraColor = resourceData.activeColorTexture;

            TextureDesc desc    = cameraColor.GetDescriptor(renderGraph);
            desc.depthBufferBits = DepthBits.None;

            TextureHandle tensorTex = renderGraph.CreateTexture(desc);
            TextureHandle blurHTex  = renderGraph.CreateTexture(desc);
            TextureHandle tfmTex    = renderGraph.CreateTexture(desc);
            TextureHandle resultTex = renderGraph.CreateTexture(desc);

            // ── Pass 0: Structure Tensor ──────────────────
            AddBlitPass(renderGraph, "Kuwahara_StructureTensor",
                cameraColor, tensorTex, _material, 0);

            // ── Pass 1: Gaussian Blur Horizontal ─────────
            AddBlitPass(renderGraph, "Kuwahara_BlurH",
                tensorTex, blurHTex, _material, 1);

            // ── Pass 2: Gaussian Blur Vertical + TFM ─────
            AddBlitPass(renderGraph, "Kuwahara_BlurV_TFM",
                blurHTex, tfmTex, _material, 2);

            // ── Pass 3: Anisotropic Kuwahara ──────────────
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Kuwahara_Anisotropic", out var passData))
            {
                passData.source   = cameraColor;
                passData.tfm      = tfmTex;
                passData.material = _material;
                passData.pass     = 3;

                builder.UseTexture(passData.source);
                builder.UseTexture(passData.tfm);
                builder.SetRenderAttachment(resultTex, 0);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalTexture(Shader.PropertyToID("_TFM"), data.tfm);
                    Blitter.BlitTexture(context.cmd, data.source,
                        new Vector4(1, 1, 0, 0), data.material, data.pass);
                });
            }

            // ── Copy result back to camera color ─────────
            AddBlitPass(renderGraph, "Kuwahara_CopyToCamera",
                resultTex, resourceData.activeColorTexture, null, -1);
        }

        // ─────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────

        /// <summary>
        /// Adds a simple blit pass from source to destination using the given material and pass index.
        /// Pass index -1 uses the default blit shader (copy only).
        /// </summary>
        private static void AddBlitPass(RenderGraph renderGraph, string name,
            TextureHandle source, TextureHandle destination,
            Material material, int passIndex)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(name, out var passData))
            {
                passData.source   = source;
                passData.material = material;
                passData.pass     = passIndex;

                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(destination, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    if (data.material != null)
                        Blitter.BlitTexture(context.cmd, data.source,
                            new Vector4(1, 1, 0, 0), data.material, data.pass);
                    else
                        Blitter.BlitTexture(context.cmd, data.source,
                            new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }
    }
}