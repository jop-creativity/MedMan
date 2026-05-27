Shader "MedMan/KuwaharaFilter"
{
    Properties
    {
        _KernelSize  ("Kernel Size",   Range(2, 12)) = 6
        _Sharpness   ("Sharpness",     Range(1, 18)) = 8
        _Hardness    ("Hardness",      Range(1, 18)) = 8
        _ZeroCrossing("Zero Crossing", Range(0, 3))  = 0.58
        _Zeta        ("Zeta",          Range(0, 3))  = 0.1
        _Intensity   ("Intensity",     Range(0, 1))  = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        // ─────────────────────────────────────────
        // Pass 0 — Structure Tensor
        // Computes per-pixel Sobel gradients and stores
        // the outer product (Jxx, Jyy, Jxy) as the structure tensor.
        // Output is used in Pass 1 to determine edge orientation.
        // ─────────────────────────────────────────
        Pass
        {
            Name "StructureTensor"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag_StructureTensor

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half4 Frag_StructureTensor(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 d = _BlitTexture_TexelSize.xy;
                float2 uv = input.texcoord;

                // Sobel X and Y gradients
                float3 Sx = (
                     1.0 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-d.x, -d.y)).rgb +
                     2.0 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-d.x,  0.0)).rgb +
                     1.0 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-d.x,  d.y)).rgb +
                    -1.0 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( d.x, -d.y)).rgb +
                    -2.0 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( d.x,  0.0)).rgb +
                    -1.0 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( d.x,  d.y)).rgb
                ) / 4.0;

                float3 Sy = (
                     1.0 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-d.x, -d.y)).rgb +
                     2.0 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( 0.0, -d.y)).rgb +
                     1.0 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( d.x, -d.y)).rgb +
                    -1.0 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-d.x,  d.y)).rgb +
                    -2.0 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( 0.0,  d.y)).rgb +
                    -1.0 * SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( d.x,  d.y)).rgb
                ) / 4.0;

                return float4(dot(Sx, Sx), dot(Sy, Sy), dot(Sx, Sy), 1.0);
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────
        // Pass 1 — Gaussian Blur Horizontal
        // Blurs the structure tensor horizontally to smooth
        // the gradient field before eigenvalue decomposition.
        // ─────────────────────────────────────────
        Pass
        {
            Name "GaussianBlurH"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag_BlurH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define PI 3.14159265358979

            float gaussian(float sigma, float pos)
            {
                return (1.0 / sqrt(2.0 * PI * sigma * sigma)) * exp(-(pos * pos) / (2.0 * sigma * sigma));
            }

            half4 Frag_BlurH(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 d  = _BlitTexture_TexelSize.xy;

                float4 col = 0;
                float  sum = 0;

                [loop]
                for (int x = -5; x <= 5; ++x)
                {
                    float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(x, 0) * d);
                    float  g = gaussian(2.0, x);
                    col += c * g;
                    sum += g;
                }

                return col / sum;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────
        // Pass 2 — Gaussian Blur Vertical + TFM
        // Completes the blur vertically, then decomposes
        // the structure tensor into eigenvalues to produce
        // the Tensor Flow Map (TFM): edge tangent direction,
        // angle phi, and anisotropy A stored in float4.
        // ─────────────────────────────────────────
        Pass
        {
            Name "GaussianBlurV_TFM"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag_BlurV

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define PI 3.14159265358979

            float gaussian(float sigma, float pos)
            {
                return (1.0 / sqrt(2.0 * PI * sigma * sigma)) * exp(-(pos * pos) / (2.0 * sigma * sigma));
            }

            half4 Frag_BlurV(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 d  = _BlitTexture_TexelSize.xy;

                float4 col = 0;
                float  sum = 0;

                [loop]
                for (int y = -5; y <= 5; ++y)
                {
                    float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(0, y) * d);
                    float  g = gaussian(2.0, y);
                    col += c * g;
                    sum += g;
                }

                // Smoothed structure tensor components
                float3 g3 = (col / sum).rgb;

                // Eigenvalue decomposition of 2x2 structure tensor
                float lambda1 = 0.5 * (g3.y + g3.x + sqrt(max(0,
                    g3.y * g3.y - 2.0 * g3.x * g3.y + g3.x * g3.x + 4.0 * g3.z * g3.z)));
                float lambda2 = 0.5 * (g3.y + g3.x - sqrt(max(0,
                    g3.y * g3.y - 2.0 * g3.x * g3.y + g3.x * g3.x + 4.0 * g3.z * g3.z)));

                // Edge tangent direction (perpendicular to gradient)
                float2 v   = float2(lambda1 - g3.x, -g3.z);
                float2 t   = length(v) > 0.0 ? normalize(v) : float2(0.0, 1.0);
                float  phi = -atan2(t.y, t.x);

                // Anisotropy measure
                float A = (lambda1 + lambda2 > 0.0) ? (lambda1 - lambda2) / (lambda1 + lambda2) : 0.0;

                return float4(t, phi, A);
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────
        // Pass 3 — Anisotropic Kuwahara
        // Uses the TFM to orient an elliptical kernel
        // along edges. Samples N=8 sectors with Gaussian
        // polynomial weighting. Sector with lowest variance
        // (weighted by sharpness/hardness) determines output.
        // Blend with original controlled by Intensity.
        // ─────────────────────────────────────────
        Pass
        {
            Name "AnisotropicKuwahara"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag_Kuwahara

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define PI     3.14159265358979
            #define TWO_PI 6.28318530717959
            #define N      8

            TEXTURE2D_X(_TFM);
            SAMPLER(sampler_TFM);

            float _KernelSize;
            float _Sharpness;
            float _Hardness;
            float _ZeroCrossing;
            float _Zeta;
            float _Intensity;

            float gaussian(float sigma, float pos)
            {
                return (1.0 / sqrt(2.0 * PI * sigma * sigma)) * exp(-(pos * pos) / (2.0 * sigma * sigma));
            }

            half4 Frag_Kuwahara(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv  = input.texcoord;
                float2 d   = _BlitTexture_TexelSize.xy;
                int    r   = max(1, (int)_KernelSize);

                // Read edge tangent and anisotropy from TFM
                float4 tfm = SAMPLE_TEXTURE2D_X(_TFM, sampler_LinearClamp, uv);
                float  phi = tfm.z;
                float  A   = tfm.w;

                // Ellipse radii — stretch along edge based on anisotropy
                float  a = clamp((float)r * (1.0 + A), 1.0, 16.0);
                float  b = clamp((float)r / (1.0 + A), 1.0, 16.0);
                float  cos_phi = cos(phi);
                float  sin_phi = sin(phi);

                // Sector accumulators
                float3 m[8];
                float3 s[8];
                float  w[8];
                for (int i = 0; i < N; ++i) { m[i] = 0; s[i] = 0; w[i] = 0; }

                // Sector weighting function parameters
                float  piOverN  = PI / (float)N;
                float  cosZC    = cos(_ZeroCrossing);
                float  sinZC    = sin(_ZeroCrossing);
                float  zeta     = _Zeta;

                int iA = (int)ceil(a);
                int iB = (int)ceil(b);

                [loop]
                for (int y = -iB; y <= iB; ++y)
                {
                    [loop]
                    for (int x = -iA; x <= iA; ++x)
                    {
                        // Rotate sample into edge-aligned space
                        float2 v = float2(
                             cos_phi * x + sin_phi * y,
                            -sin_phi * x + cos_phi * y
                        );

                        // Check inside ellipse
                        if ((v.x * v.x) / (a * a) + (v.y * v.y) / (b * b) > 1.0) continue;

                        float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp,
                                         uv + float2(x, y) * d).rgb;

                        // Gaussian weight
                        float  gw = gaussian(0.5 * a, length(v));

                        // Distribute across sectors using polynomial weighting
                        [unroll]
                        for (int k = 0; k < N; ++k)
                        {
                            float  sectorAngle = TWO_PI * (float)k / (float)N;
                            float2 sectorDir   = float2(cos(sectorAngle), sin(sectorAngle));
                            float  cosA  = dot(normalize(v + 0.0001), sectorDir);
                            float  cosZC = cos(_ZeroCrossing);
                            float  poly  = max(0.0, (cosA - cosZC) / (1.0 - cosZC));
                            float  wp    = pow(poly, _Hardness) * gw;

                            m[k] += col * wp;
                            s[k] += col * col * wp;
                            w[k] += wp;
                        }
                    }
                }

                 // Pick sector with lowest variance — hard selection gives paint-stroke look
                float  minVar = 1e9;
                float3 result = float3(0, 0, 0);

                [unroll]
                for (int k = 0; k < N; ++k)
                {
                    if (w[k] < 0.0001) continue;

                    float3 mean = m[k] / w[k];
                    float3 sq   = s[k] / w[k];
                    float  var  = dot(sq - mean * mean, float3(1, 1, 1));

                    if (var < minVar)
                    {
                        minVar = var;
                        result = mean;
                    }
                }

                float3 filtered  = result;
                float3 original  = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
                float3 final     = lerp(original, filtered, _Intensity);

                return half4(final, 1.0);
            }
            ENDHLSL
        }
    }
}