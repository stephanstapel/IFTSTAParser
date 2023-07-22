# IFTSTAParser
Parse EDIFACT IFTSTA files easily with C#

You can use the precompiled C# component via:
http://www.nuget.org/packages/IFTSTAParser/

The demo solution shows you how to use the parser:


```csharp
string path = "iftsta-file.txt";
IFTSTADocument document = IFTSTAParser.Load(path);

if (document.CreationDate.HasValue)
{
  // process header information
}

foreach(IFTSTAConsigment consigment in document.Consigments)
{
  // process consignment
}
```

which will then deliver you the list  of consigment objects in the original file.

Please note that I only covered the segments that are absolutely necessary. If you need more fields, I invite you to send pull requests or drop me a message.

## Disclaimer:
The sample.txt file was copied from the IFTSTA sample on the GS1 homepage:
http://www.gs1.at/EANCOM_2002_Release2012/ean02s4/part2/iftsta/examples.htm
