namespace CalamityRangerExpansion.Content.DeveloperItems.Weapon.HD2.LAS17
{
    internal class LAS17 : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "DeveloperItems.LAS17";
        public override void SetDefaults()
        {
            // 设置武器基本属性
            Item.width = 40; // 弓的宽度
            Item.height = 80; // 弓的高度
            Item.damage = 41; // 武器伤害
            Item.DamageType = DamageClass.Ranged; // 伤害类型：远程
            Item.useTime = 5; // 使用时间（5帧）
            Item.useAnimation = 5; // 动画时间（5帧）
            Item.useStyle = ItemUseStyleID.Shoot; // 使用风格：射击
            Item.knockBack = 4; // 击退力
            // Item.UseSound = SoundID.Item5; // 一般情况下他没有使用音效
            Item.shoot = ModContent.ProjectileType<LAS17Hold>(); // 手持弹幕
            Item.shootSpeed = 0f; // 手持弹幕的初始速度为0
            Item.noMelee = true; // 不进行近战攻击
            Item.noUseGraphic = true; // 使用时隐藏物品模型
            Item.channel = true; // 支持长按
            Item.autoReuse = true; // 自动连点
            Item.useAmmo = AmmoID.Bullet;
            Item.Calamity().devItem = true;

            Item.value = CalamityGlobalItem.RarityPinkBuyPrice;
            Item.rare = ItemRarityID.Pink;
        }
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
        public override bool CanConsumeAmmo(Item ammo, Player player) => player.ownedProjectileCounts[Item.shoot] > 0;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 shootVelocity = velocity;
            Vector2 shootDirection = shootVelocity.SafeNormalize(Vector2.UnitX * player.direction);
            Projectile.NewProjectile(source, position, shootDirection, ModContent.ProjectileType<LAS17Hold>(), damage, knockback, player.whoAmI);
            return false;
        }


    }
}
