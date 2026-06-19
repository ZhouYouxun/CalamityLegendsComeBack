using CalamityMod.Buffs.DamageOverTime;
using CalamityMod;
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
    internal static class PFSkeletronEffect
    {
        private const int FireInterval = 28;

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
            PFLeftEffectRules.FireSpread(
                holdout,
                ModContent.ProjectileType<PFSkeletronBonefireSkull>(),
                3,
                MathHelper.ToRadians(7.5f),
                13.2f,
                0.55f,
                0.46f,
                5.2f,
                9,
                new Color(220, 218, 196),
                0.58f,
                16f);

            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.58f, Pitch = -0.18f }, holdout.GunTipPosition);
        }
    }

    internal sealed class PFSkeletronBonefireSkull : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "Terraria/Images/Item_" + ItemID.Bone;

        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(220, 218, 196));

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 118;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = MathHelper.Lerp(0.78f, 1.08f, Utils.GetLerpValue(118f, 88f, Projectile.timeLeft, true));
            HomeTowardNearestEnemy();
            EmitTrail();

            if (Projectile.numUpdates == 0)
                Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.34f);
        }

        private void HomeTowardNearestEnemy()
        {
            NPC target = Projectile.Center.ClosestNPCAt(720f);
            if (target == null)
                return;

            Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * 15.6f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.045f);
        }

        private void EmitTrail()
        {
            if (Main.dedServ || Projectile.numUpdates != 0)
                return;

            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
            GeneralParticleHandler.SpawnParticle(new LineParticle(
                Projectile.Center + back * 7f,
                back * Main.rand.NextFloat(0.7f, 1.6f),
                false,
                Main.rand.Next(8, 13),
                Main.rand.NextFloat(0.22f, 0.34f),
                Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.2f, 0.55f))));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.BoneTorch, back.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.7f, 2.2f), 80, ThemeColor, Main.rand.NextFloat(0.75f, 1.15f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Shadowflame>(), 150);
            ReleaseBonePulse(target.Center, 7);
        }

        public override void OnKill(int timeLeft)
        {
            ReleaseBonePulse(Projectile.Center, 9);
            SoundEngine.PlaySound(SoundID.NPCHit2 with { Volume = 0.42f, Pitch = 0.22f }, Projectile.Center);
        }

        private void ReleaseBonePulse(Vector2 center, int count)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / count).ToRotationVector2() * Main.rand.NextFloat(1.8f, 4.2f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(center, velocity, false, Main.rand.Next(13, 20), Main.rand.NextFloat(0.36f, 0.62f), Color.Lerp(ThemeColor, Color.White, 0.34f)));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color glow = (Color.Lerp(ThemeColor, Color.White, 0.28f) with { A = 0 }) * Projectile.Opacity;

            PFLeftEffectRules.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, drawPosition, null, glow * 0.26f * fade, Projectile.rotation, bloom.Size() * 0.5f, 0.06f * fade, SpriteEffects.None, 0);
            }
            PFLeftEffectRules.EndAdditive();

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
