using Verse.AI;
using Verse.AI.Group;

namespace RimWorld
{
	public class LordToil_ExitMapAndDefendSelf : LordToil
	{
		public override void UpdateAllDuties()
		{
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				lord.ownedPawns[i].mindState.duty = new PawnDuty(DutyDefOf.ExitMapBestAndDefendSelf);
			}
		}
	}
}
