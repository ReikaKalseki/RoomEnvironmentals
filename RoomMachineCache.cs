using UnityEngine;  //Needed for most Unity Enginer manipulations: Vectors, GameObjects, Audio, etc.
using System.IO;    //For data read/write methods
using System;    //For data read/write methods
using System.Collections.Generic;   //Working with Lists and Collections
using System.Linq;   //More advanced manipulation of lists/collections
using Harmony;
using ReikaKalseki;

namespace ReikaKalseki.RoomEnvironmentals
{
	/// <summary>
	/// Description of RoomMachineCache.
	/// </summary>
	public class RoomMachineCache
	{
		
		internal float heaterPower = 0;
		internal float coolerPower = 0;
		internal float vaporPower = 0;
		internal float filterPower = 0;
		
		//private List<Coordinate> belts = new List<Coordinate>();
		
		public readonly Coordinate location;
		public readonly RoomController controller;
		
		internal int area;
		internal int volume;
		private float powerRatio;
		
		private bool isChanging;
		private bool lastChanging;
		
		private bool everCalculated;
		
		public RoomMachineCache(RoomController c) {
			controller = c;
			location = new Coordinate(c);
		}
		/*
		public void addBelt(ConveyorEntity e) {
			belts.Add(new Coordinate(e));
		}*/
		
		public bool setChanging(bool changing) {
			bool flag = !everCalculated;
			everCalculated = true;
			lastChanging = isChanging;
			isChanging = changing;
			powerRatio = isChanging ? 1 : Math.min(area/(float)volume, 1F);
			return flag || lastChanging != changing;
		}
		
		public float getPowerRatio() {
			return powerRatio;
		}
		
		public void reset() {
			//belts.Clear();
			
			heaterPower = 0;
			coolerPower = 0;
			filterPower = 0;
			vaporPower = 0;
		}
		
		public override string ToString() {
			return "["+heaterPower+"/"+coolerPower+"/"+vaporPower+"] ["+volume+"/"+area+"] @ "+location.ToString();
		} 
	}
}
