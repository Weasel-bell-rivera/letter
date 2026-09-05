Shader "W1/Environment/Fog Height Distance Silhouette 2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _SilhouetteColor ("Silhouette Color", Color) = (0.025, 0.032, 0.036, 1)
        _FogColor ("Fog Color", Color) = (0.68, 0.73, 0.74, 1)
        _HeightFogStart ("Height Fog Start", Float) = -4
        _HeightFogEnd ("Height Fog End", Float) = 5
        _DistanceFogStart ("Distance Fog Start", Float) = 8
        _DistanceFogEnd ("Distance Fog End", Float) = 20
        _HeightFogStrength ("Height Fog Strength", Range(0, 1)) = 0.65
        _DistanceFogStrength ("Distance Fog Strength", Range(0, 1)) = 0.8
        _ColorFogStrength ("Color Fog Strength", Range(0, 1)) = 0.9
        _AlphaFadeStrength ("Alpha Fade Strength", Range(0, 1)) = 0.28
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "FogHeightDistanceSilhouette"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float worldY : TEXCOORD1;
                float viewDepth : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _SilhouetteColor;
                half4 _FogColor;
                float _HeightFogStart;
                float _HeightFogEnd;
                float _DistanceFogStart;
                float _DistanceFogEnd;
                half _HeightFogStrength;
                half _DistanceFogStrength;
                half _ColorFogStrength;
                half _AlphaFadeStrength;
            CBUFFER_END

            float SafeInverseRange(float value, float startValue, float endValue)
            {
                float range = max(abs(endValue - startValue), 0.0001);
                return saturate((value - min(startValue, endValue)) / range);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 positionVS = TransformWorldToView(positionWS);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.color = input.color;
                output.worldY = positionWS.y;
                output.viewDepth = -positionVS.z;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half sourceAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                half heightFog = SafeInverseRange(input.worldY, _HeightFogStart, _HeightFogEnd)
                    * _HeightFogStrength;
                half distanceFog = SafeInverseRange(input.viewDepth, _DistanceFogStart, _DistanceFogEnd)
                    * _DistanceFogStrength;
                half fogAmount = 1.0h - (1.0h - heightFog) * (1.0h - distanceFog);

                half3 silhouette = _SilhouetteColor.rgb * input.color.rgb;
                half3 finalColor = lerp(silhouette, _FogColor.rgb,
                    saturate(fogAmount * _ColorFogStrength));
                half finalAlpha = sourceAlpha * _SilhouetteColor.a * input.color.a
                    * (1.0h - saturate(fogAmount * _AlphaFadeStrength));
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
