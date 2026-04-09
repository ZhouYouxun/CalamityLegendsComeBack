using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight
{
    public class YC_RightHoldOut : ModProjectile, ILocalizedModType
    {
        public const float MaxTargetRange = 100f * 16f;
        public const float TargetConeDegrees = 45f;
        public const int RightDroneCount = 8;
        public const int LaserCruiserCount = 4;
        public const int BattleshipCount = 2;
        public const int RepairShipCount = 1;

        private const float AnimationRampMax = 180f;
        private const int SoundInterval = 20;
        private const int AAttackCooldown = 10;
        private const int GroupPauseCooldown = 34;
        private const int CyclePauseCooldown = 46;

        private bool shipsSpawned;
        private int attackCooldown;
        private int attackStep;
        private float holdFrameCounter;

        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YharimsCrystalPrism";
        public new string LocalizationCategory => "Projectiles";

        private Player Owner => Main.player[Projectile.owner];
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
                (!Main.mouseRight || Main.mapFullscreen || Main.blockMouse))
            {
                Projectile.Kill();
                return;
            }

            holdFrameCounter++;
            UpdateHoldout();
            UpdateAnimation();
            EnsureShipsExist();
            RunAttackPattern();
        }

        public override void OnKill(int timeLeft)
        {
            KillBoundRightProjectiles();
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
                if (holdFrameCounter > 1f)
                    SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.18f, Pitch = 0.05f }, Projectile.Center);
            }
        }

        private void UpdateAnimation()
        {
            Projectile.frameCounter++;
            int framesPerUpdate = holdFrameCounter >= AnimationRampMax ? 2 :
                holdFrameCounter >= AnimationRampMax * 0.66f ? 3 : 4;

            if (Projectile.frameCounter >= framesPerUpdate)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }

        private void EnsureShipsExist()
        {
            if (shipsSpawned || Projectile.owner != Main.myPlayer)
                return;

            shipsSpawned = true;
            KillBoundRightProjectiles();

            for (int i = 0; i < RightDroneCount; i++)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, ForwardDirection, ModContent.ProjectileType<YC_Right_Drone>(), Projectile.damage, Projectile.knockBack, Projectile.owner, i, Projectile.whoAmI);
            }

            for (int i = 0; i < LaserCruiserCount; i++)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, ForwardDirection, ModContent.ProjectileType<YC_Right_LaserCruiser>(), Projectile.damage, Projectile.knockBack, Projectile.owner, i, Projectile.whoAmI);
            }

            for (int i = 0; i < BattleshipCount; i++)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, ForwardDirection, ModContent.ProjectileType<YC_Right_Battleship>(), Projectile.damage, Projectile.knockBack, Projectile.owner, i, Projectile.whoAmI);
            }

            for (int i = 0; i < RepairShipCount; i++)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, ForwardDirection, ModContent.ProjectileType<YC_Right_RepairShip>(), Projectile.damage, Projectile.knockBack, Projectile.owner, i, Projectile.whoAmI);
            }
        }

        private void RunAttackPattern()
        {
            if (attackCooldown > 0)
            {
                attackCooldown--;
                return;
            }

            if (Projectile.owner != Main.myPlayer)
                return;

            int currentStep = attackStep;
            bool fireB = currentStep % 5 == 4;

            if (fireB)
                FireBAttack();
            else
                FireAAttack();

            attackStep = (attackStep + 1) % 10;

            if (!fireB)
                attackCooldown = AAttackCooldown;
            else if (currentStep == 4)
                attackCooldown = GroupPauseCooldown;
            else
                attackCooldown = CyclePauseCooldown;
        }

        private void FireAAttack()
        {
            NPC target = ChooseVisibleTargetAhead();
            Vector2 forward = ForwardDirection;
            Vector2 start = Projectile.Center + forward * 18f;
            int targetIndex = target?.whoAmI ?? -1;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                start,
                forward,
                ModContent.ProjectileType<YC_Right_TrackerLaser>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                targetIndex,
                0f);

            SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.24f, Pitch = 0.2f }, start);
        }

        private void FireBAttack()
        {
            Vector2 forward = ForwardDirection;
            Vector2 start = Projectile.Center + forward * 20f;

            YC_CBeam.SpawnBeam(
                Projectile.GetSource_FromThis(),
                start,
                forward,
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI,
                YC_CBeam.BeamAnchorKind.RightHoldout,
                MaxTargetRange,
                24f,
                28,
                false,
                true,
                new Color(255, 198, 96),
                Color.White,
                24f,
                0f,
                -1,
                6);

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.34f, Pitch = -0.15f }, start);
        }

        private NPC ChooseVisibleTargetAhead()
        {
            List<NPC> targets = new();
            Vector2 forward = ForwardDirection;
            float maxDistanceSquared = MaxTargetRange * MaxTargetRange;
            float maxAngle = MathHelper.ToRadians(TargetConeDegrees) * 0.5f;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                Vector2 toNpc = npc.Center - Projectile.Center;
                if (toNpc.LengthSquared() > maxDistanceSquared)
                    continue;

                if (System.Math.Abs(MathHelper.WrapAngle(forward.ToRotation() - toNpc.ToRotation())) > maxAngle)
                    continue;

                if (!Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;

                targets.Add(npc);
            }

            return targets.Count <= 0 ? null : targets[Main.rand.Next(targets.Count)];
        }

        private void KillBoundRightProjectiles()
        {
            List<int> beamAnchorIndices = new() { Projectile.whoAmI };

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner)
                    continue;

                if (!YC_RightHelper.IsOwnedRightProjectileType(other.type))
                    continue;

                beamAnchorIndices.Add(other.whoAmI);
                other.Kill();
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != ModContent.ProjectileType<YC_CBeam>())
                    continue;

                YC_CBeam.BeamAnchorKind kind = (YC_CBeam.BeamAnchorKind)(int)other.ai[1];
                int anchorIndex = (int)other.ai[0];
                bool killForHoldout = kind == YC_CBeam.BeamAnchorKind.RightHoldout && anchorIndex == Projectile.whoAmI;
                bool killForShip = kind == YC_CBeam.BeamAnchorKind.RightDrone && beamAnchorIndices.Contains(anchorIndex);

                if (killForHoldout || killForShip)
                    other.Kill();
            }
        }
    }
}
