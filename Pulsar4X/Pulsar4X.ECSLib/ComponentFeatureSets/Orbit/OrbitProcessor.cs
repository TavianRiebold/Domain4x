﻿using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Orbit processor.
    /// How Orbits are calculated:
    /// First we get the time since epoch. (time from when the planet is at its closest to it's parent)
    /// Then we get the Mean Anomaly. (stored) 
    /// Eccentric Anomaly is calculated from the Mean Anomaly, and takes the most work. 
    /// True Anomaly, is calculated using the Eccentric Anomaly this is the angle from the parent (or focal point of the ellipse) to the body. 
    /// With the true anomaly, we can then use trig to calculate the position.  
    /// </summary>
    public class OrbitProcessor : OrbitProcessorBase, IHotloopProcessor
    {
        /// <summary>
        /// TypeIndexes for several dataBlobs used frequently by this processor.
        /// </summary>
        private static readonly int OrbitTypeIndex = EntityManager.GetTypeIndex<OrbitDB>();
        private static readonly int PositionTypeIndex = EntityManager.GetTypeIndex<PositionDB>();
        private static readonly int StarInfoTypeIndex = EntityManager.GetTypeIndex<StarInfoDB>();

        public TimeSpan RunFrequency => TimeSpan.FromMinutes(5);

        public TimeSpan FirstRunOffset => TimeSpan.FromTicks(0);

        public Type GetParameterType => typeof(OrbitDB);


        public void Init(Game game)
        {
            //nothing needed to do in this one. still need this function since it's required in the interface. 
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            DateTime toDate = entity.Manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            UpdateOrbit(entity, entity.GetDataBlob<OrbitDB>().Parent.GetDataBlob<PositionDB>(), toDate);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            DateTime toDate = manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            return UpdateSystemOrbits(manager, toDate);
        }

        internal static int UpdateSystemOrbits(EntityManager manager, DateTime toDate)
        {
            //TimeSpan orbitCycle = manager.Game.Settings.OrbitCycleTime;
            //DateTime toDate = manager.ManagerSubpulses.SystemLocalDateTime + orbitCycle;
            //starSystem.SystemSubpulses.AddSystemInterupt(toDate + orbitCycle, UpdateSystemOrbits);
            //manager.ManagerSubpulses.AddSystemInterupt(toDate + orbitCycle, PulseActionEnum.OrbitProcessor);
            // Find the first orbital entity.
            Entity firstOrbital = manager.GetFirstEntityWithDataBlob(StarInfoTypeIndex);

            if (!firstOrbital.IsValid)
            {
                // No orbitals in this manager.
                return 0;
            }

            Entity root = firstOrbital.GetDataBlob<OrbitDB>(OrbitTypeIndex).Root;
            var rootPositionDB = root.GetDataBlob<PositionDB>(PositionTypeIndex);

            // Call recursive function to update every orbit in this system.
            int count = UpdateOrbit(root, rootPositionDB, toDate);
            return count;
        }

        public static int UpdateOrbit(ProtoEntity entity, PositionDB parentPositionDB, DateTime toDate)
        {
            var entityOrbitDB = entity.GetDataBlob<OrbitDB>(OrbitTypeIndex);
            var entityPosition = entity.GetDataBlob<PositionDB>(PositionTypeIndex);
            int counter = 1;
            //if(toDate.Minute > entityOrbitDB.OrbitalPeriod.TotalMinutes)

            // Get our Parent-Relative coordinates.
            try
            {
                Vector3 newPosition = entityOrbitDB.GetPosition_AU(toDate);

                // Get our Absolute coordinates.
                entityPosition.AbsolutePosition_AU = parentPositionDB.AbsolutePosition_AU + newPosition;

            }
            catch (OrbitProcessorException e)
            {
                //Do NOT fail to the UI. There is NO data-corruption on this exception.
                // In this event, we did NOT update our position.  
                Event evt = new Event(StaticRefLib.CurrentDateTime, "Non Critical Position Exception thrown in OrbitProcessor for EntityItem " + entity.Guid + " " + e.Message);
                evt.EventType = EventType.Opps;
                StaticRefLib.EventLog.AddEvent(evt);
            }

            // Update our children.
            foreach (Entity child in entityOrbitDB.Children)
            {
                // RECURSION!
                
                counter += UpdateOrbit(child, entityPosition, toDate);
            }

            return counter;
        }


        #region Orbit Position Calculations

        //public static Vector4 GetAbsolutePositionForParabolicOrbit_AU()
        //{ }

        //public static Vector4 GetAbsolutePositionForHyperbolicOrbit_AU(OrbitDB orbitDB, DateTime time)
        //{
            
        //}

        /// <summary>
        /// Gets the orbital vector, will be either Absolute or relative depending on static bool UserelativeVelocity
        /// </summary>
        /// <returns>The orbital vector.</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 GetOrbitalVector_AU(OrbitDB orbit, DateTime atDateTime)
        {
            if (UseRelativeVelocity)
            {
                return orbit.InstantaneousOrbitalVelocityVector_AU(atDateTime);
            }
            else
            {
                return orbit.AbsoluteOrbitalVector_AU(atDateTime);
            }
        }
        
        /// <summary>
        /// Gets the orbital vector, will be either Absolute or relative depending on static bool UserelativeVelocity
        /// </summary>
        /// <returns>The orbital vector.</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 GetOrbitalVector_m(OrbitDB orbit, DateTime atDateTime)
        {
            if (UseRelativeVelocity)
            {
                return orbit.InstantaneousOrbitalVelocityVector_m(atDateTime);
            }
            else
            {
                return orbit.AbsoluteOrbitalVector_m(atDateTime);
            }
        }

        public static Vector3 GetOrbitalInsertionVector_m(Vector3 departureVelocity, OrbitDB targetOrbit, DateTime arrivalDateTime)
        {
            if (UseRelativeVelocity)
                return departureVelocity;
            else
            {
                var targetVelocity = targetOrbit.AbsoluteOrbitalVector_m(arrivalDateTime);
                return departureVelocity - targetVelocity;
            }
        }

        public static Entity FindSOIForPosition(StarSystem starSys, Vector3 AbsolutePosition)
        {
            var orbits = starSys.GetAllDataBlobsOfType<OrbitDB>();
            var withinSOIOf = new List<Entity>(); 
            foreach (var orbit in orbits)
            {
                var subOrbit = orbit.FindSOIForOrbit(AbsolutePosition);
                if(subOrbit != null)
                    withinSOIOf.Add(subOrbit.OwningEntity);
            }

            var closestDist = double.PositiveInfinity;
            Entity closestEntity = orbits[0].Root;
            foreach (var entity in withinSOIOf)
            {
                var pos = entity.GetDataBlob<PositionDB>().AbsolutePosition_m;
                var distance = (AbsolutePosition - pos).Length();
                if (distance < closestDist)
                {
                    closestDist = distance;
                    closestEntity = entity;
                }

            }
            return closestEntity;
        }

        /// <summary>
        /// Calculates a cartisian position for an intercept for a ship and an target's orbit using warp. 
        /// </summary>
        /// <returns>The intercept position and DateTime</returns>
        /// <param name="mover">The entity that is trying to intercept a target.</param>
        /// <param name="targetOrbit">Target orbit.</param>
        /// <param name="atDateTime">Datetime of transit start</param>
        public static (Vector3 position, DateTime etiDateTime) GetInterceptPosition_m(Entity mover, OrbitDB targetOrbit, DateTime atDateTime, Vector3 offsetPosition = new Vector3())
        {
            Vector3 moverPos = mover.GetAbsoluteFuturePosition(atDateTime);
            double spd_m = mover.GetDataBlob<WarpAbilityDB>().MaxSpeed;
            return OrbitMath.GetInterceptPosition_m(moverPos, spd_m, targetOrbit, atDateTime, offsetPosition);
        }
        
        internal class OrbitProcessorException : Exception
        {
            public override string Message { get; }
            public Entity Entity { get; }

            public OrbitProcessorException(string message, Entity entity)
            {
                Message = message;
                Entity = entity;
            }
        }

        #endregion
    }



    public class OrbitUpdateOftenProcessor : IHotloopProcessor
    {
        private static readonly int OrbitTypeIndex = EntityManager.GetTypeIndex<OrbitUpdateOftenDB>();
        private static readonly int PositionTypeIndex = EntityManager.GetTypeIndex<PositionDB>();
        
        public TimeSpan RunFrequency => TimeSpan.FromSeconds(1);

        public TimeSpan FirstRunOffset => TimeSpan.FromTicks(0);

        public Type GetParameterType => typeof(OrbitUpdateOftenDB);


        public void Init(Game game)
        {
            //nothing needed to do in this one. still need this function since it's required in the interface. 
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            var orbit = entity.GetDataBlob<OrbitUpdateOftenDB>(OrbitTypeIndex);
            DateTime toDate = entity.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            UpdateOrbit(orbit, toDate);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var orbits = manager.GetAllDataBlobsOfType<OrbitUpdateOftenDB>(OrbitTypeIndex);
            DateTime toDate = manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            foreach (var orbit in orbits)
            {
                UpdateOrbit(orbit, toDate);
            }

            return orbits.Count;
        }

        public static void UpdateOrbit(OrbitUpdateOftenDB entityOrbitDB, DateTime toDate)
        {
            
            PositionDB entityPosition = entityOrbitDB.OwningEntity.GetDataBlob<PositionDB>(PositionTypeIndex);
            try
            {
                Vector3 newPosition = entityOrbitDB.GetPosition_m(toDate);
                entityPosition.RelativePosition_m = newPosition;
            }
            catch (OrbitProcessor.OrbitProcessorException e)
            {
                var entity = e.Entity;
                string name = "Un-Named";
                if (entity.HasDataBlob<NameDB>())
                    name = entity.GetDataBlob<NameDB>().OwnersName;
                //Do NOT fail to the UI. There is NO data-corruption on this exception.
                // In this event, we did NOT update our position.  
                Event evt = new Event(StaticRefLib.CurrentDateTime, "Non Critical Position Exception thrown in OrbitProcessor for EntityItem " + name + " " + entity.Guid + " " + e.Message);
                evt.EventType = EventType.Opps;
                StaticRefLib.EventLog.AddEvent(evt);
            }
        }
    }
}