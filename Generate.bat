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