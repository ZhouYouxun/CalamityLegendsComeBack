using System;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill
{
    internal sealed class BFEXWeapon : ModProjectile, ILocalizedModType
    {
        private const int ChargeFrames = 60;
        private const int BarrageFrames = 180;
        private const float IdleHoldOffset = 22f;
        private const float ChargedHoldOffset = 14f;

        private int timer;
        private int fireSoundTimer;
        private bool leftHeldLastFrame;
        private float holdOffsetFromArm = IdleHoldOffset;
        private float extraFrontArmRotation;
        private float extraBackArmRotation;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/NewLegendBlossomFlux";

        private Player Owner => Main.player[Projectile.owner];
        private int State
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 GunTip => Projectile.Center + AimDirection * 44f;
        private Color MainColor => new Color(86, 255, 148);
        private Color AccentColor => new Color(204, 255, 220);

        public override void SetDefaults()
        {
            Projectile.width = 78;
            Projectile.height = 78;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(IEntitySource source)
        {
            CloseSelectionPanel();
            KillNormalHoldout();
            SoundEngine.PlaySound(SoundID.Item164 with { Volume = 0.8f, Pitch = -0.35f }, Projectile.Center);
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendBlossomFlux>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;

            UpdateHeldProjectileVariables(State != 2);
            ManipulatePlayerVariables();

            switch (State)
            {
                case 0:
                    ChargePhase();
                    break;

                case 1:
                    ReadyPhase();
                    break;

                default:
                    BarragePhase();
                    break;
            }
        }

        private void ChargePhase()
        {
            timer++;
            float chargeCompletion = Utils.GetLerpValue(0f, ChargeFrames, timer, true);
            holdOffsetFromArm = MathHelper.Lerp(IdleHoldOffset, ChargedHoldOffset, chargeCompletion);
            extraFrontArmRotation = -0.08f * chargeCompletion;
            extraBackArmRotation = 0.05f * chargeCompletion;

            SpawnAbsorbEffects(strong: true);

            if (timer >= ChargeFrames)
                EnterReadyPhase();
        }

        private void ReadyPhase()
        {
            timer++;
            holdOffsetFromArm = ChargedHoldOffset;
            extraFrontArmRotation = -0.08f;
            extraBackArmRotation = 0.05f;

            SpawnAbsorbEffects(strong: false);

            bool validLeftClick =
                Main.myPlayer == Projectile.owner &&
                Main.mouseLeft &&
                !Owner.mouseInterface &&
                !Main.blockMouse &&
                !Main.mapFullscreen;

            if (validLeftClick && !leftHeldLastFrame)
                EnterBarragePhase();

            leftHeldLastFrame = validLeftClick;
        }

        private void BarragePhase()
        {
            timer++;
            fireSoundTimer++;

            float barrageCompletion = Utils.GetLerpValue(0f, BarrageFrames, timer, true);
            holdOffsetFromArm = MathHelper.Lerp(ChargedHoldOffset - 2f, IdleHoldOffset - 1f, barrageCompletion);
            extraFrontArmRotation = -0.1f;
            extraBackArmRotation = 0.06f;

            SpawnAbsorbEffects(strong: false, subduedFactor: 0.35f);
            SpawnMuzzleEffects();

            if (Main.myPlayer == Projectile.owner && timer % 2 == 0)
                FireBarrageVolley();

            if (fireSoundTimer >= 4)
            {
                fireSoundTimer = 0;
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.58f, Pitch = 0.2f + Main.rand.NextFloat(-0.08f, 0.08f) }, GunTip);
            }

            if (timer >= BarrageFrames)
                Projectile.Kill();
        }

        private void EnterReadyPhase()
        {
            State = 1;
            timer = 0;
            Projectile.netUpdate = true;

            SpawnChargeBurst(1.1f);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.78f, Pitch = -0.08f }, GunTip);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.42f, Pitch = -0.2f }, GunTip);
        }

        private void EnterBarragePhase()
        {
            State = 2;
            timer = 0;
            fireSoundTimer = 0;
            leftHeldLastFrame = false;
            Projectile.velocity = AimDirection;
            Projectile.netUpdate = true;

            SpawnChargeBurst(1.35f);
            SoundEngine.PlaySound(SoundID.Item163 with { Volume = 0.95f, Pitch = -0.16f }, GunTip);
        }

        private void FireBarrageVolley()
        {
            Vector2 forward = AimDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            int shotDamage = (int)(Projectile.damage * 0.7f);

            for (int i = 0; i < 3; i++)
            {
                float offset = Main.rand.NextFloat(-24f, 24f);
                Vector2 spawnPosition = GunTip + right * offset + forward * Main.rand.NextFloat(-6f, 10f);
                Vector2 velocity = forward * Main.rand.NextFloat(18f, 24f);
                int delay = Main.rand.Next(16, 52);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<BFEXVernalShot>(),
                    shotDamage,
                    Projectile.knockBack * 0.8f,
                    Projectile.owner,
                    delay,
                    Main.rand.NextFloat(-1f, 1f));
            }
        }

        private void SpawnAbsorbEffects(bool strong, float subduedFactor = 1f)
        {
            if (Main.dedServ)
                return;

            float strength = (strong ? 1f : 0.42f) * subduedFactor;
            Vector2 forward = AimDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 center = GunTip;

            int ellipticalCount = strong ? 4 : 2;
            for (int i = 0; i < ellipticalCount; i++)
            {
                float ellipseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                float shortAxis = Main.rand.NextFloat(38f, 58f);
                float longAxis = Main.rand.NextFloat(118f, 156f);
                Vector2 spawnPosition =
                    center +
                    forward * ((float)Math.Sin(ellipseAngle) * shortAxis) +
                    right * ((float)Math.Cos(ellipseAngle) * longAxis) +
                    Main.rand.NextVector2Circular(6f, 6f);

                Vector2 toCenter = (center - spawnPosition).SafeNormalize(forward);
                Vector2 swirl = toCenter.RotatedBy(MathHelper.PiOver2 * (Main.rand.NextBool() ? 1f : -1f));
                Vector2 velocity =
                    toCenter * Main.rand.NextFloat(6f, 13f) * strength +
                    swirl * Main.rand.NextFloat(1.8f, 5.5f) * strength +
                    forward * Main.rand.NextFloat(0.2f, 1.6f);

                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    spawnPosition,
                    velocity,
                    Main.rand.NextFloat(0.3f, 0.65f) * (0.9f + strength * 0.65f),
                    Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat(0.15f, 0.5f)),
                    Main.rand.Next(16, 28)));

                if (strong && Main.myPlayer == Projectile.owner && Main.rand.NextBool(3))
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        spawnPosition,
                        velocity * 0.45f,
                        ModContent.ProjectileType<NewSHPS>(),
                        0,
                        0f,
                        Projectile.owner,
                        4f,
                        Projectile.whoAmI,
                        2f);
                }
            }

            int forwardCount = strong ? 8 : 4;
            for (int i = 0; i < forwardCount; i++)
            {
                Vector2 spawnPosition =
                    center -
                    forward * Main.rand.NextFloat(6f, 34f) +
                    right * Main.rand.NextFloat(-24f, 24f);

                Vector2 velocity = forward * Main.rand.NextFloat(10f, 27f) * MathHelper.Lerp(0.7f, 1f, strength);
                float scale = Main.rand.NextFloat(0.18f, 0.42f) * (0.85f + strength * 0.75f);
                int life = strong ? Main.rand.Next(12, 20) : Main.rand.Next(8, 14);
                Color color = Color.Lerp(MainColor, Color.White, Main.rand.NextFloat(0.18f, 0.52f));

                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    spawnPosition,
                    velocity,
                    scale,
                    color,
                    life,
                    1f,
                    1.55f));
            }

            Lighting.AddLight(center, MainColor.ToVector3() * (0.34f + 0.24f * strength));
        }

        private void SpawnChargeBurst(float scaleFactor)
        {
            if (Main.dedServ)
                return;

            Color flashColor = Color.Lerp(MainColor, Color.White, 0.28f);
            GeneralParticleHandler.SpawnParticle(new StrongBloom(GunTip, Vector2.Zero, flashColor, 0.85f * scaleFactor, 22));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(GunTip, Vector2.Zero, flashColor * 0.75f, Vector2.One, Main.rand.NextFloat(-0.2f, 0.2f), 0f, 0.24f * scaleFactor, 18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(GunTip, Vector2.Zero, MainColor * 0.8f, new Vector2(0.58f, 5.8f) * scaleFactor, Projectile.rotation, 0.2f, 0.035f, 24));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(GunTip, Vector2.Zero, MainColor * 0.7f, new Vector2(0.52f, 4.4f) * scaleFactor, Projectile.rotation + MathHelper.PiOver2, 0.16f, 0.03f, 22));
        }

        private void SpawnMuzzleEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = AimDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            if (timer % 2 == 0)
            {
                Dust dust = Dust.NewDustPerfect(
                    GunTip + right * Main.rand.NextFloat(-12f, 12f),
                    DustID.GemEmerald,
                    forward * Main.rand.NextFloat(3.5f, 8f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    150,
                    Color.Lerp(MainColor, Color.White, 0.2f),
                    Main.rand.NextFloat(1f, 1.45f));
                dust.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                GunTip + right * Main.rand.NextFloat(-6f, 6f),
                forward * Main.rand.NextFloat(3f, 6.5f),
                Main.rand.NextFloat(0.2f, 0.36f),
                Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat(0.2f, 0.6f)),
                Main.rand.Next(10, 16)));
        }

        private void UpdateHeldProjectileVariables(bool allowTurning)
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            if (Main.myPlayer == Projectile.owner && allowTurning)
            {
                Vector2 aimTarget = Owner.Calamity().mouseWorld;
                if (aimTarget == Vector2.Zero)
                    aimTarget = Main.MouseWorld;

                Vector2 aimDirection = aimTarget - armPosition;
                if (aimDirection == Vector2.Zero)
                    aimDirection = Vector2.UnitX * Owner.direction;

                Vector2 desiredVelocity = aimDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
                Vector2 oldVelocity = Projectile.velocity;
                Projectile.velocity = oldVelocity == Vector2.Zero ? desiredVelocity : Vector2.Lerp(oldVelocity, desiredVelocity, 0.38f);
                if (Vector2.DistanceSquared(oldVelocity, Projectile.velocity) > 0.0001f)
                    Projectile.netUpdate = true;
            }

            Projectile.Center = armPosition + AimDirection * holdOffsetFromArm;
            Projectile.rotation = AimDirection.ToRotation();
            Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction;
            Projectile.timeLeft = 2;
        }

        private void ManipulatePlayerVariables()
        {
            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemAnimation = 2;
            Owner.itemTime = 2;

            float armRotation = Projectile.rotation - MathHelper.PiOver2;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation + extraFrontArmRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRotation + extraBackArmRotation);
        }

        private void KillNormalHoldout()
        {
            int holdoutType = ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.active && projectile.owner == Projectile.owner && projectile.type == holdoutType)
                    projectile.Kill();
            }
        }

        private void CloseSelectionPanel()
        {
            int selectionPanelType = ModContent.ProjectileType<BFSelectionPanel>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Projectile.owner || projectile.type != selectionPanelType)
                    continue;

                projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D weaponTexture = TextureAssets.Projectile[Type].Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = weaponTexture.Size() * 0.5f;
            float rotation = Projectile.rotation;
            SpriteEffects effects = SpriteEffects.None;
            float outlinePulse = 0.78f + 0.22f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.8f + Projectile.identity * 0.43f);
            float outlineDistance = 1.6f + 1f * outlinePulse;
            Color outlineColor = Color.Lerp(MainColor, AccentColor, 0.34f) * (0.24f + 0.16f * outlinePulse);
            Color glowColor = Color.Lerp(MainColor, Color.White, 0.24f) * (0.1f + 0.08f * outlinePulse);

            if (Owner.gravDir == 1f)
            {
                if (Projectile.spriteDirection == -1)
                    effects = SpriteEffects.FlipVertically;
            }
            else
            {
                origin.Y = weaponTexture.Height - origin.Y;
                if (Projectile.spriteDirection == 1)
                    effects = SpriteEffects.FlipVertically;
            }

            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f;
                Vector2 offset = angle.ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(weaponTexture, drawPosition + offset, null, outlineColor, rotation, origin, Projectile.scale, effects, 0);
            }

            Main.EntitySpriteDraw(weaponTexture, drawPosition, null, glowColor, rotation, origin, Projectile.scale * (1.015f + 0.025f * outlinePulse), effects, 0);
            Main.EntitySpriteDraw(weaponTexture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, effects, 0);

            Vector2 gunTipDrawPosition = GunTip - Main.screenPosition;
            float bloomScale = State == 0 ? 0.42f + Utils.GetLerpValue(0f, ChargeFrames, timer, true) * 0.34f : (State == 1 ? 0.78f : 0.96f);
            Main.EntitySpriteDraw(bloomTexture, gunTipDrawPosition, null, Color.Lerp(MainColor, Color.White, 0.25f) * 0.7f, 0f, bloomTexture.Size() * 0.5f, bloomScale, SpriteEffects.None, 0);

            if (State >= 1)
            {
                Vector2 forward = AimDirection;
                Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
                float lineLength = State == 1 ? 520f : 620f;
                float spread = State == 1 ? 16f : 10f;
                Color lineColor = Color.Lerp(MainColor, AccentColor, 0.35f) * (State == 1 ? 0.58f : 0.4f);

                DrawLine(pixel, gunTipDrawPosition + right * spread, gunTipDrawPosition + right * spread + forward * lineLength, lineColor, State == 1 ? 2.4f : 1.5f);
                DrawLine(pixel, gunTipDrawPosition - right * spread, gunTipDrawPosition - right * spread + forward * lineLength, lineColor, State == 1 ? 2.4f : 1.5f);
            }

            return false;
        }

        private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 direction = end - start;
            float length = direction.Length();
            if (length <= 0.01f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                start,
                null,
                color,
                direction.ToRotation(),
                Vector2.Zero,
                new Vector2(length, width),
                SpriteEffects.None,
                0f);
        }
    }
}
