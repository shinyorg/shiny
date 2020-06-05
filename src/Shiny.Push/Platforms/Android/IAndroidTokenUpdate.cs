using System;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IAndroidTokenUpdate
    {
        Task UpdateNativePushToken(string token);
    }
}
