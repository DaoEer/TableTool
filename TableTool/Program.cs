using ExcelDataReader;
using System.Data;
using System.Text;

internal class Program
{
    private static readonly Dictionary<string, Action<BinaryWriter, string>> _writeFuncByType = new()
        {
            {"int", (writer, value) => {writer.Write(string.IsNullOrWhiteSpace(value) ? default : int.Parse(value));}},
            {"long", (writer, value) => {writer.Write(string.IsNullOrWhiteSpace(value) ? default : long.Parse(value));}},
            {"double", (writer, value) => {writer.Write(string.IsNullOrWhiteSpace(value) ? default : double.Parse(value));}},
            {"float", (writer, value) => {writer.Write(string.IsNullOrWhiteSpace(value) ? default : float.Parse(value));}},
            {"string", (writer, value) => {writer.Write(value);}},
            {"byte", (writer, value) => {writer.Write(string.IsNullOrWhiteSpace(value) ? default : byte.Parse(value));}},
            {"bool", (writer, value) => {writer.Write(value.Equals(1));}},
            {"int[]", (writer, value) => {
                string[] splits = value.Split(',');
                int[] intArray = new int[splits.Length];
                for(int i = 0; i < splits.Length; i++)
                {
                    intArray[i] = int.Parse(splits[i]);
                }
                writer.Write(intArray.Length);
                for (int i = 0; i < intArray.Length; i++)
                {
                    writer.Write(intArray[i]);
                }
            }}
        };
    private static Dictionary<string, string> _readFuncByType = new()
        {
            {"byte", "ReadByte"},
            {"int", "ReadInt32"},
            {"long", "ReadInt64"},
            {"float", "ReadSingle"},
            {"string", "ReadString"},
            {"bool", "ReadBoolean"},
            {"int[]", "ReadInt32Array"}
        };
    private static string _tablePath = string.Empty;
    private static string _binaryOutPath = string.Empty;
    private static string _cSharpOutPath = string.Empty;

    private class TableInfo
    {
        public readonly string Name;
        public readonly Dictionary<int, Tuple<string, string, string>> TableHead;
        public readonly byte[] Data;

        public TableInfo(DataTable dataTable, ref bool isReady)
        {
            TableHead = [];
            Name = dataTable.TableName;

            for (int i = 1; i < dataTable.Columns.Count; i++)
            {
                string name = dataTable.Rows[0][i].ToString().Trim();
                string type = dataTable.Rows[1][i].ToString().Trim();
                string comment = dataTable.Rows[2][i].ToString().Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }
                if (!_writeFuncByType.ContainsKey(type) && !string.IsNullOrWhiteSpace(type))
                {
                    Console.WriteLine($"无法识别“{type}”类型：{dataTable.TableName}表格/{2}行/{i + 1}列\r\n");
                    isReady = false;
                    continue;
                }
                TableHead.Add(i, new Tuple<string, string, string>(name, type, comment));
            }

            HashSet<int> ids = [];
            using MemoryStream memoryStream = new();
            using BinaryWriter binaryWriter = new(memoryStream);
            _writeFuncByType["int"].Invoke(binaryWriter, (dataTable.Rows.Count - 3).ToString());
            for (int i = 3; i < dataTable.Rows.Count; i++)
            {
                int id = int.Parse(dataTable.Rows[i][0].ToString().Trim());
                if (!ids.Add(id))
                {
                    Console.WriteLine($"数据ID重复：{Name}表格/{dataTable.TableName}分页/{i + 1}行\r\n");
                    isReady = false;
                    continue;
                }
                _writeFuncByType["int"].Invoke(binaryWriter, id.ToString());
                foreach (KeyValuePair<int, Tuple<string, string, string>> headInfo in TableHead)
                {
                    try
                    {
                        string value = dataTable.Rows[i][headInfo.Key].ToString().Trim();
                        _writeFuncByType[headInfo.Value.Item2.ToLower()].Invoke(binaryWriter, value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"数据解析错误：{Name}表格/{i + 1}行/{headInfo.Key + 1}列\r\n" + ex.Message);
                        isReady = false;
                    }
                }
            }
            Data = memoryStream.ToArray();
        }
    }

    private static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _tablePath = args[0];
        _binaryOutPath = args[1];
        _cSharpOutPath = args[2];

        Console.WriteLine("开始生成表格数据");
        bool isReady = true;
        List<TableInfo> tables = GetAllTable(_tablePath, ref isReady);
        if (!isReady)
        {
            Console.WriteLine("表格数据生成中断");
            return;
        }

        InitOutFolder(_binaryOutPath);
        foreach (var table in tables)
        {
            using FileStream binaryStream = new(string.Format("{0}\\{1}", _binaryOutPath, table.Name + ".bytes"), FileMode.Create);
            binaryStream.Write(table.Data, 0, table.Data.Length);
        }

        StringBuilder builder = new();
        builder.AppendLine("using System;");
        builder.AppendLine("using System.IO;");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine("using System.Collections.Generic;\r\n");
        builder.AppendLine("public static class StaticData");
        builder.AppendLine("{");

        foreach (TableInfo table in tables)
        {
            builder.AppendLine($"\tpublic const string {table.Name}DataPath = @\"{_binaryOutPath}\\{table.Name}.bytes\";");
        }
        builder.AppendLine();

        foreach (TableInfo table in tables)
        {
            builder.AppendLine($"\tpublic static IReadOnlyDictionary<int, {table.Name}> {table.Name}s {{ get; private set; }} = {table.Name}Dictionary = new();");
        }
        builder.AppendLine();

        foreach (TableInfo table in tables)
        {
            builder.AppendLine($"\tprivate static Dictionary<int, {table.Name}> {table.Name}Dictionary;");
        }
        builder.AppendLine();

        // 加载全部配置表方法
        builder.AppendLine("\tpublic static async void LoadAllData(Action onComplete)");
        builder.AppendLine("\t{");
        builder.AppendLine("\t\tawait Task.Run(() =>");
        builder.AppendLine("\t\t{");
        foreach (TableInfo table in tables)
        {
            builder.AppendLine($"\t\t\tLoadOneData<{table.Name}>({table.Name}DataPath);");
        }
        builder.AppendLine("\t\t});");
        builder.AppendLine("\t\tonComplete?.Invoke();");
        builder.AppendLine("\t}\r\n");

        // 加载单个配置表方法
        builder.AppendLine("\tpublic static void LoadOneData<T>(string filePath) where T : DataRowBase, new()");
        builder.AppendLine("\t{");
        builder.AppendLine("\t\tParseData<T>(File.ReadAllBytes(filePath));");
        builder.AppendLine("\t}\r\n");

        // 异步加载单个配置表方法
        builder.AppendLine("\tpublic static async void LoadOneData<T>(string filePath, Action onComplete) where T : DataRowBase, new()");
        builder.AppendLine("\t{");
        builder.AppendLine("\t\tawait Task.Run(() =>");
        builder.AppendLine("\t\t{");
        builder.AppendLine("\t\t\tLoadOneData<T>(filePath);");
        builder.AppendLine("\t\t});");
        builder.AppendLine("\t\tonComplete?.Invoke();");
        builder.AppendLine("\t}\r\n");

        // 配置表解析方法
        builder.AppendLine("\tprivate static void ParseData<T>(byte[] buffer) where T : DataRowBase, new()");
        builder.AppendLine("\t{");
        builder.AppendLine("\t\tusing MemoryStream memoryStream = new(buffer);");
        builder.AppendLine("\t\tusing BinaryReader binaryReader = new(memoryStream);");
        builder.AppendLine("\t\tint count = binaryReader.ReadInt32();");
        builder.AppendLine("\t\tfor (int i = 0; i < count; i++)");
        builder.AppendLine("\t\t{");
        builder.AppendLine("\t\t\tT data = new();");
        builder.AppendLine("\t\t\tdata.ParseData(binaryReader);");
        builder.AppendLine("\t\t}");
        builder.AppendLine("\t}\r\t");

        // BinaryReader扩展方法
        builder.AppendLine("\tprivate static int[] ReadInt32Array(this BinaryReader binaryReader)");
        builder.AppendLine("\t{");
        builder.AppendLine("\t\tint length = binaryReader.ReadInt32();");
        builder.AppendLine("\t\tint[] intArray = new int[length];");
        builder.AppendLine("\t\tfor (int i = 0; i < length; i++)");
        builder.AppendLine("\t\t{");
        builder.AppendLine("\t\t\tintArray[i] = binaryReader.ReadInt32();");
        builder.AppendLine("\t\t}");
        builder.AppendLine("\t\treturn intArray;");
        builder.AppendLine("\t}\r\n");

        // 数据行基类
        builder.AppendLine("\tpublic class DataRowBase");
        builder.AppendLine("\t{");
        builder.AppendLine("\t\tprotected int _id;");
        builder.AppendLine("\t\tpublic int Id");
        builder.AppendLine("\t\t{");
        builder.AppendLine("\t\t\tget");
        builder.AppendLine("\t\t\t{");
        builder.AppendLine("\t\t\t\treturn _id;");
        builder.AppendLine("\t\t\t}");
        builder.AppendLine("\t\t}\r\n");
        builder.AppendLine("\t\tpublic void ParseData(byte[] dataBytes)");
        builder.AppendLine("\t\t{");
        builder.AppendLine("\t\t\tusing MemoryStream memoryStream = new(dataBytes);");
        builder.AppendLine("\t\t\tusing BinaryReader binaryReader = new(memoryStream);");
        builder.AppendLine("\t\t\t_id = binaryReader.ReadInt32();");
        builder.AppendLine("\t\t\tParseData(binaryReader);");
        builder.AppendLine("\t\t}\r\n");
        builder.AppendLine("\t\tpublic virtual void ParseData(BinaryReader binaryReader) { }");
        builder.AppendLine("\t}\r\n");

        // 数据行类
        foreach (TableInfo table in tables)
        {
            OutFormatAnnotation($"{table.Name}", 1, builder);
            builder.AppendLine($"\tpublic class {table.Name} : DataRowBase");
            builder.AppendLine("\t{");

            foreach (var head in table.TableHead.Values)
            {
                if (string.IsNullOrWhiteSpace(head.Item1)) continue;
                if (string.IsNullOrWhiteSpace(head.Item2)) continue;
                OutFormatAnnotation(head.Item3, 2, builder);
                builder.AppendLine($"\t\tpublic {head.Item2} {head.Item1}");
                builder.AppendLine("\t\t{");
                builder.AppendLine("\t\t\tget;");
                builder.AppendLine("\t\t\tprivate set;");
                builder.AppendLine("\t\t}\r\n");
            }

            builder.AppendLine("\t\tpublic override void ParseData(BinaryReader binaryReader)");
            builder.AppendLine("\t\t{");
            builder.AppendLine("\t\t\t_id = binaryReader.ReadInt32();");
            foreach (var head in table.TableHead.Values)
            {
                if (head.Item2.ToLower().Equals("byte"))
                {
                    builder.AppendLine($"\t\t\t{head.Item1} = (byte)binaryReader.{_readFuncByType[head.Item2]}();");
                    continue;
                }
                builder.AppendLine($"\t\t\t{head.Item1} = binaryReader.{_readFuncByType[head.Item2]}();");
            }
            builder.AppendLine($"\t\t\t{table.Name}Dictionary[_id] = this;");
            builder.AppendLine("\t\t}");
            builder.AppendLine("\t}\r\n");
        }
        builder.AppendLine("}\r\n");

        File.WriteAllText($"{_cSharpOutPath}/StaticData.cs", builder.ToString(), Encoding.UTF8);

        Console.WriteLine("表格数据生成完毕");
    }

    private static List<TableInfo> GetAllTable(string path, ref bool isReady)
    {
        List<TableInfo> tableInfos = [];

        foreach (string item in Directory.GetFiles(path))
        {
            if (Path.GetFileName(item).IndexOf("~$") > -1) continue;
            if (!Path.GetExtension(item).Equals(".xls") && !Path.GetExtension(item).Equals(".xlsx")) continue;
            using FileStream stream = File.Open(item, FileMode.Open, FileAccess.Read, FileShare.Read);
            using DataSet table = Path.GetExtension(item).Equals(".xsl")
                ? ExcelReaderFactory.CreateBinaryReader(stream).AsDataSet()
                : ExcelReaderFactory.CreateOpenXmlReader(stream).AsDataSet();

            foreach (DataTable sheet in table.Tables)
            {
                if (sheet.Rows.Count <= 0) continue;
                if (tableInfos.Any(info => info.Name == sheet.TableName))
                {
                    Console.WriteLine($"表格名字重复：{Path.GetFileName(item)}/{sheet.TableName}");
                    isReady = false;
                    continue;
                }
                tableInfos.Add(new(sheet, ref isReady));
            }
        }

        foreach (string item in Directory.GetDirectories(path))
        {
            tableInfos.AddRange(GetAllTable(item, ref isReady));
        }

        return tableInfos;
    }

    private static void OutFormatAnnotation(string annotation, int tabNum, StringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(annotation)) return;
        StringBuilder tabSbr = new();
        for (int i = tabNum; i > 0; i--)
        {
            tabSbr.Append('\t');
        }
        string tabString = tabSbr.ToString();
        string[] lines = annotation.Split('\n');
        builder.AppendLine($"{tabString}/// <summary>");
        foreach (string line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                builder.AppendLine($"{tabString}/// {line.Trim([' ', ';', ',', '；', '，'])}");
            }
        }
        builder.AppendLine($"{tabString}/// </summary>");
    }

    private static void InitOutFolder(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            return;
        }
        foreach (var file in Directory.EnumerateFiles(path))
        {
            File.Delete(file);
        }
    }
}