using RimWorld;
using Verse;
using Verse.AI;

namespace BilledDeepDrill
{
    public class BDD_WorkGiver_DoBill : WorkGiver_DoBill
    {
        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction)
            {
                return false;
            }
            if (!(t is Building building))
            {
                return false;
            }
            if (building.IsForbidden(pawn))
            {
                return false;
            }
            if (!pawn.CanReserve(building, 1, -1, null, forced))
            {
                return false;
            }
            if (!building.TryGetComp<CompDeepDrill>().CanDrillNow())
            {
                return false;
            }
            if (building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) != null)
            {
                return false;
            }
            if (building.IsBurning())
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (!(thing is IBillGiver billGiver) || !ThingIsUsableBillGiver(thing) || !billGiver.BillStack.AnyShouldDoNow || !billGiver.UsableForBillsAfterFueling() || !pawn.CanReserve(thing, 1, -1, null, forced) || thing.IsBurning() || thing.IsForbidden(pawn) || (thing.def.hasInteractionCell && !pawn.CanReserveSittableOrSpot(thing.InteractionCell, forced)))
            {
                return null;
            }
            billGiver.BillStack.RemoveIncompletableBills();
            return BDD_StartOrResumeBillJob(pawn, billGiver);
        }

        private Job BDD_StartOrResumeBillJob(Pawn pawn, IBillGiver giver)
        {
            for (int i = 0; i < giver.BillStack.Count; i++)
            {
                Bill bill = giver.BillStack[i];
                if ((bill.recipe.requiredGiverWorkType == null || bill.recipe.requiredGiverWorkType == def.workType) && bill.ShouldDoNow() && bill.PawnAllowedToStartAnew(pawn))
                {
                    SkillRequirement skillRequirement = bill.recipe.FirstSkillRequirementPawnDoesntSatisfy(pawn);
                    if (skillRequirement == null)
                    {
                        return BDD_TryStartNewDoBillJob(bill, giver);
                    }
                    JobFailReason.Is("UnderRequiredSkill".Translate(skillRequirement.minLevel), bill.Label);
                }
            }
            return null;
        }

        private Job BDD_TryStartNewDoBillJob(Bill bill, IBillGiver giver)
        {
            Thing thing = giver as Thing;
            Job job = JobMaker.MakeJob(JobDefOf.OperateDeepDrill, thing, 1500, checkOverrideOnExpiry: true);
            job.haulMode = HaulMode.ToCellNonStorage;
            job.bill = bill;
            return job;
        }
    }

}
