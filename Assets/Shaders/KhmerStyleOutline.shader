Shader "WhatTheFunan/KhmerStyleOutline"
{
    // Character outline shader with Khmer-inspired golden accents
    // Used for character highlighting and selection
    
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Main Color", Color) = (1, 1, 1, 1)
        
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.78, 0.63, 0.15, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
        
        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (1, 0.9, 0.5, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 1
        
        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(0, 5)) = 2
        
        [Header(Animation)]
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1
        _PulseIntensity ("Pulse Intensity", Range(0, 1)) = 0.2
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200
        
        // Outline pass
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "Always" }
            
            Cull Front
            ZWrite On
            ColorMask RGB
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
            };
            
            float _OutlineWidth;
            float4 _OutlineColor;
            float _PulseSpeed;
            float _PulseIntensity;
            
            v2f vert(appdata v)
            {
                v2f o;
                
                // Animated pulse
                float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                float animatedWidth = _OutlineWidth * (1 + pulse * _PulseIntensity);
                
                // Expand vertices along normals
                float3 norm = normalize(v.normal);
                v.vertex.xyz += norm * animatedWidth;
                
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
        
        // Main surface pass
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }
            
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                SHADOW_COORDS(4)
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _RimColor;
            float _RimPower;
            float _RimIntensity;
            float _FresnelPower;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                TRANSFER_SHADOW(o);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Base color
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Rim lighting
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.viewDir);
                float rim = 1.0 - saturate(dot(viewDir, normal));
                rim = pow(rim, _RimPower) * _RimIntensity;
                
                // Fresnel
                float fresnel = pow(1.0 - saturate(dot(viewDir, normal)), _FresnelPower);
                
                // Apply rim
                col.rgb += _RimColor.rgb * rim;
                
                // Shadow
                float shadow = SHADOW_ATTENUATION(i);
                col.rgb *= shadow * 0.5 + 0.5;
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}

