using Autofac;
using ServiceStack.Configuration;

namespace app.Infrastructure
{
    public class AutofacIocAdapter : IContainerAdapter
    {
        private readonly IContainer container;

        public AutofacIocAdapter(IContainer container)
        {
            this.container = container;
        }

        public T Resolve<T>()
        {
            return container.Resolve<T>();
        }

        public T TryResolve<T>()
        {
            T result;

            if (container.TryResolve<T>(out result))
            {
                return result;
            }

            return default(T);
        }
    }
}
