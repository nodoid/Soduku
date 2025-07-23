using CommunityToolkit.Mvvm.Messaging;
using Sudoku.Interfaces;
using Suduku.Services;

namespace Sudoku.Helpers
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
