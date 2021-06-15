using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.Eternity.Tests.Mocks
{
    public class MockEngine
    {

        public MockEngine(Action<IServiceCollection> builder = null)
        {
            Clock = new MockClock();
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton<IEternityClock>(Clock);
            services.AddSingleton<IEternityStorage, MockStorage>();
            services.AddSingleton<MockEmailService>();
            builder?.Invoke(services);
            this.Services = services.BuildServiceProvider();
        }

        public readonly IServiceProvider Services;

        public readonly MockClock Clock;

    }
}
