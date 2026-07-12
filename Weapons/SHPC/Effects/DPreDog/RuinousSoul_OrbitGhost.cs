using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class RuinousSoul_OrbitGhost : ModProjectile, ILocalizedModType
    {
        public const int ReleaseCap = 27;
        public const int SpawnBatchSize = 4;
        public const int ForcedReleaseTime = 100;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/DPreDog/RuinousSoul_OrbitGhost";

        public ref float State => ref Projectile.ai[0];
        public ref float TargetIndex => ref Projectile.ai[1];

        private int time;
        private float dustRotation;
        private bool launched;
        private NPC targeted;
        private int orbitIndex;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 5;
            Projectile.timeLeft = 900;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16 * Projectile.MaxUpdates;
            Projectile.ArmorPenetration = 15;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            // ai[2] stores the monotonically increasing spawn order.  Keep only its
            // slot for the visual orbit, while the full value is used by the owner to
            // release the oldest souls first.
            orbitIndex = Projectile.ai[2] >= 0f ? (int)Projectile.ai[2] % ReleaseCap : Projectile.identity % ReleaseCap;

            if (State == 1f)
            {
                launched = true;
                Projectile.penetrate = 1;

                if (Main.npc.IndexInRange((int)TargetIndex) && Main.npc[(int)TargetIndex].active)
                    targeted = Main.npc[(int)TargetIndex];
            }
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }

            if (Projectile.frame >= Main.projFrames[Type])
                Projectile.frame = 0;

            dustRotation += 0.12f;
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.5f);

            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0f).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi);

            if (State == 1f)
                LaunchedAI(owner);
            else
                OrbitAI(owner);

            SpawnPhantasmalTrail();
            time++;
        }

        public void Release(int targetIndex, bool forceReleaseTime = false)
        {
            State = 1f;
            TargetIndex = targetIndex;
            Projectile.penetrate = 1;
            launched = true;
            time = 500;

            if (forceReleaseTime)
                Projectile.timeLeft = ForcedReleaseTime;
            else
                Projectile.timeLeft = System.Math.Max(Projectile.timeLeft, ForcedReleaseTime);

            if (Main.npc.IndexInRange(targetIndex) && Main.npc[targetIndex].active)
                targeted = Main.npc[targetIndex];

            // An orbiting soul has usually been damped almost to a halt.  Give it an
            // immediate outward velocity when it is released, otherwise it can look
            // as if it stayed in the orbit until the homing code catches up.
            Player owner = Main.player[Projectile.owner];
            Vector2 destination = targeted?.Center ?? owner.Calamity().mouseWorld;
            Vector2 direction = (destination - Projectile.Center).SafeNormalize(
                (Projectile.Center - owner.Center).SafeNormalize(Vector2.UnitX));
            Projectile.velocity = direction * 10f;

            Projectile.netUpdate = true;
        }

        private void LaunchedAI(Player owner)
        {
            launched = true;

            if (targeted == null || !targeted.active || targeted.life <= 0 || !targeted.CanBeChasedBy(Projectile))
            {
                if (Main.npc.IndexInRange((int)TargetIndex) && Main.npc[(int)TargetIndex].active && Main.npc[(int)TargetIndex].CanBeChasedBy(Projectile))
                    targeted = Main.npc[(int)TargetIndex];
                else
                    targeted = FindLaunchTarget(owner);
            }

            if (targeted != null)
            {
                int launchAge = System.Math.Max(0, time - 500);
                float warmup = Utils.GetLerpValue(0f, 42f, launchAge, true);
                Vector2 predictedCenter = targeted.Center + targeted.velocity * MathHelper.Lerp(4f, 18f, warmup);
                Vector2 desired = (predictedCenter - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
                float speed = MathHelper.Lerp(7.5f, 17.5f, warmup);
                float inertia = MathHelper.Lerp(21f, 3.8f, warmup);

                Projectile.velocity = (Projectile.velocity * inertia + desired * speed) / (inertia + 1f);
                Projectile.velocity = Projectile.velocity.SafeNormalize(desired) * MathHelper.Clamp(Projectile.velocity.Length(), 5f, 18.5f);
            }

            if (time < 550 && targeted == null)
            {
                Vector2 mouseDirection = (owner.Calamity().mouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX);
                if (Projectile.velocity.Length() < 6f)
                    Projectile.velocity += mouseDirection * 0.35f;
                else
                    Projectile.velocity *= 0.9f;
            }
        }

        private NPC FindLaunchTarget(Player owner)
        {
            NPC best = null;
            float bestScore = 1650f;
            Vector2 mouseWorld = owner.Calamity().mouseWorld;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distanceToGhost = Projectile.Distance(npc.Center);
                float distanceToMouse = Vector2.Distance(mouseWorld, npc.Center);
                float score = distanceToGhost * 0.62f + distanceToMouse * 0.38f;
                if (npc.boss)
                    score *= 0.74f;

                if (score >= bestScore)
                    continue;

                best = npc;
                bestScore = score;
            }

            return best;
        }

        private void OrbitAI(Player owner)
        {
            if (!OwnerStillHoldingSHPC(owner))
            {
                NPC target = FindLaunchTarget(owner);
                int targetIdx = target != null ? target.whoAmI : -1;
                Release(targetIdx);
                return;
            }

            Projectile.timeLeft = 2;

            if (time <= 15)
                return;

            float angle = time * 0.05f + orbitIndex * MathHelper.TwoPi / ReleaseCap;
            Vector2 circle = owner.Center + new Vector2(0f, -30f).RotatedBy(angle);
            Vector2 moveToOwnerOrbit = (circle - Projectile.Center).SafeNormalize(Vector2.UnitX);

            if (Projectile.velocity.Length() < 8f)
                Projectile.velocity += moveToOwnerOrbit * Main.rand.NextFloat(0.2f, 0.4f);
            else
                Projectile.velocity *= 0.85f;
        }

        private static bool OwnerStillHoldingSHPC(Player owner)
        {
            int heldType = owner.HeldItem?.type ?? ItemID.None;
            return heldType == ModContent.ItemType<global::CalamityLegendsComeBack.Weapons.SHPC.NewLegendSHPC>();
        }

        private void SpawnPhantasmalTrail()
        {
            if (time <= 5)
                return;

            Vector2 dustPos = Projectile.Center + (MathHelper.Pi + dustRotation + MathHelper.PiOver2).ToRotationVector2() * 10f * Projectile.scale;
            Dust dust = Dust.NewDustPerfect(
                dustPos,
                DustID.SpectreStaff,
                (MathHelper.Pi + dustRotation * System.Math.Sign(Projectile.velocity.Length())).ToRotationVector2() * 2f);
            dust.noGravity = false;
            dust.scale = Main.rand.NextFloat(0.75f, 1.2f);
            dust.alpha = Main.rand.Next(100, 171);
            dust.velocity = dust.velocity.RotatedByRandom(0f);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Orbiting souls retain their charge damage; released souls are primarily
            // a visual/utility follow-up and should only deal 17% damage.
            modifiers.SourceDamage *= launched ? 0.17f : 0.3f;

            Player owner = Main.player[Projectile.owner];
            Vector2 launchVelocity = (owner.Center - target.Center).SafeNormalize(Vector2.UnitY) * -10f * (launched ? 0.5f : 1f);
            target.MoveNPC(launchVelocity, 10f * (launched ? 0.5f : 1f), true);
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (targeted != null)
                return target == targeted ? null : false;

            return null;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i <= 3; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.SpectreStaff,
                    (Projectile.velocity * 6f).RotatedByRandom(0.4f) * Main.rand.NextFloat(0.1f, 0.8f),
                    100,
                    default,
                    Main.rand.NextFloat(1.2f, 1.8f));
                dust.noGravity = true;

                Dust chargeFull = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(4) ? 278 : 267);
                chargeFull.velocity = Projectile.velocity.RotatedByRandom(0.25f) * Main.rand.NextFloat(1f, 4f);
                chargeFull.scale = Main.rand.NextFloat(0.5f, 0.9f);
                chargeFull.noGravity = true;
                chargeFull.color = Color.Lerp(Color.White, Color.Aqua, 0.3f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.White, 1);
            return false;
        }
    }
}
