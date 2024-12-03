using UnityEngine;  //Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.
using System.IO;    //For data read/write methods
using System;    //For data read/write methods
using System.Collections.Generic;   //Working with Lists and Collections
using System.Linq;   //More advanced manipulation of lists/collections
using Harmony;
using ReikaKalseki.FortressCore;

namespace ReikaKalseki.RoomEnvironmentals
{
  public class RoomEnvironmentalsMod : FCoreMod
  {
    public const string MOD_KEY = "ReikaKalseki.RoomEnvironmentals";
    public const string CUBE_KEY = "ReikaKalseki.RoomEnvironmentals_Key";
    //public static ushort ModCubeType;
    
    private const int HEATER_BASE_POWER = 100;
    private const int COOLER_BASE_POWER = 200;
    private const int VAPOR_BASE_POWER = 10;
    
    private const float HEATER_POWER_FRACTION = 0.12F;
    private const float COOLER_POWER_FRACTION = 0.4F;
    private const float VAPOR_POWER_FRACTION = 1F;
    
    private const float HEATER_FACTOR = /*20*/16*HEATER_POWER_FRACTION;
    private const float COOLER_FACTOR = /*15*/12*COOLER_POWER_FRACTION;
    private const float VAPOR_FACTOR = 1*VAPOR_POWER_FRACTION;
    
    private const float SMELTER_HEATER_VALUE = 0.2F;//0.5F;//1F;
    private const float BLAST_FURNACE_HEATER_VALUE = 2.5F;//3F;//5;
    private const float BLAST_BASIN_HEATER_VALUE = 0.5F;//1;//2;
    private const float C5_HEATER_VALUE = 24F;//18F;//12F;//10;//8;
    private const float GEO_HEATER_VALUE = 0.05F; //tiny because applies for each block 
    
    private const float VENT_O2_FRACTION = 0.2F;
    
    private static readonly Dictionary<RoomController, RoomMachineCache> roomCache = new Dictionary<RoomController, RoomMachineCache>();
    private static readonly Dictionary<Coordinate, RoomMachineCache> machineRoomLookup = new Dictionary<Coordinate, RoomMachineCache>();
    
    public RoomEnvironmentalsMod() : base("RoomEnvironmentals") {
    	
    }

    protected override void loadMod(ModRegistrationData registrationData) {
        runHarmony();
    }
    
    public static float getGrappleCooldownFromRoom(float orig) {
    	LocalPlayerScript p = WorldScript.instance.localPlayerInstance;
    	return isRoomProtected(p) ? Math.Min(orig, 0.1F) : orig;
    }
    
    private static bool isRoomProtected(LocalPlayerScript ep) {
    	if (CCCCC.ActiveAndWorking && Math.Abs(CCCCC.CCCCC_X-ep.mWorldX)+Math.Abs(CCCCC.CCCCC_Y-ep.mWorldY)+Math.Abs(CCCCC.CCCCC_Z-ep.mWorldZ) <= 24)
    		return true;
    	/*
    	int id = ep.CurrentRoomID;
    	if (id < 0)
    		return false;
    	RoomController room = tryFindRoomByID(id);
    	if (room == null)
    		return false;
    	long num = room.mnY - 4611686017890516992L;
		if (num < (long)BiomeLayer.CavernColdCeiling && num > (long)BiomeLayer.CavernColdFloor) {
    		return room.mrHeatModulation >= 1;
    	}
		if (num < (long)BiomeLayer.CavernMagmaCeiling && num > (long)BiomeLayer.CavernMagmaFloor) {
    		return room.mrHeatModulation <= -1;
    	}
		if (num < (long)BiomeLayer.CavernToxicCeiling && num > (long)BiomeLayer.CavernToxicFloor) {
    		return room.mrToxicRating >= 1;
    	}
    	return false;
    	*/
    	return ep.CooledRoomLatch > 0.2 || ep.HeatedRoomLatch > 0.2 || ep.ToxicRoomLatch > 0.2;
    }
    
    public static void onRoomFindMachine(RoomController c, SegmentEntity e) {
		if (e.mnRoomID == -1 && e.mRoomController == c)
			e.mRoomController = null;
    	
    	//FUtil.log("Room controller @ "+new Coordinate(c)+" found entity "+e.GetType().Name+" @ "+new Coordinate(e));
    	
    	RoomMachineCache cache = getOrCreateCache(c);
    	bool cacheRoomMachine = false;
    	switch(e.mType) {
    		case eSegmentEntity.Room_Enviro:
	    		Room_Enviro re = e as Room_Enviro;
	    		if (re.ActiveAndWorking) {
					if (re.mValue == (ushort) 0)
						cache.heaterPower += HEATER_FACTOR;
					if (re.mValue == (ushort) 1)
						cache.coolerPower += COOLER_FACTOR;
					if (re.mValue == (ushort) 2)
						cache.vaporPower += VAPOR_FACTOR;
	    		}
	    		cacheRoomMachine = true;
    		break;
    		case eSegmentEntity.CCCCC:
	    		CCCCC c5 = e as CCCCC;
	    		if (c5.mbIsCenter && c5.mMBMState == MachineEntity.MBMState.Linked && CCCCC.ActiveAndWorking) {
	    			cache.heaterPower += HEATER_FACTOR*C5_HEATER_VALUE;
	    		}
	    	break;
	    	case eSegmentEntity.BlastFurnace:
	    		BlastFurnace f = e as BlastFurnace;
	    		if (f.mOperatingState == BlastFurnace.OperatingState.Smelting || f.mOperatingState == BlastFurnace.OperatingState.WaitingOnBasin) {
	    			cache.heaterPower += HEATER_FACTOR*BLAST_FURNACE_HEATER_VALUE;
	    		}
	    		cacheRoomMachine = true;
	    	break;
	    	case eSegmentEntity.OreSmelter:
	    		OreSmelter s = e as OreSmelter;
	    		if (s.mrTemperature > 5 && s.mrTargetTemp > 100) {
	    			cache.heaterPower += HEATER_FACTOR*SMELTER_HEATER_VALUE*s.mrTemperature/s.mrTargetTemp;
	    		}
	    		cacheRoomMachine = true;
	    	break;
	    	case eSegmentEntity.ForcedInduction:
	    		ForcedInduction ind = e as ForcedInduction;
	    		
	    		cacheRoomMachine = true;
	    	break;
	    	case eSegmentEntity.ContinuousCastingBasin:
	    		if ((e as ContinuousCastingBasin).mMBMState == MachineEntity.MBMState.Linked) {
	    			cache.heaterPower += HEATER_FACTOR*BLAST_BASIN_HEATER_VALUE;
	    		}
	    		cacheRoomMachine = true;
	    	break;
	    	case eSegmentEntity.GeothermalGenerator:
	    		GeothermalGenerator gen = e as GeothermalGenerator;
	    		if (gen.mMBMState == MachineEntity.MBMState.Linked) {
	    			long y = gen.mShaftEndY - WorldUtil.COORD_OFFSET;
	    			if (y <= -1000) {
	    				float hf = Math.Min(1F, (Math.Abs(y)-1000)/120F);
		    			cache.heaterPower += HEATER_FACTOR*GEO_HEATER_VALUE*hf;
	    			}
	    		}
	    		cacheRoomMachine = true;
	    	break;
	    	case eSegmentEntity.Conveyor:
	    		ConveyorEntity belt = e as ConveyorEntity;
	    		//cache.addBelt(e as ConveyorEntity);
	    		//Coordinate loc = new Coordinate(e);
	    		cacheRoomMachine = true;
	    	break;
	    	case eSegmentEntity.PyrothermicGenerator:
	    		PyrothermicGenerator pgen = e as PyrothermicGenerator;
	    		//cache.addBelt(e as ConveyorEntity);
	    		//Coordinate loc = new Coordinate(e);
	    		cacheRoomMachine = true;
	    	break;
    	}
    	if (cacheRoomMachine) {
    		machineRoomLookup[new Coordinate(e)] = cache;
    	}
    	//if (significant) do not do this, it blows up the logs
    	//	FUtil.log("Room controller @ "+new Coordinate(c)+" found "+Enum.GetName(typeof(eSegmentEntity), e)+" "+e+" and now has "+cache);
    }
    
    public static void tickPTG(PyrothermicGenerator gen) {
    	float o2 = getO2StarvationFactor(gen);
    	if (o2 < 1) {
    		float e = computePTGEfficiency(gen);
	    	if (e < 1) {
    			float newE = 1-((1-e)*o2);
    			gen.mrEfficiency = newE;
    			float scale = newE;
				if (DifficultySettings.mbCasualResource) //from PTG constructor
					scale *= 3F;
				if (DifficultySettings.mbEasyPower)
					scale *= 2.5F;
				if (DifficultySettings.mbRushMode)
					scale *= 5F;
				gen.mrPowerGenerationScalar = scale;
	    	}
    	}
    }
    
    public static void tickSmelter(OreSmelter furn) {
    	int y = (int)(furn.mnY-WorldUtil.COORD_OFFSET);
    	if (y < -40) {
	    	bool air = getO2StarvationFactor(furn) < 0.1F;
	    	furn.mbTooDeep = !air;
	    	if (air) {
	    		float time = 15;
				if (DifficultySettings.mbCasualResource) //from OreSmelter constructor
					time = 5;
				if (DifficultySettings.mbRushMode)
					time = 3;
				if (furn.mValue == 1)
					time /= 2;
				furn.mrSmeltTime = time;
	    	}
	    	else {
	    		furn.mrSmeltTime = 1024;
	    	}
    	}
    }
    
    public static void checkForcedInductionDepth(ForcedInduction duc) {
    	int y = (int)(duc.mnY-WorldUtil.COORD_OFFSET);
    	if (y < -32)
    		duc.mbTooDeep = getO2StarvationFactor(duc) >= 0.1F;
    }
    
    public static float computePTGEfficiency(SegmentEntity e) {
    	return e.mnY;
    }
    
    public static float computePTGEfficiency(long y) {
    	y -= WorldUtil.COORD_OFFSET;
		if (y < 0) {
			y += 25L;
			float num = ((y*3)+100)/100F;
			return Mathf.Clamp(num, 0.025F, 1);
		}
    	return 1;
    }
    
    public static int onRoomCalculateEnvironment(int originalVolume, RoomController c) {
    	RoomMachineCache cache = getOrCreateCache(c);
    	//FUtil.log("Room controller @ "+new Coordinate(c)+" loaded "+cache);
    	
    	c.NumHeaters = (int)(cache.heaterPower);
    	c.NumCoolers = (int)(cache.coolerPower);
    	c.NumMoistureEmitters = (int)(cache.vaporPower);
    	//c.NumFilters = (int)Math.Round(cache.filterPower);
    	
    	//foreach (ConveyorEntity e in cache.getBelts()) {
    	//	
    	//}
    	
    	cache.volume = originalVolume;
    	cache.area = getSurfaceArea(c);
    	if (cache.setChanging(isStillChangingTemp(c))) {
    		FUtil.log("Room controller @ "+new Coordinate(c)+" with "+cache+" just changed state; all enviros now have a power ratio of "+cache.getPowerRatio());
    	}
    	cache.reset();
    	
    	return originalVolume;
    }
    
    private static bool isStillChangingTemp(RoomController c) {
    	return c.mrHeatModulation > -1 && c.mrHeatModulation < 1;
    }
    
    private static int getScrubberCount(RoomController c) { //see ScanWall @ L930
    	return c.NumFans;
    }
    
    private static int getVentCount(RoomController c) { //see ScanWall @ L930
    	return c.NumFilters;
    }
    
    public static float getO2StarvationFactor(MachineEntity e) {
    	RoomMachineCache cache;
    	Coordinate cc = new Coordinate(e);
    	if (machineRoomLookup.TryGetValue(cc, out cache)) {
    		RoomController c = cache.controller;
    		if (c != null) {
    			return Mathf.Max(0, 1-c.mrCleanRating/*getVentCount(c)*VENT_O2_FRACTION*/);
    		}
    	}
    	return 1;
    }
    
    private static int getSurfaceArea(RoomController c) {
    	int sizeX = -c.mRoomExtentXNeg + c.mRoomExtentXPlus - 1;
		int sizeY = -c.mRoomExtentYNeg + c.mRoomExtentYPlus - 1;
		int sizeZ = -c.mRoomExtentZNeg + c.mRoomExtentZPlus - 1;
		return sizeX*sizeY*2+sizeZ*sizeY*2+sizeX*sizeZ*2;
    }
    
    public static int onBeltReactToEnvironment(int original, ConveyorEntity e) {
    	RoomMachineCache cache;
    	Coordinate c = new Coordinate(e);
    	if (machineRoomLookup.TryGetValue(c, out cache)) {
	    	if (cache.controller != null && cache.controller.mrHeatModulation >= 1) {
			   	e.mbConveyorFrozen = false;
			   	e.mnCurrentPenaltyFactor = 0;
			   	return 0;
	    	}
    	}
    	return original;
    }
    
    public static void onRoomEnviroPPSCalculation(Room_Enviro e) {
    	e.PPS = getRoomEnviroPPS(e);
    	FUtil.log("Overriding room enviro @ "+new Coordinate(e)+" to base consumption "+e.PPS+" PPS");
    }
    
    public static void onRoomEnviroPPSCost(Room_Enviro e) {
    	RoomMachineCache cache;
    	Coordinate c = new Coordinate(e);
    	if (machineRoomLookup.TryGetValue(c, out cache)) {
    		e.PPS = getRoomEnviroPPS(e)*cache.getPowerRatio();
	    	//FUtil.log("Overriding room enviro @ "+new Coordinate(e)+" to dynamic consumption "+e.PPS+" PPS");
    	}
    }
    
    private static float getRoomEnviroPPS(Room_Enviro e) {
    	switch(e.mValue) {
    		case 0: //heater
    			return HEATER_BASE_POWER*HEATER_POWER_FRACTION;
    		case 1: //cooler
    			return COOLER_BASE_POWER*COOLER_POWER_FRACTION;
    		case 2: //vapor
    			return VAPOR_BASE_POWER*VAPOR_POWER_FRACTION;
    		default:
    			return e.PPS;
    	}
    }
    
    private static RoomController getRoomAt(RoomController e) {
    	RoomMachineCache get = null;
    	if (roomCache.TryGetValue(e, out get)) {
    		return get != null ? get.controller : null;
    	}
    	return null;
    }
    
    private static RoomMachineCache getOrCreateCache(RoomController c) {
    	RoomMachineCache get = null;
    	//Coordinate loc = new Coordinate(c);
    	if (!roomCache.TryGetValue(c, out get)) {
    		FUtil.log("Creating new cache for room @ "+new Coordinate(c));
    		get = new RoomMachineCache(c);
    		roomCache.Add(c, get);
    	}
    	return get;
    }

  }
}
