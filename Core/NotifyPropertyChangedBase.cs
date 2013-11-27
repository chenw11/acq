using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;

/*
 * Source code from: 
 * http://joshsmithonwpf.wordpress.com/2007/08/29/a-base-class-which-implements-inotifypropertychanged/
 * with some modifications based on comments to that article, and my own thoughts:
 *   - lock on the private dictionary object instead of a public type
 *   - dictionary lookup using TryGet
 *   - constructor/field-initializer cleanup
 *   - Added SetAndNotify_ methods, based on "AssignAndNotify" from #comment-4713
 * */

namespace eas_lab
{

    /// <summary>
    /// Implements the INotifyPropertyChanged interface and 
    /// exposes a RaisePropertyChanged method for derived 
    /// classes to raise the PropertyChange event.
    /// Also exposes SetAndNotify to further simplify writing properties.
    /// </summary>
    /// <remarks> The event arguments created by this class are cached to prevent 
    /// managed heap fragmentation.</remarks>
    [Serializable]
    public abstract class NotifyPropertyChangedBase : Disposable, INotifyPropertyChanged
    {
        
        static readonly Dictionary<string, PropertyChangedEventArgs> eventArgCache
            = new Dictionary<string, PropertyChangedEventArgs>();

        protected NotifyPropertyChangedBase() { }


        /// <summary>
        /// Raised when a public property of this object is set.
        /// </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Returns an instance of PropertyChangedEventArgs for 
        /// the specified property name.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property to create event args for.
        /// </param>		
        public static PropertyChangedEventArgs GetPropertyChangedEventArgs(string propertyName)
        {
            if (String.IsNullOrEmpty(propertyName))
                throw new ArgumentException(
                    "propertyName cannot be null or empty.");

            PropertyChangedEventArgs args;
            lock (eventArgCache)
            {
                if (!eventArgCache.TryGetValue(propertyName, out args))
                {
                    args = new PropertyChangedEventArgs(propertyName);
                    eventArgCache.Add(propertyName, args);
                }
            }
            return args;
        }



        /// <summary>
        /// Derived classes can override this method to
        /// execute logic after a property is set. The 
        /// base implementation does nothing.
        /// </summary>
        /// <param name="propertyName">
        /// The property which was changed.
        /// </param>
        protected virtual void AfterPropertyChanged(string propertyName) { }

        /// <summary>
        /// Attempts to raise the PropertyChanged event, and 
        /// invokes the virtual AfterPropertyChanged method, 
        /// regardless of whether the event was raised or not.
        /// </summary>
        /// <param name="propertyName">
        /// The property which was changed.
        /// </param>
        protected void RaisePropertyChanged(string propertyName, bool async)
        {
            Contract.Requires(propertyName != null);
            this.VerifyProperty(propertyName);

            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                // Get the cached event args.
                PropertyChangedEventArgs args = GetPropertyChangedEventArgs(propertyName);

                // Raise the PropertyChanged event.
                InvokeHandler(handler, args, async);
            }

            this.AfterPropertyChanged(propertyName);
        }

        //protected void RaisePropertyChangedAsync(string propertyName)
        //{
        //    RaisePropertyChanged(propertyName, true);
        //}

        protected void RaisePropertyChanged(string propertyName)
        {
            RaisePropertyChanged(propertyName, false);
        }

        /// <summary>
        /// Automatically calls RaisePropertyChange using the name of the calling method/property as the argument.
        /// Only works when C# compiler version >= 5.0
        /// </summary>
        /// <param name="caller"></param>
        protected void RaisePropertyChangedInferName([CallerMemberName] string caller = "")
        {
            RaisePropertyChanged(caller);
        }


        /// <summary>
        /// Can be override by derived classes to execute asynchronously, or in a different context
        /// </summary>
        protected virtual void InvokeHandler(PropertyChangedEventHandler handler, PropertyChangedEventArgs args, bool async)
        {
            handler(this, args);
        }

        protected void RaisePropertyChanged(params string[] propertyNames)
        {
            foreach (string name in propertyNames)
                RaisePropertyChanged(name);
        }

        protected void RaisePropertyChangedAsync(params string[] propertyNames)
        {
            foreach (string name in propertyNames)
                RaisePropertyChanged(name, true);
        }


        #region Set and notify
        bool _justNotifyAlready(string[] propertyNames)
        {
            Contract.Requires(propertyNames.Length > 0);
            RaisePropertyChanged(propertyNames);
            return true;
        }

        /// <summary>
        /// If the target is not already equal to the value, 
        /// then sets the target equal to value and raises the PropertyChanged events
        /// </summary>
        protected bool SetAndNotify_ValueTypeIComparable<T>(ref T target, T value, params string[] propertyNames)
            where T : struct,IComparable
        {
            if (target.CompareTo(value) == 0) return false;

            target = value;
            return _justNotifyAlready(propertyNames);
        }

        /// <summary>
        /// If the target is not already equal to the value, 
        /// then sets the target equal to value and raises the PropertyChanged events
        /// </summary>
        protected bool SetAndNotify_ValueTypeIEquatable<T>(ref T target, T value, params string[] propertyNames)
            where T : struct, IEquatable<T>
        {
            if (target.Equals(value)) return false;

            target = value;
            return _justNotifyAlready(propertyNames);
        }

        protected bool SetAndNotify_ValueTypeIEquatable_InferName<T>(ref T target, T value, [CallerMemberName] string caller = "")
            where T:struct, IEquatable<T>
        {
            return SetAndNotify_ValueTypeIEquatable(ref target, value, caller);
        }

        /// <summary>
        /// If the target is not already equal to the value, 
        /// then sets the target equal to value and raises the PropertyChanged events
        /// </summary>
        protected bool SetAndNotify_RefTypeIEquatable<T>(ref T target, T value, params string[] propertyNames)
            where T : class, IEquatable<T>
        {
            if ((target == null) && (value == null)) return false;

            if ((target != null) && target.Equals(value)) return false;

            target = value;
            return _justNotifyAlready(propertyNames);
        }

        /// <summary>
        /// If the target and value refer to different objects, or if exactly one is null,
        /// then sets the target equal to value and raises the PropertyChanged events
        /// </summary>
        protected bool SetAndNotify_RefEquality<T>(ref T target, T value, params string[] propertyNames)
            where T : class
        {
            if ((target == null) && (value == null)) return false;

            if (Object.ReferenceEquals(target, value)) return false;

            target = value;
            return _justNotifyAlready(propertyNames);
        }
        #endregion



        [Conditional("DEBUG")]
        protected void VerifyProperty(string propertyName)
        {
            Contract.Requires<ArgumentNullException>(propertyName != null);
            Type type = this.GetType();

            // Look for a public property with the specified name.
            PropertyInfo propInfo = type.GetProperty(propertyName);

            if (propInfo == null)
            {
                // The property could not be found,
                // so alert the developer of the problem.

                string msg = string.Format(
                    "{0} is not a public property of {1}",
                    propertyName, type.FullName);

                Debug.Fail(msg);
            }
        }



        protected override void RunOnceDisposer() { }
    }

}