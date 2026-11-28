Shader "Planet Harvest/Lake Water"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Output { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            Output Vert(float4 p : POSITION, float2 uv : TEXCOORD0)
            {
                Output o;
                o.pos = TransformObjectToHClip(p.xyz);
                o.uv = uv;
                return o;
            }
            half4 Frag(Output i) : SV_Target
            {
                float2 p = i.uv;
                float t = _Time.y;
                float ripple = sin(p.y*7 + sin(p.x*2+t)*.8 + t*1.6);
                float bank = smoothstep(2.5,6.5,length(p));
                float3 water = lerp(float3(.035,.29,.39),float3(.13,.62,.6),bank);
                water += float3(.20,.29,.24)*smoothstep(.85,.98,ripple)*smoothstep(.25,.8,sin(p.x*1.7+p.y+t*.4));
                return half4(water,1);
            }
            ENDHLSL
        }
    }
}
