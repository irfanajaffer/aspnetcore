# Validating issue #50260

This sample validates the framework fix for [dotnet/aspnetcore issue #50260](https://github.com/dotnet/aspnetcore/issues/50260), where a static SSR page used to resolve either `RemoteNavigationManager` or `HttpNavigationManager` based on whether interactive server services were registered.

## Files involved

- `Components/Pages/Home.razor` renders the resolved `NavigationManager` type for a static SSR page and posts a form that calls `NavigationManager.NavigateTo("weather")`.
- `Program.cs` controls whether interactive server services are registered.

## Manual validation steps

1. From the repository root, activate the local .NET SDK:

   ```powershell
   . .\activate.ps1
   ```

2. Run the sample:

   ```powershell
   dotnet run --project .\src\Components\Samples\BlazorWebAppPerPage\BlazorWebAppPerPage.csproj
   ```

3. Browse to one of the routes handled by the static SSR page:

   - `/`
   - `/foo`
   - `/foo/bar`

4. Observe the value rendered in the `nav-type` block.

   With the sample as currently checked in, `Program.cs` includes both:

   - `AddInteractiveServerComponents()`
   - `AddInteractiveServerRenderMode()`

   The static SSR page should resolve:

   ```text
   Microsoft.AspNetCore.Components.Endpoints.HttpNavigationManager
   ```

5. Click **Navigate to relative "weather"** to exercise `NavigationManager.NavigateTo("weather")` from the static SSR page.

6. Confirm the navigation goes to `/weather`.

   For example:

   - browsing to `/` should navigate to `/weather`
   - browsing to `/foo` should navigate to `/weather`
   - browsing to `/foo/bar` should navigate to `/weather`

7. Stop the app, then temporarily remove interactive server registration from `Program.cs`:

   - Remove `.AddInteractiveServerComponents()`
   - Remove `.AddInteractiveServerRenderMode()`

   Leave the rest of the sample unchanged.

8. Run the sample again:

   ```powershell
   dotnet run --project .\src\Components\Samples\BlazorWebAppPerPage\BlazorWebAppPerPage.csproj
   ```

9. Browse again to `/`, `/foo`, or `/foo/bar` and re-check `nav-type`.

   The same static SSR page should still resolve:

   ```text
   Microsoft.AspNetCore.Components.Endpoints.HttpNavigationManager
   ```

10. Click **Navigate to relative "weather"** again and confirm navigation still goes to `/weather`.

## Expected results

- **Static SSR scope:** resolves `HttpNavigationManager` regardless of whether interactive server support is enabled elsewhere in the app.
- **Static SSR navigation:** `NavigateTo("weather")` resolves relative to the app base path and navigates to `/weather`.
- **Regression check:** enabling interactive server services no longer changes the `NavigationManager` implementation used by the static SSR page.

## Targeted automated checks

From the repository root:

```powershell
. .\activate.ps1
dotnet test .\src\Components\Endpoints\test\Microsoft.AspNetCore.Components.Endpoints.Tests.csproj --filter "FullyQualifiedName~RazorComponentsServiceCollectionExtensionsTest.AddRazorComponentsWithInteractiveServerComponents_UsesHttpNavigationManagerOutsideCircuitScopes"
dotnet test .\src\Components\Server\test\Microsoft.AspNetCore.Components.Server.Tests.csproj --filter "FullyQualifiedName~ComponentServiceCollectionExtensionsTest.AddServerSideBlazor_"
```
