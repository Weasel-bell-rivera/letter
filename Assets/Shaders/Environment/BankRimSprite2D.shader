Shader "W1/Environment/Bank Rim Sprite 2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Rim silhouette", 2D) = "white" {}
        _Color ("Rim color", Color) = (1,1,1,1)
        _WorldFade ("Fade start, end, reverse", Vector) = (0,1,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct Varyings { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; float worldX:TEXCOORD1; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _WorldFade;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionHCS=TransformObjectToHClip(v.positionOS.xyz);
                o.uv=v.uv; o.worldX=TransformObjectToWorld(v.positionOS.xyz).x;
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                // Remove faint alpha dust; preserve the painted outline and flat fill.
                float a=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;
                a=saturate((a-0.04)/0.96);
                float fade=smoothstep(_WorldFade.x,_WorldFade.y,i.worldX);
                a*=lerp(fade,1.0-fade,_WorldFade.z);
                return half4(_Color.rgb,_Color.a*a);
            }
            ENDHLSL
        }
    }
}
