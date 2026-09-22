Shader "Serum/Eye Blink Overlay"
{
    Properties
    {
        [Toggle] _AutoBlink ("Auto Blink", Float) = 1
        _BlinkInterval ("Blink Interval", Range(0.5, 10)) = 3.5
        _BlinkDuration ("Blink Duration", Range(0.05, 1)) = 0.15
        _Blink ("Blink (Manual)", Range(0, 1)) = 0
        [Enum(Top, 0, Bottom, 1, Left, 2, Right, 3)] _BlinkDirection ("Blink Direction", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "EyeBlink"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _AutoBlink;
                float _BlinkInterval;
                float _BlinkDuration;
                float _Blink;
                float _BlinkDirection;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float phase = frac(_Time.y / max(_BlinkInterval, 0.01));
                float progress = phase / max(_BlinkDuration, 0.01);
                float autoBlink = saturate(1.0 - abs(progress * 2.0 - 1.0));
                autoBlink *= step(phase, _BlinkDuration) * _AutoBlink;
                float blink = max(_Blink, autoBlink);

                float blackCover;
                if (_BlinkDirection < 0.5)
                    blackCover = step(1.0 - blink, input.uv.y); // Top
                else if (_BlinkDirection < 1.5)
                    blackCover = step(input.uv.y, blink); // Bottom
                else if (_BlinkDirection < 2.5)
                    blackCover = step(input.uv.x, blink); // Left
                else
                    blackCover = step(1.0 - blink, input.uv.x); // Right

                return half4(0, 0, 0, blackCover);
            }
            ENDHLSL
        }
    }
}
