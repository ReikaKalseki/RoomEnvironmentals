/*
 * Created by SharpDevelop.
 * User: Reika
 * Date: 04/11/2019
 * Time: 11:28 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;    //For data read/write methods
using System.Collections;   //Working with Lists and Collections
using System.Collections.Generic;   //Working with Lists and Collections
using System.Linq;   //More advanced manipulation of lists/collections
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;  //Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.
using ReikaKalseki.FortressCore;

namespace ReikaKalseki.RoomEnvironmentals {
	
	[HarmonyPatch(typeof(RoomController))]
	[HarmonyPatch("Link")]
	public static class RoomControllerPatch {
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			//FileLog.Log(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name+": running patch roomcontroller from trace "+System.Environment.StackTrace);
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			try {
				int start = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Stfld, typeof(RoomController), "mnMachinesInRoom")+1;
				object operand = codes[InstructionHandlers.getFirstOpcode(codes, start, OpCodes.Ldloc_S)].operand;
				int end = InstructionHandlers.getInstruction(codes, start, 0, OpCodes.Stfld, typeof(RoomController), "NumHeaters");
				FileLog.Log("Running link patch, which found anchors "+InstructionHandlers.toString(codes, start)+" and "+InstructionHandlers.toString(codes, end));
				//FileLog.Log("Codes are "+InstructionHandlers.toString(codes));
				if (end > start && end >= 0) {
					//InstructionHandlers.nullInstructions(codes, startAll, start);
					codes.RemoveRange(start, end-start+1);
					FileLog.Log("Deletion of range successful, injecting new instructions");
					List<CodeInstruction> inject = new List<CodeInstruction>();
					inject.Add(new CodeInstruction(OpCodes.Ldarg_0));
					inject.Add(new CodeInstruction(OpCodes.Ldloc_S, operand));
					inject.Add(InstructionHandlers.createMethodCall(typeof(RoomEnvironmentalsMod), "onRoomFindMachine", false, typeof(RoomController), typeof(SegmentEntity)));
					FileLog.Log("Injecting "+inject.Count+" instructions: "+InstructionHandlers.toString(inject));
					codes.InsertRange(start, inject);
				}
				FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
				//FileLog.Log("Codes are "+InstructionHandlers.toString(codes));
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
			return codes.AsEnumerable();
		}
	}
	
	[HarmonyPatch(typeof(RoomController))]
	[HarmonyPatch("CalcRoomRating")]
	public static class RoomControllerCalculatePatch {
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			try {
				int loc = InstructionHandlers.getLastInstructionBefore(codes, codes.Count, OpCodes.Stfld, typeof(RoomController), "mnVolume");
				FileLog.Log("Running patch, which found anchor "+InstructionHandlers.toString(codes, loc));
				List<CodeInstruction> inject = new List<CodeInstruction>();
				inject.Add(new CodeInstruction(OpCodes.Ldarg_0));
				inject.Add(InstructionHandlers.createMethodCall(typeof(RoomEnvironmentalsMod), "onRoomCalculateEnvironment", false, typeof(int), typeof(RoomController)));
				FileLog.Log("Injecting "+inject.Count+" instructions: "+InstructionHandlers.toString(inject));
				codes.InsertRange(loc, inject);
				FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
				//FileLog.Log("Codes are "+InstructionHandlers.toString(codes));
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
			return codes.AsEnumerable();
		}
	}
	
	[HarmonyPatch(typeof(Room_Enviro))]
	[HarmonyPatch(MethodType.Constructor, new Type[] {typeof(Segment), typeof(long), typeof(long), typeof(long), typeof(ushort), typeof(byte), typeof(ushort), typeof(bool)})]
	public static class RoomEnviroPPSPatch {
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			try {
				int start = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Stfld, typeof(MachineEntity), "mValue")+1;
				int end = InstructionHandlers.getInstruction(codes, start, 0, OpCodes.Ret)-1;
				FileLog.Log("Running PPS patch, which found anchors "+InstructionHandlers.toString(codes, start)+" and "+InstructionHandlers.toString(codes, end));
				if (end > start && end >= 0) {
					codes.RemoveRange(start, end-start+1);
					FileLog.Log("Deletion of range successful, injecting new instructions");
					List<CodeInstruction> inject = new List<CodeInstruction>();
					inject.Add(new CodeInstruction(OpCodes.Ldarg_0));
					inject.Add(InstructionHandlers.createMethodCall(typeof(RoomEnvironmentalsMod), "onRoomEnviroPPSCalculation", false, typeof(Room_Enviro)));
					FileLog.Log("Injecting "+inject.Count+" instructions: "+InstructionHandlers.toString(inject));
					codes.InsertRange(start, inject);
				}
				FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
			return codes.AsEnumerable();
		}
	}
	
	[HarmonyPatch(typeof(Room_Enviro))]
	[HarmonyPatch("LowFrequencyUpdate")]
	public static class RoomEnviroPPSUsePatch {
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			try {
				int start = 0;
				FileLog.Log("Running patch, which found anchor "+InstructionHandlers.toString(codes, start));
				List<CodeInstruction> inject = new List<CodeInstruction>();
				inject.Add(new CodeInstruction(OpCodes.Ldarg_0));
				inject.Add(InstructionHandlers.createMethodCall(typeof(RoomEnvironmentalsMod), "onRoomEnviroPPSCost", false, typeof(Room_Enviro)));
				FileLog.Log("Injecting "+inject.Count+" instructions: "+InstructionHandlers.toString(inject));
				codes.InsertRange(start, inject);
				FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
			return codes.AsEnumerable();
		}
	}
	
	[HarmonyPatch(typeof(ConveyorEntity))]
	[HarmonyPatch("CalcPenaltyFactor")]
	public static class BeltPatch {
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			try {
				int loc = InstructionHandlers.getInstruction(codes, 0, 1, OpCodes.Stfld, typeof(ConveyorEntity), "mnPenaltyFactor");
				FileLog.Log("Running patch, which found anchor "+InstructionHandlers.toString(codes, loc));
				if (loc >= 0) {
					List<CodeInstruction> inject = new List<CodeInstruction>();
					inject.Add(new CodeInstruction(OpCodes.Ldarg_0));
					inject.Add(InstructionHandlers.createMethodCall(typeof(RoomEnvironmentalsMod), "onBeltReactToEnvironment", false, typeof(int), typeof(ConveyorEntity)));
					//inject.Add(new CodeInstruction(OpCodes.Stfld, InstructionHandlers.convertFieldOperand("ConveyorEntity", "mnPenaltyFactor")));
					FileLog.Log("Injecting "+inject.Count+" instructions: "+InstructionHandlers.toString(inject));
					codes.InsertRange(loc, inject);
				}
				FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
				//FileLog.Log("Codes are "+InstructionHandlers.toString(codes));
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
			return codes.AsEnumerable();
		}
	}
	
	[HarmonyPatch(typeof(SurvivalGrapplingHook))]
	[HarmonyPatch("Update")]
	public static class GrappleRoomBypass {
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			try {
				FileLog.Log("Running patch "+MethodBase.GetCurrentMethod().DeclaringType);
				FieldInfo set = InstructionHandlers.convertFieldOperand(typeof(SurvivalGrapplingHook), "mrGrappleDebounce");
				for (int i = 0; i < codes.Count; i++) {
					CodeInstruction ci = codes[i];
					if (ci.opcode == OpCodes.Stfld && InstructionHandlers.matchOperands(ci.operand, set)) {
						FileLog.Log("Found match at pos "+InstructionHandlers.toString(codes, i));
						codes.Insert(i, InstructionHandlers.createMethodCall(typeof(RoomEnvironmentalsMod), "getGrappleCooldownFromRoom", false, new Type[]{typeof(float)}));
						i += 2;
					}
				}
				FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
			return codes.AsEnumerable();
		}
	}
	
	[HarmonyPatch(typeof(PyrothermicGenerator))]
	[HarmonyPatch("LowFrequencyUpdate")]
	public static class PTGUpdateHook {
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			try {
				FileLog.Log("Running patch "+MethodBase.GetCurrentMethod().DeclaringType);
				InstructionHandlers.patchInitialHook(codes, new CodeInstruction(OpCodes.Ldarg_0), InstructionHandlers.createMethodCall(typeof(RoomEnvironmentalsMod), "tickPTG", false, new Type[]{typeof(PyrothermicGenerator)}));
				FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
			return codes.AsEnumerable();
		}
	}
	
	[HarmonyPatch(typeof(OreSmelter))]
	[HarmonyPatch("LowFrequencyUpdate")]
	public static class SmelterUpdateHook {
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			try {
				FileLog.Log("Running patch "+MethodBase.GetCurrentMethod().DeclaringType);
				InstructionHandlers.patchInitialHook(codes, new CodeInstruction(OpCodes.Ldarg_0), InstructionHandlers.createMethodCall(typeof(RoomEnvironmentalsMod), "tickSmelter", false, new Type[]{typeof(OreSmelter)}));
				FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
			return codes.AsEnumerable();
		}
	}
	
	[HarmonyPatch(typeof(ForcedInduction))]
	[HarmonyPatch("LowFrequencyUpdate")]
	public static class ForcedInductionDeepCheckHook {
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
			try {
				FileLog.Log("Running patch "+MethodBase.GetCurrentMethod().DeclaringType);
				int idx = InstructionHandlers.getInstruction(codes, 0, 0, OpCodes.Callvirt, typeof(OreSmelterInterface), "SetSmelterSupportsTier4", true, new Type[]{typeof(bool)});
				codes.InsertRange(idx+1, new List<CodeInstruction>(){
					new CodeInstruction(OpCodes.Ldarg_0),
					InstructionHandlers.createMethodCall(typeof(RoomEnvironmentalsMod), "checkForcedInductionDepth", false, new Type[]{typeof(ForcedInduction)})
				});
				FileLog.Log("Done patch "+MethodBase.GetCurrentMethod().DeclaringType);
			}
			catch (Exception e) {
				FileLog.Log("Caught exception when running patch "+MethodBase.GetCurrentMethod().DeclaringType+"!");
				FileLog.Log(e.Message);
				FileLog.Log(e.StackTrace);
				FileLog.Log(e.ToString());
			}
			return codes.AsEnumerable();
		}
	}
	
}
