Shader "Lab1/Simple Sprite Tint URP"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Tint("Tint", Color) = (1,1,1,1)
        _OverlayTex("Overlay Texture", 2D) = "white" {}
        _OverlayTint("Overlay Tint", Color) = (1,1,1,1)
        _OverlayStrength("Overlay Strength", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
                float2 overlayUV : TEXCOORD1;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            TEXTURE2D(_OverlayTex);
            SAMPLER(sampler_OverlayTex);
            float4 _OverlayTex_ST;
            float4 _Tint;
            float4 _OverlayTint;
            float _OverlayStrength;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.overlayUV = TRANSFORM_TEX(input.uv, _OverlayTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 baseColor = sprite * input.color * _Tint;
                half4 overlay = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, input.overlayUV);
                half strength = saturate(_OverlayStrength);
                half3 overlayRgb = overlay.rgb * _OverlayTint.rgb;
                half overlayAlpha = overlay.a * _OverlayTint.a;
                baseColor.rgb = lerp(baseColor.rgb, baseColor.rgb * overlayRgb, strength);
                baseColor.a = baseColor.a * lerp(1.0h, overlayAlpha, strength);
                return baseColor;
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
