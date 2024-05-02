Shader "Sttplay/ABGR" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_Tex("Tex", 2D) = "white" {}
	_MainTex("MainTex", 2D) = "white" {}
	}
		SubShader{
			Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
			Pass{
				Tags { "LightMode" = "ForwardBase" }
				//open z write
				ZWrite On
		//set mix
		//Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM

		#pragma vertex vert
		#pragma fragment frag
		#include "Lighting.cginc"

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		fixed4 _Color;
		sampler2D _Tex;
		float4 _Tex_ST;

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
			o.uv = v.texcoord.xy * _Tex_ST.xy + _Tex_ST.zw;
			//set y
			o.uv.y *= -1;
			return o;
		}

		fixed4 GammaToLinear4(fixed4 src)
		{
			return fixed4(GammaToLinearSpace(src.rgb), src.a);
		}

		fixed4 frag(v2f i) : SV_Target{
				fixed4 rgba = tex2D(_Tex, i.uv);
#ifdef UNITY_COLORSPACE_GAMMA
		return rgba.abgr * _Color;
#else
		return GammaToLinear4(rgba.abgr * _Color);
#endif
		}

		ENDCG
	}

	}
		FallBack "Diffuse"
}
