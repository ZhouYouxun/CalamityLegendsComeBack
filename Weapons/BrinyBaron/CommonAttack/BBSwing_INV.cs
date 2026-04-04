using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.BrinyBaron.POWER;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BBSwing_INV : ModProjectile, ILocalizedModType
    {
        private const float SmallSlashScale = 0.41f;
        private const float GiantSlashScale = 0.775f;
        private const float SmallSlashDamageFactor = 0.28f;
        private const float GiantSlashDamageFactor = 0.34f;

        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int SquareSize => Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : 150;
        private float EncodedSwingScale => Projectile.ai[1] == 0f ? 1f : Projectile.ai[1];
        private float SwingVisualScale => EncodedSwingScale < 0f ? -EncodedSwingScale : EncodedSwingScale;
        private bool IsGiantSwing => EncodedSwingScale < 0f;
        private float SlashAngle => Projectile.ai[2];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 150;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override void OnSpawn(IEntitySource source)
        {
            ResizeToSquare(SquareSize);
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = SlashAngle;
        }

        private void ResizeToSquare(int size)
        {
            if (size < 1)
                size = 1;

            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = size;
            Projectile.Center = center;
        }

        private void SpawnHitSlashBurst(NPC target)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            if (IsGiantSwing)
            {
                float[] angleOffsets =
                {
                    MathHelper.ToRadians(-20f),
                    0f,
                    MathHelper.ToRadians(20f)
                };

                for (int i = 0; i < angleOffsets.Length; i++)
                {
                    Vector2 slashDirection = SlashAngle.ToRotationVector2().RotatedBy(angleOffsets[i]);
                    Vector2 spawnOffset = slashDirection.RotatedBy(MathHelper.PiOver2) * ((i - 1) * 10f * SwingVisualScale);

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        target.Center + spawnOffset,
                        slashDirection * (6.5f + i * 0.6f),
                        ModContent.ProjectileType<BBSwing_Slash>(),
                        Math.Max(1, (int)(Projectile.damage * GiantSlashDamageFactor)),
                        Projectile.knockBack,
                        Projectile.owner,
                        GiantSlashScale * SwingVisualScale,
                        angleOffsets[i]
                    );
                }

                return;
            }

            Vector2 smallSlashDirection = SlashAngle.ToRotationVector2();
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                smallSlashDirection * 6f,
                ModContent.ProjectileType<BBSwing_Slash>(),
                Math.Max(1, (int)(Projectile.damage * SmallSlashDamageFactor)),
                Projectile.knockBack,
                Projectile.owner,
                SmallSlashScale * SwingVisualScale,
                0f
            );
        }

        private void SpawnGiantLaserRain(NPC target)
        {
            if (!IsGiantSwing || Main.myPlayer != Projectile.owner)
                return;

            const int laserCount = 3;
            const float laserSpeed = 18f;
            const float laserDamageFactor = 0.5f;

            for (int i = 0; i < laserCount; i++)
            {
                Vector2 impactPoint = target.Top + new Vector2(
                    Main.rand.NextFloat(-26f, 26f),
                    Main.rand.NextFloat(-18f, 10f));

                Vector2 spawnPoint = target.Top + new Vector2(
                    Main.rand.NextFloat(-110f, 110f),
                    Main.rand.NextFloat(-260f, -160f));

                Vector2 velocity = (impactPoint - spawnPoint).SafeNormalize(Vector2.UnitY) * laserSpeed;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPoint,
                    velocity,
                    ModContent.ProjectileType<BBShuriken_Lazer>(),
                    Math.Max(1, (int)(Projectile.damage * laserDamageFactor)),
                    Projectile.knockBack * 0.65f,
                    Projectile.owner);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsGiantSwing)
                Main.player[Projectile.owner].GetModPlayer<BBEXPlayer>().AddTide();

            for (int i = 0; i < 6; i++)
            {
                Vector2 vel = SlashAngle.ToRotationVector2()
                    .RotatedByRandom(0.7f) * Main.rand.NextFloat(7f, 20f) * -1f;

                Dust dust = Dust.NewDustPerfect(
                    target.Center,
                    Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                    vel,
                    0,
                    default,
                    Main.rand.NextFloat(1.15f, 1.8f) * SwingVisualScale
                );
                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan;
            }

            SpawnGiantLaserRain(target);
            SpawnHitSlashBurst(target);
            SoundEngine.PlaySound(SoundID.Splash, target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
