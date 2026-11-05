Shader "CheckInPlease/Matte" {
 Properties { _Color ("Color", Color) = (1,1,1,1) _MainTex ("Grain", 2D) = "white" {} }
 SubShader {
  Tags { "RenderType"="Opaque" }
  LOD 150
  CGPROGRAM
  #pragma surface surf Lambert fullforwardshadows
  #pragma target 3.0
  sampler2D _MainTex; fixed4 _Color;
  struct Input { float2 uv_MainTex; float3 worldPos; };
  void surf (Input IN, inout SurfaceOutput o) {
   float grain=frac(sin(dot(floor(IN.worldPos*120),float3(12.9898,78.233,45.164)))*43758.5453);
   o.Albedo=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb*(.9+grain*.17);o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Diffuse"
}
