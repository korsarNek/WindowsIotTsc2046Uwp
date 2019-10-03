### UWP Windows 10 IOT Example for handling touch from a Tsc2046 or XPT2046 display.

The TouchPanels project contains all the code for handling input from a TSC2046 or XPT2046 display.

It works by polling the display for new input and then triggering touch events via an InputInjector which makes
the handling of the touch events transparent to the application.

The only initialization logic needed is this:

```csharp
private async void Init()
{
    await Manager.StartDevice();
    await Manager.LoadCalibrationMatrix();
}
```

The project comes with a default calibration matrix, you can trigger the calibration on your own this way:
```csharp
await Manager.Calibrate(CalibrationStyle.CornersAndCenter);
await Manager.SaveCalibrationMatrix();
```
The calibration matrix gets saved in the LocalCacheFolder of the application.

The application including the project needs to add this to its appxmanifest:
```xml
<Package
    xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
    xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest"
    xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
    xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
    IgnorableNamespaces="uap mp rescap"
>
```

```xml
<Capabilities>
    <rescap:Capability Name="inputInjectionBrokered" />
</Capabilities>
```
This is necessary to use the InputInjector.

Based on the code by Morten Nielsen (https://www.hackster.io/dotMorten/windowsiottouch-44af19)
