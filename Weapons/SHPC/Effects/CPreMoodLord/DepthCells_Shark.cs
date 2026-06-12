using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    /// <summary>
    /// 深渊细胞生成的主鲨鱼。
    /// 贴图本身朝左，并且不是对称图，因此绘制时必须根据水平速度翻转贴图。
    /// </summary>
    internal sealed class DepthCells_Shark : ModProjectile, ILocalizedModType
    {
        private const float GravityDelay = 8f;
        private const float GravityStrength = 0.19f;
        private const float TerminalVelocity = 15f;
        private static readonly int[] AbyssDustTypes = { 191, 29, 104 };

        public new string LocalizationCategory => "Projectiles.SHPC";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 58;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 210;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            // 鲨鱼像重型抛射物一样坠落。稍微保留水平惯性，避免轨迹变成直线下落。
            if (Projectile.localAI[0] > GravityDelay)
            {
                Projectile.velocity.Y = Math.Min(TerminalVelocity, Projectile.velocity.Y + GravityStrength);
                Projectile.velocity.X *= 0.997f;
            }

            // 贴图原始朝向为左。朝右运动时水平翻转；旋转值也按当前朝向补偿。
            bool movingRight = Projectile.velocity.X >= 0f;
            Projectile.spriteDirection = movingRight ? 1 : -1;
            Projectile.rotation = movingRight
                ? Projectile.velocity.ToRotation()
                : Projectile.velocity.ToRotation() - MathHelper.Pi;

            Lighting.AddLight(Projectile.Center, Color.Lerp(DepthCells_Drop.AbyssToxic, DepthCells_Drop.AbyssCyan, 0.42f).ToVector3() * 0.62f);
            SpawnFlightEffects();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CrushDepth>(), 240);
            target.AddBuff(ModContent.BuffType<Eutrophication>(), 240);
            Projectile.ai[0] = target.whoAmI + 1;
            Projectile.netUpdate = true;
        }

        public override void OnKill(int timeLeft)
        {
            // NPCDeath13 是现有深渊细胞液滴已经使用的黏腻生物死亡音色。
            // 降低音高后更接近大型深海生物被撕裂时的爆裂感。
            SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.82f, Pitch = -0.24f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = -0.48f }, Projectile.Center);
            SpawnHorrificBurst();

            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<DepthCells_SharkExplosion>(),
                Math.Max(1, (int)(Projectile.damage * 1f)),
                Projectile.knockBack,
                Projectile.owner);

            // 鲨鱼尸体向上喷出 6 枚深渊液滴。每一枚的横向散布和上升速度都不同。
            for (int i = 0; i < 6; i++)
            {
                Vector2 dropVelocity = new(
                    Main.rand.NextFloat(-5.8f, 5.8f),
                    -Main.rand.NextFloat(6.8f, 13.8f));
                dropVelocity *= 0.6f;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 7f),
                    dropVelocity,
                    ModContent.ProjectileType<DepthCells_Drop>(),
                    (int)(Projectile.damage * 0.7f),
                    Projectile.knockBack,
                    Projectile.owner,
                    0f,
                    0f,
                    Projectile.ai[0]);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // 用较淡的深蓝残影强调重量和下坠方向，不覆盖鲨鱼本体细节。
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float opacity = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length * 0.24f;
                Color trailColor = Color.Lerp(DepthCells_Drop.AbyssDeep, DepthCells_Drop.AbyssCyan, 0.45f) * opacity;
                Main.EntitySpriteDraw(
                    texture,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    null,
                    trailColor,
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    effects);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }

        private void SpawnFlightEffects()
        {
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (Projectile.numUpdates == 0 && Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center + back * 18f + Main.rand.NextVector2Circular(5f, 5f),
                    back * Main.rand.NextFloat(0.3f, 1.3f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    Color.Lerp(DepthCells_Drop.AbyssDeep, DepthCells_Drop.AbyssBlue, Main.rand.NextFloat(0.2f, 0.6f)),
                    Main.rand.Next(22, 38),
                    Main.rand.NextFloat(0.48f, 0.86f),
                    0.38f,
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    false));
            }

            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + back * 16f + Main.rand.NextVector2Circular(5f, 5f),
                    AbyssDustTypes[Main.rand.Next(AbyssDustTypes.Length)],
                    back * Main.rand.NextFloat(0.8f, 2.8f) + Main.rand.NextVector2Circular(0.55f, 0.55f),
                    110,
                    Color.Lerp(DepthCells_Drop.AbyssBlue, DepthCells_Drop.AbyssToxic, Main.rand.NextFloat(0.3f, 0.85f)),
                    Main.rand.NextFloat(0.9f, 1.45f));
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                Dust foam = Dust.NewDustPerfect(
                    Projectile.Center + back * 15f,
                    DustID.Water,
                    back * Main.rand.NextFloat(0.4f, 1.4f),
                    120,
                    DepthCells_Drop.AbyssFoam,
                    Main.rand.NextFloat(0.72f, 1.05f));
                foam.noGravity = true;
            }
        }

        private void SpawnHorrificBurst()
        {
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                DepthCells_Drop.AbyssToxic,
                Vector2.One,
                0f,
                0.12f,
                0.78f,
                24));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(DepthCells_Drop.AbyssDeep, Color.DarkRed, 0.35f),
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.1f,
                0.48f,
                20));

            for (int i = 0; i < 46; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2.2f, 9.2f);
                Color color = Main.rand.NextBool(3)
                    ? Color.Lerp(Color.DarkRed, DepthCells_Drop.AbyssDeep, 0.42f)
                    : Color.Lerp(DepthCells_Drop.AbyssBlue, DepthCells_Drop.AbyssToxic, Main.rand.NextFloat());
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 7f),
                    AbyssDustTypes[Main.rand.Next(AbyssDustTypes.Length)],
                    velocity,
                    100,
                    color,
                    Main.rand.NextFloat(1.05f, 2.05f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 14; i++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(18f, 12f),
                    Main.rand.NextVector2Circular(3.2f, 3.2f),
                    Main.rand.NextBool(3) ? Color.Lerp(Color.DarkRed, Color.Black, 0.45f) : DepthCells_Drop.AbyssDeep,
                    Main.rand.Next(30, 52),
                    Main.rand.NextFloat(0.72f, 1.32f),
                    0.5f,
                    Main.rand.NextFloat(-0.08f, 0.08f),
                    false));
            }
        }
    }

    /// <summary>
    /// 鲨鱼死亡时的短命范围伤害。视觉由鲨鱼自身生成，这个弹幕仅负责判定。
    /// </summary>
    internal sealed class DepthCells_SharkExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 176;
            Projectile.height = 176;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CrushDepth>(), 240);
            target.AddBuff(ModContent.BuffType<Eutrophication>(), 240);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
