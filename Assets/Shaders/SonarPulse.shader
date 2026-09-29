Shader "Serum/Sonar Pulse"
{
    Properties
    {
        _PulseColor ("Pulse Color", Color) = (0.2, 0.85, 1, 1)
        _Intensity ("Intensity", Range(0, 5)) = 1.5
        _Fade ("Fade", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            Name "SonarPulse"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _PulseColor;
                half _Intensity;
                half _Fade;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 viewDirection = SafeNormalize(GetCameraPositionWS() - input.positionWS);
                half rim = pow(saturate(1.0h - abs(dot(SafeNormalize(input.normalWS), viewDirection))), 2.5h);
                half alpha = rim * _Fade * _PulseColor.a;
                return half4(_PulseColor.rgb * _Intensity * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
