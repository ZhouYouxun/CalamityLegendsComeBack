// **TrailWarpDistortion.fx - 空间扭曲拖尾**
sampler uImage0 : register(s0);  // 拖尾主纹理
sampler uImage1 : register(s1);  // 透明度控制纹理（Fade Map）
sampler uBackground : register(s2); // 背景纹理（用于折射效果）

float3 uColor;  // 主要颜色（空间能量）
float uOpacity;  // 拖尾透明度
float uTime;  // 全局时间
float uWarpIntensity; // 扭曲强度
float uDistortionSpeed; // 速度影响扭曲程度
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

// **像素着色器**
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0{
    float2 coords = input.TextureCoordinates.xy;

// **修正 UV 坐标，防止拉伸**
coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;

// **时间控制空间抖动**
float timeWarp = sin(uTime * 2.0 + coords.x * 5.0) * 0.05;

// **计算透明度（类似黑洞事件视界）**
float4 fadeMapColor = tex2D(uImage1, coords - float2(uTime * 0.4, 0));
float opacity = fadeMapColor.r * pow(sin(coords.y * 3.141 + uTime * 2.0), 2.0) * uOpacity;

// **空间扭曲效果**
float2 warpOffset = float2(sin(coords.y * 10.0 + uTime * uDistortionSpeed), cos(coords.x * 10.0 + uTime * uDistortionSpeed)) * uWarpIntensity;

// **折射背景，让拖尾看起来像折叠空间**
float4 bgColor = tex2D(uBackground, coords + warpOffset);

// **颜色随时间变化**
float3 spaceColor = lerp(uColor, bgColor.rgb, 0.6 + 0.4 * sin(uTime * 1.5 + coords.x * 5.0));

return float4(spaceColor, opacity);
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
//    GameShaders.Misc["ModNamespace:TrailWarpDistortion"]
//        .SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/FabstaffStreak"))
//        .UseColor(new Color(80, 30, 200)) // 紫色时空扭曲
//        .Apply();
//
//    PrimitiveRenderer.RenderTrail(Projectile.oldPos, new(TrailWidth, (completionRatio, vertexPos) => TrailColor(completionRatio), (_) = > Projectile.Size * 0.5f, shader: GameShaders.Misc["ModNamespace:TrailWarpDistortion"]), 10);
//    Main.spriteBatch.ExitShaderRegion();
//
//    Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, 0, 0);
//    return false;
//}
//
//public Color TrailColor(float completionRatio) {
//    float opacity = Utils.GetLerpValue(1f, 0.5f, completionRatio, true) * Projectile.Opacity;
//    return new Color(80, 30, 200) * opacity; // 深紫色空间扭曲
//}
//
//public float TrailWidth(float completionRatio) {
//    return MathHelper.SmoothStep(10f, 22f, Utils.GetLerpValue(0f, 1f, completionRatio, true));
//}
