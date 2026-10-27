Shader "Cromware/PatternPainter/ScenePreviewTransparent"
{
    Properties
    {
        _Color ("Color", Color) = (0,1,0,0.5)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Lighting Off

        Pass
        {
            Color [_Color]
        }
    }
}