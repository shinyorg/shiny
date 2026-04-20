using System;

namespace Shiny.Locations;


public class StationaryDetector
{
    Position? lastPosition;
    DateTimeOffset? lastMovement;

    public bool IsStationary { get; private set; }


    public bool Detect(GpsReading reading, int metersThreshold, int secondsThreshold)
    {
        this.lastMovement ??= reading.Timestamp;

        if (this.lastPosition != null)
        {
            var distance = reading.Position.GetDistanceTo(this.lastPosition);
            if (distance.TotalMeters < metersThreshold)
            {
                var time = reading.Timestamp - this.lastMovement;
                if (time!.Value.TotalSeconds >= secondsThreshold)
                    this.IsStationary = true;
            }
            else
            {
                this.lastMovement = reading.Timestamp;
                this.IsStationary = false;
            }
        }

        this.lastPosition = reading.Position;
        return this.IsStationary;
    }


    public void Reset()
    {
        this.lastPosition = null;
        this.lastMovement = null;
        this.IsStationary = false;
    }
}
