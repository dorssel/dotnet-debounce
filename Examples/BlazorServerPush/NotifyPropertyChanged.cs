// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BlazorServerPush;

public class NotifyPropertyChanged : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnNotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (!Equals(field, value))
        {
            field = value;
            OnNotifyPropertyChanged(propertyName);
            return true;
        }
        else
        {
            return false;
        }
    }
}
