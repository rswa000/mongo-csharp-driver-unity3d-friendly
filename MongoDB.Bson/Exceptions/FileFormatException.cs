using System;
using System.IO;

public class FileFormatException : FileLoadException
{
	public FileFormatException (string message) : base(message)
	{
	}
	
	public FileFormatException (string message, Exception ex) : base(message, ex)
	{
	}
}
