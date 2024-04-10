// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

	
// Upgrade NOTE: replaced 'samplerRECT' with 'sampler2D'

//from http://forum.unity3d.com/threads/68402-Making-a-2D-game-for-iPhone-iPad-and-need-better-performance


Shader "RM_SceneHidden" //Unlit Transparent Vertex Colored Additive 
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
		//GrabPass { }
		
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


//#pragma profileoption NumTemps=64
//#pragma profileoption NumInstructionSlots=2048

//float4 _Color;
sampler2D _MainTex;
//sampler2D _LevelTex;
//sampler2D _DpTex;
//sampler2D _PalTex;
uniform float _BlurDepth;
uniform float2 _MenuCamPos;
uniform float _BlurRange;

//#if defined(SHADER_API_PSSL)
//sampler2D _GrabTexture;
//#else
//sampler2D _GrabTexture : register(s0);
//#endif

uniform float _RAIN;

//uniform float4 _spriteRect;
//uniform float2 _screenSize;


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
   half2 spriteSize = half2(lerp(1, 1400, i.clr.x), lerp(1, 800, i.clr.y));
   float spriteDepth = lerp(0, 10, i.clr.z);

   half dp = 1-tex2D(_MainTex, half2(i.uv.x, i.uv.y/2)).x;


  // half2 getPos = lerp(half2(i.uv.x, 0.5+i.uv.y/2), half2(_MenuCamPos.x, 0.5 + _MenuCamPos.y/2), dp*0.025);
  half2 getPos = half2(i.uv.x, 0.5+i.uv.y/2);
 // getPos.x -= ((0.5 - i.scrPos.x) * dp*50) / spriteSize.x;
//  getPos.y -= ((0.5 - i.scrPos.y) * dp*50) / spriteSize.y;
   getPos.x -= (_MenuCamPos.x - i.uv.x) * dp*0.025;/// spriteSize.x;// / spriteDepth;
   getPos.y -= (_MenuCamPos.y - i.uv.y) * dp*0.025;/// spriteSize.y;// / spriteDepth;
  
  getPos.y = max(0.5 + 1.0/spriteSize.y, getPos.y);

  // dp = 1-tex2D(_MainTex, half2(getPos.x, getPos.y-0.5)).x;

   half4 getCol = tex2D(_MainTex, getPos);
   float horFac = spriteSize.y / spriteSize.x;
    
  //  half red = 0;
  //half add = 0;
  float div = 1.0;
  float coef = 1;
  float fI = 1.0;
  float _BlurAmount = clamp(abs((spriteDepth+dp*2) - _BlurDepth)*_BlurRange, 0, 1)*0.001;
  half4 texcol = half4(0,0,0,0);
    
#if defined(SHADER_API_SWITCH)
    for (int j = 0; j < 2; j++) {
#else
    for (int j = 0; j < 5; j++) {
#endif
    	fI++;
    	coef*=0.92;
    	
    	if(getPos.y - fI * _BlurAmount > 0.5 + 1.0/spriteSize.y){
    	texcol = tex2D(_MainTex, float2(getPos.x, getPos.y - fI * _BlurAmount));
    	getCol += texcol * coef * texcol.w;
    	div += coef;// * texcol.w;
    	}
    	
    	texcol = tex2D(_MainTex, float2(getPos.x - fI * _BlurAmount * horFac, getPos.y));
    	getCol += texcol * coef * texcol.w;
    	div += coef;// * texcol.w;
    	
    	texcol = tex2D(_MainTex, float2(getPos.x + fI * _BlurAmount * horFac, getPos.y));
  	    getCol += texcol * coef * texcol.w;
    	div += coef;// * texcol.w;
    	
    	texcol = tex2D(_MainTex, float2(getPos.x, getPos.y + fI * _BlurAmount));
  	    getCol += texcol * coef * texcol.w;
    	div += coef;// * texcol.w;
    	//alpha += texcol.w;
    	//alphaDiv += coef;
    	
    	if(getPos.y - fI * _BlurAmount * 0.72 > 0.5 + 1.0/spriteSize.y){
    	texcol = tex2D(_MainTex, float2(getPos.x - fI * _BlurAmount * 0.72 * horFac, getPos.y - fI * _BlurAmount * 0.72));
	    getCol += texcol * coef * texcol.w;
    	div += coef;// * texcol.w;
    	}
    	
    	if(getPos.y - fI * _BlurAmount * 0.72 > 0.5 + 1.0/spriteSize.y){
    	texcol = tex2D(_MainTex, float2(getPos.x + fI * _BlurAmount * 0.72 * horFac, getPos.y - fI * _BlurAmount * 0.72));
    	getCol += texcol * coef * texcol.w;
    	div += coef;// * texcol.w;
    	}
    	
    	texcol = tex2D(_MainTex, float2(getPos.x - fI * _BlurAmount * 0.72 * horFac, getPos.y + fI * _BlurAmount * 0.72));
        getCol += texcol * coef * texcol.w;
    	div += coef;// * texcol.w;
    	
    	texcol = tex2D(_MainTex, float2(getPos.x + fI * _BlurAmount * 0.72 * horFac, getPos.y + fI * _BlurAmount * 0.72));
    	getCol += texcol * coef * texcol.w;
    	div += coef;// * texcol.w;
    }
    
 
    
     getCol /= div;

	 getCol.x = 0.0;
	 getCol.y = 0.0;
	 getCol.z = 0.0;
     getCol.w *= i.clr.w;
     
     return getCol;

}
ENDCG
				
				
				
			}
		} 
	}
}