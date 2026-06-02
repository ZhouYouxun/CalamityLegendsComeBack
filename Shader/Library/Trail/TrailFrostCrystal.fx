// **FrostCrystalTrail.fx - 霜冻冰晶拖尾**
sampler uImage0 : register(s0);  // 拖尾主纹理
sampler uImage1 : register(s1);  // 透明度控制纹理（Fade Map）

float3 uColor;  // 主颜色
float3 uSecondaryColor; // 冰晶颜色
float uOpacity;  // 拖尾透明度
float uBlurAmount;  // 模糊程度
float uTime;  // 全局时间
float uSpeedFactor; // 速度影响拖尾形态
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

// **冰晶粒子动态**
float iceParticle = tex2D(uImage1, coords * 2.5 - float2(uTime * 0.4, uTime * 0.2)).r;

// **计算透明度**
float4 fadeMapColor = tex2D(uImage1, coords - float2(uTime * 0.6, 0));
float opacity = fadeMapColor.r * pow(sin(coords.y * 3.141), 1.5) * uOpacity;

// **冷气模糊效果**
float blurEffect = 1.0 - pow(smoothstep(0.0, 0.6, coords.x), uBlurAmount);

// **颜色渐变（冰蓝 - 纯白）**
float3 frostColor = lerp(uColor, uSecondaryColor, iceParticle);

// **最终颜色计算**
return float4(frostColor * blurEffect, opacity);
}

// **Shader Pass**
technique TrailTechnique{
    pass TrailPass {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}



//public Color TrailColor(float completionRatio)
//{
//    float opacity = Utils.GetLerpValue(1f, 0.7f, completionRatio, true) * Projectile.Opacity;
//    return new Color(120, 200, 255) * opacity; // 冰蓝色拖尾
//}
//public float TrailWidth(float completionRatio)
//{
//    return MathHelper.SmoothStep(8f, 20f, Utils.GetLerpValue(0f, 1f, completionRatio, true));
//}
//public override bool PreDraw(ref Color lightColor)
//{
//    Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY;
//    Vector2 origin = texture.Size() * 0.5f;
//
//    Main.spriteBatch.EnterShaderRegion();
//
//    GameShaders.Misc["ModNamespace:TrailFrostCrystalEffect"]
//        .SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/FabstaffStreak"));
//
//    GameShaders.Misc["ModNamespace:TrailFrostCrystalEffect"]
//        .UseColor(new Color(120, 200, 255)) // 冰蓝色
//        .UseSecondaryColor(new Color(255, 255, 255)) // 纯白（冰晶效果）
//        .Apply();
//
//    PrimitiveRenderer.RenderTrail(Projectile.oldPos,
//        new(TrailWidth, (completionRatio, vertexPos) => TrailColor(completionRatio), (_) = > Projectile.Size * 0.5f, shader: GameShaders.Misc["ModNamespace:TrailFrostCrystalEffect"]),
//        10);
//
//    Main.spriteBatch.ExitShaderRegion();
//
//    Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, 0, 0);
//
//    return false;
//}