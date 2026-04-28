using CalamityLegendsComeBack.Weapons.SHPC.RightClick;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClickMortar
{
    internal sealed class RightClickMortar_HoldOut : RightClickHoldoutBase, ILocalizedModType
    {
        private const int StartupFrames = 16;
        private const int FireInterval = 60;
        private const int ManaPerShot = 24;
        private const float MortarSpeed = 12f;

        private int chargeTimer;
        private int startupTimer = StartupFrames;
        private int muzzleFlashTimer;
        private int chargeSparkTimer;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Items/Weapons/Magic/SHPC";
        public override int AssociatedItemID => ModContent.ItemType<NewLegendSHPC>();
        public override bool UseBaseDraw => true;

        public override Vector2 GunTipPosition =>
            Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * 58f;

        public override float MaxOffsetLengthFromArm => 45f;
        public override float RecoilResolveSpeed => 0.09f;
        public override float OffsetXUpwards => -18f;
        public override float BaseOffsetY => -5f;
        public override float OffsetYUpwards => 22f;

        private float ChargeCompletion => MathHelper.Clamp(chargeTimer / (float)FireInterval, 0f, 1f);

        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            FrontArmStretch = Player.CompositeArmStretchAmount.Quarter;
            BackArmStretch = Player.CompositeArmStretchAmount.Full;
            ExtraBackArmRotation = MathHelper.ToRadians(12f);
            SoundEngine.PlaySound(SoundID.Item149 with { Volume = 0.55f, Pitch = -0.35f }, Projectile.Center);
        }

        public override void ManageHoldout()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Vector2 mouseWorld = GetMouseWorld();
            Vector2 skyAnchor = new(MathHelper.Lerp(mouseWorld.X, Owner.Center.X, 0.55f), Owner.Center.Y - 500f * Owner.gravDir);
            Vector2 ownerToSky = skyAnchor - Owner.Center;
            if (ownerToSky == Vector2.Zero)
                ownerToSky = -Vector2.UnitY * Owner.gravDir;

            float holdoutDirection = Projectile.velocity.ToRotation();
            float proximityLookingUpwards = Vector2.Dot(ownerToSky.SafeNormalize(Vector2.Zero), -Vector2.UnitY * Owner.gravDir);
            int direction = Math.Sign(ownerToSky.X);
            if (direction == 0)
                direction = Owner.direction;

            Vector2 lengthOffset = Projectile.rotation.ToRotationVector2() * OffsetLengthFromArm;
            Vector2 armOffset = new(
                Utils.Remap(MathF.Abs(proximityLookingUpwards), 0f, 1f, 0f, proximityLookingUpwards > 0f ? OffsetXUpwards : OffsetXDownwards) * direction,
                BaseOffsetY * Owner.gravDir +
                Utils.Remap(MathF.Abs(proximityLookingUpwards), 0f, 1f, 0f, proximityLookingUpwards > 0f ? OffsetYUpwards : OffsetYDownwards) * Owner.gravDir);

            Projectile.Center = armPosition + lengthOffset + armOffset;
            Projectile.velocity = holdoutDirection.AngleTowards(ownerToSky.ToRotation(), WeaponTurnSpeed).ToRotationVector2();
            Projectile.rotation = holdoutDirection;
            Projectile.spriteDirection = direction;

            Owner.ChangeDir(direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir +
                (Owner.gravDir == -1 ? MathHelper.Pi : 0f);

            Owner.SetCompositeArmFront(true, FrontArmStretch, armRotation + ExtraFrontArmRotation * direction);
            Owner.SetCompositeArmBack(true, BackArmStretch, armRotation + ExtraBackArmRotation * direction);

            Projectile.timeLeft = 2;

            if (OffsetLengthFromArm != MaxOffsetLengthFromArm)
                OffsetLengthFromArm = MathHelper.Lerp(OffsetLengthFromArm, MaxOffsetLengthFromArm, RecoilResolveSpeed);

            if (Projectile.owner == Main.myPlayer)
                Projectile.netUpdate = true;
        }

        public override void HoldoutAI()
        {
            if (startupTimer > 0)
            {
                startupTimer--;
                SpawnChargeEffects(true);
                return;
            }

            if (muzzleFlashTimer > 0)
                muzzleFlashTimer--;

            chargeTimer++;
            SpawnChargeEffects(false);

            if (chargeTimer < FireInterval)
                return;

            if (Owner.CheckMana(Owner.HeldItem, ManaPerShot, true, false))
            {
                FireMortarShell();
                chargeTimer = 0;
                return;
            }

            chargeTimer = FireInterval - 10;
            SpawnDryFireSmoke();
        }

        private void FireMortarShell()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(-Vector2.UnitY * Owner.gravDir);
            Vector2 mouseWorld = GetMouseWorld();
            Vector2 spawnPosition = GunTipPosition + direction * 6f;
            int damage = Projectile.damage;

            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    direction.RotatedByRandom(MathHelper.ToRadians(3f)) * MortarSpeed,
                    ModContent.ProjectileType<RightClickMortar_Proj>(),
                    damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    mouseWorld.X,
                    mouseWorld.Y);
            }

            SoundEngine.PlaySound(NewLegendSHPC.MortarSentryShot, GunTipPosition);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 5f);
            OffsetLengthFromArm -= 23f;
            muzzleFlashTimer = 16;
            SpawnMuzzleBurst(direction);
        }

        private void SpawnChargeEffects(bool startup)
        {
            if (Main.dedServ)
                return;

            chargeSparkTimer++;
            Vector2 direction = Projectile.velocity.SafeNormalize(-Vector2.UnitY * Owner.gravDir);
            Vector2 right = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 core = GunTipPosition + direction * MathHelper.Lerp(-3f, 8f, ChargeCompletion);

            Color deepBlue = new(55, 170, 255);
            Color cyan = new(120, 235, 255);
            Color white = Color.Lerp(cyan, Color.White, 0.45f);
            Lighting.AddLight(core, cyan.ToVector3() * (0.16f + ChargeCompletion * 0.36f));

            if (startup || chargeSparkTimer % 5 == 0)
            {
                Vector2 dustVelocity =
                    direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.8f, 2.4f) +
                    right * Main.rand.NextFloat(-0.8f, 0.8f);

                Dust dust = Dust.NewDustPerfect(core + Main.rand.NextVector2Circular(4f, 4f), DustID.RainbowMk2);
                dust.velocity = dustVelocity;
                dust.color = Color.Lerp(deepBlue, white, Main.rand.NextFloat(0.2f, 0.85f));
                dust.scale = Main.rand.NextFloat(0.75f, 1.12f);
                dust.noGravity = true;
            }

            if (chargeSparkTimer % 12 == 0)
            {
                Particle spark = new CustomSpark(
                    core + right * Main.rand.NextFloat(-8f, 8f),
                    direction.RotatedByRandom(0.22f) * Main.rand.NextFloat(3f, 6f),
                    "CalamityMod/Particles/BloomLineSoftEdge",
                    false,
                    12,
                    Main.rand.NextFloat(0.026f, 0.04f),
                    Color.Lerp(deepBlue, white, Main.rand.NextFloat(0.25f, 0.8f)),
                    new Vector2(0.9f, 0.65f),
                    shrinkSpeed: 0.8f);

                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private void SpawnMuzzleBurst(Vector2 direction)
        {
            if (Main.dedServ)
                return;

            Vector2 muzzle = GunTipPosition + direction * 8f;
            Vector2 right = direction.RotatedBy(MathHelper.PiOver2);
            Color techBlue = new(70, 190, 255);
            Color electricWhite = new(215, 250, 255);

            for (int i = 0; i < 8; i++)
            {
                float offset = MathHelper.Lerp(-0.34f, 0.34f, i / 7f);
                Vector2 lane = direction.RotatedBy(offset);

                Particle line = new CustomSpark(
                    muzzle + right * Main.rand.NextFloat(-5f, 5f),
                    lane * Main.rand.NextFloat(11f, 18f),
                    "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade",
                    false,
                    11,
                    Main.rand.NextFloat(0.042f, 0.065f),
                    Color.Lerp(techBlue, electricWhite, Main.rand.NextFloat(0.35f, 0.9f)),
                    new Vector2(0.8f, 1.65f),
                    glowCenter: true,
                    shrinkSpeed: 0.82f,
                    glowCenterScale: 0.8f,
                    glowOpacity: 0.7f);

                GeneralParticleHandler.SpawnParticle(line);
            }

            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(muzzle + Main.rand.NextVector2Circular(5f, 5f), DustID.RainbowMk2);
                dust.velocity =
                    direction.RotatedByRandom(0.52f) * Main.rand.NextFloat(4f, 10f) +
                    right * Main.rand.NextFloat(-1.2f, 1.2f);
                dust.color = Color.Lerp(techBlue, electricWhite, Main.rand.NextFloat(0.2f, 0.85f));
                dust.scale = Main.rand.NextFloat(0.95f, 1.4f);
                dust.noGravity = true;
            }

            for (int i = 0; i < 3; i++)
            {
                Particle ring = new DirectionalPulseRing(
                    muzzle,
                    direction * Main.rand.NextFloat(0.8f, 2.2f),
                    Color.Lerp(techBlue, electricWhite, Main.rand.NextFloat(0.35f, 0.8f)) * 0.85f,
                    new Vector2(1f, 1f),
                    direction.ToRotation(),
                    0.08f,
                    Main.rand.NextFloat(0.21f, 0.34f),
                    18);

                GeneralParticleHandler.SpawnParticle(ring);
            }
        }

        private void SpawnDryFireSmoke()
        {
            if (Main.dedServ || chargeSparkTimer % 8 != 0)
                return;

            Vector2 smokeVelocity = -Vector2.UnitY.RotatedByRandom(0.3f) * Main.rand.NextFloat(1.2f, 3.2f);
            Particle smoke = new HeavySmokeParticle(
                GunTipPosition,
                smokeVelocity,
                new Color(120, 170, 190),
                36,
                Main.rand.NextFloat(0.28f, 0.44f),
                0.45f,
                Main.rand.NextFloat(-0.1f, 0.1f),
                Main.rand.NextBool());

            GeneralParticleHandler.SpawnParticle(smoke);
        }

        private Vector2 GetMouseWorld()
        {
            Vector2 mouseWorld = Owner.Calamity().mouseWorld;
            if (mouseWorld == Vector2.Zero)
                mouseWorld = Main.MouseWorld;

            return mouseWorld;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = SpriteEffects.None;

            if (Owner.gravDir == 1f)
            {
                if (Projectile.spriteDirection == -1)
                    effects = SpriteEffects.FlipVertically;
            }
            else
            {
                origin.Y = texture.Height - origin.Y;
                if (Projectile.spriteDirection == 1)
                    effects = SpriteEffects.FlipVertically;
            }

            float chargePulse = 0.45f + 0.55f * ChargeCompletion;
            float flashPulse = muzzleFlashTimer / 16f;
            Color outlineColor = (Color.Lerp(new Color(92, 214, 255), Color.White, 0.62f) with { A = 0 }) * (0.72f + chargePulse * 0.38f + flashPulse * 0.85f);
            Color innerOutlineColor = (Color.Lerp(new Color(165, 242, 255), Color.White, 0.72f) with { A = 0 }) * (0.5f + chargePulse * 0.26f + flashPulse * 0.52f);
            float outlineDistance = 2.2f + ChargeCompletion * 2.8f + flashPulse * 4.2f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            if (flashPulse > 0f)
            {
                HoldoutOutlineHelper.DrawStarmadaRainbowOutline(
                    texture,
                    drawPosition,
                    Projectile.rotation,
                    origin,
                    Vector2.One * Projectile.scale * (1f + flashPulse * 0.05f),
                    effects,
                    3.2f + flashPulse * 7.2f,
                    flashPulse * 0.95f,
                    Main.GlobalTimeWrappedHourly + Projectile.identity * 0.19f,
                    22,
                    manageBlendState: false);
            }

            for (int i = 0; i < 14; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 14f).ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(texture, drawPosition + offset, null, outlineColor, Projectile.rotation, origin, Projectile.scale, effects, 0);
            }

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * (1.1f + flashPulse * 1.5f);
                Main.EntitySpriteDraw(texture, drawPosition + offset, null, innerOutlineColor, Projectile.rotation, origin, Projectile.scale * (1.02f + flashPulse * 0.04f), effects, 0);
            }

            DrawMuzzleGlow(flashPulse);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects, 0);
            return false;
        }

        private void DrawMuzzleGlow(float flashPulse)
        {
            if (Main.dedServ)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(-Vector2.UnitY * Owner.gravDir);
            Vector2 muzzle = GunTipPosition + direction * 8f - Main.screenPosition;
            Color cyan = new(90, 210, 255, 0);
            Color white = new(230, 255, 255, 0);
            float charge = ChargeCompletion;
            float time = Main.GlobalTimeWrappedHourly;

            Main.EntitySpriteDraw(
                bloom,
                muzzle,
                null,
                Color.Lerp(cyan, white, charge) * (0.28f + charge * 0.34f + flashPulse * 0.55f),
                0f,
                bloom.Size() * 0.5f,
                new Vector2(0.42f + charge * 0.35f + flashPulse * 0.42f, 0.22f + charge * 0.18f),
                SpriteEffects.None,
                0);

            for (int i = 0; i < 4; i++)
            {
                float rotation = direction.ToRotation() + MathHelper.PiOver4 * i + time * (1.4f + i * 0.16f);
                Main.EntitySpriteDraw(
                    star,
                    muzzle,
                    null,
                    Color.Lerp(cyan, white, 0.55f) * (0.22f + charge * 0.28f + flashPulse * 0.65f),
                    rotation,
                    star.Size() * 0.5f,
                    new Vector2(0.3f + flashPulse * 0.35f, 1.3f + charge * 0.9f + flashPulse * 1.5f),
                    SpriteEffects.None,
                    0);
            }
        }
    }
}
