using CalamityLegendsComeBack.Weapons.GlacialEmbrace.General;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.Passive
{
    public class GlacialWeaponFamiliar : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int AttackInterval = 42;

        private int ClassKind => (int)Projectile.ai[0];
        private ref float AttackTimer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            GlacialEmbracePlayer modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();
            if (!modPlayer.HasGlacialEmbraceInInventory || modPlayer.HeldSupportClass != ClassKind)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Vector2 idleOffset = GetIdleOffset(player);
            Vector2 targetPosition = player.Center + idleOffset;
            Projectile.Center = Vector2.Lerp(Projectile.Center, targetPosition, 0.18f);
            Projectile.rotation = Projectile.rotation.AngleLerp((Main.MouseWorld - Projectile.Center).ToRotation() + MathHelper.PiOver4, 0.12f);

            AttackTimer++;
            NPC target = FindTarget(player, 900f);
            if (target != null)
            {
                Vector2 aim = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX * player.direction);
                Projectile.rotation = Projectile.rotation.AngleLerp(aim.ToRotation() + MathHelper.PiOver4, 0.24f);
                if (AttackTimer >= AttackInterval && Main.myPlayer == Projectile.owner)
                {
                    AttackTimer = 0f;
                    int damage = Math.Max(1, Projectile.damage);
                    float speed = ClassKind == GlacialEmbracePlayer.SupportClassRanged ? 18f : 15f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, aim * speed,
                        ModContent.ProjectileType<GlacialWeaponFamiliarShot>(), damage, Projectile.knockBack, Projectile.owner, ClassKind);
                    SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.42f, Pitch = GetPitch() }, Projectile.Center);
                }
            }

            Lighting.AddLight(Projectile.Center, GetClassColor().ToVector3() * 0.28f);
        }

        private Vector2 GetIdleOffset(Player player)
        {
            float side = player.direction == 0 ? 1f : player.direction;
            float wave = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.6f + ClassKind) * 7f;
            return new Vector2(-side * (68f + ClassKind * 6f), -68f - wave);
        }

        private float GetPitch()
        {
            return ClassKind switch
            {
                GlacialEmbracePlayer.SupportClassMelee => -0.18f,
                GlacialEmbracePlayer.SupportClassRanged => 0.08f,
                GlacialEmbracePlayer.SupportClassMagic => 0.3f,
                _ => 0.18f
            };
        }

        private Color GetClassColor()
        {
            return ClassKind switch
            {
                GlacialEmbracePlayer.SupportClassMelee => new Color(150, 235, 255),
                GlacialEmbracePlayer.SupportClassRanged => new Color(170, 255, 210),
                GlacialEmbracePlayer.SupportClassMagic => new Color(170, 190, 255),
                _ => new Color(230, 245, 255)
            };
        }

        private static NPC FindTarget(Player player, float range)
        {
            if (player.HasMinionAttackTargetNPC)
            {
                NPC marked = Main.npc[player.MinionAttackTargetNPC];
                if (marked.active && marked.CanBeChasedBy())
                    return marked;
            }

            NPC best = null;
            float bestDistance = range * range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.DistanceSquared(player.Center, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = npc;
                }
            }
            return best;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int itemId = ClassKind switch
            {
                GlacialEmbracePlayer.SupportClassMelee => ItemID.IceBlade,
                GlacialEmbracePlayer.SupportClassRanged => ItemID.IceBow,
                GlacialEmbracePlayer.SupportClassMagic => ItemID.IceRod,
                _ => ItemID.IceBoomerang
            };

            Texture2D texture = TextureAssets.Item[itemId].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color color = GetClassColor();
            float scale = 0.72f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f + ClassKind) * 0.03f;

            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * 2.2f;
                Main.EntitySpriteDraw(texture, drawPosition + offset, null, color with { A = 0 } * 0.42f,
                    Projectile.rotation, origin, scale, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White, Projectile.rotation, origin, scale, SpriteEffects.None);
            return false;
        }
    }

    public class GlacialWeaponFamiliarShot : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int ClassKind => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 80;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (ClassKind == GlacialEmbracePlayer.SupportClassMagic)
                Projectile.velocity = Projectile.velocity.RotatedBy((float)Math.Sin(Projectile.timeLeft * 0.18f) * 0.018f);

            Color color = GetClassColor();
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0f, 8f),
                    Main.rand.NextBool() ? DustID.Frost : DustID.Ice,
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.12f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    80, color, Main.rand.NextFloat(0.85f, 1.25f));
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.35f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn2, ClassKind == GlacialEmbracePlayer.SupportClassMelee ? 180 : 120);
        }

        public override void OnKill(int timeLeft)
        {
            Color color = GetClassColor();
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Ice,
                    Main.rand.NextVector2Circular(3f, 3f), 80, color, Main.rand.NextFloat(0.8f, 1.3f));
                dust.noGravity = true;
            }
        }

        private Color GetClassColor()
        {
            return ClassKind switch
            {
                GlacialEmbracePlayer.SupportClassMelee => new Color(150, 235, 255),
                GlacialEmbracePlayer.SupportClassRanged => new Color(170, 255, 210),
                GlacialEmbracePlayer.SupportClassMagic => new Color(170, 190, 255),
                _ => new Color(230, 245, 255)
            };
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.MagicPixel.Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = GetClassColor() with { A = 0 };
            Rectangle frame = new Rectangle(0, 0, 1, 1);
            Vector2 origin = new Vector2(0.5f, 0.5f);
            Vector2 scale = new Vector2(28f, 4f);

            Main.EntitySpriteDraw(texture, drawPosition, frame, color * 0.9f,
                Projectile.rotation, origin, scale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, frame, Color.White with { A = 0 } * 0.45f,
                Projectile.rotation, origin, new Vector2(14f, 2f), SpriteEffects.None);
            return false;
        }
    }
}
