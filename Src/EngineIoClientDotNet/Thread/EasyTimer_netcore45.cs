﻿
using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Quobject.EngineIoClientDotNet.Thread
{
    //from http://www.dailycoding.com/Posts/easytimer__javascript_style_settimeout_and_setinterval_in_c.aspx
    public class EasyTimer
    {
        private DispatcherTimer timer;

        public EasyTimer(DispatcherTimer timer)
        {
            this.timer = timer;
        }


        public static async Task<EasyTimer> SetTimeout(Action method, long delayInMilliseconds)
        {
            //http://stackoverflow.com/questions/10579027/run-code-on-ui-thread-in-winrt
            var dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;
            EasyTimer result = null;
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var timer1 = new DispatcherTimer();

                timer1.Interval = TimeSpan.FromMilliseconds(delayInMilliseconds);
                timer1.Tick += async (source, e) =>
                {
                    timer1.Stop();
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => method());
                };

                timer1.Start();
                result = new EasyTimer(timer1);
            });
            return result;
        }

        internal void Stop()
        {
            this.timer.Stop();
        }
    }


}

