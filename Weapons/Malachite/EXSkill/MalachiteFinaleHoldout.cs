using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Malachite.EXSkill
{
    // The EX finale's continuous hand-held cutter. Its seven-frame texture loops every 21 ticks.
    public sealed class MalachiteFinaleHoldout : ModProjectile, ILocalizedModType
    {
        private const int FrameCount = 7;
        private const int FrameDuration = 3;
        private const int DamageInterval = 3;
        private const float HoldoutDistance = 38f;
        private const float BladeReach = 94f;
        private const float BladeWidth = 28f;

        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/EXSkill/孔雀翎刀光";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private ref float Lifetime => ref Projectile.ai[0];
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 84;
            Projectile.height = 56;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = DamageInterval;
        }

        public override bool ShouldUpdatePosition() => false;

        // The active frame is the strike frame: a target can only be cut once per three ticks.
        public override bool? CanDamage() => Timer > 0f && (int)Timer % DamageInterval == 0;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Timer++;
            Projectile.timeLeft = 2;
            if (Lifetime > 0f && Timer >= Lifetime)
            {
                Projectile.Kill();
                return;
            }

            UpdateAim(owner);
            UpdateHeldPose(owner);
            Projectile.frame = ((int)Timer / FrameDuration) % FrameCount;
            Lighting.AddLight(Projectile.Center, 0.12f, 0.42f, 0.12f);
        }

        private void UpdateAim(Player owner)
        {
            Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 mouseWorld = owner.Calamity().mouseWorld;
                if (mouseWorld == Vector2.Zero)
                    mouseWorld = Main.MouseWorld;

                aim = (mouseWorld - owner.MountedCenter).SafeNormalize(aim);
                if (Vector2.Dot(aim, Projectile.velocity.SafeNormalize(aim)) < 0.9995f)
                    Projectile.netUpdate = true;
            }

            Projectile.velocity = aim;
            Projectile.rotation = aim.ToRotation();
            Projectile.direction = Projectile.spriteDirection = aim.X >= 0f ? 1 : -1;
        }

        private void UpdateHeldPose(Player owner)
        {
            Vector2 armPosition = owner.RotatedRelativePoint(owner.MountedCenter, true);
            Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            Projectile.Center = armPosition + aim * HoldoutDistance + new Vector2(0f, owner.gfxOffY - 4f * owner.gravDir);

            owner.ChangeDir(Projectile.direction);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = Math.Max(owner.itemTime, 2);
            owner.itemAnimation = Math.Max(owner.itemAnimation, 2);
            owner.itemRotation = (aim * Projectile.direction).ToRotation();

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * owner.gravDir;
            if (owner.gravDir == -1f)
                armRotation += MathHelper.Pi;

            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRotation + MathHelper.ToRadians(8f) * Projectile.direction);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            Vector2 start = owner.MountedCenter - aim * 8f;
            Vector2 end = Projectile.Center + aim * BladeReach;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, BladeWidth, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 3 * 60);
            target.AddBuff(ModContent.BuffType<Plague>(), 2 * 60);

            if (Projectile.owner == Main.myPlayer && !Main.dedServ)
                SpawnCutEffects(target.Center);
        }

        private void SpawnCutEffects(Vector2 center)
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            Color green = new(100, 255, 135);

            for (int i = 0; i < 4; i++)
            {
                Vector2 position = center + normal * Main.rand.NextFloat(-24f, 24f) - forward * Main.rand.NextFloat(8f, 34f);
                Vector2 velocity = forward * Main.rand.NextFloat(4.5f, 8.5f) + normal * Main.rand.NextFloat(-1.8f, 1.8f);
                Color color = Color.Lerp(green, Color.White, Main.rand.NextFloat(0.12f, 0.36f));
                GeneralParticleHandler.SpawnParticle(new LineParticle(position, velocity * 0.38f, false, Main.rand.Next(9, 14), Main.rand.NextFloat(0.38f, 0.58f), color));
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(2.4f, 2.4f) + forward * Main.rand.NextFloat(0.8f, 2.4f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center + Main.rand.NextVector2Circular(18f, 18f),
                    velocity,
                    false,
                    Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.32f, 0.48f),
                    Color.Lerp(green, Color.White, 0.25f),
                    true,
                    false,
                    true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, FrameCount, 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color glow = new Color(70, 255, 125, 0) * 0.34f;

            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (MathHelper.PiOver2 * i).ToRotationVector2() * 1.6f;
                Main.EntitySpriteDraw(texture, drawPosition + offset, frame, glow, Projectile.rotation, origin, 1.12f, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, drawPosition, frame, Color.Lerp(lightColor, Color.White, 0.45f), Projectile.rotation, origin, 1f, SpriteEffects.None);
            return false;
        }
    }
}
