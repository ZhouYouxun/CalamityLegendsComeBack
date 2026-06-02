sampler2D uScreen;  // 画面纹理
float2 uCenter;     // 万花筒中心
float uSegments;    // 区域数
float uRadius;      // 作用范围
float uOpacity;     // 透明度控制

// **旋转坐标**
float2 Rotate(float2 coord, float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return float2(coord.x * c - coord.y * s, coord.x * s + coord.y * c);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
    float2 uv = coords;
    float2 offset = uv - uCenter;
    float dist = length(offset);

    // **超出范围，不影响画面**
    if (dist > uRadius) {
        return tex2D(uScreen, uv);
    }

    float segmentAngle = 6.283185 / uSegments; // 360° / uSegments
    float angle = atan2(offset.y, offset.x);

    angle = fmod(angle, segmentAngle);
    if (angle < 0) angle += segmentAngle;

    float2 mirroredOffset = Rotate(offset, -angle);
    float2 finalCoord = uCenter + mirroredOffset;

    float4 color = tex2D(uScreen, finalCoord);

    // **应用透明度**
    return lerp(tex2D(uScreen, uv), color, uOpacity);
}

// **定义 Shader**
technique KaleidoscopeEffect{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}

// 这个东西先有点问题，先不要用了