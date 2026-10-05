using System.Reflection;
using System.Runtime.Versioning;

namespace DnsTube.Service.Tests;

/// <summary>
/// DnsTube ships framework-dependent, and install-service.bat, the README and the
/// release notes all name the runtime users must install. This guards that the
/// shipped assemblies target that runtime.
/// </summary>
public class TargetFrameworkTests
{
	private const string ExpectedFramework = ".NETCoreApp,Version=v10.0";

	[Fact]
	public void Service_targets_dotnet_10()
	{
		Assert.Equal(ExpectedFramework, TargetFrameworkOf(typeof(Program).Assembly));
	}

	[Fact]
	public void Core_targets_dotnet_10()
	{
		Assert.Equal(ExpectedFramework, TargetFrameworkOf(typeof(DnsTube.Core.Services.DbService).Assembly));
	}

	private static string? TargetFrameworkOf(Assembly assembly)
	{
		return assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
	}
}
