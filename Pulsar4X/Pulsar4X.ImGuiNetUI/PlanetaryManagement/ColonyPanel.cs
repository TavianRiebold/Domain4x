﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.Industry;
using Pulsar4X.ImGuiNetUI.EntityManagement;
using Vector2 = System.Numerics.Vector2;


namespace Pulsar4X.SDL2UI
{
    public class ColonyPanel : PulsarGuiWindow
    {
        EntityState _selectedEntity;
        //CargoStorageVM _storeVM;
        private FactionInfoDB _factionInfoDB;

        private IndustryAbilityDB _industryDB;
        private IndustryPannel2 _industryPannel;

        CargoListPannelSimple _cargoList;
        StaticDataStore _staticData;
        private ColonyPanel(StaticDataStore staticData, EntityState selectedEntity)
        {
            _selectedEntity = selectedEntity;
            _cargoList = new CargoListPannelSimple(staticData, selectedEntity);
            _staticData = staticData;
            
        }

        public static ColonyPanel GetInstance(StaticDataStore staticData, EntityState selectedEntity)
        {
            ColonyPanel instance;
            if (selectedEntity.CmdRef == null)
               selectedEntity.CmdRef = CommandReferences.CreateForEntity(_uiState.Game, selectedEntity.Entity);
            if (!_uiState.LoadedWindows.ContainsKey(typeof(ColonyPanel)))
            {
                instance = new ColonyPanel(staticData, selectedEntity);
                instance.HardRefresh();
                return instance;
            }
            instance = (ColonyPanel)_uiState.LoadedWindows[typeof(ColonyPanel)];
            if (instance._selectedEntity != selectedEntity)
            {
                instance._selectedEntity = selectedEntity;
                instance.HardRefresh();


            }


            return instance;
        }

        internal override void Display()
        {

            //resources list include refined or seperate?
            //factories/installations list - expandable to show health and disable/enable specific installations
            //mining stats pannel. 
            //refinary panel, expandable?
            //construction pannel, expandable?
            //constructed but not installed components. 
            //installation pannel (install constructed components
            if (IsActive)
            {
                //_flags = ImGuiWindowFlags.AlwaysAutoResize;
                if (ImGui.Begin("Industry", ref IsActive, _flags))
                {
                    ImGui.BeginTabBar("IndustryTabs");

                    
                    if (ImGui.BeginTabItem("Overview"))
                    {
                        OverViewPannel.Display(_selectedEntity);
                        ImGui.EndTabItem();
                    }
                    
                    if (ImGui.BeginTabItem("Facilities"))
                    {
                        FacilitiesViewPannel.Display(_selectedEntity);
                        ImGui.EndTabItem();
                    }

                    
                    
                    
                    if( ImGui.BeginTabItem("Cargo and Storage"))
                    {
                        _cargoList.Display();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Industry and Construction"))
                    {
                        if (_industryPannel != null)// && ImGui.CollapsingHeader("Refinary Points: " + _industryDB.ConstructionPoints))
                        {
                            _industryPannel.Display();
                        }
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                    //_cargoList.Display();


                }
                ImGui.End();
            }
        }
        
        
        internal void HardRefresh()
        {
            _factionInfoDB = _uiState.Faction.GetDataBlob<FactionInfoDB>();


            if (_selectedEntity.Entity.HasDataBlob<IndustryAbilityDB>())
            {
                _industryDB = _selectedEntity.Entity.GetDataBlob<IndustryAbilityDB>();
                _industryPannel = new IndustryPannel2(_uiState, _selectedEntity.Entity, _industryDB);
            }
            else
            {
                _industryDB = null;
                _industryPannel = null;
            }
            
            _cargoList = new CargoListPannelSimple(_staticData, _selectedEntity);
            

        }

        internal override void EntityClicked(EntityState entity, MouseButtons button)
        {
            if (button == MouseButtons.Primary)
                _selectedEntity = entity;
        }
    }

    public class OverViewPannel
    {
        private static Guid _entityID;
        private static Dictionary<Guid, IndustryTypeSD> _industryTypes;
        public static void Setup(EntityState selectedEntity)
        {
            _entityID = selectedEntity.Entity.Guid;
            _industryTypes = Pulsar4X.ECSLib.StaticRefLib.StaticData.IndustryTypes;
            
        }
        
        public static void Display(EntityState selectedEntity)
        {
            EntityInfoPanel.AbilitesDisplay.Display(selectedEntity.Entity);
            
        }
    }

    public class FacilitiesViewPannel
    {

        private static Guid _entityID;
        private static Dictionary<Guid, IndustryTypeSD> _industryTypes;
        public static void Setup(EntityState selectedEntity)
        {
            _entityID = selectedEntity.Entity.Guid;
            _industryTypes = Pulsar4X.ECSLib.StaticRefLib.StaticData.IndustryTypes;
            
        }

        public static void Display(EntityState selectedEntity)
        {
            ComponentInstancesDB intances = selectedEntity.Entity.GetDataBlob<ComponentInstancesDB>();
            var componentInstances = EntityInfoPanel.ComponentsDisplay.CreateNewInstanceArray(selectedEntity.Entity);
            EntityInfoPanel.ComponentsDisplay.DisplayComplex(componentInstances);

 

        }
        

    }
}
