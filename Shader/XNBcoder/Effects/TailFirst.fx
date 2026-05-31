
// 拖尾效果：宽度恒定，颜色受 uColor 影响，默认情况下较暗。
// 透明度控制：基于 uImage1（Fade Map）和 sin() 计算，使拖尾尾端逐渐淡出。
sampler uImage0 : register(s0);  // 拖尾主纹理
sampler uImage1 : register(s1);  // 透明度控制纹理（Fade Map）

float uTime;                      // 全局时间（用于动态变化）
float3 uColor;                    // 拖尾的基本颜色
float uOpacity;                    // 拖尾透明度
matrix uWorldViewProjection;       // 变换矩阵

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
    float4 Position : SV_POSITION; // 计算后的顶点位置
    float4 Color : COLOR0;         // 颜色
    float3 TextureCoordinates : TEXCOORD0; // 纹理坐标
};

// **顶点着色器**
VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.Position = mul(input.Position, uWorldViewProjection); // 计算世界变换后的顶点位置
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
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
    float4 baseColor = tex2D(uImage0, input.TextureCoordinates.xy);  // 读取主纹理颜色

// **修正 UV 坐标，防止拉伸**
float2 coords = input.TextureCoordinates.xy;
coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;

// **从 `uImage1` 获取透明度控制（Fade Map）**
float4 fadeMapColor = tex2D(uImage1, coords - float2(uTime * 0.6, 0));
float opacity = fadeMapColor.r * pow(sin(coords.y * 3.141), 1.5) * uOpacity;

// **颜色渐变**
float brightness = InverseLerp(0.1, 0, coords.x) * 1.2; // 让拖尾前端更亮
baseColor.rgb *= uColor * brightness;

// **最终颜色输出**
return float4(baseColor.rgb, opacity);
}

// **ps_3_0 强制要求**
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
//    float opacity = Utils.GetLerpValue(1f, 0.5f, completionRatio, true) * Projectile.Opacity; // 让尾端透明
//    Color color = Color.Lime * opacity; // 让拖尾为亮绿色
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
//    GameShaders.Misc["ModNamespace:TailFirstEffect"].SetShaderTexture(
//        ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/FabstaffStreak")
//    );
//
//    // **应用 Shader**
//    GameShaders.Misc["ModNamespace:TailFirstEffect"].Apply();
//
//    // **渲染拖尾**
//    PrimitiveRenderer.RenderTrail(
//        Projectile.oldPos,
//        new(TrailWidth, (completionRatio, vertexPos) => TrailColor(completionRatio), (_) = > Projectile.Size * 0.5f, shader: GameShaders.Misc["ModNamespace:TailFirstEffect"]),
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
    