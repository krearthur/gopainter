Shader "Custom/Xray"
{
    Properties
    {
		_Color("Color", Color) = (1, 1, 1, 1)
		_Emission("Emission", Color) = (0, 0, 0, 0)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		LOD 100

        Pass
        {   
			Cull Off
			ZTest Always
			Blend SrcAlpha OneMinusSrcAlpha
			
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
				float4 normal: NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
				float3 worldNormal: TEXCOORD1;
				float3 worldPosition: TEXCOORD2;
				UNITY_FOG_COORDS(3)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float4 _Color;
			float4 _Emission;

            v2f vert (appdata v)
            {
                v2f o;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldPosition = mul(unity_ObjectToWorld, v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				//fixed3 lightDifference = i.worldPosition - _LightPoint.xyz;
				fixed3 lightDirection = _WorldSpaceLightPos0.xyz; 
				fixed intensity = .5 * dot(lightDirection, i.worldNormal) + 0.5;
				fixed4 col = tex2D(_MainTex, i.uv);
				col.rgb *= intensity;
				col += _Emission;
				col *= _Color;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
