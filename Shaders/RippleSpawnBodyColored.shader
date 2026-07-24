// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

	
// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'

//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance

Shader "RippleSpawnBodyColored" //Unlit Transparent Vertex Colored Additive 
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
			GrabPass { }
				Pass 
			{
				//SetTexture [_MainTex] 
				//{
				//	Combine texture * primary
				//}
				
				
				
CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_local _ absorption
#include "UnityCG.cginc"
#include "_ShaderFix.cginc"
#include "_Functions.cginc"
#include "_RippleClip.cginc"

//#pragma profileoption NumTemps=64
//#pragma profileoption NumInstructionSlots=2048

//float4 _Color;


#if defined(SHADER_API_PSSL)
sampler2D _GrabTexture;
#else
sampler2D _GrabTexture : register(s0);
#endif
sampler2D _PalTex;
sampler2D _LevelTex;
sampler2D _NoiseTex2;
sampler2D _PreLevelColorGrab;
uniform float2 _screenSize;
uniform float4 _spriteRect;
uniform float _waterLevel;
uniform float _RAIN;

struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
    float2 scrPos : TEXCOORD1;
    float4 clr : COLOR;
    float2 texCoord : TEXCOORD3;
};

sampler2D _MainTex;
float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
    o.scrPos = ComputeScreenPos(o.pos);
    o.texCoord = iLerp(_spriteRect.xy,_spriteRect.zw,o.scrPos);
    o.clr = v.color;
    return o;
}



half4 frag (v2f i) : SV_Target
{
	clip(tex2D(_MainTex, i.uv).a - 0.5);

	float2 grabPos = i.scrPos;

	float fade = smoothstep(0,.3,i.clr.w);
	fade *= fade;
	grabPos += (tex2D(_NoiseTex2, half2(i.texCoord.x*1.5, i.texCoord.y*0.75 + _RAIN*0.1)).xy + half2(-0.5, -0.5)) * 0.008 * fade;
	grabPos = QuantizeToPixels(grabPos,_screenSize);

	fixed4 grabCol = tex2D(_GrabTexture, grabPos);
	fixed4 color = RippleSpawnColorCustom(i.clr, i.scrPos);

	fixed contrast = abs(mod(lightness(grabCol*color.w)-_RAIN*.1,.2)-.1)/.1;
	contrast = lerp(contrast,1-contrast,color.w);
	fixed4 distortedBody = color*lerp(.5,1,contrast);
	contrast *= color.w;
	distortedBody *= color.w;

	// #if absorption
	// i.clr.x = 1-i.clr.x;
	// float len = length(i.uv*2-1);
	// fade *= (tex2D(_PreLevelColorGrab, i.scrPos) != 0)*smoothstep(1,.5,len);
	// fade = max(fade,iLerpClamp(1,0.5,len+i.clr.x));
	// fade *= i.clr.w;
	// #endif

	return fixed4(lerp(grabCol,distortedBody,i.clr.w*i.clr.w*.75*(.5+contrast*.5)*fade*color.w).xyz,fade);
	// return fixed4(0.5, 0.5, 0.5, fade);
}
ENDCG
				
				
				
			}
		} 
	}
}
