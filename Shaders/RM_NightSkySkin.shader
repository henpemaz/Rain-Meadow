Shader "RM_NightSkySkin" //Unlit Transparent Vertex Colored
{
	Properties
	{
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	
	Category
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		ZWrite Off
		//Alphatest Greater 0
		Blend SrcAlpha OneMinusSrcAlpha
		Fog { Color(0,0,0,0) }
		Lighting Off
		Cull Off //we can turn backface culling off because we know nothing will be facing backwards

		BindChannels
		{
			Bind "Vertex", vertex
			Bind "texcoord", texcoord 
			Bind "Color", color 
		}

		SubShader
		{
			Pass
			{
				CGPROGRAM
#pragma target 4.0
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

struct v2f
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float4 uv2 : TEXCOORD1;
	float4 clr : COLOR0;
};

uniform float _RAIN;

sampler2D _MainTex;
float4 _MainTex_ST;
sampler2D _RM_NightSky;
sampler2D _RM_NightSky_glow;
float4 _RM_NightSky_ST;
float4 _RM_NightSky_TexelSize;
//half4 _RM_NightSky_HDR;

v2f vert (appdata_full v)
{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
	o.uv2 = ComputeScreenPos(o.pos);
	o.clr = v.color;
	return o;
}

half4 frag (v2f input) : SV_Target
{
	float2 textureCoordinate = input.uv2.xy / input.uv2.w;
	textureCoordinate = textureCoordinate * _ScreenParams.xy * _RM_NightSky_TexelSize.xy;// * 2.0;
	fixed4 mainColor = tex2D (_MainTex, input.uv);
	if(mainColor.a == 0) return mainColor;

	half glowAmount = saturate(0.1 + (0.9 + 0.1*sin(55*_RAIN)) * (0.6 + 0.4*sin(17*_RAIN)) * sin(2.0*_RAIN) * sin((1.0/3.0)*_RAIN));
	half4 texColor = half4(tex2D(_RM_NightSky, textureCoordinate).rgb + glowAmount * tex2D(_RM_NightSky_glow, textureCoordinate).rgb,1);
	if(any(input.clr.xyz)){
		input.clr.xyz = 0.5 + input.clr.xyz/(max(input.clr.x,max(input.clr.y,input.clr.z))); // cheap and innacurate high-value color
	}
	else{
		input.clr.xyz = 1;
	}
	return mainColor * texColor * input.clr;
}
				ENDCG
			}
		} 
	}
}



//Blend SrcAlpha OneMinusSrcAlpha 