# 配置表工具
使用 C# 基于 ExcelDataReader 开发的轻量化配置表工具，适用于 C# 项目。将 xlsx 和 xls 文件转换成 C# 代码和二进制数据，提供了静态数据管理和数据解析的功能。<br>
ExcelDataReader：https://github.com/ExcelDataReader/ExcelDataReader
## 特性
* 轻量化，生成的只有一个脚本，放到项目中就能用。
* 数据安全，所有数据在解析后无法被修改。
## 使用方法
* 在 Config.json 中配置相关路径后启动程序。
* 使用生成的 StaticData 脚本当中的 ParseBinaryData<T\>(byte[] buffer) 方法解析加载的二进制数据。
* 通过 StaticData 中自动生成的字段获取相关数据。
## 注意事项
* 生成的 C# 代码中的数据类名是根据表格文件中的分页名来的。
* 分页名不能有重复。