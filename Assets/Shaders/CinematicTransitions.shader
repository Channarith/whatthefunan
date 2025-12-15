Shader "WhatTheFunan/CinematicTransitions"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _TransitionTex ("Transition Texture", 2D) = "white" {}
        _Progress ("Transition Progress", Range(0, 1)) = 0
        _Color ("Fade Color", Color) = (0, 0, 0, 1)
        _Softness ("Edge Softness", Range(0.001, 0.5)) = 0.1
        
        [KeywordEnum(Fade, Wipe, Circle, Dissolve, Pixelate)] _TransitionType ("Transition Type", Float) = 0
        _WipeAngle ("Wipe Angle", Range(0, 360)) = 0
        _CircleCenter ("Circle Center", Vector) = (0.5, 0.5, 0, 0)
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
            "IgnoreProjector" = "True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _TRANSITIONTYPE_FADE _TRANSITIONTYPE_WIPE _TRANSITIONTYPE_CIRCLE _TRANSITIONTYPE_DISSOLVE _TRANSITIONTYPE_PIXELATE
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            sampler2D _TransitionTex;
            float4 _MainTex_ST;
            float _Progress;
            float4 _Color;
            float _Softness;
            float _WipeAngle;
            float4 _CircleCenter;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            // Fade transition - simple alpha blend
            float4 transitionFade(float2 uv, float progress)
            {
                return float4(_Color.rgb, progress);
            }
            
            // Wipe transition - directional wipe
            float4 transitionWipe(float2 uv, float progress)
            {
                float angle = radians(_WipeAngle);
                float2 dir = float2(cos(angle), sin(angle));
                
                // Calculate wipe position
                float wipePos = dot(uv - 0.5, dir) + 0.5;
                float alpha = smoothstep(progress - _Softness, progress + _Softness, wipePos);
                
                return float4(_Color.rgb, 1 - alpha);
            }
            
            // Circle transition - expanding/contracting circle
            float4 transitionCircle(float2 uv, float progress)
            {
                float2 center = _CircleCenter.xy;
                float dist = distance(uv, center);
                
                // Max distance from center to corner
                float maxDist = distance(center, float2(0, 0));
                maxDist = max(maxDist, distance(center, float2(1, 0)));
                maxDist = max(maxDist, distance(center, float2(0, 1)));
                maxDist = max(maxDist, distance(center, float2(1, 1)));
                
                float radius = progress * maxDist * 1.5;
                float alpha = smoothstep(radius - _Softness, radius + _Softness, dist);
                
                return float4(_Color.rgb, 1 - alpha);
            }
            
            // Dissolve transition - noise-based dissolve
            float4 transitionDissolve(float2 uv, float progress)
            {
                float noise = tex2D(_TransitionTex, uv).r;
                float alpha = smoothstep(progress - _Softness, progress + _Softness, noise);
                
                return float4(_Color.rgb, 1 - alpha);
            }
            
            // Pixelate transition - pixelation effect
            float4 transitionPixelate(float2 uv, float progress)
            {
                // Increase pixel size as progress increases
                float pixelSize = lerp(1, 100, progress);
                float2 pixelatedUV = floor(uv * pixelSize) / pixelSize;
                
                float4 col = tex2D(_MainTex, pixelatedUV);
                float fadeOut = smoothstep(0.7, 1.0, progress);
                
                return lerp(col, _Color, fadeOut);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 result;
                
                #if _TRANSITIONTYPE_FADE
                    result = transitionFade(i.uv, _Progress);
                #elif _TRANSITIONTYPE_WIPE
                    result = transitionWipe(i.uv, _Progress);
                #elif _TRANSITIONTYPE_CIRCLE
                    result = transitionCircle(i.uv, _Progress);
                #elif _TRANSITIONTYPE_DISSOLVE
                    result = transitionDissolve(i.uv, _Progress);
                #elif _TRANSITIONTYPE_PIXELATE
                    result = transitionPixelate(i.uv, _Progress);
                #else
                    result = transitionFade(i.uv, _Progress);
                #endif
                
                return result;
            }
            ENDCG
        }
    }
    
    FallBack "UI/Default"
}

