// 拖尾效果：颜色在红 - 绿 - 蓝之间动态变化，不断循环变换，制造出彩色拖尾效果。
// 透明度控制：基于 uImage1（Fade Map）和 sin() 计算，使拖尾尾端逐渐淡出。

// **拖尾纹理（采样器）**
sampler uImage0 : register(s0);  // 拖尾的主纹理
sampler uImage1 : register(s1);  // 额外的透明度控制纹理

// **拖尾的动态属性**
float3 uColor;            // 拖尾的基础颜色
float uTime;              // 全局时间，用于动态变色
float uOpacity;           // 拖尾的透明度
matrix uWorldViewProjection;  // 用于计算顶点位置的变换矩阵

// **输入结构（顶点着色器）**
struct VertexShaderInput
{
    float4 Position : POSITION0;  // 顶点位置
    float4 Color : COLOR0;        // 颜色
    float3 TextureCoordinates : TEXCOORD0;  // 纹理坐标（改成 `float3`）
};

// **输出结构（传递给像素着色器）**
struct VertexShaderOutput
{
    float4 Position : SV_POSITION;  // 计算后的顶点位置
    float4 Color : COLOR0;          // 颜色
    float3 TextureCoordinates : TEXCOORD0;  // 纹理坐标
};

// **顶点着色器**
VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.Position = mul(input.Position, uWorldViewProjection); // 计算世界变换
    output.Color = input.Color; // 保持输入颜色
    output.TextureCoordinates = input.TextureCoordinates; // 传递纹理坐标
    return output;
}

// **工具函数：线性插值**
float InverseLerp(float a, float b, float x)
{
    return saturate((x - a) / (b - a));
}

// **像素着色器**
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // **修正 UV 坐标，防止拉伸**
    float2 coords = input.TextureCoordinates.xy;
    coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;

    // **从纹理中获取基础颜色**
    float4 baseColor = tex2D(uImage0, coords);

    // **从 `uImage1` 获取透明度控制（Fade Map）**
    float4 fadeMapColor = tex2D(uImage1, coords - float2(uTime * 0.3, 0));
    float opacity = fadeMapColor.r * pow(sin(coords.y * 3.141), 1.5) * uOpacity;

    // **计算一个时间变化因子，控制颜色**
    float timeFactor = (sin(uTime * 1.5) + 1.0) * 0.5;  // 0~1 之间变化

    // **动态颜色变化（红 - 绿 - 蓝）**
    float3 dynamicColor = lerp(float3(1, 0, 0), float3(0, 0, 1), timeFactor);

    // **最终颜色计算**
    return float4(baseColor.rgb * dynamicColor, opacity);
}

// **修正 Pass 名称，确保与 `RegisterTrailShader()` 匹配**
technique TrailTechnique
{
    pass TrailPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}



//public Color TrailColor(float completionRatio)
//{
//    // **动态颜色变化（红 - 绿 - 蓝）**
//    float hue = (Main.GlobalTimeWrappedHourly * 0.5f + completionRatio) % 1f; // 让颜色随时间变换
//    float opacity = Utils.GetLerpValue(1f, 0.5f, completionRatio, true) * Projectile.Opacity; // 让尾端透明
//    Color color = Main.hslToRgb(hue, 1f, 0.8f) * opacity; // 颜色渐变（HSL 转换）
//    color.A = (byte)(int)(Utils.GetLerpValue(0f, 0.2f, completionRatio) * 128); // 额外控制透明度
//    return color;
//}
//
//public float TrailWidth(float completionRatio)
//{
//    float widthInterpolant = Utils.GetLerpValue(0f, 0.3f, completionRatio, true) * Utils.GetLerpValue(1.0f, 0.7f, completionRatio, true);
//    return MathHelper.SmoothStep(6f, 20f, widthInterpolant); // 让拖尾起点宽，尾端窄
//}
//
//public override bool PreDraw(ref Color lightColor)
//{
//    Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY;
//    Vector2 origin = texture.Size() * 0.5f;
//
//    // **进入 Shader 渲染区域**
//    Main.spriteBatch.EnterShaderRegion();
//
//    // **设置拖尾 Shader 贴图**
//    GameShaders.Misc["ModNamespace:TailSecondEffect"].SetShaderTexture(
//        ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/FabstaffStreak")
//    );
//
//    // **应用 Shader**
//    GameShaders.Misc["ModNamespace:TailSecondEffect"].Apply();
//
//    // **渲染拖尾**
//    PrimitiveRenderer.RenderTrail(
//        Projectile.oldPos,
//        new(TrailWidth, (completionRatio, vertexPos) => TrailColor(completionRatio), (_) = > Projectile.Size * 0.5f, shader: GameShaders.Misc["ModNamespace:TailSecondEffect"]),
//        10
//    );
//
//    // **退出 Shader 渲染**
//    Main.spriteBatch.ExitShaderRegion();
//
//    // **绘制弹幕本体**
//    Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, 0, 0);
//
//    return false;
//}