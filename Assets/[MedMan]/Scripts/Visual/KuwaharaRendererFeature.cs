using UnityEngine;
using UnityEngine.Rendering.Universal;
using NaughtyAttributes;

namespace MedMan.Visual
{
    /// <summary>
    /// URP ScriptableRendererFeature that registers the Kuwahara Filter pass.
    /// Add this to the URP Renderer asset (Universal Renderer Data) in Project Settings.
    /// KuwaharaIntensity is read by VisualManager and updated per game state
    /// from FearProfileSO.KuwaharaIntensity.
    /// </summary>
    public class KuwaharaRendererFeature : ScriptableRendererFeature
    {
        // ─────────────────────────────────────────
        // Fields
        // ─────────────────────────────────────────

        [BoxGroup("Shader")]
        [Tooltip("Assign KuwaharaShader material here.")]
        [SerializeField] private Material _material;

        [BoxGroup("Parameters")]
        [Range(2f, 12f)]
        [Tooltip("Radius of the Kuwahara kernel. Higher = stronger painterly effect, more expensive.")]
        [SerializeField] private float _kernelSize = 6f;

        [BoxGroup("Parameters")]
        [Range(0f, 1f)]
        [Tooltip("Blend between original and filtered image. 0 = off, 1 = full effect.")]
        [SerializeField] private float _intensity = 1f;

        [BoxGroup("Parameters")]
        [Range(1f, 18f)]
        [Tooltip("Sharpness of sector boundaries. Higher = more distinct brush strokes.")]
        [SerializeField] private float _sharpness = 8f;
        
        [BoxGroup("Parameters")]
        [Range(1f, 18f)]
        [SerializeField] private float _hardness = 8f;

        [BoxGroup("Parameters")]
        [Range(0f, 3f)]
        [SerializeField] private float _zeroCrossing = 0.58f;

        [BoxGroup("Parameters")]
        [Range(0f, 3f)]
        [SerializeField] private float _zeta = 0.1f;

        private KuwaharaRenderPass _pass;

        // ─────────────────────────────────────────
        // Properties
        // ─────────────────────────────────────────

        /// <summary>
        /// Current intensity of the Kuwahara effect.
        /// Set by VisualManager based on FearProfileSO per game state.
        /// </summary>
        public float KuwaharaIntensity
        {
            get => _intensity;
            set => _intensity = Mathf.Clamp01(value);
        }

        // ─────────────────────────────────────────
        // ScriptableRendererFeature overrides
        // ─────────────────────────────────────────

        /// <summary>
        /// Creates the KuwaharaRenderPass instance.
        /// Called by URP when the renderer is initialized.
        /// </summary>
        public override void Create()
        {
            _pass = new KuwaharaRenderPass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
            };
        }

        /// <summary>
        /// Configures and enqueues the pass each frame.
        /// Skips if material is not assigned or intensity is zero.
        /// </summary>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_material == null)
            {
                Debug.LogWarning("[KuwaharaRendererFeature] No material assigned.");
                return;
            }

            if (_intensity <= 0f) return;

            _pass.Setup(_material, _kernelSize, _sharpness, _hardness, _zeroCrossing, _zeta, _intensity);
            renderer.EnqueuePass(_pass);
        }
    }
}