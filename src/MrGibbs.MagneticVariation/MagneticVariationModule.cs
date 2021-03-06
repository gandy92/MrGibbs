﻿using System;
using MrGibbs.Contracts;
using MrGibbs.Configuration;
using Ninject.Modules;

namespace MrGibbs.MagneticVariation
{
    /// <summary>
    /// configures types releavant to this plugin
    /// </summary>
	public class MagneticVariationModule:NinjectModule
	{
		public override void Load()
		{
			Kernel.Bind<IPlugin> ()
				.To<MagneticVariationPlugin> ()
				.InSingletonScope ()
				.WithConstructorArgument("cofFilePath"
					,ConfigurationHelper.ReadStringAppSetting("cofFilePath",
						ConfigurationHelper.FindNewestFileWithExtension("COF")));

		}
	}
}