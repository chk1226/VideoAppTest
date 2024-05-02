Shader "Sttplay/XYUVX" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_YTex("YTex", 2D) = "white" {}
		_UTex("UTex", 2D) = "white" {}
		_VTex("VTex", 2D) = "white" {}
		_MainTex("MainTex", 2D) = "white" {}
		_OFFSET("OFFSET", vector) = (-0.0627451017, -0.501960814, -0.501960814, 0)
		_RCOEFF("RCOEFF", vector) = (1.1644, 0.000, 1.596, 0)
		_GCOEFF("GCOEFF", vector) = (1.1644, -0.3918, -0.813, 0)
		_BCOEFF("BCOEFF", vector) = (1.1644, 2.0172, 0.000, 0)
	}
		SubShader{
			Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
			Pass{
				Tags { "LightMode" = "ForwardBase" }
				//open z write
				ZWrite On
		//mix mode
		//Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM

		#pragma vertex vert
		#pragma fragment frag
		#include "Lighting.cginc"

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		fixed4 _Color;
		sampler2D _YTex;
		float4 _YTex_ST;

		sampler2D _UTex;
		float4 _UTex_ST;

		sampler2D _VTex;
		float4 _VTex_ST;

		vector _OFFSET;
		vector _RCOEFF;
		vector _GCOEFF;
		vector _BCOEFF;

		struct a2v {
			float4 vertex : POSITION;
			float4 texcoord : TEXCOORD0;
		};

		struct v2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD2;
		};

		v2f vert(a2v v) {
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			//set uv texcoord and offset
			o.uv = v.texcoord.xy * _YTex_ST.xy + _YTex_ST.zw;
			//set y
			o.uv.y *= -1;
			return o;
		}

		fixed4 GammaToLinear4(fixed4 src)
		{
			return fixed4(GammaToLinearSpace(src.rgb), src.a);
		}
		fixed4 frag(v2f i) : SV_Target{
			
			fixed3 yuv;
			fixed3 rgb;
			yuv.x = tex2D(_YTex, i.uv).r;
			yuv.y = tex2D(_UTex, i.uv).r;
			yuv.z = tex2D(_VTex, i.uv).r;
			yuv += _OFFSET;
			
			rgb.r = dot(yuv, _RCOEFF);
			rgb.g = dot(yuv, _GCOEFF);
			rgb.b = dot(yuv, _BCOEFF);
#ifdef UNITY_COLORSPACE_GAMMA
			return fixed4(rgb * _Color.rgb, _Color.a);
#else
			return GammaToLinear4(fixed4(rgb * _Color.rgb, _Color.a));
#endif
		}

		ENDCG
	}

	}
		FallBack "Diffuse"
}
