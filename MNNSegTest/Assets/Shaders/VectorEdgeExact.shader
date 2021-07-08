Shader "Unlit/VectorEdgeExact"
{
	Properties
	{
		_Radius("Radius", int) = 2
        _Vec("Vec", vector) = (3, 5, 0, 0)
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
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
			
			uniform fixed4 _SegTex_TexelSize;
			sampler2D _SegTex;
			int _Radius;
			float4 _Vec;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float col = 0;
				float x_down, x_up, y_down, y_up;
				if (_Vec.x > 0)
				{
					x_down = -_Radius;
					x_up = _Radius + (int)_Vec.x * 10;
				}
				else
				{
					x_up = _Radius;
					x_down = -_Radius + (int)_Vec.x * 10;
				}
				if (_Vec.y > 0)
				{
					y_down = -_Radius;
					y_up = _Radius + (int)_Vec.y * 10;
				}
				else
				{
					y_up = _Radius;
					y_down = -_Radius + (int)_Vec.y * 10;
				}

				int pixelCount = (-x_down + x_up + 1) * (-y_down + y_up + 1);
				float pixelRatio = 1.0f / (float)pixelCount;
				for(int ti = x_down; ti <= x_up; ti ++)
				{
					for(int tj = y_down; tj <= y_up; tj ++)
					{
						fixed2 deltaUV = fixed2(_SegTex_TexelSize.x * ti, _SegTex_TexelSize.y * tj);
						col += tex2D(_SegTex, i.uv + deltaUV).r;
					}
				}
                col = abs(2 * col - pixelCount) * pixelRatio;
				return fixed4(col, col, col, 1);
			}
			ENDCG
		}
	}
}