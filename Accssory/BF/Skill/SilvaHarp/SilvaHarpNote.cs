using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SilvaHarp
{
    internal sealed class SilvaHarpNote : ModProjectile
    {
        public const int NoteCount = 5;
        public int Slot => (int)Projectile.ai[0];

        // 魔法竖琴另外两种音符贴图，暂时不用，留在这里方便以后切换：
        // ProjectileID.QuarterNote   -> Terraria/Images/Projectile_76
        // ProjectileID.EighthNote    -> Terraria/Images/Projectile_77
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.TiedEighthNote}";

        public override void SetDefaults()
        {
            // TiedEighthNote 在原版 Projectile.SetDefaults 中就是 22×24。
            Projectile.width = 22;
            Projectile.height = 24;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.alpha = 100;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers))
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || !owner.GetModPlayer<BFAccessoryPlayer>().SilvaHarpEquipped)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                Projectile.rotation = Main.rand.NextFloat() * MathHelper.TwoPi;
            }

            // 以下轨迹逐项对应原版 AI_175_TitaniumStormShards：
            // 固定槽位均分相位，miscCounterNormalized * 6 控制环绕速度；
            // 纵轴压缩到 0.05，形成从玩家身前/身后横向穿过的椭圆轨道。
            Projectile.rotation += MathF.PI / 200f;
            int slot = Utils.Clamp(Slot, 0, NoteCount - 1);
            float phase = (slot / (float)NoteCount + owner.miscCounterNormalized * 6f) * MathHelper.TwoPi;
            float radius = 24f + NoteCount * 6f;

            Vector2 playerMovement = owner.position - owner.oldPosition;
            Projectile.Center += playerMovement;

            Vector2 orbitDirection = phase.ToRotationVector2();
            Projectile.localAI[0] = orbitDirection.Y;
            Vector2 destination = owner.Center + orbitDirection * new Vector2(1f, 0.05f) * radius;
            Projectile.Center = Vector2.Lerp(Projectile.Center, destination, 0.3f);

            // 主体保持原版魔法竖琴贴图，只补一层很轻的 BF 战术色照明。
            Color tacticalColor = BFArrowCommon.GetPresetColor(owner.GetModPlayer<BFAccessoryPlayer>().CurrentPreset);
            Lighting.AddLight(Projectile.Center, tacticalColor.ToVector3() * 0.24f);
        }

        public override void DrawBehind(
            int index,
            List<int> behindNPCsAndTiles,
            List<int> behindNPCs,
            List<int> behindProjectiles,
            List<int> overPlayers,
            List<int> overWiresUI)
        {
            // 原版钛金碎片以轨道 Y 分量决定从玩家身后还是身前经过。
            if (Projectile.localAI[0] <= 0f)
                behindProjectiles.Add(index);
            else
                overPlayers.Add(index);
        }
    }
}
