Shader "W1/Environment/Quiet Water Sprite 2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Water silhouette", 2D) = "white" {}
        _DeepColor ("Deep water", Color) = (0.018, 0.05, 0.04, 1)
        _ShallowColor ("Distant water", Color) = (0.04, 0.12, 0.095, 1)
        _ReflectionColor ("Reflected light", Color) = (0.13, 0.27, 0.19, 1)
        _ReflectionX ("Light center in sprite UV", Range(0,1)) = 0.63
        _ReflectionWidth ("Light width", Range(0.01,0.5)) = 0.1
        _RippleStrength ("Ripple brightness", Range(0,0.1)) = 0.012
        _Speed ("Ripple speed", Range(0,1)) = 0.15
        _RippleRows ("Ripple frequency along depth", Range(8,300)) = 225
        _SurfaceBand ("Surface band depth in UV", Range(0.02,0.5)) = 0.17
        _WaterlineWidth ("Waterline width in UV", Range(0.002,0.03)) = 0.009
        _WaterlineStrength ("Broken waterline brightness", Range(0,1)) = 0.65
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
            struct Varyings { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor, _ShallowColor, _ReflectionColor;
                float _ReflectionX, _ReflectionWidth, _RippleStrength, _Speed, _RippleRows;
                float _SurfaceBand, _WaterlineWidth, _WaterlineStrength;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionHCS=TransformObjectToHClip(v.positionOS.xyz);
                o.uv=v.uv; o.color=v.color;
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                // Keep the authored shoreline alpha fixed; only light moves.
                float alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;
                float t=_Time.y*_Speed;
                float depth=1.0-i.uv.y;
                float surface=1.0-smoothstep(0.02,_SurfaceBand,depth);
                // Uneven horizontal groups stay near the surface instead of
                // marching down the body like a sheet of falling water.
                float row=depth*_RippleRows
                    + sin(i.uv.x*19.0+t*0.38)*0.55
                    + sin(i.uv.x*41.0-t*0.23)*0.16;
                float breaks=smoothstep(0.15,0.85,
                    sin(i.uv.x*58.0+sin(depth*51.0)*2.0+t*0.5));
                float ripple=pow(saturate(sin(row)),24.0)*breaks*surface;
                float bentX=i.uv.x+sin(i.uv.y*155.0+t)*0.009+sin(row)*0.003;
                float width=_ReflectionWidth*lerp(1.4,0.65,saturate(i.uv.y));
                float reflection=exp(-pow((bentX-_ReflectionX)/max(width,0.01),2.0));
                reflection*=smoothstep(0.15,0.65,i.uv.y)*(0.45+0.35*ripple);
                float3 col=lerp(_DeepColor.rgb,_ShallowColor.rgb,smoothstep(0.15,0.85,i.uv.y));
                col=lerp(col,_ReflectionColor.rgb,reflection);
                col+=ripple*_RippleStrength*_ReflectionColor.rgb;
                // A thin interrupted glint, not a displaced collision/water edge.
                float ridge=_WaterlineWidth*(0.7+0.15*sin(i.uv.x*34.0+t*0.4));
                float edgeGlint=1.0-smoothstep(_WaterlineWidth*0.25,_WaterlineWidth,abs(depth-ridge));
                float lineBreaks=smoothstep(-0.2,0.65,
                    sin(i.uv.x*48.0+t*0.45)+0.3*sin(i.uv.x*93.0-t*0.3));
                col=lerp(col,_ReflectionColor.rgb,edgeGlint*lineBreaks*_WaterlineStrength);
                return half4(col*i.color.rgb,alpha*i.color.a*_DeepColor.a);
            }
            ENDHLSL
        }
    }
}
