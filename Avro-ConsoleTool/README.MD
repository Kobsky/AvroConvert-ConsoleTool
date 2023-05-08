# AVRO console tool

It's a wrapper on AvroConvert that can be run from the console to automate some tasks or scenarios like avro-tools just oriented for Windows users and C#

## Usage:

	Commands:
	-help	show help message
	-shematoclass or -stc convert avsc shema to C# .cs model
		example:	
			Avro-ConsoleTool.exe -shematoclass my-shema.avsc my-class.cs
			Avro-ConsoleTool.exe -stc my-shema.avsc my-class.cs			

