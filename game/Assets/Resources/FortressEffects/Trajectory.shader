Shader "Mini Fortress/Trajectory"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _FlowStrength ("Flow Strength", Float) = 0
        _FlowDensity ("Flow Density", Float) = 12
        _FlowSpeed ("Flow Speed", Float) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Trajectory"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _FlowStrength;
                float _FlowDensity;
                float _FlowSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _Color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Stretch UVs increase from the launch point to the end of the line.
                float phase = frac(input.uv.x * _FlowDensity - _Time.y * _FlowSpeed);
                half pulse = 1.0h - smoothstep(0.08, 0.24, abs(phase - 0.5));
                half strength = saturate(_FlowStrength);
                half4 color = input.color;
                color.rgb = lerp(color.rgb, half3(1, 1, 1), pulse * strength * 0.65h);
                // Retain at least 80% of the base alpha, including vertex end fades.
                color.a *= lerp(1.0h, 0.8h + 0.2h * pulse, strength);
                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
