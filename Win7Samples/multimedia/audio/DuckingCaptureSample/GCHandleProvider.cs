using System.Runtime.InteropServices;

namespace Vanara.PInvoke
{
	/// <summary>Self-disposing wrapper around a <see cref="GCHandle"/>.</summary>
	/// <seealso cref="System.IDisposable"/>
	public class GCHandleProvider : IDisposable
	{
		/// <summary>Initializes a new instance of the <see cref="GCHandleProvider"/> class.</summary>
		/// <param name="target">The target object.</param>
		public GCHandleProvider(object target) => Handle = GCHandle.Alloc(target);

		/// <summary>Initializes a new instance of the <see cref="GCHandleProvider"/> class.</summary>
		/// <param name="target">The target object.</param>
		/// <param name="type">Indicates the type of handle to create.</param>
		public GCHandleProvider(object target, GCHandleType type) => Handle = GCHandle.Alloc(target, type);

		/// <summary>Finalizes an instance of the <see cref="GCHandleProvider"/> class.</summary>
		~GCHandleProvider()
		{
			ReleaseUnmanagedResources();
		}

		/// <summary>Gets the <see cref="GCHandle"/> instance associated with this object.</summary>
		/// <value>The handle.</value>
		public GCHandle Handle { get; }

		/// <summary>Gets the internal representation of the <see cref="GCHandle"/>.</summary>
		/// <value>The handle's pointer.</value>
		public IntPtr Pointer => GCHandle.ToIntPtr(Handle);

		/// <summary>Performs an implicit conversion from <see cref="GCHandleProvider"/> to <see cref="IntPtr"/>.</summary>
		/// <param name="hProv">The <see cref="GCHandleProvider"/> instance.</param>
		/// <returns>The resulting <see cref="IntPtr"/> instance from the conversion.</returns>
		public static implicit operator IntPtr(GCHandleProvider hProv) => hProv.Pointer;

		/// <summary>Gets a <see cref="GCHandle"/>'s target from a pointer.</summary>
		/// <typeparam name="T">The type of the target.</typeparam>
		/// <param name="gcHandle">The <see cref="GCHandle"/> instance.</param>
		/// <returns>The target of the handle.</returns>
		public static T GetTarget<T>(IntPtr gcHandle) => (T)GCHandle.FromIntPtr(gcHandle).Target;

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			ReleaseUnmanagedResources();
			GC.SuppressFinalize(this);
		}

		private void ReleaseUnmanagedResources()
		{
			if (Handle.IsAllocated) Handle.Free();
		}
	}
}