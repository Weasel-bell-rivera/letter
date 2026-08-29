Shader "W1/Environment/CaveDust2D"
{
    Properties
    {
        _Color ("Dust Color", Color) = (0.55, 0.4, 0.24, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.25
        _NoiseScale ("Broad Scale", Range(0.5, 10)) = 2.5
        _DetailScale ("Detail Scale", Range(2, 24)) = 9
        _Speed ("Speed", Vector) = (0.015, 0.003, 0, 0)
        _Density ("Density", Range(0, 1)) = 0.5
        _Softness ("Softness", Range(0.02, 0.5)) = 0.24
        _BandCenter ("Band Center", Range(0, 1)) = 0.45
        _BandWidth ("Band Width", Range(0.05, 1)) = 0.55
        _BandStrength ("Band Strength", Range(0, 1)) = 0.65
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "CaveDust2D"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Opacity;
                half _NoiseScale;
                half _DetailScale;
                float4 _Speed;
                half _Density;
                half _Softness;
                half _BandCenter;
                half _BandWidth;
                half _BandStrength;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
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
                float broad = Noise(input.uv * _NoiseScale + drift);
                float medium = Noise(input.uv * (_NoiseScale * 2.35) - drift * 1.25 + 11.7);
                float detail = Noise(input.uv * _DetailScale + drift * 2.1 + 27.3);
                float field = broad * .58 + medium * .29 + detail * .13;
                float threshold = 1.0 - _Density;
                float dust = smoothstep(threshold - _Softness, threshold + _Softness, field);
                float warpedY = input.uv.y + (broad - .5) * .24;
                float bandDistance = abs(warpedY - _BandCenter);
                float band = 1.0 - smoothstep(_BandWidth * .35, _BandWidth, bandDistance);
                dust *= lerp(1.0, band, _BandStrength);
                return half4(_Color.rgb, _Color.a * _Opacity * dust);
            }
            ENDHLSL
        }
    }
}
