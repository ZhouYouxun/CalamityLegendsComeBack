using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.PeaShooter
{
    internal enum PeaShooterPeaType
    {
        Normal = 0,
        Fire = 1,
        Ice = 2,
        Poison = 3,
        Electric = 4,
        Rock = 5
    }

    internal sealed class PeaShooterPea : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/豌豆";

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

            if (Projectile.localAI[0]++ > 2f)
                SpawnFlightDust(PeaType);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ApplyDebuffs(target, PeaType, StageIndex);
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

            int splashDamage = Math.Max(1, (int)Math.Round(Projectile.damage * BalancePeaShooter.GetSplashDamageMultiplier(PeaType)));
            int splashIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<PeaShooterSplash>(),
                splashDamage,
                Projectile.knockBack * BalancePeaShooter.GetKnockbackMultiplier(PeaType),
                Projectile.owner,
                (float)PeaType,
                StageIndex);

            if (Main.projectile.IndexInRange(splashIndex))
            {
                Projectile splash = Main.projectile[splashIndex];
                splash.CritChance = Projectile.CritChance;
                splash.originalDamage = splash.damage;
            }

            if (PeaType != PeaShooterPeaType.Electric)
                return;

            int cloudDamage = Math.Max(1, (int)Math.Round(Projectile.damage * BalancePeaShooter.ElectricCloudDamageMultiplier));
            int cloudIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<PeaShooterElectricCloud>(),
                cloudDamage,
                Projectile.knockBack * 0.3f,
                Projectile.owner,
                (float)PeaType,
                StageIndex);

            if (Main.projectile.IndexInRange(cloudIndex))
            {
                Projectile cloud = Main.projectile[cloudIndex];
                cloud.CritChance = Projectile.CritChance;
                cloud.originalDamage = cloud.damage;
            }
        }

        private void SpawnFlightDust(PeaShooterPeaType peaType)
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

        internal static void ApplyDebuffs(NPC target, PeaShooterPeaType peaType, int stageIndex)
        {
            int duration = BalancePeaShooter.GetDebuffDuration(stageIndex);
            switch (peaType)
            {
                case PeaShooterPeaType.Fire:
                    target.AddBuff(BuffID.OnFire, duration);
                    target.AddBuff(BuffID.OnFire3, duration);
                    break;

                case PeaShooterPeaType.Ice:
                    target.AddBuff(BuffID.Frostburn, duration);
                    target.AddBuff(BuffID.Frostburn2, duration);
                    break;

                case PeaShooterPeaType.Poison:
                    target.AddBuff(BuffID.Poisoned, duration);
                    TryAddCalamityBuff(target, "AcidVenom", duration);
                    break;

                case PeaShooterPeaType.Electric:
                    target.AddBuff(BuffID.Electrified, duration);
                    break;
            }
        }

        internal static void SpawnImpactVisuals(Vector2 center, PeaShooterPeaType peaType, float scale)
        {
            Color color = GetPeaColor(peaType);
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.18f + scale * 0.06f, Pitch = peaType == PeaShooterPeaType.Rock ? -0.28f : 0.18f }, center);

            int dustCount = peaType == PeaShooterPeaType.Rock ? 16 : 10;
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
            PeaShooterPeaType.Fire => new Color(255, 112, 48),
            PeaShooterPeaType.Ice => new Color(126, 224, 255),
            PeaShooterPeaType.Poison => new Color(124, 232, 74),
            PeaShooterPeaType.Electric => new Color(116, 220, 255),
            PeaShooterPeaType.Rock => new Color(164, 148, 118),
            _ => new Color(126, 238, 92)
        };

        internal static int GetDustType(PeaShooterPeaType peaType) => peaType switch
        {
            PeaShooterPeaType.Fire => DustID.Torch,
            PeaShooterPeaType.Ice => DustID.IceTorch,
            PeaShooterPeaType.Poison => DustID.GreenTorch,
            PeaShooterPeaType.Electric => DustID.Electric,
            PeaShooterPeaType.Rock => DustID.Stone,
            _ => DustID.GrassBlades
        };

        private static string GetTexturePath(PeaShooterPeaType peaType) => peaType switch
        {
            PeaShooterPeaType.Fire => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/火焰豌豆",
            PeaShooterPeaType.Ice => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/寒冰豌豆",
            PeaShooterPeaType.Poison => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/毒性豌豆",
            PeaShooterPeaType.Electric => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/电光豌豆",
            PeaShooterPeaType.Rock => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/岩石豌豆",
            _ => "CalamityLegendsComeBack/Weapons/A_Dev/PeaShooter/豌豆"
        };

        private static void TryAddCalamityBuff(NPC target, string buffName, int duration)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod calamityMod) &&
                calamityMod.TryFind(buffName, out ModBuff buff))
            {
                target.AddBuff(buff.Type, duration);
            }
        }
    }
}
