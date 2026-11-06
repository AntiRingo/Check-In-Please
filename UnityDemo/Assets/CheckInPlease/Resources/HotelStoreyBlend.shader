Shader "CheckInPlease/StoreyTransition"
{
    Properties { _MainTex ("Full scene",2D)="white"{} _StairTex ("Stairwell",2D)="white"{} _FloorOpacity ("Floor opacity",Float)=1 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex,_StairTex;
            float4 _MainTex_TexelSize;
            float _FloorOpacity;
            struct v2f { float4 position:SV_POSITION;float2 sceneUV:TEXCOORD0;float2 stairUV:TEXCOORD1; };
            v2f vert(appdata_img v)
            {
                v2f o;o.position=UnityObjectToClipPos(v.vertex);o.sceneUV=v.texcoord;o.stairUV=v.texcoord;
                #if UNITY_UV_STARTS_AT_TOP
                if(_MainTex_TexelSize.y<0)o.stairUV.y=1-o.stairUV.y;
                #endif
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {return lerp(tex2D(_StairTex,i.stairUV),tex2D(_MainTex,i.sceneUV),_FloorOpacity);}
            ENDCG
        }
    }
}
