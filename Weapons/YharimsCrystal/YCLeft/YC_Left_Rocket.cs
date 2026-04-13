using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    public class YC_Left_Rocket : ModProjectile, ILocalizedModType
    {
        private enum RocketPhase
        {
            Launch,
            Brake,
            Pivot,
            Strike
        }

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCLeft/YC_Left_Rocket";

        private ref float PhaseValue => ref Projectile.ai[0];
        private ref float PhaseTimer => ref Projectile.ai[1];
        private ref float CachedTargetIndex => ref Projectile.localAI[0];

        private RocketPhase Phase
        {
            get => (RocketPhase)(int)PhaseValue;
            set => PhaseValue = (float)value;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => Phase == RocketPhase.Strike ? null : false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.velocity = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(5f));
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            CachedTargetIndex = -1f;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            switch (Phase)
            {
                case RocketPhase.Launch:
                    RunLaunchPhase();
                    break;

                case RocketPhase.Brake:
                    RunBrakePhase();
                    break;

                case RocketPhase.Pivot:
                    RunPivotPhase(owner);
                    break;

                case RocketPhase.Strike:
                    RunStrikePhase(owner);
                    break;
            }

            Projectile.rotation = CurrentFacing.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Color(88, 180, 255).ToVector3() * 0.38f);
            EmitExhaust();
            PhaseTimer++;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.Resize(68, 68);
                Projectile.penetrate = -1;
                Projectile.maxPenetrate = -1;
                Projectile.usesLocalNPCImmunity = true;
                Projectile.localNPCHitCooldown = -1;
                Projectile.Damage();
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.36f, Pitch = 0.05f }, Projectile.Center);

            if (Main.dedServ)
                return;

            for (int i = 0; i < 18; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                YC_LeftSquadronHelper.EmitTechDust(
                    Projectile.Center,
                    direction * Main.rand.NextFloat(2.4f, 5.6f),
                    Color.Lerp(new Color(255, 145, 90), new Color(110, 210, 255), Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.9f, 1.4f));
            }

            for (int i = 0; i < 10; i++)
            {
                Dust smoke = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Smoke,
                    Main.rand.NextVector2Circular(2.5f, 2.5f),
                    120,
                    default,
                    Main.rand.NextFloat(0.9f, 1.6f));
                smoke.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color outlineColor = new(70, 210, 255, 0);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = outlineColor * (0.08f + completion * 0.18f);

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition,
                    null,
                    trailColor,
                    Projectile.rotation,
                    origin,
                    Projectile.scale * (0.9f + completion * 0.15f),
                    SpriteEffects.None,
                    0);
            }

            Vector2 centerDrawPosition = Projectile.Center - Main.screenPosition;
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (MathHelper.PiOver2 * i).ToRotationVector2() * 1.4f;
                Main.EntitySpriteDraw(
                    texture,
                    centerDrawPosition + offset,
                    null,
                    outlineColor * 0.42f,
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                texture,
                centerDrawPosition,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }

        private Vector2 CurrentFacing => (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2();

        private void RunLaunchPhase()
        {
            Projectile.velocity *= 0.992f;

            if (PhaseTimer >= 12f)
                SwitchPhase(RocketPhase.Brake);
        }

        private void RunBrakePhase()
        {
            Projectile.velocity *= 0.965f;

            if (Projectile.velocity.Length() < 5.2f)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 5.2f;

            if (PhaseTimer >= 16f)
                SwitchPhase(RocketPhase.Pivot);
        }

        private void RunPivotPhase(Player owner)
        {
            NPC target = AcquireTarget(owner, 1600f);
            Vector2 desiredDirection = target != null
                ? (target.Center - Projectile.Center).SafeNormalize(CurrentFacing)
                : CurrentFacing.RotatedBy(MathHelper.ToRadians((float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.9f));

            float currentSpeed = Math.Max(2.1f, Projectile.velocity.Length() * 0.93f);
            Vector2 newDirection = RotateTowards(CurrentFacing, desiredDirection, MathHelper.ToRadians(3.2f));
            Projectile.velocity = newDirection * currentSpeed;

            if (PhaseTimer >= 28f)
                SwitchPhase(RocketPhase.Strike);
        }

        private void RunStrikePhase(Player owner)
        {
            NPC target = AcquireTarget(owner, 1800f);
            Vector2 desiredDirection = target != null
                ? (target.Center - Projectile.Center).SafeNormalize(CurrentFacing)
                : CurrentFacing;

            float speed = MathHelper.Lerp(Projectile.velocity.Length(), 17.5f, 0.09f);
            Vector2 newDirection = RotateTowards(CurrentFacing, desiredDirection, MathHelper.ToRadians(5.6f));
            Projectile.velocity = newDirection * speed;
        }

        private NPC AcquireTarget(Player owner, float range)
        {
            int cachedIndex = (int)CachedTargetIndex;
            if (cachedIndex >= 0 && cachedIndex < Main.maxNPCs)
            {
                NPC cached = Main.npc[cachedIndex];
                if (cached.active &&
                    cached.CanBeChasedBy(Projectile) &&
                    Vector2.DistanceSquared(Projectile.Center, cached.Center) <= range * range)
                {
                    return cached;
                }
            }

            NPC target = YC_LeftSquadronHelper.FindPriorityTarget(owner, Projectile.Center, range, CurrentFacing, 110f, false);
            CachedTargetIndex = target?.whoAmI ?? -1;
            return target;
        }

        private void EmitExhaust()
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(CurrentFacing);
            Vector2 exhaustOrigin = Projectile.Center - direction * 10f;
            float intensity = Phase == RocketPhase.Strike ? 1.2f : Phase == RocketPhase.Pivot ? 0.95f : 0.75f;

            if (Main.rand.NextBool(2))
            {
                Dust smoke = Dust.NewDustPerfect(
                    exhaustOrigin,
                    DustID.Smoke,
                    -direction * Main.rand.NextFloat(1.5f, 3.1f) * intensity + Main.rand.NextVector2Circular(0.6f, 0.6f),
                    110,
                    default,
                    Main.rand.NextFloat(0.75f, 1.2f));
                smoke.noGravity = true;
            }

            YC_LeftSquadronHelper.EmitTechDust(
                exhaustOrigin,
                -direction.RotatedByRandom(0.22f) * Main.rand.NextFloat(1.6f, 4.2f) * intensity,
                Color.Lerp(new Color(255, 170, 90), new Color(110, 210, 255), Main.rand.NextFloat(0.2f, 0.55f)),
                Main.rand.NextFloat(0.75f, 1.15f));
        }

        private void SwitchPhase(RocketPhase newPhase)
        {
            Phase = newPhase;
            PhaseTimer = 0f;
            Projectile.netUpdate = true;
        }

        private static Vector2 RotateTowards(Vector2 currentDirection, Vector2 desiredDirection, float maxTurnRadians)
        {
            float currentAngle = currentDirection.ToRotation();
            float desiredAngle = desiredDirection.ToRotation();
            float delta = MathHelper.WrapAngle(desiredAngle - currentAngle);
            delta = MathHelper.Clamp(delta, -maxTurnRadians, maxTurnRadians);
            return (currentAngle + delta).ToRotationVector2();
        }
    }
}
