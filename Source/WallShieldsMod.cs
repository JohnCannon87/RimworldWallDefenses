using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace WallShields
{
    class WallShieldsMod : Mod
    {
        private Vector2 scrollPosition = Vector2.zero;

        public WallShieldsMod(ModContentPack content) : base(content)
        {
            LongEventHandler.ExecuteWhenFinished(GetSettings);
        }

        public void GetSettings()
        {
            GetSettings<WallShieldsSettings>();
        }

        public static IEnumerable<ThingDef> PossibleThingDefs()
        {
            return from d in DefDatabase<ThingDef>.AllDefs
                   where (d.category == ThingCategory.Item && d.scatterableOnMapGen && !d.destroyOnDrop && !d.MadeFromStuff)
                   select d;
        }

        public override void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard list = new Listing_Standard()
            {
                ColumnWidth = rect.width
            };

            list.Begin(rect);

            list.Label("Shield - Power Per Cell Covered: " + WallShieldsSettings.shieldPowerPerCell);
            WallShieldsSettings.shieldPowerPerCell = list.Slider(WallShieldsSettings.shieldPowerPerCell, 0, 100).RoundToAsInt(1);

            list.Label("Shield - Cell Exponent Multiplication Value: " + WallShieldsSettings.shieldCellExponent);
            WallShieldsSettings.shieldCellExponent = list.Slider(WallShieldsSettings.shieldCellExponent, 1, 20).RoundToAsInt(1);

            list.Label("Shield total power calculation is ('Cells Covered' ^ 'Cell Exponent') * power per cell");

            list.Label("AA Turret - Protection Range Value (56 Max range as that's the largest target area the game can draw): " + WallShieldsSettings.protectionRange);
            WallShieldsSettings.protectionRange = list.Slider(WallShieldsSettings.protectionRange, 1, 56).RoundToAsInt(1);

            list.Label("AA Turret - Reload Speed (in ticks) Value: " + WallShieldsSettings.reloadSpeed);
            WallShieldsSettings.reloadSpeed = list.Slider(WallShieldsSettings.reloadSpeed, 1, 240).RoundToAsInt(1);

            list.Label("AA Turret - Ammo Count Value: " + WallShieldsSettings.ammoCount);
            WallShieldsSettings.ammoCount = list.Slider(WallShieldsSettings.ammoCount, 1, 20).RoundToAsInt(1);

            list.Label("This is per shot, lots of turrets in a single area will likely result in a few droppods being destroyed/occupants heavily injured and the rest being fine.");

            list.Label("AA Turret - Chance To Completely Destroy Droppod: " + WallShieldsSettings.chanceOfCompletelyDestroyingDropPod);
            WallShieldsSettings.chanceOfCompletelyDestroyingDropPod = list.Slider(WallShieldsSettings.chanceOfCompletelyDestroyingDropPod, 0, 100).RoundToAsInt(1);

            list.Label("AA Turret - Damage to Droppod Occupant Per Hit: " + WallShieldsSettings.bulletDamage);
            WallShieldsSettings.bulletDamage = list.Slider(WallShieldsSettings.bulletDamage, 1, 100).RoundToAsInt(1);

            list.Label("AA Turret - Max Amount Of Hits To A Droppod Occupant (between 1 and this value) : " + WallShieldsSettings.maxShotsAtDropPodOccupant);
            WallShieldsSettings.maxShotsAtDropPodOccupant = list.Slider(WallShieldsSettings.maxShotsAtDropPodOccupant, 1, 100).RoundToAsInt(1);

            list.End();
        }

        public override string SettingsCategory()
        {
            return "Wall Defenses";
        }
    }
}
