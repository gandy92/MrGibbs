﻿using System;
using System.Collections.Generic;

using MrGibbs.Contracts;
using MrGibbs.Contracts.Infrastructure;
using MrGibbs.Contracts.Persistence;

namespace MrGibbs.StateLogger
{
	/// <summary>
	/// Plugin to write important state fields to sqlite table
	/// </summary>
	public class StateLoggerPlugin:IPlugin
	{
		private ILogger _logger;
		private bool _initialized = false;
		private IList<IPluginComponent> _components;
		private StateRecorder _recorder;

		public StateLoggerPlugin(ILogger logger,StateRecorder recorder)
		{
			_logger = logger;
			_recorder = recorder;
		}

		/// <inheritdoc />
		public void Initialize(PluginConfiguration configuration, Action<Action<ISystemController, IRaceController>> queueCommand)
		{
			if (_recorder != null) 
			{
				_recorder.Dispose ();
			}

			_components = new List<IPluginComponent>();

			_initialized = false;

			_recorder.Plugin = this;
			configuration.Recorders.Add (_recorder);
			_components.Add (_recorder);

			_initialized = true;
		}

		/// <inheritdoc />
		public bool Initialized
		{
			get { return _initialized; }
		}

		/// <inheritdoc />
		public IList<IPluginComponent> Components
		{
			get { return _components; }
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (_components != null)
			{
				foreach (var component in _components)
				{
					component.Dispose();
				}
			}
		}
	}
}

