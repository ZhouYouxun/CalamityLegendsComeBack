using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BrinyBaron_TornadoBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "Terraria/Images/Projectile_407";

        private const float HomingRange = 920f;
        private const float MaxBonusDistance = 20f * 16f;
        private const float TrueMeleeDistance = 4f * 16f;

        private bool spawnedTornado;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            Projectile.rotation = 0f;
            Lighting.AddLight(Projectile.Center, 0.04f, 0.18f, 0.24f);
            HomeTowardTarget();

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    DustID.Water,
                    -Projectile.velocity * 0.12f + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    100,
                    new Color(95, 205, 255),
                    Main.rand.NextFloat(0.75f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnTornado();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnTornado();
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            SpawnTornado();
        }

        private void HomeTowardTarget()
        {
            NPC target = FindNearestTarget(HomingRange);
            if (target == null)
                return;

            Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * 17.5f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.095f);
        }

        private void SpawnTornado()
        {
            if (spawnedTornado || Main.myPlayer != Projectile.owner)
                return;

            spawnedTornado = true;
            Player owner = Main.player[Projectile.owner];
            float bonus = GetProximityBonus(owner);
            int duration = (int)MathHelper.Lerp(64f, 190f, bonus);
            int damage = Math.Max(1, (int)(Projectile.damage * MathHelper.Lerp(0.78f, 1.65f, bonus)));
            int tornadoType = ModContent.ProjectileType<BrinyBaron_Tornado>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Projectile.owner && projectile.type == tornadoType)
                    projectile.Kill();
            }

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                tornadoType,
                damage,
                Projectile.knockBack,
                Projectile.owner,
                duration);

            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.55f, Pitch = -0.15f }, Projectile.Center);
        }

        private float GetProximityBonus(Player owner)
        {
            if (!owner.active || owner.dead)
                return 0f;

            float distance = Projectile.Distance(owner.MountedCenter);
            return 1f - Utils.GetLerpValue(TrueMeleeDistance, MaxBonusDistance, distance, true);
        }

        private NPC FindNearestTarget(float maxDistance)
        {
            NPC closestTarget = null;
            float closestDistance = maxDistance;

            foreach (NPC npc in Main.npc)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closestTarget = npc;
            }

            return closestTarget;
        }
    }
}
