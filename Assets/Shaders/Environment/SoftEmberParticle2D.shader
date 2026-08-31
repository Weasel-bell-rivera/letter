Shader "W1/Environment/SoftEmberParticle2D"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 0.32, 0.06, 1)
        _Softness ("Edge Softness", Range(0.05, 0.8)) = 0.45
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
            Name "SoftEmberParticle2D"
            Blend SrcAlpha One
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
                half4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Softness;
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
                float radius = length(input.uv - 0.5) * 2.0;
                float alpha = 1.0 - smoothstep(1.0 - _Softness, 1.0, radius);
                half4 tint = _Color * input.color;
                return half4(tint.rgb * alpha, tint.a * alpha);
            }
            ENDHLSL
        }
    }
}
