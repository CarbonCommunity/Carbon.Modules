using System;
using API.Logger;

/*
 *
 * Copyright (c) 2022-2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Utility;

public sealed class Logger
{
	private static readonly ILogger _handle = null;

	internal static void Log(string text)
	{
		if (_handle is null) return;
		_handle.Console($"[{Properties.GetProduct()}] {text}", Severity.Notice);
	}

	internal static void Warning(string text)
	{
		if (_handle is null) return;
		_handle.Console($"[{Properties.GetProduct()}] {text}", Severity.Warning);
	}

	internal static void Error(string text, Exception e = null)
	{
		if (_handle is null) return;
		_handle.Console($"[{Properties.GetProduct()}] {text}", Severity.Error, e);
	}
}