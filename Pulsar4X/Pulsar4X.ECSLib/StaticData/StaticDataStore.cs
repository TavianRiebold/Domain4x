﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This class acts as a simple store of all the games static data.
    /// It is saved alongside the rest of the game data.
    /// This class is generaly managed by the StaticDataManager.
    /// </summary>
    public class StaticDataStore
    {
        /// <summary>
        /// List which stores all the atmospheric gases.
        /// </summary>
        public WeightedList<AtmosphericGasSD> AtmosphericGases = new WeightedList<AtmosphericGasSD>();
        [JsonIgnore]
        public Type AtmosphericGasesType;
        [JsonIgnore]
        private const string AtmosphericGasesTypeString = "AtmosphericGases";

        /// <summary>
        /// List which stores all the Commander Name themes.
        /// </summary>
        public List<CommanderNameThemeSD> CommanderNameThemes = new List<CommanderNameThemeSD>();
        [JsonIgnore]
        public Type CommanderNameThemesType;
        [JsonIgnore]
        private const string CommanderNameThemesTypeString = "CommanderNameThemes";

        /// <summary>
        /// List which stores all the Minerals.
        /// </summary>
        public List<MineralSD> Minerals = new List<MineralSD>();
        [JsonIgnore]
        public Type MineralsType;
        [JsonIgnore]
        private const string MineralsTypeString = "Minerals";

        /// <summary>
        /// Dictionary which stores all the Technologies.
        /// stored in a dictionary to allow fast lookup of a specifc Technology based on its guid.
        /// </summary>
        public JDictionary<Guid, TechSD> Techs = new JDictionary<Guid, TechSD>();
        [JsonIgnore]
        public Type TechsType;
        [JsonIgnore]
        private const string TechsTypeString = "Techs";

        /// <summary>
        /// List which stores all of the installations
        /// </summary>
        public JDictionary<Guid, InstallationSD> Installations = new JDictionary<Guid, InstallationSD>();
        [JsonIgnore]
        public Type InstallationsType;
        [JsonIgnore]
        private const string InstallationsTypeString = "Installations";

        /// <summary>
        /// Dictionary which stores all the Recipes.
        /// </summary>
        public JDictionary<Guid, ConstructableObjSD> ConstructableObjects = new JDictionary<Guid, ConstructableObjSD>();
        [JsonIgnore]
        public Type ConstructableObjType;
        [JsonIgnore]
        private const string ConstructableObjTypeString = "ConstructableObj";


        ///< @todo add a whole bunch more static data.

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public StaticDataStore()
        {
            AtmosphericGasesType = AtmosphericGases.GetType();
            CommanderNameThemesType = CommanderNameThemes.GetType();
            MineralsType = Minerals.GetType();
            TechsType = Techs.GetType();
            InstallationsType = Installations.GetType();
        }

        /// <summary>
        /// Stores Atmospheric Gas Static Data.
        /// </summary>
        public void Store(WeightedList<AtmosphericGasSD> atmosphericGases)
        {
            if (atmosphericGases != null)
                AtmosphericGases.AddRange(atmosphericGases);
        }

        /// <summary>
        /// Stores Commander Name Themes.
        /// </summary>
        public void Store(List<CommanderNameThemeSD> commanderNameThemes)
        {
            if (commanderNameThemes != null)
                CommanderNameThemes.AddRange(commanderNameThemes);
        }

        /// <summary>
        /// Stores Mineral Static Data. Will overwrite an existing mineral if the IDs match.
        /// </summary>
        public void Store(List<MineralSD> minerals)
        {
            if (minerals != null)
            {
                foreach (var min in minerals)
                {
                    int i = Minerals.FindIndex(x => x.ID == min.ID);
                    if (i >= 0) // found existing element!
                        Minerals[i] = min;
                    else
                        Minerals.Add(min);
                }
            }
        }

        /// <summary>
        /// Stores Technology Static Data. Will overwrite any existing Techs with the same ID.
        /// </summary>
        public void Store(JDictionary<Guid, TechSD> techs)
        {
            if (techs != null)
            {
                foreach (var tech in techs)
                    Techs[tech.Key] = tech.Value;  // replace existing value or insert a new one as required.
            }
        }

        /// <summary>
        /// Stores Installation Static Data. Will overwrite any existing Installations with the same ID.
        /// </summary>
        public void Store(JDictionary<Guid, InstallationSD> installations)
        {
            if (installations != null)
            {
                foreach (var facility in installations)
                    Installations[facility.Key] = facility.Value;               
            }
        }

        /// <summary>
        /// Stores ConstructableObj Static Data. Will overwrite any existing ConstructableObjs with the same ID.
        /// </summary>
        public void Store(JDictionary<Guid, ConstructableObjSD> recipies)
        {
            if (recipies != null)
            {
                foreach (var recipe in recipies)
                    ConstructableObjects[recipe.Key] = recipe.Value;
            }
        }

        /// <summary>
        /// Returns a type custom string for a type of static data. This string is used to tell 
        /// what type of static data is being imported (and is thus exported as well). 
        /// </summary>
        public string GetTypeString(Type type)
        {
            if (type == AtmosphericGasesType)
            {
                return AtmosphericGasesTypeString;
            }
            else if (type == CommanderNameThemesType)
            {
                return CommanderNameThemesTypeString;
            }
            else if (type == MineralsType)
            {
                return MineralsTypeString;
            }
            else if (type == TechsType)
            {
                return TechsTypeString;
            }
            else if (type == InstallationsType)
            {
                return InstallationsTypeString;
            }
            else if (type == ConstructableObjType)
            {
                return ConstructableObjTypeString;
            }

            return null;
        }

        /// <summary>
        /// Gets the matching type for a type string. Used when importing previousle exported 
        /// static data to knopw what type to import it as.
        /// </summary>
        public Type GetType(string typeString)
        {
            switch (typeString)
            {
                case AtmosphericGasesTypeString:
                    return AtmosphericGasesType;
                case CommanderNameThemesTypeString:
                    return CommanderNameThemesType;
                case  MineralsTypeString:
                    return MineralsType;
                case TechsTypeString:
                    return TechsType;
                case InstallationsTypeString:
                    return InstallationsType;
                case ConstructableObjTypeString:
                    return ConstructableObjType;
                default:
                    return null;
            }
        }

        /// <summary>
        /// This functin goes through each Static Data type in the store looking for one that has an ID that
        /// matches the one provided.
        /// Returns null if the id is not found.
        /// </summary>
        public object FindDataObjectUsingID(Guid id)
        {
            foreach (var m in Minerals)
            {
                if (m.ID == id)
                    return m;
            }

            if (Techs.ContainsKey(id))
                return Techs[id];

            if (Installations.ContainsKey(id))
                return Installations[id];

            if (ConstructableObjects.ContainsKey(id))
                return ConstructableObjects[id];

            return null;
        }
    }
}