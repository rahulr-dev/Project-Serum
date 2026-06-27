Shader "Hidden/AgX_Tonemapper"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "AgX_Mathematical_Pass"
            ZWrite Off ZTest Always Blend Off Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            // Core libraries must precede URP Core and Blit
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Exact sRGB Inverse EOTF (Vectorized)
            float3 AgX_SRGBToLinear(float3 c)
            {
                float3 linearLo = c / 12.92;
                float3 linearHi = pow(max((c + 0.055) / 1.055, 0.0), float3(2.4, 2.4, 2.4));
                return lerp(linearLo, linearHi, step(0.04045, c));
            }

            float3 ApplyAgX(float3 val)
            {
                // Row sums strictly equal 1.0 to preserve white point
                const float3x3 agx_mat = float3x3(
                    0.84247906, 0.07843360, 0.07922375,
                    0.04232824, 0.87846864, 0.07916613,
                    0.04237565, 0.07843360, 0.87914297
                );

                // Row sums strictly equal 1.0
                const float3x3 agx_mat_inv = float3x3(
                    1.19687901, -0.09802088, -0.09902974,
                -0.05289685,  1.15190313, -0.09896118,
                -0.05297164, -0.09804345,  1.15107367
                );

                val = mul(agx_mat, val);

                const float min_ev = -10.0;
                const float max_ev =  6.5;
                val = clamp(log2(max(val, 1e-6)), min_ev, max_ev);
                val = (val - min_ev) / (max_ev - min_ev);

                float3 x2 = val * val;
                float3 x4 = x2 * x2;
                val = 15.5 * x4 * x2 - 40.14 * x4 * val + 31.96 * x4 - 6.868 * x2 * val + 0.4298 * x2 + 0.1191 * val - 0.00232;

                val = mul(agx_mat_inv, val);
                return val;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb;

                color = ApplyAgX(color);
                color = AgX_SRGBToLinear(color);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
