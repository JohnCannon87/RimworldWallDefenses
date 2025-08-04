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
            // Setup scroll view area and inner content height estimate
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, 1600f); // height might need adjustment
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            Listing_Standard list = new Listing_Standard()
            {
                ColumnWidth = viewRect.width
            };
            list.Begin(viewRect);

            // --- Laser Grid Section ---
            list.Label("Laser Grid Settings");
            list.GapLine();

            list.Label($"Max Distance Between Emitter and Reflector: {WallShieldsSettings.maxLaserGridDistance}");
            WallShieldsSettings.maxLaserGridDistance = list.Slider(WallShieldsSettings.maxLaserGridDistance, 0, 100).RoundToAsInt(1);

            list.Label($"Power Per Cell Covered: {WallShieldsSettings.laserPowerPerCell}");
            WallShieldsSettings.laserPowerPerCell = list.Slider(WallShieldsSettings.laserPowerPerCell, 0, 100).RoundToAsInt(1);

            list.Label($"Cell Exponent Multiplication Value: {WallShieldsSettings.laserGridExponent}");
            WallShieldsSettings.laserGridExponent = list.Slider(WallShieldsSettings.laserGridExponent, 1, 20).RoundToAsInt(1);

            list.Label("Total power = (Cells Covered ^ Cell Exponent) * Power Per Cell");

            list.GapLine();
            list.Gap(12f);

            // --- Shield Section ---
            list.Label("Shield Settings");
            list.GapLine();

            list.Label($"Power Per Cell Covered: {WallShieldsSettings.shieldPowerPerCell}");
            WallShieldsSettings.shieldPowerPerCell = list.Slider(WallShieldsSettings.shieldPowerPerCell, 0, 100).RoundToAsInt(1);

            list.Label($"Cell Exponent Multiplication Value: {WallShieldsSettings.shieldCellExponent}");
            WallShieldsSettings.shieldCellExponent = list.Slider(WallShieldsSettings.shieldCellExponent, 1, 20).RoundToAsInt(1);

            list.Label("Total power = (Cells Covered ^ Cell Exponent) * Power Per Cell");

            list.GapLine();
            list.Gap(12f);

            // --- AA Turret Section ---
            list.Label("AA Turret Settings");
            list.GapLine();

            list.Label($"Protection Range (max 56): {WallShieldsSettings.protectionRange}");
            WallShieldsSettings.protectionRange = list.Slider(WallShieldsSettings.protectionRange, 1, 56).RoundToAsInt(1);

            list.Label($"Reload Speed (ticks): {WallShieldsSettings.reloadSpeed}");
            WallShieldsSettings.reloadSpeed = list.Slider(WallShieldsSettings.reloadSpeed, 1, 240).RoundToAsInt(1);

            list.Label($"Ammo Count: {WallShieldsSettings.ammoCount}");
            WallShieldsSettings.ammoCount = list.Slider(WallShieldsSettings.ammoCount, 1, 20).RoundToAsInt(1);

            list.Label("Note: Multiple turrets can heavily damage or destroy droppods in range.");
            list.Gap(6f);

            list.Label($"Chance To Completely Destroy Droppod (%): {WallShieldsSettings.chanceOfCompletelyDestroyingDropPod}");
            WallShieldsSettings.chanceOfCompletelyDestroyingDropPod = list.Slider(WallShieldsSettings.chanceOfCompletelyDestroyingDropPod, 0, 100).RoundToAsInt(1);

            list.Label($"Damage To Droppod Occupant Per Hit: {WallShieldsSettings.bulletDamage}");
            WallShieldsSettings.bulletDamage = list.Slider(WallShieldsSettings.bulletDamage, 1, 100).RoundToAsInt(1);

            list.Label($"Max Hits To Droppod Occupant: {WallShieldsSettings.maxShotsAtDropPodOccupant}");
            WallShieldsSettings.maxShotsAtDropPodOccupant = list.Slider(WallShieldsSettings.maxShotsAtDropPodOccupant, 1, 100).RoundToAsInt(1);

            list.GapLine();
            list.Gap(12f);

            // --- Wall Turret Section ---
            list.Label("Wall Turret Settings");
            list.GapLine();

            list.Label($"Battery Drain Per Beam Fired: {WallShieldsSettings.laserCannonDrain}");
            WallShieldsSettings.laserCannonDrain = list.Slider(WallShieldsSettings.laserCannonDrain, 1, 100).RoundToAsInt(1);

            list.Label($"Wall Turret Beam - Damage per Shot: {WallShieldsSettings.laserCannonDamage:F1}");
            WallShieldsSettings.laserCannonDamage = list.Slider(WallShieldsSettings.laserCannonDamage, 1f, 200f).RoundToAsInt(1);


            list.End();
            Widgets.EndScrollView();
        }


        /*public override void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard list = new Listing_Standard()
            {
                ColumnWidth = rect.width
            };

            list.Begin(rect);

            list.Label("Laser Grid - Max Distane Between Emitter and Reflector: " + WallShieldsSettings.maxLaserGridDistance);
            WallShieldsSettings.maxLaserGridDistance = list.Slider(WallShieldsSettings.maxLaserGridDistance, 0, 100).RoundToAsInt(1);

            list.Label("Laser Grid - Power Per Cell Covered: " + WallShieldsSettings.laserPowerPerCell);
            WallShieldsSettings.laserPowerPerCell = list.Slider(WallShieldsSettings.laserPowerPerCell, 0, 100).RoundToAsInt(1);

            list.Label("Laser Grid - Cell Exponent Multiplication Value: " + WallShieldsSettings.laserGridExponent);
            WallShieldsSettings.laserGridExponent = list.Slider(WallShieldsSettings.laserGridExponent, 1, 20).RoundToAsInt(1);

            list.Label("Laser Grid total power calculation is ('Cells Covered' ^ 'Cell Exponent') * power per cell");

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


            list.Label("Wall Turret, Battery drain per beam fired) : " + WallShieldsSettings.laserCannonDrain);
            WallShieldsSettings.laserCannonDrain = list.Slider(WallShieldsSettings.laserCannonDrain, 1, 100).RoundToAsInt(1);

            list.End();
        }*/

        public override string SettingsCategory()
        {
            return "Wall Defenses";
        }
    }
}
