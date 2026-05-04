using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static FrontEndSN.Components.Pages.Menus.Catalogos.GruposPrestaciones;
using static FrontEndSN.Components.Pages.Menus.Catalogos.SalariosMinimos;
using static FrontEndSN.Components.Pages.Menus.Catalogos.Uma;

namespace FrontEndSN.Components.Pages.Menus.Catalogos
{
	public partial class Departamentos
	{
		private List<Departamento>? Deptos;

		protected Departamento DeptoModel = new();

		private bool isModalVisible = false;

		private List<PrestacionLey> GruposList = [];

		private EditContext? editContext;

		private List<dynamic> estatusOpciones = [
			new { Valor = 'S', Texto = "Activo" },
			new { Valor = 'N', Texto = "Inactivo" }
		];

		protected override async Task OnInitializedAsync()
		{
			await CargarDepartamentos();
		}

		private async Task CargarDepartamentos()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				if (string.IsNullOrEmpty(token))
				{
					Nav.NavigateTo("/login");
					return;
				}

				var request = new HttpRequestMessage(HttpMethod.Get, "api/Departamentos/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<Departamento>>>(
									new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
					);

					Deptos = resultado.Data ?? [];
		

					await CargarGruposPrestaciones();
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

				await JS.InvokeVoidAsync("notifier.error", $"Ocurrió un error: " + ex.Message);
			}
			finally
			{

				StateHasChanged();
			}
		}

		private async Task CargarGruposPrestaciones()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				if (string.IsNullOrEmpty(token))
				{
					Nav.NavigateTo("/login");
					return;
				}

				var request = new HttpRequestMessage(HttpMethod.Get, "api/GruposPrestaciones/Listar");
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<List<PrestacionLey>>>(
									new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
					);
					GruposList = resultado.Data ?? new();
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

				await JS.InvokeVoidAsync("notifier.error", $"Ocurrió un error: " + ex.Message);
			}
		}

		

		private void MostrarModal(Departamento? item = null)
		{
			if (item == null)
			{
				DeptoModel = new Departamento();
				DeptoModel.Id = 0;
				DeptoModel.Nombre = "";
				DeptoModel.Descripcion = "";
				DeptoModel.Activo = 'S';
			
			}
			else
			{
				DeptoModel = new Departamento { Id = item.Id, Descripcion = item.Descripcion, Nombre = item.Nombre,Activo=item.Activo,IdGrupoPrestacion=item.IdGrupoPrestacion };
			}
			editContext = new EditContext(DeptoModel);
			isModalVisible = true;
			StateHasChanged();
		}


		private async Task Guardar()
		{
			try
			{
				var token = await JS.InvokeAsync<string>("localStorage.getItem", "Token");

				if (editContext == null || !editContext.Validate()) return;

				bool esNuevo = DeptoModel.Id == 0;
				string url = esNuevo ? "api/Departamentos/Agregar" : $"api/Departamentos/Editar/{DeptoModel.Id}";
				var metodo = HttpMethod.Post;

				var request = new HttpRequestMessage(metodo, url);
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

				request.Content = JsonContent.Create(DeptoModel);
				var response = await Http.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					var resultado = await response.Content.ReadFromJsonAsync<RespuestaBase<object>>();

					if (resultado != null && resultado.IsSuccess)
					{

						isModalVisible = false;
						await CargarDepartamentos();

						string mensajeExito = esNuevo ? "Registro creado con éxito" : "Registro actualizado correctamente";
						await JS.InvokeVoidAsync("notifier.success", mensajeExito);
					}
				}
				else
				{
					var errorData = await response.Content.ReadFromJsonAsync<RespuestaError>();

					string mensajePrincipal = errorData?.Message ?? "Error inesperado";
					string detalleTecnico = errorData?.Details ?? "No hay detalles adicionales.";
					await JS.InvokeVoidAsync("notifier.error", "No se pudo guardar la información: " + mensajePrincipal + " " + detalleTecnico);
				}
			}
			catch (Exception ex)
			{

				await JS.InvokeVoidAsync("notifier.error", $"Ocurrió un error: " + ex.InnerException);
			}

		}


		public class Departamento
		{
			public int Id { get; set; }

			[Required(ErrorMessage = "El nombre es obligatorio")]
			[StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
			public string Nombre { get; set; }=string.Empty;

			[Required(ErrorMessage = "La descripción es obligatoria")]
			[StringLength(255, ErrorMessage = "Máximo 255 caracteres")]
			public string Descripcion { get; set; } = string.Empty;

			[Range(1, int.MaxValue, ErrorMessage = "Seleccione un grupo de prestaciones")]
			[JsonPropertyName("id_grupo_prestacion")]
			public int IdGrupoPrestacion { get; set; }
			public char? Activo { get; set; }
			public DateTime? Fecha_Creacion { get; set; }
			public DateTime? Fecha_Actualizacion { get; set; }
		}

	}
}
