using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class RuinousSoul_OrbitGhost : ModProjectile, ILocalizedModType
    {
        public const int ReleaseCap = 6;

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
            orbitIndex = Projectile.ai[2] >= 0f ? (int)Projectile.ai[2] : Projectile.identity % ReleaseCap;

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

        public void Release(int targetIndex)
        {
            State = 1f;
            TargetIndex = targetIndex;
            Projectile.penetrate = 1;
            launched = true;
            time = 500;

            if (Main.npc.IndexInRange(targetIndex) && Main.npc[targetIndex].active)
                targeted = Main.npc[targetIndex];

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
                    targeted = Projectile.Center.ClosestNPCAt(950f);
            }

            CalamityUtils.HomeInOnSelectedNPC(Projectile, targeted, true, 0.15f, 6f, 0.98f, accelerate: true);

            if (time < 550 && targeted == null)
            {
                Vector2 mouseDirection = (owner.Calamity().mouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX);
                if (Projectile.velocity.Length() < 6f)
                    Projectile.velocity += mouseDirection * 0.35f;
                else
                    Projectile.velocity *= 0.9f;
            }
        }

        private void OrbitAI(Player owner)
        {
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
            modifiers.SourceDamage *= launched ? 1f : 0.3f;

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
