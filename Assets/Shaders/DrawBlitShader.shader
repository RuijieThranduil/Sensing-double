Shader "Custom/DrawBlitShader"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" { }
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        Cull Off Zwrite Off ZTest Always
        
        Pass
        {
            NAME "DrawPass"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _BrushSize;
            half4 _BrushColor;
            float _TexWidth;
            float _TexHeight;
            float2 _DrawPos;
            sampler2D _MainTex;
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : COLOR
            {
                float2 uv = i.uv;
                float ratio = _TexWidth / _TexHeight;
                uv.x *= ratio;
                
                float dist = distance(uv, _DrawPos);

                half4 texColor = tex2D(_MainTex, i.uv);
                half3 paintColor = lerp(texColor.rgb, _BrushColor.rgb, _BrushColor.a);
                
                if (dist < _BrushSize)
                {
                    return half4(_BrushColor.rgb, _BrushColor.a);
                }
                else
                    return texColor;
            }
            ENDCG
        }

        Pass
        {
            NAME "ClearPass"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : COLOR
            {
                return half4(0,0,0,0);
            }
            ENDCG
        }

        Pass
        {
            NAME "InvertMaskPass"
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : COLOR
            {
                half4 texColor = tex2D(_MainTex, i.uv);
                
                if (texColor.a > 0.01)
                {
                    return half4(0,0,0,0);
                }
                else
                {
                    return half4(1,1,1,1);
                }
            }
            ENDCG
        }
    }
}
