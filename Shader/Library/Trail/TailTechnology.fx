// TechTrailShader.fx - 科技风格拖尾
sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uScanlineSpeed;
float uTime;
matrix uWorldViewProjection;

struct VertexShaderInput {
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput {
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input) {
    VertexShaderOutput output;
    output.Position = mul(input.Position, uWorldViewProjection);
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0{
    float2 coords = input.TextureCoordinates.xy;
    coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;  // 修正 UV 坐标

    float scanline = tex2D(uImage1, coords * 4.0 + float2(0, uTime * uScanlineSpeed)).r;
    float4 baseColor = float4(lerp(uColor, uSecondaryColor, scanline), uOpacity);

    float4 fadeMapColor = tex2D(uImage1, coords - float2(uTime * 0.6, 0));  // 透明度修正
    float opacity = fadeMapColor.r * scanline;

    return float4(baseColor.rgb, opacity);
}

technique TrailTechnique{
    pass TrailPass {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}


//public Color TrailColor(float completionRatio)
//{
//    float opacity = Utils.GetLerpValue(1f, 0.6f, completionRatio, true) * Projectile.Opacity;
//    return new Color(255, 60, 60) * opacity; // 红色拖尾
//}
//public float TrailWidth(float completionRatio)
//{
//    return MathHelper.SmoothStep(12f, 28f, Utils.GetLerpValue(0f, 1f, completionRatio, true));
//}
//public override bool PreDraw(ref Color lightColor)
//{
//    Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY;
//    Vector2 origin = texture.Size() * 0.5f;

//    Main.spriteBatch.EnterShaderRegion();

//    GameShaders.Misc["ModNamespace:TailTechnologyEffect"]
//        .SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/FabstaffStreak"));

//    GameShaders.Misc["ModNamespace:TailTechnologyEffect"]
//        .UseColor(new Color(255, 60, 60)) // 红色
//        .UseSecondaryColor(new Color(0, 0, 0)) // 黑色扫描线
//        .Apply();

//    PrimitiveRenderer.RenderTrail(Projectile.oldPos,
//        new(TrailWidth, (completionRatio, vertexPos) => TrailColor(completionRatio), (completionRatio, vertexPos) => Projectile.Size * 0.5f, shader: GameShaders.Misc["ModNamespace:TailTechnologyEffect"]),
//        10);

//    Main.spriteBatch.ExitShaderRegion();

//    Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, 0, 0);

//    return false;
//}