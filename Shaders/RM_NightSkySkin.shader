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

struct appdata
{
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
};

struct v2f
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float4 uv2 : TEXCOORD1;
	float4 clr : COLOR0;
};

sampler2D _MainTex;
float4 _MainTex_ST;
sampler2D _RM_NightSky;
float4 _RM_NightSky_ST;
float4 _RM_NightSky_TexelSize;

v2f vert (appdata_full v)
{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
	o.uv2 = ComputeScreenPos(o.pos);
	o.clr = v.color;
	return o;
}

half4 frag (v2f i) : SV_Target
{
	float2 textureCoordinate = i.uv2.xy / i.uv2.w;
	textureCoordinate = textureCoordinate * _ScreenParams.xy * _RM_NightSky_TexelSize.xy * 2.0;
	//float aspect = _ScreenParams.x / _ScreenParams.y;
    //textureCoordinate.x = textureCoordinate.x * aspect;
	//textureCoordinate = TRANSFORM_TEX(textureCoordinate, _RM_NightSky);
	fixed4 color = tex2D (_MainTex, i.uv) * tex2D (_RM_NightSky, textureCoordinate) * i.clr;
	return color;
}
				ENDCG
			}
		} 
	}
}



//Blend SrcAlpha OneMinusSrcAlpha 