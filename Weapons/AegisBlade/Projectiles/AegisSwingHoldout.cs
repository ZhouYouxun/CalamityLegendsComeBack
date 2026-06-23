using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    // 左键挥舞holdout。一次性直接挥舞，65%进度时向两侧发射火球。
    // 后期解锁（世纪之花后）升级为4个火球。
    public class AegisSwingHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/AegisBlade";

        private Player Owner => Main.player[Projectile.owner];

        private int  swingCount      = 0;
        private int  stateTimer      = 0;
        private int  swingDir        = 1;
        private bool fireballSpawned = false;
        private float currentAngle   = 0f;
        private float startAngle     = 0f;
        private float endAngle       = 0f;
        private Vector2 lockedMouseDir = Vector2.UnitX;
        private float scale          = 1f;
        private float fadeIn         = 0f;

        private const int   SwingDuration          = 26;
        private static readonly float SwingArc     = MathHelper.ToRadians(220f);
        private const float FireballSpawnProgress  = 0.65f;

        private static readonly Color TrailColor   = new(255, 200, 80);

        public override void SetDefaults()
        {
            Projectile.width  = Projectile.height = 60;
            Projectile.friendly   = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate  = -1;
            Projectile.usesLocalNPCImmunity  = true;
            Projectile.localNPCHitCooldown   = SwingDuration;
            Projectile.timeLeft = 4;
            Projectile.noEnchantmentVisuals  = true;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }

        private bool IsLeftHeld()
            => Owner.channel
               && (Main.myPlayer != Projectile.owner || Main.mouseLeft)
               && !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;

        private Vector2 GetMouseDir()
        {
            Vector2 mouse = Owner.Calamity().mouseWorld;
            if (mouse == Vector2.Zero) mouse = Main.MouseWorld;
            return Owner.MountedCenter.DirectionTo(mouse).SafeNormalize(Vector2.UnitX);
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<AegisBlade>())
            {
                Projectile.Kill();
                return;
            }

            scale = Owner.GetMeleeScale();
            Owner.heldProj  = Projectile.whoAmI;
            Owner.GetModPlayer<AegisBladePlayer>().IsSwinging = true;

            Owner.itemTime      = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Projectile.timeLeft = 4;
            Projectile.Center   = Owner.MountedCenter;

            DoSwing();

            float armAngle = currentAngle - MathHelper.ToRadians(130f);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.SetCompositeArmBack(true,  Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.itemLocation = Owner.Center;
            Owner.itemRotation = currentAngle;
        }

        private void StartSwing()
        {
            stateTimer    = 0;
            fireballSpawned = false;
            lockedMouseDir = GetMouseDir();

            swingDir = -Math.Sign(Owner.Center.X - Owner.Calamity().mouseWorld.X);
            if (swingDir == 0) swingDir = Owner.direction;
            Owner.direction = swingDir;

            float baseAngle = lockedMouseDir.ToRotation();
            int parity = swingCount % 2 == 0 ? 1 : -1;
            startAngle = baseAngle + MathHelper.ToRadians(-110f * swingDir * parity);
            endAngle   = baseAngle + MathHelper.ToRadians(-110f * swingDir * -parity);
            currentAngle = startAngle;
        }

        private void DoSwing()
        {
            if (stateTimer == 0) StartSwing();

            stateTimer++;
            float progress    = (float)stateTimer / SwingDuration;
            float easedProg   = CalamityUtils.EaseInOutExp(progress, 5f, 2f);
            currentAngle      = MathHelper.Lerp(startAngle, endAngle, easedProg);

            // 65%进度时生成火球
            if (!fireballSpawned && progress >= FireballSpawnProgress && Main.myPlayer == Projectile.owner)
            {
                SpawnFireballs();
                fireballSpawned = true;
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = Main.rand.NextFloat(-0.1f, 0.2f) }, Owner.Center);
            }

            bool hitWindow = progress >= 0.25f && progress <= 0.85f;
            fadeIn = MathHelper.Lerp(fadeIn, hitWindow ? 1f : 0f, hitWindow ? 0.3f : 0.35f);

            EmitSwingTrail(progress);

            if (stateTimer >= SwingDuration)
            {
                if (IsLeftHeld())
                {
                    swingCount++;
                    Projectile.ResetLocalNPCHitImmunity();
                    stateTimer      = 0;
                    fireballSpawned = false;
                }
                else
                {
                    Projectile.Kill();
                }
            }
        }

        // ── 火球生成 ──────────────────────────────────────────────────────

        private void SpawnFireballs()
        {
            int fireballType = ModContent.ProjectileType<AegisFireball>();
            int damage       = (int)(Projectile.damage * 0.6f);
            const float speed = 14f;

            Vector2 bladeDir = currentAngle.ToRotationVector2();
            Vector2 perp     = bladeDir.RotatedBy(MathHelper.PiOver2);
            Vector2 upBias   = new Vector2(0f, -1.5f);

            if (BalanceAegisBlade.FourFireballsUnlocked())
            {
                // 4个火球：垂直双向 + 斜向双向
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.Center,
                    perp * speed + upBias, fireballType, damage, 2f, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.Center,
                    -perp * speed + upBias, fireballType, damage, 2f, Projectile.owner);

                Vector2 diagA = (perp + bladeDir * 0.6f).SafeNormalize(perp) * (speed * 0.85f);
                Vector2 diagB = (-perp + bladeDir * 0.6f).SafeNormalize(-perp) * (speed * 0.85f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.Center,
                    diagA + upBias, fireballType, damage, 2f, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.Center,
                    diagB + upBias, fireballType, damage, 2f, Projectile.owner);
            }
            else
            {
                // 2个火球：两侧
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.Center,
                    perp * speed + upBias, fireballType, damage, 2f, Projectile.owner);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.Center,
                    -perp * speed + upBias, fireballType, damage, 2f, Projectile.owner);
            }
        }

        // ── 伤害判定 ──────────────────────────────────────────────────────

        public override bool? CanDamage()
        {
            float progress = (float)stateTimer / SwingDuration;
            return progress >= 0.25f && progress <= 0.85f ? null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 bladeDir = currentAngle.ToRotationVector2();
            Vector2 bladeTip = Owner.Center + bladeDir * 140f * scale;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(),
                Owner.Center, bladeTip, 28f * scale, ref _) ? null : false;
        }

        // ── 绘制 ──────────────────────────────────────────────────────────

        private void EmitSwingTrail(float progress)
        {
            if (Main.dedServ || !(progress > 0.2f && progress < 0.9f)) return;

            Vector2 bladeDir = currentAngle.ToRotationVector2();
            int swipeDir = Math.Sign(endAngle - startAngle);
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = Owner.Center + bladeDir * Main.rand.NextFloat(40f, 130f) * scale;
                Vector2 vel = bladeDir.RotatedBy(MathHelper.PiOver2 * swipeDir) * Main.rand.NextFloat(2f, 5f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(pos, vel, false,
                    Main.rand.Next(6, 12), Main.rand.NextFloat(0.04f, 0.07f) * scale,
                    Main.rand.NextBool(3) ? Color.White : TrailColor,
                    new Vector2(1.4f, 0.2f), true, false, 0.75f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Texture2D tex      = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin     = new Vector2(0f, tex.Height);
            float drawRotation = currentAngle + MathHelper.PiOver4;
            Vector2 drawPos    = Owner.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);

            // 挥砍弧光 smear
            if (fadeIn > 0.01f)
            {
                Texture2D swoosh = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge").Value;
                float swipeDir   = Math.Sign(endAngle - startAngle);
                float smearRot   = drawRotation + MathHelper.PiOver2 * swipeDir;
                Main.EntitySpriteDraw(swoosh, drawPos, null,
                    TrailColor with { A = 0 } * fadeIn * 0.38f,
                    smearRot, swoosh.Size() * 0.5f, scale * 0.72f, SpriteEffects.None, 0);
            }

            // 刀尖光晕
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 bladeTip = Owner.Center + currentAngle.ToRotationVector2() * 120f * scale - Main.screenPosition;
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, bladeTip, null,
                TrailColor with { A = 0 } * 0.35f,
                0f, bloom.Size() * 0.5f, scale * 0.6f, SpriteEffects.None, 0);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            Main.EntitySpriteDraw(tex, drawPos, null, lightColor, drawRotation, origin, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
