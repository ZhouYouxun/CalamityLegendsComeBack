using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.PeaShooter
{
    internal enum PeaShooterPeaType
    {
        Normal = 0,
        Electric = 1,
        Fire = 2,
        Ice = 3,
        Starlight = 4,
        Poison = 5,
        Rock = 6
    }

    internal sealed class PeaShooterPea : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/PeaPROJ/豌豆";

        private PeaShooterPeaType PeaType => (PeaShooterPeaType)(int)Projectile.ai[0];
        private int StageIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 360;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation += MathHelper.Clamp(Projectile.velocity.X * 0.018f, -0.26f, 0.26f);
            Color color = GetPeaColor(PeaType);
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.18f);

            if (Projectile.localAI[0]++ <= 2f)
                return;

            SpawnFlightEffects(PeaType);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (PeaType == PeaShooterPeaType.Normal)
                modifiers.SourceDamage *= BalancePeaShooter.NormalDirectDamageMultiplier;

            if (PeaType == PeaShooterPeaType.Rock)
            {
                if (IsBossLike(target))
                    modifiers.Knockback *= 0f;
                else
                    modifiers.Knockback *= BalancePeaShooter.RockKnockbackMultiplier;
            }

            if (IsZombieTarget(target))
            {
                modifiers.FinalDamage *= BalancePeaShooter.ZombieDamageMultiplier;
                SpawnZombieBonusImpact(target.Center);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ApplyDebuffs(target, PeaType);
            ApplySpecialHitEffect(target, damageDone);
            SpawnImpact(withDamage: true);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnImpact(withDamage: true);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.localAI[1] == 0f)
                SpawnImpactVisuals(Projectile.Center, PeaType, 0.65f);
        }

        private void SpawnImpact(bool withDamage)
        {
            if (Projectile.localAI[1] != 0f)
                return;

            Projectile.localAI[1] = 1f;
            SpawnImpactVisuals(Projectile.Center, PeaType, PeaType == PeaShooterPeaType.Rock ? 1.24f : 0.85f);

            if (!withDamage || Main.myPlayer != Projectile.owner)
                return;

            int splashDamage = Math.Max(1, (int)Math.Round(Projectile.damage * BalancePeaShooter.SplashDamageMultiplier));
            int splashIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<PeaShooterSplash>(),
                splashDamage,
                Projectile.knockBack,
                Projectile.owner,
                (float)PeaType);

            if (Main.projectile.IndexInRange(splashIndex))
            {
                Projectile splash = Main.projectile[splashIndex];
                splash.CritChance = Projectile.CritChance;
                splash.originalDamage = splash.damage;
            }
        }

        private void ApplySpecialHitEffect(NPC target, int damageDone)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            switch (PeaType)
            {
                case PeaShooterPeaType.Electric:
                    ReleaseMicroLightning(target);
                    break;

                case PeaShooterPeaType.Fire:
                    if (Main.rand.NextBool(2))
                        SpawnFirePatch();
                    break;

                case PeaShooterPeaType.Ice:
                    if (!IsBossLike(target) && Main.rand.NextFloat() < 0.1f)
                        target.GetGlobalNPC<PeaShooterGlobalNPC>().ApplyFreeze(BalancePeaShooter.IceFreezeTime);
                    break;

                case PeaShooterPeaType.Rock:
                    if (IsBossLike(target) && Main.rand.NextFloat() < 0.05f)
                        target.GetGlobalNPC<PeaShooterGlobalNPC>().ApplyBossStun(BalancePeaShooter.BossStunTime);
                    break;
            }
        }

        private void ReleaseMicroLightning(NPC firstTarget)
        {
            List<NPC> targets = new();
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.whoAmI == firstTarget.whoAmI || !npc.CanBeChasedBy(Projectile))
                    continue;

                if (Vector2.DistanceSquared(npc.Center, Projectile.Center) <= 430f * 430f)
                    targets.Add(npc);
            }

            int lightningCount = BalancePeaShooter.ElectricLightningCount;
            int damage = Math.Max(1, (int)Math.Round(Projectile.damage * BalancePeaShooter.ElectricLightningDamageMultiplier));
            for (int i = 0; i < lightningCount; i++)
            {
                Vector2 direction;
                if (i < targets.Count)
                    direction = Projectile.Center.DirectionTo(targets[i].Center);
                else
                    direction = Main.rand.NextVector2CircularEdge(1f, 1f);

                int lightningIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    direction.RotatedByRandom(0.16f) * 18f,
                    ModContent.ProjectileType<PeaShooterLightning>(),
                    damage,
                    Projectile.knockBack * 0.25f,
                    Projectile.owner,
                    0f,
                    0.2f);

                if (Main.projectile.IndexInRange(lightningIndex))
                {
                    Projectile lightning = Main.projectile[lightningIndex];
                    lightning.CritChance = Projectile.CritChance;
                    lightning.originalDamage = lightning.damage;
                }
            }
        }

        private void SpawnFirePatch()
        {
            int damage = Math.Max(1, (int)Math.Round(Projectile.damage * BalancePeaShooter.FirePatchDamageMultiplier));
            int fireIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<PeaShooterFirePatch>(),
                damage,
                0f,
                Projectile.owner);

            if (Main.projectile.IndexInRange(fireIndex))
            {
                Projectile patch = Main.projectile[fireIndex];
                patch.CritChance = Projectile.CritChance;
                patch.originalDamage = patch.damage;
            }
        }

        private void SpawnFlightEffects(PeaShooterPeaType peaType)
        {
            switch (peaType)
            {
                case PeaShooterPeaType.Electric:
                    SpawnElectricFlightEffects();
                    break;

                case PeaShooterPeaType.Fire:
                    SpawnFireFlightEffects();
                    break;

                case PeaShooterPeaType.Ice:
                    SpawnIceFlightEffects();
                    break;

                case PeaShooterPeaType.Starlight:
                    SpawnStarlightFlightEffects();
                    break;

                case PeaShooterPeaType.Poison:
                    SpawnPoisonFlightEffects();
                    break;

                default:
                    SpawnSimpleFlightDust(peaType);
                    break;
            }
        }

        private void SpawnSimpleFlightDust(PeaShooterPeaType peaType)
        {
            if (!Main.rand.NextBool(peaType == PeaShooterPeaType.Rock ? 4 : 2))
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center - direction * Main.rand.NextFloat(2f, 8f) + Main.rand.NextVector2Circular(1.8f, 1.8f),
                GetDustType(peaType),
                -direction.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.35f, 1.45f),
                110,
                Color.Lerp(GetPeaColor(peaType), Color.White, Main.rand.NextFloat(0.04f, 0.22f)),
                Main.rand.NextFloat(0.42f, 0.82f));
            dust.noGravity = peaType != PeaShooterPeaType.Rock;
        }

        private void SpawnElectricFlightEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color usedColor = Main.rand.NextBool(4) ? Color.Orchid : new Color(80, 220, 255);

            if (Main.rand.NextBool(3))
            {
                Particle bolt = new BoltParticle(
                    Projectile.Center,
                    -direction * Main.rand.NextFloat(0.12f, 0.45f),
                    false,
                    18,
                    Main.rand.NextFloat(0.055f, 0.09f),
                    usedColor,
                    new Vector2(0.36f, 0.16f),
                    true,
                    true,
                    false,
                    0.14f);
                GeneralParticleHandler.SpawnParticle(bolt);
            }

            if (Main.rand.NextBool(16))
            {
                Particle spark = new CustomSpark(
                    Projectile.Center,
                    direction.RotatedByRandom(0.8f) * Main.rand.NextFloat(-0.4f, 0.4f),
                    "CalamityMod/Particles/DrainLineBloom",
                    false,
                    34,
                    Main.rand.NextFloat(0.18f, 0.26f),
                    usedColor,
                    new Vector2(0.18f, 0.62f),
                    true,
                    true);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private void SpawnFireFlightEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float roll = Main.rand.NextFloat();
            if (roll < 0.2f)
            {
                Particle smoke = new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                    -direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.25f, 0.9f),
                    Color.Lerp(new Color(255, 55, 35), Color.DarkRed, 0.35f),
                    18,
                    Main.rand.NextFloat(0.18f, 0.32f),
                    0.42f,
                    Main.rand.NextFloat(-0.03f, 0.03f),
                    true);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
            else if (roll < 0.5f)
            {
                ParticleOrchestrator.RequestParticleSpawn(
                    clientOnly: false,
                    ParticleOrchestraType.FlameWaders,
                    new ParticleOrchestraSettings { PositionInWorld = Projectile.Center },
                    Projectile.owner);
            }
            else
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? DustID.Torch : DustID.Smoke,
                    -direction.RotatedByRandom(0.34f) * Main.rand.NextFloat(0.7f, 1.9f),
                    120,
                    new Color(255, 100, 42),
                    Main.rand.NextFloat(0.45f, 0.82f));
                dust.noGravity = Main.rand.NextBool();

                if (Main.rand.NextBool(2))
                {
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                        -direction.RotatedByRandom(0.42f) * Main.rand.NextFloat(0.6f, 1.8f),
                        false,
                        Main.rand.Next(12, 20),
                        Main.rand.NextFloat(0.18f, 0.32f),
                        new Color(255, 132, 54)));
                }
            }
        }

        private void SpawnIceFlightEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (Main.rand.NextBool(2))
            {
                Particle mist = new MediumMistParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    -direction.RotatedByRandom(0.42f) * Main.rand.NextFloat(0.45f, 1.2f) + new Vector2(0f, -0.35f),
                    Color.White,
                    Color.Transparent,
                    Main.rand.NextFloat(0.12f, 0.22f),
                    Main.rand.NextFloat(80f, 130f));
                GeneralParticleHandler.SpawnParticle(mist);
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? DustID.IceTorch : DustID.Snow,
                    -direction.RotatedByRandom(0.22f) * Main.rand.NextFloat(0.25f, 1.1f),
                    120,
                    new Color(156, 232, 255),
                    Main.rand.NextFloat(0.45f, 0.78f));
                dust.noGravity = true;
            }
        }

        private void SpawnStarlightFlightEffects()
        {
            if (Main.rand.NextBool(2))
                CLCBLightingBoltsSystem.Spawn_PeaShooterBlueStars(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), 0.42f);

            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    -Projectile.velocity * Main.rand.NextFloat(0.02f, 0.08f),
                    false,
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.26f, 0.48f),
                    Main.rand.NextBool() ? new Color(102, 214, 255) : Color.White));
            }
        }

        private void SpawnPoisonFlightEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                Main.rand.NextBool(3) ? DustID.Poisoned : DustID.GreenTorch,
                -direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.45f, 1.35f),
                115,
                Main.rand.NextBool() ? new Color(102, 232, 74) : new Color(180, 255, 88),
                Main.rand.NextFloat(0.5f, 0.86f));
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(GetTexturePath(PeaType)).Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color peaColor = GetPeaColor(PeaType);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color afterimageColor = Color.Lerp(peaColor, Color.White, 0.18f) * (0.06f + completion * 0.26f);
                afterimageColor.A = 0;
                Main.EntitySpriteDraw(texture, drawPosition, null, afterimageColor, Projectile.oldRot[i], origin, Projectile.scale * (0.72f + completion * 0.18f), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        internal static void ApplyDebuffs(NPC target, PeaShooterPeaType peaType)
        {
            switch (peaType)
            {
                case PeaShooterPeaType.Electric:
                    target.AddBuff(BuffID.Electrified, BalancePeaShooter.DebuffDuration);
                    break;

                case PeaShooterPeaType.Fire:
                    target.AddBuff(BuffID.OnFire, BalancePeaShooter.DebuffDuration);
                    target.AddBuff(BuffID.OnFire3, BalancePeaShooter.DebuffDuration);
                    break;

                case PeaShooterPeaType.Ice:
                    target.AddBuff(BuffID.Frostburn, BalancePeaShooter.DebuffDuration);
                    target.AddBuff(BuffID.Frostburn2, BalancePeaShooter.DebuffDuration);
                    target.AddBuff(BuffID.Chilled, BalancePeaShooter.DebuffDuration / 2);
                    break;

                case PeaShooterPeaType.Starlight:
                    TryAddCalamityBuff(target, "AstralInfectionDebuff", BalancePeaShooter.DebuffDuration);
                    break;

                case PeaShooterPeaType.Poison:
                    ApplyRandomPoisonDebuffs(target);
                    break;
            }
        }

        internal static void SpawnImpactVisuals(Vector2 center, PeaShooterPeaType peaType, float scale)
        {
            Color color = GetPeaColor(peaType);
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.18f + scale * 0.06f, Pitch = peaType == PeaShooterPeaType.Rock ? -0.28f : 0.18f }, center);

            if (peaType == PeaShooterPeaType.Starlight)
                CLCBLightingBoltsSystem.Spawn_PeaShooterBlueStars(center, Vector2.Zero, 1.25f * scale);

            int dustCount = peaType == PeaShooterPeaType.Rock ? 18 : 10;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, peaType == PeaShooterPeaType.Rock ? 5.6f : 3.8f);
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(4f, 4f),
                    GetDustType(peaType),
                    velocity,
                    100,
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.05f, 0.34f)),
                    Main.rand.NextFloat(0.72f, 1.22f) * scale);
                dust.noGravity = peaType != PeaShooterPeaType.Rock;
            }
        }

        internal static Color GetPeaColor(PeaShooterPeaType peaType) => peaType switch
        {
            PeaShooterPeaType.Electric => new Color(116, 220, 255),
            PeaShooterPeaType.Fire => new Color(255, 112, 48),
            PeaShooterPeaType.Ice => new Color(126, 224, 255),
            PeaShooterPeaType.Starlight => new Color(98, 176, 255),
            PeaShooterPeaType.Poison => new Color(124, 232, 74),
            PeaShooterPeaType.Rock => new Color(164, 148, 118),
            _ => new Color(126, 238, 92)
        };

        internal static int GetDustType(PeaShooterPeaType peaType) => peaType switch
        {
            PeaShooterPeaType.Electric => DustID.Electric,
            PeaShooterPeaType.Fire => DustID.Torch,
            PeaShooterPeaType.Ice => DustID.IceTorch,
            PeaShooterPeaType.Starlight => DustID.BlueTorch,
            PeaShooterPeaType.Poison => DustID.GreenTorch,
            PeaShooterPeaType.Rock => DustID.Stone,
            _ => DustID.GrassBlades
        };

        private static string GetTexturePath(PeaShooterPeaType peaType) => peaType switch
        {
            PeaShooterPeaType.Electric => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/PeaPROJ/电光豌豆",
            PeaShooterPeaType.Fire => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/PeaPROJ/火焰豌豆",
            PeaShooterPeaType.Ice => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/PeaPROJ/寒冰豌豆",
            PeaShooterPeaType.Starlight => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/PeaPROJ/星光豌豆",
            PeaShooterPeaType.Poison => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/PeaPROJ/毒性豌豆",
            PeaShooterPeaType.Rock => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/PeaPROJ/岩石豌豆",
            _ => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/PeaPROJ/豌豆"
        };

        private static void ApplyRandomPoisonDebuffs(NPC target)
        {
            AddOnePoisonDebuff(target);
            if (Main.rand.NextFloat() < 0.25f)
                AddOnePoisonDebuff(target);
        }

        private static void AddOnePoisonDebuff(NPC target)
        {
            string[] calamityBuffs =
            {
                "AbsorberAffliction",
                "AcidVenom",
                "AstralInfectionDebuff",
                "BrainRot",
                "BurningBlood",
                "Plague",
                "SagePoison",
                "SulphuricPoisoning",
                "WhisperingDeath"
            };

            int choice = Main.rand.Next(calamityBuffs.Length + 1);
            if (choice == calamityBuffs.Length)
                target.AddBuff(BuffID.Poisoned, BalancePeaShooter.DebuffDuration);
            else
                TryAddCalamityBuff(target, calamityBuffs[choice], BalancePeaShooter.DebuffDuration);
        }

        private static void TryAddCalamityBuff(NPC target, string buffName, int duration)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamityMod) &&
                calamityMod.TryFind(buffName, out ModBuff buff))
            {
                target.AddBuff(buff.Type, duration);
            }
        }

        private static bool IsBossLike(NPC npc)
        {
            return npc.boss || npc.realLife >= 0 || NPCID.Sets.ShouldBeCountedAsBoss[npc.type];
        }

        private static bool IsZombieTarget(NPC npc)
        {
            string internalName = npc.ModNPC?.Name ?? NPCID.Search.GetName(npc.type);
            if (!string.IsNullOrEmpty(internalName) && internalName.Contains("Zombie", StringComparison.OrdinalIgnoreCase))
                return true;

            string typeName = npc.TypeName;
            return !string.IsNullOrEmpty(typeName) && typeName.Contains("Zombie", StringComparison.OrdinalIgnoreCase);
        }

        private static void SpawnZombieBonusImpact(Vector2 center)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new ImpactParticle(center, 0.1f, 18, 0.55f, new Color(124, 238, 92)));
        }
    }

    internal sealed class PeaShooterSplash : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private PeaShooterPeaType PeaType => (PeaShooterPeaType)(int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] != 0f)
                return;

            Projectile.localAI[0] = 1f;
            Vector2 center = Projectile.Center;
            Projectile.Resize(BalancePeaShooter.SplashRadius * 2, BalancePeaShooter.SplashRadius * 2);
            Projectile.Center = center;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = (Projectile.Center.X < target.Center.X).ToDirectionInt();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            PeaShooterPea.ApplyDebuffs(target, PeaType);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    internal sealed class PeaShooterLightning : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float BounceCount => ref Projectile.localAI[0];
        private float sizeMult = 0.2f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 72;
            Projectile.extraUpdates = 18;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            if (Projectile.ai[1] > 0f)
                sizeMult = Projectile.ai[1];

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Color(80, 220, 255).ToVector3() * 0.11f);

            if (Projectile.timeLeft % 4 == 0)
            {
                Particle bolt = new BoltParticle(
                    Projectile.Center,
                    -Projectile.velocity * 0.05f,
                    false,
                    20,
                    0.6f * sizeMult,
                    Main.rand.NextBool(4) ? Color.Orchid : Color.Cyan,
                    new Vector2(1.8f, 0.8f) * sizeMult,
                    true,
                    true,
                    false,
                    0.3f * sizeMult);
                GeneralParticleHandler.SpawnParticle(bolt);
            }

            if (Main.rand.NextBool(24))
            {
                Particle sideBolt = new BoltParticle(
                    Projectile.Center,
                    Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.3f, 1.9f),
                    false,
                    16,
                    Main.rand.NextFloat(0.04f, 0.055f),
                    Color.Cyan,
                    new Vector2(0.36f, 0.16f),
                    true,
                    true,
                    false,
                    0.06f);
                GeneralParticleHandler.SpawnParticle(sideBolt);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= Utils.Remap(Projectile.numHits, 0f, 3f, 1f, 0.2f, true);
            modifiers.Knockback *= 0f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, BalancePeaShooter.DebuffDuration);

            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(new BoltParticle(
                    target.Center,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.7f, 2.2f),
                    true,
                    12,
                    Main.rand.NextFloat(0.025f, 0.04f),
                    Main.rand.NextBool(4) ? Color.Orchid : Color.Cyan,
                    new Vector2(0.36f, 0.16f),
                    true,
                    true,
                    false,
                    0.12f));
            }

            if (BounceCount >= 1f)
            {
                Projectile.Kill();
                return;
            }

            BounceCount++;
            NPC next = FindBounceTarget(target);
            if (next is null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = target.Center;
            Projectile.velocity = target.Center.DirectionTo(next.Center) * MathHelper.Max(Projectile.velocity.Length(), 16f);
            Projectile.timeLeft = Math.Min(Projectile.timeLeft, 48);
            Projectile.netUpdate = true;
        }

        private NPC FindBounceTarget(NPC previousTarget)
        {
            NPC bestTarget = null;
            float bestDistance = 320f * 320f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.whoAmI == previousTarget.whoAmI || !npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = npc;
                }
            }

            return bestTarget;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center - Projectile.velocity,
                Projectile.Center,
                8f,
                ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2? previous = null;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 current = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                if (previous.HasValue)
                {
                    float fade = 1f - i / (float)Projectile.oldPos.Length;
                    DrawSegment(pixel, previous.Value, current, new Color(80, 220, 255) * fade, 1.2f * fade);
                    DrawSegment(pixel, previous.Value, current, Color.White * 0.5f * fade, 0.45f * fade);
                }
                previous = current;
            }

            return false;
        }

        private static void DrawSegment(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 0.001f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                edge.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(edge.Length(), width),
                SpriteEffects.None);
        }
    }

    internal sealed class PeaShooterFirePatch : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = BalancePeaShooter.FirePatchSize;
            Projectile.height = BalancePeaShooter.FirePatchSize;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BalancePeaShooter.FirePatchLifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.12f, 0.04f) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true));

            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.45f, Projectile.height * 0.45f);
                Dust dust = Dust.NewDustPerfect(
                    pos,
                    Main.rand.NextBool() ? DustID.Torch : DustID.Smoke,
                    new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-2.2f, -0.5f)),
                    120,
                    new Color(255, 96, 42),
                    Main.rand.NextFloat(0.62f, 1.05f));
                dust.noGravity = Main.rand.NextBool(3);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, BalancePeaShooter.DebuffDuration);
            target.AddBuff(BuffID.OnFire3, BalancePeaShooter.DebuffDuration);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    internal sealed class PeaShooterGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private int freezeTimer;
        private int bossStunTimer;

        public void ApplyFreeze(int time)
        {
            freezeTimer = Math.Max(freezeTimer, time);
        }

        public void ApplyBossStun(int time)
        {
            bossStunTimer = Math.Max(bossStunTimer, time);
        }

        public override bool PreAI(NPC npc)
        {
            bool frozen = freezeTimer > 0 || bossStunTimer > 0;
            if (freezeTimer > 0)
                freezeTimer--;

            if (bossStunTimer > 0)
                bossStunTimer--;

            if (!frozen)
                return true;

            npc.velocity = Vector2.Zero;
            npc.netUpdate = true;
            return false;
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (freezeTimer > 0)
            {
                drawColor = Color.Lerp(drawColor, new Color(140, 225, 255), 0.48f);
                Lighting.AddLight(npc.Center, new Vector3(0.05f, 0.18f, 0.28f));
            }
            else if (bossStunTimer > 0)
            {
                drawColor = Color.Lerp(drawColor, new Color(190, 170, 130), 0.35f);
                Lighting.AddLight(npc.Center, new Vector3(0.16f, 0.12f, 0.04f));
            }
        }
    }
}
