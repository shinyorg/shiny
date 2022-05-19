﻿using Shiny;
using Shiny.BluetoothLE;

namespace Sample;


public class SampleBleDelegate : IBleDelegate
{
    public SampleBleDelegate()
    {
    }

    public Task OnAdapterStateChanged(AccessState state)
    {
        return Task.CompletedTask;
    }


    public Task OnConnected(IPeripheral peripheral)
    {
        return Task.CompletedTask;
    }
}

