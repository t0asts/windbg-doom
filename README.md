# WinDbg DOOM

A plugin to play DOOM inside WinDbg.

Video showcase: [https://www.youtube.com/watch?v=lDo061NRSHg](https://www.youtube.com/watch?v=lDo061NRSHg)  

## Build

Ensure you have the .NET 9 SDK installed.

```
dotnet publish -c Release -r win-x64
```

## Use

In WinDbg:  

```
.load <path>\windbg-doom.dll

!doom <path-to-IWAD>
```

**Ctrl+Break** to stop.  
`!help` lists every option.  

## Credits

Credit to Nobuaki Tanaka for [Managed Doom](https://github.com/sinshu/managed-doom), which this plugin uses internally.  
Credit to id Software for DOOM.
