Shader "W1/Gameplay/Silhouette Matte Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        _MatteLow ("Opaque Luminance", Range(0, 1)) = 0.08
        _MatteHigh ("Transparent Luminance", Range(0, 1)) = 0.65
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
            Name "SilhouetteMatte"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _MatteLow;
                float _MatteHigh;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                Varyings output;
                float3 positionOS = UnityFlipSprite(input.positionOS.xyz, unity_SpriteProps.xy);
                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = input.uv;
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half luminance = dot(source.rgb, half3(0.2126, 0.7152, 0.0722));
                // This source sheet has a baked pale matte, not an alpha channel.
                // Remove it only in presentation; never infer interaction from pixels.
                half coverage = 1.0h - smoothstep(_MatteLow, max(_MatteHigh, _MatteLow + 0.001), luminance);
                // Preserve restrained structure while letting the existing state tint
                // remain readable, including the permanent latch's cyan feedback.
                half shade = lerp(0.55h, 1.0h, saturate(luminance / max(_MatteLow, 0.001)));
                return half4(input.color.rgb * shade, input.color.a * source.a * coverage);
            }
            ENDHLSL
        }
    }
}
