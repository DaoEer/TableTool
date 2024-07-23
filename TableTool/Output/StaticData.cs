using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

public static class StaticData
{
	public static IReadOnlyDictionary<int, Member> Members { get; private set; }
	public static IReadOnlyDictionary<int, Family> Familys { get; private set; }

	public static void ParseBinaryData<T>(byte[] buffer) where T : DataRowBase
	{
		List<T> datas = new();
		using MemoryStream memoryStream = new(buffer);
		using BinaryReader binaryReader = new(memoryStream);
		int count = binaryReader.ReadInt32();
		ConstructorInfo constructor = typeof(T).GetConstructor(new[] { typeof(BinaryReader) }) ?? throw new InvalidOperationException($"Type {typeof(T)} does not have a constructor that takes a byte[] parameter.");
		for (int i = 0; i < count; i++)
		{
			T data = (T)constructor.Invoke(new object[] { binaryReader });
			datas.Add(data);
		}
		UpdateData(datas);
	}	
	public static void UpdateData<T>(List<T> datas) where T : DataRowBase
	{
		if (typeof(T).Equals(typeof(Member)))
		{
			Dictionary<int, Member> keyValuePairs = new();
			foreach (var data in datas)
			{
				if (data is Member config)
				{
					keyValuePairs.Add(config.Id, config);
					continue;
				}
				throw new InvalidCastException($"Failed to cast {data.GetType()} to GameConfig");
			}
			Members = keyValuePairs;
			return;
		}

		if (typeof(T).Equals(typeof(Family)))
		{
			Dictionary<int, Family> keyValuePairs = new();
			foreach (var data in datas)
			{
				if (data is Family config)
				{
					keyValuePairs.Add(config.Id, config);
					continue;
				}
				throw new InvalidCastException($"Failed to cast {data.GetType()} to GameConfig");
			}
			Familys = keyValuePairs;
			return;
		}

	}

	public static int[] ReadInt32Array(this BinaryReader binaryReader)
	{
		int length = binaryReader.ReadInt32();
		int[] intArray = new int[length];
		for (int i = 0; i < length; i++)
		{
			intArray[i] = binaryReader.ReadInt32();
		}
		return intArray;
	}

	public abstract class DataRowBase
	{
		public abstract int Id
		{
			get;
		}

		public virtual void ParseData(byte[] dataBytes)
		{

		}
	}

	/// <summary>
	/// Member
	/// </summary>
	public class Member : DataRowBase
	{
		private int _id;

		/// <summary>
		/// 获取场景编号
		/// </summary>
		public override int Id
		{
			get
			{
				return _id;
			}
		}

		/// <summary>
		/// 名字
		/// </summary>
		public string Name
		{
			get;
			private set;
		}

		/// <summary>
		/// 年龄
		/// </summary>
		public int Age
		{
			get;
			private set;
		}

		/// <summary>
		/// 身高
		/// </summary>
		public float Stature
		{
			get;
			private set;
		}

		/// <summary>
		/// 是否已婚
		/// 0 = false
		/// 1 = true
		/// </summary>
		public bool Married
		{
			get;
			private set;
		}

		/// <summary>
		/// 家庭成员
		/// 数组元素使用','隔开
		/// </summary>
		public int[] Family
		{
			get;
			private set;
		}

		public Member(byte[] buffer)
		{
			using MemoryStream memoryStream = new(buffer);
			using BinaryReader binaryReader = new(memoryStream);
			_id = binaryReader.ReadInt32();
			Name = binaryReader.ReadString();
			Age = binaryReader.ReadInt32();
			Stature = binaryReader.ReadSingle();
			Married = binaryReader.ReadBoolean();
			Family = binaryReader.ReadInt32Array();
		}
		public Member(BinaryReader binaryReader)
		{
			_id = binaryReader.ReadInt32();
			Name = binaryReader.ReadString();
			Age = binaryReader.ReadInt32();
			Stature = binaryReader.ReadSingle();
			Married = binaryReader.ReadBoolean();
			Family = binaryReader.ReadInt32Array();
		}
	}

	/// <summary>
	/// Family
	/// </summary>
	public class Family : DataRowBase
	{
		private int _id;

		/// <summary>
		/// 获取场景编号
		/// </summary>
		public override int Id
		{
			get
			{
				return _id;
			}
		}

		/// <summary>
		/// 名字
		/// </summary>
		public string Name
		{
			get;
			private set;
		}

		/// <summary>
		/// 地址
		/// </summary>
		public string Path
		{
			get;
			private set;
		}

		/// <summary>
		/// 家庭成员
		/// </summary>
		public int[] Members
		{
			get;
			private set;
		}

		public Family(byte[] buffer)
		{
			using MemoryStream memoryStream = new(buffer);
			using BinaryReader binaryReader = new(memoryStream);
			_id = binaryReader.ReadInt32();
			Name = binaryReader.ReadString();
			Path = binaryReader.ReadString();
			Members = binaryReader.ReadInt32Array();
		}
		public Family(BinaryReader binaryReader)
		{
			_id = binaryReader.ReadInt32();
			Name = binaryReader.ReadString();
			Path = binaryReader.ReadString();
			Members = binaryReader.ReadInt32Array();
		}
	}

}

