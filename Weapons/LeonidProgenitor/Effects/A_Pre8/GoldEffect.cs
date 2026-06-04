using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8
{
    public class GoldEffect : LeonidMetalEffect
    {
        public override int EffectID => 7;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner || !Main.rand.NextBool(10))
                return;

            SoundEngine.PlaySound(SoundID.Coins with { Volume = 0.55f }, target.Center);
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(target.Center, DustID.GoldCoin, Main.rand.NextVector2Circular(3.6f, 3.6f), 100, Color.White, Main.rand.NextFloat(0.9f, 1.25f));
                dust.noGravity = true;
            }

            int coinCount = Main.rand.Next(1, 4);
            for (int i = 0; i < coinCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(8f, 12f);
                int coin = Projectile.NewProjectile(
                    meteor.Projectile.GetSource_FromThis(),
                    target.Center + Main.rand.NextVector2Circular(10f, 10f),
                    velocity,
                    ProjectileID.GoldCoin, // Coin Gun's gold coin projectile, ID 160.
                    meteor.Projectile.damage / 3,
                    0.5f,
                    meteor.Projectile.owner);

                if (coin >= 0 && coin < Main.maxProjectiles)
                    Main.projectile[coin].DamageType = meteor.Projectile.DamageType;
            }
        }
    }
}
