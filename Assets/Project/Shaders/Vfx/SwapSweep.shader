Shader "Vfx/Swap Sweep"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _SweepProgress ("Sweep Progress", Range(0, 1)) = 0
        _SweepWidth ("Sweep Tip Width Px", Float) = 24
        _SweepSoftness ("Sweep Softness", Range(0.001, 0.25)) = 0.08
        _GlowStrength ("Glow Strength", Range(0, 3)) = 1.4
        _SweepVertical ("Sweep Vertical", Float) = 0
        _TrailLength ("Trail Length Px", Float) = 72
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _SweepAxisLength ("Sweep Axis Length Px", Float) = 100
        _CrossAxisLength ("Cross Axis Length Px", Float) = 20
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 mask : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            float _SweepProgress;
            float _SweepWidth;
            float _SweepSoftness;
            float _GlowStrength;
            float _SweepVertical;
            float _TrailLength;
            float _Dissolve;
            float _SweepAxisLength;
            float _CrossAxisLength;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 vertexPosition = UnityObjectToClipPos(input.vertex);
                output.vertex = vertexPosition;
                output.uv = input.texcoord;

                float2 pixelSize = vertexPosition.w;
                pixelSize /= abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                output.mask = half4(
                    input.vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
                    0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                {
                    input.color.rgb = UIGammaToLinear(input.color.rgb);
                }

                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float axisLen = max(_SweepAxisLength, 1.0);
                float progress = saturate(_SweepProgress);
                float softness = max(_SweepSoftness, 0.02);

                float coord = lerp(input.uv.x, input.uv.y, saturate(_SweepVertical));
                float crossCoord = lerp(input.uv.y, input.uv.x, saturate(_SweepVertical));

                float trailNorm = max(_TrailLength, 1.0) / axisLen;
                float headRadius = max(_SweepWidth, 1.0) / axisLen * (1.0 + softness * 2.2);

                float spawn = smoothstep(0.0, 0.14, progress);
                float despawn = 1.0 - smoothstep(0.86, 1.0, progress);
                float life = spawn * despawn;

                float tipPos = progress;
                float fromHead = coord - tipPos;

                float headSigma = max(headRadius * headRadius * 0.42, 0.00001);
                float ball = exp(-(fromHead * fromHead) / headSigma);

                float wakeDecay = max(trailNorm * 0.36, 0.001);
                float behind = max(-fromHead, 0.0);
                float wakeBlend = smoothstep(headRadius * 0.55, -headRadius * 0.05, fromHead);
                float wake = exp(-behind / wakeDecay) * wakeBlend;
                float body = ball + wake * (1.0 - ball * 0.55);

                float originPad = headRadius * 1.1;
                float originFade = smoothstep(-originPad, originPad * 0.65, coord);
                float aheadDist = max(fromHead, 0.0);
                float aheadMask = 1.0 - smoothstep(headRadius * 1.05, headRadius * 2.4, aheadDist);
                float endFade = 1.0 - smoothstep(0.72, 1.02, progress)
                    * smoothstep(1.0 - headRadius * 1.4, 1.0 + headRadius * 0.2, coord);

                float axisMask = originFade * aheadMask * endFade;

                float crossDist = abs(crossCoord - 0.5) * 2.0;
                float crossFade = exp(-(crossDist * crossDist) / (0.4 + softness));

                float dissolveFade = 1.0 - saturate(_Dissolve);
                float mask = life * axisMask * crossFade * dissolveFade;

                half bodyGlow = body * 1.55 * _GlowStrength * mask;
                half coreMix = saturate(ball * 1.35);
                half3 hotCore = half3(1.0, 0.93, 0.68);
                half3 trailTint = input.color.rgb;

                fixed4 color = input.color;
                color.rgb = lerp(trailTint * bodyGlow, hotCore, coreMix * mask * 0.72);
                color.a = bodyGlow * input.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                half2 clipMask = saturate(
                    (_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                color.a *= clipMask.x * clipMask.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
