using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal
{
    public class YC_Right_Drone : ModProjectile, ILocalizedModType
    {
        private const float PositionLerp = 0.35f;
        private const int BeamDuration = 18;
        private const int BeamCooldown = 60;
        private const float DetectionRange = 70f * 16f;
        private const float DetectionConeDegrees = 7f;

        private static readonly Vector2[] RelativeOffsets =
        {
            new(-26f, -8f),
            new(-44f, -24f),
            new(-60f, -46f),
            new(26f, -8f),
            new(44f, -24f),
            new(60f, -46f)
        };

        private static readonly float[] AngleOffsetsDegrees =
        {
            -2f,
            -5f,
            -8f,
            2f,
            5f,
            8f
        };

        private bool positionInitialized;
        private int fireCooldown;

        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/EXSkill/YC_EX_Drone";

        public int SlotIndex => Utils.Clamp((int)Projectile.ai[0], 0, RelativeOffsets.Length - 1);
        public int ParentHoldoutIndex => (int)Projectile.ai[1];
        public Vector2 CurrentForwardDirection { get; private set; }

        public static readonly Color DroneColor = new(255, 238, 178);
        public static readonly Color CoreColor = new(255, 255, 245);

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.hide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!TryGetParentHoldout(out Projectile holdoutProjectile, out YC_RightHoldOut holdout))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            UpdateBoundPosition(holdoutProjectile, holdout);
            EmitDroneFX();

            if (fireCooldown > 0)
                fireCooldown--;

            if (Projectile.owner == Main.myPlayer && fireCooldown <= 0 && HasEnemyAhead())
            {
                YC_CBeam.SpawnBeam(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + CurrentForwardDirection * 14f,
                    CurrentForwardDirection,
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    Projectile.whoAmI,
                    YC_CBeam.BeamAnchorKind.RightDrone,
                    DetectionRange,
                    10f,
                    BeamDuration,
                    false,
                    false,
                    DroneColor,
                    CoreColor,
                    14f,
                    0f,
                    1,
                    -1);

                fireCooldown = BeamDuration + BeamCooldown;
                SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.12f, Pitch = -0.15f + SlotIndex * 0.04f }, Projectile.Center);
            }

            Lighting.AddLight(Projectile.Center, DroneColor.ToVector3() * 0.42f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            bool beamActive = HasActiveBeam();

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0);

            if (beamActive)
            {
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition,
                    null,
                    DroneColor * 0.75f,
                    Projectile.rotation,
                    origin,
                    Projectile.scale * 1.12f,
                    SpriteEffects.None,
                    0);
            }

            return false;
        }

        private void UpdateBoundPosition(Projectile holdoutProjectile, YC_RightHoldOut holdout)
        {
            Vector2 forward = holdout.ForwardDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 local = RelativeOffsets[SlotIndex];

            Vector2 desiredCenter = holdoutProjectile.Center + right * local.X + forward * local.Y;
            CurrentForwardDirection = forward.RotatedBy(MathHelper.ToRadians(AngleOffsetsDegrees[SlotIndex]));

            if (!positionInitialized)
            {
                Projectile.Center = desiredCenter;
                positionInitialized = true;
            }
            else
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, desiredCenter, PositionLerp);
            }

            Projectile.rotation = CurrentForwardDirection.ToRotation() + MathHelper.PiOver2;
            Projectile.scale = 0.88f + 0.04f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 5f + SlotIndex * 0.7f);
        }

        private bool HasEnemyAhead()
        {
            float maxDistanceSquared = DetectionRange * DetectionRange;
            float maxAngle = MathHelper.ToRadians(DetectionConeDegrees);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                Vector2 toNpc = npc.Center - Projectile.Center;
                if (toNpc.LengthSquared() > maxDistanceSquared)
                    continue;

                if (System.Math.Abs(MathHelper.WrapAngle(CurrentForwardDirection.ToRotation() - toNpc.ToRotation())) > maxAngle)
                    continue;

                if (!Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;

                return true;
            }

            return false;
        }

        private bool TryGetParentHoldout(out Projectile holdoutProjectile, out YC_RightHoldOut holdout)
        {
            holdoutProjectile = null;
            holdout = null;

            if (ParentHoldoutIndex < 0 || ParentHoldoutIndex >= Main.maxProjectiles)
                return false;

            Projectile possibleHoldout = Main.projectile[ParentHoldoutIndex];
            if (!possibleHoldout.active || possibleHoldout.owner != Projectile.owner || possibleHoldout.type != ModContent.ProjectileType<YC_RightHoldOut>())
                return false;

            if (possibleHoldout.ModProjectile is not YC_RightHoldOut holdoutMod)
                return false;

            holdoutProjectile = possibleHoldout;
            holdout = holdoutMod;
            return true;
        }

        private void EmitDroneFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % 10 != 0)
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                DustID.GoldFlame,
                CurrentForwardDirection.RotatedByRandom(0.2f) * Main.rand.NextFloat(0.5f, 1.2f),
                0,
                DroneColor,
                Main.rand.NextFloat(0.8f, 1.05f));
            dust.noGravity = true;
        }

        private bool HasActiveBeam()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner || other.type != ModContent.ProjectileType<YC_CBeam>())
                    continue;

                if ((int)other.ai[0] == Projectile.whoAmI &&
                    (YC_CBeam.BeamAnchorKind)(int)other.ai[1] == YC_CBeam.BeamAnchorKind.RightDrone)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
