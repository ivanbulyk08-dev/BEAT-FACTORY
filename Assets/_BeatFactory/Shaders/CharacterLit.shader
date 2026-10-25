// Main character in the painted rooms: puts the clean pose sprite into the room's light.
//  - ambient: multiplies the sprite by the room's colour/brightness at the spot (sampled from the painting)
//  - key: soft gradient across the body towards the strongest room light
//  - rim: thin coloured edge on the side that faces that light (from the sprite's own alpha, no normal maps)
//  - floor shade: darker towards the ground, desaturation to match the painted look
// UI shader (Canvas, masks and RectMask2D work like UI/Default).
Shader "BeatFactory/UI/CharacterLit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Ambient ("Ambient (multiply)", Color) = (0.8,0.75,0.8,1)
        _KeyColor ("Key light colour", Color) = (1,0.65,0.4,1)
        _KeyStrength ("Key strength", Range(0,1.5)) = 0.35
        _RimColor ("Rim colour", Color) = (1,0.5,0.8,1)
        _RimStrength ("Rim strength", Range(0,3)) = 1
        _RimWidth ("Rim width (texels)", Range(0.5,12)) = 2.5
        _LightDir ("Light direction (sprite space xy)", Vector) = (1,0.4,0,0)
        _FloorShade ("Floor shade", Range(0,1)) = 0.3
        _Desaturate ("Desaturate", Range(0,1)) = 0.12

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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color, _Ambient, _KeyColor, _RimColor;
            float _KeyStrength, _RimStrength, _RimWidth, _FloorShade, _Desaturate;
            float4 _LightDir;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float2 dir = normalize(_LightDir.xy + float2(0.0001, 0));

                // rim: edge normal from the alpha gradient (two radii = soft), lit only where the edge faces the light,
                // fading away from the light along the figure
                float2 t = _MainTex_TexelSize.xy * _RimWidth;
                float gx = tex2D(_MainTex, i.uv + float2(t.x, 0)).a - tex2D(_MainTex, i.uv - float2(t.x, 0)).a;
                float gy = tex2D(_MainTex, i.uv + float2(0, t.y)).a - tex2D(_MainTex, i.uv - float2(0, t.y)).a;
                gx += 0.5 * (tex2D(_MainTex, i.uv + float2(t.x * 2, 0)).a - tex2D(_MainTex, i.uv - float2(t.x * 2, 0)).a);
                gy += 0.5 * (tex2D(_MainTex, i.uv + float2(0, t.y * 2)).a - tex2D(_MainTex, i.uv - float2(0, t.y * 2)).a);
                float2 g = float2(gx, gy);
                float glen = length(g);
                float2 n = -g / max(glen, 0.0001);           // outward edge normal
                float facing = saturate(dot(n, dir));
                float rim = c.a * saturate(glen * 0.75) * facing * facing;
                float along = saturate(0.5 + dot(i.uv - 0.5, dir));  // 1 at the lit end of the figure
                rim *= lerp(0.35, 1.0, along);

                // key: gradient across the figure towards the light (aspect-corrected sprite space)
                float2 p = i.uv - 0.5;
                p.x *= _MainTex_TexelSize.z / max(_MainTex_TexelSize.w, 1.0);
                float key = saturate(0.5 + dot(p, dir) * 1.6);

                float3 col = c.rgb;
                float lum = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(col, lum.xxx, _Desaturate);
                col = col * (_Ambient.rgb + _KeyColor.rgb * _KeyStrength * (key - 0.3));
                col *= lerp(1.0 - _FloorShade, 1.0, smoothstep(0.0, 0.45, i.uv.y));
                col += _RimColor.rgb * rim * _RimStrength * 0.5;
                col = max(col, 0);

                fixed4 o = fixed4(col, c.a) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                o.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(o.a - 0.001);
                #endif
                o.rgb *= o.a; // premultiplied (Blend One OneMinusSrcAlpha)
                return o;
            }
            ENDCG
        }
    }
}
