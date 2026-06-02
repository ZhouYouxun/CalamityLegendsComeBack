using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFBrimstoneElemental_Barrage : ModProjectile, ILocalizedModType
    {
        private ref float Timer => ref Projectile.localAI[0];
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Boss/BrimstoneBarrage";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 44;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 450;
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
                Projectile.frameCounter = 0;
            }

            float trackingPower = Utils.GetLerpValue(0f, 72f, Timer, true);
            float speed = MathHelper.Lerp(Projectile.velocity.Length(), MathHelper.Lerp(15f, 31f, trackingPower), 0.075f + trackingPower * 0.075f);
            NPC target = FindTarget(1050f);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (target != null)
            {
                Vector2 desiredDirection = Projectile.SafeDirectionTo(target.Center, direction);
                float maxTurn = MathHelper.Lerp(MathHelper.ToRadians(7f), MathHelper.ToRadians(34f), trackingPower);
                direction = direction.ToRotation().AngleTowards(desiredDirection.ToRotation(), maxTurn).ToRotationVector2();
            }
            Projectile.velocity = direction * speed;

            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.68f);
            EmitHellbornFlightEffects(direction);
        }

        private NPC FindTarget(float range)
        {
            NPC closest = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        private void EmitHellbornFlightEffects(Vector2 direction)
        {
            if (Main.dedServ)
                return;

            float sine = (float)System.Math.Sin(Projectile.timeLeft * 0.575f / System.Math.PI);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2) * sine * 16f;
            SpawnSquashDust(Projectile.Center + side, direction);
            SpawnSquashDust(Projectile.Center - side, direction);

            Dust diamond = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(22f, 22f), ModContent.DustType<DiamondDust>(), -Projectile.velocity * Main.rand.NextFloat(0.08f, 0.24f));
            diamond.noGravity = true;
            diamond.scale = Main.rand.NextFloat(0.62f, 0.82f);
            diamond.color = ThemeColor;
            diamond.fadeIn = 1f;
            diamond.noLight = true;
        }

        private void SpawnSquashDust(Vector2 position, Vector2 direction)
        {
            if (!Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(position, ModContent.DustType<SquashDust>(), -direction * Main.rand.NextFloat(3f, 5f));
            dust.noGravity = true;
            dust.scale = Main.rand.NextFloat(2.3f, 2.7f);
            dust.color = ThemeColor;
            dust.fadeIn = 2.5f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 360);

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, ThemeColor * 0.72f, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.8f, 1.35f, 16));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, 14f * Projectile.scale, targetHitbox);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], ThemeColor * 0.62f, 1);
            Main.EntitySpriteDraw(texture, drawPosition, frame, Color.Lerp(ThemeColor, Color.White, 0.24f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
