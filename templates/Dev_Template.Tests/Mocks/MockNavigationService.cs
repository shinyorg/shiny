using Prism.Navigation;
using System;
using System.Threading.Tasks;


namespace $safeprojectname$.Mocks
{
    public class MockNavigationService : INavigationService
    {

        public Task<INavigationResult> GoBackAsync()
        {
            throw new NotImplementedException();
        }

        public Task<INavigationResult> GoBackAsync(INavigationParameters parameters)
        {
            throw new NotImplementedException();
        }

        public Task<INavigationResult> NavigateAsync(Uri uri)
        {
            throw new NotImplementedException();
        }

        public Task<INavigationResult> NavigateAsync(Uri uri, INavigationParameters parameters)
        {
            throw new NotImplementedException();
        }


        public string LastPageName { get; private set; }
        public INavigationParameters LastParameters { get; private set; }
        public Task<INavigationResult> NavigateAsync(string name)
        {
            this.LastPageName = name;
            this.LastParameters = null;
            return Success();
        }

        public Task<INavigationResult> NavigateAsync(string name, INavigationParameters parameters)
        {
            this.LastPageName = name;
            this.LastParameters = parameters;
            return Success();
        }


        static Task<INavigationResult> Success() => Task.FromResult<INavigationResult>(new NavigationResult { Success = true });
    }
}
