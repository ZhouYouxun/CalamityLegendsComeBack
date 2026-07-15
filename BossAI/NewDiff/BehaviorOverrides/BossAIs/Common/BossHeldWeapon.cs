using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.WeaponAttacks;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common
{
    // =====================================================================================================================
    // BOSS-HELD WEAPON FRAMEWORK
    // Ported from the anchor/easing/damage-gate design of CalamityMod's BaseCustomUseStyleProjectile, adapted for NPCs:
    //   - The weapon is its own projectile, pinned every frame to the anchor NPC (ai[0] = npc.whoAmI).
    //   - The sprite rotates around its HANDLE (bottom-left of the item sprite), like a hand gripping it.
    //   - FinalRotation = BaseRotation (aim) + RotationOffset (animation). Blade direction == FinalRotation.
    //   - Damage is gated by CanHit and collides along the blade via a projected hitbox (Colliding, hostile-vs-player).
    //
    // Two concrete styles:
    //   BossHeldSwingWeapon — windup (ease-in heave) -> optional apex quiver -> eased slash -> recovery, then self-kill.
    //   BossHeldAimedWeapon — persistent hold; Pulse(+) thrusts toward the aim (damaging), Pulse(-) recoils away (cosmetic).
    // =====================================================================================================================
    internal abstract class BossHeldWeaponBase : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        /// <summary>LegendsWeaponBossRegistry key of the weapon item whose sprite this projectile wears.</summary>
        public abstract string WeaponName { get; }

        public virtual float SpriteScale => 1.4f;
        public virtual float HitboxOutset => 60f;
        public virtual Vector2 HitboxSize => new(70f, 70f);
        public virtual Color GlowColor => Color.DeepSkyBlue * 0.5f;

        /// <summary>Extra offset of the grip from the anchor's center (e.g. hold a bow above the head).</summary>
        public virtual Vector2 GripOffset => Vector2.Zero;

        public NPC Anchor => Main.npc[(int)Projectile.ai[0]];

        /// <summary>Damage gate — subclasses open this only during the dangerous part of the animation.</summary>
        public bool CanHit;

        /// <summary>Aim direction of the strike plane, in radians.</summary>
        public float BaseRotation;

        /// <summary>Animation offset relative to the aim, in radians (the "swing").</summary>
        public float RotationOffset;

        public float FinalRotation => BaseRotation + RotationOffset;

        /// <summary>Offset along the aim axis (thrust forward / recoil back), in pixels.</summary>
        public float AxisOutset;

        /// <summary>0-1: draws trailing rotational ghosts while > 0 (motion blur for swings).</summary>
        public float MotionGhost;

        /// <summary>Radian step between trailing ghosts; sign should oppose the swing direction.</summary>
        public virtual float TrailStep => 0f;

        public override void SetDefaults()
        {
            Projectile.width = (int)Math.Max(HitboxSize.X, 1f);
            Projectile.height = (int)Math.Max(HitboxSize.Y, 1f);
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3600;
        }

        public sealed override void AI()
        {
            int anchorIndex = (int)Projectile.ai[0];
            if (anchorIndex < 0 || anchorIndex >= Main.maxNPCs || !Anchor.active)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[0] == 0f)
            {
                // Velocity carries the initial aim, then the weapon is pinned
                BaseRotation = Projectile.velocity.ToRotation();
                Projectile.velocity = Vector2.Zero;
                OnSpawned();
            }
            Projectile.localAI[0]++;

            HeldBehavior(Projectile.localAI[0]);

            Projectile.Center = Anchor.Center + GripOffset + BaseRotation.ToRotationVector2() * AxisOutset;
            Projectile.rotation = FinalRotation;
        }

        public virtual void OnSpawned() { }

        /// <summary>Per-frame animation logic. time starts at 1 on the first frame.</summary>
        public abstract void HeldBehavior(float time);

        public override bool? CanDamage()
        {
            if (Projectile.damage <= 0)
                return false;
            return CanHit ? null : (bool?)false;
        }

        // Hostile-vs-player collision follows the blade, not the pinned center
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!CanHit)
                return false;
            Vector2 size = HitboxSize * Projectile.scale;
            Vector2 cen = Projectile.Center + new Vector2(HitboxOutset * Projectile.scale, 0f).RotatedBy(FinalRotation);
            Rectangle blade = new((int)(cen.X - size.X / 2f), (int)(cen.Y - size.Y / 2f), (int)size.X, (int)size.Y);
            return blade.Intersects(targetHitbox);
        }

        /// <summary>World position at dist pixels along the blade — for edge particles.</summary>
        public Vector2 BladePoint(float dist) => Projectile.Center + new Vector2(dist, 0f).RotatedBy(FinalRotation);

        public override bool PreDraw(ref Color lightColor)
        {
            int itemType = LegendsWeaponBossRegistry.GetItemType(WeaponName);
            if (itemType <= 0 || itemType >= TextureAssets.Item.Length)
                return false;
            Main.instance.LoadItem(itemType);
            Texture2D tex = TextureAssets.Item[itemType].Value;
            if (tex == null)
                return false;

            // Item sprites point up-right (blade at -45°); rotating the sprite by aim+45° puts the blade on FinalRotation.
            // Origin at the bottom-left = the handle, so everything pivots around the grip.
            Vector2 origin = new(0f, tex.Height);
            float drawRot = FinalRotation + MathHelper.PiOver4;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float scale = SpriteScale * Anchor.scale;
            float opacity = Projectile.Opacity * Anchor.Opacity;

            // Trailing rotational ghosts (motion blur) while swinging
            if (MotionGhost > 0.05f && TrailStep != 0f)
            {
                for (int i = 1; i <= 4; i++)
                {
                    float ghostRot = drawRot - TrailStep * i;
                    Color ghostColor = GlowColor * MotionGhost * (1f - i / 5f) * opacity;
                    Main.EntitySpriteDraw(tex, drawPos, null, ghostColor, ghostRot, origin, scale, SpriteEffects.None);
                }
            }

            // Thick zero-alpha backglow ring — brightens without darkening the scene behind it
            Color glow = GlowColor * opacity;
            glow.A = 0;
            for (int i = 0; i < 12; i++)
            {
                Vector2 off = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 3.5f;
                Main.EntitySpriteDraw(tex, drawPos + off, null, glow, drawRot, origin, scale, SpriteEffects.None);
            }
            Main.EntitySpriteDraw(tex, drawPos, null, Color.White * opacity, drawRot, origin, scale, SpriteEffects.None);
            return false;
        }
    }

    // =====================================================================================================================
    // SWING: one full strike per spawn — heave back, (quiver), explosive eased slash, recovery, self-kill.
    // ai[1] = swing direction (+1 / -1). Fully deterministic from spawn state, so it stays in sync in multiplayer.
    // =====================================================================================================================
    internal abstract class BossHeldSwingWeapon : BossHeldWeaponBase
    {
        public virtual int WindupTime => 30;
        public virtual int HoldTime => 0;
        public virtual int SlashTime => 16;
        public virtual int RecoveryTime => 24;

        /// <summary>Radians behind the aim at the end of the windup (negative = raised against the swing).</summary>
        public virtual float WindupAngle => -2.4f;

        /// <summary>Radians past the aim at the end of the slash.</summary>
        public virtual float FollowThroughAngle => 1.2f;

        public virtual float HitWindowStart => 0.2f;
        public virtual float HitWindowEnd => 0.9f;

        /// <summary>Re-aim along the anchor's velocity when the slash starts, so the blade follows the body's dash.</summary>
        public virtual bool AimAlongAnchorVelocityAtSlash => true;

        /// <summary>Gentle aim tracking toward the anchor's target during the windup (0 = locked).</summary>
        public virtual float WindupAimLerp => 0.08f;

        public float SwingDir => Projectile.ai[1] >= 0f ? 1f : -1f;

        /// <summary>ai[2] > 0 overrides WindupTime per spawn, letting the AI sync one weapon to different telegraph windows.</summary>
        public int EffectiveWindup => Projectile.ai[2] > 0f ? (int)Projectile.ai[2] : WindupTime;

        public override float TrailStep => SwingDir * 0.16f;

        public sealed override void HeldBehavior(float time)
        {
            int slashStart = EffectiveWindup + HoldTime;
            int slashEnd = slashStart + SlashTime;

            if (time <= EffectiveWindup)
            {
                // Ease-in heave: slow at first, committing near the apex
                float p = time / EffectiveWindup;
                RotationOffset = MathHelper.Lerp(0f, WindupAngle, p * p) * SwingDir;
                CanHit = false;
                MotionGhost = 0f;

                if (WindupAimLerp > 0f && Anchor.HasValidTarget)
                {
                    Player target = Main.player[Anchor.target];
                    BaseRotation = BaseRotation.AngleLerp((target.Center - Anchor.Center).ToRotation(), WindupAimLerp);
                }
            }
            else if (time <= slashStart)
            {
                // Apex quiver — the blade trembles at full raise, telegraphing the strike
                RotationOffset = (WindupAngle + MathF.Sin((float)Main.GameUpdateCount * 1.1f) * 0.05f) * SwingDir;
                CanHit = false;
            }
            else if (time <= slashEnd)
            {
                float p = (time - slashStart) / SlashTime;
                if (time == slashStart + 1)
                {
                    if (AimAlongAnchorVelocityAtSlash && Anchor.velocity.LengthSquared() > 4f)
                        BaseRotation = Anchor.velocity.ToRotation();
                    OnSlashBegin();
                }

                // Explosive ease-out whip
                float eased = 1f - MathF.Pow(1f - p, 3f);
                RotationOffset = MathHelper.Lerp(WindupAngle, FollowThroughAngle, eased) * SwingDir;
                CanHit = p >= HitWindowStart && p <= HitWindowEnd;
                MotionGhost = MathF.Sin(p * MathHelper.Pi);
                OnSlashing(p);
            }
            else if (time <= slashEnd + RecoveryTime)
            {
                float p = (time - slashEnd) / RecoveryTime;
                RotationOffset = MathHelper.Lerp(FollowThroughAngle, 0.25f, p) * SwingDir;
                CanHit = false;
                MotionGhost = MathHelper.Lerp(MotionGhost, 0f, 0.2f);
            }
            else
            {
                Projectile.Kill();
            }
        }

        /// <summary>Called on the first frame of the slash (sound, screen shake, projectile release...).</summary>
        public virtual void OnSlashBegin() { }

        /// <summary>Called every slash frame with progress 0-1 (edge particles...).</summary>
        public virtual void OnSlashing(float slashProgress) { }
    }

    // =====================================================================================================================
    // AIMED: persistent hold pointing along BaseRotation. The boss AI drives it:
    //   SetAim(rot)      — snap/retarget the aim line.
    //   Pulse(+strength) — thrust toward the aim: sharp out (damaging on the way out), eased back.
    //   Pulse(-strength) — recoil away from the aim after firing: never damages.
    // Pulses are cosmetic-first and triggered server-side; the held sprite itself is what players read.
    // =====================================================================================================================
    internal abstract class BossHeldAimedWeapon : BossHeldWeaponBase
    {
        /// <summary>Continuous aim tracking toward the anchor's target (0 = fully AI-driven via SetAim).</summary>
        public virtual float AimLerp => 0f;

        public virtual int PulseDuration => 14;

        /// <summary>Resting distance of the grip from the anchor along the aim axis.</summary>
        public virtual float RestOutset => 0f;

        private float pulseTimer = -1f;
        private float pulseStrength;

        public void SetAim(float rotation) => BaseRotation = rotation;

        public void Pulse(float strength)
        {
            pulseTimer = 0f;
            pulseStrength = strength;
        }

        public sealed override void HeldBehavior(float time)
        {
            if (AimLerp > 0f && Anchor.HasValidTarget)
            {
                Player target = Main.player[Anchor.target];
                BaseRotation = BaseRotation.AngleLerp((target.Center - Anchor.Center).ToRotation(), AimLerp);
            }

            float outset = RestOutset;
            CanHit = false;
            MotionGhost = 0f;

            if (pulseTimer >= 0f)
            {
                pulseTimer++;
                float p = pulseTimer / PulseDuration;
                if (p >= 1f)
                {
                    pulseTimer = -1f;
                }
                else
                {
                    // Sharp attack out, smooth ride back
                    float k = p < 0.35f ? 1f - MathF.Pow(1f - p / 0.35f, 3f) : 1f - (p - 0.35f) / 0.65f;
                    outset += pulseStrength * k;
                    CanHit = pulseStrength > 0f && p < 0.5f;
                    MotionGhost = pulseStrength > 0f ? MathF.Sin(p * MathHelper.Pi) : 0f;
                }
            }

            AxisOutset = outset;
            HeldExtra(time);
        }

        /// <summary>Extra per-frame behavior after the pulse/aim update.</summary>
        public virtual void HeldExtra(float time) { }
    }
}
