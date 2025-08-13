using System;
using Microsoft.Extensions.DependencyInjection;

namespace ObligatorioTT.Helpers
{
    public static class ServiceHelper
    {
        public static IServiceProvider Services { get; set; } = default!;

        public static T GetService<T>() where T : class =>
            Services.GetRequiredService<T>();
    }
}
