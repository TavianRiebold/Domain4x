﻿using System;
namespace Pulsar4X.ECSLib
{

    /// <summary>
    /// A struct to hold kepler elements without the need to give a 'parent' as OrbitDB does.
    /// </summary>
    public struct KeplerElements
    {
        public double SemiMajorAxis;        //a
        public double SemiMinorAxis;        //b
        public double Eccentricity;         //e
        public double LinierEccentricity;
        public double Periapsis;            //q
        public double Apoapsis;             //Q
        public double LoAN;                 //Omega (upper case)
        public double AoP;                  //omega (lower case)
        public double Inclination;          //i
        public double MeanAnomaly;          //M
        public double TrueAnomaly;          //v or f or theta
        //public double Period              //P
        //public double EccentricAnomaly    //E
        public double Epoch;                //time since periapsis. 
    }

    public class OrbitMath
    {

        /// <summary>
        /// Kepler elements from velocity and position.
        /// </summary>
        /// <returns>a struct of Kepler elements.</returns>
        /// <param name="standardGravParam">Standard grav parameter.</param>
        /// <param name="position">Position ralitive to parent</param>
        /// <param name="velocity">Velocity ralitive to parent</param>
        public static KeplerElements KeplerFromVelocityAndPosition(double standardGravParam, Vector4 position, Vector4 velocity)
        {
            KeplerElements ke = new KeplerElements();
            Vector4 angularVelocity = Vector4.Cross(position, velocity);
            Vector4 nodeVector = Vector4.Cross(new Vector4(0, 0, 1, 0), angularVelocity);

            Vector4 eccentVector1 = EccentricityVector(standardGravParam, position, velocity);

            Vector4 eccentVector = EccentricityVector2(standardGravParam, position, velocity);

            double eccentricity = eccentVector.Length();

            double specificOrbitalEnergy = velocity.Length() * velocity.Length() * 0.5 - standardGravParam / position.Length();


            double semiMajorAxis;
            double p; //wtf is p? idk. it's not used, but it was in the origional formula. 
            if (Math.Abs(eccentricity) > 1) //hypobola
            {
                semiMajorAxis = -(-standardGravParam / (2 * specificOrbitalEnergy));
                p = semiMajorAxis * (1 - eccentricity * eccentricity);
            }
            else if (Math.Abs(eccentricity) < 1) //ellipse
            {
                semiMajorAxis = -standardGravParam / (2 * specificOrbitalEnergy);
                p = semiMajorAxis * (1 - eccentricity * eccentricity);
            }
            else //parabola
            {
                p = angularVelocity.Length() * angularVelocity.Length() / standardGravParam;
                semiMajorAxis = double.MaxValue;
            }

            /*
            if (Math.Abs(eccentricity - 1.0) > 1e-15)
            {
                semiMajorAxis = -standardGravParam / (2 * specificOrbitalEnergy);
                p = semiMajorAxis * (1 - eccentricity * eccentricity);
            }
            else //parabola
            {
                p = angularVelocity.Length() * angularVelocity.Length() / standardGravParam;
                semiMajorAxis = double.MaxValue;
            }
*/

            double semiMinorAxis = EllipseMath.SemiMinorAxis(semiMajorAxis, eccentricity);
            double linierEccentricity = eccentricity * semiMajorAxis;

            double inclination = Math.Acos(angularVelocity.Z / angularVelocity.Length()); //should be 0 in 2d. 
            if (double.IsNaN(inclination))
                inclination = 0;

            double loANlen = nodeVector.X / nodeVector.Length();
            double longdOfAN = 0;
            if (double.IsNaN(loANlen))
                loANlen = 0;
            else
                loANlen = GMath.Clamp(loANlen, -1, 1);
            if(loANlen != 0)
                longdOfAN = Math.Acos(loANlen); //RAAN or LoAN or Omega letter



            // https://en.wikipedia.org/wiki/Argument_of_periapsis#Calculation
            double argOfPeriaps;
            if (longdOfAN == 0)
            {
                argOfPeriaps = Math.Atan2(eccentVector.Y, eccentVector.X);
                if (Vector4.Cross(position, velocity).Z < 0) //anti clockwise orbit
                    argOfPeriaps = Math.PI * 2 - argOfPeriaps;
            }

            else
            {
                double aopLen = Vector4.Dot(nodeVector, eccentVector);
                aopLen = aopLen / (nodeVector.Length() * eccentricity);
                aopLen = GMath.Clamp(aopLen, -1, 1);
                argOfPeriaps = Math.Acos(aopLen);
                if (eccentVector.Z < 0) //anti clockwise orbit.
                    argOfPeriaps = Math.PI * 2 - argOfPeriaps;
            }




            var eccAng = Vector4.Dot(eccentVector, position);
            eccAng = semiMajorAxis / eccAng;
            eccAng = GMath.Clamp(eccAng, -1, 1);
            var eccentricAnomoly = Math.Acos(eccAng);

            var meanAnomaly = eccentricAnomoly - eccentricity * Math.Sin(eccentricAnomoly);



            ke.SemiMajorAxis = semiMajorAxis;
            ke.SemiMinorAxis = semiMinorAxis;
            ke.Eccentricity = eccentricity;

            ke.Apoapsis = EllipseMath.Apoapsis(eccentricity, semiMajorAxis);
            ke.Periapsis = EllipseMath.Periapsis(eccentricity, semiMajorAxis);
            ke.LinierEccentricity = EllipseMath.LinierEccentricity(ke.Apoapsis, semiMajorAxis);
            ke.LoAN = longdOfAN;
            ke.AoP = Angle.NormaliseRadians( argOfPeriaps);
            ke.Inclination = inclination;
            ke.MeanAnomaly = meanAnomaly;
            ke.TrueAnomaly = TrueAnomaly(eccentVector, position, velocity);
            ke.Epoch = Epoch(semiMajorAxis, semiMinorAxis, eccentricAnomoly, OrbitalPeriod(standardGravParam, semiMajorAxis));
            return ke;
        }

        public static double Epoch(double semiMaj, double semiMin, double eccentricAnomaly, double Period)
        {

            double areaOfEllipse = semiMaj * semiMin * Math.PI;
            double eccentricAnomalyArea = EllipseMath.AreaOfEllipseSector(semiMaj, semiMaj, 0, eccentricAnomaly); //we get the area as if it's a circile. 
            double trueArea = semiMin / semiMaj * eccentricAnomalyArea; //we then multiply the result by a fraction of b / a
            //double areaOfSegment = EllipseMath.AreaOfEllipseSector(semiMaj, semiMin, 0, lop + trueAnomaly);

            double t = Period * (trueArea / areaOfEllipse);

            return t;

        }

            /// <summary>
            /// https://en.wikipedia.org/wiki/Eccentricity_vector
            /// </summary>
            /// <returns>The vector.</returns>
            /// <param name="sgp">StandardGravParam.</param>
            /// <param name="position">Position, ralitive to parent.</param>
            /// <param name="velocity">Velocity, ralitive to parent.</param>
            public static Vector4 EccentricityVector(double sgp, Vector4 position, Vector4 velocity)
        {
            Vector4 angularMomentum = Vector4.Cross(position, velocity);
            Vector4 foo1 = Vector4.Cross(velocity, angularMomentum);
            foo1 = foo1 / sgp;
            var foo2 = position / position.Length();
            return foo1 - foo2;
        }

        public static double OrbitalPeriod(double sgp, double semiMajAxis)
        {
            return 2 * Math.PI * Math.Sqrt(Math.Pow(semiMajAxis, 3) / sgp);
        }

        public static Vector4 EccentricityVector2(double sgp, Vector4 position, Vector4 velocity)
        {
            var speed = velocity.Length();
            var radius = position.Length();
            var foo1 = (speed * speed - sgp / radius) * position ;
            var foo2 = Vector4.Dot(position, velocity) * velocity;
            var foo3 = (foo1 - foo2) / sgp;
            return foo3;
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/True_anomaly#From_state_vectors
        /// </summary>
        /// <returns>The anomaly.</returns>
        /// <param name="eccentVector">Eccentricity vector.</param>
        /// <param name="position">Position ralitive to parent</param>
        /// <param name="velocity">Velocity ralitive to parent</param>
        public static double TrueAnomaly(Vector4 eccentVector, Vector4 position, Vector4 velocity)
        {

            var dotEccPos = Vector4.Dot(eccentVector, position);
            var talen = eccentVector.Length() * position.Length();
            talen = dotEccPos / talen;
            talen = GMath.Clamp(talen, -1, 1);
            var trueAnomoly = Math.Acos(talen);

            if (Vector4.Dot(position, velocity) < 0)
                trueAnomoly = Math.PI * 2 - trueAnomoly;

            return trueAnomoly;
        }

        /// <summary>
        /// Velocity vector in polar coordinates.
        /// </summary>
        /// <returns>item1 is speed, item2 is angle.</returns>
        /// <param name="sgp">Sgp.</param>
        /// <param name="position">Position.</param>
        /// <param name="sma">Sma.</param>
        /// <param name="eccentricity">Eccentricity.</param>
        /// <param name="loP">Lo p.</param>
        public static Tuple<double, double> PreciseOrbitalVelocityPolarCoordinate(double sgp, Vector4 position, double sma, double eccentricity, double loP)
        {
            var radius = position.Length();
            var spd = PreciseOrbitalSpeed(sgp, radius, sma);

            double linierEcc = EllipseMath.LinierEccentricityFromEccentricity(sma, eccentricity);

            double referenceToPosAngle = Math.Atan2(position.X, -position.Y); //we switch x and y here so atan2 works in the y direction. 

            double anglef = loP - referenceToPosAngle;

            //find angle alpha using law of cos: (a^2 + b^2 - c^2) / 2ab
            double sideA = radius;
            double sideB = 2 * sma - radius;
            double sideC = 2 * linierEcc;
            double alpha = Math.Acos((sideA * sideA + sideB * sideB - sideC * sideC) / (2 * sideA * sideB));

            double angle = Math.PI - (referenceToPosAngle + ((Math.PI - alpha) * 0.5));

            return new Tuple<double, double>(spd, angle);
        }

        /// <summary>
        /// 2d! vector. 
        /// </summary>
        /// <returns>The orbital vector ralitive to the parent</returns>
        /// <param name="sgp">Standard Grav Perameter. in AU</param>
        /// <param name="position">Ralitive Position.</param>
        /// <param name="sma">SemiMajorAxis</param>
        /// <param name="loP">Longditude of Periapsis (LoAN+ AoP) </param>
        public static Vector4 PreciseOrbitalVelocityVector(double sgp, Vector4 position, double sma, double eccentricity, double loP)
        {
            var pc = PreciseOrbitalVelocityPolarCoordinate(sgp, position, sma, eccentricity, loP);
            var v = new Vector4()
            {
                X= Math.Sin(pc.Item2) * pc.Item1,
                Y = Math.Cos(pc.Item2) * pc.Item1
            };

            if (double.IsNaN(v.X) || double.IsNaN(v.Y))
                throw new Exception("Result is NaN");

            return v;
        }

        /// <summary>
        /// returns the speed for an object of a given mass at a given radius from a body. this is the vis-viva calculation
        /// </summary>
        /// <returns>The orbital speed, ralitive to the parent</returns>
        /// <param name="standardGravParameter">standardGravParameter.</param>
        /// <param name="distance">Radius.</param>
        /// <param name="semiMajAxis">Semi maj axis.</param>
        public static double PreciseOrbitalSpeed(double standardGravParameter, double distance, double semiMajAxis)
        {
            return Math.Sqrt(standardGravParameter * (2 / distance - 1 / semiMajAxis));
        }

        /// <summary>
        /// Calculates distance/s on an orbit by calculating positions now and second in the future. 
        /// Fairly slow and inefficent. 
        /// </summary>
        /// <returns>the distance traveled in a second</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDatetime">At datetime.</param>
        public double Hackspeed(OrbitDB orbit, DateTime atDatetime)
        {
            var pos1 = OrbitProcessor.GetPosition_AU(orbit, atDatetime);
            var pos2 = OrbitProcessor.GetPosition_AU(orbit, atDatetime + TimeSpan.FromSeconds(1));

            return Distance.DistanceBetween(pos1, pos2);
        }

        /// <summary>
        /// This is an aproximation of the mean velocity of an orbit. 
        /// </summary>
        /// <returns>The orbital velocity in au.</returns>
        /// <param name="orbit">Orbit.</param>
        public static double MeanOrbitalVelocityInAU(OrbitDB orbit)
        {
            double a = orbit.SemiMajorAxis;
            double b = EllipseMath.SemiMinorAxis(a, orbit.Eccentricity);
            double orbitalPerodSeconds = orbit.OrbitalPeriod.TotalSeconds;
            double peremeter = Math.PI * (3* (a + b) - Math.Sqrt((3 * a + b) * (a + 3 * b)));
            return orbitalPerodSeconds / peremeter;
        }

        /// <summary>
        /// Tsiolkovsky's rocket equation.
        /// </summary>
        /// <returns>deltaV</returns>
        /// <param name="wetMass">Wet mass.</param>
        /// <param name="dryMass">Dry mass.</param>
        /// <param name="specificImpulse">Specific impulse.</param>
        public static double TsiolkovskyRocketEquation(double wetMass, double dryMass, double specificImpulse)
        {
            double ve = specificImpulse * 9.8;
            double deltaV = ve * Math.Log(wetMass / dryMass);
            return deltaV;
        }


        public void CargoTransferCostFromPlanet(Entity recever, Entity planet, Shuttle shuttle)
        {

            //minExchangeRadius = min range to exchange cargo: body radius + atmo.
            //Dv cost at Min range. 
            //cargo capacity at Min range.
                //total capacity. - defined by shuttle
                //fuel weight.
            //Maximum Dv at 0 cargo. - defined by shuttle
            //Time per trip. (not even sure how I'm going to calc this)
            //capacity per trip. (from target orbit DV)

            //other ideas which would affect this:
            //single use launches (from a ground station) more cargo, less fuel but higher other resources.
            //re-usable - higher fuel cost ie. de-orbit burn and reentry, would be higher on atmoless planets, lower cost on other resources. 

            var bodyRadius = planet.GetDataBlob<MassVolumeDB>().RadiusInKM;
            var targetOrbit = recever.GetDataBlob<OrbitDB>();
            var sgp = targetOrbit.GravitationalParameter;

            //shuttleData: this should all come from a datablob
            double shuttleWetMass = shuttle.shuttleWetMass;
            double shuttleDryMass = shuttle.shuttleDryMass;
            double shuttleEngineISP = shuttle.shuttleEngineISP;
            double shuttleMaxCargoMass = shuttle.shuttleMaxCargoMass;



            //minExchangeRadius

            double minExchangeRadius = bodyRadius * 1.25; //ie atmosphereless body. 
            float atmoPressure = 0;
            if (planet.HasDataBlob<AtmosphereDB>())
            {
                var atmo = planet.GetDataBlob<AtmosphereDB>();
                atmoPressure = atmo.Pressure;

                // karmanLine is an 'outofmyass' calculation. 
                //earths radius is ~6.7km, the Karman line is 100km, 
                //earths mesopause is 80km, 
                //the ISS orbits at 330 to 410km
                // radius of earth * 15 gets close to the karman line. 
                var karmanLine = bodyRadius * 15;
                minExchangeRadius = bodyRadius + karmanLine * atmoPressure; //so karman line * atmopressure should give us a good safe distance for cargo tranfer.
            }

            //DvCost at min range;
            double dvToMinOrbit = PreciseOrbitalSpeed(sgp, minExchangeRadius, minExchangeRadius); //dv to a low circular orbit.  
            //todo: dvToMinOrbit += gravityDrag, atmosphereDrag, etc etc.  

            double dvAtMaxCargo = TsiolkovskyRocketEquation(shuttleWetMass + shuttleMaxCargoMass, shuttleDryMass + shuttleMaxCargoMass, shuttleEngineISP);
            double dvAtNoCargo = TsiolkovskyRocketEquation(shuttleWetMass, shuttleDryMass, shuttleEngineISP);


        }

        /// <summary>
        /// TODO: add atmo drag and 'gravity drag' (ie thrust to weight ratio) to this. 
        /// </summary>
        /// <returns>The to orbit dv.</returns>
        /// <param name="targetOrbit">Target orbit.</param>
        /// <param name="departBody">Depart body.</param>
        public double AccentToOrbitDV(OrbitDB targetOrbit, Entity departBody, double thrustToWeight, double aerodynamicDrag, double areodynamicLift, double arodynamicStrength)
        {
            var sgp = targetOrbit.GravitationalParameter;
            var bodyRadius = departBody.GetDataBlob<MassVolumeDB>().RadiusInKM;
            var periaps = Distance.AuToKm( targetOrbit.Periapsis);
            var sma = targetOrbit.SemiMajorAxis;
            var distance = periaps - bodyRadius;

            var dvAtPeriapsis = PreciseOrbitalSpeed(sgp, periaps, sma); //this could be ralitivly high speed if orbit is elliptical

            // this is an 'outofmyass' calculation, I'm suprised it's so far, I always thought of the atmosphere as being ralitivly thin compared to the radius.  
            //earths radius is ~6.7km, the Karman line is 100km, 
            //the mesopause is 80km, 
            //the ISS orbits at 330 to 410km
            // radius of earth * 15 gets close to the karman line. 
            var lowOrbitDistance = bodyRadius * 15;

            var dvAtLowOrbit = PreciseOrbitalSpeed(sgp, lowOrbitDistance, lowOrbitDistance); //dv to a low circular orbit.  

            var dvDifference = dvAtPeriapsis - dvAtLowOrbit;
            var distanceDifference = periaps - lowOrbitDistance;

            double heightOfAtmoBoundry = 0;
            float atmoPressure = 0;
            if (departBody.HasDataBlob<AtmosphereDB>())
            {

                var atmo = departBody.GetDataBlob<AtmosphereDB>();
                atmoPressure = atmo.Pressure;
                heightOfAtmoBoundry = lowOrbitDistance * 0.25;
            }
            //var timeToLowOrbit = thrustToWeight
            //var timeToLowOrbit



            double timeSpentInTroposphere;
            double timeSpentInStratosphere;
            double timeSpentInMesosphere;
            double timeSpentInThermosphere;



            throw new NotImplementedException();
        }

        public static Vector4 Pos(double combinedMass, double semiMajAxis, double meanAnomaly, double eccentricity, double aoP, double loAN, double i)
        {
            var G = 6.6725985e-11;


            double eca = meanAnomaly + eccentricity / 2;
            double diff = 10000;
            double eps = 0.000001;
            double e1 = 0;

            while (diff > eps)
            {
                e1 = eca - (eca - eccentricity * Math.Sin(eca) - meanAnomaly) / (1 - eccentricity * Math.Cos(eca));
                diff = Math.Abs(e1 - eca);
                eca = e1;
            }

            var ceca = Math.Cos(eca);
            var seca = Math.Sin(eca);
            e1 = semiMajAxis * Math.Sqrt(Math.Abs(1 - eccentricity * eccentricity));
            var xw = semiMajAxis * (ceca - eccentricity);
            var yw = e1 * seca;

            var edot = Math.Sqrt((G * combinedMass) / semiMajAxis) / (semiMajAxis * (1 - eccentricity * ceca));
            var xdw = -semiMajAxis * edot * seca;
            var ydw = e1 * edot * ceca;

            var Cw = Math.Cos(aoP);
            var Sw = Math.Sin(aoP);
            var co = Math.Cos(loAN);
            var so = Math.Sin(loAN);
            var ci = Math.Cos(i);
            var si = Math.Sin(i);
            var swci = Sw * ci;
            var cwci = Cw * ci;
            var pX = Cw * co - so * swci;
            var pY = Cw * so + co * swci;
            var pZ = Sw * si;
            var qx = -Sw * co - so * cwci;
            var qy = -Sw * so + co * cwci;
            var qz = Cw * si;

            return new Vector4()
            {
                X = xw * pX + yw * qx,
                Y = xw * pY + yw * qy,
                Z = xw * pZ + yw * qz
            };
        }
    }

    /// <summary>
    /// A bunch of convenient functions for calculating various ellipse parameters.
    /// </summary>
    public static class EllipseMath
    {
        /// <summary>
        /// SemiMajorAxis from SGP and SpecificEnergy
        /// </summary>
        /// <returns>The major axis.</returns>
        /// <param name="sgp">Standard Gravitational Parameter</param>
        /// <param name="specificEnergy">Specific energy.</param>
        public static double SemiMajorAxis(double sgp, double specificEnergy)
        {
            return sgp / (2 * specificEnergy);
        }

        public static double SemiMajorAxisFromApsis(double apoapsis, double periapsis)
        {
            return (apoapsis + periapsis) / 2;
        }
        public static double SemiMajorAxisFromLinerEccentricity(double linierEccentricity, double eccentricity)
        {
            return linierEccentricity * eccentricity;
        }
        public static double SemiMinorAxis(double semiMajorAxis, double eccentricity)
        {
            return semiMajorAxis * Math.Sqrt(1 - eccentricity * eccentricity);
        }

        public static double SemiMinorAxisFromApsis(double apoapsis, double periapsis)
        {
            return Math.Sqrt(Math.Abs(apoapsis) * Math.Abs(periapsis));
        }

        public static double LinierEccentricity(double appoapsis, double semiMajorAxis)
        {
            return appoapsis - semiMajorAxis;
        }
        public static double LinierEccentricityFromEccentricity(double semiMajorAxis, double eccentricity)
        {
            return semiMajorAxis * eccentricity;
        }
        public static double Eccentricity(double linierEccentricity, double semiMajorAxis)
        {
            return linierEccentricity / semiMajorAxis;
        }

        public static double Apoapsis(double eccentricity, double semiMajorAxis)
        {
            return (1 + eccentricity) * semiMajorAxis;
        }
        public static double Periapsis(double eccentricity, double semiMajorAxis)
        {
            return (1 - eccentricity) * semiMajorAxis;
        }

        public static double AreaOfEllipseSector(double semiMaj, double semiMin, double firstAngle, double secondAngle)
        {

            var theta1 = firstAngle;
            var theta2 = secondAngle;
            var theta3 = theta2 - theta1;

            //var foo2 = Math.Atan((semiMin - semiMaj) * Math.Sin(2 * theta2) / (semiMaj + semiMin + (semiMin - semiMaj) * Math.Cos(2 * theta2)));
            var foo2 = Math.Atan2((semiMin - semiMaj) * Math.Sin(2 * theta2) , (semiMaj + semiMin + (semiMin - semiMaj) * Math.Cos(2 * theta2)));
            //var foo3 = Math.Atan((semiMin - semiMaj) * Math.Sin(2 * theta1) / (semiMaj + semiMin + (semiMin - semiMaj) * Math.Cos(2 * theta1)));
            var foo3 = Math.Atan2((semiMin - semiMaj) * Math.Sin(2 * theta1) , (semiMaj + semiMin + (semiMin - semiMaj) * Math.Cos(2 * theta1)));

            var area = semiMaj * semiMin / 2 * (theta3 - foo2 + foo3);

            return area;
        }

    }
}
