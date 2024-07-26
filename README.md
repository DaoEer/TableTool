# 配置表工具
使用 C# 基于 ExcelDataReader 开发的轻量化配置表工具，适用于 C# 项目。将 xlsx 和 xls 文件转换成 C# 代码和二进制数据，提供了静态数据管理和数据解析的功能。<br>
ExcelDataReader：https://github.com/ExcelDataReader/ExcelDataReader
## 特性
* 轻量化，不依赖任何其他脚本，所有内容最后生成的只有一个脚本。
* 数据安全，所有数据在解析后无法被修改。
## 使用方法
* 在任意地方创建一个文本文件，将扩展名改为 .bat，按照以下格式编写批处理脚本。项目中有 Generate.bat 为示例。
```
@echo off

rem 双引号内 替换为 表格工具.exe文件 的路径
set TableToolExePath="表格工具程序的地址"

rem 等于号 后面替换为 表格所在文件夹的路径(不能包含空格)
set TablePath=表格所在文件夹的路径

rem 等于号 后面替换为 二进制文件输出的文件夹 的路径(不能包含空格)
set BinaryOutPath=二进制文件输出的文件夹的路径

rem 等于号 后面替换为 C#代码文件输出的文件夹 的路径(不能包含空格)
set CSharpOutPath=C#代码文件输出的文件夹的路径

call %TableToolExePath% %TablePath% %BinaryOutPath% %CSharpOutPath%

pause
```
* 配置好后启动批处理脚本即可。
## 关于表格
* 表格 1-3 行为表头，依次为 属性名 属性类型 注释。
* 表格 1 列固定为 Id，从第 4 行开始填写唯一的 int 类型数据。
* 每个分页名对应生成的数据类类名。分页名不可重复。
* Excel 文件夹中包含表格示例。
* Output 文件夹中有生成的代码文件和二进制文件示例。
## StaticData
* 为方便移植和不依赖任何其他脚本， StaticData 中提供了简易数据加载方法，也可自行实现相关资源加载和管理。
```
// 加载所有配置文件
public static async void LoadAllData(Action onComplete)

// 加载解析单个配置文件
public static void LoadOneData<T>(string filePath) where T : DataRowBase, new()
public static async void LoadOneData<T>(string filePath, Action onComplete) where T : DataRowBase, new()
```