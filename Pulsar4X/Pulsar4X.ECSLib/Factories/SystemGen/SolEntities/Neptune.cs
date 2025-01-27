﻿using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib.Factories.SystemGen
{
    public static partial class SolEntities
    {
        /// <summary>
        /// Creates Neptune
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Neptune</remarks>
        public static Entity Neptune(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.IceGiant, SupportsPopulations = true, Albedo = 0.300f };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(1.02413E26, Distance.KmToAU(24764));
            NameDB planetNameDB = new NameDB("Neptune");

            double planetSemiMajorAxisAU = 30.11;
            double planetEccentricity = 0.009456;
            double planetEclipticInclination = 0;   // 1.767975
            double planetLoAN = 131.784;
            double planetAoP = 276.336;
            double planetMeanAnomaly = 256.228;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            PositionDB planetPositionDB = new PositionDB(planetOrbitDB.GetPosition_AU(StaticRefLib.CurrentDateTime), sol.Guid, sun);

            var pressureAtm = Pressure.BarToAtm(1000f);         // https://nssdc.gsfc.nasa.gov/planetary/factsheet/neptunefact.html#:~:text=Surface%20Pressure%3A%20%3E%3E1000%20bars,Molecular%20hydrogen%20(H2)%20%2D
            Dictionary<AtmosphericGasSD, float> atmoGasses = new Dictionary<AtmosphericGasSD, float>
            {
                { game.StaticData.GetAtmosGasBySymbol("H2"),  0.80f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("He"),  0.19f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("CH4"), 0.015f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("HD"),  0.00019f * pressureAtm },
                { game.StaticData.GetAtmosGasBySymbol("C2H6"),0.0000015f * pressureAtm }
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, -201f, atmoGasses);

            Entity planet = new Entity(sol, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, planetAtmosphereDB });
            SensorProcessorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}
