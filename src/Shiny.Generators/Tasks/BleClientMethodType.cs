using System;


namespace Shiny.Generators.Tasks
{
    public enum BleClientMethodType
    {
        None,

        // no parameters with return value
        ReadAsync,
        ReadObservable,

        // 1 paramater, no return value
        WriteAsync,
        WriteObservable,

        // 1 stream parameter, no return value or required Observable<CharacteristicGattResult>?
        WriteStream,

        // 1 paramater /w return value
        RequestAsync,
        RequestObservable,

        // observable with no parameter and attribute
        Notification
    }
}
