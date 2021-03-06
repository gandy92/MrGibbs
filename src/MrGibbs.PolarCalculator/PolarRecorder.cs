﻿using System;
using System.Data;
using System.Linq;
using System.Transactions;

using Dapper;

using MrGibbs.Contracts;
using MrGibbs.Models;

namespace MrGibbs.PolarCalculator
{
	public class PolarRecorder:IRecorder,ICalculator
	{
		private IPlugin _plugin;
		private IDbConnection _connection;
		//TODO: make these configurable?
		private double _angleResolution=5;
		private double _speedResolution=0.5;

		private bool _forceSymmetricalPolar;

		public PolarRecorder (IDbConnection connection,bool forceSymmetricalPolar)
		{
			_connection = connection;
			_forceSymmetricalPolar = forceSymmetricalPolar;
			Initialize ();
		}

		public IPlugin Plugin {
			get {
				return _plugin;
			}
			set {
				_plugin = value;
			}
		}

		public void Dispose ()
		{
			//don't need to do anything here, we need to rely on the module to cleanup connections
		}

		private void Initialize ()
		{
			//using (var transaction = new TransactionScope()) 
			{
				if (!_connection.Query<string> ("SELECT name FROM sqlite_master WHERE type='table' and name='Polar';").Any ()) {
					//if the table doesn't exist, create it
					_connection.Execute ("create table Polar(" +
										"Id INTEGER PRIMARY KEY ASC," +
										"TrueWindAngle NUMERIC," +
										"TrueWindSpeedKnots NUMERIC," +
										"SpeedInKnots NUMERIC," +
										"Time DATETIME" +
										")");
				}
				//transaction.Complete ();
			}
		}

		public void Record (State state)
		{
			//make sure we have the sensor data we need, otherwise there's no point
			if (StateHasRequiredValues(state)) 
			{
				//using (var transaction = new TransactionScope()) 
				{
					var existing = FindValue (state);

					if (existing != null) 
					{
						if (existing.SpeedInKnots < state.StateValues [StateValue.SpeedInKnots]) 
						{
							existing.SpeedInKnots = state.StateValues [StateValue.SpeedInKnots];
							existing.Time = state.BestTime;
							_connection.Execute ("update Polar set SpeedInKnots=@SpeedInKnots,Time=@Time where Id=@Id", existing);
						}
					} 
					else 
					{
						double speed = state.StateValues [StateValue.TrueWindSpeedKnots];
						double angle = state.StateValues [StateValue.TrueWindAngle];
						NormalizeWind (ref angle, ref speed);

						var newValue = new PolarValue () 
						{
							Time = state.BestTime,
							TrueWindAngle = angle,
							TrueWindSpeedKnots = speed,
							SpeedInKnots = state.StateValues [StateValue.SpeedInKnots]
						};
						_connection.Execute ("insert into Polar(TrueWindAngle,TrueWindSpeedKnots,SpeedInKnots,Time) values (@TrueWindAngle,@TrueWindSpeedKnots,@SpeedInKnots,@Time)", newValue);
					}
					//transaction.Complete();
				}
			}
		}

		private bool StateHasRequiredValues (State state)
		{
			return state.StateValues.ContainsKey (StateValue.TrueWindAngle)
						&& state.StateValues.ContainsKey (StateValue.TrueWindSpeedKnots)
						&& state.StateValues.ContainsKey (StateValue.SpeedInKnots);
		}

		private void NormalizeWind(ref double angle, ref double speed)
		{
			//normalize to angle between 0 and 360
			angle = angle % 360;

			//make it positive
			if (angle < 0) 
			{
				angle = 360 + angle;
			}

			//normalize to one half of the circle
			if (_forceSymmetricalPolar && angle > 180) 
			{
				angle = 360 - angle;
			}

			//normalize precision
			angle = angle - (angle % _angleResolution);

			//normalize speed precision
			speed = speed - (speed % _speedResolution);
		}

		private PolarValue FindValue (State state)
		{
			//get the exact values from the state
			double windSpeed = state.StateValues [StateValue.TrueWindSpeedKnots];
			double windAngle = state.StateValues [StateValue.TrueWindAngle];

			//round/normalize them to fit in our polar
			NormalizeWind (ref windAngle, ref windSpeed);

			//find the existing segment in the graph (if it exists)
			var newPolarValue = new PolarValue () {
				TrueWindAngle = windAngle,
				TrueWindSpeedKnots = windSpeed,
				SpeedInKnots = state.StateValues[StateValue.SpeedInKnots],
				Time = state.BestTime
			};
			var existing = _connection.Query<PolarValue> ("select * from Polar where TrueWindAngle=@TrueWindAngle and TrueWindSpeedKnots<=@TrueWindSpeedKnots order by SpeedInKnots desc", newPolarValue).FirstOrDefault();

			return existing;
		}

		public void Calculate (State state)
		{
			if (StateHasRequiredValues (state)) {
				var polarValue = FindValue (state);
				if (polarValue != null) {
					state.StateValues [StateValue.PeakSpeedInKnotsForWind] = polarValue.SpeedInKnots;
					state.StateValues [StateValue.PeakSpeedPercentForWind] = state.StateValues [StateValue.SpeedInKnots] / polarValue.SpeedInKnots * 100.0;
				}
			}
		}
	}
	public class PolarValue
	{
		public int Id { get; set; }
		public double TrueWindAngle { get; set; }
		public double TrueWindSpeedKnots { get; set; }
		public double SpeedInKnots { get; set; }
		public DateTime Time { get; set; }
	}
}

