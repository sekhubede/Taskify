using Autofac;
using Taskify.Application.VaultConnection.Services;

namespace Taskify.Application.DependencyInjection;

public class ApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<VaultConnectionService>()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}
