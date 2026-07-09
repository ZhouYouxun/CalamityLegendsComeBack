# Dragoon Drizzlefish Food Taxonomy

This is a design whitelist for the Dragoon Drizzlefish feeding rewrite. It is not implementation code.

Source boundary:
- Vanilla food list follows the current Terraria wiki.gg `Food`, `Category:Food items`, `Fishing foods`, and `Item IDs` pages checked on 2026-07-09.
- Calamity food list follows the local checkout at `C:\Users\wangk\Documents\My Games\Terraria\tModLoader\ModSources\CalamityMod\Items\Potions\Food` and `...\Items\Potions\Alcohol`.
- A non-gel item must be actually consumable by the player to be accepted. Food-looking weapons, hooks, boss summons, crafting materials, and joke names do not count.
- `Apple Pie Slice` is excluded because the wiki marks it unobtainable/unimplemented and not actually consumable.
- Gel is a special fuel exception and is allowed even though it is not food.

## Categories

| Category | Meaning |
|---|---|
| Gel | Special slime fuel. Includes only normal gel and pink gel. |
| Fruit | Raw fruit and tree-shake fruit, including Calamity's fruit/plant food drops. |
| Meat | Simple land-animal or creature protein, such as bacon, steak, egg, roast bird, and similar one-piece meat foods. |
| Fish | Direct fish/seafood foods from water or fishing, including cooked fish, sashimi, oyster, lobster tail, and single seafood items. `Seafood Dinner` is not here because it is a full meal. |
| Alcohol | Ale/sake/beer/wine/cocktail/liquor style consumables, especially Calamity alcohols. |
| Feast | Processed or composed meals: soups, pies, sandwiches, noodles, pizza, dinner plates, Calamity's baguette/donut/stews/sandwich/etc. |
| Snack | Small sweets, cookies, chips, soft drinks, coffee/tea, smoothies, juice, milk, and similar light refreshments. |
| Superfood | `Golden Delight`, because it is its own top-tier golden critter food. |
| Unique | `OddMushroom`, because its Calamity effect and flavor are too abstract for the normal categories. |

## Gel

| Chinese name | English internal name | Source | Notes |
|---|---|---|---|
| 凝胶 | `Gel` | Vanilla | Special fuel exception. |
| 粉凝胶 / 粉红凝胶 | `PinkGel` | Vanilla | Special fuel exception. |

## Fruit

| Chinese name | English internal name | Source | Notes |
|---|---|---|---|
| 苹果 | `Apple` | Vanilla | Raw fruit. |
| 杏 | `Apricot` | Vanilla | Raw fruit. |
| 香蕉 | `Banana` | Vanilla | Raw fruit. |
| 黑加仑 | `Blackcurrant` | Vanilla | Raw fruit. |
| 血橙 | `BloodOrange` | Vanilla | Raw fruit. |
| 樱桃 | `Cherry` | Vanilla | Raw fruit. |
| 椰子 | `Coconut` | Vanilla | Raw fruit. |
| 火龙果 | `Dragonfruit` | Vanilla | Raw fruit. |
| 接骨木莓 | `Elderberry` | Vanilla | Raw fruit. |
| 葡萄柚 | `Grapefruit` | Vanilla | Raw fruit. |
| 葡萄 | `Grapes` | Vanilla | Raw fruit/drop fruit. |
| 柠檬 | `Lemon` | Vanilla | Raw fruit. |
| 芒果 | `Mango` | Vanilla | Raw fruit. |
| 桃子 | `Peach` | Vanilla | Raw fruit. |
| 菠萝 | `Pineapple` | Vanilla | Raw fruit. |
| 李子 | `Plum` | Vanilla | Raw fruit. |
| 石榴 | `Pomegranate` | Vanilla | Raw fruit. |
| 红毛丹 | `Rambutan` | Vanilla | Raw fruit. |
| 杨桃 | `Starfruit` | Vanilla | Raw fruit. |
| 小檗果 | `Barberry` | Calamity | Calamity fruit food. |
| 彗星果 | `Cometfruit` | Calamity | Calamity fruit food. |
| 菠萝蜜 | `Jackfruit` | Calamity | Calamity fruit food. |
| 莲花 | `Lotus` | Calamity | Plant/flower food; grouped here because there is no separate plant category. |
| 山竹 | `Mangosteen` | Calamity | Calamity fruit food. |
| 沙拉克 / 蛇皮果 | `Salak` | Calamity | Calamity fruit food. |

## Meat

| Chinese name | English internal name | Source | Notes |
|---|---|---|---|
| 培根 | `Bacon` | Vanilla | Simple meat drop. |
| 鸡块 | `ChickenNugget` | Vanilla | Simple meat food. |
| 煎蛋 | `FriedEgg` | Vanilla | Creature protein; keep with meat. |
| 烤松鼠 | `GrilledSquirrel` | Vanilla | Simple land-animal meat. |
| 烤鸟 | `RoastedBird` | Vanilla | Simple land/bird meat. |
| 烤鸭 | `RoastedDuck` | Vanilla | Simple bird meat. |
| 牛排 | `Steak` | Vanilla | Simple meat drop. |

## Fish

| Chinese name | English internal name | Source | Notes |
|---|---|---|---|
| 熏黑鱼 / 黑鱼料理 | `BlackenedFish` | Vanilla | Fish food. |
| 熟鱼 | `CookedFish` | Vanilla | Fishing food. |
| 熟虾 | `CookedShrimp` | Vanilla | Fishing food. |
| 龙虾尾 | `LobsterTail` | Vanilla | Fishing food; single seafood item, not a full dinner. |
| 生鱼片 | `Sashimi` | Vanilla | Raw fish food. |
| 去壳牡蛎 | `ShuckedOyster` | Vanilla | Fishing/oyster food. |
| 薯条 | `Fries` | Vanilla | Dropped by Flying Fish; mechanically fish-associated, but visually snack-like. Put here only if we want all water-source drops in fish. If visual taxonomy wins, move to Snack. |

## Alcohol

| Chinese name | English internal name | Source | Notes |
|---|---|---|---|
| 麦芽酒 | `Ale` | Vanilla | Vanilla alcohol; grants Tipsy rather than fed buffs. |
| 清酒 | `Sake` | Vanilla | Vanilla alcohol; grants Tipsy. |
| Wiesnbrau 啤酒 | `Wiesnbrau` | Vanilla legacy | Legacy/old-platform alcohol; include only if the target ItemID exists. |
| 培根油 | `BaconOil` | Calamity | Calamity alcohol item despite meat-like name. |
| 血腥玛丽 | `BloodyMary` | Calamity | Calamity alcohol. |
| 加勒比朗姆酒 | `CaribbeanRum` | Calamity | Calamity alcohol. |
| 肉桂卷 | `CinnamonRoll` | Calamity | In Calamity's Alcohol folder; classify as alcohol for system consistency. |
| 生命之水 / Everclear | `Everclear` | Calamity | Calamity alcohol. |
| 常青金酒 | `EvergreenGin` | Calamity | Calamity alcohol. |
| 火球威士忌 | `Fireball` | Calamity | Calamity alcohol. |
| 葡萄啤酒 | `GrapeBeer` | Calamity | Calamity alcohol. |
| 曼哈顿 | `Manhattan` | Calamity | Calamity alcohol. |
| 玛格丽塔 | `Margarita` | Calamity | Calamity alcohol. |
| 私酿酒 | `Moonshine` | Calamity | Calamity alcohol. |
| 莫斯科骡子 | `MoscowMule` | Calamity | Calamity alcohol. |
| 古典鸡尾酒 | `OldFashioned` | Calamity | Calamity alcohol. |
| 紫雾 | `PurpleHaze` | Calamity | Calamity alcohol. |
| 红酒 | `RedWine` | Calamity | Calamity alcohol. |
| 朗姆酒 | `Rum` | Calamity | Calamity alcohol. |
| 螺丝起子 | `Screwdriver` | Calamity | Calamity alcohol. |
| 星束黑麦酒 | `StarBeamRye` | Calamity | Calamity alcohol. |
| 龙舌兰 | `Tequila` | Calamity | Calamity alcohol. |
| 龙舌兰日出 | `TequilaSunrise` | Calamity | Calamity alcohol. |
| 伏特加 | `Vodka` | Calamity | Calamity alcohol. |
| 威士忌 | `Whiskey` | Calamity | Calamity alcohol. |
| 白葡萄酒 | `WhiteWine` | Calamity | Calamity alcohol. |

## Feast

| Chinese name | English internal name | Source | Notes |
|---|---|---|---|
| 苹果派 | `ApplePie` | Vanilla | Processed dessert meal. |
| 香蕉船 | `BananaSplit` | Vanilla | Processed dessert meal. |
| 烤肋排 | `BBQRibs` | Vanilla | Meat, but processed enough to count as a feast. |
| 汤碗 | `BowlOfSoup` | Vanilla | Processed soup. |
| 炖兔兔 | `BunnyStew` | Vanilla | Processed stew. |
| 汉堡 | `Burger` | Vanilla | Composed meal. |
| 熟棉花糖 | `CookedMarshmallow` | Vanilla | Processed campfire food. |
| 圣诞布丁 | `ChristmasPudding` | Vanilla | Holiday processed food. |
| 法式蜗牛 | `Escargot` | Vanilla | Processed snail dish. |
| 蛙腿三明治 | `FroggleBunwich` | Vanilla | Sandwich/processed meal. |
| 水果沙拉 | `FruitSalad` | Vanilla | Processed fruit meal. |
| 蛆虫汤 | `GrubSoup` | Vanilla | Processed soup. |
| 热狗 | `Hotdog` | Vanilla | Composed processed meat meal. |
| 怪物千层面 | `MonsterLasagna` | Vanilla | Processed meal. |
| 泰式炒河粉 | `PadThai` | Vanilla | Processed noodle meal. |
| 越南河粉 | `Pho` | Vanilla | Processed noodle/soup meal. |
| 披萨 | `Pizza` | Vanilla | Processed meal. |
| 南瓜派 | `PumpkinPie` | Vanilla | Processed pie. |
| 炒蛙腿 | `SauteedFrogLegs` | Vanilla | Processed frog dish. |
| 海鲜大餐 | `SeafoodDinner` | Vanilla | Full seafood meal; explicitly not Fish. |
| 虾肉三明治 | `ShrimpPoBoy` | Vanilla | Seafood sandwich; feast, not fish. |
| 意大利面 | `Spaghetti` | Vanilla | Processed meal. |
| 法棍 | `Baguette` | Calamity | Calamity processed staple; user wants it in Feast. |
| 亵渎甜甜圈 | `BlasphemousDonut` | Calamity | Calamity special processed food. |
| 美味肉块 | `DeliciousMeat` | Calamity | Calamity special food; user wants it in Feast. |
| 海德拉炖菜 / 深渊炖鱼 | `HadalStew` | Calamity | Calamity special stew. |
| 超级鸡肉汤 / 熔岩鸡汤 | `LavaChickenBroth` | Calamity | Calamity special broth. |
| 蘑菇碗 | `ShroomBowl` | Calamity | Processed mushroom bowl. |
| 营养不良三明治 / 三明治 | `TheSandwich` | Calamity | Calamity special sandwich. |

## Snack

| Chinese name | English internal name | Source | Notes |
|---|---|---|---|
| 苹果汁 | `AppleJuice` | Vanilla | Drink. |
| 血腥莫斯卡托 | `BloodyMoscato` | Vanilla | Fruit drink; not alcohol in this taxonomy unless we decide wine wording should override. |
| 牛奶盒 | `MilkCarton` | Vanilla | Drink. |
| 巧克力曲奇 | `ChocolateChipCookie` | Vanilla | Cookie snack. |
| 咖啡 | `CoffeeCup` | Vanilla | Drink; internal name from Item IDs is `CoffeeCup`. |
| 奶油苏打 | `CreamSoda` | Vanilla | Soft drink. |
| 蛋酒 | `Eggnog` | Vanilla | Drink. |
| 薯条 | `Fries` | Vanilla | Snack if visual taxonomy wins; currently also noted under Fish because its drop source is Flying Fish. |
| 冰冻香蕉代基里 | `FrozenBananaDaiquiri` | Vanilla | Drink. |
| 果汁 | `FruitJuice` | Vanilla | Drink. |
| 姜饼曲奇 | `GingerbreadCookie` | Vanilla | Cookie snack. |
| 葡萄汁 | `GrapeJuice` | Vanilla | Drink. |
| 冰淇淋 | `IceCream` | Vanilla | Sweet snack. |
| Joja 可乐 | `JojaCola` | Vanilla | Drink. |
| 丛林果汁 | `JungleJuice` | Vanilla | Drink; include only if the target tModLoader version exposes this ItemID. |
| 柠檬水 | `Lemonade` | Vanilla | Drink. |
| 棉花糖 | `Marshmallow` | Vanilla | Snack; raw version is consumable briefly. |
| 奶昔 | `Milkshake` | Vanilla | Drink. |
| 玉米片 | `Nachos` | Vanilla | Snack. |
| 桃子桑格利亚 | `PeachSangria` | Vanilla | Fruit drink; if we decide sangria should always be alcohol, move to Alcohol. |
| 椰林飘香 | `PinaColada` | Vanilla | Fruit drink/cocktail; current taxonomy treats vanilla fed drinks as Snack. |
| 薯片 | `PotatoChips` | Vanilla | Snack. |
| 棱镜潘趣酒 | `PrismaticPunch` | Vanilla | Drink. |
| 冰晶棒棒糖 | `RockCandy` | Vanilla | Candy snack; include only if the target tModLoader version exposes this ItemID. |
| 黑暗思慕雪 | `SmoothieofDarkness` | Vanilla | Drink; exact ItemID spelling uses lowercase `of`. |
| 辣椒 | `SpicyPepper` | Vanilla | Small snack/pepper; not a full meal. |
| 糖曲奇 | `SugarCookie` | Vanilla | Cookie snack. |
| 茶杯 | `Teacup` | Vanilla | Drink. |
| 热带思慕雪 | `TropicalSmoothie` | Vanilla | Drink. |

## Superfood

| Chinese name | English internal name | Source | Notes |
|---|---|---|---|
| 金美味 | `GoldenDelight` | Vanilla | Golden critter food; own top-tier category. |

## Unique

| Chinese name | English internal name | Source | Notes |
|---|---|---|---|
| 怪异蘑菇 | `OddMushroom` | Calamity | Keep separate because the effect is too strange and the flavor does not fit normal food groups. |

## Excluded or Guarded Items

| Chinese name | English internal name | Source | Reason |
|---|---|---|---|
| 苹果派切片 | `ApplePieSlice` | Vanilla | Unobtainable/unimplemented and not actually consumable. |
| 海鲜 | `Seafood` | Calamity | Boss summon item, not player food. |
| 蠕虫诱饵 / 蠕虫食物 | `WormFood` | Vanilla | Boss summon, not player food. |
| 腥臭蠕虫食物 | `BloodyWormFood` | Calamity | Boss summon, not player food. |
| 闪耀女皇 / 辉光女皇 | `SparklingEmpress` | Calamity | Weapon-like item; not player food. |
| Gacruxian Mollusk | `GacruxianMollusk` | Calamity | Hook/gear-like item; not player food. |
| 龙骑士细雨鱼 | `DragoonDrizzlefish` | Calamity | Weapon/fishing catch weapon, not player food. |
| 北极星鹦嘴鱼 | `PolarisParrotfish` | Calamity | Weapon/fishing catch weapon, not player food. |
| 蛇咬 | `SerpentsBite` | Calamity | Hook/tool-like item, not player food. |
| 优质糊糊 | `QualitySlop` | Calamity local gap | Explicitly excluded for this reset; do not feed it even if a future dependency exposes the item. |

## Implementation Notes For Later

- Prefer an explicit whitelist for vanilla and Calamity items instead of broad name matching.
- Use stable string names for version-dependent vanilla IDs such as `RockCandy`, `JungleJuice`, and legacy `Wiesnbrau` so missing IDs simply never classify.
- Calamity items are classified only by explicit internal-name whitelist; excluded/guarded items above must remain non-feedable.
- If one item appears visually in two groups, the chosen priority should be: Gel > Unique > Superfood > Alcohol > Feast > Fish > Meat > Fruit > Snack.
- For this design pass, `Fries` is the only intentionally duplicated candidate. Decide before implementation whether source-based Fish or visual Snack should win.
