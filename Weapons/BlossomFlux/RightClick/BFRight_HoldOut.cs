using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using CalamityLegendsComeBack.Weapons.BlossomFlux.TurretMode;
using CalamityLegendsComeBack.Weapons.SHPC.RightClick;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    internal class BFRight_HoldOut : RightClickHoldoutBase, ILocalizedModType
    {
        private const int ReloadFrames = 18;
        private const int MaxChargeFrames = 60;
        private const float ReadyPulseScale = 0.45f;
        private const float RightClickBaseDamageMultiplier = 3f;

        private int reloadTimer;
        private int chargeTimer;
        private int chargeFxTimer;
        private bool readyBurstPlayed;
        private bool releasedShot;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/NewLegendBlossomFlux";
        public override int AssociatedItemID => ModContent.ItemType<NewLegendBlossomFlux>();

        public override float MaxOffsetLengthFromArm => 22f;
        public override float WeaponTurnSpeed => 0.16f;
        public override float RecoilResolveSpeed => 0.22f;

        public override Vector2 GunTipPosition =>
            Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * 42f;

        private Player LocalOwner => Main.player[Projectile.owner];
        private BlossomFluxChloroplastPresetType CurrentPreset => LocalOwner.GetModPlayer<BFRightUIPlayer>().CurrentPreset;
        private float ChargeCompletion => MathHelper.Clamp(chargeTimer / (float)MaxChargeFrames, 0f, 1f);
        private bool ChargeReady => chargeTimer >= MaxChargeFrames && readyBurstPlayed;
        private bool TurretModeActive => LocalOwner.GetModPlayer<BFTurretModePlayer>().TurretModeActive;

        private Color PresetColor => CurrentPreset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => new Color(132, 255, 132),
            BlossomFluxChloroplastPresetType.Chlo_BRecov => new Color(110, 255, 186),
            BlossomFluxChloroplastPresetType.Chlo_CDetec => new Color(130, 220, 255),
            BlossomFluxChloroplastPresetType.Chlo_DBomb => new Color(255, 186, 110),
            BlossomFluxChloroplastPresetType.Chlo_EPlague => new Color(174, 228, 96),
            _ => Color.LightGreen
        };

        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            reloadTimer = ReloadFrames;
            chargeTimer = 0;
            chargeFxTimer = 0;
            readyBurstPlayed = false;
            releasedShot = false;

            SoundEngine.PlaySound(SoundID.Item149 with { Volume = 0.55f, Pitch = -0.2f }, Projectile.Center);
        }

        public override void KillHoldoutLogic()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != AssociatedItemID)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.owner != Main.myPlayer)
                return;

            bool stillHoldingRight =
                Owner.Calamity().mouseRight &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !(Main.playerInventory && Main.HoverItem.type == AssociatedItemID);

            if (stillHoldingRight)
                return;

            // 只有完全蓄满、并且已经给出就绪音效和特效提示后，松开右键才会真正发射。
            if (ChargeReady)
                HandleRelease();

            Projectile.Kill();
        }

        public override void HoldoutAI()
        {
            Lighting.AddLight(GunTipPosition, PresetColor.ToVector3() * (0.16f + ChargeCompletion * 0.25f));
            chargeFxTimer++;

            if (reloadTimer > 0)
            {
                UpdateReloadAnimation();
                return;
            }

            if (chargeTimer < MaxChargeFrames)
            {
                chargeTimer++;
                UpdateChargingAnimation();
            }
            else
            {
                UpdateChargedAnimation();
            }
        }

        private void UpdateReloadAnimation()
        {
            float reloadProgress = 1f - reloadTimer / (float)ReloadFrames;
            ExtraFrontArmRotation = -0.05f * (1f - reloadProgress);
            ExtraBackArmRotation = 0.04f * (1f - reloadProgress);
            OffsetLengthFromArm = MathHelper.Lerp(MaxOffsetLengthFromArm - 10f, MaxOffsetLengthFromArm, reloadProgress);

            if (chargeFxTimer % 4 == 0)
                SpawnReloadDust();

            if (reloadTimer == 1)
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.45f, Pitch = 0.1f }, GunTipPosition);

            reloadTimer--;
        }

        private void UpdateChargingAnimation()
        {
            OffsetLengthFromArm = MathHelper.Lerp(MaxOffsetLengthFromArm - 2f, MaxOffsetLengthFromArm - 8f, ChargeCompletion);
            ExtraFrontArmRotation = -0.08f * ChargeCompletion;
            ExtraBackArmRotation = 0.05f * ChargeCompletion;

            SpawnChargingDust();

            if (chargeTimer % 10 == 0)
                SpawnChargeCircle();

            if (chargeTimer >= MaxChargeFrames && !readyBurstPlayed)
                PlayChargeReadyBurst();
        }

        private void UpdateChargedAnimation()
        {
            OffsetLengthFromArm = MaxOffsetLengthFromArm - 8f;
            ExtraFrontArmRotation = -0.08f;
            ExtraBackArmRotation = 0.05f;

            if (!readyBurstPlayed)
                PlayChargeReadyBurst();

            if (chargeFxTimer % 3 == 0)
                SpawnReadyIdleDust();
        }

        private void HandleRelease()
        {
            if (releasedShot || !ChargeReady)
                return;

            releasedShot = true;
            ExtraFrontArmRotation = 0f;
            ExtraBackArmRotation = 0f;

            SpawnReleasePulse();
            ReleaseChargedShot(Owner, CurrentPreset, ChargeCompletion);
        }

        private void ReleaseChargedShot(Player player, BlossomFluxChloroplastPresetType preset, float chargeCompletion)
        {
            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    ReleaseBreakthroughShot(player, chargeCompletion);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    ReleaseRecoveryShot(player, chargeCompletion);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    ReleaseReconnaissanceShot(player, chargeCompletion);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    ReleaseBombardmentShot(player, chargeCompletion);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                    ReleaseDisseminationShot(player, chargeCompletion);
                    break;
            }
        }

        private void ReleaseBreakthroughShot(Player player, float chargeCompletion)
        {
            FireSpecialArrow(player, chargeCompletion, ModContent.ProjectileType<BFArrow_ABreak>(), 18f, 1.12f);
        }

        private void ReleaseRecoveryShot(Player player, float chargeCompletion)
        {
            FireSpecialArrow(player, chargeCompletion, ModContent.ProjectileType<BFArrow_BRecov>(), 16.25f, 0.94f);
        }

        private void ReleaseReconnaissanceShot(Player player, float chargeCompletion)
        {
            FireSpecialArrow(player, chargeCompletion, ModContent.ProjectileType<BFArrow_CDetec>(), 18.75f, 0.92f);
        }

        private void ReleaseBombardmentShot(Player player, float chargeCompletion)
        {
            FireSpecialArrow(player, chargeCompletion, ModContent.ProjectileType<BFArrow_DBomb>(), 17f, 0.88f);
        }

        private void ReleaseDisseminationShot(Player player, float chargeCompletion)
        {
            FireSpecialArrow(player, chargeCompletion, ModContent.ProjectileType<BFArrow_EPlague>(), 15.5f, 0.98f);
        }

        private void SpawnReloadDust()
        {
            Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 side = backward.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + side * Main.rand.NextFloat(-7f, 7f),
                    DustID.GemEmerald,
                    backward.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.8f, 1.6f),
                    120,
                    Color.Lerp(PresetColor, Color.White, 0.25f),
                    Main.rand.NextFloat(0.85f, 1.15f));
                dust.noGravity = true;
            }
        }

        private void SpawnChargingDust()
        {
            if (chargeFxTimer % 2 != 0)
                return;

            Vector2 center = GunTipPosition;
            Vector2 inwardPosition = center + Main.rand.NextVector2Circular(16f, 16f);
            Vector2 inwardVelocity = (center - inwardPosition).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.9f, 2.1f);

            Dust dust = Dust.NewDustPerfect(
                inwardPosition,
                DustID.GemEmerald,
                inwardVelocity,
                100,
                Color.Lerp(PresetColor, Color.White, 0.2f + 0.3f * ChargeCompletion),
                Main.rand.NextFloat(0.8f, 1.25f));
            dust.noGravity = true;

            if (Main.rand.NextBool(3))
            {
                Dust glowDust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.TerraBlade,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    100,
                    PresetColor,
                    Main.rand.NextFloat(0.9f, 1.35f));
                glowDust.noGravity = true;
            }
        }

        private void SpawnChargeCircle()
        {
            Vector2 center = GunTipPosition;
            int points = 10;
            float radius = MathHelper.Lerp(14f, 26f, ChargeCompletion);

            for (int i = 0; i < points; i++)
            {
                float angle = MathHelper.TwoPi * i / points + Main.GlobalTimeWrappedHourly * 2.4f;
                Vector2 offset = angle.ToRotationVector2() * radius;

                Dust dust = Dust.NewDustPerfect(
                    center + offset,
                    DustID.GemEmerald,
                    -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1.1f, 2.4f),
                    100,
                    Color.Lerp(PresetColor, Color.White, 0.35f),
                    Main.rand.NextFloat(0.85f, 1.2f));
                dust.noGravity = true;
            }
        }

        private void PlayChargeReadyBurst()
        {
            readyBurstPlayed = true;

            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.25f }, GunTipPosition);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.4f, Pitch = -0.15f }, GunTipPosition);

            Vector2 center = GunTipPosition;
            for (int i = 0; i < 20; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(1.5f, 4.2f);
                Dust dust = Dust.NewDustPerfect(
                    center,
                    DustID.TerraBlade,
                    velocity,
                    100,
                    Color.Lerp(PresetColor, Color.White, 0.5f),
                    Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
            }
        }

        private void SpawnReadyIdleDust()
        {
            Vector2 center = GunTipPosition;
            Vector2 driftVelocity = -Vector2.UnitY.RotatedByRandom(0.28f) * Main.rand.NextFloat(0.8f, 1.7f);

            Dust dust = Dust.NewDustPerfect(
                center + Main.rand.NextVector2Circular(4f, 4f),
                DustID.GemEmerald,
                driftVelocity,
                100,
                Color.Lerp(PresetColor, Color.White, ReadyPulseScale),
                Main.rand.NextFloat(0.95f, 1.35f));
            dust.noGravity = true;

            if (Main.rand.NextBool(4))
            {
                Dust highlight = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(2f, 2f),
                    DustID.TerraBlade,
                    driftVelocity * 0.65f,
                    100,
                    Color.White,
                    Main.rand.NextFloat(0.75f, 1.05f));
                highlight.noGravity = true;
            }
        }

        private void SpawnReleasePulse()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 center = GunTipPosition;

            SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.72f, Pitch = -0.05f }, center);

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity =
                    direction.RotatedByRandom(0.22f) * Main.rand.NextFloat(2.5f, 6f) +
                    Main.rand.NextVector2Circular(1f, 1f);

                Dust dust = Dust.NewDustPerfect(
                    center,
                    DustID.GemEmerald,
                    velocity,
                    100,
                    Color.Lerp(PresetColor, Color.White, 0.4f),
                    Main.rand.NextFloat(0.95f, 1.4f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D weaponTexture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = weaponTexture.Size() * 0.5f;
            float rotation = Projectile.rotation;
            SpriteEffects effects = SpriteEffects.None;

            if (Owner?.gravDir == 1f)
            {
                if (Projectile.spriteDirection == -1)
                    effects = SpriteEffects.FlipVertically;
            }
            else if (Owner is not null)
            {
                origin.Y = weaponTexture.Height - origin.Y;
                if (Projectile.spriteDirection == 1)
                    effects = SpriteEffects.FlipVertically;
            }

            Main.EntitySpriteDraw(weaponTexture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, effects, 0);

            if (reloadTimer > 0)
                return false;

            Texture2D arrowTexture = ModContent.Request<Texture2D>(BFArrowCommon.GetTexturePathForPreset(CurrentPreset)).Value;
            Vector2 forwardDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 arrowDrawPosition = GunTipPosition - forwardDirection * MathHelper.Lerp(10f, 5f, ChargeCompletion) - Main.screenPosition;
            float pulse = readyBurstPlayed ? (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 8f) * 0.05f : 0f;
            float arrowScale = 0.9f + ChargeCompletion * 0.2f + pulse;
            Color arrowColor = Color.Lerp(Color.White, PresetColor, 0.45f + 0.25f * ChargeCompletion);

            Main.EntitySpriteDraw(
                arrowTexture,
                arrowDrawPosition,
                null,
                TurretModeActive ? Color.Lerp(arrowColor, Color.White, 0.18f) : arrowColor,
                Projectile.rotation + MathHelper.PiOver2 + MathHelper.Pi,
                arrowTexture.Size() * 0.5f,
                TurretModeActive ? arrowScale + 0.05f : arrowScale,
                SpriteEffects.None,
                0);

            return false;
        }

        private void FireSpecialArrow(Player player, float chargeCompletion, int projectileType, float baseSpeed, float damageMultiplier)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
            float speed = MathHelper.Lerp(baseSpeed * 0.76f, baseSpeed * 1.22f, chargeCompletion);
            int damage = (int)(Projectile.damage * RightClickBaseDamageMultiplier * MathHelper.Lerp(0.8f, 1.35f, chargeCompletion) * damageMultiplier);
            float knockback = Projectile.knockBack * MathHelper.Lerp(0.85f, 1.15f, chargeCompletion);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition,
                direction * speed,
                projectileType,
                damage,
                knockback,
                player.whoAmI);
        }
    }
}
