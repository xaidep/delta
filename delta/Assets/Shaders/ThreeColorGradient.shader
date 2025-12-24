Shader "Custom/ThreeColorGradient"
{
    Properties
    {
        _ColorLeft ("左側の色", Color) = (1, 0.8, 0, 1)
        _ColorMid ("中央の色", Color) = (1, 0.4, 0, 1)
        _ColorRight ("右側の色", Color) = (1, 0, 0, 1)
        _Midpoint ("中間点", Range(0, 1)) = 0.5
        _MainTex ("テクスチャ", 2D) = "white" {}

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UI"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorLeft;
                float4 _ColorMid;
                float4 _ColorRight;
                float _Midpoint;
                float4 _MainTex_ST;
                float4 _ClipRect; // Required for RectMask2D
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = input.uv.x;
                half4 gradientColor;

                if (t < _Midpoint)
                {
                    float localT = t / _Midpoint;
                    gradientColor = lerp(_ColorLeft, _ColorMid, localT);
                }
                else
                {
                    float localT = (t - _Midpoint) / (1.0 - _Midpoint);
                    gradientColor = lerp(_ColorMid, _ColorRight, localT);
                }

                // Sample the sprite texture (essential for masking/rounded corners)
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Apply vertex color and texture alpha
                gradientColor *= input.color;
                gradientColor.a *= texColor.a;

                #ifdef UNITY_UI_CLIP_RECT
                    gradientColor.a *= UnityGet2DClipping(input.positionWS.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip (gradientColor.a - 0.001);
                #endif

                return gradientColor;
            }
            ENDHLSL
        }
    }
}
