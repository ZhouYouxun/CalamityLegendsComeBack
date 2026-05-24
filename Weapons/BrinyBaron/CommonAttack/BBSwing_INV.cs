using CalamityLegendsComeBack.Weapons.BrinyBaron.POWER;
using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BBSwing_INV : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int SquareSize => Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : 150;
        private float EncodedSwingScale => Projectile.ai[1] == 0f ? 1f : Projectile.ai[1];
        private float SwingVisualScale => EncodedSwingScale < 0f ? -EncodedSwingScale : EncodedSwingScale;
        private bool AddsTide => EncodedSwingScale < 0f;
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

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (AddsTide)
            {
                Player owner = Main.player[Projectile.owner];
                owner.GetModPlayer<BBEXPlayer>().AddTide();
                owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, 10f);
            }

            Vector2 slashVelocity = SlashAngle.ToRotationVector2();
            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = -slashVelocity.RotatedByRandom(0.74f) * Main.rand.NextFloat(5f, 18f);
                Dust dust = Dust.NewDustPerfect(
                    target.Center,
                    Main.rand.NextBool() ? DustID.Water : DustID.Frost,
                    velocity,
                    0,
                    default,
                    Main.rand.NextFloat(1.05f, 1.65f) * SwingVisualScale);

                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? Color.DeepSkyBlue : Color.Cyan;
            }

            SoundEngine.PlaySound(SoundID.Splash, target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        private void ResizeToSquare(int size)
        {
            if (size < 1)
                size = 1;

            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = size;
            Projectile.Center = center;
        }
    }
}
