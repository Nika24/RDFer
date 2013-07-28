@echo off


:: - RDF Export Scripts - ::
:: This script demonstrates how to use the RDFer application
:: Requires .NET (v3.5 or above)
RDFer.exe -c config.xml -i data.xml -o .

:: You can specify the type of output
::RDFer.exe -c config.xml -i data.xml -o . -f ttl

:: You can specify the type of output
::RDFer.exe -c config.xml -i data.xml -o . -f trig