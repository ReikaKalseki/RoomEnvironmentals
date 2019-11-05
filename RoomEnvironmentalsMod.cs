﻿using UnityEngine;  //Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.
using System.IO;    //For data read/write methods
using System;    //For data read/write methods
using System.Collections.Generic;   //Working with Lists and Collections
using System.Linq;   //More advanced manipulation of lists/collections
using Harmony;
using ReikaKalseki;

namespace ReikaKalseki.RoomEnvironmentals
{
  public class RoomEnvironmentalsMod : FortressCraftMod
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
    
    private const float HEATER_FACTOR = 20*HEATER_POWER_FRACTION;
    private const float COOLER_FACTOR = 15*COOLER_POWER_FRACTION;
    private const float VAPOR_FACTOR = 1*VAPOR_POWER_FRACTION;
    
    private const float BLAST_FURNACE_HEATER_VALUE = 2.5F;//3F;//5;
    private const float BLAST_BASIN_HEATER_VALUE = 0.5F;//1;//2;
    private const float C5_HEATER_VALUE = 12F;//10;//8;
    
    private static readonly Dictionary<RoomController, RoomMachineCache> roomCache = new Dictionary<RoomController, RoomMachineCache>();
    private static readonly Dictionary<ConveyorEntity, RoomController> beltRooms = new Dictionary<ConveyorEntity, RoomController>();
    private static readonly Dictionary<Room_Enviro, RoomController> enviroRooms = new Dictionary<Room_Enviro, RoomController>();

    public override ModRegistrationData Register()
    {
        ModRegistrationData registrationData = new ModRegistrationData();
        //registrationData.RegisterEntityHandler(MOD_KEY);
        /*
        TerrainDataEntry entry;
        TerrainDataValueEntry valueEntry;
        TerrainData.GetCubeByKey(CUBE_KEY, out entry, out valueEntry);
        if (entry != null)
          ModCubeType = entry.CubeType;
         */        
        var harmony = HarmonyInstance.Create("ReikaKalseki.RoomEnvironmentals");
        HarmonyInstance.DEBUG = true;
        FileLog.Log("Ran mod register, started harmony (harmony log)");
        Debug.Log("Ran mod register, started harmony");
        try {
			harmony.PatchAll();
        }
        catch (Exception e) {
			FileLog.Log("Caught exception when running patcher!");
			FileLog.Log(e.Message);
			FileLog.Log(e.StackTrace);
			FileLog.Log(e.ToString());
        }
        
        return registrationData;
    }
    
    public static void onRoomFindMachine(RoomController c, SegmentEntity e) {
    	//Debug.Log("Room controller @ "+new Coordinate(c)+" found entity "+e.GetType().Name+" @ "+new Coordinate(e));
    	RoomMachineCache cache = getOrCreateCache(c);
    	if (e.mType == eSegmentEntity.Room_Enviro) {
    		Room_Enviro re = e as Room_Enviro;
    		if (re.ActiveAndWorking) {
				if (re.mValue == (ushort) 0)
					cache.heaterPower += HEATER_FACTOR;
				if (re.mValue == (ushort) 1)
					cache.coolerPower += COOLER_FACTOR;
				if (re.mValue == (ushort) 2)
					cache.vaporPower += VAPOR_FACTOR;
    		}
    		enviroRooms.Remove(re);
    		enviroRooms.Add(re, cache.controller);
    	}
    	else if (e.mType == eSegmentEntity.CCCCC) {
    		CCCCC c5 = e as CCCCC;
    		if (c5.mbIsCenter && c5.mMBMState == MachineEntity.MBMState.Linked && CCCCC.ActiveAndWorking) {
    			cache.heaterPower += HEATER_FACTOR*C5_HEATER_VALUE;
    		}
    	}
    	else if (e.mType == eSegmentEntity.BlastFurnace) {
    		BlastFurnace f = e as BlastFurnace;
    		if (f.mOperatingState == BlastFurnace.OperatingState.Smelting || f.mOperatingState == BlastFurnace.OperatingState.WaitingOnBasin) {
    			cache.heaterPower += HEATER_FACTOR*BLAST_FURNACE_HEATER_VALUE;
    		}
    	}
    	else if (e.mType == eSegmentEntity.ContinuousCastingBasin && (e as ContinuousCastingBasin).mMBMState == MachineEntity.MBMState.Linked) {
    		cache.heaterPower += HEATER_FACTOR*BLAST_BASIN_HEATER_VALUE;
    	}
    	else if (e.mType == eSegmentEntity.Conveyor) {
    		ConveyorEntity belt = e as ConveyorEntity;
    		//cache.addBelt(e as ConveyorEntity);
    		//Coordinate loc = new Coordinate(e);
    		beltRooms.Remove(belt);
    		beltRooms.Add(belt, cache.controller);
    	}
    	
    	//Debug.Log("Room controller @ "+new Coordinate(c)+" has "+cache);
    }
    
    public static int onRoomCalculateEnvironment(int originalVolume, RoomController c) {
    	RoomMachineCache cache = getOrCreateCache(c);
    	//Debug.Log("Room controller @ "+new Coordinate(c)+" loaded "+cache);
    	
    	float f = 1;//isStillChangingTemp(c) ? 1 : originalVolume/(float)getSurfaceArea(c);
    	
    	//Debug.Log("Room controller @ "+new Coordinate(c)+" loaded "+cache+" multiplied against "+f+" since "+isStillChangingTemp(c));
    	
    	c.NumHeaters = (int)(cache.heaterPower*f);
    	c.NumCoolers = (int)(cache.coolerPower*f);
    	c.NumMoistureEmitters = (int)(cache.vaporPower*f);
    	//c.NumFilters = (int)Math.Round(cache.filterPower);
    	
    	//foreach (ConveyorEntity e in cache.getBelts()) {
    	//	
    	//}
    	
    	cache.volume = originalVolume;
    	cache.area = getSurfaceArea(c);
    	if (cache.setChanging(isStillChangingTemp(c))) {
    		Debug.Log("Room controller @ "+new Coordinate(c)+" with "+cache+" just changed state; all enviros now have a power ratio of "+cache.getPowerRatio());
    	}
    	cache.reset();
    	
    	return originalVolume;//isStillChangingTemp(c) ? originalVolume : getSurfaceArea(c);
    }
    
    private static bool isStillChangingTemp(RoomController c) {
    	return c.mrHeatModulation > -1 && c.mrHeatModulation < 1;
    }
    
    private static int getSurfaceArea(RoomController c) {
    	int sizeX = -c.mRoomExtentXNeg + c.mRoomExtentXPlus - 1;
		int sizeY = -c.mRoomExtentYNeg + c.mRoomExtentYPlus - 1;
		int sizeZ = -c.mRoomExtentZNeg + c.mRoomExtentZPlus - 1;
		return sizeX*sizeY*2+sizeZ*sizeY*2+sizeX*sizeZ*2;
    }
    
    public static int onBeltReactToEnvironment(int original, ConveyorEntity e) {
    	//Coordinate loc = null;
    	RoomController rc = null;
    	if (beltRooms.TryGetValue(e, out rc)) {
	    	if (rc != null && rc.mrHeatModulation >= 1) {
			   	e.mbConveyorFrozen = false;
			   	e.mnCurrentPenaltyFactor = 0;
			   	//e.mbConveyorToxic = false;
			   	//e.mbHoloDirty = true;
			   	return 0;
	    	}
    	}
    	return original;
    }
    
    public static void onRoomEnviroPPSCalculation(Room_Enviro e) {
    	e.PPS = getRoomEnviroPPS(e);
    	Debug.Log("Overriding room enviro @ "+new Coordinate(e)+" to base consumption "+e.PPS+" PPS");
    }
    
    public static void onRoomEnviroPPSCost(Room_Enviro e) {
    	RoomController rc = null;
	   	if (enviroRooms.TryGetValue(e, out rc)) {
    		RoomMachineCache cache = getOrCreateCache(rc);
    		e.PPS = getRoomEnviroPPS(e)*cache.getPowerRatio();
	    	//Debug.Log("Overriding room enviro @ "+new Coordinate(e)+" to base consumption "+e.PPS+" PPS");
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
    		Debug.Log("Creating new cache for room @ "+new Coordinate(c));
    		get = new RoomMachineCache(c);
    		roomCache.Add(c, get);
    	}
    	return get;
    }

  }
}