﻿using System;
using OpenBveApi.Runtime;

namespace Plugin
{
    internal class ATP : Device
    {
        const float ATP_SAFETY_DECELERATION_RATE = -0.70f; //m/s
        const float ATP_SAFETY_STOPPING_DISTANCE = 40.0f; //Buffer distance to next train

        const float ATP_TARGET_DECELERATION_RATE = -0.50f; //m/s
        const float ATP_TARGET_STOPPING_DISTANCE = 75.0f;

        const float ATP_RESTART_DISTANCE = 20.0f; //Distance for preceding train to move before moving off

        const bool ATP_FORCE_STATION_STOP = true; //Bring speed code down to 0 at every station
        const double ATP_STATION_SAFETY_DISTANCE = 4.0f;
        const double ATP_STATION_TARGET_DISTANCE = 2.0f;
        private double? upcomingStopLocation = null;

        const double ATP_OVERSPEED_ALERT_THRESHOLD = 3.0f;
        const double ATP_OVERSPEED_COAST_THRESHOLD = 1.0f;
        const double ATP_TARGET_OFFSET = 10.0f;
        SoundHandle atpOverspeedSoundHandle = null;

        private Train train;

        private double currentLocation;


       /* private enum AtpStates
        {
            Off,
            Active,
            Tripped
        }*/
        //private double ATP_WARNING_DURATION = 3.0;
        //private AtpStates atpState = AtpStates.Off;
        //double? atpTripTimer = null;

        public ATP(Train train)
        {
            this.train = train;
        }

        internal override int? Elapse(ElapseData data)
        {
            currentLocation = data.Vehicle.Location;

            //Calculate signalling status
            //Distance to next train
            UpdateAtpSpeeds(data, data.PrecedingVehicle);

            //Naive overspeed prevention that applies FSB
            if(train.trainModeActual == Train.TrainModes.CodedManual)
            {
                if(data.Vehicle.Speed.KilometersPerHour > train.atpSafetySpeed)
                {
                    return -train.specs.BrakeNotches;
                }
                else if(data.Vehicle.Speed.KilometersPerHour > train.atpSafetySpeed - ATP_OVERSPEED_COAST_THRESHOLD)
                {
                    return 0;
                }

                if(data.Vehicle.Speed.KilometersPerHour > train.atpSafetySpeed - ATP_OVERSPEED_ALERT_THRESHOLD)
                {
                    //play sound
                    if(atpOverspeedSoundHandle == null)
                    {
                        atpOverspeedSoundHandle = train.PlaySound(0, 1, 1, true);
                    }
                }
                else
                {
                    if (atpOverspeedSoundHandle != null)
                    {
                        atpOverspeedSoundHandle.Stop();
                        atpOverspeedSoundHandle = null;
                    }
                }
            }

            return null;
            //Check trip count
            //Check for speed limits
            //ElapseTripTimer(data);

            //Is train in RM?
            /*if (train.trainModeActual == Train.TrainModes.RestrictedManualForward ||
                train.trainModeActual == Train.TrainModes.RestrictedManualReverse ||
                train.trainModeActual == Train.TrainModes.Off)
            {
                atpState = AtpStates.Off;
            }
            else
            {
                switch (atpState)
                {
                    case AtpStates.Active:
                        if (atpTripTimer <= 0.0)
                        {
                            atpState = AtpStates.Tripped;
                        }
                        return null;
                    case AtpStates.Tripped:

                        return -train.specs.BrakeNotches - 1;
                    default:
                        return null;
                }
            }*/
        }

        /*internal void ElapseTripTimer(ElapseData data)
        {
            if (atpTripTimer != null)
            {
                atpTripTimer -= data.ElapsedTime.Seconds;
                atpTripTimer = Math.Max(atpTripTimer ?? 0, 0);
                if (data.Vehicle.Speed.KilometersPerHour <= train.atpTrackSafetySpeed)
                {
                    atpTripTimer = null;
                }
            }
            else if (data.Vehicle.Speed.KilometersPerHour > train.atpTrackSafetySpeed)
            {
                atpTripTimer = ATP_WARNING_DURATION;
            }
        }*/

        internal void UpdateAtpSpeeds(ElapseData data, PrecedingVehicleState precedingVehicle)
        {
            if (train.atpTrackNextSpeedPosition >= data.Vehicle.Location)
            {
                //Calculate braking curve to upcoming speed limit
                double newTargetSpeed = CalculateSpeedToStop(ATP_TARGET_DECELERATION_RATE, (train.atpTrackNextSpeedPosition - data.Vehicle.Location), train.atpTrackNextMaxSpeed);
                double newSafetySpeed = CalculateSpeedToStop(ATP_SAFETY_DECELERATION_RATE, (train.atpTrackNextSpeedPosition - data.Vehicle.Location), train.atpTrackNextSafetySpeed);

                //Select whichever speed is lower
                train.atpTrackMaxSpeed = Math.Min(newTargetSpeed, train.atpTrackMaxSpeed);
                train.atpTrackSafetySpeed = Math.Min(newSafetySpeed, train.atpTrackSafetySpeed);
            }

            if (ATP_FORCE_STATION_STOP)
            {
                if (upcomingStopLocation.HasValue)
                {
                    //Calculate braking curve to upcoming station stop
                    double newTargetSpeed = CalculateSpeedToStop(ATP_TARGET_DECELERATION_RATE, (upcomingStopLocation.Value + ATP_STATION_TARGET_DISTANCE - data.Vehicle.Location), 0);
                    double newSafetySpeed = CalculateSpeedToStop(ATP_SAFETY_DECELERATION_RATE, (upcomingStopLocation.Value + ATP_STATION_SAFETY_DISTANCE - data.Vehicle.Location), 0);

                    //Select whichever speed is lower
                    train.atpMaxSpeed = Math.Min(newTargetSpeed, train.atpTrackMaxSpeed);
                    train.atpSafetySpeed = Math.Min(newSafetySpeed, train.atpTrackSafetySpeed);
                    train.atpTargetSpeed = 0;
                }
                else
                {
                    var prevMaxSpeed = train.atpMaxSpeed;
                    train.atpMaxSpeed = train.atpTrackMaxSpeed;
                    train.atpSafetySpeed = train.atpTrackSafetySpeed;

                    if(train.atpTargetSpeed != 0)
                    {
                        train.currHoldSpeed = train.atpTargetSpeed;
                    }

                    if (prevMaxSpeed > train.atpMaxSpeed)
                    {
                        train.atpTargetSpeed = 0;
                    }
                    else
                    {
                        train.atpTargetSpeed = train.atpMaxSpeed - ATP_TARGET_OFFSET;
                    }
                }
            }

            if (precedingVehicle != null)
            {
                if (ATP_FORCE_STATION_STOP && (upcomingStopLocation - currentLocation) > precedingVehicle.Distance)
                {
                    //Calculate distance to next train
                    double distanceToStop = precedingVehicle.Distance - ATP_TARGET_STOPPING_DISTANCE;

                    train.atpMaxSpeed = Math.Max(Math.Min(CalculateSpeedToStop(ATP_TARGET_DECELERATION_RATE, distanceToStop), train.atpTrackMaxSpeed), 0.0);
                    if (Double.IsNaN(train.atpMaxSpeed))
                    {
                        throw new ApplicationException();
                    }

                    train.atpSafetySpeed = Math.Min(CalculateSpeedToStop(ATP_SAFETY_DECELERATION_RATE, (precedingVehicle.Distance - ATP_SAFETY_STOPPING_DISTANCE)), train.atpTrackSafetySpeed);
                    if (Double.IsNaN(train.atpSafetySpeed))
                    {
                        throw new ApplicationException();
                    }
                }
            }
        }
        internal double CalculateSpeedToStop(double acceleration, double distance, double speed = 0)
        {
            speed = speed / 3.6; //Convert to m/s

            double result = Math.Sqrt((2 * -acceleration * distance) + (speed * speed));

            if(Double.IsNaN(result))
            {
                return 0;
            }

            return result*3.6; //Convert to km/h
        }
        internal double CalculateDistanceToStop(double acceleration, double speed)
        {
            if (acceleration == 0.0)
            {
                return 0.0;
            }
            return -(speed * speed) / (2 * acceleration);
        }

        internal override void Initialize(InitializationModes mode)
        {
        }

        internal override void HornBlow(HornTypes type)
        {

        }

        internal override void KeyDown(VirtualKeys key)
        {

        }

        internal override void KeyUp(VirtualKeys key)
        {

        }

        internal override void SetBeacon(BeaconData beacon)
        {
            if (beacon.Type == 31) //Target and safety track speeds
            {
                train.atpTrackMaxSpeed = (int)(beacon.Optional / 1000);
                train.atpTrackSafetySpeed = (int)(beacon.Optional % 1000);
            }
            if (beacon.Type == 32) //Upcoming target and safety track speeds
            {
                train.atpTrackNextSpeedPosition = (int)(beacon.Optional / 1000000) + currentLocation;
                train.atpTrackNextMaxSpeed = (int)((int)(beacon.Optional / 1000) % 1000);
                train.atpTrackNextSafetySpeed = (int)(beacon.Optional % 1000);
            }
            if (beacon.Type == 33) //Distance to stop point
            {
                upcomingStopLocation = beacon.Optional + currentLocation;
            }
        }

        internal override void SetBrake(int brakeNotch)
        {

        }

        internal override void SetPower(int powerNotch)
        {

        }

        internal override void SetReverser(int reverser)
        {

        }

        internal override void SetSignal(SignalData[] signal)
        {

        }

        internal override void DoorChange(DoorStates oldState, DoorStates newState)
        {
            //Door open or closed; reset next stop
            upcomingStopLocation = null;
        }

    }
}
