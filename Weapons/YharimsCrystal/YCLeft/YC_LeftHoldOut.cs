using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    public class YC_LeftHoldOut : ModProjectile, ILocalizedModType
    {
        public const int FrigateCount = 6;
        public const int LaserCruiserCount = 4;
        public const int BattleshipCount = 2;
        public const int RepairShipCount = 1;
        public const float MaxLaserOffsetDegrees = 10f;
        private const float AnimationRampMax = 180f;
        private const int SoundInterval = 20;

        private ref float ManualAimModeFlag => ref Projectile.ai[0];
        private ref float HoldFrameCounter => ref Projectile.localAI[0];
        private ref float DronesSpawnedFlag => ref Projectile.localAI[1];

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YharimsCrystalPrism";
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        private Player Owner => Main.player[Projectile.owner];
        public bool ManualAimMode => ManualAimModeFlag == 1f;
        public Vector2 ForwardDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 22;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed || Owner.HeldItem.type != ModContent.ItemType<NewLegendYharimsCrystal>())
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.owner == Main.myPlayer &&
                (!Main.mouseLeft || Main.mapFullscreen || Main.blockMouse))
            {
                Projectile.Kill();
                return;
            }

            HoldFrameCounter++;
            UpdateHoldout();
            UpdateAnimation();
            UpdateManualAimState();
            EnsureDronesExist();
        }

        public override void OnKill(int timeLeft)
        {
            KillOwnedLeftProjectiles();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            int frameYOffset = frameHeight * Projectile.frame;
            Vector2 drawPosition = (Projectile.Center + Vector2.UnitY * Projectile.gfxOffY - Main.screenPosition).Floor();

            Main.spriteBatch.Draw(
                texture,
                drawPosition,
                new Rectangle(0, frameYOffset, texture.Width, frameHeight),
                Color.White,
                Projectile.rotation,
                new Vector2(texture.Width * 0.5f, frameHeight * 0.5f),
                Projectile.scale,
                effects,
                0f);

            return false;
        }

        private void UpdateHoldout()
        {
            Vector2 holdoutCenter = Owner.RotatedRelativePoint(Owner.MountedCenter, true);

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 aimVector = (Main.MouseWorld - holdoutCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
                if (aimVector != Projectile.velocity)
                    Projectile.netUpdate = true;

                Projectile.velocity = aimVector;
            }

            Projectile.Center = holdoutCenter;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction;

            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRotation += MathHelper.Pi;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Projectile.timeLeft = 2;

            if (Projectile.soundDelay <= 0)
            {
                Projectile.soundDelay = SoundInterval;
                if (HoldFrameCounter > 1f)
                    SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.18f, Pitch = 0.15f }, Projectile.Center);
            }
        }

        private void UpdateAnimation()
        {
            Projectile.frameCounter++;
            int framesPerUpdate = HoldFrameCounter >= AnimationRampMax ? 2 :
                HoldFrameCounter >= AnimationRampMax * 0.66f ? 3 : 4;

            if (Projectile.frameCounter >= framesPerUpdate)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }

        private void UpdateManualAimState()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            float newState = Main.mouseRight && !Main.mapFullscreen && !Main.blockMouse ? 1f : 0f;
            if (ManualAimModeFlag != newState)
            {
                ManualAimModeFlag = newState;
                Projectile.netUpdate = true;
            }
        }

        private void EnsureDronesExist()
        {
            if (DronesSpawnedFlag == 1f || Projectile.owner != Main.myPlayer)
                return;

            DronesSpawnedFlag = 1f;
            KillOwnedLeftProjectiles();

            for (int i = 0; i < FrigateCount; i++)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    ForwardDirection,
                    ModContent.ProjectileType<YC_Left_Frigate>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    i,
                    Projectile.whoAmI);
            }

            for (int i = 0; i < LaserCruiserCount; i++)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    ForwardDirection,
                    ModContent.ProjectileType<YC_Left_LaserCruiser>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    i,
                    Projectile.whoAmI);
            }

            for (int i = 0; i < BattleshipCount; i++)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    ForwardDirection,
                    ModContent.ProjectileType<YC_Left_MissileBattleship>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    i,
                    Projectile.whoAmI);
            }

            for (int i = 0; i < RepairShipCount; i++)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    ForwardDirection,
                    ModContent.ProjectileType<YC_Left_RepairShip>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    i,
                    Projectile.whoAmI);
            }

            Projectile.netUpdate = true;
        }

        private void KillOwnedLeftProjectiles()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner)
                    continue;

                if (YC_LeftSquadronHelper.IsLeftOwnedProjectileType(other.type))
                {
                    other.Kill();
                    continue;
                }

                if (other.type == ModContent.ProjectileType<YC_CBeam>())
                {
                    YC_CBeam.BeamAnchorKind kind = (YC_CBeam.BeamAnchorKind)(int)other.ai[1];
                    if (kind == YC_CBeam.BeamAnchorKind.LeftDrone || kind == YC_CBeam.BeamAnchorKind.LeftHoldout)
                        other.Kill();
                }
            }
        }
    }
}
