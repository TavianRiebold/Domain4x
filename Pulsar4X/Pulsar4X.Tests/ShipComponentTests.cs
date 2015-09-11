﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("Component Tests")]
    internal class ComponentTests
    {
        private Game _game;
        private EntityManager _entityManager;
        private Entity _faction;
        private Entity _colonyEntity;
        private MineralSD _duraniumSD;
        private MineralSD _corundiumSD;
        private StarSystem _starSystem;
        private Entity _shipClass;
        private Entity _ship;
        private Entity _engineComponent;
        [SetUp]
        public void Init()
        {
            _game = Game.NewGame("Test Game", DateTime.Now, 1);
            //Tech();
            _faction = FactionFactory.CreateFaction(_game, "Terran");
            _faction.GetDataBlob<TechDB>().ResearchedTechs.Add(new Guid("b8ef73c7-2ef0-445e-8461-1e0508958a0e"),3);
            _faction.GetDataBlob<TechDB>().ResearchedTechs.Add(new Guid("08fa4c4b-0ddb-4b3a-9190-724d715694de"), 3);
            _faction.GetDataBlob<TechDB>().ResearchedTechs.Add(new Guid("8557acb9-c764-44e7-8ee4-db2c2cebf0bc"), 5);
            _faction.GetDataBlob<TechDB>().ResearchedTechs.Add(new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c"), 1);
            _faction.GetDataBlob<TechDB>().ResearchedTechs.Add(new Guid("c827d369-3f16-43ef-b112-7d5bcafb74c7"), 1);
            _faction.GetDataBlob<TechDB>().ResearchedTechs.Add(new Guid("db6818f3-99e9-46c1-b903-f3af978c38b2"), 1);
            _starSystem = new StarSystem(_game, "Sol", -1);
            /////Ship Class/////
            _shipClass = ShipFactory.CreateNewShipClass(_game, _faction, "TestClass");
        }

 

        [Test]
        public void TestEngineComponentFactory()
        {
            ComponentSD engine = EngineComponentSD();

            ComponentDesignDB design = GenericComponentFactory.StaticToDesign(engine, _faction.GetDataBlob<TechDB>(), _game.StaticData);

            foreach (var ability in design.ComponentDesignAbilities)
            {
                if (ability.GuiHint == GuiHint.GuiSelectionList)
                {
                    List<TechSD> selectionlist = ability.SelectionDictionary.Keys.ToList();
                    ability.SetValueFromTechList(selectionlist[selectionlist.Count - 1]);
                }
                else if (ability.GuiHint == GuiHint.GuiSelectionMaxMin)
                {
                    ability.SetMax();
                    ability.SetValueFromInput(ability.MaxValue);
                }
                else
                    ability.SetValue();
            }

            design.ComponentDesignAbilities[0].SetValueFromInput(250);

            Entity engineEntity = GenericComponentFactory.DesignToEntity(_game.GlobalManager, design, _faction.GetDataBlob<TechDB>());

            Assert.AreEqual(250, engineEntity.GetDataBlob<ComponentInfoDB>().SizeInTons);

        }
        
        public static ComponentSD EngineComponentSD()
        {
            ComponentSD component = new ComponentSD();
            component.Name = "Engine";
            component.Description = "Moves a ship";
            component.ID = new Guid("E76BD999-ECD7-4511-AD41-6D0C59CA97E6");

            component.SizeGuiHint = GuiHint.GuiSelectionMaxMin;
            component.SizeFormula = "Ability(0)";

            component.HTKGuiHint = GuiHint.GuiTextDisplay;
            component.HTKFormula = "Max(1, [Size] / 100)";

            component.CrewReqGuiHint = GuiHint.GuiTextDisplay;
            component.CrewReqFormula = "[Size]";

            component.ResearchCostGuiHint = GuiHint.None;
            component.ResearchCostFormula = "[Size] * 10";

            component.MineralCostGuiHint = GuiHint.GuiTextDisplay;
            component.MineralCostFormula = new JDictionary<Guid, string> { { new Guid("2d4b2866-aa4a-4b9a-b8aa-755fe509c0b3"), "[Size] * 8" } };

            component.CreditCostGuiHint = GuiHint.GuiTextDisplay;
            component.CreditCostFormula = "[Size]";

            component.ComponentAbilitySDs = new List<ComponentAbilitySD>();

            ComponentAbilitySD SizeFormula = new ComponentAbilitySD();
            SizeFormula.Name = "Engine Size";
            SizeFormula.Description = "";
            SizeFormula.GuiHint = GuiHint.GuiSelectionMaxMin;
            SizeFormula.AbilityFormula = "250";
            SizeFormula.MaxFormula = "2500";
            SizeFormula.MinFormula = "1";
            component.ComponentAbilitySDs.Add(SizeFormula);

            ComponentAbilitySD engineTypeAbility = new ComponentAbilitySD();
            engineTypeAbility.Name = "Engine Type";
            engineTypeAbility.Description = "Type of engine Tech";
            engineTypeAbility.GuiHint = GuiHint.GuiSelectionList;
            engineTypeAbility.TechList = new List<Guid>
            {
                new Guid("35608fe6-0d65-4a5f-b452-78a3e5e6ce2c"),
                new Guid("c827d369-3f16-43ef-b112-7d5bcafb74c7"),
                new Guid("db6818f3-99e9-46c1-b903-f3af978c38b2"),
                new Guid("f3f10e56-9345-40cc-af42-342e7240355d")
                //new Guid("58d047e6-c567-4db6-8c76-bfd4a201af94"),
                //new Guid("bd75bf88-1dad-4022-b401-acdf05ab73f8"),
                //new Guid("042ce9d4-5a2c-4d8e-9ae4-be059920839c"),
                //new Guid("93611831-9183-484a-9920-13b39d64e272"),
                //new Guid("32eda0ab-c117-4224-b148-6c9d0e474296"),
                //new Guid("cbb1a7ce-3c26-4b5b-abd7-9a99c670d68d"),
                //new Guid("6e34cc46-0693-4676-b0ca-f076fb36acaf"),
                //new Guid("9bb4d1c4-680f-4c98-b927-337654073575"),
                //new Guid("c9587310-f7dd-45d0-ac4c-b6f59a1e1897")
            };
            engineTypeAbility.AbilityFormula = "TechData('f3f10e56-9345-40cc-af42-342e7240355d')";

            component.ComponentAbilitySDs.Add(engineTypeAbility);

            ComponentAbilitySD enginePowerEfficency = new ComponentAbilitySD();
            enginePowerEfficency.Name = "Engine Consumption vs Power";
            enginePowerEfficency.Description = "More Powerfull engines are less efficent for a given size";
            enginePowerEfficency.GuiHint = GuiHint.GuiSelectionMaxMin;
            enginePowerEfficency.AbilityFormula = "1";
            enginePowerEfficency.MaxFormula = "TechData('b8ef73c7-2ef0-445e-8461-1e0508958a0e')";
            enginePowerEfficency.MinFormula = "TechData('08fa4c4b-0ddb-4b3a-9190-724d715694de')";
            component.ComponentAbilitySDs.Add(enginePowerEfficency);

            ComponentAbilitySD enginePowerAbility = new ComponentAbilitySD();
            enginePowerAbility.Name = "Engine Power";
            enginePowerAbility.Description = "Move Power for ship";
            enginePowerAbility.GuiHint = GuiHint.None;
            enginePowerAbility.AbilityDataBlobType = typeof(EnginePowerDB).ToString();
            enginePowerAbility.AbilityFormula = "DataBlobArgs(Ability(1) * [Size])";
            component.ComponentAbilitySDs.Add(enginePowerAbility);

            ComponentAbilitySD fuelConsumptionBase = new ComponentAbilitySD();
            fuelConsumptionBase.Name = "Fuel Consumption";
            fuelConsumptionBase.Description = "From Tech";
            fuelConsumptionBase.GuiHint = GuiHint.None;
            fuelConsumptionBase.AbilityFormula = "TechData('8557acb9-c764-44e7-8ee4-db2c2cebf0bc') * Pow(Ability(3), 2.25)";
            component.ComponentAbilitySDs.Add(fuelConsumptionBase);

            ComponentAbilitySD fuelConsumptionSizeMod = new ComponentAbilitySD();
            fuelConsumptionSizeMod.Name = "Fuel Consumption";
            fuelConsumptionSizeMod.Description = "Size Mod";
            fuelConsumptionSizeMod.GuiHint = GuiHint.GuiTextDisplay;
            fuelConsumptionSizeMod.AbilityDataBlobType = typeof(FuelUseDB).ToString();
            fuelConsumptionSizeMod.AbilityFormula = "DataBlobArgs(Ability(3) - Ability(3) * [Size] * 0.002)";
            component.ComponentAbilitySDs.Add(fuelConsumptionSizeMod);

            ComponentAbilitySD ThermalReduction = new ComponentAbilitySD();
            ThermalReduction.Name = "Thermal Signature Reduction";
            ThermalReduction.Description = "";
            ThermalReduction.GuiHint = GuiHint.GuiSelectionMaxMin;
            ThermalReduction.AbilityFormula = "0";
            ThermalReduction.MaxFormula = "0";
            ThermalReduction.MinFormula = "0.5";
            component.ComponentAbilitySDs.Add(ThermalReduction);

            ComponentAbilitySD sensorSig = new ComponentAbilitySD();
            sensorSig.Name = "Thermal Signature";
            sensorSig.Description = "";
            sensorSig.GuiHint = GuiHint.GuiTextDisplay;
            sensorSig.AbilityDataBlobType = typeof(SensorSignatureDB).ToString();
            sensorSig.AbilityFormula = "DataBlobArgs(Ability(3) * Ability(6),0)";
            component.ComponentAbilitySDs.Add(sensorSig);
            
            JDictionary<Guid,ComponentSD> componentsDict = new JDictionary<Guid, ComponentSD>();
            componentsDict.Add(component.ID, component);
;           StaticDataManager.ExportStaticData(componentsDict, "./EngineComponentTest.json");
            
            return component;
        }
    }
}