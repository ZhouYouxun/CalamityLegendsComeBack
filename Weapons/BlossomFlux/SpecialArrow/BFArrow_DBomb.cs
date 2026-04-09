using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    // D 战术右键箭：命中后把玩家当前箭雨从天上持续砸下来。
    internal class BFArrow_DBomb : ModProjectile
    {
        private int rainCounter;
        private int storedAmmoType = ProjectileID.WoodenArrowFriendly;
        private float storedAmmoSpeed = 14f;
        private float storedAmmoKnockback = 2f;
        private Vector2 stickOffset;

        private ref float State => ref Projectile.ai[0];
        private ref float AttachedNpcIndex => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 240, penetrate: -1, extraUpdates: 1, tileCollide: true);
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => State == 0f ? null : false;

        public override bool? CanHitNPC(NPC target) => State == 0f ? null : false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];
            if (BFArrowCommon.TryPickBlossomFluxAmmo(owner, out int ammoType, out float ammoSpeed, out _, out float ammoKnockback))
            {
                storedAmmoType = ammoType;
                storedAmmoSpeed = ammoSpeed;
                storedAmmoKnockback = ammoKnockback;
            }
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, new Color(255, 186, 110).ToVector3() * 0.48f);

            if (State == 0f)
            {
                BFArrowCommon.FaceForward(Projectile);
                Projectile.velocity *= 1.002f;

                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.Torch,
                        -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.7f, 0.7f),
                        100,
                        new Color(255, 186, 110),
                        Main.rand.NextFloat(0.85f, 1.2f));
                    dust.noGravity = true;
                }

                return;
            }

            Projectile.friendly = false;
            Projectile.tileCollide = false;

            if (!BFArrowCommon.InBounds(AttachedNpcIndex, Main.maxNPCs))
            {
                Projectile.Kill();
                return;
            }

            NPC attachedNpc = Main.npc[(int)AttachedNpcIndex];
            if (!attachedNpc.active || attachedNpc.dontTakeDamage)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = attachedNpc.Center + stickOffset;
            Projectile.gfxOffY = attachedNpc.gfxOffY;
            rainCounter++;

            if (rainCounter % 8 == 0)
                SpawnArrowRain(attachedNpc);

            if (rainCounter % 24 == 0)
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.18f, Pitch = 0.45f }, attachedNpc.Center);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (State != 0f)
                return;

            stickOffset = Projectile.Center - target.Center;
            State = 1f;
            AttachedNpcIndex = target.whoAmI;
            Projectile.damage = 0;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 96;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.45f, Pitch = -0.1f }, target.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.3f, Pitch = -0.2f }, Projectile.Center);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Torch,
                    Main.rand.NextVector2CircularEdge(3f, 3f) * Main.rand.NextFloat(1.5f, 5.5f),
                    100,
                    new Color(255, 186, 110),
                    Main.rand.NextFloat(0.95f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BFArrowCommon.DrawAfterimagesThenProjectile(Projectile, lightColor);
            return false;
        }

        private void SpawnArrowRain(NPC target)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector2 spawnPosition = target.Center + new Vector2(Main.rand.NextFloat(-140f, 140f), -620f - Main.rand.NextFloat(0f, 140f));
                Vector2 targetPosition = target.Center + Main.rand.NextVector2Circular(28f, 18f);
                Vector2 velocity = (targetPosition - spawnPosition).SafeNormalize(Vector2.UnitY) * (storedAmmoSpeed * Main.rand.NextFloat(1.15f, 1.45f));

                int projectileIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    storedAmmoType,
                    (int)(Projectile.damage * 0.55f),
                    storedAmmoKnockback,
                    Projectile.owner);

                if (!BFArrowCommon.InBounds(projectileIndex, Main.maxProjectiles))
                    continue;

                Projectile rainArrow = Main.projectile[projectileIndex];
                rainArrow.arrow = true;
                rainArrow.noDropItem = true;
                BFArrowCommon.TagBlossomFluxLeftArrow(rainArrow);
            }
        }
    }
}
