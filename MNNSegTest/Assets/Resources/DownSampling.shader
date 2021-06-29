//----------------------------------------------------------------------------------------------------------
// X-PostProcessing Library
// https://github.com/QianMo/X-PostProcessing-Library
// Copyright (C) 2020 QianMo. All rights reserved.
// Licensed under the MIT License 
// You may not use this file except in compliance with the License.You may obtain a copy of the License at
// http://opensource.org/licenses/MIT
//----------------------------------------------------------------------------------------------------------

Shader "Hidden/DownSampling"
{
	Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _RGBTex("Texture", 2D) = "white" {}
		_AverageTex("Texture", 2D) = "white" {}
    }

	HLSLINCLUDE
	
	#include "HLSLs/StdLib.hlsl"
	#include "HLSLs/XPostProcessing.hlsl"
	
	uniform float4 _MainTex_ST;
	uniform half _Offset;

	TEXTURE2D_SAMPLER2D(_RGBTex, sampler_RGBTex);
	uniform float4 _RGBTex_ST;
	TEXTURE2D_SAMPLER2D(_AverageTex, sampler_AverageTex);
	uniform float4 _AverageTex_ST;
	
	struct v2f_DownSample
	{
		float4 vertex: SV_POSITION;
		float2 texcoord: TEXCOORD0;
		float2 uv: TEXCOORD1;
		float4 uv01: TEXCOORD2;
		float4 uv23: TEXCOORD3;
	};
	
	
	struct v2f_UpSample
	{
		float4 vertex: SV_POSITION;
		float2 texcoord: TEXCOORD0;
		float4 uv01: TEXCOORD1;
		float4 uv23: TEXCOORD2;
		float4 uv45: TEXCOORD3;
		float4 uv67: TEXCOORD4;
	};

	struct appdata
	{
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
	};

	struct v2f_Blend
	{
		float4 vertex: SV_POSITION;
		float2 texcoord: TEXCOORD0;
		float2 uv: TEXCOORD1;
	};

	v2f_Blend Vert_Blend(appdata v)
	{
		v2f_Blend o;
		o.vertex = mul (mul(unity_MatrixVP, unity_ObjectToWorld), v.vertex);
		o.texcoord = v.texcoord;

		// #if UNITY_UV_STARTS_AT_TOP
		// 	o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
		// #endif
		o.uv = TRANSFORM_TEX(o.texcoord, _MainTex);

		return o;
	}

	half4 Frag_Blend(v2f_Blend i): SV_Target
	{
		half4 col = SAMPLE_TEXTURE2D(_RGBTex, sampler_RGBTex, i.uv);
		half segment = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).x; // segment [0, 1]
		// half confidence = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).y; // confidence [0, 1]
		col *= segment;
		col.w = segment; // final alpha is used for counting sky pixels. average(sky) = average(texture) / alpha. I have already proved that.
		return col;
	}

	half ColorDistance(half3 e1, half3 e2)
	{
		half rmean = (e1.x + e2.x) / 2;
		half r = e1.x - e2.x;
		half g = e1.y - e2.y;
		half b = e1.z - e2.z;
		return sqrt((2 + rmean) * r * r + 4 * g * g + (2 + 0.99609375 - rmean) * b * b);
	}

	half4 Frag_AverageSky(v2f_Blend i): SV_Target
	{
		half4 average = SAMPLE_TEXTURE2D(_AverageTex, sampler_AverageTex, i.uv);
		average.xyz /= average.w;

		half3 col = SAMPLE_TEXTURE2D(_RGBTex, sampler_RGBTex, i.uv).xyz;
		half segment = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).x; // segment [0, 1]
		half confidence = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).y; // confidence

		half flag = ColorDistance(col, average.xyz) < 0.3;
		half bconf = confidence > 0.98;

		half result = 0;

		//	 if (segment == 1 && flag == 1 && bconf == 0) result = 1;
		//else if (segment == 1 && flag == 0 && bconf == 0) result = confidence;
		//else if (segment == 1 && flag == 1 && bconf == 1) result = 1;
		//else if (segment == 1 && flag == 0 && bconf == 1) result = 1;
		//else if (segment == 0 && flag == 1 && bconf == 0) result =  (1 - confidence * 0.2);
		//else if (segment == 0 && flag == 0 && bconf == 0) result = 0;
		//else if (segment == 0 && flag == 1 && bconf == 1) result = 0;
		// else if (segment == 0 && flag == 0 && bconf == 1) result = 0;

		if (segment == 1 && flag == 1) result = 1;
		else if (segment == 1 && flag == 0 && bconf == 0) result = confidence;
		else if (segment == 1 && flag == 0 && bconf == 1) result = 1;
		else if (segment == 0 && flag == 1 && bconf == 0) result = (1 - confidence * 0.2);

		return half4(result, 0, 0, 1);
		// return half4(average.xyz, 1.0);
		
	}
	
	v2f_DownSample Vert_DownSample(appdata v)
	{
		v2f_DownSample o;
		// o.vertex = float4(v.vertex.xy, 0.0, 1.0);
		// o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);
		o.vertex = mul (mul(unity_MatrixVP, unity_ObjectToWorld), v.vertex);
		o.texcoord = v.texcoord;
		
		// #if UNITY_UV_STARTS_AT_TOP
		// 	o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
		// #endif
		float2 uv = TRANSFORM_TEX(o.texcoord, _MainTex);
		
		_MainTex_TexelSize *= 0.5;
		o.uv = uv;
		o.uv01.xy = uv - _MainTex_TexelSize * float2(1 + _Offset, 1 + _Offset);//top right
		o.uv01.zw = uv + _MainTex_TexelSize * float2(1 + _Offset, 1 + _Offset);//bottom left
		o.uv23.xy = uv - float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * float2(1 + _Offset, 1 + _Offset);//top left
		o.uv23.zw = uv + float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * float2(1 + _Offset, 1 + _Offset);//bottom right
		
		return o;
	}
	
	half4 Frag_DownSample(v2f_DownSample i): SV_Target
	{
		half4 sum = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.zw);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.zw);
		
		return sum * 0.25;
	}
	ENDHLSL
	
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex Vert_Blend
			#pragma fragment Frag_Blend
			
			ENDHLSL
		}
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex Vert_DownSample
			#pragma fragment Frag_DownSample
			
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex Vert_Blend
			#pragma fragment Frag_AverageSky
			
			ENDHLSL
		}
	}
}


