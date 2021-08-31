using System;
using FluentAssertions;
using Shiny.Infrastructure;


namespace Shiny.Tests.Core.Serialization
{
    public abstract class BaseSerializationTests<T> where T : ISerializer
    {
        protected abstract T Create();


        //[Fact]
        //public virtual void GeofenceRegion()
        //{
        //    var start = new GeofenceRegion("testing", new Position(59.1, 62.5), Distance.FromKilometers(99))
        //    {
        //        NotifyOnEntry = true,
        //        NotifyOnExit = true,
        //        SingleUse = true
        //    };
        //    this.RunTest(start);
        //}


        //[Fact]
        //public virtual void Notification()
        //{
        //    var start = new Notification
        //    {
        //        Title = "TheTitle",
        //        Message = "TheMessage",
        //        BadgeCount = 1,

        //        Sound = NotificationSound.DefaultSystem,
        //        Android = new AndroidOptions
        //        {
        //            UseBigTextStyle = true
        //        }
        //    };
        //    this.RunTest(start);
        //}


        protected void RunTest(T obj, Action<T> postAction = null)
        {
            var serializer = this.Create();
            var value = serializer.Serialize(obj);
            var end = serializer.Deserialize<T>(value);
            obj.Should().Be(end);
            postAction?.Invoke(end);
        }
    }
}
