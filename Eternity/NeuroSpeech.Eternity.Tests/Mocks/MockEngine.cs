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
            Storage = new MockStorage(Clock);
            Bag = new MockBag();
            EmailService = new MockEmailService();
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton<IEternityClock>(Clock);
            services.AddSingleton(Bag);
            services.AddSingleton<IEternityStorage>(Storage);
            services.AddSingleton(EmailService);
            services.AddSingleton<EternityContext>();
            builder?.Invoke(services);
            this.Services = services.BuildServiceProvider();
        }

        public readonly IServiceProvider Services;

        public readonly MockBag Bag;

        public readonly MockClock Clock;

        public readonly MockStorage Storage;

        public readonly MockEmailService EmailService;

        public T Resolve<T>()
        {
            return Services.GetRequiredService<T>();
        }

    }
}
