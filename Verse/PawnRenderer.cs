using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse.AI.Group;

namespace Verse
{
	public class PawnRenderer
	{
		private Pawn pawn;

		public PawnGraphicSet graphics;

		public PawnDownedWiggler wiggler;

		private PawnHeadOverlays statusOverlays;

		private PawnStatusEffecters effecters;

		private PawnWoundDrawer woundOverlays;

		private Graphic_Shadow shadowGraphic;

		private const float CarriedThingDrawAngle = 16f;

		private const float SubInterval = 3f / 980f;

		private const float YOffset_PrimaryEquipmentUnder = 0f;

		private const float YOffset_Behind = 3f / 980f;

		private const float YOffset_Utility_South = 3f / 490f;

		private const float YOffset_Body = 9f / 980f;

		private const float YOffsetInterval_Clothes = 3f / 980f;

		private const float YOffset_Wounds = 9f / 490f;

		private const float YOffset_Shell = 3f / 140f;

		private const float YOffset_Head = 6f / 245f;

		private const float YOffset_Utility = 27f / 980f;

		private const float YOffset_OnHead = 3f / 98f;

		private const float YOffset_PostHead = 33f / 980f;

		private const float YOffset_CarriedThing = 9f / 245f;

		private const float YOffset_PrimaryEquipmentOver = 9f / 245f;

		private const float YOffset_Status = 39f / 980f;

		private RotDrawMode CurRotDrawMode
		{
			get
			{
				if (pawn.Dead && pawn.Corpse != null)
				{
					return pawn.Corpse.CurRotDrawMode;
				}
				return RotDrawMode.Fresh;
			}
		}

		public PawnRenderer(Pawn pawn)
		{
			this.pawn = pawn;
			wiggler = new PawnDownedWiggler(pawn);
			statusOverlays = new PawnHeadOverlays(pawn);
			woundOverlays = new PawnWoundDrawer(pawn);
			graphics = new PawnGraphicSet(pawn);
			effecters = new PawnStatusEffecters(pawn);
		}

		public void RenderPawnAt(Vector3 drawLoc)
		{
			RenderPawnAt(drawLoc, CurRotDrawMode, !pawn.health.hediffSet.HasHead, pawn.IsInvisible());
		}

		public void RenderPawnAt(Vector3 drawLoc, RotDrawMode bodyDrawType, bool headStump, bool invisible)
		{
			if (!graphics.AllResolved)
			{
				graphics.ResolveAllGraphics();
			}
			if (pawn.GetPosture() == PawnPosture.Standing)
			{
				RenderPawnInternal(drawLoc, 0f, renderBody: true, bodyDrawType, headStump, invisible);
				if (pawn.carryTracker != null)
				{
					Thing carriedThing = pawn.carryTracker.CarriedThing;
					if (carriedThing != null)
					{
						Vector3 drawPos = drawLoc;
						bool behind = false;
						bool flip = false;
						if (pawn.CurJob == null || !pawn.jobs.curDriver.ModifyCarriedThingDrawPos(ref drawPos, ref behind, ref flip))
						{
							if (carriedThing is Pawn || carriedThing is Corpse)
							{
								drawPos += new Vector3(0.44f, 0f, 0f);
							}
							else
							{
								drawPos += new Vector3(0.18f, 0f, 0.05f);
							}
						}
						if (behind)
						{
							drawPos.y -= 9f / 245f;
						}
						else
						{
							drawPos.y += 9f / 245f;
						}
						carriedThing.DrawAt(drawPos, flip);
					}
				}
				if (!invisible)
				{
					if (pawn.def.race.specialShadowData != null)
					{
						if (shadowGraphic == null)
						{
							shadowGraphic = new Graphic_Shadow(pawn.def.race.specialShadowData);
						}
						shadowGraphic.Draw(drawLoc, Rot4.North, pawn);
					}
					if (graphics.nakedGraphic != null && graphics.nakedGraphic.ShadowGraphic != null)
					{
						graphics.nakedGraphic.ShadowGraphic.Draw(drawLoc, Rot4.North, pawn);
					}
				}
			}
			else
			{
				float angle = BodyAngle();
				Rot4 rot = LayingFacing();
				Building_Bed building_Bed = pawn.CurrentBed();
				bool renderBody;
				Vector3 rootLoc;
				if (building_Bed != null && pawn.RaceProps.Humanlike)
				{
					renderBody = building_Bed.def.building.bed_showSleeperBody;
					AltitudeLayer altLayer = (AltitudeLayer)Mathf.Max((int)building_Bed.def.altitudeLayer, 17);
					Vector3 vector;
					Vector3 a = (vector = pawn.Position.ToVector3ShiftedWithAltitude(altLayer));
					vector.y += 6f / 245f;
					Rot4 rotation = building_Bed.Rotation;
					rotation.AsInt += 2;
					float d = 0f - BaseHeadOffsetAt(Rot4.South).z;
					Vector3 a2 = rotation.FacingCell.ToVector3();
					rootLoc = a + a2 * d;
					rootLoc.y += 9f / 980f;
				}
				else
				{
					renderBody = true;
					rootLoc = drawLoc;
					if (!pawn.Dead && pawn.CarriedBy == null)
					{
						rootLoc.y = AltitudeLayer.LayingPawn.AltitudeFor() + 9f / 980f;
					}
				}
				RenderPawnInternal(rootLoc, angle, renderBody, rot, rot, bodyDrawType, portrait: false, headStump, invisible);
			}
			if (pawn.Spawned && !pawn.Dead)
			{
				pawn.stances.StanceTrackerDraw();
				pawn.pather.PatherDraw();
			}
			DrawDebug();
		}

		public void RenderPortrait()
		{
			Vector3 zero = Vector3.zero;
			float angle;
			if (pawn.Dead || pawn.Downed)
			{
				angle = 85f;
				zero.x -= 0.18f;
				zero.z -= 0.18f;
			}
			else
			{
				angle = 0f;
			}
			RenderPawnInternal(zero, angle, renderBody: true, Rot4.South, Rot4.South, CurRotDrawMode, portrait: true, !pawn.health.hediffSet.HasHead, pawn.IsInvisible());
		}

		private void RenderPawnInternal(Vector3 rootLoc, float angle, bool renderBody, RotDrawMode draw, bool headStump, bool invisible)
		{
			RenderPawnInternal(rootLoc, angle, renderBody, pawn.Rotation, pawn.Rotation, draw, portrait: false, headStump, invisible);
		}

		private void RenderPawnInternal(Vector3 rootLoc, float angle, bool renderBody, Rot4 bodyFacing, Rot4 headFacing, RotDrawMode bodyDrawType, bool portrait, bool headStump, bool invisible)
		{
			if (!graphics.AllResolved)
			{
				graphics.ResolveAllGraphics();
			}
			Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
			Mesh mesh = null;
			if (renderBody)
			{
				Vector3 loc = rootLoc;
				loc.y += 9f / 980f;
				if (bodyDrawType == RotDrawMode.Dessicated && !pawn.RaceProps.Humanlike && graphics.dessicatedGraphic != null && !portrait)
				{
					graphics.dessicatedGraphic.Draw(loc, bodyFacing, pawn, angle);
				}
				else
				{
					mesh = ((!pawn.RaceProps.Humanlike) ? graphics.nakedGraphic.MeshAt(bodyFacing) : MeshPool.humanlikeBodySet.MeshAt(bodyFacing));
					List<Material> list = graphics.MatsBodyBaseAt(bodyFacing, bodyDrawType);
					for (int i = 0; i < list.Count; i++)
					{
						Material mat = OverrideMaterialIfNeeded_NewTemp(list[i], pawn, portrait);
						GenDraw.DrawMeshNowOrLater(mesh, loc, quaternion, mat, portrait);
						loc.y += 3f / 980f;
					}
					if (bodyDrawType == RotDrawMode.Fresh)
					{
						Vector3 drawLoc = rootLoc;
						drawLoc.y += 9f / 490f;
						woundOverlays.RenderOverBody(drawLoc, mesh, quaternion, portrait);
					}
				}
			}
			Vector3 vector = rootLoc;
			Vector3 a = rootLoc;
			if (bodyFacing != Rot4.North)
			{
				a.y += 6f / 245f;
				vector.y += 3f / 140f;
			}
			else
			{
				a.y += 3f / 140f;
				vector.y += 6f / 245f;
			}
			Vector3 vector2 = rootLoc;
			vector2.y += ((bodyFacing == Rot4.South) ? (3f / 490f) : (27f / 980f));
			List<ApparelGraphicRecord> apparelGraphics = graphics.apparelGraphics;
			if (graphics.headGraphic != null)
			{
				Vector3 b = quaternion * BaseHeadOffsetAt(headFacing);
				Material material = graphics.HeadMatAt_NewTemp(headFacing, bodyDrawType, headStump, portrait);
				if (material != null)
				{
					GenDraw.DrawMeshNowOrLater(MeshPool.humanlikeHeadSet.MeshAt(headFacing), a + b, quaternion, material, portrait);
				}
				Vector3 loc2 = rootLoc + b;
				loc2.y += 3f / 98f;
				bool flag = false;
				if (!portrait || !Prefs.HatsOnlyOnMap)
				{
					Mesh mesh2 = graphics.HairMeshSet.MeshAt(headFacing);
					for (int j = 0; j < apparelGraphics.Count; j++)
					{
						if (apparelGraphics[j].sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Overhead)
						{
							if (!apparelGraphics[j].sourceApparel.def.apparel.hatRenderedFrontOfFace)
							{
								flag = true;
								Material original = apparelGraphics[j].graphic.MatAt(bodyFacing);
								original = OverrideMaterialIfNeeded_NewTemp(original, pawn, portrait);
								GenDraw.DrawMeshNowOrLater(mesh2, loc2, quaternion, original, portrait);
							}
							else
							{
								Material original2 = apparelGraphics[j].graphic.MatAt(bodyFacing);
								original2 = OverrideMaterialIfNeeded_NewTemp(original2, pawn, portrait);
								Vector3 loc3 = rootLoc + b;
								loc3.y += ((bodyFacing == Rot4.North) ? (3f / 980f) : (33f / 980f));
								GenDraw.DrawMeshNowOrLater(mesh2, loc3, quaternion, original2, portrait);
							}
						}
					}
				}
				if (!flag && bodyDrawType != RotDrawMode.Dessicated && !headStump)
				{
					GenDraw.DrawMeshNowOrLater(graphics.HairMeshSet.MeshAt(headFacing), mat: graphics.HairMatAt_NewTemp(headFacing, portrait), loc: loc2, quat: quaternion, drawNow: portrait);
				}
			}
			if (renderBody)
			{
				for (int k = 0; k < apparelGraphics.Count; k++)
				{
					ApparelGraphicRecord apparelGraphicRecord = apparelGraphics[k];
					if (apparelGraphicRecord.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Shell && !apparelGraphicRecord.sourceApparel.def.apparel.shellRenderedBehindHead)
					{
						Material original3 = apparelGraphicRecord.graphic.MatAt(bodyFacing);
						original3 = OverrideMaterialIfNeeded_NewTemp(original3, pawn, portrait);
						GenDraw.DrawMeshNowOrLater(mesh, vector, quaternion, original3, portrait);
					}
					if (RenderAsPack(apparelGraphicRecord.sourceApparel))
					{
						Material original4 = apparelGraphicRecord.graphic.MatAt(bodyFacing);
						original4 = OverrideMaterialIfNeeded_NewTemp(original4, pawn, portrait);
						if (apparelGraphicRecord.sourceApparel.def.apparel.wornGraphicData != null)
						{
							Vector2 vector3 = apparelGraphicRecord.sourceApparel.def.apparel.wornGraphicData.BeltOffsetAt(bodyFacing, pawn.story.bodyType);
							Vector2 vector4 = apparelGraphicRecord.sourceApparel.def.apparel.wornGraphicData.BeltScaleAt(pawn.story.bodyType);
							Matrix4x4 matrix = Matrix4x4.Translate(vector2) * Matrix4x4.Rotate(quaternion) * Matrix4x4.Translate(new Vector3(vector3.x, 0f, vector3.y)) * Matrix4x4.Scale(new Vector3(vector4.x, 1f, vector4.y));
							GenDraw.DrawMeshNowOrLater_NewTemp(mesh, matrix, original4, portrait);
						}
						else
						{
							GenDraw.DrawMeshNowOrLater(mesh, vector, quaternion, original4, portrait);
						}
					}
				}
			}
			if (!portrait && pawn.RaceProps.Animal && pawn.inventory != null && pawn.inventory.innerContainer.Count > 0 && graphics.packGraphic != null)
			{
				Graphics.DrawMesh(mesh, vector, quaternion, graphics.packGraphic.MatAt(bodyFacing), 0);
			}
			if (portrait)
			{
				return;
			}
			DrawEquipment(rootLoc);
			if (pawn.apparel != null)
			{
				List<Apparel> wornApparel = pawn.apparel.WornApparel;
				for (int l = 0; l < wornApparel.Count; l++)
				{
					wornApparel[l].DrawWornExtras();
				}
			}
			Vector3 bodyLoc = rootLoc;
			bodyLoc.y += 39f / 980f;
			statusOverlays.RenderStatusOverlays(bodyLoc, quaternion, MeshPool.humanlikeHeadSet.MeshAt(headFacing));
		}

		private void DrawEquipment(Vector3 rootLoc)
		{
			if (pawn.Dead || !pawn.Spawned || pawn.equipment == null || pawn.equipment.Primary == null || (pawn.CurJob != null && pawn.CurJob.def.neverShowWeapon))
			{
				return;
			}
			Stance_Busy stance_Busy = pawn.stances.curStance as Stance_Busy;
			if (stance_Busy != null && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid)
			{
				Vector3 a = ((!stance_Busy.focusTarg.HasThing) ? stance_Busy.focusTarg.Cell.ToVector3Shifted() : stance_Busy.focusTarg.Thing.DrawPos);
				float num = 0f;
				if ((a - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
				{
					num = (a - pawn.DrawPos).AngleFlat();
				}
				Vector3 drawLoc = rootLoc + new Vector3(0f, 0f, 0.4f).RotatedBy(num);
				drawLoc.y += 9f / 245f;
				DrawEquipmentAiming(pawn.equipment.Primary, drawLoc, num);
			}
			else if (CarryWeaponOpenly())
			{
				if (pawn.Rotation == Rot4.South)
				{
					Vector3 drawLoc2 = rootLoc + new Vector3(0f, 0f, -0.22f);
					drawLoc2.y += 9f / 245f;
					DrawEquipmentAiming(pawn.equipment.Primary, drawLoc2, 143f);
				}
				else if (pawn.Rotation == Rot4.North)
				{
					Vector3 drawLoc3 = rootLoc + new Vector3(0f, 0f, -0.11f);
					drawLoc3.y += 0f;
					DrawEquipmentAiming(pawn.equipment.Primary, drawLoc3, 143f);
				}
				else if (pawn.Rotation == Rot4.East)
				{
					Vector3 drawLoc4 = rootLoc + new Vector3(0.2f, 0f, -0.22f);
					drawLoc4.y += 9f / 245f;
					DrawEquipmentAiming(pawn.equipment.Primary, drawLoc4, 143f);
				}
				else if (pawn.Rotation == Rot4.West)
				{
					Vector3 drawLoc5 = rootLoc + new Vector3(-0.2f, 0f, -0.22f);
					drawLoc5.y += 9f / 245f;
					DrawEquipmentAiming(pawn.equipment.Primary, drawLoc5, 217f);
				}
			}
		}

		public void DrawEquipmentAiming(Thing eq, Vector3 drawLoc, float aimAngle)
		{
			Mesh mesh = null;
			float num = aimAngle - 90f;
			if (aimAngle > 20f && aimAngle < 160f)
			{
				mesh = MeshPool.plane10;
				num += eq.def.equippedAngleOffset;
			}
			else if (aimAngle > 200f && aimAngle < 340f)
			{
				mesh = MeshPool.plane10Flip;
				num -= 180f;
				num -= eq.def.equippedAngleOffset;
			}
			else
			{
				mesh = MeshPool.plane10;
				num += eq.def.equippedAngleOffset;
			}
			num %= 360f;
			Material material = null;
			Graphic_StackCount graphic_StackCount = eq.Graphic as Graphic_StackCount;
			Graphics.DrawMesh(material: (graphic_StackCount == null) ? eq.Graphic.MatSingle : graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle, mesh: mesh, position: drawLoc, rotation: Quaternion.AngleAxis(num, Vector3.up), layer: 0);
		}

		private Material OverrideMaterialIfNeeded_NewTemp(Material original, Pawn pawn, bool portrait = false)
		{
			Material baseMat = ((!portrait && pawn.IsInvisible()) ? InvisibilityMatPool.GetInvisibleMat(original) : original);
			return graphics.flasher.GetDamagedMat(baseMat);
		}

		[Obsolete("Only need this overload to not break mod compatibility.")]
		private Material OverrideMaterialIfNeeded(Material original, Pawn pawn)
		{
			return OverrideMaterialIfNeeded_NewTemp(original, pawn);
		}

		private bool CarryWeaponOpenly()
		{
			if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null)
			{
				return false;
			}
			if (pawn.Drafted)
			{
				return true;
			}
			if (pawn.CurJob != null && pawn.CurJob.def.alwaysShowWeapon)
			{
				return true;
			}
			if (pawn.mindState.duty != null && pawn.mindState.duty.def.alwaysShowWeapon)
			{
				return true;
			}
			Lord lord = pawn.GetLord();
			if (lord != null && lord.LordJob != null && lord.LordJob.AlwaysShowWeapon)
			{
				return true;
			}
			return false;
		}

		public Rot4 LayingFacing()
		{
			if (pawn.GetPosture() == PawnPosture.LayingOnGroundFaceUp)
			{
				return Rot4.South;
			}
			if (pawn.RaceProps.Humanlike)
			{
				switch (pawn.thingIDNumber % 4)
				{
				case 0:
					return Rot4.South;
				case 1:
					return Rot4.South;
				case 2:
					return Rot4.East;
				case 3:
					return Rot4.West;
				}
			}
			else
			{
				switch (pawn.thingIDNumber % 4)
				{
				case 0:
					return Rot4.South;
				case 1:
					return Rot4.East;
				case 2:
					return Rot4.West;
				case 3:
					return Rot4.West;
				}
			}
			return Rot4.Random;
		}

		public float BodyAngle()
		{
			if (pawn.GetPosture() == PawnPosture.Standing)
			{
				return 0f;
			}
			Building_Bed building_Bed = pawn.CurrentBed();
			if (building_Bed != null && pawn.RaceProps.Humanlike)
			{
				Rot4 rotation = building_Bed.Rotation;
				rotation.AsInt += 2;
				return rotation.AsAngle;
			}
			if (pawn.Downed || pawn.Dead)
			{
				return wiggler.downedAngle;
			}
			if (pawn.RaceProps.Humanlike)
			{
				return LayingFacing().AsAngle;
			}
			Rot4 rot = Rot4.West;
			switch (pawn.thingIDNumber % 2)
			{
			case 0:
				rot = Rot4.West;
				break;
			case 1:
				rot = Rot4.East;
				break;
			}
			return rot.AsAngle;
		}

		public Vector3 BaseHeadOffsetAt(Rot4 rotation)
		{
			Vector2 headOffset = pawn.story.bodyType.headOffset;
			switch (rotation.AsInt)
			{
			case 0:
				return new Vector3(0f, 0f, headOffset.y);
			case 1:
				return new Vector3(headOffset.x, 0f, headOffset.y);
			case 2:
				return new Vector3(0f, 0f, headOffset.y);
			case 3:
				return new Vector3(0f - headOffset.x, 0f, headOffset.y);
			default:
				Log.Error("BaseHeadOffsetAt error in " + pawn);
				return Vector3.zero;
			}
		}

		public void Notify_DamageApplied(DamageInfo dam)
		{
			graphics.flasher.Notify_DamageApplied(dam);
			wiggler.Notify_DamageApplied(dam);
		}

		public void RendererTick()
		{
			wiggler.WigglerTick();
			effecters.EffectersTick();
		}

		public static bool RenderAsPack(Apparel apparel)
		{
			if (apparel.def.apparel.LastLayer.IsUtilityLayer)
			{
				if (apparel.def.apparel.wornGraphicData != null)
				{
					return apparel.def.apparel.wornGraphicData.renderUtilityAsPack;
				}
				return true;
			}
			return false;
		}

		private void DrawDebug()
		{
			if (DebugViewSettings.drawDuties && Find.Selector.IsSelected(pawn) && pawn.mindState != null && pawn.mindState.duty != null)
			{
				pawn.mindState.duty.DrawDebug(pawn);
			}
		}
	}
}
