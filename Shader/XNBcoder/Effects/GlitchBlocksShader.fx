// 失败品，失败原因：无法分割色块，但整体的色彩跳跃没问题
//sampler2D uImage0;  // 物品的基础纹理
//float uTime;        // 时间变量
//float uBlockSize;   // 小方块尺寸（默认 6x6）
//float uOpacity;     // 染色透明度
//
//// **HSV 转 RGB**
//float3 HUEtoRGB(float H) {
//    float R = abs(H * 6 - 3) - 1;
//    float G = 2 - abs(H * 6 - 2);
//    float B = 2 - abs(H * 6 - 4);
//    return saturate(float3(R, G, B));
//}
//
//// **伪随机函数**
//float random(float2 p) {
//    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
//}
//
//// 主要像素着色器
//float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
//    float4 baseColor = tex2D(uImage0, coords); // 获取物品的颜色
//
//// **跳过透明像素**
//if (baseColor.a < 0.01) return baseColor;
//
//// **计算小方块的固定坐标**
//float2 blockCoords = floor(coords / uBlockSize) * uBlockSize;
//
//// **让整个小方块的颜色一致**
//float2 blockID = blockCoords / uBlockSize; // 计算小方块的索引
//float hue = random(blockID + floor(uTime * 3)) * 1.0; // 让颜色跳跃变化
//float3 glitchColor = HUEtoRGB(hue); // 生成彩虹色
//
//// **构造最终颜色**
//float4 glitchEffect = float4(glitchColor, 1.0);
//
//// **颜色混合**
//return lerp(baseColor, glitchEffect, uOpacity) * baseColor.a;
//}
//
//// 定义 Technique
//technique GlitchBlocksEffect{
//    pass P0 {
//        PixelShader = compile ps_2_0 PixelShaderFunction();
//    }
//}

        // 故障色块
//public override bool PreDraw(ref Color lightColor)
//{
//    // **确保 Shader 存在**
//    Effect shader = ShaderGames.GlitchBlocksShader;
//    if (shader == null) return true;
//
//    // **设置 Shader 变量**
//    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
//    shader.Parameters["uBlockSize"].SetValue(3f); // 每个小方块的大小 6x6
//    shader.Parameters["uOpacity"].SetValue(0.7f); // 透明度，0.7 表示 70% 染色强度
//
//    // **应用 Shader**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
//
//    // **获取弹幕贴图**
//    Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
//    int frameHeight = texture.Height / Main.projFrames[Projectile.type]; // 计算单帧高度
//    Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight); // 选取当前帧
//
//    // **绘制弹幕**
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition;
//    Main.spriteBatch.Draw(texture, drawPosition, sourceRect, Color.White, Projectile.rotation, new Vector2(texture.Width / 2, frameHeight / 2), Projectile.scale, SpriteEffects.None, 0f);
//
//    // **恢复 SpriteBatch**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
//
//    return false; // `false` 表示不让默认   绘制进行，我们已经手动绘制了
//}





// 失败品2号，失败原因：有线条故障感，但是不够强烈，颜色也变得不够强烈
//sampler2D uImage0;  // 物品的基础纹理
//float uTime;        // 时间变量
//float uBlockSize;   // 小方块尺寸
//float uOpacity;     // 染色透明度
//
//// **HSV 转 RGB**
//float3 HUEtoRGB(float H) {
//    float R = abs(H * 6 - 3) - 1;
//    float G = 2 - abs(H * 6 - 2);
//    float B = 2 - abs(H * 6 - 4);
//    return saturate(float3(R, G, B));
//}
//
//// **快速随机函数**
//float random(float2 p) {
//    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
//}
//
//// **优化后的噪声函数（减少指令）**
//float glitchNoise(float2 coords) {
//    return frac(sin(coords.x * 0.1 + coords.y * 0.1 + uTime * 3.0) * 100.0);
//}
//
//// 主要像素着色器
//float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
//    float4 baseColor = tex2D(uImage0, coords); // 获取物品的颜色
//
//// **跳过透明像素**
//if (baseColor.a < 0.01) return baseColor;
//
//// **计算方块索引**
//float2 blockCoords = floor(coords / uBlockSize) * uBlockSize;
//
//// **减少计算量，避免超出 `ps_2_0` 限制**
//float hue = random(blockCoords + uTime) * 1.0;
//float3 glitchColor = HUEtoRGB(hue);
//
//// **减少 smoothstep()，用 noise 替代**
//float edgeFactor = glitchNoise(coords);
//
//// **构造最终颜色**
//float4 glitchEffect = float4(glitchColor, 1.0);
//
//// **颜色混合**
//return lerp(baseColor, glitchEffect, uOpacity * edgeFactor) * baseColor.a;
//}
//
//// **降低 `ps_2_0` 计算负担**
//technique GlitchBlocksEffect{
//    pass P0 {
//        PixelShader = compile ps_2_0 PixelShaderFunction();
//    }
//}

//之前出过一些问题，现已被修复，主要问题如下：
//着色器指令超标
//
//ps_2_0 允许最多 64 条算术指令，但原来的代码使用了 104 条。
//ps_2_0 允许 96 条指令总量，但原来的代码用了 105 条。
//主要超标部分：
//过多的 sin() / cos() 计算 → 计算量过大，影响 glitchNoise()。
//smoothstep() 的大量使用 → smoothstep() 需要多个指令执行浮点运算。
//复杂的随机计算 → 原 random() 计算过于复杂，指令过多。
//ps_2_0 的限制
//
//tModLoader 只能使用 ps_2_0，它比 ps_3_0 限制更严格。
//ps_2_0 不支持循环和复杂的数学运算，所以指令数量容易超标。


        //public override bool PreDraw(ref Color lightColor)
        //{
        //    // **确保 Shader 存在**
        //    Effect shader = ShaderGames.GlitchBlocksShader;
        //    if (shader == null) return true;

        //    // **设置 Shader 变量**
        //    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
        //    shader.Parameters["uBlockSize"].SetValue(8f); // 控制色块大小
        //    shader.Parameters["uOpacity"].SetValue(0.7f); // 控制染色强度

        //    // **应用 Shader**
        //    Main.spriteBatch.End();
        //    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);

        //    // **获取弹幕贴图**
        //    Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
        //    int frameHeight = texture.Height / Main.projFrames[Projectile.type]; // 计算单帧高度
        //    Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight); // 选取当前帧

        //    // **绘制弹幕**
        //    Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        //    Main.spriteBatch.Draw(texture, drawPosition, sourceRect, Color.White, Projectile.rotation, new Vector2(texture.Width / 2, frameHeight / 2), Projectile.scale, SpriteEffects.None, 0f);

        //    // **恢复 SpriteBatch**
        //    Main.spriteBatch.End();
        //    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        //    return false; // `false` 表示不让默认绘制进行，我们已经手动绘制了
        //}





// 注意，这个第3个版本依旧有明显的偏红色迹象，但是已经可以使用了
sampler2D uImage0;  // 物品的基础纹理
float uTime;        // 时间变量
float uBlockSize;   // 故障块尺寸
float uOpacity;     // 故障效果透明度

// **优化后的随机函数**
float random(float2 p) {
    return frac(sin(dot(p, float2(23.140692632779, 2.665144142690225))) * 43758.5453);
}

// **优化后的故障噪声**
float chaosNoise(float2 coords) {
    return frac(sin(coords.x * 0.1 + coords.y * 0.1 + uTime * 3.0) * 100.0);
}

// **RGB 颜色池**
static const float3 colorPool[6] = {
    float3(1.0, 0.0, 0.0), // 红色
    float3(0.0, 1.0, 0.0), // 绿色
    float3(0.0, 0.0, 1.0), // 蓝色
    float3(1.0, 1.0, 0.0), // 黄色
    float3(1.0, 0.0, 1.0), // 紫色
    float3(0.0, 1.0, 1.0)  // 青色
};

// **主要像素着色器**
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
    float4 baseColor = tex2D(uImage0, coords); // 获取原始颜色（但不使用）

// **跳过透明像素**
if (baseColor.a < 0.01) return baseColor;

// **计算不规则色块的索引**
float2 blockCoords = floor(coords / uBlockSize) * uBlockSize;

// **直接从颜色池中随机选取颜色**
int colorIndex = (int)(random(blockCoords) * 6) % 6;
float3 glitchColor = colorPool[colorIndex];

// **生成随机故障裂缝**
float glitchEffect = chaosNoise(coords * 1.5) > 0.6 ? 1.0 : 0.3; // 让裂缝更不规则

// **构造最终故障颜色**
float4 finalGlitch = float4(glitchColor * glitchEffect, 1.0);

// **完全覆盖原始颜色**
return lerp(baseColor, finalGlitch, uOpacity) * baseColor.a;
}

// **定义 Shader**
technique GlitchChaosEffect{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}



//chaosNoise() 计算量过大 → 由于 sin() + cos() 计算太多，导致指令超限。
//random() 计算过于复杂 → 需要简化，让 Shader 能适应 ps_2_0 限制。



//public override bool PreDraw(ref Color lightColor)
//{
//    // **确保 Shader 存在**
//    Effect shader = ShaderGames.GlitchBlocksShader;
//    if (shader == null) return true;
//
//    // **设置 Shader 变量**
//    shader.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly);
//    shader.Parameters["uBlockSize"].SetValue(8f); // 控制故障块大小
//    shader.Parameters["uOpacity"].SetValue(1.0f); // 彻底覆盖原始颜色
//
//    // **应用 Shader**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
//
//    // **获取弹幕贴图**
//    Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
//    int frameHeight = texture.Height / Main.projFrames[Projectile.type]; // 计算单帧高度
//    Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight); // 选取当前帧
//
//    // **绘制弹幕**
//    Vector2 drawPosition = Projectile.Center - Main.screenPosition;
//    Main.spriteBatch.Draw(texture, drawPosition, sourceRect, Color.White, Projectile.rotation, new Vector2(texture.Width / 2, frameHeight / 2), Projectile.scale, SpriteEffects.None, 0f);
//
//    // **恢复 SpriteBatch**
//    Main.spriteBatch.End();
//    Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
//
//    return false; // `false` 表示不让默认绘制进行，我们已经手动绘制了
//}
