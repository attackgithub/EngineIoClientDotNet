﻿using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
//using log4net;
using System.Runtime.CompilerServices;
using EngineIoClientDotNet.Modules;

namespace Quobject.EngineIoClientDotNet.ComponentEmitter
{

    /// <remarks>
    /// The event emitter which is ported from the JavaScript module.
    /// <see href="https://github.com/component/emitter">https://github.com/component/emitter</see>
    /// </remarks>
    public class Emitter
    {
        private ImmutableDictionary<string, ImmutableList<IListener>> callbacks;

        private ImmutableDictionary<IListener, IListener> _onceCallbacks;


        public Emitter()
        {
            this.Off();
        }

        /// <summary>
        /// Executes each of listeners with the given args.
        /// </summary>
        /// <param name="eventString">an event name.</param>
        /// <param name="args"></param>
        /// <returns>a reference to this object.</returns>
        public Emitter Emit(string eventString, params object[] args) 
        {
            //var log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod());
            //log.Info("Emitter emit event = " + eventString);
            if (this.callbacks.ContainsKey(eventString))
            {
                ImmutableList<IListener> callbacksLocal = this.callbacks[eventString];                
                foreach (var fn in callbacksLocal)
                {
                    fn.Call(args);
                }                    
            }
            return this;            
        }

        /// <summary>
        ///  Listens on the event.
        /// </summary>
        /// <param name="eventString">event name</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter On(string eventString, IListener fn)
        {
            if (!this.callbacks.ContainsKey(eventString))
            {
                //this.callbacks[eventString] = ImmutableList<IListener>.Empty;
                this.callbacks = this.callbacks.Add(eventString, ImmutableList<IListener>.Empty);
            }
            ImmutableList<IListener> callbacksLocal = this.callbacks[eventString];
            callbacksLocal = callbacksLocal.Add(fn);
            //this.callbacks[eventString] = callbacksLocal;
            this.callbacks = this.callbacks.Remove(eventString).Add(eventString, callbacksLocal);
            return this;
        }

        /// <summary>
        ///  Listens on the event.
        /// </summary>
        /// <param name="eventString">event name</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter On(string eventString, Action fn)
        {
            var listener = new ListenerImpl(fn);
            return this.On(eventString, listener);
        }

        /// <summary>
        ///  Listens on the event.
        /// </summary>
        /// <param name="eventString">event name</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter On(string eventString, Action<object> fn)
        {
            var listener = new ListenerImpl(fn);
            return this.On(eventString, listener);
        }


        /// <summary>
        /// Adds a one time listener for the event.
        /// </summary>
        /// <param name="eventString">an event name.</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter Once(string eventString, IListener fn)
        {
            var on = new OnceListener(eventString, fn, this);

            _onceCallbacks = _onceCallbacks.Add(fn, on);
            this.On(eventString, on);
            return this;

        }

        /// <summary>
        /// Adds a one time listener for the event.
        /// </summary>
        /// <param name="eventString">an event name.</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter Once(string eventString, Action fn)
        {
            var listener = new ListenerImpl(fn);
            return this.Once(eventString, listener);
        }

        /// <summary>
        /// Removes all registered listeners.
        /// </summary>
        /// <returns>a reference to this object.</returns>
        public Emitter Off()
        {
            callbacks = ImmutableDictionary<string, ImmutableList<IListener>>.Empty;
            _onceCallbacks = ImmutableDictionary<IListener, IListener>.Empty;
            return this;
        }

        /// <summary>
        /// Removes all listeners of the specified event.
        /// </summary>
        /// <param name="eventString">an event name</param>
        /// <returns>a reference to this object.</returns>
        public Emitter Off(string eventString)
        {
            ImmutableList<IListener> retrievedValue;
            if (!callbacks.TryGetValue(eventString, out retrievedValue))
            {
                var log = LogManager.GetLogger(Global.CallerName());
                log.Info(string.Format("Emitter.Off Could not remove {0}", eventString));
            }

            if (retrievedValue != null)
            {
                callbacks = callbacks.Remove(eventString);

                foreach (var listener in retrievedValue)
                {
                    _onceCallbacks.Remove(listener);
                }
            }           
            return this;
        }


        /// <summary>
        /// Removes the listener
        /// </summary>
        /// <param name="eventString">an event name</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object.</returns>
        public Emitter Off(string eventString, IListener fn)
        {
            if (this.callbacks.ContainsKey(eventString))
            {
                ImmutableList<IListener> callbacksLocal = this.callbacks[eventString];
                IListener offListener;
                _onceCallbacks.TryGetValue(fn,out offListener);
                _onceCallbacks = _onceCallbacks.Remove(fn);


                if (callbacksLocal.Count > 0 && callbacksLocal.Contains(offListener ?? fn))
                {
                    callbacksLocal = callbacksLocal.Remove(offListener ?? fn);
                    this.callbacks = this.callbacks.Remove(eventString);
                    this.callbacks = this.callbacks.Add(eventString, callbacksLocal);
                }
            }
            return this;
        }

        /// <summary>
        ///  Returns a list of listeners for the specified event.
        /// </summary>
        /// <param name="eventString">an event name.</param>
        /// <returns>a reference to this object</returns>
        public ImmutableList<IListener> Listeners(string eventString)
        {
            if (this.callbacks.ContainsKey(eventString))
            {
                ImmutableList<IListener> callbacksLocal = this.callbacks[eventString];
                return callbacksLocal ?? ImmutableList<IListener>.Empty;
            }
            return ImmutableList<IListener>.Empty;
        }

        /// <summary>
        /// Check if this emitter has listeners for the specified event.
        /// </summary>
        /// <param name="eventString">an event name</param>
        /// <returns>bool</returns>
        public bool HasListeners(string eventString)
        {
            return this.Listeners(eventString).Count > 0;
        }

    }

    public interface IListener {
        void Call(params object[] args);
    }

    public class ListenerImpl : IListener
    {
        private readonly Action fn1; 
        private readonly Action<object> fn;

        public ListenerImpl(Action<object> fn)
        {

            this.fn = fn;
        }

        public ListenerImpl(Action fn)
        {

            this.fn1 = fn;
        }

        public void Call(params object[] args)
        {
            if (fn != null)
            {
                fn(args[0]);
            }
            else
            {
                fn1();
            }
        }
    }

    public class OnceListener : IListener
    {
        private readonly string _eventString;
        private readonly IListener _fn;
        private readonly Emitter _emitter;

        public OnceListener(string eventString, IListener fn, Emitter emitter)
        {
            this._eventString = eventString;
            this._fn = fn;
            this._emitter = emitter;
        }

        void IListener.Call(params object[] args)
        {
            _emitter.Off(_eventString, this);
            _fn.Call(args);
        }
    }
}
