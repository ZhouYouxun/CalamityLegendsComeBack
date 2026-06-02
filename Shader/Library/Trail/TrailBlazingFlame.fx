// **TrailBlazingFlame.fx - 炽热火焰拖尾**
sampler uImage0 : register(s0);  // 拖尾主纹理
sampler uImage1 : register(s1);  // 透明度控制纹理（Fade Map）

float3 uColor;  // 火焰主颜色（橙色）
float3 uSecondaryColor; // 火焰燃尽颜色（红黑）
float uOpacity;  // 拖尾透明度
float uTime;  // 全局时间
float uFlameIntensity; // 火焰摆动强度
matrix uWorldViewProjection;  // 变换矩阵

// **输入结构**
struct VertexShaderInput {
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

// **输出结构**
struct VertexShaderOutput {
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

// **顶点着色器**
VertexShaderOutput VertexShaderFunction(VertexShaderInput input) {
    VertexShaderOutput output;
    output.Position = mul(input.Position, uWorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

// **工具函数：线性插值**
float InverseLerp(float a, float b, float x) {
    return saturate((x - a) / (b - a));
}

// **像素着色器**
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0{
    float2 coords = input.TextureCoordinates.xy;

// **修正 UV 坐标，防止拉伸**
coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;

// **火焰摆动效果**
float flameMotion = sin(coords.y * 10.0 + uTime * 5.0) * uFlameIntensity;

// **透明度控制**
float4 fadeMapColor = tex2D(uImage1, coords - float2(uTime * 0.6, 0));
float opacity = fadeMapColor.r * pow(sin(coords.y * 3.141 + uTime * 1.5), 1.8) * uOpacity;

// **火焰颜色变化（橙色 → 红色 → 黑色）**
float3 fireColor = lerp(uColor, uSecondaryColor, InverseLerp(0.2, 0.8, coords.x));

// **最终颜色计算**
return float4(fireColor * (1.0 + flameMotion * 0.2), opacity);
}

// **Shader Pass**
technique TrailTechnique{
    pass TrailPass {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}



//public override bool PreDraw(ref Color lightColor) {
//    Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY;
//    Vector2 origin = texture.Size() * 0.5f;
//
//    Main.spriteBatch.EnterShaderRegion();
//    GameShaders.Misc["ModNamespace:TrailBlazingFlame"]
//        .SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/FabstaffStreak"))
//        .UseColor(new Color(255, 140, 0)) // 橙色火焰
//        .UseSecondaryColor(new Color(150, 30, 30)) // 炭化红黑
//        .Apply();
//
//    PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(TrailWidth, (completionRatio, vertexPos) => TrailColor(completionRatio), (_) = > Projectile.Size * 0.5f, shader: GameShaders.Misc["ModNamespace:TrailBlazingFlame"]), 10);
//    Main.spriteBatch.ExitShaderRegion();
//
//    Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, 0, 0);
//    return false;
//}
//
//public Color TrailColor(float completionRatio) {
//    float opacity = Utils.GetLerpValue(1f, 0.5f, completionRatio, true) * Projectile.Opacity;
//    return new Color(255, 140, 0) * opacity; // 橙色火焰
//}
//
//public float TrailWidth(float completionRatio) {
//    return MathHelper.SmoothStep(12f, 25f, Utils.GetLerpValue(0f, 1f, completionRatio, true));
//}








