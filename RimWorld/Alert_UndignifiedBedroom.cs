using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
	public class Alert_UndignifiedBedroom : Alert
	{
		private List<Pawn> targetsResult = new List<Pawn>();

		public List<Pawn> Targets
		{
			get
			{
				targetsResult.Clear();
				List<Map> maps = Find.Maps;
				for (int i = 0; i < maps.Count; i++)
				{
					foreach (Pawn freeColonist in maps[i].mapPawns.FreeColonists)
					{
						if (freeColonist.royalty != null && freeColonist.royalty.GetUnmetBedroomRequirements().Any())
						{
							targetsResult.Add(freeColonist);
						}
					}
				}
				return targetsResult;
			}
		}

		public Alert_UndignifiedBedroom()
		{
			defaultLabel = "UndignifiedBedroom".Translate();
			defaultExplanation = "UndignifiedBedroomDesc".Translate();
		}

		public override AlertReport GetReport()
		{
			return AlertReport.CulpritsAre(Targets);
		}

		public override TaggedString GetExplanation()
		{
			return defaultExplanation + "\n" + Targets.Select(delegate(Pawn t)
			{
				RoyalTitle royalTitle = t.royalty.HighestTitleWithBedroomRequirements();
				RoyalTitleDef royalTitleDef = royalTitle.RoomRequirementGracePeriodActive(t) ? royalTitle.def.GetPreviousTitle(royalTitle.faction) : royalTitle.def;
				string[] array = t.royalty.GetUnmetBedroomRequirements(includeOnGracePeriod: false).ToArray();
				string[] array2 = t.royalty.GetUnmetBedroomRequirements(includeOnGracePeriod: true, onlyOnGracePeriod: true).ToArray();
				bool flag = royalTitleDef != null && array.Length != 0;
				StringBuilder stringBuilder = new StringBuilder();
				if (flag)
				{
					stringBuilder.Append(t.LabelShort + " (" + royalTitleDef.GetLabelFor(t.gender) + "):\n" + array.ToLineList("- "));
				}
				if (array2.Length != 0)
				{
					if (flag)
					{
						stringBuilder.Append("\n\n");
					}
					stringBuilder.Append(t.LabelShort + " (" + royalTitle.def.GetLabelFor(t.gender) + ", " + "RoomRequirementGracePeriodDesc".Translate(royalTitle.RoomRequirementGracePeriodDaysLeft.ToString("0.0")) + ")" + ":\n" + array2.ToLineList("- "));
				}
				return stringBuilder.ToString();
			}).ToLineList("\n");
		}
	}
}
