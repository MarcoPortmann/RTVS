# Contributing to RTVS

## Code of Conduct
This project's code of conduct can be found in the [CODE_OF_CONDUCT.md file](CODE_OF_CONDUCT.md)
(v1.4.0 of the http://contributor-covenant.org/ CoC).

## Contributor License Agreement
If your contribution is large enough, you will be asked to sign the Microsoft CLA (the CLA bot will tell you if it's necessary).

## Development

### Prerequisites

1. Visual Studio 2015 Update 1 or higher.
   - You must have C++, Web Tools, and VS Extensibility components (VS SDK) installed.
1. R 3.2.2 or later; either one of:
   - [CRAN R](https://cran.r-project.org/bin/windows/);
   - [Microsoft R Open](https://mran.revolutionanalytics.com/open/).
1. [Wix Tools 3.10](https://wix.codeplex.com/releases/view/617257) (only needed if you want to build the installer).

### Getting the source code

This repository uses git submodules for some of its dependencies, so you will need to clone it with `--recursive` command line
switch to obtain everything that is needed for a successful build:

```shell
git clone --recursive https://github.com/Microsoft/RTVS.git
```

The remaining dependencies are referenced as NuGet packages, and will be automatically downloaded by VS during the build.

### First build and switching between Visual Studio versions
RTVS can be built for *Visual Studio 2015* aka *VS14* and *Visual Studio 15 Preview* aka *VS15*. 	
Accordingly, to build for the former, R.sln must be opened in VS14, and to build the latter, it has to be opened in VS15.

Some of the nuget dependencies for VS14 and VS15 are incompatible:

| Nuget package                        | VS14           | VS15                     | Dependent projects
| ------------------------------------ |---------------:| ------------------------:| ------------------
| Microsoft.VSSDK.BuildTools           | 14.2.25201     | 15.0.25201-Dev15Preview2 | <ul><li>Microsoft.VisualStudio.R.Package</li></ul>
| Microsoft.VisualStudio.ProjectSystem | 14.0.50617-pre | 15.0.183-pre             | <ul><li>Microsoft.VisualStudio.ProjectSystem.FileSystemMirroring</li><li>Microsoft.VisualStudio.ProjectSystem.FileSystemMirroring.Test</li><li>Microsoft.VisualStudio.R.Package</li><li>Microsoft.VisualStudio.R.Package.Test</li><li>Microsoft.VisualStudio.Shell.Mocks</li></ul>

Because of that, `project.json` in the dependent projects has to be updated before they can be built. This can be done:
- By running `msbuild src/build.proj /t:ResetNuget` from the root folder in *Developer Command Prompt for VS2015* for *VS14* or in *Developer Command Prompt for VS15* for *VS15*. Executing `msbuild build.proj /t:Reset` will also erase `bin` and `obj` folders.
- By rebuilding solution several times. 
  - During first run, `project.json` will be replaced by either `project.14.0.json` or `project.15.0.json` in projects that require replacement.
  - During second run, nuget will update its `project.lock.json` files and related `*.nuget.props` and `*.nuget.targets` files
  - During third run, projects will be built
- Manually by replacing `project.json` content and deleting all `project.lock.json`, `*.nuget.props` and `*.nuget.targets` files

This step is required before first build and every time R.sln is opened in the different version of Visual Studio.

### Building and running the product
1. Open `R.sln` solution file in Visual Studio 2015 or Visual Studio 15 Preview.
1. Set `Microsoft.VisualStudio.R.Package` as a startup project.
1. Unload `SetupBundle` project - it has some internal dependencies, and cannot be built by third parties.
1. If you are not planning to build the installer MSI (see next section), you can also unload `Setup`, `SetupRHost` and `SetupCustomActions` projects.
1. Build the solution. Note that this will _not_ build `Setup` by default.
1. Start Debugging (F5).
1. VS experimental instance should start, and you should see "R Tools" entry in the main menu.

### Building the installer
1. Build `Setup` project specifically (right-click on it in Solution Explorer and select "Build").
1. Look for the MSI that it generates under `bin`. Running it will install the product.
