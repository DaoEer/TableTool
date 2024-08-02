using ExcelDataReader;
using System.Data;
using System.Text;

internal class Program
{
    private static Dictionary<string, Action<BinaryWriter, string>> _writeFuncByType = new()
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
    private static HashSet<string> _mainKeyType = new()
    {
        "int",
        "string",
        "long",
        "float",
        "double"
    };
    private static string _tablePath = string.Empty;
    private static string _binaryOutPath = string.Empty;
    private static string _cSharpOutPath = string.Empty;

    private struct TableInfo
    {
        public string Name;
        public Dictionary<int, Tuple<string, string, string>> TableHead;
        public byte[] Data;
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
            builder.AppendLine($"\tprivate const string {table.Name}DataPath = @\"{_binaryOutPath}\\{table.Name}.bytes\";");
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

            builder.AppendLine($"\t\tpublic static IReadOnlyDictionary<int, {table.Name}> Data {{ get; private set; }} = DataDictionary = new();");
            foreach (Tuple<string, string, string> headInfo in table.TableHead.Values)
            {
                if (!headInfo.Item1.StartsWith('#')) continue;
                string name = headInfo.Item1[1..];
                builder.AppendLine($"\t\tpublic static IReadOnlyDictionary<{headInfo.Item2.ToLower()}, {table.Name}> {name}ToData {{ get; private set; }} = {name}ToDataDictionary = new();");
            }
            builder.AppendLine();

            builder.AppendLine($"\t\tprivate static Dictionary<int, {table.Name}> DataDictionary;");
            foreach (Tuple<string, string, string> headInfo in table.TableHead.Values)
            {
                if (!headInfo.Item1.StartsWith('#')) continue;
                string name = headInfo.Item1[1..];
                builder.AppendLine($"\t\tprivate static Dictionary<{headInfo.Item2.ToLower()}, {table.Name}> {name}ToDataDictionary;");
            }
            builder.AppendLine();

            foreach (Tuple<string, string, string> headInfo in table.TableHead.Values)
            {
                if (string.IsNullOrWhiteSpace(headInfo.Item1)) continue;
                if (string.IsNullOrWhiteSpace(headInfo.Item2)) continue;
                string name = headInfo.Item1.StartsWith('#') ? headInfo.Item1[1..] : headInfo.Item1;
                OutFormatAnnotation(headInfo.Item3, 2, builder);
                builder.AppendLine($"\t\tpublic {headInfo.Item2} {name}");
                builder.AppendLine("\t\t{");
                builder.AppendLine("\t\t\tget;");
                builder.AppendLine("\t\t\tprivate set;");
                builder.AppendLine("\t\t}\r\n");
            }

            builder.AppendLine("\t\tpublic override void ParseData(BinaryReader binaryReader)");
            builder.AppendLine("\t\t{");
            builder.AppendLine("\t\t\t_id = binaryReader.ReadInt32();");
            foreach (Tuple<string, string, string> headInfo in table.TableHead.Values)
            {
                string name = headInfo.Item1.StartsWith('#') ? headInfo.Item1[1..] : headInfo.Item1;
                if (headInfo.Item2.ToLower().Equals("byte"))
                {
                    builder.AppendLine($"\t\t\t{name} = (byte)binaryReader.{_readFuncByType[headInfo.Item2]}();");
                    continue;
                }
                builder.AppendLine($"\t\t\t{name} = binaryReader.{_readFuncByType[headInfo.Item2]}();");
            }

            builder.AppendLine($"\t\t\tDataDictionary[_id] = this;");
            foreach (Tuple<string, string, string> headInfo in table.TableHead.Values)
            {
                if (!headInfo.Item1.StartsWith('#')) continue;
                string name = headInfo.Item1[1..];
                builder.AppendLine($"\t\t\t{name}ToDataDictionary[{name}] = this;");
            }

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
                tableInfos.Add(ParseData(sheet, ref isReady));
            }
        }

        foreach (string item in Directory.GetDirectories(path))
        {
            tableInfos.AddRange(GetAllTable(item, ref isReady));
        }

        return tableInfos;
    }

    private static TableInfo ParseData(DataTable dataTable, ref bool isReady)
    {
        TableInfo tableInfo = new()
        {
            TableHead = [],
            Name = dataTable.TableName
        };

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
                Console.WriteLine($"无法识别“{type}”类型：{dataTable.TableName}表格/2行/{i + 1}列\r\n");
                isReady = false;
                continue;
            }
            tableInfo.TableHead.Add(i, new Tuple<string, string, string>(name, type, comment));
        }

        HashSet<int> ids = [];
        Dictionary<int, HashSet<string>> mainKeys = [];
        foreach (KeyValuePair<int, Tuple<string, string, string>> headInfo in tableInfo.TableHead)
        {
            if (!headInfo.Value.Item1.StartsWith('#')) continue;
            if (!_mainKeyType.Contains(headInfo.Value.Item2))
            {
                Console.WriteLine($"无法作为主键“{headInfo.Value.Item2}”类型：{dataTable.TableName}/{headInfo.Key + 1}列");
                isReady = false;
                continue;
            }
            mainKeys.Add(headInfo.Key, []);
        }

        using MemoryStream memoryStream = new();
        using BinaryWriter binaryWriter = new(memoryStream);
        _writeFuncByType["int"].Invoke(binaryWriter, (dataTable.Rows.Count - 3).ToString());
        for (int i = 3; i < dataTable.Rows.Count; i++)
        {
            int id = int.Parse(dataTable.Rows[i][0].ToString().Trim());
            if (!ids.Add(id))
            {
                Console.WriteLine($"数据ID重复：{tableInfo.Name}表格/{dataTable.TableName}分页/{i + 1}行\r\n");
                isReady = false;
                continue;
            }
            _writeFuncByType["int"].Invoke(binaryWriter, id.ToString());
            foreach (KeyValuePair<int, Tuple<string, string, string>> headInfo in tableInfo.TableHead)
            {
                try
                {
                    string value = dataTable.Rows[i][headInfo.Key].ToString().Trim();
                    if (mainKeys.ContainsKey(headInfo.Key))
                    {
                        if (!mainKeys[headInfo.Key].Add(value))
                        {
                            Console.WriteLine($"数据主键“{headInfo.Value.Item1}”值重复：{tableInfo.Name}表格/{dataTable.TableName}分页/{i + 1}行/{headInfo.Key + 1}列\r\n");
                            isReady = false;
                            continue;
                        }
                    }
                    _writeFuncByType[headInfo.Value.Item2.ToLower()].Invoke(binaryWriter, value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"数据解析错误：{tableInfo.Name}表格/{i + 1}行/{headInfo.Key + 1}列\r\n" + ex.Message);
                    isReady = false;
                }
            }
        }
        tableInfo.Data = memoryStream.ToArray();
        return tableInfo;
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