// **TrailGhostlyPhantom.fx - 亡灵幽影拖尾**
sampler uImage0 : register(s0);  // 拖尾主纹理
sampler uImage1 : register(s1);  // 透明度控制纹理（Fade Map）

float3 uColor;  // 主颜色（幽绿色）
float3 uSecondaryColor; // 副颜色（淡紫色）
float uOpacity;  // 拖尾透明度
float uTime;  // 全局时间
float uGhostFade; // 影响残影消散速度
matrix uWorldViewProjection;  // 变换矩阵

// **输入结构**
struct VertexShaderInput {
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

// **输出结构**
struct VertexShaderOutput {
    float4 Position : SV_POSITION;  // 修正 `POSITION`
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

// **计算透明度（幽灵残影效果）**
float4 fadeMapColor = tex2D(uImage1, coords - float2(uTime * 0.4, 0));
float ghostOpacity = fadeMapColor.r * pow(sin(coords.y * 3.141 + uTime * 2.0), 1.3) * uOpacity;

// **颜色渐变（幽绿色 - 淡紫色）**
float3 ghostlyColor = lerp(uColor, uSecondaryColor, sin(uTime * 1.2 + coords.x * 3.0) * 0.5 + 0.5);

// **增加幽灵影子残影效果**
float ghostFade = pow(1.0 - coords.x, uGhostFade);

return float4(ghostlyColor * ghostFade, ghostOpacity);
}

// **Shader Pass**
technique TrailTechnique{
    pass TrailPass {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}



//public override bool PreDraw(ref Color lightColor)
//{
//    Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY;
//    Vector2 origin = texture.Size() * 0.5f;
//
//    Main.spriteBatch.EnterShaderRegion();
//
//    GameShaders.Misc["ModNamespace:TrailGhostlyPhantom"]
//        .SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/FabstaffStreak"));
//
//    GameShaders.Misc["ModNamespace:TrailGhostlyPhantom"]
//        .UseColor(new Color(80, 255, 120)) // 幽绿色
//        .UseSecondaryColor(new Color(180, 120, 255)) // 淡紫色幽魂光晕
//        .Apply();
//
//    PrimitiveRenderer.RenderTrail(Projectile.oldPos,
//        new(TrailWidth, (completionRatio, vertexPos) => TrailColor(completionRatio), (_) = > Projectile.Size * 0.5f, shader: GameShaders.Misc["ModNamespace:TrailGhostlyPhantom"]),
//        10);
//
//    Main.spriteBatch.ExitShaderRegion();
//
//    Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, 0, 0);
//
//    return false;
//}
//public Color TrailColor(float completionRatio)
//{
//    float opacity = Utils.GetLerpValue(1f, 0.6f, completionRatio, true) * Projectile.Opacity;
//    return new Color(80, 255, 120) * opacity; // 幽绿色拖尾
//}
//public float TrailWidth(float completionRatio)
//{
//    return MathHelper.SmoothStep(10f, 22f, Utils.GetLerpValue(0f, 1f, completionRatio, true));
//}
