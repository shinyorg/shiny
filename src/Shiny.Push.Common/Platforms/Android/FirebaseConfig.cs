using System;

namespace Shiny.Push;


public record FirebaseConfig(
    string? AppId,
    string? SenderId,
    string? ApiKey,
    string? ProjectId
)
{
    public void AssertValid()
    {
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