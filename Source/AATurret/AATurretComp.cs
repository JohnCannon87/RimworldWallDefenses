using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Sound;

namespace WallShields
{
    class AATurretComp : ThingComp
    {
        protected AATurretTop top;
        int range = 0;
        int tickCount = 0;
        int ammoRemaining = 0;

        public static readonly SoundDef HitSoundDef = SoundDef.Named("AAGun_Fire");

        public CompProperties_AATurretComp AATurretComps
        {
            get
            {
                return (CompProperties_AATurretComp)this.props;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (top != null)
            {
                top.DrawTurret();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            range = WallShieldsSettings.protectionRange;
            ammoRemaining = WallShieldsSettings.ammoCount;
            top = new AATurretTop(this);
        }

        public override void CompTick()
        {
            tickCount++;
            if (!IsActive())
            {
                top.TurretTopTick();
                return;
            }

            this.ShootThings();
            if(tickCount >= WallShieldsSettings.reloadSpeed)
            {
                tickCount = 0;
                Reload();
            }
            top.TurretTopTick();
        }

        private void ShootThings()
        {
            if (ammoRemaining > 0)
            {
                List<Thing> targets = new List<Thing>();
                targets.AddRange(GetOffensiveDroppods());
                targets.AddRange(this.parent.Map.listerThings.ThingsOfDef(ThingDefOf.ShipChunkIncoming));
                targets.AddRange(this.parent.Map.listerThings.ThingsOfDef(ThingDefOf.CrashedShipPartIncoming));
                targets.AddRange(this.parent.Map.listerThings.ThingsOfDef(ThingDefOf.DefoliatorShipPart));
                targets.AddRange(this.parent.Map.listerThings.ThingsOfDef(ThingDefOf.PsychicDronerShipPart));

                if (targets != null)
                {
                    IEnumerable<Thing> targetsInRange = targets.Where<Thing>(t => t.Position.InHorDistOf(this.parent.Position, this.range));

                    foreach (Thing target in targetsInRange)
                    {
                        HitSoundDef.PlayOneShot((SoundInfo)new TargetInfo(this.parent.Position, this.parent.Map, false));
                        GenExplosion.DoExplosion(target.Position, this.parent.Map, 1, DamageDefOf.Bomb, target);
                        HitSoundDef.PlayOneShot((SoundInfo)new TargetInfo(this.parent.Position, this.parent.Map, false));
                        top.CurRotation = (target.Position.ToVector3Shifted() - this.parent.DrawPos).AngleFlat();
                        top.ticksUntilIdleTurn = Rand.RangeInclusive(150, 350);

                        target.Destroy(DestroyMode.Vanish);
                        ammoRemaining--;
                        if (ammoRemaining <= 0)
                        {
                            tickCount = 0;
                            break;
                        }
                    }
                    tickCount = 0;
                }
            }
        }

        private IEnumerable<Thing> GetOffensiveDroppods()
        {
            List<Thing> result = new List<Thing>();

            List<Thing> dropPods = this.parent.Map.listerThings.ThingsOfDef(ThingDefOf.DropPodIncoming);

            foreach (DropPodIncoming pod in dropPods)
            {
                if(pod.Contents.innerContainer.Any((Thing x) => x.Faction.HostileTo(Faction.OfPlayer))){
                    result.Add(pod);
                }
            }

            return result;
        }

        private void Reload()
        {
            ammoRemaining = WallShieldsSettings.ammoCount;
        }

        private bool PowerOn
        {
            get
            {
                CompPowerTrader comp = this.parent.GetComp<CompPowerTrader>();
                return comp != null && comp.PowerOn;
            }
        }

        private bool IsActive()
        {
            bool isActive = this.parent.Spawned && this.PowerOn && IsThereAThreat();
            return isActive;
        }

        private bool IsThereAThreat()
        {
            if (GenHostility.AnyHostileActiveThreatTo(parent.MapHeld, parent.Faction))
            {
                return true;
            }
            return false;
        }
    }
}
