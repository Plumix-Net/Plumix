# PlumixTemplateApp

A [Plumix](https://plumix.net) app: Flutter's widget model in C#. The widgets live in `PlumixTemplateApp`; each
platform project only hosts them.

| Project | Platform | Host |
| --- | --- | --- |
| `PlumixTemplateApp` | shared widgets | — |
<!--#if (desktop)-->
| `PlumixTemplateApp.Desktop` | Windows, macOS, Linux | Avalonia (`PlumixApplication`) |
<!--#endif-->
<!--#if (android)-->
| `PlumixTemplateApp.Android` | Android 7.0+ | Plumix native host on Impeller (`PlumixActivity`) |
<!--#endif-->
<!--#if (ios)-->
| `PlumixTemplateApp.iOS` | iOS 15+ | Plumix native host on Impeller (`PlumixViewController`) |
<!--#endif-->

## Run
<!--#if (desktop)-->

```bash
dotnet run --project PlumixTemplateApp.Desktop
```
<!--#endif-->
<!--#if (android)-->

Android needs the `android` workload (`dotnet workload install android`), JDK 17 or newer and a device or
emulator:

```bash
dotnet build PlumixTemplateApp.Android -t:Run
```
<!--#endif-->
<!--#if (ios)-->

iOS needs macOS with Xcode and the `ios` workload (`dotnet workload install ios`):

```bash
dotnet build PlumixTemplateApp.iOS -t:Run -r iossimulator-arm64
```
<!--#endif-->
