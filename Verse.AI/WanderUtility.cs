using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace Verse.AI
{
	public static class WanderUtility
	{
		private static List<IntVec3> gatherSpots = new List<IntVec3>();

		public static IntVec3 BestCloseWanderRoot(IntVec3 trueWanderRoot, Pawn pawn)
		{
			for (int i = 0; i < 50; i++)
			{
				IntVec3 intVec = (i >= 8) ? (trueWanderRoot + GenRadial.RadialPattern[i - 8 + 1] * 7) : (trueWanderRoot + GenRadial.RadialPattern[i]);
				if (intVec.InBounds(pawn.Map) && intVec.Walkable(pawn.Map) && pawn.CanReach(intVec, PathEndMode.OnCell, Danger.Some))
				{
					return intVec;
				}
			}
			return IntVec3.Invalid;
		}

		public static bool InSameRoom(IntVec3 locA, IntVec3 locB, Map map)
		{
			Room room = locA.GetRoom(map, RegionType.Set_All);
			if (room == null)
			{
				return true;
			}
			return room == locB.GetRoom(map, RegionType.Set_All);
		}

		public static IntVec3 GetColonyWanderRoot(Pawn pawn)
		{
			if (pawn.RaceProps.Humanlike)
			{
				gatherSpots.Clear();
				for (int i = 0; i < pawn.Map.gatherSpotLister.activeSpots.Count; i++)
				{
					IntVec3 position = pawn.Map.gatherSpotLister.activeSpots[i].parent.Position;
					if (!position.IsForbidden(pawn) && pawn.CanReach(position, PathEndMode.Touch, Danger.None))
					{
						gatherSpots.Add(position);
					}
				}
				if (gatherSpots.Count > 0)
				{
					return gatherSpots.RandomElement();
				}
			}
			List<Building> allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
			if (allBuildingsColonist.Count > 0)
			{
				int num = 0;
				while (true)
				{
					num++;
					if (num > 20)
					{
						break;
					}
					Building building = allBuildingsColonist.RandomElement();
					if ((building.def != ThingDefOf.Wall && !building.def.building.ai_chillDestination) || building.Position.IsForbidden(pawn) || !pawn.Map.areaManager.Home[building.Position])
					{
						continue;
					}
					int num2 = 15 + num * 2;
					if ((pawn.Position - building.Position).LengthHorizontalSquared <= num2 * num2)
					{
						IntVec3 intVec = GenAdjFast.AdjacentCells8Way(building).RandomElement();
						if (intVec.Standable(building.Map) && !intVec.IsForbidden(pawn) && pawn.CanReach(intVec, PathEndMode.OnCell, Danger.None) && !intVec.IsInPrisonCell(pawn.Map))
						{
							return intVec;
						}
					}
				}
			}
			if (pawn.Map.mapPawns.FreeColonistsSpawned.Where((Pawn c) => !c.Position.IsForbidden(pawn) && pawn.CanReach(c.Position, PathEndMode.Touch, Danger.None)).TryRandomElement(out Pawn result))
			{
				return result.Position;
			}
			return pawn.Position;
		}
	}
}
