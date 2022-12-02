using System.Threading.Tasks;

namespace Shiny;


public interface IPermissions
{
    /// <summary>
    /// Will set or replace any current permission checks
    /// </summary>
    /// <typeparam name="TPermission"></typeparam>
    /// <param name="permissionName"></param>
    void Request(IPermission permission);

}


// TODO: android needs access to top activity
// TODO: ios needs mainthread
public interface IPermission
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task Request(string permissionName);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Task GetCurrent();
}