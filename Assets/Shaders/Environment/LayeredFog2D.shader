Shader "W1/Environment/LayeredFog2D"
{
    Properties
    {
        _Color ("Fog Color", Color) = (0.72, 0.75, 0.76, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.25
        _NoiseScale ("Noise Scale", Range(0.5, 12)) = 3
        _DetailScale ("Detail Scale", Range(1, 24)) = 8
        _Speed ("Speed", Vector) = (0.025, 0.006, 0, 0)
        _Density ("Density", Range(0, 1)) = 0.5
        _Softness ("Softness", Range(0.01, 0.5)) = 0.22
        _VerticalFade ("Vertical Fade", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "LayeredFog2D"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Opacity;
                half _NoiseScale;
                half _DetailScale;
                float4 _Speed;
                half _Density;
                half _Softness;
                half _VerticalFade;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                local = local * local * (3.0 - 2.0 * local);
                float a = Hash21(cell);
                float b = Hash21(cell + float2(1, 0));
                float c = Hash21(cell + float2(0, 1));
                float d = Hash21(cell + float2(1, 1));
                return lerp(lerp(a, b, local.x), lerp(c, d, local.x), local.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 drift = _Time.y * _Speed.xy;
                float broad = ValueNoise(input.uv * _NoiseScale + drift);
                float detail = ValueNoise(input.uv * _DetailScale - drift * 1.7 + 7.13);
                float field = broad * 0.72 + detail * 0.28;
                float threshold = 1.0 - _Density;
                float fog = smoothstep(threshold - _Softness, threshold + _Softness, field);
                float vertical = lerp(1.0, smoothstep(0.0, 0.55, input.uv.y), _VerticalFade);
                return half4(_Color.rgb, _Color.a * _Opacity * fog * vertical);
            }
            ENDHLSL
        }
    }
}
