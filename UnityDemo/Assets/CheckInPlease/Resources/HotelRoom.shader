Shader "CheckInPlease/ObservedRoom"
{
    Properties
    {
        _Color ("Material color", Color) = (.4,.4,.35,1)
        _SurfaceAtlas ("Painted surface atlas", 2D) = "white" {}
        _Pattern ("Surface pattern", Float) = 0
        _RoomCenter ("Room center (-100 = exterior)", Float) = -100
        _Floor ("Room floor", Float) = 0
        _Camera ("Camera XZ, yaw radians, enabled", Vector) = (0,0,0,0)
        _Sight ("Cos outer, cos inner, range, door open", Vector) = (.866,.92,8.8,0)
        _Observer ("Keeper XZ", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            fixed4 _Color;
            sampler2D _SurfaceAtlas;
            float _Pattern, _RoomCenter, _Floor;
            float4 _Camera, _Sight, _Observer, _RoomSize;
            float4 _Partitions[6];
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float2 uv:TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal); o.uv=v.uv; return o;
            }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p),f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            float cross2(float2 a,float2 b){return a.x*b.y-a.y*b.x;}
            float partitionSight(float2 origin,float2 destination)
            {
                origin.x-=_RoomCenter;destination.x-=_RoomCenter;
                float2 ray=destination-origin;float visibility=1;
                [unroll] for(int i=0;i<6;i++)
                {
                    float2 wall=_Partitions[i].zw-_Partitions[i].xy;
                    float den=cross2(ray,wall);
                    if(abs(den)<.00001)continue;
                    float2 offset=_Partitions[i].xy-origin;
                    float t=cross2(offset,wall)/den,u=cross2(offset,ray)/den;
                    if(t<=.002||t>=.995)continue;
                    float edge=max(-u,u-1)*length(wall);
                    visibility=min(visibility,smoothstep(-.06,.22,edge));
                }
                return visibility;
            }
            float cameraSight(float2 p)
            {
                float2 delta=p-_Camera.xy; float d=length(delta);
                float dotAngle=dot(delta/max(.001,d),float2(sin(_Camera.z),cos(_Camera.z)));
                return _Camera.w*smoothstep(_Sight.x,_Sight.y,dotAngle)*(1-smoothstep(_Sight.z-.8,_Sight.z,d))*partitionSight(_Camera.xy,p);
            }
            float directSight(float2 p)
            {
                if(abs(_Observer.z-_Floor)>.1)return 0;
                float2 observer=_Observer.xy; float d=distance(p,observer);
                float range=(1-smoothstep(7.2,8.2,d))*partitionSight(observer,p);
                if(abs(observer.x-_RoomCenter)<_RoomSize.x-.18 && observer.y>.18 && observer.y<_RoomSize.y) return range;
                if(_Sight.w<.5 || observer.y>=.18 || observer.y< -3.5 || p.y<=0) return 0;
                float t=-observer.y/max(.001,p.y-observer.y);
                float crossing=lerp(observer.x,p.x,t)-_RoomCenter;
                return (1-smoothstep(.62,.76,abs(crossing)))*range;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal);
                float2 planar=abs(n.y)>.5 ? i.world.xz : (abs(n.x)>.5 ? i.world.zy : i.world.xy);
                float fine=hash(floor(planar*95));
                float mottled=noise(planar*3)*.12+noise(planar*13)*.06;
                float textureShade=.83+mottled+fine*.045;
                if(_Pattern>0.5 && _Pattern<1.5)
                {
                    float row=floor(planar.x*5);
                    float2 cell=float2(planar.x*5,planar.y*.8+fmod(row,2)*.5);
                    float2 f=frac(cell);
                    float seam=smoothstep(.0,.028,min(f.x,1-f.x))*smoothstep(.0,.018,min(f.y,1-f.y));
                    float grain=noise(float2(planar.x*50,planar.y*.65));
                    textureShade=(.69+hash(floor(cell))*.19+grain*.14)*lerp(.45,1,seam);
                }
                else if(_Pattern>1.5 && _Pattern<2.5)
                    textureShade=.84+noise(planar*9)*.13+sin(planar.x*82)*sin(planar.y*82)*.035;
                else if(_Pattern>3.5 && _Pattern<4.5)
                {
                    float2 edge=min(i.uv,1-i.uv);
                    float border=step(.045,min(edge.x,edge.y))*step(min(edge.x,edge.y),.09);
                    float diamonds=pow(abs(sin(i.uv.x*48)+cos(i.uv.y*60))*.5,6);
                    textureShade=.7+border*.5+diamonds*.25+noise(planar*40)*.15;
                }
                else if(_Pattern>4.5 && _Pattern<5.5)
                {
                    float2 f=frac(planar*2); float seam=smoothstep(0,.025,min(min(f.x,1-f.x),min(f.y,1-f.y)));
                    textureShade=(.8+hash(floor(planar*2))*.15)*lerp(.5,1,seam);
                }
                if(_Pattern<2.5 || (_Pattern>3.5 && _Pattern<4.5))
                {
                    float2 quadrant=_Pattern<.5?float2(0,.5):_Pattern<1.5?float2(.5,.5):_Pattern<2.5?float2(.5,0):float2(0,0);
                    float2 uv=frac(planar/(_Pattern<.5?3.0:_Pattern<1.5?2.3:1.5));
                    if(_Pattern>3.5)uv=i.uv;
                    float3 surface=tex2D(_SurfaceAtlas,quadrant+.004+uv*.492).rgb;
                    float baseline=_Pattern<.5?.18:_Pattern<1.5?.10:_Pattern<2.5?.18:.052;
                    float detail=dot(surface,float3(.3,.59,.11))/baseline;
                    textureShade*=lerp(1,clamp(detail,.25,1.8),.82);
                }
                float lighting=.47+.45*saturate(dot(n,normalize(float3(-.45,.85,-.4))));
                float3 color=_Color.rgb*textureShade*lighting;
                float cx=_RoomCenter< -50 ? round(i.world.x/10.2)*10.2 : _RoomCenter;
                float warm=exp(-distance(i.world.xz,float2(cx+3.25,6.4))*1.25)*.17;
                warm+=exp(-distance(i.world.xz,float2(cx,-.6))*1.25)*.12;
                color+=_Color.rgb*float3(1,.66,.31)*warm;
                if(_RoomCenter> -50)
                {
                    float camera=cameraSight(i.world.xz),direct=directSight(i.world.xz);
                    color*=lerp(float3(1,1,1),float3(.88,.99,1.01),camera*(1-direct)*.55);
                    color*=max(camera,direct);
                }
                return fixed4(color,1);
            }
            ENDCG
        }
    }
}
