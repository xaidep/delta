Shader "UI/PixelArt"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _PixelCountX ("Pixel Count X", Float) = 64
        _PixelCountY ("Pixel Count Y", Float) = 64
        
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 3)) = 0 // Changed from Toggle to Range
        _AlphaThreshold ("Alpha Threshold", Range(0, 1)) = 0.5
        _PosterizeSteps ("Posterize Steps", Range(1, 255)) = 255

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
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
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            float _PixelCountX;
            float _PixelCountY;
            
            fixed4 _OutlineColor;
            float _OutlineWidth; // Optional: Control thickness (integer steps recommended) or on/off
            float _AlphaThreshold;
            float _PosterizeSteps;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPosition);

                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Pixelation Logic
                float2 uv = i.texcoord;
                
                float px = 0.0;
                float py = 0.0;
                
                if (_PixelCountX > 0) {
                    px = 1.0 / _PixelCountX;
                    // Sample center of the pixel to avoid edge artifacts
                    uv.x = (floor(uv.x * _PixelCountX) + 0.5) / _PixelCountX;
                }
                    
                if (_PixelCountY > 0) {
                    py = 1.0 / _PixelCountY;
                    uv.y = (floor(uv.y * _PixelCountY) + 0.5) / _PixelCountY;
                }

                half4 color = (tex2Dlod(_MainTex, float4(uv, 0, 0)) + _TextureSampleAdd) * i.color;
                
                // Alpha Cutoff / Threshold handling
                if (color.a < _AlphaThreshold)
                {
                    discard;
                }
                else
                {
                    color.a = 1.0;
                }

                // Restore vertex alpha
                color.a *= i.color.a;
                
                // Color Posterization (Clean up gradation/anti-aliasing)
                if (_PosterizeSteps < 255)
                {
                    color.rgb = floor(color.rgb * _PosterizeSteps + 0.5) / _PosterizeSteps;
                }

                // Outline Logic (Inner)
                if (_OutlineWidth > 0 && color.a > 0.01) 
                {
                    int width = floor(_OutlineWidth);
                    bool isEdge = false;
                    
                    for (int x = -3; x <= 3; x++)
                    {
                        if (abs(x) > width) continue;

                        for (int y = -3; y <= 3; y++)
                        {
                            if (abs(y) > width) continue;
                            if (x == 0 && y == 0) continue;
                            
                            float2 samplePos = uv + float2(x * px, y * py);
                            
                            // Check if coordinate is outside valid UV bounds (0-1)
                            // If so, consider it transparent (Edge of the world)
                            if (samplePos.x < 0.0 || samplePos.x > 1.0 || samplePos.y < 0.0 || samplePos.y > 1.0)
                            {
                                isEdge = true;
                                break;
                            }
                            
                            float4 sampleUV = float4(samplePos, 0, 0);
                            fixed4 neighbor = tex2Dlod(_MainTex, sampleUV);
                            
                            // Check neighbor against the same threshold
                            if (neighbor.a < _AlphaThreshold)
                            {
                                isEdge = true;
                                break;
                            }
                        }
                        if (isEdge) break;
                    }

                    if (isEdge)
                    {
                        color.rgb = _OutlineColor.rgb;
                        color.a = _OutlineColor.a * i.color.a; 
                    }
                }

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
