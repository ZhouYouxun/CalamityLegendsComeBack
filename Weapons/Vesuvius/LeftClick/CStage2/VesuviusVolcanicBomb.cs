using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.AStage0;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.CStage2
{
    public class VesuviusVolcanicBomb : VesuviusMoltenAsteroid
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 42;
            Projectile.height = 42;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f && Projectile.ai[1] <= 0f)
                Projectile.ai[1] = 1.2f;

            base.AI();
            VesuviusProjectileVisuals.SpawnBombTrail(Projectile, 1.05f);
            Projectile.velocity.Y += 0.08f;
            Projectile.velocity *= 0.992f;
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 oldCenter = Projectile.Center;

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    oldCenter,
                    Vector2.Zero,
                    ModContent.ProjectileType<VesuviusLingeringLava>(),
                    Math.Max(1, (int)(Projectile.damage * 0.42f)),
                    0f,
                    Projectile.owner,
                    82f * Projectile.scale);
            }

            base.OnKill(timeLeft);

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.78f, Pitch = -0.18f }, oldCenter);
                VesuviusProjectileVisuals.SpawnBombDetonation(oldCenter, Projectile.scale);
            }
        }
    }

    public class VesuviusLingeringLava : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 88;
            Projectile.height = 88;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                float size = Projectile.ai[0] <= 0f ? 80f : Projectile.ai[0];
                Projectile.Resize((int)size, (int)(size * 0.65f));
                Projectile.localAI[0] = 1f;
            }

            if (!Main.dedServ)
            {
                VesuviusProjectileVisuals.SpawnLavaPoolBubble(
                    Projectile.Center,
                    new Vector2(Projectile.width * 0.45f, Projectile.height * 0.3f),
                    Projectile.timeLeft > 28 ? 1f : Utils.GetLerpValue(0f, 28f, Projectile.timeLeft, true));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smoke = ModContent.Request<Texture2D>("CalamityMod/Particles/HighResFoggyCircleHardEdge").Value;
            float fade = Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + Projectile.identity);
            Vector2 poolScale = new(Projectile.width / (float)bloom.Width, Projectile.height / (float)bloom.Height);

            Main.EntitySpriteDraw(
                smoke,
                Projectile.Center - Main.screenPosition - Vector2.UnitY * 8f,
                null,
                Color.Lerp(Color.Black, VesuviusProjectileVisuals.RavagerSmoke, 0.55f) * 0.22f * fade,
                Projectile.rotation + pulse * 0.12f,
                smoke.Size() * 0.5f,
                poolScale * new Vector2(1.9f, 0.95f),
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(255, 70, 20, 0) * (0.2f + pulse * 0.05f) * fade,
                0f,
                bloom.Size() * 0.5f,
                poolScale * 1.55f,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                VesuviusProjectileVisuals.LavaGold with { A = 0 } * 0.16f * fade,
                0f,
                bloom.Size() * 0.5f,
                poolScale * new Vector2(0.82f, 0.42f),
                SpriteEffects.None);

            return false;
        }
    }
}
