using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Marvin.JsonPatch;
using Marvin.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Colyseus
{
	using PatchListener = Listener<Action<string[], object>>;
	using FallbackPatchListener = Listener<Action<string, string, object>>;

	public struct Listener<T>
	{
		public T callback;
		public string operation;
		public Regex[] rules;
	}

	public abstract class BaseRoom
	{
		protected Client client;

		public String name;

		protected long _id = 0;


		public event EventHandler OnJoin;
		public event EventHandler OnLeave;
		public event EventHandler OnError;

		public event EventHandler<MessageEventArgs> OnPatch;
		public event EventHandler<MessageEventArgs> OnData;



		public BaseRoom(Client client, String name)
		{
			this.client = client;
			this.name = name;
		}

		public long id
		{
			get { return this._id; }
			set
			{
				this._id = value;
				this.OnJoin.Invoke(this, EventArgs.Empty);
			}
		}


		public void Leave(bool requestLeave = true)
		{
			if (requestLeave && this._id > 0)
			{
				this.Send(new object[] { Protocol.LEAVE_ROOM, this._id });
			}
			else
			{
				this.OnLeave.Invoke(this, EventArgs.Empty);
			}
		}

		public void Send(object data)
		{
			this.client.Send(new object[] { Protocol.ROOM_DATA, this._id, data });
		}


		public void EmitError(MessageEventArgs args)
		{
			this.OnError.Invoke(this, args);
		}

		public void ReceiveData(object data)
		{
			if (this.OnData != null)
				this.OnData.Invoke(this, new MessageEventArgs(this, data));
		}

		public abstract void SetState(JObject state, double remoteCurrentTime, long remoteElapsedTime);
		public abstract void ApplyPatch(string patch);
	};


	public class Room<T> : BaseRoom where T : class
	{
		// public DeltaContainer state = new DeltaContainer(new RoomState());
		//public IndexedDictionary<string, object> state;
		public T state;
		private T _previousState;


		public Room(Client client, string name) : base(client, name)
		{
			Reset();
		}

		public event EventHandler<RoomUpdateEventArgs<T>> OnUpdate;

		public override void SetState(JObject state, double remoteCurrentTime, long remoteElapsedTime)
		{
			T st = state.ToObject<T>();
			SetState(st, remoteCurrentTime, remoteElapsedTime);
		}

		public void SetState(T state, double remoteCurrentTime, long remoteElapsedTime)
		{
			this.state = state;

			// TODO:
			// Create a "clock" for remoteCurrentTime / remoteElapsedTime to match the JavaScript API.

			// Creates serializer.
			if (this.OnUpdate != null)
				this.OnUpdate.Invoke(this, new RoomUpdateEventArgs<T>(this, state, null));
		}

		/// <summary>Internal usage, shouldn't be called.</summary>
		public override void ApplyPatch(string patch)
		{
			var patches = JsonConvert.DeserializeObject<JsonPatchDocument<T>>(patch);
			patches.ApplyTo(state);

			CheckPatches(patches);

			//this.state = state
			if (this.OnUpdate != null)
				this.OnUpdate.Invoke(this, new RoomUpdateEventArgs<T>(this, this.state, null));
		}



		private Dictionary<string, List<PatchListener>> listeners;
		private List<FallbackPatchListener> fallbackListeners;

		private Dictionary<string, Regex> matcherPlaceholders = new Dictionary<string, Regex>()
		{
			{":id", new Regex(@"^([a-zA-Z0-9\-_]+)$")},
			{":number", new Regex(@"^([0-9]+)$")},
			{":string", new Regex(@"^(\w+)$")},
			{":axis", new Regex(@"^([xyz])$")},
			{"*", new Regex(@"(.*)")},
		};

		public FallbackPatchListener Listen(Action<string, string, object> callback)
		{
			FallbackPatchListener listener = new FallbackPatchListener
			{
				callback = callback,
				operation = "",
				rules = new Regex[] { }
			};

			this.fallbackListeners.Add(listener);

			return listener;
		}

		public PatchListener Listen(string segments, string operation, Action<string[], object> callback)
		{
			var regexpRules = this.ParseRegexRules(segments.Split('/'));

			PatchListener listener = new PatchListener
			{
				callback = callback,
				operation = operation,
				rules = regexpRules
			};

			this.listeners[operation].Add(listener);

			return listener;
		}

		public void RemoveListener(PatchListener listener)
		{
			for (var i = this.listeners[listener.operation].Count - 1; i >= 0; i--)
			{
				if (this.listeners[listener.operation][i].Equals(listener))
				{
					this.listeners[listener.operation].RemoveAt(i);
				}
			}
		}

		public void RemoveAllListeners()
		{
			this.Reset();
		}

		private void Reset()
		{
			this.listeners = new Dictionary<string, List<PatchListener>>()
			{
				{"add", new List<PatchListener>()},
				{"remove", new List<PatchListener>()},
				{"replace", new List<PatchListener>()}
			};
			this.fallbackListeners = new List<FallbackPatchListener>();
		}

		protected Regex[] ParseRegexRules(string[] rules)
		{
			Regex[] regexpRules = new Regex[rules.Length];

			for (int i = 0; i < rules.Length; i++)
			{
				var segment = rules[i];
				if (segment.IndexOf(':') == 0)
				{
					if (this.matcherPlaceholders.ContainsKey(segment))
					{
						regexpRules[i] = this.matcherPlaceholders[segment];
					}
					else
					{
						regexpRules[i] = this.matcherPlaceholders["*"];
					}
				}
				else
				{
					regexpRules[i] = new Regex(segment);
				}
			}

			return regexpRules;
		}

		public void RegisterPlaceholder(string placeholder, Regex matcher)
		{
			this.matcherPlaceholders[placeholder] = matcher;
		}


		private void CheckPatches(JsonPatchDocument<T> patches)
		{
			var matched = false;

			foreach (var operation in patches.Operations)
			{
				foreach (var listener in listeners[operation.op])
				{
					var matches = this.CheckPatch(operation, listener);
					if (matches.Length > 0)
					{
						listener.callback.Invoke(matches, operation.value);
						matched = true;
					}
				}

				// check for fallback listener
				if (!matched && fallbackListeners.Count > 0)
				{
					foreach (var listener in fallbackListeners)
					{
						listener.callback.Invoke(operation.path, operation.op, operation.value);
					}
				}
			}
		}

		private string[] CheckPatch(Operation patch, PatchListener listener)
		{
			// skip if rules count differ from patch
			// if (patch.path.Length != listener.rules.Length)
			// {
			//     return new string[] { };
			// }

			List<string> pathVars = new List<string>();

			for (var i = 0; i < listener.rules.Length; i++)
			{
				var matches = listener.rules[i].Matches(patch.path);
				if (matches.Count == 0 || matches.Count > 2)
				{
					return new string[] { };
				}
				pathVars.Add(matches[0].ToString());
				// pathVars = pathVars.concat(matches.slice(1));
			}

			return pathVars.ToArray();
		}
	}
}