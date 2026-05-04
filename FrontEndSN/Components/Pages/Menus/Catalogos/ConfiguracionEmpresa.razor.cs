using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using static FrontEndSN.Components.Pages.Menus.Catalogos.Departamentos;
using static System.Net.WebRequestMethods;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class ConfiguracionEmpresa
	{
		private ConfiguracionEmpresaModel Modelo = new();
		private EditContext? editContext;

		// Flags de carga de archivos
		private bool subiendoLogo = false;
		private bool subiendoCer = false;
		private bool subiendoKey = false;

		private string? logoBase64;

		private bool cargando = true;

		protected override async Task OnInitializedAsync()
		{
			editContext = new EditContext(Modelo);
			await CargarConfiguracion();
		}

		private async Task CargarConfiguracion()
		{
			cargando = true;
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				if (string.IsNullOrEmpty(token))
				{
					Nav.NavigateTo("/login");
					return;					
				}

				var request = new HttpRequestMessage(HttpMethod.Get, "api/ConfiguracionEmpresa/Obtener");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<ConfiguracionEmpresaModel>>(
									new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
					);

					Modelo = resultado.Data;

					editContext = new EditContext(Modelo);

					if (Modelo.LogoArchivo != null)
					{
						var ext = Path.GetExtension(Modelo.LogoNombre ?? ".png").ToLower();
						var mime = ext == ".png" ? "image/png" : "image/jpeg";
						logoBase64 = $"data:{mime};base64,{Convert.ToBase64String(Modelo.LogoArchivo)}";
					}


				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();

					string mensajePrincipal = errorData?.Message ?? "Error inesperado";
					string detalleTecnico = errorData?.Details ?? "No hay detalles adicionales.";
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la información: " + mensajePrincipal + " " + detalleTecnico);
				}
				

			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Error al cargar configuración: " + ex.Message);
			}
			finally
			{
				cargando = false;
				StateHasChanged();
			}
		}

		private async Task Guardar()
		{
			if (editContext == null || !editContext.Validate()) return;

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Post, "api/ConfiguracionEmpresa/Guardar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(Modelo);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					await JS.InvokeVoidAsync("notifier.success", "Configuración guardada correctamente.");
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "Error al guardar: " + (errorData?.Message ?? "Error inesperado"));
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}

		private async Task SubirArchivo(InputFileChangeEventArgs e, string tipo)
		{
			try
			{
				if (tipo == "logo") subiendoLogo = true;
				if (tipo == "cer") subiendoCer = true;
				if (tipo == "key") subiendoKey = true;
				StateHasChanged();

				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var archivo = e.File;
				var contenido = new MultipartFormDataContent();
				var streamContent = new StreamContent(archivo.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024));
				contenido.Add(streamContent, "archivo", archivo.Name);

				var request = new HttpRequestMessage(HttpMethod.Post, $"api/ConfiguracionEmpresa/SubirArchivo?tipo={tipo}");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = contenido;

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaArchivoResult>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

					// Actualizar nombre y base64 del logo
					if (tipo == "logo")
					{
						logoBase64 = resultado?.Base64;
						Modelo.LogoNombre = resultado?.Nombre;
					}
					if (tipo == "cer") Modelo.CerNombre = resultado?.Nombre;
					if (tipo == "key") Modelo.KeyNombre = resultado?.Nombre;

					await JS.InvokeVoidAsync("notifier.success", "Archivo guardado correctamente.");
				}
				else
				{
					await JS.InvokeVoidAsync("notifier.error", "Error al subir el archivo.");
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Error: " + ex.Message);
			}
			finally
			{
				if (tipo == "logo") subiendoLogo = false;
				if (tipo == "cer") subiendoCer = false;
				if (tipo == "key") subiendoKey = false;
				StateHasChanged();
			}
		}
		public class RespuestaArchivoResult
		{
			public bool IsSuccess { get; set; }
			public string? Nombre { get; set; }
			public string? Base64 { get; set; }
			public string? Mensaje { get; set; }
		}

		public class ConfiguracionEmpresaModel
		{
			public int Id { get; set; }

			[Required(ErrorMessage = "La razón social es obligatoria")]
			[StringLength(200, ErrorMessage = "Máximo 200 caracteres")]
			[JsonPropertyName("razon_social")]
			public string RazonSocial { get; set; } = string.Empty;

			[Required(ErrorMessage = "El RFC es obligatorio")]
			[StringLength(13, MinimumLength = 12, ErrorMessage = "El RFC debe tener entre 12 y 13 caracteres")]
			[JsonPropertyName("rfc")]
			public string Rfc { get; set; } = string.Empty;
			public string? Calle { get; set; }

			[JsonPropertyName("numero_exterior")]
			public string? NumeroExterior { get; set; }

			[JsonPropertyName("numero_interior")]
			public string? NumeroInterior { get; set; }

			public string? Colonia { get; set; }
			public string? Municipio { get; set; }
			public string? Estado { get; set; }

			[JsonPropertyName("codigo_postal")]
			[Required(ErrorMessage = "El C.P. es obligatorio")]
			[StringLength(5, MinimumLength = 5, ErrorMessage = "El C.P. debe tener 5 dígitos")]
			public string? CodigoPostal { get; set; }
			// Archivos binarios
			[JsonPropertyName("logo_archivo")]
			public byte[]? LogoArchivo { get; set; }

			[JsonPropertyName("cer_archivo")]
			public byte[]? CerArchivo { get; set; }

			[JsonPropertyName("key_archivo")]
			public byte[]? KeyArchivo { get; set; }

			// Nombres de archivos
			[JsonPropertyName("logo_nombre")]
			public string? LogoNombre { get; set; }

			[JsonPropertyName("cer_nombre")]
			public string? CerNombre { get; set; }

			[JsonPropertyName("key_nombre")]
			public string? KeyNombre { get; set; }


			[JsonPropertyName("cer_password")]
			public string? CerPassword { get; set; }

			public string? Pac { get; set; } = "SWE";

			[JsonPropertyName("serie_cfdi")]
			public string? SerieCfdi { get; set; } = "A";

			[JsonPropertyName("folio_actual")]
			public int? FolioActual { get; set; } = 1;
		}
	}
}