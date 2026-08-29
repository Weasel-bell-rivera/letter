Shader "W1/Environment/RadialCaveGlow2D"
{
    Properties
    {
        _Color ("Glow Color", Color) = (0.72, 0.52, 0.3, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0.5
        _Falloff ("Falloff", Range(0.5, 8)) = 2.8
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
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _Falloff;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = (input.uv - .5) * 2.0;
                float radial = saturate(1.0 - dot(centered, centered));
                radial = pow(radial, _Falloff);
                return half4(_Color.rgb, _Color.a * _Intensity * radial);
            }
            ENDHLSL
        }
    }
}
