using CalamityLegendsComeBack.Weapons.Vesuvius.EXSkill;
using CalamityLegendsComeBack.Weapons.Vesuvius.RightClick.Javelin;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.RightClick
{
    public class VesuviusRightJavelinHoldout : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuvius";

        private const int PullbackLength = 30;
        private const int MaxChargeLength = 78;
        private const int ReadyFlashLength = 20;
        private const float MinThrowSpeed = 50f;
        private const float MaxThrowSpeed = 72f;
        private const float VisualRotationOffset = MathHelper.PiOver4;

        private bool releaseRequested;
        private bool readySoundPlayed;
        private bool fired;
        private int chargeTimer;

        private Player Owner => Main.player[Projectile.owner];
        private int Stage => (int)MathHelper.Clamp(Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0], 1f, 5f);
        private Vector2 Direction => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private float PullbackCompletion => Utils.GetLerpValue(0f, PullbackLength, chargeTimer, true);
        private float HeatCompletion => Utils.GetLerpValue(PullbackLength, MaxChargeLength, chargeTimer, true);
        private Vector2 JavelinTip => Projectile.Center + Direction * 48f * Projectile.scale;

        public override void SetDefaults()
        {
            Projectile.width = 62;
            Projectile.height = 62;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            bool stillHoldingVesuvius = Owner.HeldItem.ModItem is NewVesuvius;
            UpdateAimAndPosition();
            ManipulateOwner();

            if (!stillHoldingVesuvius)
            {
                releaseRequested = true;
                chargeTimer = Math.Max(chargeTimer, PullbackLength);
            }

            if (Main.myPlayer == Projectile.owner &&
                stillHoldingVesuvius &&
                (!Owner.Calamity().mouseRight || !CanUseWorldRightClick(Owner)))
            {
                releaseRequested = true;
            }

            if (!releaseRequested || chargeTimer < PullbackLength)
            {
                chargeTimer = Math.Min(MaxChargeLength, chargeTimer + 1);
                Projectile.timeLeft = 2;
                ChargingEffects();
                return;
            }

            FireJavelin();
            Projectile.Kill();
        }

        private void UpdateAimAndPosition()
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 mouse = Owner.Calamity().mouseWorld;
                Vector2 aimDirection = (mouse - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
                float aimInterpolant = Utils.GetLerpValue(5f, 25f, Owner.Distance(mouse), true);
                Projectile.velocity = Vector2.Lerp(Direction, aimDirection, aimInterpolant).SafeNormalize(Vector2.UnitX * Owner.direction);
                Projectile.netUpdate = true;
            }

            Projectile.direction = Direction.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction;
            Projectile.rotation = GetVisualRotation(Direction, Projectile.direction);

            float pullback = PullbackCompletion;
            float forwardReach = MathHelper.Lerp(50f, 20f, pullback);
            float armAngle = GetFrontArmRotation();
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Vector2 armSeat = (armAngle + MathHelper.PiOver2).ToRotationVector2() * Projectile.scale * 15f;
            Vector2 heatRumble = !releaseRequested && pullback >= 1f
                ? Main.rand.NextVector2Circular(HeatCompletion * 1.6f, HeatCompletion * 1.6f)
                : Vector2.Zero;

            Projectile.Center = armPosition + armSeat + Direction * forwardReach + heatRumble;
        }

        private void ManipulateOwner()
        {
            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Direction * Projectile.direction).ToRotation();
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, GetFrontArmRotation());
        }

        private float GetFrontArmRotation()
        {
            float rotation = Direction.ToRotation() - MathHelper.PiOver2;
            rotation -= PullbackCompletion * Owner.direction * 0.74f;
            return rotation;
        }

        private static bool CanUseWorldRightClick(Player player)
        {
            return !player.noItems &&
                !player.CCed &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !player.mouseInterface &&
                (!Main.playerInventory || Main.HoverItem.IsAir);
        }

        private void ChargingEffects()
        {
            Lighting.AddLight(Projectile.Center, 0.78f + HeatCompletion * 0.55f, 0.22f + HeatCompletion * 0.18f, 0.04f);

            if (Main.dedServ)
                return;

            Color stageColor = VesuviusProgression.GetStageColor(Stage);
            float pullback = PullbackCompletion;
            float heat = HeatCompletion;

            if (chargeTimer == 1)
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.46f, Pitch = -0.35f }, Projectile.Center);

            if (chargeTimer >= PullbackLength && !readySoundPlayed)
            {
                readySoundPlayed = true;
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.18f + Stage * 0.03f }, JavelinTip);
                GeneralParticleHandler.SpawnParticle(new StrongBloom(JavelinTip, Vector2.Zero, Color.Lerp(stageColor, Color.White, 0.28f), 1f + Stage * 0.18f, 20));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(JavelinTip, Direction * 0.2f, stageColor, new Vector2(0.75f, 1.25f), Direction.ToRotation() - MathHelper.PiOver2, 0.08f, 0.8f, 18));
            }

            if (Main.rand.NextFloat() < 0.42f + heat * 0.28f)
                VesuviusVolcanicVisuals.SpawnTravelMix(JavelinTip + Main.rand.NextVector2Circular(8f, 8f), -Direction * Main.rand.NextFloat(0.4f, 1.2f), 0.48f + heat * 0.7f, Stage >= 4);

            if (pullback >= 1f && Main.rand.NextBool(3))
                VesuviusProjectileVisuals.SpawnMoltenBloom(JavelinTip + Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextFloat(20f, 38f) * (0.65f + heat * 0.5f), 0.56f);

            if (pullback >= 1f && Main.rand.NextBool(4))
            {
                Vector2 sparkVelocity = Direction.RotatedByRandom(0.42f) * Main.rand.NextFloat(4f, 10f + Stage);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    JavelinTip,
                    sparkVelocity,
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.24f, 0.56f),
                    Main.rand.NextBool(4) ? Color.White : stageColor));
            }

            if (pullback >= 1f)
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, (0.12f + heat * 0.28f) * Utils.GetLerpValue(1500f, 220f, Main.LocalPlayer.Distance(Projectile.Center), true));
        }

        private void FireJavelin()
        {
            if (fired)
                return;

            fired = true;
            if (Main.myPlayer == Projectile.owner)
            {
                float throwSpeed = MathHelper.Lerp(MinThrowSpeed, MaxThrowSpeed, 0.45f + HeatCompletion * 0.55f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    JavelinTip,
                    Direction * throwSpeed,
                    ModContent.ProjectileType<VesuviusFaultJavelin>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    0f,
                    0f,
                    Stage);

                if (VesuviusRightMeteorPlayer.TryReleaseFullVolley(Owner, Direction, Projectile.damage, Projectile.knockBack))
                    SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.78f, Pitch = -0.18f }, Owner.Center);

                Owner.GetModPlayer<VesuviusEXPlayer>().GainEX(1);
            }

            if (!Main.dedServ)
            {
                Color stageColor = VesuviusProgression.GetStageColor(Stage);
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.78f, Pitch = -0.22f }, JavelinTip);
                SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.62f, Pitch = -0.32f + Stage * 0.04f }, JavelinTip);
                GeneralParticleHandler.SpawnParticle(new StrongBloom(JavelinTip, Vector2.Zero, Color.Lerp(stageColor, Color.White, 0.25f), 1.25f + HeatCompletion * 0.75f, 18));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(JavelinTip, Direction, stageColor, new Vector2(0.8f, 1.8f), Direction.ToRotation() - MathHelper.PiOver2, 0.08f, 1.2f + HeatCompletion * 0.5f, 18));

                for (int i = 0; i < 22 + Stage * 3; i++)
                {
                    Vector2 velocity = Direction.RotatedByRandom(0.48f) * Main.rand.NextFloat(4f, 18f + Stage * 2f);
                    Dust dust = Dust.NewDustPerfect(JavelinTip, Main.rand.NextBool(3) ? DustID.Torch : DustID.Smoke, velocity, 80, Main.rand.NextBool(4) ? Color.White : stageColor, Main.rand.NextFloat(0.8f, 1.45f));
                    dust.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuviusGlow").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D lightFlash = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/fx_LightFlash2").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 tipPosition = JavelinTip - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            bool facingLeft = Projectile.spriteDirection == -1;
            SpriteEffects flip = facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float drawRotation = Projectile.rotation + (facingLeft ? MathHelper.PiOver2 : 0f);

            Color stageColor = VesuviusProgression.GetStageColor(Stage);
            bool fullVolleyReady = Owner.GetModPlayer<VesuviusRightMeteorPlayer>().HasFullReadyVolley();
            float volleyBoost = fullVolleyReady ? 1f : 0f;
            Color additiveColor = Color.Lerp(stageColor, VesuviusProjectileVisuals.LavaGold, volleyBoost * 0.42f);
            float pullback = PullbackCompletion;
            float heat = HeatCompletion;
            float readyFlash = Utils.GetLerpValue(PullbackLength + ReadyFlashLength, PullbackLength, chargeTimer, true);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * (8f + Stage));

            Color additive = VesuviusProjectileVisuals.AdditiveColor(additiveColor);
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // A single body-wide heat glow instead of the old 8-18 offset copies of the whole
            // staff sprite, which smeared the weapon rather than outlining it. The charge read
            // now comes from the tip bloom and ring growing, not from the staff going blurry.
            Main.EntitySpriteDraw(bloom, drawPosition, null, additive * (0.2f + pullback * 0.18f + heat * 0.22f),
                0f, bloom.Size() * 0.5f, 0.7f + pullback * 0.2f + heat * 0.2f, SpriteEffects.None);

            Main.EntitySpriteDraw(bloom, tipPosition, null, additive * (0.25f + pullback * 0.3f + heat * 0.35f + volleyBoost * 0.22f),
                Projectile.rotation, bloom.Size() * 0.5f, 0.38f + pullback * 0.28f + heat * 0.22f + volleyBoost * 0.18f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloomRing, tipPosition, null, additive * (0.16f + heat * 0.34f + volleyBoost * 0.2f),
                -Main.GlobalTimeWrappedHourly * 2.2f, bloomRing.Size() * 0.5f, 0.15f + pullback * 0.25f + heat * 0.22f + volleyBoost * 0.16f, SpriteEffects.None);
            Main.EntitySpriteDraw(lightFlash, tipPosition, null, VesuviusProjectileVisuals.AdditiveColor(Color.White) * (readyFlash * 0.35f + heat * 0.22f),
                Projectile.rotation, lightFlash.Size() * 0.5f, new Vector2(0.34f + heat * 0.18f, 0.1f + pulse * 0.04f), SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, drawRotation, origin, Projectile.scale, flip);
            Main.EntitySpriteDraw(glow, drawPosition, null, Color.White with { A = 0 } * (0.5f + pullback * 0.3f + heat * 0.2f), drawRotation, glow.Size() * 0.5f, Projectile.scale, flip);
            return false;
        }

        private static float GetVisualRotation(Vector2 velocity, int fallbackDirection)
        {
            return velocity.SafeNormalize(Vector2.UnitX * fallbackDirection).ToRotation() + VisualRotationOffset;
        }
    }
}
