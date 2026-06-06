Shader "UI/Scrolling Stripes Fill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Stripes)]
        _StripeColorA ("Stripe Color A", Color) = (1, 1, 1, 1)
        _StripeColorB ("Stripe Color B", Color) = (0.75, 0.75, 0.75, 1)
        _StripeSize ("Stripe Size (px)", Float) = 16
        _ScrollSpeed ("Scroll Speed (px/s)", Float) = 48
        _StripeAngle ("Stripe Angle (deg)", Range(0, 360)) = 45
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
            Name "Default"

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
                float2 texcoord : TEXCOORD0;
                float2 localPosition : TEXCOORD1;
                float4 mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            fixed4 _StripeColorA;
            fixed4 _StripeColorB;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            float _StripeSize;
            float _ScrollSpeed;
            float _StripeAngle;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 vertexPosition = UnityObjectToClipPos(input.vertex);
                output.vertex = vertexPosition;
                output.localPosition = input.vertex.xy;
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);

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
                half4 spriteSample = tex2D(_MainTex, input.texcoord) + _TextureSampleAdd;

                float stripeSize = max(_StripeSize, 0.001);
                float angleRadians = _StripeAngle * 0.0174532925;
                float2 stripeDirection = float2(cos(angleRadians), sin(angleRadians));

                float stripeCoord = dot(input.localPosition, stripeDirection) + _Time.y * _ScrollSpeed;
                float stripeBand = floor(stripeCoord / stripeSize);
                half useStripeB = stripeBand - 2.0 * floor(stripeBand * 0.5);

                half4 stripeSample = lerp(_StripeColorA, _StripeColorB, useStripeB);
                fixed4 color = stripeSample * spriteSample * input.color;

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
