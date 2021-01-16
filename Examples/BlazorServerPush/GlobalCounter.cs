// SPDX-FileCopyrightText: 2021 Frans van Dorsselaer
//
// SPDX-License-Identifier: MIT

using System.Threading;
using System.Threading.Tasks;

namespace BlazorServerPush
{
    public class GlobalCounter : NotifyPropertyChanged
    {
        long _Count;
        public long Count
        {
            get { return Interlocked.Read(ref _Count); }
        }

        public void Increment()
        {
            Interlocked.Increment(ref _Count);
            OnNotifyPropertyChanged(nameof(Count));
        }

        bool _Enabled;
        public bool Enabled
        {
            get { return _Enabled; }
            set
            {
                if (SetProperty(ref _Enabled, value))
                {
                    var myGeneration = ++Generation;
                    if (value)
                    {
                        Task.Run(() =>
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
}
