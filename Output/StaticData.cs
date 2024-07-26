using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

public static class StaticData
{
	public const string MemberDataPath = @"D:\Work\Unity\YouYouFramework\Client\Assets\GameMain\DataTable\Test\Data\Member.bytes";
	public const string FamilyDataPath = @"D:\Work\Unity\YouYouFramework\Client\Assets\GameMain\DataTable\Test\Data\Family.bytes";

	public static IReadOnlyDictionary<int, Member> Members { get; private set; } = MemberDictionary = new();
	public static IReadOnlyDictionary<int, Family> Familys { get; private set; } = FamilyDictionary = new();

	private static Dictionary<int, Member> MemberDictionary;
	private static Dictionary<int, Family> FamilyDictionary;

	public static async void LoadAllData(Action onComplete)
	{
		await Task.Run(() =>
		{
			LoadOneData<Member>(MemberDataPath);
			LoadOneData<Family>(FamilyDataPath);
		});
		onComplete?.Invoke();
	}

	public static void LoadOneData<T>(string filePath) where T : DataRowBase, new()
	{
		ParseData<T>(File.ReadAllBytes(filePath));
	}

	public static async void LoadOneData<T>(string filePath, Action onComplete) where T : DataRowBase, new()
	{
		await Task.Run(() =>
		{
			LoadOneData<T>(filePath);
		});
		onComplete?.Invoke();
	}

	private static void ParseData<T>(byte[] buffer) where T : DataRowBase, new()
	{
		using MemoryStream memoryStream = new(buffer);
		using BinaryReader binaryReader = new(memoryStream);
		int count = binaryReader.ReadInt32();
		for (int i = 0; i < count; i++)
		{
			T data = new();
			data.ParseData(binaryReader);
		}
	}	
	private static int[] ReadInt32Array(this BinaryReader binaryReader)
	{
		int length = binaryReader.ReadInt32();
		int[] intArray = new int[length];
		for (int i = 0; i < length; i++)
		{
			intArray[i] = binaryReader.ReadInt32();
		}
		return intArray;
	}

	public class DataRowBase
	{
		protected int _id;
		public int Id
		{
			get
			{
				return _id;
			}
		}

		public void ParseData(byte[] dataBytes)
		{
			using MemoryStream memoryStream = new(dataBytes);
			using BinaryReader binaryReader = new(memoryStream);
			_id = binaryReader.ReadInt32();
			ParseData(binaryReader);
		}

		public virtual void ParseData(BinaryReader binaryReader) { }
	}

	/// <summary>
	/// Member
	/// </summary>
	public class Member : DataRowBase
	{
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

		public override void ParseData(BinaryReader binaryReader)
		{
			_id = binaryReader.ReadInt32();
			Name = binaryReader.ReadString();
			Age = binaryReader.ReadInt32();
			Stature = binaryReader.ReadSingle();
			Married = binaryReader.ReadBoolean();
			Family = binaryReader.ReadInt32Array();
			MemberDictionary[_id] = this;
		}
	}

	/// <summary>
	/// Family
	/// </summary>
	public class Family : DataRowBase
	{
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

		public override void ParseData(BinaryReader binaryReader)
		{
			_id = binaryReader.ReadInt32();
			Name = binaryReader.ReadString();
			Path = binaryReader.ReadString();
			Members = binaryReader.ReadInt32Array();
			FamilyDictionary[_id] = this;
		}
	}

}

