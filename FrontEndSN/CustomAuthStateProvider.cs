using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
	private readonly IJSRuntime _jsRuntime;
	private static readonly AuthenticationState _anonymous =
		new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

	public CustomAuthStateProvider(IJSRuntime jsRuntime)
	{
		_jsRuntime = jsRuntime;
	}

	public override async Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		try
		{
			var token = await _jsRuntime.InvokeAsync<string>(
				"localStorage.getItem", "Token"
			);

			if (string.IsNullOrEmpty(token))
				return _anonymous;

			var handler = new JwtSecurityTokenHandler();

			// Verificar que sea un JWT válido antes de leerlo
			if (!handler.CanReadToken(token))
				return _anonymous;

			var jwtToken = handler.ReadJwtToken(token);

			// Verificar que el token no haya expirado
			if (jwtToken.ValidTo < DateTime.UtcNow)
				return _anonymous;

			var claims = jwtToken.Claims.ToList();
			var identity = new ClaimsIdentity(claims, "jwt");
			var user = new ClaimsPrincipal(identity);

			return new AuthenticationState(user);
		}
		catch
		{
			// JS Interop no disponible (pre-render), retornar anónimo
			return _anonymous;
		}
	}

	// Llama a esto DESDE AFUERA (ej: después de login/logout)
	public void NotifyUserAuthenticated(string token)
	{
		var handler = new JwtSecurityTokenHandler();
		var jwtToken = handler.ReadJwtToken(token);
		var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
		var user = new ClaimsPrincipal(identity);
		NotifyAuthenticationStateChanged(
			Task.FromResult(new AuthenticationState(user))
		);
	}

	public void NotifyUserLoggedOut()
	{
		NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
	}
}