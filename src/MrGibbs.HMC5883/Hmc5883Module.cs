﻿using System;

using Ninject;
using Ninject.Modules;

using MrGibbs.Contracts;
using MrGibbs.Configuration;

namespace MrGibbs.HMC5883
{
    /// <summary>
    /// configures types releavant to this plugin
    /// </summary>
	public class Hmc5883Module:NinjectModule
	{
		public override void Load()
		{
			Kernel.LoadIfNotLoaded<I2CModule> ();

			Kernel.Bind<IPlugin> ()
				.To<Hmc5883Plugin> ()
				.InSingletonScope ();
		}
	}
}