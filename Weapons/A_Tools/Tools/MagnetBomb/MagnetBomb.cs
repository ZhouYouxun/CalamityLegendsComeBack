using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Tools.MagnetBomb
{
    public class MagnetBombItem : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Tools/DebugTools/BossProgress/CTRLBoss/CTRLBoss";

        private static int BombType => ModContent.ProjectileType<MagnetBombProj>();

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = BombType;
            Item.shootSpeed = 10f;
            Item.damage = 120;
            Item.knockBack = 4f;
            Item.DamageType = DamageClass.Ranged;
            Item.value = Item.sellPrice(gold: 2);
            Item.rare = ItemRarityID.Orange;
            Item.noUseGraphic = true;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
                return true;
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                // Right click: detonate all bombs
                int bombType = BombType;
                bool anyDetonated = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (!proj.active || proj.owner != player.whoAmI || proj.type != bombType) continue;
                    proj.ai[0] = 1f;
                    proj.netUpdate = true;
                    anyDetonated = true;
                }
                if (anyDetonated)
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.4f, Pitch = -0.3f }, player.Center);
                return false;
            }

            // Left click: throw a bomb
            Vector2 throwVelocity = velocity;
            Projectile.NewProjectile(source, position, throwVelocity, BombType, damage, knockback, player.whoAmI);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = 0.1f }, player.Center);
            return false;
        }
    }

    public class MagnetBombProj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Tools";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Tools/DebugTools/BossProgress/CTRLBoss/CTRLBoss";

        private bool isStuck;

        // ai[0] = 1 when detonated
        private bool ShouldDetonate => Projectile.ai[0] >= 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = int.MaxValue / 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
        }

        public override bool ShouldUpdatePosition() => !isStuck;

        public override void AI()
        {
            if (ShouldDetonate)
            {
                Projectile.Kill();
                return;
            }

            if (!isStuck)
            {
                Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.28f, -20f, 16f);
                Projectile.rotation += Projectile.velocity.X * 0.08f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!isStuck)
            {
                isStuck = true;
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.55f, Pitch = 0.2f }, Projectile.Center);
            }
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            for (int i = 0; i < 25; i++)
            {
                Dust.NewDust(Projectile.Center - new Vector2(20), 40, 40, DustID.Smoke,
                    Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f), 150, default, 1.8f);
            }
            for (int i = 0; i < 12; i++)
            {
                Dust.NewDust(Projectile.Center - new Vector2(16), 32, 32, DustID.Torch,
                    Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-5f, 5f), 0, default, 2.2f);
            }

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 savedCenter = Projectile.Center;
                Projectile.position = savedCenter - new Vector2(90f);
                Projectile.width = 180;
                Projectile.height = 180;
                Projectile.Center = savedCenter;
                Projectile.maxPenetrate = -1;
                Projectile.penetrate = -1;
                Projectile.usesLocalNPCImmunity = true;
                Projectile.localNPCHitCooldown = 10;
                Projectile.Damage();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            float scale = 0.6f;
            Color drawColor = isStuck ? new Color(255, 100, 100) : Color.White;
            drawColor *= Projectile.Opacity;

            // Draw pulsing indicator when stuck
            if (isStuck)
            {
                float pulse = 0.7f + MathF.Sin(Main.GlobalTimeWrappedHourly * 8f) * 0.3f;
                drawColor = new Color(255, (int)(100 * pulse), 0) * pulse;
            }

            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null,
                drawColor, Projectile.rotation, tex.Size() * 0.5f, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
