// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

namespace BlazorServerPush;

#pragma warning disable CA1812 // Instantiated by DI
sealed class GlobalCounter : NotifyPropertyChanged
#pragma warning restore CA1812
{
    long _Count;
    public long Count => Interlocked.Read(ref _Count);

    public void Increment()
    {
        _ = Interlocked.Increment(ref _Count);
        OnNotifyPropertyChanged(nameof(Count));
    }

    bool _Enabled;
    public bool Enabled
    {
        get => _Enabled;
        set
        {
            if (SetProperty(ref _Enabled, value))
            {
                var myGeneration = ++Generation;
                if (value)
                {
                    _ = Task.Run(() =>
                    {
                        while (myGeneration == Generation)
                        {
                            Increment();
                        }
                    });
                }
            }
        }
    }

    volatile uint Generation;
}
