sampler2D uImage0;  // 物品的基础纹理
float2 uCenter;     // 万花筒中心点（弹幕中心）
float uSegments;    // 万花筒区域数（默认 6）
float uOpacity;     // 透明度（1.0 = 完全覆盖原画面）

// **计算旋转后的坐标**
float2 Rotate(float2 coord, float angle) {
    float s = sin(angle);
    float c = cos(angle);
    return float2(coord.x * c - coord.y * s, coord.x * s + coord.y * c);
}

// **主要像素着色器**
float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0{
    // **计算当前像素到中心点的偏移**
    float2 offset = coords - uCenter;

// **计算分割角度**
float segmentAngle = 6.283185 / uSegments; // 360° / uSegments，6.283185 ≈ 2π

// **计算当前像素的角度**
float angle = atan2(offset.y, offset.x);

// **将角度限制到第一个扇区**
angle = fmod(angle, segmentAngle);
if (angle < 0) angle += segmentAngle;

// **旋转回基准方向**
float2 mirroredOffset = Rotate(offset, -angle);

// **获取最终采样坐标**
float2 finalCoord = uCenter + mirroredOffset;

// **采样颜色**
float4 color = tex2D(uImage0, finalCoord);

// **应用透明度**
return lerp(tex2D(uImage0, coords), color, uOpacity);
}

// **定义 Technique**
technique KaleidoscopeEffect{
    pass P0 {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}



//public override bool PreDraw(ref Color lightColor)
//{
//    // **确保 Shader 存在**
//    Effect shader = ShaderGames.KaleidoscopeShader;
//    if (shader == null) return true;
//
//    // **获取弹幕中心点（归一化坐标）**
//    Vector2 screenCenter = (Projectile.Center - Main.screenPosition) / new Vector2(Main.screenWidth, Main.screenHeight);
//
//    // **设置 Shader 变量**
//    shader.Parameters["uCenter"].SetValue(screenCenter);
//    shader.Parameters["uSegments"].SetValue(6f); // 默认 6 个区域
//    shader.Parameters["uOpacity"].SetValue(1.0f); // 1.0 完全覆盖原画面
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
//
//    return false; // `false` 表示不让默认绘制进行，我们已经手动绘制了
//}
