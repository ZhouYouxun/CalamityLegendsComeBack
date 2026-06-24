using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityLegendsComeBack.Weapons.Visuals;

namespace CalamityLegendsComeBack.Weapons.A_Olds.PhoenixBlade
{
    internal class PhoenixBladeHoldout : BaseCustomUseStyleProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PhoenixBlade";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Olds/PhoenixBlade/凤凰之刃";
        public override int AssignedItemID => ModContent.ItemType<PhoenixBlade>();

        public override Vector2 SpriteOrigin => new Vector2(0f, 106f);
        public override float HitboxOutset => 90f;
        public override Vector2 HitboxSize => new Vector2(150f, 150f);
        public override float HitboxRotationOffset => MathHelper.ToRadians(-45f);

        private float spinAngle;
        private int useAnim;
        private float fadeIn;

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12; // hits every 12 frames
            Projectile.extraUpdates = 0;
            Projectile.scale = 1.25f; // Nice size
        }

        public override void WhenSpawned()
        {
            IgnoreActiveAnimation = true;
            DrawUnconditionally = true;
            Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;
            Projectile.knockBack = 0f;

            spinAngle = (Main.MouseWorld - Owner.MountedCenter).ToRotation();
            int facing = spinAngle.ToRotationVector2().X >= 0f ? 1 : -1;
            Owner.direction = facing;
            FlipAsSword = Owner.direction == -1;
            Projectile.ai[1] = -1f;
            CanHit = true;
        }

        public override void AI()
        {
            if (whenSpawned)
            {
                WhenSpawned();
                whenSpawned = false;
                Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;
                Projectile.netUpdate = true;
            }

            if (Owner.HeldItem.type != AssignedItemID || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Owner.Calamity().mouseWorldListener = true;
            Owner.Calamity().rightClickListener = true;

            Animation++;
            UseStyle();
            Owner.heldProj = Projectile.whoAmI;

            int itemAnimationMax = Math.Max(1, Owner.itemAnimationMax);
            AnimationProgress = Animation % itemAnimationMax;

            if (AbsolutePosition == Vector2.Zero)
                Projectile.position = Owner.position + Owner.Size / 2f - Projectile.Size / 2f + Offset;
            else
            {
                AbsolutePosition += Projectile.velocity;
                Projectile.position = AbsolutePosition - Projectile.Size / 2f + Offset;
            }

            // Ensure the correct center positioning is applied after base class position updates
            Projectile.Center = Owner.MountedCenter + spinAngle.ToRotationVector2() * Projectile.scale * 10f;
        }

        private bool IsLeftHeld()
        {
            return Owner.channel &&
                   (Main.myPlayer != Projectile.owner || Main.mouseLeft) &&
                   !Main.mapFullscreen &&
                   !Main.blockMouse;
        }

        public override void UseStyle()
        {
            Player owner = Owner;
            useAnim = Math.Max(1, owner.itemAnimationMax);
            AnimationProgress = Animation % useAnim;
            DrawUnconditionally = true;

            if (IsLeftHeld())
            {
                Projectile.timeLeft = Math.Max(Projectile.timeLeft, 2);
                owner.itemTime = Math.Max(owner.itemTime, 2);
                owner.itemAnimation = Math.Max(owner.itemAnimation, 2);
            }
            else
            {
                if (AnimationProgress == useAnim - 1)
                {
                    Projectile.Kill();
                    return;
                }
            }

            // Continuous rotation
            float spinRate = MathHelper.TwoPi / useAnim * owner.direction;
            spinAngle += spinRate;
            spinAngle = MathHelper.WrapAngle(spinAngle);

            // Update facing direction
            int facing = spinAngle.ToRotationVector2().X >= 0f ? 1 : -1;
            owner.ChangeDir(facing);
            FlipAsSword = facing < 0;

            // Rotation and center
            Projectile.rotation = spinAngle + MathHelper.PiOver4;
            Projectile.Center = owner.MountedCenter + spinAngle.ToRotationVector2() * Projectile.scale * 10f;

            fadeIn = MathHelper.Lerp(fadeIn, 1f, 0.15f);

            // Set composite arm
            ArmRotationOffset = MathHelper.ToRadians(-135f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-102f);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffset);
            owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + RotationOffset + ArmRotationOffsetBack);

            owner.itemRotation = spinAngle;
            if (owner.direction != 1)
                owner.itemRotation -= MathHelper.Pi;
            owner.itemRotation = MathHelper.WrapAngle(owner.itemRotation);

            SpawnSpinParticles();
        }

        private Vector2 GetBladeTip()
        {
            Vector2 bladeDirection = spinAngle.ToRotationVector2();
            float bladeReach = 135f * Projectile.scale;
            return Owner.MountedCenter + bladeDirection * bladeReach;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 bladeStart = Owner.MountedCenter;
            Vector2 bladeEnd = GetBladeTip();
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), bladeStart, bladeEnd, 18f * Projectile.scale, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                // On hit, spawn the FuckYou explosion projectile
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<FuckYou>(),
                    (int)(Projectile.damage * 0.9f),
                    0f,
                    Projectile.owner,
                    132f // blast size
                );
            }
        }

        private void SpawnSpinParticles()
        {
            if (Main.rand.NextFloat() < 0.6f)
            {
                Vector2 tip = GetBladeTip();
                Dust dust = Dust.NewDustPerfect(
                    tip,
                    DustID.SolarFlare,
                    spinAngle.ToRotationVector2().RotatedBy(MathHelper.PiOver2 * Owner.direction) * Main.rand.NextFloat(2f, 5f),
                    100,
                    default,
                    Main.rand.NextFloat(1f, 1.5f)
                );
                dust.noGravity = true;
            }

            if (Main.rand.NextFloat() < 0.3f)
            {
                float dist = Main.rand.NextFloat(10f, 120f) * Projectile.scale;
                Vector2 pos = Owner.MountedCenter + spinAngle.ToRotationVector2() * dist;
                Dust dust = Dust.NewDustPerfect(
                    pos,
                    DustID.Torch,
                    Main.rand.NextVector2Circular(1f, 1f),
                    100,
                    default,
                    Main.rand.NextFloat(0.8f, 1.2f)
                );
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!DrawUnconditionally && Owner.itemAnimation <= 0)
                return false;

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = FlipAsSword ? new Vector2(texture.Width - SpriteOrigin.X, SpriteOrigin.Y) : SpriteOrigin;
            float r = FlipAsSword ? MathHelper.ToRadians(90f) : 0f;
            SpriteEffects effects = FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            Vector2 bladeScale = Vector2.One * Projectile.scale;

            // 1. Draw outline (包边效果)
            HoldoutOutlineHelper.DrawSolidOutline(
                texture,
                drawPosition,
                Projectile.rotation + RotationOffset + r,
                origin,
                bladeScale,
                effects,
                new Color(255, 95, 36),
                3.5f * Projectile.scale,
                fadeIn,
                Main.GlobalTimeWrappedHourly
            );

            // 2. Draw circular fire smear VFX
            Asset<Texture2D> smearTex = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearFire2");
            Vector2 smearDrawPos = Owner.MountedCenter - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            float smearRot = spinAngle;
            if (Owner.direction < 0)
                smearRot = spinAngle + MathHelper.Pi;

            float smearScale = (135f * Projectile.scale) / 78f;
            Color smearColor = new Color(255, 120, 30) with { A = 0 } * fadeIn * 0.65f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                smearTex.Value,
                smearDrawPos,
                null,
                smearColor,
                smearRot,
                smearTex.Size() * 0.5f,
                smearScale,
                Owner.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0f
            );
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            // 3. Draw sword body
            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                lightColor,
                Projectile.rotation + RotationOffset + r,
                origin,
                bladeScale,
                effects,
                0f
            );

            return false;
        }
    }
}
