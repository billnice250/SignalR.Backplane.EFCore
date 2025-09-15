using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SignalR.Backplane.EFCore.Interfaces;
using SignalR.Backplane.EFCore.Options;
using System;

namespace SignalR.Backplane.EFCore.EF;

public static class EfBackplaneExtensions
{
    public static ISignalRBuilder AddBackplaneDbContext<TContext>(
        this ISignalRBuilder builder,
        Action<DbContextOptionsBuilder> optionsAction,
        Action<EfBackplaneOptions>? configure = null)
        where TContext : DbContext, IBackplaneDbContext
    {
        var options = new EfBackplaneOptions();
        configure?.Invoke(options);

        builder.Services.AddSingleton(options);
        builder.Services.AddDbContextFactory<TContext>(optionsAction);
        builder.Services.AddSingleton<EfBackplaneCleaner<TContext>>();
        builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<EfBackplaneCleaner<TContext>>());
        builder.Services.AddSingleton<IBackplaneStore, EfBackplaneStore<TContext>>();
        builder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(SignalR.BusHubLifetimeManager<>));

        return builder;
    }
}
