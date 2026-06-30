using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.ZhuangFangYiPet
{
    internal static class ZhuangFangYiPetVisuals
    {
        public static readonly Color Teal = new(54, 255, 212);
        public static readonly Color Green = new(92, 255, 154);
        public static readonly Color Pale = new(177, 255, 232);

        public static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 1f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                (start + end) * 0.5f - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                edge.ToRotation(),
                new Vector2(0.5f, 0.5f),
                new Vector2(edge.Length(), width),
                SpriteEffects.None);
        }

        public static void DrawSquareOutline(Vector2 center, float size, Color color, float width, float rotation = 0f)
        {
            Vector2[] corners = new Vector2[4];
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 local = i switch
                {
                    0 => new Vector2(-1f, -1f),
                    1 => new Vector2(1f, -1f),
                    2 => new Vector2(1f, 1f),
                    _ => new Vector2(-1f, 1f)
                } * size * 0.5f;
                corners[i] = center + local.RotatedBy(rotation);
            }

            for (int i = 0; i < corners.Length; i++)
                DrawLine(corners[i], corners[(i + 1) % corners.Length], color, width);
        }

        public static void DrawFilledDiamond(Vector2 center, float size, Color color, float rotation)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.EntitySpriteDraw(
                pixel,
                center - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                rotation + MathHelper.PiOver4,
                new Vector2(0.5f),
                new Vector2(size, size),
                SpriteEffects.None);
        }

        public static void SpawnElectricDust(Vector2 center, float radius, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(radius, radius),
                    DustID.FireworksRGB,
                    Main.rand.NextVector2Circular(speed, speed),
                    0,
                    Main.rand.NextBool() ? Teal : Green,
                    Main.rand.NextFloat(0.75f, 1.35f));
                dust.noGravity = true;
            }
        }
    }

    internal sealed class ZhuangFangYiConvergenceController : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TargetIndex => (int)Projectile.ai[0];
        private bool HarmonyAttack => Projectile.ai[1] == 1f;
        private int timer;
        private bool initialized;
        private bool exploded;
        private Vector2 convergenceCenter;

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 44;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            timer++;
            if (!initialized)
                InitializeBlocks();

            if (!exploded && timer >= 24)
                Explode();

            if (timer > 36)
                Projectile.Kill();
        }

        private void InitializeBlocks()
        {
            initialized = true;
            convergenceCenter = ResolveCenter();
            Projectile.Center = convergenceCenter;

            if (Main.myPlayer != Projectile.owner)
                return;

            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.PiOver2 * i + MathHelper.PiOver4;
                Vector2 start = convergenceCenter + angle.ToRotationVector2() * 250f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    start,
                    Vector2.Zero,
                    ModContent.ProjectileType<ZhuangFangYiConvergenceBlock>(),
                    0,
                    0f,
                    Projectile.owner,
                    convergenceCenter.X,
                    convergenceCenter.Y,
                    angle);
            }

            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.44f, Pitch = 0.38f }, convergenceCenter);
        }

        private Vector2 ResolveCenter()
        {
            if (Main.npc.IndexInRange(TargetIndex))
            {
                NPC target = Main.npc[TargetIndex];
                if (target.active && target.CanBeChasedBy())
                    return target.Center;
            }

            return Projectile.Center;
        }

        private void Explode()
        {
            exploded = true;
            convergenceCenter = ResolveCenter();
            Projectile.Center = convergenceCenter;

            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                convergenceCenter,
                Vector2.Zero,
                ModContent.ProjectileType<ZhuangFangYiLightningExplosion>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner);

            if (HarmonyAttack)
            {
                int shockwaveDamage = Math.Max(1, (int)(Projectile.damage * (2.4f / 5.4f)));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    convergenceCenter,
                    Vector2.Zero,
                    ModContent.ProjectileType<ZhuangFangYiSquareShockwave>(),
                    shockwaveDamage,
                    Projectile.knockBack * 0.4f,
                    Projectile.owner);
            }
        }
    }

    internal sealed class ZhuangFangYiConvergenceBlock : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Vector2 CenterPoint => new(Projectile.ai[0], Projectile.ai[1]);
        private float BaseAngle => Projectile.ai[2];
        private Vector2 startCenter;
        private int timer;

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 34;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            timer++;
            if (startCenter == Vector2.Zero)
                startCenter = Projectile.Center;

            float progress = Utils.GetLerpValue(7f, 23f, timer, true);
            float easedProgress = 1f - (float)Math.Pow(1f - progress, 3.1f);
            Projectile.Center = Vector2.Lerp(startCenter, CenterPoint, easedProgress);
            Projectile.rotation += 0.12f + progress * 0.16f;

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    DustID.FireworksRGB,
                    (CenterPoint - Projectile.Center).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.5f, 1.8f),
                    0,
                    Main.rand.NextBool() ? ZhuangFangYiPetVisuals.Teal : ZhuangFangYiPetVisuals.Green,
                    Main.rand.NextFloat(0.65f, 0.95f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fadeIn = Utils.GetLerpValue(0f, 6f, timer, true);
            float fadeOut = Utils.GetLerpValue(34f, 21f, Projectile.timeLeft, true);
            float opacity = fadeIn * fadeOut;
            float pulse = 1f + (float)Math.Sin((Main.GameUpdateCount + Projectile.identity) * 0.22f) * 0.08f;
            Color fill = ZhuangFangYiPetVisuals.Teal with { A = 0 } * 0.36f * opacity;
            Color edge = ZhuangFangYiPetVisuals.Pale with { A = 0 } * 0.78f * opacity;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            ZhuangFangYiPetVisuals.DrawFilledDiamond(Projectile.Center, 30f * pulse, fill, Projectile.rotation + BaseAngle * 0.15f);
            ZhuangFangYiPetVisuals.DrawSquareOutline(Projectile.Center, 34f * pulse, edge, 2.4f, Projectile.rotation + MathHelper.PiOver4);
            ZhuangFangYiPetVisuals.DrawLine(Projectile.Center, CenterPoint, ZhuangFangYiPetVisuals.Green with { A = 0 } * 0.18f * opacity, 2f);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

    internal sealed class ZhuangFangYiLightningExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int timer;

        public override void SetDefaults()
        {
            Projectile.width = 220;
            Projectile.height = 220;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 5;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            timer++;
            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.78f, Pitch = 0.28f }, Projectile.Center);
                ZhuangFangYiPetVisuals.SpawnElectricDust(Projectile.Center, 90f, 38, 8f);

                for (int i = 0; i < 12; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * Main.rand.NextFloat(2.4f, 7.5f);
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        Projectile.Center + velocity * 2f,
                        velocity,
                        false,
                        Main.rand.Next(10, 18),
                        Main.rand.NextFloat(0.58f, 0.95f),
                        Main.rand.NextBool() ? ZhuangFangYiPetVisuals.Teal : ZhuangFangYiPetVisuals.Green));
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 10 * 60);
            target.AddBuff(ModContent.BuffType<StaticDischarge>(), 10 * 60);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacity = Utils.GetLerpValue(0f, 2f, timer, true) * Utils.GetLerpValue(0f, 5f, Projectile.timeLeft, true);
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = 0; i < 4; i++)
            {
                float rotation = MathHelper.PiOver4 * i + Main.GlobalTimeWrappedHourly * (i % 2 == 0 ? 0.8f : -0.65f);
                ZhuangFangYiPetVisuals.DrawSquareOutline(
                    Projectile.Center,
                    90f + i * 38f + timer * 18f,
                    (i % 2 == 0 ? ZhuangFangYiPetVisuals.Teal : ZhuangFangYiPetVisuals.Pale) with { A = 0 } * opacity * 0.55f,
                    3f,
                    rotation);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

    internal sealed class ZhuangFangYiSquareShockwave : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 6;
        private int timer;

        public override void SetDefaults()
        {
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            timer++;
            Vector2 center = Projectile.Center;
            float progress = Utils.GetLerpValue(0f, Lifetime, timer, true);
            float easedProgress = 1f - (float)Math.Pow(1f - progress, 2.4f);
            int size = (int)MathHelper.Lerp(96f, 1280f, easedProgress);
            Projectile.width = size;
            Projectile.height = size;
            Projectile.Center = center;

            if (timer == 1)
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.62f, Pitch = 0.2f }, Projectile.Center);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 5 * 60);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = Utils.GetLerpValue(0f, Lifetime, timer, true);
            float opacity = Utils.GetLerpValue(1f, 0f, progress, true);
            float size = Projectile.width;
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            ZhuangFangYiPetVisuals.DrawSquareOutline(Projectile.Center, size, ZhuangFangYiPetVisuals.Teal with { A = 0 } * opacity * 0.72f, 6f, MathHelper.PiOver4);
            ZhuangFangYiPetVisuals.DrawSquareOutline(Projectile.Center, size * 0.62f, ZhuangFangYiPetVisuals.Pale with { A = 0 } * opacity * 0.42f, 3f, 0f);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

    internal sealed class ZhuangFangYiWeakLightning : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Vector2 EndPoint => new(Projectile.ai[0], Projectile.ai[1]);
        private int timer;

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 12;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            timer++;
            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.52f, Pitch = 0.38f }, Projectile.Center);
                ZhuangFangYiPetVisuals.SpawnElectricDust(EndPoint, 30f, 14, 4.5f);
            }

            Projectile.velocity = Vector2.Zero;
            Projectile.Center = Vector2.Lerp(Projectile.Center, EndPoint, 0.02f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                EndPoint,
                18f,
                ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 5 * 60);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacity = Utils.GetLerpValue(12f, 7f, Projectile.timeLeft, true);
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = 0; i < 4; i++)
            {
                Vector2 jitter = Main.rand.NextVector2Circular(5f, 5f);
                Color color = (i % 2 == 0 ? ZhuangFangYiPetVisuals.Teal : ZhuangFangYiPetVisuals.Pale) with { A = 0 };
                ZhuangFangYiPetVisuals.DrawLine(Projectile.Center + jitter, EndPoint - jitter, color * opacity * (0.35f + i * 0.12f), 3f + i);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
