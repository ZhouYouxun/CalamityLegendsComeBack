using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    public sealed class SeasSearingRadiationBulletGlobal : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool IsRadiationBullet;

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!IsRadiationBullet)
                return;

            Player owner = Main.player[projectile.owner];
            SeasSearingPlayer ssPlayer = owner.GetModPlayer<SeasSearingPlayer>();
            ssPlayer.OnHitWithSeasSearing();

            if (Main.myPlayer == projectile.owner)
                target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, projectile.owner, 3, 8 * 60);

            if (!Main.dedServ)
                SpawnHitRadiationBurst(target.Center);
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(IsRadiationBullet);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            IsRadiationBullet = bitReader.ReadBit();
        }

        private static void SpawnHitRadiationBurst(Vector2 center)
        {
            for (int i = 0; i < 10; i++)
            {
                float angle = MathHelper.TwoPi * i / 10f + Main.rand.NextFloat(-0.15f, 0.15f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(1.8f, 5f);
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.GemEmerald,
                    velocity,
                    120,
                    Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.BiohazardLime, Main.rand.NextFloat(0.2f, 0.8f)),
                    Main.rand.NextFloat(0.55f, 0.95f));
                dust.noGravity = true;
            }
        }
    }
}
