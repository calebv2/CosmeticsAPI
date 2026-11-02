Shader "CosmeticsAPI/Creator Lit"
{
 Properties
 {
  _Color("Tint",Color)=(1,1,1,1)
  _MainTex("Colour texture",2D)="white"{}
  _BumpMap("Normal map",2D)="bump"{}
  _Metallic("Metal",Range(0,1))=0
  _Glossiness("Smoothness",Range(0,1))=.2
 }
 SubShader
 {
  Tags {"RenderType"="Opaque"}
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.5
  #pragma multi_compile_instancing
  #pragma multi_compile __ STEREO_INSTANCING_ON UNITY_SINGLE_PASS_STEREO
  sampler2D _MainTex,_BumpMap;fixed4 _Color;half _Metallic,_Glossiness;
  struct Input {float2 uv_MainTex;float4 color:COLOR;};
  void surf(Input i,inout SurfaceOutputStandard o)
  {o.Albedo=tex2D(_MainTex,i.uv_MainTex).rgb*_Color.rgb*i.color.rgb;o.Normal=UnpackNormal(tex2D(_BumpMap,i.uv_MainTex));o.Metallic=_Metallic;o.Smoothness=_Glossiness;o.Alpha=1;}
  ENDCG
 }
 Fallback "Diffuse"
}
