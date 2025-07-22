using CommunityToolkit.Mvvm.Messaging;
using TimeOfDeath_MAUI.ViewModels;
using TimeOfDeath_MAUI.Interfaces;
using TimeOfDeath_MAUI.Services;
using Soduku.Interfaces;
using Soduku.Services;

namespace Soduku.Helpers
{
    public static class InjectionContainer
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection services)
        {
            var i = new ServiceCollection();

            i.AddSingleton<IMessenger>(WeakReferenceMessenger.Default).
                AddSingleton<IDataChecking, DataChecking>();

            services = i;

            return services;
        }
    }
}
