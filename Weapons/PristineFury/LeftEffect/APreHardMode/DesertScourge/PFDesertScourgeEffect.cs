using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFDesertScourgeEffect
    {
        private const int FireInterval = 20;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            holdout.LeftTimer++;
            if (holdout.LeftTimer < FireInterval)
                return;

            holdout.LeftTimer = 0;
            PFLeftEffectRules.FireSingle(
                holdout,
                ModContent.ProjectileType<PFDesertScourgeGlassBlock>(),
                15.5f,
                MathHelper.ToRadians(2.2f),
                0.82f,
                5.6f,
                8,
                new Color(240, 214, 145),
                0.52f,
                16f);

            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.52f, Pitch = -0.12f }, holdout.GunTipPosition);
        }
    }

    internal sealed class PFDesertScourgeGlassBlock : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "Terraria/Images/Item_" + ItemID.Glass;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += Projectile.velocity.X * 0.032f;
            Projectile.velocity.Y += 0.025f;

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.GemDiamond, -Projectile.velocity * 0.04f, 80, new Color(210, 235, 255), Main.rand.NextFloat(0.55f, 0.9f));
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.36f, 0.34f, 0.22f));
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<SulphuricPoisoning>(), 120);
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                int count = Main.rand.Next(6, 9);
                for (int i = 0; i < count; i++)
                {
                    float angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.22f, 0.22f);
                    Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(6.5f, 11.5f) + Projectile.velocity * 0.16f;
                    int shard = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        velocity,
                        ModContent.ProjectileType<PFDesertScourgeGlassShard>(),
                        (int)(Projectile.damage * 0.46f),
                        Projectile.knockBack * 0.35f,
                        Projectile.owner);
                    PFLeftEffectRules.ApplyTheme(shard, PristineFuryMark.DesertScourge);
                }
            }

            SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.62f, Pitch = 0.1f }, Projectile.Center);
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GemDiamond, Main.rand.NextVector2Circular(5f, 5f), 70, new Color(215, 235, 255), Main.rand.NextFloat(0.7f, 1.15f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, drawPosition, null, new Color(190, 230, 255, 0) * (0.22f * fade), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }

    internal sealed class PFDesertScourgeGlassShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "Terraria/Images/Item_" + ItemID.Glass;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 54;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += Projectile.velocity.X * 0.08f;
            Projectile.velocity.Y += 0.04f;
            Projectile.Opacity = MathHelper.Clamp(Projectile.timeLeft / 18f, 0f, 1f);

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GemDiamond, -Projectile.velocity * 0.02f, 90, new Color(220, 245, 255), Main.rand.NextFloat(0.38f, 0.65f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Bleeding, 90);
        }
    }
}
