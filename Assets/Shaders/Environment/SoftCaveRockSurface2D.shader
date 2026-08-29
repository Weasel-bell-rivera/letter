Shader "W1/Environment/SoftCaveRockSurface2D"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.22, 0.14, 0.075, 1)
        _DarkColor ("Dark Color", Color) = (0.1, 0.055, 0.025, 1)
        _TextureScale ("Texture Scale", Range(1, 20)) = 5
        _Variation ("Variation", Range(0, 1)) = 0.25
        _Wetness ("Wet Streaks", Range(0, 1)) = 0.15
        _Strata ("Strata", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
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
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half alpha : TEXCOORD1;
            };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _DarkColor;
                half _TextureScale;
                half _Variation;
                half _Wetness;
                half _Strata;
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
                output.alpha = input.color.a;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float broad = Noise(input.uv * _TextureScale);
                float fine = Noise(input.uv * (_TextureScale * 3.1) + 17.4);
                float rock = broad * .72 + fine * .28;
                float horizontal = Noise(float2(input.uv.x * 2.2, input.uv.y * 18.0));
                float streakSeed = Noise(float2(input.uv.x * 22.0, 3.7));
                float streak = smoothstep(.66, .92, streakSeed) * smoothstep(.08, .95, input.uv.y);
                float tone = saturate(.52 + (rock - .5) * _Variation - horizontal * _Strata - streak * _Wetness);
                float edge = smoothstep(0.0, 1.0, input.alpha);
                return half4(lerp(_DarkColor.rgb, _BaseColor.rgb, tone), edge);
            }
            ENDHLSL
        }
    }
}
