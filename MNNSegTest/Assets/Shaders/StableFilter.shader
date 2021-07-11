Shader "Hidden/StableFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        // _SegTex ("Texture", 2D) = "white" {}
        // _Radius("Radius", int) = 20
    }

    CGINCLUDE

        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        sampler2D _MainTex;
        sampler2D _SourceTex;

        float4x4 _HomographyMatrix;

        v2f vert (appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        fixed4 frag (v2f i) : SV_Target
        {
            float4 tmp = mul(_HomographyMatrix, float4(i.uv.x - 0.5, i.uv.y - 0.5, 1.0, 0.0));

            fixed4 col = tex2D(_MainTex, fixed2(tmp.x + 0.5, tmp.y + 0.5));
            fixed4 ret;
            ret.r = tex2D(_SourceTex, i.uv).r;
            ret.g = col.r;
            ret.b = col.g;
            ret.a = col.b;
            return ret;
        }

        fixed4 frag1 (v2f i) : SV_Target
        {
            fixed4 ret = tex2D(_MainTex, i.uv).rrrr;
            return ret;
        }

    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag1

            ENDCG
        }
    }
}
