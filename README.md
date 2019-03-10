# Updater
Light weight file updater. 

![alt tag](http://i.imgur.com/FztHxfx.png)

* Feed update manifest url as command line argument.
	- i.e. a URL to a text file.
* Update parameters are separated by new lines.
* First paramenter expected is the new package version.
	- This is usually only needed for UpdateCheck.cs, which opens the Updater.
* Second parameter expected is an executable to be run when the updater finishes.
	- Ideally the program that opens the updater, but can be anything.
* Rest of parameters expected will be in pairs of two
	- File download URL
	- File path and/or File name
	
Example:

```
1.2.3.4
fileToRunWhenComplete.exe
(file2 direct download URL)
file2.dll
(file3 direct download URL)
file3.txt
```