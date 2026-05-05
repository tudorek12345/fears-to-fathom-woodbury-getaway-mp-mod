using System;
using System.Linq;
using System.Reflection;

namespace PixelCrushers;

public static class RuntimeTypeUtility
{
	public static Type GetTypeFromName(string typeName)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			try
			{
				Type type = assembly.GetType(typeName);
				if (type != null)
				{
					return type;
				}
			}
			catch (Exception)
			{
			}
		}
		return null;
	}

	public static Assembly[] GetAssemblies()
	{
		return AppDomain.CurrentDomain.GetAssemblies();
	}

	public static Type GetWrapperType(Type type)
	{
		if (type == null || string.IsNullOrEmpty(type.Namespace) || !type.Namespace.StartsWith("PixelCrushers"))
		{
			return type;
		}
		try
		{
			string wrapperName = type.Namespace + ".Wrappers." + type.Name;
			Assembly[] assemblies = GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				try
				{
					Type[] array = (from assemblyType in assembly.GetExportedTypes()
						where string.Equals(assemblyType.FullName, wrapperName)
						select assemblyType).ToArray();
					if (array.Length != 0)
					{
						return array[0];
					}
				}
				catch (Exception)
				{
				}
			}
		}
		catch (Exception)
		{
		}
		return null;
	}
}
