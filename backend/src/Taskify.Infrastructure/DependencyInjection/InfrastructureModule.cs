using System.ComponentModel;
using Autofac;
using Taskify.Domain.Interfaces;
using Taskify.Infrastructure.MFilesInterop;

namespace Taskify.Infrastructure.DependencyInjection;

public class InfrastructureModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MFilesVaultConnectionManager>()
            .As<IVaultConnectionManager>()
            .SingleInstance()
            .OnRelease(instance => instance.Dispose());
    }
}