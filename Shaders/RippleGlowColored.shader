// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

	
// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'

//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance

Shader "RippleGlowColored" //Unlit Transparent Vertex Colored Additive 
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
sampler2D _NoiseTex2;
uniform float2 _screenSize;
uniform float4 _spriteRect;
uniform float _RAIN;


struct v2f {
    float4  pos : SV_POSITION;
    float2  uv : TEXCOORD0;
    float2 scrPos : TEXCOORD1;
    float4 clr : COLOR;
};

float4 _MainTex_ST;

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);
    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
    o.scrPos = ComputeScreenPos(o.pos);
    o.clr = v.color;
    return o;
}



half4 frag (v2f i) : SV_Target
{
     float dist = clamp(distance(i.uv.xy, half2(0.5, 0.5))*2, 0, 1);
     half effect = pow((1-pow(dist,1.5)), 3.5);
     
     half2 grabPos = half2(i.scrPos.x, i.scrPos.y);
     grabPos.x = (floor(grabPos.x*_screenSize.x)+0.5)/_screenSize.x;
     grabPos.y = (floor(grabPos.y*_screenSize.y)+0.5)/_screenSize.y;
	 fixed4 color = i.clr;
	 #if RIPPLE || ripple_other_side
		#if ripple_other_side
			color = fixed4(i.clr.xyz,1);
		#else
			color = fixed4(i.clr.xyz,0.4);
		#endif
	 #endif
     color.xyz*=.5;
     
     
     half4 grabCol = tex2D(_GrabTexture, grabPos);
     float d = distance(lightness(grabCol),lightness(color));
     float highlight = smoothstep(.7,.6,d);

     i.clr.w=clamp(i.clr.w*2,0,.5);
     half4 glow = lerp(grabCol, color*(1+highlight), d*i.clr.w*(.5+i.clr.w*.5));

     return half4(glow.xyz, effect*d*color.w);

}
ENDCG
				
				
				
			}
		} 
	}
}
