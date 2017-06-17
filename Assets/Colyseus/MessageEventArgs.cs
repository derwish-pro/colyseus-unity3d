using System;

namespace Colyseus
{
	/// <summary>
	/// Representation of a message received from the server.
	/// </summary>
	public class MessageEventArgs : EventArgs
	{
		/// <summary>
		/// Target <see cref="Room"/> affected by this message. May be null.
		/// </summary>
		public BaseRoom room = null;

		/// <summary>
		/// Data coming from the server.
		/// </summary>
		public object data = null;

		/// <summary>
		/// </summary>
		public MessageEventArgs(BaseRoom room, object data = null)
		{
			this.room = room;
			this.data = data;
		}
	}

	/// <summary>
	/// Room Update Message
	/// </summary>
	public class RoomUpdateEventArgs<T> : EventArgs where T : class
	{
		/// <summary>
		/// Affected <see cref="Room" /> instance.
		/// </summary>
		public Room<T> room = null;

		/// <summary>
		/// New state of the <see cref="Room" />
		/// </summary>
		public T state;

		/// <summary>
		/// Patches applied to the <see cref="Room" /> state.
		/// </summary>
		public string patches = null;

		/// <summary>
		/// </summary>
		public RoomUpdateEventArgs(Room<T> room, T state, string patches = null)
		{
			this.room = room;
			this.state = state;
			this.patches = patches;
		}
	}
}