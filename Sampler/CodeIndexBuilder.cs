using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sampler;

/// <summary>
/// Given a list of assemblies, this class will build an index of all the public types and members in those assemblies, so that we can
/// quickly look up information about them later.
/// </summary>
internal static class CodeIndexBuilder
{
	//public static CodeIndex BuildIndex(IEnumerable<Assembly> assemblies)
	//{
	//	var index = new CodeIndex();
	//	foreach (var assembly in assemblies)
	//	{
	//		foreach (var type in assembly.GetExportedTypes())
	//		{
	//			index.Types[type.FullName] = type;
	//			foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
	//			{
	//				index.Members[$"{type.FullName}.{member.Name}"] = member;
	//			}
	//		}
	//	}
	//	return index;
	//}
}
