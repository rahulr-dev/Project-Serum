Shader "Serum/Interactable Toon Outline"
{
    Properties
    {
        [HDR] _OutlineColor ("Outline Color", Color) = (0.25, 1.2, 1.5, 1)
        _OutlineWidthPixels ("Outline Width (Pixels)", Range(1, 12)) = 3
        [HDR] _ShineColor ("Shine Color", Color) = (1.5, 2, 2, 1)
        _ShineExtraWidthPixels ("Shine Extra Width (Pixels)", Range(0, 12)) = 4
        _ShineInterval ("Shine Interval (Seconds)", Range(0.5, 10)) = 3
        _ShineDuration ("Shine Duration (Seconds)", Range(0.1, 3)) = 0.8
        _ShineAngle ("Shine Angle (Degrees)", Range(0, 360)) = 0
        _ShineWidth ("Shine Width (Screen Fraction)", Range(0.01, 0.3)) = 0.08
        _ShineSoftness ("Shine Softness", Range(0.001, 0.2)) = 0.03
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry+1" }

        Pass
        {
            Name "InteractableOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                half4 _ShineColor;
                float _OutlineWidthPixels;
                float _ShineExtraWidthPixels;
                float _ShineInterval;
                float _ShineDuration;
                float _ShineAngle;
                float _ShineWidth;
                float _ShineSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 screenUV : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float ShineAtScreenPosition(float2 screenUV)
            {
                float angle = radians(_ShineAngle);
                float2 direction = float2(cos(angle), sin(angle));
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 centeredPosition = (screenUV - 0.5) * float2(aspect, 1.0);
                float span = abs(direction.x) * aspect + abs(direction.y);
                float sweepPosition = dot(centeredPosition, direction) / span + 0.5;

                // One sweep per interval, followed by a pause.
                float interval = max(_ShineInterval, _ShineDuration);
                float elapsed = fmod(_Time.y, interval);
                float active = 1.0 - step(_ShineDuration, elapsed);
                float progress = saturate(elapsed / max(_ShineDuration, 0.001));
                float center = lerp(-_ShineWidth, 1.0 + _ShineWidth, progress);
                float distanceToSweep = abs(sweepPosition - center);
                return active * (1.0 - smoothstep(_ShineWidth, _ShineWidth + _ShineSoftness, distanceToSweep));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 clipPosition = TransformObjectToHClip(input.positionOS.xyz);
                float2 screenUV = clipPosition.xy / clipPosition.w * 0.5 + 0.5;
                float shine = ShineAtScreenPosition(screenUV);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 normalVS = TransformWorldToViewDir(normalWS);
                float2 normalCS = mul((float3x3)UNITY_MATRIX_P, normalVS).xy;
                float2 outlineDirection = normalCS / max(length(normalCS), 0.0001);

                float pixelWidth = _OutlineWidthPixels + _ShineExtraWidthPixels * shine;
                clipPosition.xy += outlineDirection * pixelWidth * 2.0 * clipPosition.w / _ScreenParams.xy;
                output.positionCS = clipPosition;
                output.screenUV = screenUV;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float shine = ShineAtScreenPosition(input.screenUV);
                return lerp(_OutlineColor, _ShineColor, shine);
            }
            ENDHLSL
        }
    }
}
