using System;

namespace Shiny.Push;


public record FirebaseConfig(
    bool UseEmbeddedConfiguration,
    string? AppId = null,
    string? SenderId = null,
    string? ApiKey = null,
    string? ProjectId = null
)
{
    public void AssertValid()
    {
        if (this.UseEmbeddedConfiguration)
            return;

        if (this.AppId == null)
            throw new ArgumentNullException(nameof(this.AppId));

        if (this.SenderId == null)
            throw new ArgumentNullException(nameof(this.SenderId));

        if (this.ApiKey == null)
            throw new ArgumentNullException(nameof(this.ApiKey));

        if (this.ProjectId == null)
            throw new ArgumentNullException(nameof(this.ProjectId));
    }
}