using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle
{
    internal sealed class DesertEagleHoldout : BaseGunHoldoutProjectile
    {
        private enum HoldoutState
        {
            Spinning = 0,
            Recovery = 1
        }

        private const int RecoveryFrames = 60;

        public override string Texture => DesertEagle.TextureAssetPath;
        public override int AssociatedItemID => ModContent.ItemType<DesertEagle>();
        public override float MaxOffsetLengthFromArm => 22f;
        public override float OffsetXUpwards => -4f;
        public override float BaseOffsetY => -4f;
        public override float OffsetYDownwards => 4f;
        public override float RecoilResolveSpeed => 0.42f;

        public ref float State => ref Projectile.ai[0];
        public ref float ChargeTimer => ref Projectile.ai[1];
        public ref float RecoveryTimer => ref Projectile.ai[2];

        private float spinVisualRotation;
        private float lockedRotation;

        private new Player Owner => Main.player[Projectile.owner];
        private bool RightHeld =>
            Main.myPlayer == Projectile.owner &&
            Owner.Calamity().mouseRight &&
            !Main.mapFullscreen &&
            !Main.blockMouse;

        private bool RecoveryState => (HoldoutState)(int)State == HoldoutState.Recovery;
        private bool FullyCharged => ChargeTimer >= DesertEaglePlayer.SpinChargeMax;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 82;
            Projectile.height = 46;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void HoldoutAI()
        {
            if (!Owner.active || Owner.dead || Owner.CCed || Owner.noItems || Owner.HeldItem.type != AssociatedItemID)
            {
                Projectile.Kill();
                return;
            }

            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Owner.whoAmI)
                Owner.Calamity().rightClickListener = true;

            Vector2 aimDirection = (Owner.Calamity().mouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
            Projectile.velocity = aimDirection;
            Projectile.rotation = RecoveryState ? lockedRotation : aimDirection.ToRotation();

            Owner.ChangeDir(Projectile.velocity.X >= 0f ? 1 : -1);

            DesertEaglePlayer playerState = Owner.GetModPlayer<DesertEaglePlayer>();
            playerState.SetHoldingDesertEagle();

            if (RecoveryState)
            {
                HandleRecoveryState(playerState);
                return;
            }

            if (!RightHeld)
            {
                playerState.UpdateChargeBar(false, 0f);
                Projectile.Kill();
                return;
            }

            ChargeTimer = MathHelper.Clamp(ChargeTimer + 1f, 0f, DesertEaglePlayer.SpinChargeMax);
            playerState.UpdateChargeBar(true, ChargeTimer / DesertEaglePlayer.SpinChargeMax);

            OffsetLengthFromArm = MathHelper.Lerp(OffsetLengthFromArm, 10f, 0.35f);
            spinVisualRotation += 0.5f * Projectile.direction;
            Lighting.AddLight(Projectile.Center, DesertEagleEffects.SilverMain.ToVector3() * 0.55f);

            if (Main.rand.NextBool())
                DesertEagleEffects.SpawnSilverSpinTrail(Projectile.Center, Projectile.velocity, spinVisualRotation, 1f);

            if (Main.myPlayer == Owner.whoAmI && FullyCharged && Main.mouseLeft && Main.mouseLeftRelease)
                FireHeavyShot();
        }

        private void HandleRecoveryState(DesertEaglePlayer playerState)
        {
            playerState.UpdateChargeBar(false, 0f);
            RecoveryTimer--;
            OffsetLengthFromArm = MathHelper.Lerp(OffsetLengthFromArm, 18f, 0.12f);
            Projectile.rotation = lockedRotation;

            if (Main.rand.NextBool(2))
            {
                Vector2 upwardSmoke = new Vector2(Main.rand.NextFloat(-0.25f, 0.25f), Main.rand.NextFloat(-2.8f, -1.2f));
                DesertEagleEffects.SpawnHeavySmoke(GunTipPosition + new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f)), upwardSmoke, 0.95f);
            }

            if (RecoveryTimer <= 0f)
            {
                if (RightHeld)
                {
                    State = (int)HoldoutState.Spinning;
                    ChargeTimer = 0f;
                    return;
                }

                Projectile.Kill();
            }
        }

        private void FireHeavyShot()
        {
            Vector2 shotDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * Projectile.direction);
            lockedRotation = shotDirection.ToRotation();
            State = (int)HoldoutState.Recovery;
            RecoveryTimer = RecoveryFrames;
            ChargeTimer = 0f;

            OffsetLengthFromArm -= 20f;
            Owner.velocity -= shotDirection * 12f;
            Owner.SetScreenshake(10f);

            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTipPosition,
                    shotDirection * 22f,
                    ModContent.ProjectileType<DesertEagleHeavyRound>(),
                    (int)(Projectile.damage * 3.8f),
                    Projectile.knockBack * 3f,
                    Projectile.owner);
            }

            SoundEngine.PlaySound(SoundID.Item38 with { Volume = 1.1f, Pitch = -0.18f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.8f, Pitch = -0.3f }, Projectile.Center);
            DesertEagleEffects.SpawnSilverMuzzleFlash(GunTipPosition, shotDirection, 1.65f);
            DesertEagleEffects.SpawnSilverImpact(GunTipPosition + shotDirection * 10f, shotDirection, 1.25f, true);
            for (int i = 0; i < 4; i++)
                DesertEagleEffects.SpawnHeavySmoke(GunTipPosition, -shotDirection * Main.rand.NextFloat(0.8f, 2.4f) + Main.rand.NextVector2Circular(0.8f, 0.8f), 1.05f);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (RecoveryState)
            {
                modifiers.SourceDamage *= 0f;
                return;
            }

            modifiers.SourceDamage *= 1.8f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (RecoveryState)
                return;

            DesertEagleEffects.SpawnSilverImpact(target.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), 1.15f, true);
            Owner.velocity -= Projectile.velocity.SafeNormalize(Vector2.UnitX) * 4f;
            Projectile.Kill();
        }

        public override bool? CanDamage() => RecoveryState ? false : null;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (RecoveryState)
                return false;

            return CalamityUtils.CircularHitboxCollision(Projectile.Center, 68f, targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float drawRotation = Projectile.rotation + (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f) - (Owner.gravDir == -1 ? MathHelper.Pi * Owner.direction : 0f);
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects flipSprite = (Projectile.spriteDirection * Owner.gravDir == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float spinRotation = RecoveryState ? 0f : spinVisualRotation;
            float chargeCompletion = MathHelper.Clamp(ChargeTimer / DesertEaglePlayer.SpinChargeMax, 0f, 1f);
            Color outlineColor = Color.Lerp(DesertEagleEffects.SilverAccent, Color.White, 0.55f) * (0.12f + 0.5f * chargeCompletion);
            float outlineDistance = 1f + 4f * chargeCompletion;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(
                    texture,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    null,
                    Color.Lerp(DesertEagleEffects.SilverDark, DesertEagleEffects.SilverAccent, 0.3f) * (0.07f * completion),
                    drawRotation + spinRotation * completion,
                    origin,
                    Projectile.scale * MathHelper.Lerp(0.92f, 1f, completion),
                    flipSprite,
                    0);
            }

            if (!RecoveryState && chargeCompletion > 0f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 outlineOffset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * outlineDistance;
                    Main.EntitySpriteDraw(
                        texture,
                        drawPosition + outlineOffset,
                        null,
                        outlineColor,
                        drawRotation + spinRotation,
                        origin,
                        Projectile.scale,
                        flipSprite,
                        0);
                }
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), drawRotation + spinRotation, origin, Projectile.scale, flipSprite, 0);
            return false;
        }

        public override void KillHoldoutLogic()
        {
        }
    }
}
