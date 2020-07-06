// Copyright © 2015-2020 Alex Kukhtin. All rights reserved.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace A2v10.Data
{
	public abstract class DynamicClass
	{
		public override String ToString()
		{
			var props = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
			var strProps = String.Join(", ", props.Select(p => $"{p.Name}={p.GetValue(this, null)}"));
			return $"{{{strProps}}}";
		}
	}

	public class DynamicProperty
	{
		public String Name { get; }
		public Type Type { get; }

		public DynamicProperty(String name, Type type)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Type = type ?? throw new ArgumentNullException(nameof(type));
		}

		public Boolean Equals(DynamicProperty other)
		{
			return Name != other.Name && Type == other.Type;
		}
	}

	internal class Signature : IEquatable<Signature>
	{
		private DynamicProperty[] _properties;
		public Int32 _hashCode;

		public Signature(Object obj)
		{
			Init(GetProperties(obj));
		}

		public Signature(IEnumerable<DynamicProperty> properties)
		{
			Init(properties);
		}

		void Init(IEnumerable<DynamicProperty> properties)
		{
			_properties = properties.ToArray();
			_hashCode = 0;

			foreach (DynamicProperty p in properties)
				_hashCode ^= p.Name.GetHashCode() ^ p.Type.GetHashCode();
		}

		public DynamicProperty[] Properties => _properties;

		List<DynamicProperty> GetProperties(Object obj)
		{
			var props = new List<DynamicProperty>();
			var d = obj as IDictionary<String, Object>;
			foreach (var itm in d)
			{
				switch (itm.Value)
				{
					case IList<ExpandoObject> _:
						props.Add(new DynamicProperty(itm.Key, typeof(IList<Object>)));
						break;
					case ExpandoObject _:
					case null:
						props.Add(new DynamicProperty(itm.Key, typeof(Object)));
						break;
					default:
						props.Add(new DynamicProperty(itm.Key, itm.Value.GetType()));
						break;
				}
			}
			return props;
		}

		public override Int32 GetHashCode()
		{
			return _hashCode;
		}

		public override Boolean Equals(Object obj)
		{
			return obj is Signature signature && Equals(signature);
		}

		public Boolean Equals(Signature other)
		{
			if (_properties.Length != other._properties.Length)
				return false;
			for (Int32 i = 0; i < _properties.Length; i++)
			{
				if (!_properties[i].Equals(other._properties[i]))
					return false;
			}
			return true;
		}
	}
}
