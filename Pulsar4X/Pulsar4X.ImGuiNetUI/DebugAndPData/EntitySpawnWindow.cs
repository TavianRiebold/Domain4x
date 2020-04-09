﻿using ImGuiNET;
using Pulsar4X.ECSLib;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System;
using Pulsar4X.ECSLib.ComponentFeatureSets;


namespace Pulsar4X.SDL2UI
{
    public class EntitySpawnWindow : PulsarGuiWindow
    {
        private List<ShipDesign> _exsistingClasses;
        private string[] _entitytypes = new string[]{ "Ship", "Planet" };
        private int _entityindex;


        private EntitySpawnWindow()
	    {
	        _flags = ImGuiWindowFlags.AlwaysAutoResize;
        
        }

        internal static EntitySpawnWindow GetInstance() {

            EntitySpawnWindow thisItem;
            if (!_state.LoadedWindows.ContainsKey(typeof(EntitySpawnWindow)))
            {
                thisItem = new EntitySpawnWindow();
            }
            else
            {
                thisItem = (EntitySpawnWindow)_state.LoadedWindows[typeof(EntitySpawnWindow)];
            }
             

            return thisItem;


        }
        //displays selected entity info
        internal override void Display()
        {
           
            if (IsActive && ImGui.Begin("Spawn Entity", _flags))
            {


                
                if (ImGui.Combo("##entityselector", ref _entityindex, _entitytypes, _entitytypes.Length)) 
                { 

                }


                if (_entitytypes[_entityindex] == "Ship") 
                {
                    //ImGui.BeginChild("exsistingdesigns");

                    if (_exsistingClasses == null || _exsistingClasses.Count != _state.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList().Count)
                    {
                        _exsistingClasses = _state.Faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.ToList();
                    }

                    for (int i = 0; i < _exsistingClasses.Count; i++)
                    {

                        string name = _exsistingClasses[i].Name;
                        if (ImGui.Selectable(name))
                        {

                            Entity _spawnedship = ShipFactory.CreateShip(_exsistingClasses[i], _state.Faction, _state.LastClickedEntity.Entity, _state.SelectedSystem, Guid.NewGuid().ToString());
                            NewtonionMovementProcessor.CalcDeltaV(_spawnedship);
                            //_state.SelectedSystem.SetDataBlob(_spawnedship.ID, new TransitableDB());
                            //var rp1 = NameLookup.GetMaterialSD(game, "LOX/Hydrocarbon");
                            //StorageSpaceProcessor.AddCargo(_spawnedship.GetDataBlob<CargoStorageDB>(), rp1, 15000);
                        }
                    }

                    //ImGui.EndChild();
                }





            }

            
        }

    }
}
