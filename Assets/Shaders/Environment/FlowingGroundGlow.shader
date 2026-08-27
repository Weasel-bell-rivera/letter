Shader "W1/Environment/Flowing Ground Glow"
{
    Properties
    {
        [HDR] _GlowColor ("Glow Color", Color) = (0.08,2.8,0.38,1)
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.9
        _FlowSpeed ("Flow Speed", Float) = 1.15
        _FlowScale ("Flow Scale", Float) = 1.8
        _PulseSharpness ("Pulse Sharpness", Range(1, 12)) = 6
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "UniversalMaterialType"="Unlit"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteUnlit"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 lineUV : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 lineUV : TEXCOORD0;
                float2 worldPosition : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                half _GlowStrength;
                half _FlowSpeed;
                half _FlowScale;
                half _PulseSharpness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.worldPosition = positionWS.xy;
                output.lineUV = input.lineUV;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float phase = input.worldPosition.x * _FlowScale - _Time.y * _FlowSpeed;
                float primaryPulse = pow(0.5 + 0.5 * sin(phase), _PulseSharpness * 0.55);
                float secondaryPulse = pow(0.5 + 0.5 * sin(phase * 0.47 + 2.1), _PulseSharpness * 0.38);
                float movingEnergy = saturate(primaryPulse + secondaryPulse * 0.65);

                float centerDistance = abs(input.lineUV.y - 0.5) * 2.0;
                float softHalo = pow(saturate(1.0 - centerDistance), 1.8);
                float hotCore = pow(saturate(1.0 - centerDistance), 7.0);
                float intensity = (0.1 * softHalo + hotCore * (0.16 + movingEnergy * 2.15)) * _GlowStrength;

                half3 color = _GlowColor.rgb * intensity * input.color.rgb;
                half alpha = saturate((0.12 * softHalo + hotCore * (0.2 + movingEnergy * 0.8)) * input.color.a);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
