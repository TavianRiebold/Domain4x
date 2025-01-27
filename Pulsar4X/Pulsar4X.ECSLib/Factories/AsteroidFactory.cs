﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Asteroid factory. creates rocks to collide with planets
    /// </summary>
    public static class AsteroidFactory
    {
        /// <summary>
        /// creates an asteroid that will collide with the given entity on the given date. 
        /// </summary>
        /// <param name="starSys"></param>
        /// <param name="target"></param>
        /// <param name="collisionDate"></param>
        /// <returns></returns>
        public static Entity CreateAsteroid(StarSystem starSys, Entity target, DateTime collisionDate, double asteroidMass = -1.0)
        {
            //todo rand these a bit.
            double radius = Distance.KmToAU(0.5);

            double mass;
            if (asteroidMass < 0)
                mass = 1.5e+12; //about 1.5 billion tonne
            else
                mass = asteroidMass;

            var speed = 40000;
            Vector3 velocity = new Vector3(speed, 0, 0);


            var massVolume = MassVolumeDB.NewFromMassAndRadius_AU(mass, radius);
            var planetInfo = new SystemBodyInfoDB();
            var name = new NameDB("Ellie");
            var AsteroidDmg = new AsteroidDamageDB();
            AsteroidDmg.FractureChance = new PercentValue(0.75f);
            var dmgPfl = EntityDamageProfileDB.AsteroidDamageProfile(massVolume.Volume_km3, massVolume.DensityDry_gcm, massVolume.RadiusInM, 50);
            var sensorPfil = new SensorProfileDB();

            planetInfo.SupportsPopulations = false;
            planetInfo.BodyType = BodyType.Asteroid;

            Vector3 targetPos = target.GetDataBlob<OrbitDB>().GetAbsolutePosition_m(collisionDate);
            TimeSpan timeToCollision = collisionDate - StaticRefLib.CurrentDateTime;


            var parent = target.GetDataBlob<OrbitDB>().Parent;
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            var myMass = massVolume.MassDry;

            double sgp = OrbitMath.CalculateStandardGravityParameterInM3S2(myMass, parentMass);
            OrbitDB orbit = OrbitDB.FromVector(parent, myMass, parentMass, sgp, targetPos, velocity, collisionDate);

            var currentpos = orbit.GetAbsolutePosition_AU(StaticRefLib.CurrentDateTime);
            var posDB = new PositionDB(currentpos.X, currentpos.Y, currentpos.Z, parent.Manager.ManagerGuid, parent);


            var planetDBs = new List<BaseDataBlob>
            {
                posDB,
                massVolume,
                planetInfo,
                name,
                orbit,
                AsteroidDmg,
                dmgPfl,
                sensorPfil
            };

            Entity newELE = new Entity(starSys, planetDBs);
            return newELE;
        }

        public static Entity CreateAsteroid4(Vector3 position, OrbitDB origOrbit, DateTime atDateTime, double asteroidMass = -1.0)
        {
            //todo rand these a bit.
            double radius = Distance.KmToAU(0.5);

            double mass;
            if (asteroidMass == -1.0)
                mass = 1.5e+12; //about 1.5 billion tonne
            else
                mass = asteroidMass;

            var speed = Distance.KmToAU(40);
            Vector3 velocity = new Vector3(speed, 0, 0);


            var massVolume = MassVolumeDB.NewFromMassAndRadius_AU(mass, radius);
            var planetInfo = new SystemBodyInfoDB();
            var name = new NameDB("Ellie");
            var AsteroidDmg = new AsteroidDamageDB();
            AsteroidDmg.FractureChance = new PercentValue(0.75f);
            var dmgPfl = EntityDamageProfileDB.AsteroidDamageProfile(massVolume.Volume_km3, massVolume.DensityDry_gcm, massVolume.RadiusInM, 50);
            var sensorPfil = new SensorProfileDB();

            planetInfo.SupportsPopulations = false;
            planetInfo.BodyType = BodyType.Asteroid;


            var parent = origOrbit.Parent;
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            var myMass = massVolume.MassDry;

            double sgp = UniversalConstants.Science.GravitationalConstant * (parentMass + myMass) / 3.347928976e33;
            //OrbitDB orbit = OrbitDB.FromVector(parent, myMass, parentMass, sgp, position, velocity, atDateTime);
            //OrbitDB orbit = (OrbitDB)origOrbit.Clone();
            OrbitDB orbit = new OrbitDB(origOrbit.Parent, parentMass, myMass, origOrbit.SemiMajorAxis_AU, 
                origOrbit.Eccentricity, origOrbit.Inclination_Degrees, origOrbit.LongitudeOfAscendingNode_Degrees, 
                origOrbit.ArgumentOfPeriapsis_Degrees, origOrbit.MeanMotion_DegreesSec, origOrbit.Epoch);

            var posDB = new PositionDB(position.X, position.Y, position.Z, parent.Manager.ManagerGuid, parent);


            var planetDBs = new List<BaseDataBlob>
            {
                posDB,
                massVolume,
                planetInfo,
                name,
                orbit,
                AsteroidDmg,
                dmgPfl,
                sensorPfil
            };

            Entity newELE = new Entity(origOrbit.OwningEntity.Manager, planetDBs);
            return newELE;
        }
    }
}
