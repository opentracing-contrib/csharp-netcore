# Release Process

The release process consists of these steps:
1. Create a GitHub release with release notes. The tag name must be a semantic version, prefixed with "v" - e.g. `v0.1.0` or `v0.1.0-rc1`
1. Wait for the AppVeyor build to finish the *tag* build: https://ci.appveyor.com/project/opentracing/csharp-netcore
1. As a signed-in AppVeyor user, click "Deploy" on the build details page and select "NuGet (OpenTracing)".
This will upload the packages to NuGet.org
