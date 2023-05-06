using System.Reflection;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * Copyright (c) 2022 kasvoton
 * All rights reserved.
 *
 */

namespace Utility;

public sealed class Properties
{
	public static string GetProduct()
	{
		try
		{
			Assembly asm = typeof(Properties).Assembly;
			object[] attribs = asm.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
			return ((AssemblyProductAttribute)attribs[0]).Product;
		}
		catch { return string.Empty; }
	}

	public static string GetCopyright()
	{
		try
		{
			Assembly asm = typeof(Properties).Assembly;
			object[] attribs = asm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true);
			return ((AssemblyCopyrightAttribute)attribs[0]).Copyright;
		}
		catch { return string.Empty; }
	}

	public static string GetVersion()
	{
		try
		{
			Assembly asm = typeof(Properties).Assembly;
			object[] attribs = asm.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), true);
			return ((AssemblyInformationalVersionAttribute)attribs[0]).InformationalVersion;
		}
		catch { return "Unknown"; }
	}
}
