Shader "Sttplay/Split" {
	Properties{
		_Tex("Tex", 2D) = "white" {}
	_AlphaDir("AlphaDir", int) = 0
	_MainTex("MainTex", 2D) = "white" {}
	}
		SubShader{
			Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
			Pass{
				Tags { "LightMode" = "ForwardBase" }
				//open z write
				ZWrite On
		//set alpha
		//Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM

		#pragma vertex vert
		#pragma fragment frag
		#include "Lighting.cginc"

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _Tex;
		float4 _Tex_TexelSize;
		int _AlphaDir;

		struct a2v {
			float4 vertex : POSITION;
			float4 texcoord : TEXCOORD0;
		};

		struct v2f {
			float4 pos : SV_POSITION;
			float4 uv : TEXCOORD2;
		};

		float4 CalcUVVertical(float2 texelSize, float2 uv)
		{
			float4 result = uv.xyxy;
			float offset = texelSize.y * 1.5;
			result.y = 1 - lerp(0.5 - offset, 0.0 + offset, uv.y);
			result.w = 0.5 - lerp(0.5 - offset, 0.0 + offset, uv.y);
			return result;
		}

		float4 CalcUVHorizontal(float2 texelSize, float2 uv)
		{
			float4 result = uv.xyxy;
			float offset = texelSize.x * 1.5;
			result.x = lerp(0.0 + offset, 0.5 - offset, uv.x);
			result.z = result.x + 0.5;
			return result;
		}

		v2f vert(a2v v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = v.texcoord;
			o.uv = _AlphaDir > 0 ? CalcUVVertical(_Tex_TexelSize, o.uv.xy) : CalcUVHorizontal(_Tex_TexelSize, o.uv.xy);
			return o;
		}

		fixed4 frag(v2f i) : SV_Target{
			fixed4 rgba = tex2D(_Tex, i.uv.xy);
			fixed4 alpha = tex2D(_Tex, i.uv.zw);
			rgba.a = (alpha.r + alpha.g + alpha.b) / 3.0;
			return rgba;
		}

		ENDCG
	}
	
	}
		FallBack "Diffuse"
}