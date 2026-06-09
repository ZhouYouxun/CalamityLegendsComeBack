using CalamityMod;
using CalamityMod.Projectiles.Turret;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClickTurret
{
    internal sealed class MilitaryFriendlyTurret : ModProjectile, ILocalizedModType
    {
        private const int DeployAnimationTime = 24;

        private int firingTime;
        private int targetIndex = -1;
        private int deployTimer = DeployAnimationTime;
        private float angle;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private MilitaryTurretKind Kind => (MilitaryTurretKind)Utils.Clamp((int)Projectile.ai[0], 0, 6);
        private int SourceDamage => Math.Max(1, (int)Projectile.ai[1]);
        private bool ReplacedByNewTurret => Projectile.ai[2] == 1f;
        private MilitaryTurretStats Stats => MilitaryTurretUtility.GetStats(Kind);
        private int Direction => Math.Cos(angle) > 0D ? 1 : -1;
        private Vector2 BodyTopLeft => Projectile.Bottom - new Vector2(27f, 36f);
        private Vector2 TurretPivot => BodyTopLeft + new Vector2(22f + 4f * Direction, -2f);
        private Vector2 MuzzlePosition => TurretPivot + angle.ToRotationVector2() * Stats.ShootForwardOffset;

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = 36;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = MilitaryTurretUtility.TurretLifetime;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.velocity = Vector2.Zero;
            Projectile.Bottom = MilitaryTurretUtility.FindRestingPoint(Projectile.Bottom);

            if (deployTimer > 0)
                deployTimer--;

            NPC target = ChooseTarget();
            if (target == null)
            {
                targetIndex = -1;
                firingTime = 0;
                UpdateAngle(GetRestingAngle());
                SpawnIdleEffects();
                return;
            }

            targetIndex = target.whoAmI;
            float targetAngle = (target.Center - TurretPivot).ToRotation();
            UpdateAngle(targetAngle);

            firingTime++;
            if (CanFireAt(target, targetAngle) &&
                firingTime >= Stats.StartupDelay &&
                (firingTime - Stats.StartupDelay) % Stats.UseTime == 0)
            {
                FireShot();
            }

            SpawnIdleEffects();
        }

        public override void OnKill(int timeLeft)
        {
            Color color = Stats.ThemeColor;
            if (!Main.dedServ)
            {
                for (int i = 0; i < (ReplacedByNewTurret ? 36 : 18); i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(26f, 18f), ReplacedByNewTurret ? DustID.Torch : DustID.Electric);
                    dust.velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, ReplacedByNewTurret ? 9f : 5f);
                    dust.color = Color.Lerp(color, Color.White, Main.rand.NextFloat(0.2f, 0.82f));
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(0.75f, ReplacedByNewTurret ? 1.65f : 1.2f);
                }
            }

            if (!ReplacedByNewTurret || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<MilitaryTurretSelfDestruct>(),
                Math.Max(1, GetScaledDamage() * 3),
                8f,
                Projectile.owner,
                (float)Kind);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(firingTime);
            writer.Write(targetIndex);
            writer.Write(deployTimer);
            writer.Write(angle);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            firingTime = reader.ReadInt32();
            targetIndex = reader.ReadInt32();
            deployTimer = reader.ReadInt32();
            angle = reader.ReadSingle();
        }

        private NPC ChooseTarget()
        {
            NPC current = Main.npc.IndexInRange(targetIndex) ? Main.npc[targetIndex] : null;
            if (IsValidTarget(current))
                return current;

            NPC closest = null;
            float bestDistance = Stats.MaxRange * Stats.MaxRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!IsValidTarget(npc))
                    continue;

                float distance = npc.DistanceSQ(TurretPivot);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        private bool IsValidTarget(NPC npc)
        {
            if (npc == null || !npc.CanBeChasedBy(Projectile, false) || npc.CountsAsACritter)
                return false;

            if (Vector2.DistanceSquared(npc.Center, TurretPivot) > Stats.MaxRange * Stats.MaxRange)
                return false;

            Vector2 toTarget = npc.Center - TurretPivot;
            return CalamityUtils.PreciseCanHitInLine(TurretPivot, toTarget.ToRotation(), toTarget.Length());
        }

        private void UpdateAngle(float targetAngle)
        {
            float deltaAngle = MathHelper.WrapAngle(angle - targetAngle);
            bool close = Math.Abs(deltaAngle) <= Math.Max(Stats.CloseAimThreshold, Stats.MaxDeltaAngle);

            angle = close
                ? Utils.AngleLerp(angle, targetAngle, Stats.CloseAimLerp)
                : MathHelper.WrapAngle(angle - Stats.MaxDeltaAngle * Math.Sign(deltaAngle));

            Projectile.spriteDirection = Direction;
        }

        private float GetRestingAngle()
        {
            Player owner = Main.player[Projectile.owner];
            return owner.Center.X >= Projectile.Center.X ? 0f : MathHelper.Pi;
        }

        private bool CanFireAt(NPC target, float targetAngle)
        {
            if (deployTimer > 0)
                return false;

            float deltaAngle = Math.Abs(MathHelper.WrapAngle(angle - targetAngle));
            return deltaAngle <= Stats.MaxAngleDeviance && IsValidTarget(target);
        }

        private void FireShot()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 velocity = angle.ToRotationVector2() * Stats.ShootSpeed;
            int damage = GetScaledDamage();

            if (Kind == MilitaryTurretKind.Onyx)
            {
                SoundEngine.PlaySound(SoundID.Item36 with { Volume = 0.65f }, MuzzlePosition);
                for (int i = -1; i <= 1; i++)
                    SpawnCalamityTurretProjectile(velocity.RotatedBy(Main.rand.NextFloat(0.035f, 0.11f) * i), damage);
            }
            else
                SpawnCalamityTurretProjectile(velocity, damage);

            Projectile.netUpdate = true;
        }

        private void SpawnCalamityTurretProjectile(Vector2 velocity, int damage)
        {
            Projectile shot = Projectile.NewProjectileDirect(
                Projectile.GetSource_FromThis(),
                MuzzlePosition,
                velocity,
                Stats.ProjectileType,
                damage,
                Stats.Knockback,
                Projectile.owner);

            shot.DamageType = DamageClass.Magic;
            shot.CritChance = Projectile.CritChance;
        }

        private int GetScaledDamage()
        {
            float sourceScale = MathHelper.Clamp(SourceDamage / 80f, 0.65f, 2.5f);
            return Math.Max(1, (int)Math.Round(Stats.BaseDamage * sourceScale));
        }

        private void SpawnIdleEffects()
        {
            if (Main.dedServ || Main.rand.NextBool(10))
                return;

            Dust dust = Dust.NewDustPerfect(TurretPivot + Main.rand.NextVector2Circular(8f, 4f), DustID.Electric);
            dust.velocity = Main.rand.NextVector2Circular(0.8f, 0.8f);
            dust.color = Color.Lerp(Stats.ThemeColor, Color.White, Main.rand.NextFloat(0.2f, 0.75f));
            dust.noGravity = true;
            dust.scale = Main.rand.NextFloat(0.45f, 0.8f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            MilitaryTurretStats stats = Stats;
            Texture2D body = ModContent.Request<Texture2D>(stats.BodyTexture).Value;
            Texture2D head = ModContent.Request<Texture2D>(stats.HeadTexture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 bodyDrawPosition = Projectile.Bottom - Main.screenPosition;
            Vector2 headDrawPosition = TurretPivot - Main.screenPosition;
            float deployScale = MathHelper.Lerp(0.72f, 1f, 1f - deployTimer / (float)DeployAnimationTime);
            float fade = MathHelper.Clamp(Projectile.timeLeft / 45f, 0f, 1f);
            Color drawColor = Projectile.GetAlpha(lightColor) * fade;
            Color glowColor = stats.ThemeColor with { A = 0 };
            SpriteEffects effects = Direction == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            Main.EntitySpriteDraw(
                bloom,
                headDrawPosition,
                null,
                glowColor * (0.22f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7f + Projectile.identity)),
                0f,
                bloom.Size() * 0.5f,
                new Vector2(0.34f, 0.2f) * deployScale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                body,
                bodyDrawPosition,
                null,
                drawColor,
                0f,
                new Vector2(body.Width * 0.5f, body.Height),
                deployScale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                head,
                headDrawPosition,
                null,
                drawColor,
                angle,
                head.Size() * 0.5f,
                deployScale,
                effects,
                0);

            return false;
        }
    }
}
