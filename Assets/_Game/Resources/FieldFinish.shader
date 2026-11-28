Shader "Planet Harvest/Field Finish"
{
    Properties { _Tint ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Input { float4 p : POSITION; float3 n : NORMAL; float4 c : COLOR; };
            struct Output { float4 pos : SV_POSITION; half3 color : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END
            Output Vert(Input v)
            {
                Output o;
                o.pos = TransformObjectToHClip(v.p.xyz);
                half diffuse = saturate(dot(TransformObjectToWorldNormal(v.n), GetMainLight().direction));
                o.color = v.c.rgb * _Tint.rgb * (0.52 + 0.5 * diffuse);
                return o;
            }
            half4 Frag(Output i) : SV_Target { return half4(i.color, 1); }
            ENDHLSL
        }
    }
}
