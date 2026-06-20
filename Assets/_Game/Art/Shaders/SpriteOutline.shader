Shader "GemCafe/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1, 0.92, 0.3, 1)
        _OutlineSize ("Outline Thickness (px)", Range(0, 8)) = 2.5
        _OutlineGlow ("Outline Glow", Range(1, 4)) = 1.9
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _RendererColor;
            fixed4 _OutlineColor;
            float  _OutlineSize;
            float  _OutlineGlow;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                // Edge detection: take the strongest neighbouring alpha around this texel.
                float2 o = _MainTex_TexelSize.xy * _OutlineSize;
                float a = c.a;
                float n = 0;
                n = max(n, tex2D(_MainTex, IN.texcoord + float2( o.x, 0)).a);
                n = max(n, tex2D(_MainTex, IN.texcoord + float2(-o.x, 0)).a);
                n = max(n, tex2D(_MainTex, IN.texcoord + float2(0,  o.y)).a);
                n = max(n, tex2D(_MainTex, IN.texcoord + float2(0, -o.y)).a);
                n = max(n, tex2D(_MainTex, IN.texcoord + float2( o.x * 0.7,  o.y * 0.7)).a);
                n = max(n, tex2D(_MainTex, IN.texcoord + float2(-o.x * 0.7,  o.y * 0.7)).a);
                n = max(n, tex2D(_MainTex, IN.texcoord + float2( o.x * 0.7, -o.y * 0.7)).a);
                n = max(n, tex2D(_MainTex, IN.texcoord + float2(-o.x * 0.7, -o.y * 0.7)).a);

                // Outline appears where the current texel is transparent but a neighbour is opaque.
                float outline = saturate(n - a);
                float outlineA = outline * _OutlineColor.a;

                // Composite the sprite over the glowing outline. Output is premultiplied
                // alpha to match "Blend One OneMinusSrcAlpha".
                float3 spritePre  = c.rgb * c.a;
                float3 outlinePre = _OutlineColor.rgb * _OutlineGlow * outlineA;
                float3 rgb   = spritePre + outlinePre * (1.0 - c.a);
                float  alpha = c.a + outlineA * (1.0 - c.a);
                return fixed4(rgb, alpha);
            }
        ENDCG
        }
    }

    Fallback "Sprites/Default"
}
