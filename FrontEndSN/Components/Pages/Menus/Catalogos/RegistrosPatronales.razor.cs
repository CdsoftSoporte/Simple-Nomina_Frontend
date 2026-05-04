using FrontEndSN.Clases;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class RegistrosPatronales
	{

		private List<RegistroPatronalModel> Lista = new();
		private RegistroPatronalModel Modelo = new();
		private EditContext? editContext;
		private bool isModalVisible = false;

		// Listas para los combos SAT
		private List<CatalogoSatModel> PaisesList = new();
		private List<CatalogoSatModel> EntidadesList = new();
		private List<CatalogoSatModel> RiesgosList = new();
		private List<CatalogoSatModel> OrigenRecursosList = new();

		private List<dynamic> estatusOpciones = new()
		{
			new { Valor = 'S', Texto = "Activo" },
			new { Valor = 'N', Texto = "Inactivo" }
		};

		protected override async Task OnInitializedAsync()
		{
	
			await Task.WhenAll(
				CargarCatalogosSat(),
				CargarLista()
			);
		}

		private async Task CargarCatalogosSat()
		{
			PaisesList = await CargarCatalogo("api/CatalogosSat/Paises");
			EntidadesList = await CargarCatalogo("api/CatalogosSat/EntidadesFederativas");
			RiesgosList = await CargarCatalogo("api/CatalogosSat/RiesgoPuesto");
			OrigenRecursosList = await CargarCatalogo("api/CatalogosSat/OrigenRecurso");
		}

		// Método genérico para cargar cualquier catálogo SAT
		private async Task<List<CatalogoSatModel>> CargarCatalogo(string url)
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) return new();

				var request = new HttpRequestMessage(HttpMethod.Get, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<CatalogoSatModel>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					return resultado?.Data ?? new();
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Error al cargar catálogo: " + ex.Message);
			}
			return new();
		}

		private async Task CargarLista()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var request = new HttpRequestMessage(HttpMethod.Get, "api/RegistrosPatronales/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<RegistroPatronalModel>>>(
						new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					Lista = resultado?.Data ?? new();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo cargar la lista: " + (errorData?.Message ?? "Error inesperado"));
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
			finally
			{
				StateHasChanged();
			}
		}

		private void MostrarModal(RegistroPatronalModel? item = null)
		{
			if (item == null)
			{
				Modelo = new RegistroPatronalModel
				{
					Id = 0,
					Rfc = "",
					RazonSocial = "",
					Descripcion = "",
					SerieTimbrado = "A",
					Activo = 'S'
				};
			}
			else
			{
				Modelo = new RegistroPatronalModel
				{
					Id = item.Id,
					Rfc = item.Rfc,
					RazonSocial = item.RazonSocial,
					Descripcion = item.Descripcion,
					IdPais = item.IdPais,
					IdEntidadFederativa = item.IdEntidadFederativa,
					IdRiesgo = item.IdRiesgo,
					IdOrigenRecursos = item.IdOrigenRecursos,
					Cp = item.Cp,
					SerieTimbrado = item.SerieTimbrado,
					ClaveDeclaranteAf02 = item.ClaveDeclaranteAf02,
					Activo = item.Activo
				};
			}

			editContext = new EditContext(Modelo);
			isModalVisible = true;
			StateHasChanged();
		}

		private async Task Guardar()
		{
			if (editContext == null || !editContext.Validate()) return;

			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");
				if (string.IsNullOrEmpty(token)) { Nav.NavigateTo("/login"); return; }

				var url = Modelo.Id == 0
					? "api/RegistrosPatronales/Agregar"
					: $"api/RegistrosPatronales/Editar/{Modelo.Id}";

				var request = new HttpRequestMessage(HttpMethod.Post, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
				request.Content = JsonContent.Create(Modelo);

				var response = await Http.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					await JS.InvokeVoidAsync("notifier.success",
						Modelo.Id == 0 ? "Registro patronal agregado exitosamente." : "Registro patronal actualizado correctamente.");
					isModalVisible = false;
					await CargarLista();
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();
					await JS.InvokeVoidAsync("notifier.error", "No se pudo guardar: " + (errorData?.Message ?? "Error inesperado"));
				}
			}
			catch (Exception ex)
			{
				await JS.InvokeVoidAsync("notifier.error", "Ocurrió un error: " + ex.Message);
			}
		}


		

		public class RegistroPatronalModel
		{
			public int Id { get; set; }

			[Required(ErrorMessage = "El RFC es obligatorio")]
			[StringLength(13, MinimumLength = 12, ErrorMessage = "El RFC debe tener entre 12 y 13 caracteres")]
			public string Rfc { get; set; } = string.Empty;

			[Required(ErrorMessage = "La razón social es obligatoria")]
			[StringLength(250, ErrorMessage = "Máximo 250 caracteres")]
			[JsonPropertyName("razon_social")]
			public string RazonSocial { get; set; } = string.Empty;

			[StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
			public string? Descripcion { get; set; }

			[Range(1, int.MaxValue, ErrorMessage = "Seleccione un país")]
			[JsonPropertyName("id_pais")]
			public int IdPais { get; set; }

			[Range(1, int.MaxValue, ErrorMessage = "Seleccione una entidad federativa")]
			[JsonPropertyName("id_entidad_federativa")]
			public int IdEntidadFederativa { get; set; }

			[Range(1, int.MaxValue, ErrorMessage = "Seleccione un riesgo de puesto")]
			[JsonPropertyName("id_riesgo")]
			public int IdRiesgo { get; set; }

			[Range(1, int.MaxValue, ErrorMessage = "Seleccione un origen de recursos")]
			[JsonPropertyName("id_origen_recursos")]
			public int IdOrigenRecursos { get; set; }

			[Required(ErrorMessage = "El C.P. es obligatorio")]
			[StringLength(5, MinimumLength = 5, ErrorMessage = "El C.P. debe tener 5 dígitos")]
			public string Cp { get; set; } = string.Empty;

			[JsonPropertyName("serie_timbrado")]
			[Required(ErrorMessage = "La serie es obligatoria")]
			[StringLength(10, MinimumLength = 1, ErrorMessage = "La serie debe tener entre 1 y 10 caracteres")]
			public string? SerieTimbrado { get; set; } = "A";

			[JsonPropertyName("clave_declarante_af02")]
			public string? ClaveDeclaranteAf02 { get; set; }

			public char? Activo { get; set; } = 'S';
		}

	}
}
