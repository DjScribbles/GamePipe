/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.ComponentModel;

namespace GamePipe.ViewModel
{
	/// <summary>
	/// Base class for all ViewModel classes in the application.
	/// It provides support for property change notifications 
	/// and has a DisplayName property.  This class is abstract.
	/// </summary>
	public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
	{
		#region "Constructor"

		protected ViewModelBase()
		{
		}

		#endregion

		#region "DisplayName"

		/// <summary>
		/// Returns the user-friendly name of this object.
		/// Child classes can set this property to a new value,
		/// or override it to determine the value on-demand.
		/// </summary>
		private string privateDisplayName;
		[System.Xml.Serialization.XmlIgnore()]
		public virtual string DisplayName {
			get { return privateDisplayName; }
			protected set { privateDisplayName = value; }
		}

		#endregion

		#region "Debugging Aides"

		/// <summary>
		/// Warns the developer if this object does not have
		/// a public property with the specified name. This 
		/// method does not exist in a Release build.
		/// </summary>
		[Conditional("DEBUG"), DebuggerStepThrough()]
		public void VerifyPropertyName(string propertyName)
		{
			// Verify that the property name matches a real,  
			// public, instance property on this object.
			if (TypeDescriptor.GetProperties(this)[propertyName] == null) {
				string msg = "Invalid property name: " + propertyName;

				if (this.ThrowOnInvalidPropertyName) {
					throw new Exception(msg);
				} else {
					Debug.Fail(msg);
				}
			}
		}

		/// <summary>
		/// Returns whether an exception is thrown, or if a Debug.Fail() is used
		/// when an invalid property name is passed to the VerifyPropertyName method.
		/// The default value is false, but subclasses used by unit tests might 
		/// override this property's getter to return true.
		/// </summary>
		private bool privateThrowOnInvalidPropertyName;
		protected virtual bool ThrowOnInvalidPropertyName {
			get { return privateThrowOnInvalidPropertyName; }
			set { privateThrowOnInvalidPropertyName = value; }
		}

		#endregion

		#region "INotifyPropertyChanged Members"

		/// <summary>
		/// Raised when a property on this object has a new value.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises this object's PropertyChanged event.
		/// </summary>
		/// <param name="propertyName">The property that has a new value.</param>
		protected virtual void NotifyPropertyChanged(string propertyName)
		{
			this.VerifyPropertyName(propertyName);

			PropertyChangedEventHandler handler = this.PropertyChanged;
			if (handler != null) {
				dynamic e = new PropertyChangedEventArgs(propertyName);
				handler(this, e);
			}
		}

		#endregion

		#region "IDisposable Members"

		/// <summary>
		/// Invoked when this object is being removed from the application
		/// and will be subject to garbage collection.
		/// </summary>
		public void Dispose()
		{
			this.OnDispose();
		}

		/// <summary>
		/// Child classes can override this method to perform 
		/// clean-up logic, such as removing event handlers.
		/// </summary>
		protected virtual void OnDispose()
		{
		}

		#if DEBUG
		/// <summary>
		/// Useful for ensuring that ViewModel objects are properly garbage collected.
		/// </summary>
        ~ViewModelBase()
        {
			string msg = string.Format("{0} ({1}) ({2}) Finalized", this.GetType().Name, this.DisplayName, this.GetHashCode());
			System.Diagnostics.Debug.WriteLine(msg);
		}
		#endif

		#endregion
	}
}
