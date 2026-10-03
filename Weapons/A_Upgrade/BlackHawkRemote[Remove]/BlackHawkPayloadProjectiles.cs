using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.BlackHawkRemote
{
    internal sealed class BlackHawkPayload : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BlackHawk";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private BlackHawkLoadout Loadout => BlackHawkLoadoutInfo.Sanitize((int)Projectile.ai[0]);
        private Vector2 AttackDirection => Projectile.ai[1].ToRotationVector2();
        private int BaseDamage => Math.Max(1, Projectile.originalDamage > 0 ? Projectile.originalDamage : Projectile.damage);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 7;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 36;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.timeLeft = Loadout switch
            {
                BlackHawkLoadout.ClusterBomb => 28,
                BlackHawkLoadout.Napalm => 34,
                BlackHawkLoadout.Cryogenic => 34,
                BlackHawkLoadout.EMP => 38,
                BlackHawkLoadout.HolyPayload => 32,
                BlackHawkLoadout.DirtyBomb => 42,
                BlackHawkLoadout.HeavyBomb => 56,
                _ => 32
            };
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            float drag = Loadout == BlackHawkLoadout.HeavyBomb ? 0.88f : 0.91f;
            Projectile.velocity *= drag;
            Projectile.rotation = AttackDirection.ToRotation();
            Color color = BlackHawkLoadoutInfo.Color(Loadout);
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.42f);

            if (Main.GameUpdateCount % 3 == Projectile.identity % 3)
            {
                Vector2 rear = Projectile.Center - AttackDirection * 8f;
                BlackHawkVFX.SpawnSmokePoint(rear, -AttackDirection * 0.45f, color, new Color(54, 57, 61),
                    Loadout == BlackHawkLoadout.HeavyBomb ? 0.58f : 0.36f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            switch (Loadout)
            {
                case BlackHawkLoadout.ClusterBomb:
                    SpawnClusterShards();
                    break;
                case BlackHawkLoadout.Napalm:
                    SpawnBlast(Projectile.damage, 72f);
                    SpawnNapalmZones();
                    break;
                case BlackHawkLoadout.Cryogenic:
                    SpawnBlast(Projectile.damage, 82f);
                    SpawnCryogenicShards();
                    SpawnZone(Projectile.Center, BlackHawkLoadout.Cryogenic, ScaleBaseDamage(0.12f), 150, AttackDirection.ToRotation());
                    break;
                case BlackHawkLoadout.EMP:
                    SpawnBlast(Projectile.damage, 118f);
                    break;
                case BlackHawkLoadout.HolyPayload:
                    SpawnBlast(Projectile.damage, 88f);
                    SpawnHolyShards();
                    break;
                case BlackHawkLoadout.DirtyBomb:
                    SpawnBlast(Projectile.damage, 106f);
                    SpawnDirtyZones();
                    break;
                case BlackHawkLoadout.HeavyBomb:
                    SpawnBlast(Projectile.damage, 205f);
                    break;
            }
        }

        private void SpawnClusterShards()
        {
            Vector2 side = AttackDirection.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 6; i++)
            {
                float across = i - 2.5f;
                Vector2 velocity = AttackDirection * (7.2f + i * 0.72f) + side * across * 1.35f;
                Vector2 spawn = Projectile.Center + AttackDirection * across * 4f + side * across * 3f;
                SpawnChild(spawn, velocity, ModContent.ProjectileType<BlackHawkClusterShard>(), Projectile.damage);
            }

            BlackHawkVFX.SpawnPulse(Projectile.Center, new Color(255, 174, 72), 0.08f, 0.56f, 13,
                new Vector2(1f, 0.52f), AttackDirection.ToRotation());
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.38f, Pitch = 0.32f }, Projectile.Center);
        }

        private void SpawnNapalmZones()
        {
            for (int i = 0; i < 4; i++)
            {
                float along = (i - 1.5f) * 70f;
                SpawnZone(Projectile.Center + AttackDirection * along, BlackHawkLoadout.Napalm,
                    ScaleBaseDamage(0.18f), 180, AttackDirection.ToRotation());
            }
        }

        private void SpawnCryogenicShards()
        {
            float[] angles = { -2.72f, -0.34f, 0f, 0.34f, 2.72f };
            for (int i = 0; i < angles.Length; i++)
            {
                Vector2 velocity = AttackDirection.RotatedBy(angles[i]) * (7.2f + i * 0.48f);
                SpawnChild(Projectile.Center, velocity, ModContent.ProjectileType<BlackHawkCryoShard>(), ScaleBaseDamage(0.30f));
            }
        }

        private void SpawnHolyShards()
        {
            float[] angles = { -0.42f, -0.14f, 0.14f, 0.42f };
            for (int i = 0; i < angles.Length; i++)
            {
                Vector2 velocity = AttackDirection.RotatedBy(angles[i]) * (11.5f + i * 0.55f);
                SpawnChild(Projectile.Center, velocity, ModContent.ProjectileType<BlackHawkHolyShard>(), ScaleBaseDamage(0.40f));
            }
        }

        private void SpawnDirtyZones()
        {
            Vector2 side = AttackDirection.RotatedBy(MathHelper.PiOver2);
            for (int i = -1; i <= 1; i++)
            {
                SpawnZone(Projectile.Center + side * (i * 100f), BlackHawkLoadout.DirtyBomb,
                    ScaleBaseDamage(0.22f), 240, AttackDirection.ToRotation());
            }
        }

        private void SpawnBlast(int damage, float radius)
        {
            int index = Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<BlackHawkCompactBlast>(), damage, Projectile.knockBack,
                Projectile.owner, (float)Loadout, radius);
            PrepareChild(index);
        }

        private void SpawnZone(Vector2 position, BlackHawkLoadout loadout, int damage, int duration, float rotation)
        {
            int index = Projectile.NewProjectile(Projectile.GetSource_Death(), position, Vector2.Zero,
                ModContent.ProjectileType<BlackHawkPersistentZone>(), damage, 0f, Projectile.owner,
                (float)loadout, rotation);
            if (!Main.projectile.IndexInRange(index))
                return;
            Main.projectile[index].timeLeft = duration;
            PrepareChild(index);
        }

        private void SpawnChild(Vector2 position, Vector2 velocity, int type, int damage)
        {
            int index = Projectile.NewProjectile(Projectile.GetSource_Death(), position, velocity, type, damage,
                Projectile.knockBack * 0.35f, Projectile.owner);
            PrepareChild(index);
        }

        private void PrepareChild(int projectileIndex)
        {
            if (!Main.projectile.IndexInRange(projectileIndex))
                return;
            Projectile child = Main.projectile[projectileIndex];
            child.originalDamage = BaseDamage;
            child.CritChance = Projectile.CritChance;
            child.netUpdate = true;
        }

        private int ScaleBaseDamage(float multiplier) => Math.Max(1, (int)Math.Round(BaseDamage * multiplier));

        public override bool PreDraw(ref Color lightColor)
        {
            Color color = BlackHawkLoadoutInfo.Color(Loadout);
            Vector2 direction = AttackDirection;
            float length = Loadout == BlackHawkLoadout.HeavyBomb ? 20f : 13f;
            float width = Loadout == BlackHawkLoadout.HeavyBomb ? 7f : 4f;
            BlackHawkVFX.DrawWorldLine(Projectile.Center - direction * length * 0.5f,
                Projectile.Center + direction * length * 0.5f, Color.Lerp(new Color(56, 60, 65), color, 0.22f), width);
            BlackHawkVFX.DrawBloom(Projectile.Center, color, Loadout == BlackHawkLoadout.HeavyBomb ? 9f : 6f, 0.42f);

            if (Loadout == BlackHawkLoadout.HeavyBomb)
            {
                float pulse = 0.8f + 0.12f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f);
                BlackHawkVFX.DrawRing(Projectile.Center, color, 15f * pulse, 0.32f,
                    Main.GlobalTimeWrappedHourly * 0.8f, new Vector2(1f, 0.62f));
            }
            return false;
        }
    }

    internal sealed class BlackHawkCompactBlast : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BlackHawk";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private BlackHawkLoadout Loadout => BlackHawkLoadoutInfo.Sanitize((int)Projectile.ai[0]);
        private float MaxRadius => Math.Max(24f, Projectile.ai[1]);
        private int Lifetime => Loadout == BlackHawkLoadout.HeavyBomb ? 30 : 22;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 22;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            float progress = MathHelper.Clamp(Projectile.localAI[0] / Lifetime, 0f, 1f);
            float expansion = 1f - (float)Math.Pow(1f - progress, 4f);
            ResizeHitbox(Math.Max(8f, MaxRadius * expansion));
            Projectile.Opacity = MathHelper.Clamp(1f - progress * progress, 0f, 1f);

            if (Projectile.localAI[0] == 1f)
            {
                Color color = BlackHawkLoadoutInfo.Color(Loadout);
                bool heavy = Loadout == BlackHawkLoadout.HeavyBomb;
                BlackHawkVFX.SpawnCompactImpact(Projectile.Center, Vector2.UnitX, color, heavy);
                SoundEngine.PlaySound(SoundID.Item14 with
                {
                    Volume = heavy ? 0.92f : 0.52f,
                    Pitch = heavy ? -0.48f : 0.06f
                }, Projectile.Center);
            }

            Color light = BlackHawkLoadoutInfo.Color(Loadout);
            Lighting.AddLight(Projectile.Center, light.ToVector3() * (0.78f * Projectile.Opacity));
        }

        public override bool? CanDamage()
        {
            return Projectile.localAI[0] >= 2f && Projectile.localAI[0] <= Lifetime - 4f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            switch (Loadout)
            {
                case BlackHawkLoadout.Napalm:
                    target.AddBuff(BuffID.OnFire3, 150);
                    break;
                case BlackHawkLoadout.Cryogenic:
                    target.AddBuff(BuffID.Frostburn2, 150);
                    target.GetGlobalNPC<BlackHawkTargetStatusNPC>().ApplyCryogenic(Projectile.owner, 150);
                    break;
                case BlackHawkLoadout.EMP:
                    target.GetGlobalNPC<BlackHawkTargetStatusNPC>().ApplyEMP(Projectile.owner, 210);
                    break;
                case BlackHawkLoadout.DirtyBomb:
                    target.AddBuff(BuffID.Poisoned, 240);
                    break;
            }
        }

        private void ResizeHitbox(float radius)
        {
            Vector2 center = Projectile.Center;
            int diameter = Math.Max(8, (int)(radius * 2f));
            Projectile.width = diameter;
            Projectile.height = diameter;
            Projectile.Center = center;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = MathHelper.Clamp(Projectile.localAI[0] / Lifetime, 0f, 1f);
            float expansion = 1f - (float)Math.Pow(1f - progress, 4f);
            float radius = MaxRadius * expansion;
            Color color = BlackHawkLoadoutInfo.Color(Loadout);
            float opacity = Projectile.Opacity;

            BlackHawkVFX.DrawBloom(Projectile.Center, color, radius * 0.74f, opacity * 0.34f);
            BlackHawkVFX.DrawRing(Projectile.Center, color, radius, opacity * 0.82f,
                Main.GlobalTimeWrappedHourly * (Loadout == BlackHawkLoadout.EMP ? 1.1f : 0.32f),
                Loadout == BlackHawkLoadout.EMP ? new Vector2(1f, 0.74f) : Vector2.One);

            if (Loadout == BlackHawkLoadout.HeavyBomb)
            {
                BlackHawkVFX.DrawRing(Projectile.Center, new Color(255, 220, 150), radius * 0.66f,
                    opacity * 0.58f, -Main.GlobalTimeWrappedHourly * 0.24f, new Vector2(1f, 0.86f));
                BlackHawkVFX.DrawBloom(Projectile.Center, Color.White, radius * 0.30f, opacity * 0.46f);
            }
            else if (Loadout == BlackHawkLoadout.HolyPayload)
            {
                BlackHawkVFX.DrawWorldLine(Projectile.Center - Vector2.UnitX * radius * 0.62f,
                    Projectile.Center + Vector2.UnitX * radius * 0.62f,
                    BlackHawkVFX.Additive(Color.White) * (opacity * 0.46f), 2f);
                BlackHawkVFX.DrawWorldLine(Projectile.Center - Vector2.UnitY * radius * 0.62f,
                    Projectile.Center + Vector2.UnitY * radius * 0.62f,
                    BlackHawkVFX.Additive(Color.White) * (opacity * 0.46f), 2f);
            }
            return false;
        }
    }

    internal sealed class BlackHawkPersistentZone : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BlackHawk";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private BlackHawkLoadout Loadout => BlackHawkLoadoutInfo.Sanitize((int)Projectile.ai[0]);
        private float AxisRotation => Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 46;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 30;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Vector2 center = Projectile.Center;
            switch (Loadout)
            {
                case BlackHawkLoadout.Cryogenic:
                    Projectile.width = 104;
                    Projectile.height = 62;
                    break;
                case BlackHawkLoadout.DirtyBomb:
                    Projectile.width = 92;
                    Projectile.height = 58;
                    break;
                default:
                    Projectile.width = 64;
                    Projectile.height = 42;
                    break;
            }
            Projectile.Center = center;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            float fadeIn = MathHelper.Clamp(Projectile.localAI[0] / 12f, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / 24f, 0f, 1f);
            Projectile.Opacity = fadeIn * fadeOut;

            Color color = BlackHawkLoadoutInfo.Color(Loadout);
            Lighting.AddLight(Projectile.Center, color.ToVector3() * (0.18f * Projectile.Opacity));
            if (Main.GameUpdateCount % 4 == Projectile.identity % 4)
            {
                Vector2 offset = Main.rand.NextVector2Circular(Projectile.width * 0.30f, Projectile.height * 0.30f)
                    .RotatedBy(AxisRotation);
                Vector2 velocity = Loadout == BlackHawkLoadout.Napalm
                    ? new Vector2(0f, -0.55f)
                    : Main.rand.NextVector2Circular(0.22f, 0.22f);
                BlackHawkVFX.SpawnSmokePoint(Projectile.Center + offset, velocity, color,
                    Loadout == BlackHawkLoadout.DirtyBomb ? new Color(44, 57, 38) : new Color(72, 82, 88),
                    Loadout == BlackHawkLoadout.Cryogenic ? 0.52f : 0.46f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            switch (Loadout)
            {
                case BlackHawkLoadout.Napalm:
                    target.AddBuff(BuffID.OnFire3, 120);
                    break;
                case BlackHawkLoadout.Cryogenic:
                    target.AddBuff(BuffID.Frostburn2, 90);
                    target.GetGlobalNPC<BlackHawkTargetStatusNPC>().ApplyCryogenic(Projectile.owner, 120);
                    break;
                case BlackHawkLoadout.DirtyBomb:
                    target.AddBuff(BuffID.Poisoned, 180);
                    break;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color color = BlackHawkLoadoutInfo.Color(Loadout);
            float pulse = 0.96f + 0.04f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f + Projectile.identity);
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;
            Vector2 scale = new(Projectile.width / (float)bloom.Width, Projectile.height / (float)bloom.Height);
            Vector2 ringScale = new Vector2(Projectile.width / (float)ring.Width, Projectile.height / (float)ring.Height) * pulse;

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null,
                BlackHawkVFX.Additive(color) * (0.23f * Projectile.Opacity), AxisRotation,
                bloom.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ring, Projectile.Center - Main.screenPosition, null,
                BlackHawkVFX.Additive(color) * (0.42f * Projectile.Opacity), AxisRotation,
                ring.Size() * 0.5f, ringScale, SpriteEffects.None, 0f);

            if (Loadout == BlackHawkLoadout.Napalm)
            {
                Vector2 axis = AxisRotation.ToRotationVector2();
                BlackHawkVFX.DrawWorldLine(Projectile.Center - axis * Projectile.width * 0.35f,
                    Projectile.Center + axis * Projectile.width * 0.35f,
                    BlackHawkVFX.Additive(new Color(255, 190, 68)) * (0.46f * Projectile.Opacity), 2.2f);
            }
            return false;
        }
    }
}
