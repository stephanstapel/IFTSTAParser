# IFTSTAParser
[![NuGet](https://img.shields.io/nuget/v/BMECat.net?color=blue)](https://www.nuget.org/packages/IFTSTAParser/)

IFTSTAParser  is a .net open source library that allows you to read IFTSTA status information files (tracking files) easily.

# License
Subject to the Apache license http://www.apache.org/licenses/LICENSE-2.0.html

# Installation
Just use nuget or Visual Studio Package Manager and download 'BMECat.net'.

You can find more information about the nuget package here:

[![NuGet](https://img.shields.io/nuget/v/BMECat.net?color=blue)](https://www.nuget.org/packages/IFTSTAParser/)

https://www.nuget.org/packages/IFTSTAParser/

# Usage

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
