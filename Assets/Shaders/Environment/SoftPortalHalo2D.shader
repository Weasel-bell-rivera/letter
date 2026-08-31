Shader "W1/Environment/SoftPortalHalo2D"
{
    Properties
    {
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 4)) = 2.4
        _RingWidth ("Ring Width", Range(0.01, 0.2)) = 0.055
        _Softness ("Edge Softness", Range(0.005, 0.2)) = 0.02
        _CoreOpacity ("Core Opacity", Range(0, 0.5)) = 0.04
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
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Opacity;
                half _RingWidth;
                half _Softness;
                half _CoreOpacity;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float radius = length((input.uv - 0.5) * 2.0);
                float outer = 1.0 - smoothstep(0.86, 0.86 + _Softness, radius);
                float inner = smoothstep(0.86 - _RingWidth - _Softness, 0.86 - _RingWidth, radius);
                float ring = outer * inner;
                float core = pow(saturate(1.0 - radius * radius), 2.5) * _CoreOpacity;
                half4 tint = input.color * _Tint;
                return half4(tint.rgb, saturate(tint.a * _Opacity * (ring + core)));
            }
            ENDHLSL
        }
    }
}
